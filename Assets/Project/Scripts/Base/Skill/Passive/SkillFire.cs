using UnityEngine;
using DG.Tweening;

/// <summary>
/// Kỹ năng Passive Hệ Hỏa: Bom Cảm Tử theo chuẩn GDD.
/// - Đối tượng: 1 Quái hàng trước + Splash chữ thập.
/// - Mốc 3 (Tier 1): 120% DMG chính, 40% DMG lan.
/// - Mốc 6 (Tier 2): 180% DMG chính, 70% DMG lan.
/// - Mốc 9 (Tier 3): 250% DMG chính, 100% DMG lan + Thiêu Đốt (DoT).
/// </summary>
public class SkillFire : SkillPassive
{
    [Header("Fire Bomb Settings")]
    public float splashRadius = 1.6f;
    public float flightSpeed = 10f;

    public SkillFire()
    {
        stoneType = TypeStone.Fire;
    }

    public override void Active()
    {
        base.Active();
        transform.localScale = Vector3.one * 3.5f;
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 32;
            sr.color = new Color(1f, 0.35f, 0.1f, 1f);
        }

        var ec = EnemyController.instance != null ? EnemyController.instance : FindFirstObjectByType<EnemyController>();
        EnemyBase targetEnemy = ec != null ? ec.GetValidTarget(allowBackRow: false) : null;

        if (targetEnemy != null)
        {
            target = targetEnemy.transform;
            float dist = Vector3.Distance(transform.position, target.position);
            float duration = Mathf.Max(0.2f, dist / flightSpeed);

            transform.DOMove(target.position, duration)
                .SetEase(Ease.InQuad)
                .OnComplete(() => Explode(targetEnemy));
        }
        else
        {
            // Không có quái: Bay thẳng về phía trước (sang phải) rồi tự hủy
            transform.DOMove(transform.position + Vector3.right * 8f, 0.6f)
                .SetEase(Ease.Linear)
                .OnComplete(DespawnSelf);
        }
    }

    private void Explode(EnemyBase directTarget)
    {
        int tier = Mathf.Max(1, (int)(skillValue / 3f));

        float mainDmgRatio = tier switch
        {
            1 => 1.20f,
            2 => 1.80f,
            _ => 2.50f
        };

        float splashDmgRatio = tier switch
        {
            1 => 0.40f,
            2 => 0.70f,
            _ => 1.00f
        };

        // 1. Gây sát thương mục tiêu chính
        if (directTarget != null && directTarget.gameObject.activeInHierarchy && directTarget.CurrentHP > 0)
        {
            var (mainDmg, isCrit) = CalculateDamage(mainDmgRatio);
            directTarget.TakeDamage(mainDmg, isCrit, GetElementType());

            if (tier >= 3)
            {
                ApplyBurnDoT(directTarget);
            }
        }

        // 2. Gây sát thương lan chữ thập cho các quái lân cận
        var ec = EnemyController.instance != null ? EnemyController.instance : FindFirstObjectByType<EnemyController>();
        if (ec != null && ec.LivingEnemies != null)
        {
            Vector3 center = transform.position;
            for (int i = 0; i < ec.LivingEnemies.Count; i++)
            {
                var eb = ec.LivingEnemies[i];
                if (eb != null && eb != directTarget && eb.gameObject.activeInHierarchy && eb.CurrentHP > 0)
                {
                    float dist = Vector2.Distance(eb.transform.position, center);
                    if (dist <= splashRadius)
                    {
                        var (splashDmg, isCrit) = CalculateDamage(splashDmgRatio);
                        eb.TakeDamage(splashDmg, isCrit, GetElementType());

                        if (tier >= 3)
                        {
                            ApplyBurnDoT(eb);
                        }
                    }
                }
            }
        }

        // Hiệu ứng nổ phình to kèm VFX Lửa rực rỡ
        EnemyBase.SpawnSkillVFX(transform.position, ElementType.Fire);
        Camera.main?.transform.DOShakePosition(0.18f, 0.25f, 10, 90f, false, true);
        transform.DOScale(transform.localScale * 1.6f, 0.18f).OnComplete(DespawnSelf);
    }

    private void ApplyBurnDoT(EnemyBase enemy)
    {
        float playerAtk = Player.Instance != null ? Player.Instance.PlayerATK : 200f;
        EffectPayLoad burnEffect = new EffectPayLoad
        {
            EffectType = Effect.DoT,
            duration = 3.0f,
            value = playerAtk * 0.25f,
            tickRate = 1.0f
        };
        enemy.PlayEffect(burnEffect);
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
