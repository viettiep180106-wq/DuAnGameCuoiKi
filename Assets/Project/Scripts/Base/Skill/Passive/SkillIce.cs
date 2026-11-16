using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Kỹ năng Passive Hệ Thủy: Mưa Băng Xói Mòn theo chuẩn GDD.
/// - Đối tượng: Toàn bộ map ngẫu nhiên (BỎ QUA HOÀN TOÀN rào cản Hàng trước - Hàng sau).
/// - Mốc 3 (Tier 1): 3 tia băng ngẫu nhiên (mỗi tia 50% DMG).
/// - Mốc 6 (Tier 2): 5 tia băng ngẫu nhiên (mỗi tia 70% DMG) + 15% Tỷ lệ Làm Chậm.
/// - Mốc 9 (Tier 3): 8 tia băng ngẫu nhiên (mỗi tia 100% DMG) + 30% Tỷ lệ Đóng Băng.
/// </summary>
public class SkillIce : SkillPassive
{
    [Header("Ice Rain Settings")]
    public float boltDelay = 0.08f;

    public SkillIce()
    {
        stoneType = TypeStone.Ice;
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
        transform.localScale = Vector3.one;
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.DOKill();
            sr.color = new Color(0.2f, 0.8f, 1f, 0.4f);
            sr.transform.localScale = Vector3.one;
        }
    }

    public override void OnDespawn()
    {
        StopAllCoroutines();
        transform.DOKill();
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.DOKill();
            sr.color = new Color(0.2f, 0.8f, 1f, 0.4f);
            sr.transform.localScale = Vector3.one;
        }
        transform.localScale = Vector3.one;
        base.OnDespawn();
    }

    public override void Active()
    {
        base.Active();
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.DOKill();
            sr.color = new Color(0.2f, 0.8f, 1f, 0.4f);
            sr.transform.localScale = Vector3.one * 1.5f;
            sr.DOFade(0f, 0.45f);
        }
        StartCoroutine(FireIceRainRoutine());
    }

    private IEnumerator FireIceRainRoutine()
    {
        int tier = Mathf.Max(1, (int)(skillValue / 3f));
        int boltCount = tier switch
        {
            1 => 3,
            2 => 5,
            _ => 8
        };

        float dmgRatio = tier switch
        {
            1 => 0.50f,
            2 => 0.70f,
            _ => 1.00f
        };

        float slowChance = tier >= 2 ? 0.15f : 0f;
        float freezeChance = tier >= 3 ? 0.30f : 0f;

        var ec = EnemyController.instance != null ? EnemyController.instance : FindFirstObjectByType<EnemyController>();
        List<EnemyBase> aliveList = new List<EnemyBase>();

        if (ec != null && ec.LivingEnemies != null)
        {
            for (int i = 0; i < ec.LivingEnemies.Count; i++)
            {
                var eb = ec.LivingEnemies[i];
                if (eb != null && eb.gameObject.activeInHierarchy && eb.CurrentHP > 0)
                {
                    aliveList.Add(eb);
                }
            }
        }

        if (aliveList.Count == 0)
        {
            DespawnSelf();
            yield break;
        }

        // Bắn tuần tự các tia băng ngẫu nhiên vào các quái trên map
        for (int i = 0; i < boltCount; i++)
        {
            // Lọc lại quái còn sống
            aliveList.RemoveAll(e => e == null || !e.gameObject.activeInHierarchy || e.CurrentHP <= 0);
            if (aliveList.Count == 0) break;

            EnemyBase targetEnemy = aliveList[Random.Range(0, aliveList.Count)];
            if (targetEnemy != null)
            {
                // Hiệu ứng tia băng giáng xuống từ trên trời
                Vector3 strikePos = targetEnemy.transform.position;
                StrikeIceBolt(targetEnemy, strikePos, dmgRatio, slowChance, freezeChance);
            }

            yield return new WaitForSeconds(boltDelay);
        }

        yield return new WaitForSeconds(0.3f);
        DespawnSelf();
    }

    private static GameObject _iceBoltPrefab;

    private void StrikeIceBolt(EnemyBase enemy, Vector3 pos, float dmgRatio, float slowChance, float freezeChance)
    {
        if (enemy == null || enemy.CurrentHP <= 0) return;

        if (_iceBoltPrefab == null)
        {
            _iceBoltPrefab = Resources.Load<GameObject>("Prefab/Base/Skill/Passive/IceBolt");
        }

        // Điểm thả từ trên trời cao (22.0 units) lao sấm sét xuống quái, Z=985 để luôn ở trước quái
        Vector3 targetPos = new Vector3(pos.x, pos.y, 985f);
        Vector3 startSkyPos = targetPos + Vector3.up * 22.0f;

        GameObject bolt = null;
        if (ObjectPoolingManager.Instance != null && _iceBoltPrefab != null)
        {
            bolt = ObjectPoolingManager.Instance.Spawn(_iceBoltPrefab, startSkyPos, Quaternion.identity);
        }
        else if (_iceBoltPrefab != null)
        {
            bolt = Instantiate(_iceBoltPrefab, startSkyPos, Quaternion.identity);
        }

        float fallDuration = 0.20f;
        if (bolt != null)
        {
            bolt.transform.position = startSkyPos;
            bolt.transform.localScale = new Vector3(3.5f, 7.5f, 1f);
            var srBolt = bolt.GetComponent<SpriteRenderer>();
            if (srBolt != null)
            {
                srBolt.sortingLayerName = "Default";
                srBolt.sortingOrder = 50;
            }

            var tr = bolt.GetComponent<TrailRenderer>();
            if (tr != null)
            {
                tr.Clear();
                tr.widthMultiplier = 2.0f;
                tr.sortingLayerName = "Default";
                tr.sortingOrder = 49;
            }

            bolt.transform.DOMove(targetPos, fallDuration).SetEase(Ease.InQuad).OnComplete(() =>
            {
                if (ObjectPoolingManager.Instance != null)
                {
                    ObjectPoolingManager.Instance.Despawn(bolt);
                }
                else
                {
                    Destroy(bolt);
                }

                if (enemy != null && enemy.CurrentHP > 0)
                {
                    var (rawDmg, isCrit) = CalculateDamage(dmgRatio);
                    enemy.TakeDamage(rawDmg, isCrit, GetElementType());
                    EnemyBase.SpawnSkillVFX(targetPos, ElementType.Water);
                    if (Camera.main != null)
                    {
                        Camera.main.transform.DOShakePosition(0.12f, new Vector3(0.22f, 0.22f, 0f), 10, 90f, false, true);
                    }
                    ApplyIceCrowdControl(enemy, slowChance, freezeChance);
                }
            });
        }
        else
        {
            var (rawDmg, isCrit) = CalculateDamage(dmgRatio);
            enemy.TakeDamage(rawDmg, isCrit, GetElementType());
            EnemyBase.SpawnSkillVFX(targetPos, ElementType.Water);
            if (Camera.main != null)
            {
                Camera.main.transform.DOShakePosition(0.12f, new Vector3(0.22f, 0.22f, 0f), 10, 90f, false, true);
            }
            ApplyIceCrowdControl(enemy, slowChance, freezeChance);
        }
    }

    private void ApplyIceCrowdControl(EnemyBase enemy, float slowChance, float freezeChance)
    {
        if (freezeChance > 0f && Random.value < freezeChance)
        {
            EffectPayLoad freezeEffect = new EffectPayLoad
            {
                EffectType = Effect.Freeze,
                duration = 2.0f,
                value = 0.5f,
                slowRate = 0.5f
            };
            enemy.PlayEffect(freezeEffect);
        }
        else if (slowChance > 0f && Random.value < slowChance)
        {
            EffectPayLoad slowEffect = new EffectPayLoad
            {
                EffectType = Effect.Slow,
                duration = 2.5f,
                value = 0.3f,
                slowRate = 0.4f
            };
            enemy.PlayEffect(slowEffect);
        }
    }

    private void DespawnSelf()
    {
        StopAllCoroutines();
        transform.DOKill();
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.DOKill();
        }
        if (gameObject.activeInHierarchy)
        {
            if (ObjectPoolingManager.Instance != null)
                ObjectPoolingManager.Instance.Despawn(gameObject);
            else
                Destroy(gameObject);
        }
    }
}
