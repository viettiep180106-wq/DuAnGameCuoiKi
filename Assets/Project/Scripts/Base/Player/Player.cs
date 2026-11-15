using System;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(PlayerAnimationController), typeof(BoxCollider2D))]
public class Player : BaseCharacter
{
    public static Player Instance { get; set; }
    public static void SetInstanceForTest(Player p) => Instance = p;

    [Header("----Player Stats (Level 1 GDD)----")]
    [SerializeField] private float playerAtk = 200f;
    [SerializeField] private float critRate = 0.1f;    // 10%
    [SerializeField] private float critDmg = 1.0f;     // +100% dmg khi bạo kích (x2.0)
    [SerializeField] private int energyCores = 50;     // Lõi năng lượng dùng cho Gacha/Sự kiện

    public float PlayerATK => playerAtk;
    public float CritRate => critRate;
    public float CritDamage => critDmg;
    public int EnergyCores => energyCores;
    public float MoveSpeed => data != null && data.Speed_Trans > 0 ? data.Speed_Trans : 0.8f;

    public void ModifyATK(float multiplier)
    {
        playerAtk *= multiplier;
    }

    public void ModifyMaxHP(float multiplier)
    {
        MaxHP *= multiplier;
        HP = Mathf.Min(HP, MaxHP);
    }

    public void ModifyDEF(float delta)
    {
        if (data != null) data.Dev += delta;
    }

    public void ModifyCrit(float deltaRate, float deltaDmg)
    {
        critRate += deltaRate;
        critDmg += deltaDmg;
    }

    public void SetStatsForTest(float hp, float atk, int energy = 50)
    {
        HP = hp;
        MaxHP = hp;
        playerAtk = atk;
        energyCores = energy;
    }

    public float CurrentShield { get; private set; } = 0f;
    public int MetalShieldTier { get; set; } = 0;
    public int MetalShieldRoundCounter { get; set; } = 0;
    public bool HasImmunityCrowdControl { get; set; } = false;
    public int WoodArmorTier { get; set; } = 0;
    public bool HasUsedEmergencyHeal { get; set; } = false;

    public event Action<float> OnShieldChanged;
    public event Action<int> OnEnergyCoresChanged;

    [Header("Shield Visual HUD")]
    private GameObject _activeShieldVisualObj;
    private TMPro.TextMeshPro _shieldHpText;

