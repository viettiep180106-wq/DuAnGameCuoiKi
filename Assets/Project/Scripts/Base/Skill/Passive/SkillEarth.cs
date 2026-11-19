using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Kỹ năng Passive Hệ Thổ: Đứt Gãy Địa Tầng theo chuẩn GDD.
/// - Đối tượng: Toàn bộ Hàng Trước (Cleave). Nếu Hàng trước chết hết, tự động đánh càn Hàng sau.
/// - Mốc 3 (Tier 1): 90% DMG toàn hàng trước.
/// - Mốc 6 (Tier 2): 130% DMG toàn hàng trước + Giảm 20% DEF.
/// - Mốc 9 (Tier 3): 180% DMG toàn hàng trước + Giảm 40% DEF + 20% Tỷ lệ Choáng (Stun).
/// </summary>
public class SkillEarth : SkillPassive
{
    [Header("Earth Cleave Settings")]
    public float slamHeight = 22.0f; // Điểm thả cao vút từ trên trời rơi xuống
    public float impactRadius = 2.5f;

    public SkillEarth()
    {
        stoneType = TypeStone.Earth;
    }

    public override void Active()
    {
        base.Active();

        var ec = EnemyController.instance != null ? EnemyController.instance : FindFirstObjectByType<EnemyController>();
        if (ec == null)
        {
            DespawnSelf();
            return;
        }

        // Xác định nhóm hàng mục tiêu: Ưu tiên Hàng trước, nếu Hàng trước chết sạch thì đánh Hàng sau
        bool frontDead = ec.AllFrontRowEnemiesDead();
        EnemyRow targetRow = frontDead ? EnemyRow.Back : EnemyRow.Front;

        List<EnemyBase> cleaveTargets = new List<EnemyBase>();
        Vector3 centerPos = Vector3.zero;

        if (ec.LivingEnemies != null)
        {
            for (int i = 0; i < ec.LivingEnemies.Count; i++)
            {
                var eb = ec.LivingEnemies[i];
                if (eb != null && eb.gameObject.activeInHierarchy && eb.CurrentHP > 0 && eb.row == targetRow)
                {
                    cleaveTargets.Add(eb);
                    centerPos += eb.transform.position;
                }
            }
        }

        if (cleaveTargets.Count == 0 && ec.LivingEnemies != null)
        {
            for (int i = 0; i < ec.LivingEnemies.Count; i++)
            {
                var eb = ec.LivingEnemies[i];
                if (eb != null && eb.gameObject.activeInHierarchy && eb.CurrentHP > 0)
                {
                    cleaveTargets.Add(eb);
                    centerPos += eb.transform.position;
                }
            }
        }

        if (cleaveTargets.Count == 0)
        {
            // Không có quái nào trên sân -> despawn
            DespawnSelf();
            return;
        }

        centerPos /= cleaveTargets.Count;
        centerPos.z = 985f; // Nổi bật ở phía trước quái (quái ở z=990)

        // Tảng thiên thạch / khối địa chấn to lớn giáng xuống từ trên trời vào tâm hàng
        transform.localScale = Vector3.one * 2.5f;
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.drawMode = SpriteDrawMode.Simple;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 50;
            sr.color = Color.white; // Giữ nguyên màu sắc chi tiết của khối nham thạch
        }

        transform.position = centerPos + Vector3.up * slamHeight;
        transform.DOMove(centerPos, 0.40f).SetEase(Ease.InExpo).OnComplete(() =>
        {
            // Rung chấn mặt đất (khóa trục Z để không rung làm lệch camera frustum)
            if (Camera.main != null)
            {
                Camera.main.transform.DOShakePosition(0.25f, new Vector3(0.35f, 0.35f, 0f), 14, 90f, false, true);
            }
            EnemyBase.SpawnSkillVFX(centerPos, ElementType.Earth);
            ApplyCleaveSlam(cleaveTargets);
            DOVirtual.DelayedCall(0.45f, DespawnSelf);
        });
    }

    private void ApplyCleaveSlam(List<EnemyBase> targets)
    {
        int tier = Mathf.Max(1, (int)(skillValue / 3f));

        float cleaveDmgRatio = tier switch
        {
            1 => 0.90f,
            2 => 1.30f,
            _ => 1.80f
        };

        float defReductionPercent = tier == 2 ? 0.20f : (tier >= 3 ? 0.40f : 0f);
        float stunChance = tier >= 3 ? 0.20f : 0f;

        for (int i = 0; i < targets.Count; i++)
        {
            EnemyBase eb = targets[i];
            if (eb != null && eb.gameObject.activeInHierarchy && eb.CurrentHP > 0)
            {
                var (cleaveDmg, isCrit) = CalculateDamage(cleaveDmgRatio);
                eb.TakeDamage(cleaveDmg, isCrit, GetElementType());

                // Giảm DEF
                if (defReductionPercent > 0f)
                {
                    eb.currentDef = Mathf.Max(0f, eb.currentDef * (1f - defReductionPercent));
                }

                // Tỷ lệ Choáng (Stun)
                if (stunChance > 0f && Random.value < stunChance)
                {
                    EffectPayLoad stunEffect = new EffectPayLoad
                    {
                        EffectType = Effect.Stun,
                        duration = 1.5f
                    };
                    eb.PlayEffect(stunEffect);
                }
            }
        }

        // Rung nhẹ camera tạo cảm giác uy lực
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.transform.DOShakePosition(0.2f, new Vector3(0.15f, 0.15f, 0f), 15, 90f, false, true);
        }
    }

    private void DespawnSelf()
    {
        transform.DOKill();
        if (gameObject.activeInHierarchy)
        {
            ObjectPoolingManager.Instance.Despawn(gameObject);
        }
    }
}
