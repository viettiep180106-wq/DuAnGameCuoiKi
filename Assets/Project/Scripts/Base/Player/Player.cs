using System;
using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerAnimationController), typeof(BoxCollider2D))]
public class Player : BaseCharacter
{
    public static Player Instance { get; set; }
    public static void SetInstanceForTest(Player p) => Instance = p;

    [Header("----ScriptableObject Data Reference (Single Source of Truth)----")]
    [SerializeField] private PlayerDataSO playerData;
    public PlayerDataSO PlayerData => playerData;

    [Header("----Player Runtime Stats----")]
    [HideInInspector] [SerializeField] private float playerAtk = 200f;
    [HideInInspector] [SerializeField] private float critRate = 0.1f;    // 10%
    [HideInInspector] [SerializeField] private float critDmg = 1.0f;     // +100% dmg khi bạo kích (x2.0)
    [HideInInspector] [SerializeField] private int energyCores = 50;     // Lõi năng lượng dùng cho Gacha/Sự kiện
    private float _additionalDEF = 0f;

    public float PlayerATK => playerAtk;
    public float CritRate => critRate;
    public float CritDamage => critDmg;
    public int EnergyCores => energyCores;

    public override float CurrentDEF => (playerData != null ? playerData.def : 0f) + _additionalDEF;
    public override float MoveSpeed => playerData != null && playerData.speed > 0f ? playerData.speed : 0.8f;
    public override float CurrentATK => playerAtk;

    public event Action OnStatsUpdated;

    protected override void PlayFlyOrMoveAnimation()
    {
        PlayFlyAnimation();
    }

    public void ModifyATK(float multiplier)
    {
        playerAtk *= multiplier;
        OnStatsUpdated?.Invoke();
    }

    public void ModifyMaxHP(float multiplier)
    {
        MaxHP *= multiplier;
        HP = Mathf.Min(HP, MaxHP);
        UpdateHPUI(MaxHP > 0 ? HP / MaxHP : 1f);
        OnStatsUpdated?.Invoke();
    }

    public void ModifyDEF(float delta)
    {
        _additionalDEF += delta;
        OnStatsUpdated?.Invoke();
    }