    private void EnsureShieldHUD()
    {
        if (_shieldHpText == null)
        {
            Transform existing = transform.Find("ShieldHpHUD");
            if (existing != null)
            {
                _shieldHpText = existing.GetComponent<TMPro.TextMeshPro>();
            }
            else
            {
                GameObject hudObj = new GameObject("ShieldHpHUD");
                hudObj.transform.SetParent(transform, false);
                hudObj.transform.localPosition = new Vector3(0f, 1.45f, -0.2f);
                _shieldHpText = hudObj.AddComponent<TMPro.TextMeshPro>();
                _shieldHpText.fontSize = 2.4f;
                _shieldHpText.alignment = TMPro.TextAlignmentOptions.Center;
                _shieldHpText.fontStyle = TMPro.FontStyles.Bold;
                _shieldHpText.outlineWidth = 0.25f;
                _shieldHpText.outlineColor = Color.black;
                _shieldHpText.color = new Color(1f, 0.88f, 0.15f); // Hoàng kim
                var mr = hudObj.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.sortingLayerName = "Default";
                    mr.sortingOrder = 45;
                }
            }
        }
    }

    public void UpdateShieldVisualAndText(GameObject shieldVisual = null)
    {
        EnsureShieldHUD();
        if (shieldVisual != null)
        {
            _activeShieldVisualObj = shieldVisual;
        }

        if (CurrentShield > 0)
        {
            if (_shieldHpText != null)
            {
                _shieldHpText.gameObject.SetActive(true);
                _shieldHpText.text = $"[KHIÊN: {Mathf.RoundToInt(CurrentShield)}]";
            }
            if (_activeShieldVisualObj != null)
            {
                _activeShieldVisualObj.SetActive(true);
            }
        }
        else
        {
            if (_shieldHpText != null)
            {
                _shieldHpText.gameObject.SetActive(false);
            }
            if (_activeShieldVisualObj != null && _activeShieldVisualObj.activeSelf)
            {
                // Khiên vỡ vụn
                EnemyBase.SpawnSkillVFX(transform.position, ElementType.Metal);
                _activeShieldVisualObj.SetActive(false);
            }
        }
    }

    public void AddShield(float amount, GameObject visualObj = null)
    {
        CurrentShield = Mathf.Max(CurrentShield, amount);
        UpdateShieldVisualAndText(visualObj);
        OnShieldChanged?.Invoke(CurrentShield);
    }

    public void AddShield(float amount)
    {
        AddShield(amount, null);
    }

    public void TickRoundMetalShield()
    {
        if (MetalShieldTier == 2)
        {
            MetalShieldRoundCounter++;
            if (MetalShieldRoundCounter % 3 == 0)
            {
                AddShield(MaxHP * 0.15f);
            }
        }
        else if (MetalShieldTier >= 3)
        {
            MetalShieldRoundCounter++;
            if (MetalShieldRoundCounter % 2 == 0)
            {
                AddShield(MaxHP * 0.25f);
            }
        }
    }

    public override void PlayEffect(EffectPayLoad effectPayLoad)
    {
        if (HasImmunityCrowdControl && (effectPayLoad.EffectType == Effect.Stun || effectPayLoad.EffectType == Effect.Freeze || effectPayLoad.EffectType == Effect.Slow))
        {
            HasImmunityCrowdControl = false; // Miễn nhiễm 1 hiệu ứng khống chế đầu tiên theo GDD Tier 3
            return;
        }
        base.PlayEffect(effectPayLoad);
    }

    public void Heal(float amount)
    {
        // Tier 2 Mộc Giáp Hồi Sinh: Tăng 15% lượng hồi máu nhận vào
        if (WoodArmorTier >= 2)
        {
            amount *= 1.15f;
        }

        HP = Mathf.Min(HP + amount, MaxHP);
        UpdateHPUI(MaxHP > 0 ? HP / MaxHP : 1f);

        // Hiển thị Pop-up số máu hồi phục màu xanh lá rực rỡ
        if (DamageTextManager.Instance != null && amount > 0)
        {
            DamageTextManager.Instance.SpawnCustomText($"+{Mathf.RoundToInt(amount)} HP", new Color(0.2f, 1f, 0.35f), transform.position + Vector3.up * 1.8f, 1.4f);
        }
    }

    private PlayerAnimationController animController;

    public readonly Vector3 CombatPosition = new Vector3(-16.0f, -0.7f, 0f);
    public readonly Vector3 MarchingPosition = new Vector3(-5.693f, -0.7f, 0f);

    public void AlignToCombatPosition(float duration = 0f, Action onComplete = null)
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) spriteRenderer.flipX = true; // Sprite gốc quay trái -> flipX=true để quay mặt sang PHẢI về phía quái
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        if (duration > 0f)
        {
            transform.DOKill();
            transform.DOLocalMove(CombatPosition, duration).SetEase(Ease.OutQuad).OnComplete(() => onComplete?.Invoke());
        }
        else
        {
            transform.localPosition = CombatPosition;
            onComplete?.Invoke();
        }
    }

    public void AlignToMarchingPosition(float duration = 0.35f, Action onComplete = null)
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) spriteRenderer.flipX = true; // Luôn quay mặt sang PHẢI theo hướng hành tiến
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        if (duration > 0f)
        {
            transform.DOKill();
            transform.DOLocalMove(MarchingPosition, duration).SetEase(Ease.OutQuad).OnComplete(() => onComplete?.Invoke());
        }
        else
        {
            transform.localPosition = MarchingPosition;
            onComplete?.Invoke();
        }
    }

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
        animController = GetComponent<PlayerAnimationController>();
        AlignToMarchingPosition();

        // Chỉ số Level 1 chuẩn theo GDD
        if (data != null && data.HP > 0)
        {
            HP = data.HP;
            MaxHP = data.HP;
            playerAtk = data.Atk > 0 ? data.Atk : 200f;
            critRate = data.Crit_Rate > 0 ? data.Crit_Rate : 0.1f;
            critDmg = data.Crit_Dmg > 0 ? data.Crit_Dmg : 1.0f;
        }
        else
        {
            HP = 500f;
            MaxHP = 500f;
            playerAtk = 200f;
            critRate = 0.1f;
            critDmg = 1.0f;
        }
        UpdateHPUI(1f);
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
        AlignToCombatPosition();
        // Áp dụng stats chuẩn Level 1 từ GDD
        if (data != null)
        {
            HP = data.HP > 0 ? data.HP : 500f;
            MaxHP = HP;
            playerAtk = data.Atk > 0 ? data.Atk : 200f;
            critRate = data.Crit_Rate > 0 ? data.Crit_Rate : 0.1f;
            critDmg = data.Crit_Dmg > 0 ? data.Crit_Dmg : 1.0f;
        }
        else
        {
            HP = 500f;
            MaxHP = 500f;
        }
        UpdateHPUI(1f);
    }

    /// <summary>
    /// Thử vận may bạo kích theo tỉ lệ GDD
    /// </summary>
    public (bool isCrit, float multiplier) RollCrit()
    {
        bool isCrit = UnityEngine.Random.value < critRate;
        float multiplier = isCrit ? (1f + critDmg) : 1f;
        return (isCrit, multiplier);
    }

    /// <summary>
    /// Thêm hoặc tiêu hao lõi năng lượng
    /// </summary>
    public bool TrySpendEnergy(int amount)
    {
        if (energyCores >= amount)
        {
            energyCores -= amount;
            OnEnergyCoresChanged?.Invoke(energyCores);
            return true;
        }
        return false;
    }

    public void AddEnergy(int amount)
    {
        energyCores += amount;
        OnEnergyCoresChanged?.Invoke(energyCores);
    }

    public override void TakeDamage(float damage)
    {
        TakeDamage(damage, false, ElementType.Wood);
    }

    public override void TakeDamage(float rawDamage, bool isCrit, ElementType attackerElement)
    {
        if (animController != null)
        {
            animController.ActiveAnimation(TypeAnimationAtivity.isHit);
        }

        float defense = data != null ? data.Dev : 0f;
        float elementMultiplier = ElementalHelper.GetMultiplier(attackerElement, currentElement);
        float finalDamage = Mathf.Max(1f, (rawDamage * elementMultiplier) - defense);

        // 1. Khiên Kim Cang (Shield) hấp thụ sát thương trước HP
        if (CurrentShield > 0)
        {
            float absorbed = Mathf.Min(CurrentShield, finalDamage);
            CurrentShield -= absorbed;
            finalDamage -= absorbed;
            UpdateShieldVisualAndText();
            OnShieldChanged?.Invoke(CurrentShield);

            if (DamageTextManager.Instance != null && absorbed > 0)
            {
                DamageTextManager.Instance.SpawnCustomText($"-{Mathf.RoundToInt(absorbed)} [KHIÊN]", new Color(1f, 0.88f, 0.2f), transform.position + Vector3.up * 1.5f, 1.3f);
            }
        }

        // 2. Trừ sát thương dư vào HP
        if (finalDamage > 0)
        {
            if (HP - finalDamage > 0)
            {
                HP -= finalDamage;
            }
            else
            {
                HP = 0;
                Deid();
                return;
            }
            UpdateHPUI(MaxHP > 0 ? HP / MaxHP : 1f);

            // Pop-up số damage nhận vào của Player
            if (DamageTextManager.Instance != null)
            {
                DamageTextManager.Instance.SpawnDamage(finalDamage, isCrit, attackerElement, transform.position + Vector3.up * 0.8f);
            }
        }

        // 3. Mộc Giáp Hồi Sinh: Mỗi khi nhận sát thương, tự động hồi lại % HP tối đa
        if (WoodArmorTier >= 1 && HP > 0)
        {
            float healPercent = WoodArmorTier switch
            {
                1 => 0.03f,
                2 => 0.05f,
                _ => 0.08f
            };
            Heal(MaxHP * healPercent);

            // Tier 3: Nếu HP < 30%, lập tức hồi phục 20% HP tối đa (1 lần/trận)
            if (WoodArmorTier >= 3 && !HasUsedEmergencyHeal && HP < (MaxHP * 0.3f))
            {
                HasUsedEmergencyHeal = true;
                Heal(MaxHP * 0.20f);
            }
        }
    }

    public void PlayAttackAnimation()
    {
        if (animController != null)
        {
            animController.ActiveAnimation(TypeAnimationAtivity.isAttack);
        }
    }

    public void PlayFlyAnimation()
    {
        if (animController != null)
        {
            animController.ActiveAnimation(TypeAnimationAtivity.isFly);
        }
    }

    public void PlayIdleAnimation()
    {
        if (animController != null)
        {
            animController.ActiveAnimation(TypeAnimationAtivity.isIdel);
        }
    }

    public override void Deid()
    {
        CleanUpAllEffect();
        if (animController != null)
        {
            animController.ActiveAnimation(TypeAnimationAtivity.isDied);
        }

        if (GameManager.instance != null)
        {
            GameManager.instance.OnLoseGame?.Invoke(true);
        }
    }
}
