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
    public float splashRadius = 2.6f;
    public float flightSpeed = 24f;
    [SerializeField] private Sprite emberSprite;
    [SerializeField] private Sprite fireballSprite;
    [SerializeField] private FireBombBurst burstPrefab;

    public SkillFire()
    {
        stoneType = TypeStone.Fire;
    }

    public override void Active()
    {
        base.Active();
        transform.localScale = Vector3.one * 5.8f;
        var tr = GetComponent<TrailRenderer>();
        if (tr != null)
        {
            tr.Clear();
            tr.enabled = true;
        }
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 32;
            sr.enabled = true;
            if (emberSprite && fireballSprite)
            {
                sr.sprite = emberSprite;
                sr.color = Color.white;
            }
            else
            {
                sr.color = new Color(1f, 0.4f, 0.1f, 1f);
            }
        }

        var ec = EnemyController.instance != null ? EnemyController.instance : FindFirstObjectByType<EnemyController>();
        EnemyBase targetEnemy = ec != null ? ec.GetValidTarget(allowBackRow: false) : null;

        if (targetEnemy != null)
        {
            target = targetEnemy.transform;
            float dist = Vector3.Distance(transform.position, target.position);
            // Tốc độ bay nhanh gấp đôi, dứt khoát
            float duration = Mathf.Clamp(dist / flightSpeed, 0.08f, 0.22f);

            FlyTo(target.position, duration, Ease.InQuad, sr, () => Explode(targetEnemy));
        }
        else
        {
            // Không có quái: Bay nhanh về phía trước rồi tự hủy
            FlyTo(transform.position + Vector3.right * 8f, 0.25f, Ease.Linear, sr, DespawnSelf);
        }
    }

    private void FlyTo(Vector3 destination, float duration, Ease ease, SpriteRenderer renderer, TweenCallback onArrival)
    {
        Tween movement = transform.DOMove(destination, duration).SetEase(ease);
        if (renderer && emberSprite && fireballSprite)
        {
            DOTween.Sequence()
                .SetTarget(transform)
                .Append(movement)
                .InsertCallback(duration * 0.28f, () => renderer.sprite = fireballSprite)
                .OnComplete(onArrival);
        }
        else
        {
            movement.OnComplete(onArrival);
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
        Camera.main?.transform.DOShakePosition(0.18f, new Vector3(0.25f, 0.25f, 0f), 10, 90f, false, true);
        if (burstPrefab)
        {
            var pool = ObjectPoolingManager.Instance;
            var burst = pool ? pool.Spawn(burstPrefab, transform.position, Quaternion.identity)
                : Instantiate(burstPrefab, transform.position, Quaternion.identity);
            burst.Play(splashRadius, pool);
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr)
                sr.enabled = false;
            // Keep the original projectile lifetime so its trail can finish.
            DOVirtual.DelayedCall(0.18f, DespawnSelf, ignoreTimeScale: false).SetTarget(transform);
        }
        else
        {
            EnemyBase.SpawnSkillVFX(transform.position, ElementType.Fire);
            transform.DOScale(transform.localScale * 1.6f, 0.18f).OnComplete(DespawnSelf);
        }
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

    public override void OnDespawn()
    {
        transform.DOKill();
        base.OnDespawn();
    }
}