    public void ModifyCrit(float deltaRate, float deltaDmg)
    {
        critRate += deltaRate;
        critDmg += deltaDmg;
        OnStatsUpdated?.Invoke();
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
    [SerializeField] private WoodArmorVisual woodArmorVisualPrefab;
    private WoodArmorVisual woodArmorVisual;

    [Header("Turn-based Debuffs & Status Effects (GDD)")]
    public List<ActiveRoundEffect> ActiveRoundEffects = new List<ActiveRoundEffect>();
    public bool IsStunned => StunRounds > 0;
    public int StunRounds = 0;
    public int ExtraSkillCost = 0;
    public int ExtraSkillCostRounds = 0;
    public float AtkDebuffPercent = 0f;
    public int AtkDebuffRounds = 0;
    public float DefDebuffPercent = 0f;
    public int DefDebuffRounds = 0;

    public event Action<float> OnShieldChanged;
    public event Action<int> OnEnergyCoresChanged;

    [Header("Shield Visual HUD")]
    [SerializeField] private ShieldSpriteVisual metalShieldVisual;
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
                hudObj.transform.localPosition = new Vector3(0f, 1.6f, -0.2f);
                _shieldHpText = hudObj.AddComponent<TMPro.TextMeshPro>();
                _shieldHpText.fontSize = 3.2f;
                _shieldHpText.alignment = TMPro.TextAlignmentOptions.Center;
                _shieldHpText.fontStyle = TMPro.FontStyles.Bold;
                _shieldHpText.color = new Color(1f, 0.88f, 0.15f); // Hoàng kim
                if (Application.isPlaying)
                {
                    _shieldHpText.outlineWidth = 0.25f;
                    _shieldHpText.outlineColor = Color.black;
                }
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
            ClearShield();
        }
    }

    public bool HasActiveShield()
    {
        return CurrentShield > 0 && _activeShieldVisualObj != null && _activeShieldVisualObj.activeInHierarchy;
    }

    public void PulseShieldVisual()
    {
        if (_activeShieldVisualObj != null)
        {
            _activeShieldVisualObj.transform.DOKill();
            float baseScale = 14.0f / Mathf.Max(0.01f, transform.localScale.x);
            _activeShieldVisualObj.transform.DOScale(baseScale * 1.15f, 0.15f).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                if (_activeShieldVisualObj != null)
                {
                    _activeShieldVisualObj.transform.DOScale(baseScale, 0.2f).SetEase(Ease.InQuad);
                }
            });
        }
    }

    public void ClearShield()
    {
        CurrentShield = 0f;
        if (_activeShieldVisualObj != null)
        {
            _activeShieldVisualObj.transform.DOKill();
            _activeShieldVisualObj.transform.SetParent(null);
            if (ObjectPoolingManager.Instance != null && _activeShieldVisualObj.activeInHierarchy)
            {
                ObjectPoolingManager.Instance.Despawn(_activeShieldVisualObj);
            }
            else
            {
                if (Application.isPlaying)
                    Destroy(_activeShieldVisualObj);
                else
                    DestroyImmediate(_activeShieldVisualObj);
            }
            _activeShieldVisualObj = null;
        }

        if (_shieldHpText != null)
        {
            _shieldHpText.text = "";
            _shieldHpText.gameObject.SetActive(false);
        }

        OnShieldChanged?.Invoke(0f);
    }

    public void AddShield(float amount, GameObject visualObj = null)
    {
        // Cộng dồn chỉ số khiên chính xác theo yêu cầu GDD
        CurrentShield += amount;
        UpdateShieldVisualAndText(visualObj);
        OnShieldChanged?.Invoke(CurrentShield);
    }

    public void AddShield(float amount)
    {
        AddShield(amount, null);
    }

    internal bool HasMetalShieldVfx => metalShieldVisual && metalShieldVisual.IsReady;

    internal void AddMetalShield(float amount, GameObject visualObj = null)
    {
        AddShield(amount, visualObj);
        if (HasMetalShieldVfx)
            metalShieldVisual.PlayGrant();
    }

    public void TickRoundMetalShield()
    {
        if (MetalShieldTier == 2)
        {
            MetalShieldRoundCounter++;
            if (MetalShieldRoundCounter % 3 == 0)
            {
                AddMetalShield(MaxHP * 0.15f);
            }
        }
        else if (MetalShieldTier >= 3)
        {
            MetalShieldRoundCounter++;
            if (MetalShieldRoundCounter % 2 == 0)
            {
                AddMetalShield(MaxHP * 0.25f);
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

        // Debuff: Khi bị trúng đòn 50% x2 hiệu ứng quái (1 Level)
        if (InterWaveEventManager.Instance != null && InterWaveEventManager.Instance.HasActiveDebuff(BuffDebuffType.Debuff_TangHieuUngQuai))
        {
            if (UnityEngine.Random.value < 0.5f)
            {
                effectPayLoad.value *= 2f;
                if (DamageTextManager.Instance != null)
                {
                    DamageTextManager.Instance.SpawnCustomText("[X2 HIỆU ỨNG QUÁI!]", Color.magenta, transform.position + Vector3.up * 1.8f, 1.3f);
                }
            }
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
        OnStatsUpdated?.Invoke();

        // Hiển thị Pop-up số máu hồi phục màu xanh lá rực rỡ
        if (DamageTextManager.Instance != null && amount > 0)
        {
            DamageTextManager.Instance.SpawnCustomText($"+{Mathf.RoundToInt(amount)} HP", new Color(0.2f, 1f, 0.35f), transform.position + Vector3.up * 1.8f, 1.4f);
        }
    }

    internal bool TryPlayWoodArmorVisual(WoodArmorVisual.Kind kind)
    {
        if (!woodArmorVisualPrefab || !woodArmorVisualPrefab.IsConfigured)
            return false;

        if (!woodArmorVisual)
        {
            woodArmorVisual = Instantiate(woodArmorVisualPrefab, transform.position, Quaternion.identity);
            woodArmorVisual.transform.SetParent(transform, true);
        }

        woodArmorVisual.Play(kind);
        return true;
    }

    private PlayerAnimationController animController;

    public readonly Vector3 CombatPosition = new Vector3(-20.0f, 5.0f, 0f);
    public readonly Vector3 MarchingPosition = new Vector3(-5.693f, 5.0f, 0f);

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
        ClearShield(); // Despawn khiên khi kết thúc wave / bước sang chế độ di chuyển
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

    private void InitializeStats()
    {
        if (playerData != null)
        {
            HP = playerData.maxHP;
            MaxHP = playerData.maxHP;
            playerAtk = playerData.atk;
            critRate = playerData.critRate;
            critDmg = playerData.critDmg;
            energyCores = playerData.initialEnergyCores;
            currentElement = playerData.element;
        }
        else
        {
            HP = 500f;
            MaxHP = 500f;
            playerAtk = 200f;
            critRate = 0.1f;
            critDmg = 1.0f;
            energyCores = 50;
        }
        _additionalDEF = 0f;
        UpdateHPUI(1f);
        OnStatsUpdated?.Invoke();
    }

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
        animController = GetComponent<PlayerAnimationController>();
        AlignToMarchingPosition();
        InitializeStats();
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
        AlignToCombatPosition();
        InitializeStats();
    }

    /// <summary>
    /// Thử vận may bạo kích theo tỉ lệ GDD
    /// </summary>
    public (bool isCrit, float multiplier) RollCrit()
    {
        bool isCrit = UnityEngine.Random.value < critRate;
        float multiplier = isCrit ? (1f + critDmg) : 1f;

        // Debuff: Mỗi khi có chí mạng người chơi tự mất đi 5% lõi năng lượng (1 Level)
        if (isCrit && InterWaveEventManager.Instance != null && InterWaveEventManager.Instance.HasActiveDebuff(BuffDebuffType.Debuff_MatLoiKhiCrit))
        {
            int loss = Mathf.Max(1, Mathf.RoundToInt(energyCores * 0.05f));
            TrySpendEnergy(loss);
            if (DamageTextManager.Instance != null)
            {
                DamageTextManager.Instance.SpawnCustomText($"-{loss} LÕI (CRIT DEBUFF)", new Color(1f, 0.4f, 0.4f), transform.position + Vector3.up * 2.2f, 1.3f);
            }
        }

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
            OnStatsUpdated?.Invoke();
            return true;
        }
        return false;
    }

    public void AddEnergy(int amount)
    {
        energyCores += amount;
        OnEnergyCoresChanged?.Invoke(energyCores);
        OnStatsUpdated?.Invoke();
    }

    public override void TakeDamage(float damage)
    {
        TakeDamage(damage, false, ElementType.Wood, 0f, false);
    }

    public override void TakeDamage(float rawDamage, bool isCrit, ElementType attackerElement)
    {
        TakeDamage(rawDamage, isCrit, attackerElement, 0f, false);
    }

    public void TakeDamage(float rawDamage, bool isCrit, ElementType attackerElement, float ignoreDefPercent, bool ignoreShield)
    {
        if (animController != null)
        {
            animController.ActiveAnimation(TypeAnimationAtivity.isHit);
        }

        // Tính toán thủ sau khi trừ debuff giảm DEF và trừ % xuyên giáp của quái
        float baseDef = CurrentDEF;
        float defense = baseDef * Mathf.Clamp01(1f - DefDebuffPercent);
        if (ignoreDefPercent > 0f)
        {
            defense *= Mathf.Clamp01(1f - ignoreDefPercent);
        }
        defense = Mathf.Max(0f, defense);

        float elementMultiplier = ElementalHelper.GetMultiplier(attackerElement, currentElement);
        float finalDamage = Mathf.Max(1f, (rawDamage * elementMultiplier) - defense);

        // Phản đòn từ Buff Passive (Shield25_Reflect & Reflect100)
        if (InterWaveEventManager.Instance != null)
        {
            if (InterWaveEventManager.Instance.HasActiveBuff(BuffDebuffType.Buff_Passive_Shield25_Reflect) && CurrentShield > 0)
            {
                TryReflectDamageToEnemy(rawDamage * 0.5f);
            }
            if (InterWaveEventManager.Instance.HasActiveBuff(BuffDebuffType.Buff_Passive_Reflect100) && UnityEngine.Random.value < 0.5f)
            {
                TryReflectDamageToEnemy(rawDamage);
            }
        }

        // 1. Khiên Kim Cang (Shield) hấp thụ sát thương trước HP (trừ khi đòn đánh bỏ qua khiên - ignoreShield)
        if (!ignoreShield && CurrentShield > 0)
        {
            bool showImpact = HasMetalShieldVfx && metalShieldVisual.IsProtecting;
            float absorbed = Mathf.Min(CurrentShield, finalDamage);
            CurrentShield -= absorbed;
            finalDamage -= absorbed;
            UpdateShieldVisualAndText();
            OnShieldChanged?.Invoke(CurrentShield);
            if (showImpact)
                metalShieldVisual.PlayImpact(CurrentShield <= 0f);

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
                UpdateHPUI(0f);
                OnStatsUpdated?.Invoke();
                Deid();
                return;
            }
            UpdateHPUI(MaxHP > 0 ? HP / MaxHP : 1f);
            OnStatsUpdated?.Invoke();

            // Pop-up số damage nhận vào của Player
            if (DamageTextManager.Instance != null)
            {
                DamageTextManager.Instance.SpawnDamage(finalDamage, isCrit, attackerElement, transform.position + Vector3.up * 0.8f);
            }
        }

        // 3. Mộc Giáp Hồi Sinh: Mỗi khi nhận sát thương, tự động hồi lại % HP tối đa
        if (WoodArmorTier >= 1 && HP > 0)
        {
            float hpBeforeWoodHeal = HP;
            float healPercent = WoodArmorTier switch
            {
                1 => 0.05f,
                2 => 0.075f,
                _ => 0.1f
            };
            Heal(MaxHP * healPercent);

            // Tier 3: Nếu HP < 30%, lập tức hồi phục 20% HP tối đa (1 lần/trận)
            bool emergencyHeal = WoodArmorTier >= 3 && !HasUsedEmergencyHeal && HP < (MaxHP * 0.3f);
            if (emergencyHeal)
            {
                HasUsedEmergencyHeal = true;
                Heal(MaxHP * 0.20f);
            }

            if (HP > hpBeforeWoodHeal)
            {
                TryPlayWoodArmorVisual(emergencyHeal ? WoodArmorVisual.Kind.EmergencyHeal : WoodArmorVisual.Kind.HealOnHit);
            }
            WoodArmorTier = 0;
        }
    }

    private void TryReflectDamageToEnemy(float reflectDmg)
    {
        if (reflectDmg <= 0) return;
        if (EnemyController.instance != null)
        {
            var enemies = EnemyController.instance.GetOrderedEnemiesForTurn();
            if (enemies != null)
            {
                for (int i = 0; i < enemies.Count; i++)
                {
                    if (enemies[i] != null && enemies[i].gameObject.activeInHierarchy && enemies[i].CurrentHP > 0)
                    {
                        enemies[i].TakeDamageFromPlayer(reflectDmg, false, ElementType.Metal, true);
                        if (DamageTextManager.Instance != null)
                        {
                            DamageTextManager.Instance.SpawnCustomText($"[PHẢN {Mathf.RoundToInt(reflectDmg)} ST]", Color.cyan, transform.position + Vector3.up * 2.2f, 1.3f);
                        }
                        return;
                    }
                }
            }
        }
    }

    internal ActiveRoundEffect FindRoundEffect(Effect effectType, ElementType element)
    {
        return ActiveRoundEffects.Find(effect => effect.EffectType == effectType
            && (effectType != Effect.DoT || effect.Element == element));
    }

    public void ApplyRoundDoT(Effect effectType, float hpPercentPerRound, int rounds, ElementType element, string displayName = "")
    {
        if (HP <= 0 || rounds <= 0) return;

        var existing = FindRoundEffect(effectType, element);
        if (existing != null)
        {
            existing.RoundsRemaining = Mathf.Max(existing.RoundsRemaining, rounds);
            if (effectType == Effect.DoT && hpPercentPerRound > existing.Value && !string.IsNullOrEmpty(displayName))
                existing.DisplayName = displayName;
            existing.Value = Mathf.Max(existing.Value, hpPercentPerRound);
        }
        else
        {
            ActiveRoundEffects.Add(new ActiveRoundEffect
            {
                EffectType = effectType,
                Value = hpPercentPerRound,
                RoundsRemaining = rounds,
                Element = element,
                DisplayName = string.IsNullOrEmpty(displayName) ? effectType.ToString() : displayName
            });
        }

        if (DamageTextManager.Instance != null)
        {
            Color labelColor = effectType switch
            {
                Effect.DoT when element == ElementType.Wood => new Color(0.65f, 0.35f, 0.95f),
                Effect.DoT => new Color(1f, 0.45f, 0.1f),
                Effect.Bleed => new Color(0.9f, 0.15f, 0.15f),
                _ => new Color(0.6f, 0.2f, 0.9f)
            };
            string tag = string.IsNullOrEmpty(displayName) ? $"[{effectType.ToString().ToUpper()}]" : $"[{displayName.ToUpper()}]";
            DamageTextManager.Instance.SpawnCustomText(tag, labelColor, transform.position + Vector3.up * 1.6f, 1.4f);
        }
    }

    public void ApplyStun(int rounds)
    {
        if (HasImmunityCrowdControl)
        {
            HasImmunityCrowdControl = false;
            if (DamageTextManager.Instance != null)
            {
                DamageTextManager.Instance.SpawnCustomText("[MIỄN NHIỄM CHOÁNG]", Color.white, transform.position + Vector3.up * 1.6f, 1.4f);
            }
            return;
        }

        StunRounds = Mathf.Max(StunRounds, rounds);
        if (DamageTextManager.Instance != null)
        {
            DamageTextManager.Instance.SpawnCustomText("[CHOÁNG]", Color.yellow, transform.position + Vector3.up * 1.6f, 1.5f);
        }
    }

    public void ConsumeStunRound()
    {
        if (StunRounds > 0)
        {
            StunRounds--;
        }
    }

    public void ApplySkillCostDebuff(int extraCost, int rounds)
    {
        ExtraSkillCost = Mathf.Max(ExtraSkillCost, extraCost);
        ExtraSkillCostRounds = Mathf.Max(ExtraSkillCostRounds, rounds);
        if (DamageTextManager.Instance != null)
        {
            DamageTextManager.Instance.SpawnCustomText($"+{extraCost} CHI PHÍ SKILL", new Color(0.2f, 0.7f, 1f), transform.position + Vector3.up * 1.6f, 1.4f);
        }
    }

    public void ApplyStatDebuff(bool isAtk, float percent, int rounds)
    {
        if (isAtk)
        {
            AtkDebuffPercent = Mathf.Max(AtkDebuffPercent, percent);
            AtkDebuffRounds = Mathf.Max(AtkDebuffRounds, rounds);
            if (DamageTextManager.Instance != null)
            {
                DamageTextManager.Instance.SpawnCustomText($"-{Mathf.RoundToInt(percent * 100f)}% CÔNG", new Color(1f, 0.5f, 0.2f), transform.position + Vector3.up * 1.6f, 1.4f);
            }
        }
        else
        {
            DefDebuffPercent = Mathf.Max(DefDebuffPercent, percent);
            DefDebuffRounds = Mathf.Max(DefDebuffRounds, rounds);
            if (DamageTextManager.Instance != null)
            {
                DamageTextManager.Instance.SpawnCustomText($"-{Mathf.RoundToInt(percent * 100f)}% THỦ", new Color(0.3f, 0.7f, 1f), transform.position + Vector3.up * 1.6f, 1.4f);
            }
        }
    }

    public void ConsumeAllEnergy()
    {
        energyCores = 0;
        OnEnergyCoresChanged?.Invoke(energyCores);
        if (DamageTextManager.Instance != null)
        {
            DamageTextManager.Instance.SpawnCustomText("TRIỆT TIÊU NĂNG LƯỢNG!", new Color(0.2f, 0.8f, 1f), transform.position + Vector3.up * 1.6f, 1.5f);
        }
    }

    public void TickRoundEffects()
    {
        if (HP <= 0) return;

        // 1. Tick các hiệu ứng DoT (Thiêu đốt, Chảy máu, Độc tố)
        float labelHeight = 0.8f;
        for (int i = ActiveRoundEffects.Count - 1; i >= 0; i--)
        {
            var effect = ActiveRoundEffects[i];
            float dotDamage = Mathf.Max(1f, MaxHP * effect.Value);

            HP = Mathf.Max(0f, HP - dotDamage);
            UpdateHPUI(MaxHP > 0 ? HP / MaxHP : 1f);
            OnStatsUpdated?.Invoke();

            Color dotColor = effect.EffectType switch
            {
                Effect.DoT when effect.Element == ElementType.Wood => new Color(0.65f, 0.35f, 0.95f),
                Effect.DoT => new Color(1f, 0.4f, 0.1f),
                Effect.Bleed => new Color(0.9f, 0.1f, 0.1f),
                _ => new Color(0.6f, 0.2f, 0.9f)
            };

            if (DamageTextManager.Instance != null)
            {
                DamageTextManager.Instance.SpawnCustomText($"-{Mathf.RoundToInt(dotDamage)} {effect.DisplayName}", dotColor,
                    transform.position + Vector3.up * labelHeight, 1.3f);
                labelHeight += 1.8f;
            }

            if (HP <= 0)
            {
                Deid();
                return;
            }

            effect.RoundsRemaining--;
            if (effect.RoundsRemaining <= 0)
            {
                ActiveRoundEffects.RemoveAt(i);
            }
        }

        // 2. Giảm thời hạn các debuff
        if (ExtraSkillCostRounds > 0)
        {
            ExtraSkillCostRounds--;
            if (ExtraSkillCostRounds <= 0) ExtraSkillCost = 0;
        }

        if (AtkDebuffRounds > 0)
        {
            AtkDebuffRounds--;
            if (AtkDebuffRounds <= 0) AtkDebuffPercent = 0f;
        }

        if (DefDebuffRounds > 0)
        {
            DefDebuffRounds--;
            if (DefDebuffRounds <= 0) DefDebuffPercent = 0f;
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
        HP = 0;
        UpdateHPUI(0f);
        OnStatsUpdated?.Invoke();

        if (woodArmorVisual)
            woodArmorVisual.Stop();
        CleanUpAllEffect();
        ClearShield();
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

[System.Serializable]
public class ActiveRoundEffect
{
    public Effect EffectType;
    public float Value; // % Max HP mỗi round cho DoT
    public int RoundsRemaining;
    public ElementType Element;
    public string DisplayName;
}
