using System.Collections;
using UnityEngine;
using DG.Tweening;
using System;
using System.Collections.Generic;

public abstract class BaseCharacter : MonoBehaviour, IPoolable
{
    [Header("----Setting UI References----")]
    public TextUI textHP;
    public FillBarUI fillBarUI;
    public SpriteRenderer spriteRenderer;

    [Header("----Runtime State (Internal)----")]
    [HideInInspector] [SerializeField] protected float HP; 
    [HideInInspector] [SerializeField] protected float MaxHP;
    [HideInInspector] [SerializeField] protected ElementType currentElement = ElementType.Wood;

    [HideInInspector] public Tween moveTwen;
    [HideInInspector] public Vector3 currentTargetPos;
    [HideInInspector] public Color colorOrigin;

    public ElementType Element 
    { 
        get => currentElement; 
        set => currentElement = value; 
    }

    public float CurrentHP => HP;
    public float MaxHealth => MaxHP;

    // Abstract properties để các lớp con tự cung cấp theo DataSO của chúng
    public abstract float CurrentDEF { get; }
    public abstract float MoveSpeed { get; }
    public abstract float CurrentATK { get; }

    public event Action<float, bool, ElementType> OnTakeDamageDetail;

    private Dictionary<Effect, BaseEffect> effectDictionary = new Dictionary<Effect, BaseEffect>();

    protected virtual void Awake()
    {
        if (spriteRenderer != null)
        {
            colorOrigin = spriteRenderer.color;
        }

        BaseEffect[] list = GetComponents<BaseEffect>();
        foreach (BaseEffect effect in list)
        {
            if (!effectDictionary.ContainsKey(effect.EffectType))
            {
                effectDictionary.Add(effect.EffectType, effect);
            }
        }
    }

    public virtual void OnSpawn()
    {
        UpdateHPUI(MaxHP > 0 ? HP / MaxHP : 1f);
    }

    public virtual void OnDespawn()
    {
        CleanUpAllEffect();
    }

    /// <summary>
    /// Cho phép các lớp con kích hoạt animation bay/di chuyển tương ứng mà không tốn GetComponent runtime
    /// </summary>
    protected virtual void PlayFlyOrMoveAnimation()
    {
    }

    public virtual void Trans(Vector3 posTarget, Ease easeType = Ease.Linear)
    {
        PlayFlyOrMoveAnimation();

        currentTargetPos = posTarget;
        Vector2 currentPos2D = transform.position;
        Vector2 targetPos2D = posTarget;
        float distance = Vector2.Distance(targetPos2D, currentPos2D);

        if (distance > 0.05f)
        {
            float speed = MoveSpeed > 0f ? MoveSpeed : 1f;
            float duration = distance / speed;
            moveTwen?.Kill();
            moveTwen = transform.DOMove(posTarget, duration).SetEase(easeType).OnComplete(() =>
            {
                moveTwen = null;
            });
        }
    }

    /// <summary>
    /// Nhận sát thương cơ bản
    /// </summary>
    public virtual void TakeDamage(float damage)
    {
        TakeDamage(damage, false, ElementType.Wood);
    }

    /// <summary>
    /// Nhận sát thương chi tiết có tính Ngũ Hành, Bạo Kích và Phòng Thủ
    /// </summary>
    public virtual void TakeDamage(float rawDamage, bool isCrit, ElementType attackerElement)
    {
        float defense = CurrentDEF;
        float elementMultiplier = ElementalHelper.GetMultiplier(attackerElement, currentElement);
        
        // Công thức GDD: (Sát thương x Khắc chế) - Thủ, tối thiểu 1 sát thương
        float finalDamage = Mathf.Max(1f, (rawDamage * elementMultiplier) - defense);

        if (HP - finalDamage > 0)
        {
            HP -= finalDamage;
        }
        else
        {
            HP = 0;
            Deid();
        }

        UpdateHPUI(MaxHP > 0 ? HP / MaxHP : 0f);

        // Phát sự kiện để Floating Damage Text hiển thị
        OnTakeDamageDetail?.Invoke(finalDamage, isCrit, attackerElement);
        if (DamageTextManager.Instance != null)
        {
            DamageTextManager.Instance.SpawnDamage(finalDamage, isCrit, attackerElement, transform.position + Vector3.up * 0.5f);
        }
    }

    public void UpdateHPUI(float current)
    {
        if (fillBarUI != null) fillBarUI.DoFill(current, 0.4f);
        if (textHP != null) textHP.UpdateText(HP, 0.4f);
    }

    public virtual void PlayEffect(EffectPayLoad effectPayLoad)
    {
        if (effectDictionary.TryGetValue(effectPayLoad.EffectType, out BaseEffect effect))
        {
            effect.Active(effectPayLoad);
        }
    }

    public void Health(float buffHp)
    {
        HP = Mathf.Min(MaxHP, HP + buffHp);
        UpdateHPUI(MaxHP > 0 ? HP / MaxHP : 1f);
    }

    public virtual void Deid()
    {
        CleanUpAllEffect();
        StartCoroutine(Death());
    }

    public IEnumerator Death()
    {
        yield return YieldCaches.Wait060;
        ObjectPoolingManager.Instance.Despawn(gameObject);
    }

    public void CleanUpAllEffect()
    {
        foreach (BaseEffect effect in effectDictionary.Values)
        {
            effect.RemoveEffect();
        }
        moveTwen?.Kill();
    }
}

public enum Effect { DoT, Stun, Bleed, Slow, Freeze, Knockback };

public enum TypeAnimationAtivity { isFly, isAttack, isIdel, isAttack_1, isAttack_2, isDied, isHit };

public struct EffectPayLoad
{
    public Effect EffectType;
    public float duration;
    public int rounds; // Số Round tồn tại trong chiến đấu theo lượt (Turn-based)
    public float value;
    public float tickRate; // cd mỗi lần nhảy sát thương
    public float slowRate; 
    public float disKnockback;
}
