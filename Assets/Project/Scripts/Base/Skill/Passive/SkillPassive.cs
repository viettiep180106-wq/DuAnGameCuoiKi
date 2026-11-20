using DG.Tweening;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public class SkillPassive : MonoBehaviour, IPoolable
{
    [Header("Skill Settings")]
    public float moveSpeed = 7f;
    public float timeToLive = 5f;
    public LayerMask targetLayer;
    protected TypeStone stoneType;
    protected float skillValue;
    public Transform target;
    protected bool isDespawned = false;
    protected Rigidbody2D rb;
    protected Collider2D col;

    // Zero-GC buffer for Physics2D Overlap according to unity-performance.md
    private static readonly Collider2D[] _overlapBuffer = new Collider2D[24];

    protected virtual void Awake()
    {
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
    }

    public virtual void OnSpawn()
    {
        target = null; 
        isDespawned = false;
        if (col != null) col.isTrigger = true;
    }

    public virtual void OnDespawn()
    {
        target = null;
        isDespawned = true;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        DOTween.Kill(this.gameObject.GetInstanceID());
    }

    public void Initialize(float value)
    {
        skillValue = value;
    }

    public TypeStone GetTypeStone() => stoneType;

    public ElementType GetElementType()
    {
        return stoneType switch
        {
            TypeStone.Fire => ElementType.Fire,
            TypeStone.Ice => ElementType.Water,
            TypeStone.Water => ElementType.Water,
            TypeStone.Earth => ElementType.Earth,
            TypeStone.Wood => ElementType.Wood,
            TypeStone.Light => ElementType.Metal,
            TypeStone.Wind => ElementType.Metal,
            _ => ElementType.Metal
        };
    }

    /// <summary>
    /// Kiểm tra xem một vị trí trong world space có nằm trong tầm nhìn của Camera (đang hiển thị trên màn hình) hay không
    /// </summary>
    public static bool IsVisibleInCamera(Vector3 worldPos, float padding = 0.2f)
    {
        Camera cam = Camera.main;
        if (cam == null) return true;
        Vector3 vp = cam.WorldToViewportPoint(worldPos);
        return vp.z > 0 && vp.x >= -padding && vp.x <= (1f + padding) && vp.y >= -padding && vp.y <= (1f + padding);
    }

    /// <summary>
    /// Tìm mục tiêu gần nhất sử dụng NonAlloc để đạt hiệu năng Zero-GC, chỉ nhắm vào kẻ địch trong tầm nhìn màn hình
    /// </summary>
    public Transform FindNearest(float radius)
    {
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, radius, _overlapBuffer, targetLayer);
        float minDis = Mathf.Infinity;
        Transform nearest = null;

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = _overlapBuffer[i];
            if (hit == null) continue;

            // Loại trừ quái vật ở tận ngoài màn hình chưa bước vào khung hình
            if (!IsVisibleInCamera(hit.transform.position, 0.25f)) continue;

            float dis = Vector2.Distance(hit.transform.position, transform.position);
            if (dis < minDis)
            {
                minDis = dis;
                nearest = hit.transform;
            }
        }

        return nearest;
    }

    public float GetRateBuffSkill()
    {
        return skillValue / 3f;
    }

    /// <summary>
    /// Tính sát thương theo công thức GDD: (match/3) * playerAtk * baseSkillDamageRatio * Crit
    /// </summary>
    public (float finalRawDamage, bool isCrit) CalculateDamage(float baseSkillDamageRatio = 1.0f)
    {
        float playerAtk = Player.Instance != null ? Player.Instance.PlayerATK : 200f;
        float factor = Mathf.Max(1f, skillValue / 3f);
        
        var (isCrit, critMul) = Player.Instance != null ? Player.Instance.RollCrit() : (false, 1f);

        float raw = factor * playerAtk * baseSkillDamageRatio * critMul;

        // Buff Passive: x2 hiệu quả đá nguyên tố (CD: 2 Round)
        if (InterWaveEventManager.Instance != null && InterWaveEventManager.Instance.CanTriggerDoubleStoneEffect())
        {
            raw *= 2f;
            InterWaveEventManager.Instance.ConsumeDoubleStoneEffect();
            if (DamageTextManager.Instance != null && Player.Instance != null)
            {
                DamageTextManager.Instance.SpawnCustomText("[X2 HIỆU QUẢ ĐÁ!]", new Color(0.3f, 1f, 0.5f), Player.Instance.transform.position + Vector3.up * 1.9f, 1.3f);
            }
        }

        return (raw, isCrit);
    }

    public virtual void Active()
    {
        DOVirtual.DelayedCall(timeToLive, () =>
        {
            if (gameObject.activeSelf) ObjectPoolingManager.Instance.Despawn(gameObject); 
        }).SetId(gameObject.GetInstanceID());
    }
}
