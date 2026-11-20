using System;
using UnityEngine;
using System.Collections;
using DG.Tweening;

public enum EnemyCombatType
{
    Melee,
    Ranged
}

public enum EnemyRow
{
    Front, // Hàng trước (tối đa 2 quái tiền tuyến che chắn)
    Back   // Hàng sau (tối đa 3 quái hậu phương)
}

[RequireComponent(typeof(EnemyAnimationController), typeof(BoxCollider2D))]
public class EnemyBase : BaseCharacter
{
    [Header("----ScriptableObject Data Reference (Single Source of Truth)----")]
    public EnemyDataSO enemyData;

    public EnemyType type
    {
        get => enemyData != null ? enemyData.enemyType : EnemyType.BoRac;
        set
        {
            if (enemyData == null || enemyData.enemyType != value)
            {
                enemyData = EnemyDataSO.Get(value);
            }
        }
    }

    private bool _isDynamicBoss = false;
    public bool isBoss
    {
        get => _isDynamicBoss || (enemyData != null && (enemyData.isBoss || enemyData.category != EnemyCategory.Normal));
        set => _isDynamicBoss = value;
    }

    public override float CurrentDEF => currentDef;
    public override float MoveSpeed => enemyData != null ? BlockMetrics.ConvertSpeedToWorldUnits(enemyData.speed) : 0.4f;
    public override float CurrentATK => currentAtk;

    protected override void PlayFlyOrMoveAnimation()
    {
        if (animController != null)
        {
            animController.ActiveAnimation(TypeAnimationAtivity.isFly);
        }
    }

    [HideInInspector] public float temporaryReflectBuff = 0f;
    [HideInInspector] public int reflectBuffRounds = 0;
    public float reflectPercent => (enemyData != null ? enemyData.reflect : 0f) + temporaryReflectBuff;
    [HideInInspector] public float currentAtk = 10f;
    [HideInInspector] public float currentDef = 0f;
    public float attackCooldown => enemyData != null ? enemyData.attackCd : 2f;
    public EnemyCombatType combatType => enemyData != null ? enemyData.combatType : EnemyCombatType.Melee;
    public float attackRange => enemyData != null ? enemyData.attackRange : 0.85f;

    [Header("Turn-Based Settings (GDD 5v1)")]
    [HideInInspector] public EnemyRow row = EnemyRow.Front;
    public EnemyRole role => enemyData != null ? enemyData.role : EnemyRole.DPS_FBS;

    [HideInInspector] public int skillCooldownRound = 0;
    public int maxSkillCooldownRound => enemyData != null ? enemyData.skillCdRound : 2;
    public float skillMultiplier => enemyData != null ? enemyData.skillMultiplier : 1.5f;
    public float critRate => enemyData != null ? enemyData.critRate : 0.1f;
    public float critDmg => enemyData != null ? enemyData.critDmg : 1.5f;

    [HideInInspector] public bool isRevived = false;

    [Header("Combat Buffs & Shields (GDD)")]
    [HideInInspector] public float damageReductionPercent = 0f;
    [HideInInspector] public float currentShield = 0f;
    [HideInInspector] public float maxShieldGranted = 0f;
    [HideInInspector] public float shieldExplodeMultiplierOnBreak = 0f;
    [HideInInspector] public int damageReductionRounds = 0;
    [HideInInspector] public int shieldRounds = 0;
    [HideInInspector] public float atkBuffPercent = 0f;
    [HideInInspector] public int atkBuffRounds = 0;
    [HideInInspector] public float defBuffPercent = 0f;
    [HideInInspector] public int defBuffRounds = 0;
    [HideInInspector] public int roundCounterForPassive = 0;

    public bool IsTargetable => row == EnemyRow.Front || (EnemyController.instance != null && EnemyController.instance.AllFrontRowEnemiesDead());

    public void SetStatsForTest(float hp, float atk, EnemyRow enemyRow = EnemyRow.Front)
    {
        HP = hp;
        MaxHP = hp;
        currentAtk = atk;
        row = enemyRow;
    }

    private EnemyAnimationController animController;
    private float _nextAttackTime = 0f;
    private Player _cachedPlayer;

    protected override void Awake()
    {
        base.Awake();
        animController = GetComponent<EnemyAnimationController>();
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            colorOrigin = Color.white;
        }

        if (enemyData != null)
        {
            currentAtk = enemyData.atk;
            currentDef = enemyData.def;
            row = enemyData.defaultRow;
            skillCooldownRound = enemyData.skillCdRound;
            currentElement = enemyData.element;
        }
        else
        {
            currentAtk = 10f;
            currentDef = 0f;
        }
        _nextAttackTime = Time.time + 0.5f;

        // Passive Cỗ Máy Vĩnh Cửu: Tự tạo khiên 15% Max HP đầu trận
        if (type == EnemyType.CoMayVinhCuu)
        {
            currentShield = MaxHealth * 0.15f;
            shieldRounds = 999;
        }
        // Passive Hỏa Thần Diệt Vong: Phá hủy 50% DEF của Player suốt trận đấu
        else if (type == EnemyType.HoaThanDietVong)
        {
            Player p = Player.Instance;
            if (p != null)
            {
                p.ApplyStatDebuff(false, 0.50f, 999);
            }
        }
    }

    // TỐI ƯU HIỆU SUẤT: Loại bỏ 100% hàm Update() và Coroutine rác khỏi từng MonoBehaviour Enemy.
    // Toàn bộ chu kỳ chiến đấu được quản lý tuần tự và chính xác thông qua ExecuteTurnActionRoutine() từ CombatTurnManager.

    /// <summary>
    /// Phân loại hình thái kỹ năng của quái:
    /// - MeleeDash: Lao sang trước mặt Player -> Tung chiêu -> Lùi về.
    /// - DefensiveSelf: Đứng tại chỗ bật khiên/giáp/hồi máu.
    /// - RangedSpell: Đứng tại chỗ niệm chú/bắn đạn/phong ấn.
    /// </summary>
    public EnemySkillActionType GetSkillActionType()
    {
        if (enemyData != null)
        {
            return enemyData.activeSkillActionType;
        }

        return type switch
        {
            EnemyType.ChoSanDiaNguc or
            EnemyType.ChienBinhDungNham or
            EnemyType.SatThuBongToi or
            EnemyType.BacThaySatThu or
            EnemyType.ThuyQuaiNho => EnemySkillActionType.MeleeDash,

            EnemyType.SlimeBun or
            EnemyType.BoCapDa or
            EnemyType.HatNhamThach or
            EnemyType.LinhDat or
            EnemyType.GolemThep or
            EnemyType.KySiSat or
            EnemyType.CoMayXayThit or
            EnemyType.BoRac or
            EnemyType.TinhLinhHoiMau or
            EnemyType.CoThuNganNam => EnemySkillActionType.DefensiveSelf,

            _ => EnemySkillActionType.RangedSpell
        };
    }

    /// <summary>
    /// Phân loại hình thái đòn đánh thường của quái (Cận chiến MeleeDash hoặc Tầm xa RangedSpell).
    /// </summary>
    public EnemySkillActionType GetNormalAttackActionType()
    {
        if (enemyData != null)
        {
            return enemyData.normalAttackActionType;
        }

        return combatType == EnemyCombatType.Ranged ? EnemySkillActionType.RangedSpell : EnemySkillActionType.MeleeDash;
    }

    public bool IsSkillMeleeDash() => GetSkillActionType() == EnemySkillActionType.MeleeDash;

    /// <summary>
    /// Thực thi lượt hành động của quái trong hệ thống Turn-based 5v1:
    /// - Phân biệt chính xác dựa trên ActionType của chiêu thức thay vì role:
    /// - Đánh thường: Cận chiến (MeleeDash) lao sang Player; Tầm xa (RangedSpell) đứng tại chỗ bắn.
    /// - Dùng skill: MeleeDash lao sang Player; DefensiveSelf / RangedSpell đứng tại chỗ thi triển.
    /// </summary>
    public virtual IEnumerator ExecuteTurnActionRoutine()
    {
        if (HP <= 0 || !gameObject.activeInHierarchy) yield break;

        if (_cachedPlayer == null)
        {
            _cachedPlayer = Player.Instance;
        }
        if (_cachedPlayer == null || _cachedPlayer.CurrentHP <= 0) yield break;

        Vector3 originalPos = transform.position;
        bool isSkill = (skillCooldownRound <= 0);

        // PHÂN BIỆT DỰA VÀO HÌNH THÁI CỦA ĐÚNG CHIÊU THỨC ĐANG THI TRIỂN:
        EnemySkillActionType currentActionType = isSkill ? GetSkillActionType() : GetNormalAttackActionType();
        bool shouldDashToPlayer = (currentActionType == EnemySkillActionType.MeleeDash);

        if (shouldDashToPlayer)
        {
            // === HÀNH ĐỘNG CẬN CHIẾN: Lao nhanh sang phía Player -> Đánh/Chiêu -> Lùi về ===
            Vector3 targetPos = new Vector3(_cachedPlayer.transform.position.x + 2.2f, _cachedPlayer.transform.position.y, originalPos.z);
            transform.DOMove(targetPos, 0.28f).SetEase(Ease.OutQuad);
            yield return YieldCaches.Wait030;

            if (isSkill)
            {
                PerformCombatSkill(_cachedPlayer);
                skillCooldownRound = maxSkillCooldownRound;
            }
            else
            {
                PerformNormalAttack(_cachedPlayer);
            }

            yield return YieldCaches.Wait032;

            // Lùi về vị trí ban đầu trong đội hình
            transform.DOMove(originalPos, 0.25f).SetEase(Ease.InQuad);
            yield return YieldCaches.Wait025;
        }
        else
        {
            // === HÀNH ĐỘNG ĐỨNG TẠI CHỖ THI TRIỂN: ===
            // Dành cho:
            // - Toàn bộ chiêu Phòng thủ (Tank tự bật khiên/giáp/bọc bùn)
            // - Toàn bộ chiêu Hỗ trợ (Hồi máu, Buff ATK/DEF, Hồi sinh đồng đội)
            // - Toàn bộ chiêu Pháp sư (Khóa ngọc Match-3, làm chậm, hút energy, địa chấn)
            // - Toàn bộ đòn đánh thường của quái Tầm xa (combatType == Ranged)
            if (isSkill)
            {
                PerformCombatSkill(_cachedPlayer);
                skillCooldownRound = maxSkillCooldownRound;
            }
            else
            {
                PerformNormalAttack(_cachedPlayer);
            }

            yield return YieldCaches.Wait045;
        }
    }

    protected virtual void PerformNormalAttack(Player targetPlayer)
    {
        Attack(); // Animation

        float atkDmg = currentAtk * (1f + atkBuffPercent);
        float ignoreDef = 0f;
        bool ignoreShield = false;

        // Passive: Cỗ Máy Xay Thịt đòn đánh tự động xuyên 50% DEF
        if (type == EnemyType.CoMayXayThit)
        {
            ignoreDef = 0.50f;
        }
        // Passive: Cỗ Máy Vĩnh Cửu bỏ qua toàn bộ khiên thủ
        else if (type == EnemyType.CoMayVinhCuu)
        {
            ignoreDef = 1.00f;
            ignoreShield = true;
        }
        // Passive: Thủy Thần Sa Ngã cướp 5 Energy khi đánh thường
        if (type == EnemyType.ThuyThanSaNga)
        {
            targetPlayer.TrySpendEnergy(5);
        }
        // Passive: Pháp Sư Bùa Chú đánh thường gây DoT rút 2% HP/round trong 2 Round
        else if (type == EnemyType.PhapSuBuaChu)
        {
            targetPlayer.ApplyRoundDoT(Effect.DoT, 0.02f, 2, ElementType.Wood, "ĐỘC MỘC");
        }
        // Passive: Cung Thủ Ám có 15% tỉ lệ gây độc nhẹ 1% HP
        else if (type == EnemyType.CungThuAm)
        {
            if (UnityEngine.Random.value <= 0.15f)
            {
                targetPlayer.ApplyRoundDoT(Effect.DoT, 0.01f, 2, ElementType.Wood, "ĐỘC TỐ");
            }
        }

        // Thực hiện gây sát thương
        if (GetNormalAttackActionType() == EnemySkillActionType.RangedSpell)
        {
            ShootProjectile(targetPlayer, atkDmg, ignoreDef, ignoreShield);
        }
        else
        {
            targetPlayer.TakeDamage(atkDmg, false, currentElement, ignoreDef, ignoreShield);
        }

        // Passive: Rễ Cây Ăn Thịt hút máu 10% sát thương gây ra
        if (type == EnemyType.ReCayAnThit)
        {
            Health(atkDmg * 0.10f);
        }
    }

    protected virtual void PerformCombatSkill(Player targetPlayer)
    {
        Attack(); // Animation

        // Hiển thị tên kỹ năng trên đầu quái theo chuẩn GDD
        string skillName = GetSkillDisplayName();
        if (DamageTextManager.Instance != null && !string.IsNullOrEmpty(skillName))
        {
            Color elemColor = ElementalHelper.GetColor(currentElement);
            DamageTextManager.Instance.SpawnCustomText(skillName, elemColor, transform.position + Vector3.up * 2.2f, 1.35f);
        }

        switch (type)
        {
            // ==================== HỆ HỎA (7 Quái) ====================
            case EnemyType.TinhLinhLua:
                ShootProjectile(targetPlayer, currentAtk * 1.50f);
                break;
            case EnemyType.ChoSanDiaNguc:
                targetPlayer.TakeDamage(currentAtk * 1.60f, false, currentElement);
                Camera.main?.transform.DOShakePosition(0.2f, new Vector3(0.25f, 0.25f, 0f), 10, 90f, false, true);
                break;
            case EnemyType.HoaQuyNho:
                ShootProjectile(targetPlayer, currentAtk * 1.80f);
                break;
            case EnemyType.PhuThuyLua:
                ShootProjectile(targetPlayer, currentAtk * 2.00f);
                break;
            case EnemyType.ChienBinhDungNham:
                ExecuteDamageWithDoT(targetPlayer, 2.20f, Effect.DoT, 0.02f, 2, "THIÊU ĐỐT");
                break;
            case EnemyType.HoaLongNho:
                ShootProjectile(targetPlayer, currentAtk * 2.50f);
                targetPlayer.TakeDamage(currentAtk * 0.05f, false, currentElement); // Nổ diện rộng thêm 5%
                break;
            case EnemyType.HoaThanDietVong:
                ExecuteDamageWithDoT(targetPlayer, 3.00f, Effect.DoT, 0.03f, 3, "THIÊU ĐỐT TỐI THƯỢNG");
                break;

            // ==================== HỆ KIM (7 Quái) ====================
            case EnemyType.GolemThep:
                ActivateDefensiveStance(0f, 0, 0.40f, 2, 0f, 0f);
                break;
            case EnemyType.SatThuBongToi:
                ExecutePenetrationAttack(targetPlayer, 1.80f, 0.20f, false);
                break;
            case EnemyType.KySiSat:
                ActivateDefensiveStance(0.20f, 2, 0f, 0, 0f, 0f);
                break;
            case EnemyType.CungThuXuyenGiap:
                ExecutePenetrationAttack(targetPlayer, 1.70f, 0.30f, false);
                break;
            case EnemyType.CoMayXayThit:
                ActivateDefensiveStance(0f, 0, 0.30f, 2, 0.25f, 0f);
                break;
            case EnemyType.BacThaySatThu:
                ExecuteDamageWithDoT(targetPlayer, 2.50f, Effect.Bleed, 0.02f, 3, "CHẢY MÁU");
                break;
            case EnemyType.CoMayVinhCuu:
                ExecutePenetrationAttack(targetPlayer, 3.00f, 1.00f, true);
                break;

            // ==================== HỆ MỘC (7 Quái) ====================
            case EnemyType.QuyRung:
                ExecuteTeamSupport(0, 0f, 0f, 0.25f, 0f, 0f, 2);
                break;
            case EnemyType.BoRac:
                ActivateDefensiveStance(0.40f, 2, 0f, 0, 0f, 0f);
                break;
            case EnemyType.CungThuAm:
                ShootProjectile(targetPlayer, currentAtk * 1.60f);
                break;
            case EnemyType.TinhLinhHoiMau:
                ExecuteTeamSupport(0, 0f, 0.15f, 0f, 0f, 0f, 0);
                break;
            case EnemyType.ReCayAnThit:
                ExecuteTeamSupport(1, 0.30f, 0.15f, 0f, 0f, 0f, 0);
                break;
            case EnemyType.PhapSuBuaChu:
                ExecuteTeamSupport(1, 0.40f, 0f, 0f, 0.30f, 0f, 2);
                break;
            case EnemyType.CoThuNganNam:
                ExecuteTeamSupport(5, 0.50f, 0.20f, 0f, 0f, 0.10f, 2);
                break;

            // ==================== HỆ THỔ (7 Quái) ====================
            case EnemyType.SlimeBun:
                ActivateDefensiveStance(0f, 0, 0.35f, 2, 0f, 0f);
                break;
            case EnemyType.LinhDat:
                ActivateDefensiveStance(0.25f, 2, 0f, 0, 0f, 0f);
                break;
            case EnemyType.PhapSuPhongAn:
                LockBoardStones(2, 1);
                ShootProjectile(targetPlayer, currentAtk * 0.60f);
                break;
            case EnemyType.BoCapDa:
                ActivateDefensiveStance(0f, 0, 0f, 0, 0.20f, 0f);
                break;
            case EnemyType.HatNhamThach:
                ActivateDefensiveStance(0.20f, 3, 0f, 0, 0f, 1.50f);
                break;
            case EnemyType.PhuThuyKhoaNgoc:
                LockBoardStones(4, 2);
                targetPlayer?.ApplyStatDebuff(true, 0.20f, 2);
                ShootProjectile(targetPlayer, currentAtk * 0.70f);
                break;
            case EnemyType.BossDatNung:
                ShootProjectile(targetPlayer, currentAtk * 2.00f);
                ApplyPlayerStun(targetPlayer, 1);
                LockBoardStones(5, 2);
                break;

            // ==================== HỆ THỦY (7 Quái) ====================
            case EnemyType.SlimeNuoc:
                ApplyPlayerSkillCostDebuff(targetPlayer, 2, 1);
                ShootProjectile(targetPlayer, currentAtk * 0.60f);
                break;
            case EnemyType.ThuyQuaiNho:
                targetPlayer.TakeDamage(currentAtk * 1.60f, false, currentElement);
                Camera.main?.transform.DOShakePosition(0.2f, new Vector3(0.25f, 0.25f, 0f), 10, 90f, false, true);
                break;
            case EnemyType.KeTromKyNang:
                ManipulatePlayerEnergy(targetPlayer, 5, false, 0.10f);
                ShootProjectile(targetPlayer, currentAtk * 0.50f);
                break;
            case EnemyType.PhapSuBang:
                LockBoardStones(2, 2);
                ShootProjectile(targetPlayer, currentAtk * 0.70f);
                break;
            case EnemyType.QuaiVatDamLay:
                ManipulatePlayerEnergy(targetPlayer, 10, false, 0f);
                targetPlayer?.ApplyStatDebuff(false, 0.30f, 2);
                ShootProjectile(targetPlayer, currentAtk * 0.80f);
                break;
            case EnemyType.TinhLinhBangGia:
                LockBoardStones(0, 2, freezeBottomRow: true);
                ShootProjectile(targetPlayer, currentAtk * 0.80f);
                break;
            case EnemyType.ThuyThanSaNga:
                ApplyPlayerStun(targetPlayer, 1);
                ManipulatePlayerEnergy(targetPlayer, 0, true, 0f);
                ShootProjectile(targetPlayer, currentAtk * 2.20f);
                break;

            default:
                float genericDmg = currentAtk * (skillMultiplier > 0 ? skillMultiplier : 1.5f);
                ShootProjectile(targetPlayer, genericDmg);
                break;
        }
    }

    #region Parametric Reusable Skill Methods (GDD Architecture)

    protected void ExecutePenetrationAttack(Player targetPlayer, float multiplier, float ignoreDefPercent, bool ignoreShield)
    {
        SpawnSkillVFX(transform.position, currentElement);
        float skillDmg = currentAtk * multiplier;
        if (combatType == EnemyCombatType.Melee)
        {
            targetPlayer.TakeDamage(skillDmg, false, currentElement, ignoreDefPercent, ignoreShield);
            SpawnSkillVFX(targetPlayer.transform.position, currentElement);
            Camera.main?.transform.DOShakePosition(0.2f, new Vector3(0.25f, 0.25f, 0f), 10, 90f, false, true);
        }
        else
        {
            ShootProjectile(targetPlayer, skillDmg, ignoreDefPercent, ignoreShield);
        }
    }

    protected void ExecuteDamageWithDoT(Player targetPlayer, float multiplier, Effect dotType, float hpPercentPerRound, int rounds, string dotName)
    {
        SpawnSkillVFX(transform.position, currentElement);
        float skillDmg = currentAtk * multiplier;
        if (combatType == EnemyCombatType.Melee)
        {
            targetPlayer.TakeDamage(skillDmg, false, currentElement);
            SpawnSkillVFX(targetPlayer.transform.position, currentElement);
            Camera.main?.transform.DOShakePosition(0.2f, new Vector3(0.25f, 0.25f, 0f), 10, 90f, false, true);
        }
        else
        {
            ShootProjectile(targetPlayer, skillDmg);
        }
        targetPlayer.ApplyRoundDoT(dotType, hpPercentPerRound, rounds, currentElement, dotName);
    }

    protected void ManipulatePlayerEnergy(Player targetPlayer, int energyDrain, bool drainAllToZero, float selfHealRatio)
    {
        if (targetPlayer == null) return;
        if (drainAllToZero)
        {
            targetPlayer.ConsumeAllEnergy();
        }
        else if (energyDrain > 0)
        {
            targetPlayer.TrySpendEnergy(energyDrain);
            if (DamageTextManager.Instance != null)
            {
                DamageTextManager.Instance.SpawnCustomText($"-{energyDrain} ENERGY", new Color(0.2f, 0.7f, 1f), targetPlayer.transform.position + Vector3.up * 1.5f, 1.3f);
            }
        }

        if (selfHealRatio > 0f)
        {
            Health(MaxHealth * selfHealRatio);
        }
    }

    protected void ApplyPlayerSkillCostDebuff(Player targetPlayer, int extraCost, int rounds)
    {
        if (targetPlayer == null) return;
        targetPlayer.ApplySkillCostDebuff(extraCost, rounds);
    }

    protected void LockBoardStones(int count, int rounds, bool freezeBottomRow = false)
    {
        if (BoardController.instance == null) return;
        if (freezeBottomRow)
        {
            BoardController.instance.FreezeRow(0, rounds);
            if (DamageTextManager.Instance != null)
            {
                DamageTextManager.Instance.SpawnCustomText("[ĐÓNG BĂNG HÀNG NGŨ]", new Color(0.4f, 0.8f, 1f), transform.position + Vector3.up * 2.2f, 1.4f);
            }
        }
        else if (count > 0)
        {
            BoardController.instance.LockRandomStones(count, rounds);
            if (DamageTextManager.Instance != null)
            {
                DamageTextManager.Instance.SpawnCustomText($"[PHONG ẤN {count} NGỌC]", new Color(0.95f, 0.25f, 0.25f), transform.position + Vector3.up * 2.2f, 1.5f);
            }
        }
    }

    protected void ApplyPlayerStun(Player targetPlayer, int rounds)
    {
        if (targetPlayer == null) return;
        targetPlayer.ApplyStun(rounds);
    }

    protected void ActivateDefensiveStance(float shieldHpPercent, int shieldDurationRounds, float dmgReductionPct, int dmgReductionDurationRounds, float reflectPct, float explodeMultiplierOnBreak = 0f)
    {
        if (shieldHpPercent > 0f)
        {
            currentShield = MaxHealth * shieldHpPercent;
            maxShieldGranted = currentShield;
            shieldRounds = shieldDurationRounds;
            shieldExplodeMultiplierOnBreak = explodeMultiplierOnBreak;
        }

        if (dmgReductionPct > 0f)
        {
            damageReductionPercent = dmgReductionPct;
            damageReductionRounds = dmgReductionDurationRounds;
        }

        if (reflectPct > 0f)
        {
            temporaryReflectBuff = reflectPct;
            reflectBuffRounds = 2;
        }

        SpawnSkillVFX(transform.position, currentElement);
    }

    protected void ExecuteTeamSupport(int maxReviveTargets, float reviveHpPercent, float healPercentLiving, float buffAtkPct, float buffDefPct, float shieldAlliesPct, int effectRounds)
    {
        SpawnSkillVFX(transform.position, currentElement);

        // 1. Hồi sinh đồng minh
        if (maxReviveTargets > 0 && EnemyController.instance != null)
        {
            if (maxReviveTargets >= 5)
            {
                EnemyController.instance.ReviveAllDeadAllies(currentElement, reviveHpPercent);
            }
            else
            {
                EnemyController.instance.TryReviveDeadAlly(currentElement, reviveHpPercent);
            }
        }

        // 2. Tác động lên toàn bộ đồng minh còn sống
        if (EnemyController.instance != null && EnemyController.instance.LivingEnemies != null)
        {
            var living = EnemyController.instance.LivingEnemies;
            for (int i = 0; i < living.Count; i++)
            {
                var ally = living[i];
                if (ally != null && ally.gameObject.activeInHierarchy && ally.CurrentHP > 0)
                {
                    if (healPercentLiving > 0f)
                    {
                        ally.Health(ally.MaxHealth * healPercentLiving);
                    }

                    if (buffAtkPct > 0f)
                    {
                        ally.atkBuffPercent = buffAtkPct;
                        ally.atkBuffRounds = effectRounds;
                        if (DamageTextManager.Instance != null)
                        {
                            DamageTextManager.Instance.SpawnCustomText($"+{Mathf.RoundToInt(buffAtkPct * 100f)}% CÔNG", new Color(1f, 0.5f, 0.2f), ally.transform.position + Vector3.up * 1.5f, 1.2f);
                        }
                    }

                    if (buffDefPct > 0f)
                    {
                        ally.defBuffPercent = buffDefPct;
                        ally.defBuffRounds = effectRounds;
                        if (DamageTextManager.Instance != null)
                        {
                            DamageTextManager.Instance.SpawnCustomText($"+{Mathf.RoundToInt(buffDefPct * 100f)}% THỦ", new Color(0.3f, 0.7f, 1f), ally.transform.position + Vector3.up * 1.5f, 1.2f);
                        }
                    }

                    if (shieldAlliesPct > 0f)
                    {
                        ally.currentShield = Mathf.Max(ally.currentShield, MaxHealth * shieldAlliesPct);
                        ally.shieldRounds = effectRounds;
                    }
                }
            }
        }
    }

    public void TickRoundBuffsAndShields()
    {
        if (shieldRounds > 0)
        {
            shieldRounds--;
            if (shieldRounds <= 0)
            {
                currentShield = 0f;
                maxShieldGranted = 0f;
                shieldExplodeMultiplierOnBreak = 0f;
            }
        }

        if (damageReductionRounds > 0)
        {
            damageReductionRounds--;
            if (damageReductionRounds <= 0)
            {
                damageReductionPercent = 0f;
            }
        }

        if (reflectBuffRounds > 0)
        {
            reflectBuffRounds--;
            if (reflectBuffRounds <= 0)
            {
                temporaryReflectBuff = 0f;
            }
        }

        if (atkBuffRounds > 0)
        {
            atkBuffRounds--;
            if (atkBuffRounds <= 0)
            {
                atkBuffPercent = 0f;
            }
        }

        if (defBuffRounds > 0)
        {
            defBuffRounds--;
            if (defBuffRounds <= 0)
            {
                defBuffPercent = 0f;
            }
        }

        ExecuteRoundStartPassive();
    }

    private void ExecuteRoundStartPassive()
    {
        roundCounterForPassive++;

        // Passive Boss Đất Nung: Đầu mỗi round tự tạo khiên bằng 10% Max HP Boss
        if (type == EnemyType.BossDatNung)
        {
            currentShield = MaxHealth * 0.10f;
            maxShieldGranted = currentShield;
            shieldRounds = 1;
            if (DamageTextManager.Instance != null)
            {
                DamageTextManager.Instance.SpawnCustomText("+900 KHIÊN", new Color(1f, 0.85f, 0.2f), transform.position + Vector3.up * 1.5f, 1.2f);
            }
        }
        // Passive Quái Vật Đầm Lầy: Khi HP < 50%, tự hồi phục 5% Max HP mỗi Round
        else if (type == EnemyType.QuaiVatDamLay && HP < MaxHealth * 0.5f)
        {
            Health(MaxHealth * 0.05f);
        }
        // Passive Cổ Thụ Ngàn Năm: Cứ sau 3 Round tự động triệu hồi 2 Bọ Rác (Hàng trước)
        else if (type == EnemyType.CoThuNganNam && (roundCounterForPassive % 3 == 0) && EnemyController.instance != null)
        {
            if (EnemyController.instance.GetLivingEnemyCount() < 5)
            {
                
                EnemyController.instance.Spawn(EnemyType.BoRac,0, EnemyRow.Front);
                if (DamageTextManager.Instance != null)
                {
                    DamageTextManager.Instance.SpawnCustomText("[TRIỆU HỒI BỌ RÁC]", new Color(0.4f, 1f, 0.4f), transform.position + Vector3.up * 2f, 1.4f);
                }
            }
        }
    }

    #endregion

    private string GetSkillDisplayName()
    {
        return type switch
        {
            EnemyType.TinhLinhLua => "[Tia Lửa Đỏ]",
            EnemyType.ChoSanDiaNguc => "[Cắn Xé Rực Lửa]",
            EnemyType.HoaQuyNho => "[Cầu Lửa Hắc Ám]",
            EnemyType.PhuThuyLua => "[Cột Lửa Thiêu Rụi]",
            EnemyType.ChienBinhDungNham => "[Bổ Búa Dung Nham]",
            EnemyType.HoaLongNho => "[Hơi Thở Rồng Lửa]",
            EnemyType.HoaThanDietVong => "[Mưa Sao Băng]",
            EnemyType.GolemThep => "[Pháo Đài Thép]",
            EnemyType.SatThuBongToi => "[Ám Sát Bất Ngờ]",
            EnemyType.KySiSat => "[Lá Chắn Sắt]",
            EnemyType.CungThuXuyenGiap => "[Tên Xuyên Thấu]",
            EnemyType.CoMayXayThit => "[Giáp Gai Phản Đòn]",
            EnemyType.BacThaySatThu => "[Trảm Huyết]",
            EnemyType.CoMayVinhCuu => "[Pháo Laser Hủy Diệt]",
            EnemyType.QuyRung => "[Tiếng Gầm Tự Nhiên]",
            EnemyType.BoRac => "[Vỏ Bọc Gai Nhọn]",
            EnemyType.CungThuAm => "[Mưa Tên Độc Mộc]",
            EnemyType.TinhLinhHoiMau => "[Mưa Phước Lành]",
            EnemyType.ReCayAnThit => "[Rễ Mẹ Tái Sinh]",
            EnemyType.PhapSuBuaChu => "[Bùa Chú Phục Sinh]",
            EnemyType.CoThuNganNam => "[Rễ Cây Phục Sinh]",
            EnemyType.SlimeBun => "[Bọc Bùn Giảm ST]",
            EnemyType.LinhDat => "[Lá Chắn Đất Đá]",
            EnemyType.PhapSuPhongAn => "[Phong Ấn Ngọc Đá]",
            EnemyType.BoCapDa => "[Giáp Gai Độc Thạch]",
            EnemyType.HatNhamThach => "[Khiên Dung Nham]",
            EnemyType.PhuThuyKhoaNgoc => "[Đại Phong Ấn]",
            EnemyType.BossDatNung => "[Đại Địa Chấn]",
            EnemyType.SlimeNuoc => "[Tia Nước Làm Chậm]",
            EnemyType.ThuyQuaiNho => "[Cắn Xoáy Thủy Triều]",
            EnemyType.KeTromKyNang => "[Cướp Đoạt Nội Năng]",
            EnemyType.PhapSuBang => "[Mũi Băng Hàn Giá]",
            EnemyType.QuaiVatDamLay => "[Đầm Lầy Ăn Mòn]",
            EnemyType.TinhLinhBangGia => "[Đóng Băng Hàng Ngũ]",
            EnemyType.ThuyThanSaNga => "[Sóng Thần Diệt Tuyệt]",
            _ => $"[Tuyệt Kỹ {currentElement}]"
        };
    }

    public void HealAllies(float percent)
    {
        if (EnemyController.instance == null || EnemyController.instance.LivingEnemies == null) return;
        var enemies = EnemyController.instance.LivingEnemies;
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e != null && e.gameObject.activeInHierarchy && e.CurrentHP > 0)
            {
                e.Health(e.MaxHealth * percent);
            }
        }
    }

    public bool TryReviveAlly(float hpPercent)
    {
        if (EnemyController.instance == null) return false;
        return EnemyController.instance.TryReviveDeadAlly(currentElement, hpPercent);
    }

    public static void SpawnSkillVFX(Vector3 pos, ElementType element)
    {
        if (!Application.isPlaying) return;
        string vfxPath = $"Prefab/VFX/VFX_Template_{element}";
        GameObject prefab = Resources.Load<GameObject>(vfxPath);
        if (prefab != null)
        {
            if (ObjectPoolingManager.Instance != null)
            {
                ObjectPoolingManager.Instance.Spawn(prefab, pos, Quaternion.identity);
            }
            else
            {
                GameObject vfx = Instantiate(prefab, pos, Quaternion.identity);
                Destroy(vfx, 2.0f);
            }
        }
    }

    /// <summary>
    /// Thực hiện đòn tấn công: Cận chiến đánh trực tiếp, Đánh xa bắn chưởng ma pháp
    /// </summary>
    protected virtual void PerformCombatAttack(Player targetPlayer)
    {
        PerformNormalAttack(targetPlayer);
    }

    private static GameObject _projectilePrefab;

    /// <summary>
    /// Kỹ năng đánh xa: Bắn chưởng ma thuật đổi màu theo hệ nguyên tố của quái
    /// </summary>
    protected virtual void ShootProjectile(Player targetPlayer, float damage = 0f, float ignoreDefPercent = 0f, bool ignoreShield = false)
    {
        if (targetPlayer == null) return;
        float finalDmg = damage > 0 ? damage : currentAtk;

        if (_projectilePrefab == null)
        {
            _projectilePrefab = Resources.Load<GameObject>("Prefab/Base/Enemy/EnemyProjectile");
        }

        Vector3 spawnPos = transform.position;
        Vector3 targetPos = targetPlayer.transform.position;

        GameObject projObj = null;
        if (ObjectPoolingManager.Instance != null && _projectilePrefab != null)
        {
            projObj = ObjectPoolingManager.Instance.Spawn(_projectilePrefab, spawnPos, Quaternion.identity);
        }
        else if (_projectilePrefab != null)
        {
            projObj = Instantiate(_projectilePrefab, spawnPos, Quaternion.identity);
        }

        if (projObj != null)
        {
            EnemyProjectile proj = projObj.GetComponent<EnemyProjectile>();
            if (proj != null)
            {
                proj.Launch(finalDmg, currentElement, spawnPos, targetPos, 22.0f, ignoreDefPercent, ignoreShield);
            }
        }
        else
        {
            // Fallback nếu không tải được prefab
            targetPlayer.TakeDamage(finalDmg, false, currentElement, ignoreDefPercent, ignoreShield);
        }
    }

    /// <summary>
    /// Khởi tạo quái động theo bảng thông số GDD (Level 1 & các level sau)
    /// </summary>
    public void InitializeStats(EnemyType enemyType, ElementType elem, float hp, float atk, float def, float speed, 
        float reflect = 0f, float scale = 1f, float attackCd = 2f, EnemyCombatType cType = EnemyCombatType.Melee, 
        float atkRange = 0.85f, EnemyRole enemyRole = EnemyRole.DPS_FBS, float sMultiplier = 1.5f, int sCdRound = 2, 
        float cRate = 0.1f, float cDmg = 1.5f)
    {
        type = enemyType;
        currentElement = elem;
        HP = hp;
        MaxHP = hp;
        currentAtk = atk;
        currentDef = def;
        skillCooldownRound = sCdRound;
        isRevived = false;
        damageReductionPercent = 0f;
        currentShield = 0f;

        // Giữ nguyên 100% màu gốc của Sprite nghệ thuật (không nhuộm màu theo hệ)
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            colorOrigin = Color.white;
        }

        // Nâng base scale lên 9.5f để quái (PPU = 200, scale 0.85) có chiều cao thế giới đạt ~5.0 units ngang bằng Player (5.15 units)
        transform.localScale = Vector3.one * (9.5f * scale);
        UpdateHPUI(1f);
    }

    /// <summary>
    /// Khởi tạo quái trực tiếp từ ScriptableObject EnemyDataSO theo chuẩn GDD
    /// Sử dụng EnemyDataSO làm nguồn dữ liệu duy nhất (Single Source of Truth).
    /// </summary>
    public void InitializeFromSO(EnemyDataSO so, int level = 1, StepDifficulty difficulty = StepDifficulty.Normal)
    {
        if (so == null) return;
        enemyData = so;

        float mult = EnemyStatsDatabase.GetDifficultyMultiplier(difficulty);
        float n = Mathf.Max(0f, (level - 1) / 15.0f);
        float scaleFactor = Mathf.Pow(mult, n);

        float scaledHp = Mathf.Round(so.maxHP * scaleFactor);
        float scaledAtk = Mathf.Round(so.atk * scaleFactor);
        float scaledDef = Mathf.Round(so.def * scaleFactor);

        currentElement = so.element;
        HP = scaledHp;
        MaxHP = scaledHp;
        currentAtk = scaledAtk;
        currentDef = scaledDef;
        row = so.defaultRow;
        skillCooldownRound = so.skillCdRound;
        isRevived = false;
        damageReductionPercent = 0f;
        currentShield = 0f;

        // Giữ nguyên 100% màu gốc của Sprite nghệ thuật (không nhuộm màu theo hệ)
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            colorOrigin = Color.white;
        }

        transform.localScale = Vector3.one * (9.5f * so.scale);
        UpdateHPUI(1f);
    }

    public void ReviveWithHP(float hp)
    {
        HP = Mathf.Clamp(hp, 1f, MaxHP);
        isRevived = true;
        gameObject.SetActive(true);
        UpdateHPUI(HP / MaxHP);
    }

    public virtual void Attack()
    {
        if (animController != null)
        {
            animController.ActiveAnimation(TypeAnimationAtivity.isAttack_1);
        }
    }

    public override void TakeDamage(float damage)
    {
        TakeDamage(damage, false, ElementType.Wood);
    }

    /// <summary>
    /// Nhận sát thương từ Player có tuân thủ rào cản Hàng trước - Hàng sau (GDD Row 26)
    /// </summary>
    public void TakeDamageFromPlayer(float rawDamage, bool isCrit, ElementType attackerElement, bool allowBackRow = false)
    {
        if (row == EnemyRow.Back && !allowBackRow && EnemyController.instance != null && !EnemyController.instance.AllFrontRowEnemiesDead())
        {
            // Được bảo vệ bởi rào cản Hàng trước
            return;
        }

        TakeDamage(rawDamage, isCrit, attackerElement);
    }

    public override void TakeDamage(float rawDamage, bool isCrit, ElementType attackerElement)
    {
        // Passive: Né tránh hoàn toàn (Slime Nước 10%, Kẻ Trộm Kỹ Năng 15%)
        if (type == EnemyType.SlimeNuoc && UnityEngine.Random.value <= 0.10f)
        {
            if (DamageTextManager.Instance != null)
            {
                DamageTextManager.Instance.SpawnCustomText("[NÉ TRÁNH]", Color.cyan, transform.position + Vector3.up * 1.5f, 1.2f);
            }
            return;
        }
        if (type == EnemyType.KeTromKyNang && UnityEngine.Random.value <= 0.15f)
        {
            if (DamageTextManager.Instance != null)
            {
                DamageTextManager.Instance.SpawnCustomText("[NÉ TRÁNH]", Color.cyan, transform.position + Vector3.up * 1.5f, 1.2f);
            }
            return;
        }

        // Passive: Giảm sát thương nhận vào theo thuộc tính
        if (type == EnemyType.ChienBinhDungNham && !isCrit)
        {
            rawDamage *= 0.85f; // Giảm 15% ST từ đòn đánh thường
        }
        else if (type == EnemyType.ThuyQuaiNho)
        {
            rawDamage *= 0.90f; // Giảm 10% ST vật lý
        }
        else if (type == EnemyType.TinhLinhBangGia && attackerElement == ElementType.Fire)
        {
            rawDamage *= 0.85f; // Giảm 15% ST từ kỹ năng hệ Hỏa
        }
        else if (type == EnemyType.ThuyThanSaNga && EnemyController.instance != null && EnemyController.instance.GetLivingEnemyCount() > 1)
        {
            rawDamage *= 0.80f; // Giảm 20% ST khi còn quái Thủy đồng minh
        }

        // 1. Áp dụng Giảm Sát Thương % từ Kỹ Năng Tank (nếu có)
        if (damageReductionPercent > 0f)
        {
            rawDamage *= (1f - Mathf.Clamp01(damageReductionPercent));
        }

        // 2. Hấp thụ bởi Khiên Máu Ảo (Shield) của Tank/Support (nếu có)
        float shieldBefore = currentShield;
        if (currentShield > 0f)
        {
            if (rawDamage <= currentShield)
            {
                currentShield -= rawDamage;
                rawDamage = 0f;
            }
            else
            {
                rawDamage -= currentShield;
                currentShield = 0f;
            }

            // Cơ chế nổ khiên của Hạt Nham Thạch (khi khiên vỡ phản lại 150% giá trị khiên thành ST cho Player)
            if (shieldBefore > 0f && currentShield <= 0f && shieldExplodeMultiplierOnBreak > 0f)
            {
                float explodeDamage = maxShieldGranted * shieldExplodeMultiplierOnBreak;
                Player p = Player.Instance;
                if (p != null)
                {
                    p.TakeDamage(explodeDamage, false, ElementType.Fire);
                    SpawnSkillVFX(p.transform.position, ElementType.Fire);
                    if (DamageTextManager.Instance != null)
                    {
                        DamageTextManager.Instance.SpawnCustomText($"[BÙNG NỔ KHIÊN! -{Mathf.RoundToInt(explodeDamage)}]", Color.yellow, p.transform.position + Vector3.up * 1.5f, 1.4f);
                    }
                }
            }
        }

        if (rawDamage <= 0f)
        {
            return;
        }

        float hpBefore = HP;
        base.TakeDamage(rawDamage, isCrit, attackerElement);
        float damageTaken = hpBefore - HP;

        // Buff Hiếm: Hút máu chuyển hóa và tăng tiến công kích/chí mạng
        if (InterWaveEventManager.Instance != null && damageTaken > 0)
        {
            if (InterWaveEventManager.Instance.HasActiveBuff(BuffDebuffType.Buff_Hiem_DmgToHeal))
            {
                Player.Instance?.Heal(damageTaken * 0.5f);
            }
            if (InterWaveEventManager.Instance.HasActiveBuff(BuffDebuffType.Buff_Hiem_StackAtkCrit))
            {
                Player.Instance?.ModifyATK(1.05f);
                Player.Instance?.ModifyCrit(0.05f, 0.05f);
            }
        }

        if (HP > 0 && damageTaken > 0 && animController != null)
        {
            animController.ActiveAnimation(TypeAnimationAtivity.isHit);
        }

        // Cơ chế Phản Sát Thương (Tank / Boss Hạt Nham Thạch / Boss Đất Nung phản % sát thương nhận vào)
        float totalReflect = reflectPercent;
        if (type == EnemyType.HatNhamThach || type == EnemyType.BossDatNung)
        {
            totalReflect = Mathf.Max(totalReflect, 0.10f); // Phản đòn thường trực 10%
        }

        if (totalReflect > 0 && damageTaken > 0)
        {
            float reflected = damageTaken * totalReflect;
            Player player = Player.Instance;
            if (player != null)
            {
                player.TakeDamage(reflected, false, currentElement);
            }
        }
    }

    public override void PlayEffect(EffectPayLoad effectPayLoad)
    {
        // Passive Golem Thép: Miễn nhiễm hoàn toàn hiệu ứng Choáng
        if (type == EnemyType.GolemThep && (effectPayLoad.EffectType == Effect.Stun || effectPayLoad.EffectType == Effect.Freeze))
        {
            if (DamageTextManager.Instance != null)
            {
                DamageTextManager.Instance.SpawnCustomText("[MIỄN CHOÁNG]", Color.white, transform.position + Vector3.up * 1.5f, 1.2f);
            }
            return;
        }
        base.PlayEffect(effectPayLoad);
    }

    public override void Deid()
    {
        // Passive Bọ Rác: Khi tử trận, hồi 5% Max HP cho 1 đồng minh ngẫu nhiên
        if (type == EnemyType.BoRac && EnemyController.instance != null)
        {
            var living = EnemyController.instance.LivingEnemies;
            if (living != null && living.Count > 0)
            {
                for (int i = 0; i < living.Count; i++)
                {
                    if (living[i] != this && living[i] != null && living[i].CurrentHP > 0)
                    {
                        living[i].Health(living[i].MaxHealth * 0.05f);
                        break;
                    }
                }
            }
        }

        if (animController != null)
        {
            animController.ActiveAnimation(TypeAnimationAtivity.isDied);
        }

        base.Deid();
        if (EnemyController.instance != null)
        {
            EnemyController.instance.RecordDeadEnemy(this);
            EnemyController.instance.UnregisterEnemy(this);
            EnemyController.instance.numEnemy--;

            // Kiểm tra chiến thắng Wave trong Turn-based khi toàn bộ quái bị tiêu diệt
            if (CombatTurnManager.Instance != null && CombatTurnManager.Instance.CurrentState != CombatTurnState.BattleWon && CombatTurnManager.Instance.CurrentState != CombatTurnState.NotStarted)
            {
                if (EnemyController.instance.GetLivingEnemyCount() == 0)
                {
                    CombatTurnManager.Instance.EndBattle(true);
                }
            }

            // Rơi Lõi Năng Lượng theo công thức chuẩn GDD:
            // Lõi rơi = WaveEnergy * (HP + DEF của quái) / Tổng (HP + DEF của toàn đội)
            // Quái được hồi sinh: Cố định 5 lõi
            int wave = GameManager.instance != null ? GameManager.instance.CurrentStep : 1;
            StepData stepData = LevelManager.instance != null ? LevelManager.instance.GetStepData(wave) : null;
            int waveEnergy = stepData != null ? stepData.GetWaveEnergyReward() : 25;

            int energyDrop = 5;
            if (!isRevived)
            {
                float teamTotal = EnemyController.instance != null ? EnemyController.instance.InitialWaveTotalStat : 0f;
                float myStat = MaxHP + CurrentDEF;
                if (teamTotal > 0f)
                {
                    energyDrop = Mathf.RoundToInt(waveEnergy * (myStat / teamTotal));
                }
                else
                {
                    int totalEnemies = Mathf.Max(1, EnemyController.instance.totalEnemiesInWave);
                    energyDrop = Mathf.Max(1, waveEnergy / totalEnemies);
                }
            }
            energyDrop = Mathf.Max(1, energyDrop);

            // Thu nhận Lõi Năng Lượng: Cộng trực tiếp vào chỉ số của Player trên PlayerStatsBar và kích hoạt hiệu ứng juice
            if (Player.Instance != null)
            {
                Player.Instance.AddEnergy(energyDrop);
            }
            if (PlayerStatsUI.Instance != null)
            {
                PlayerStatsUI.Instance.PlayEnergyCoreCollectJuice();
            }

        }
    }

    public override void OnDespawn()
    {
        base.OnDespawn();
        _cachedPlayer = null;
        if (EnemyController.instance != null)
        {
            EnemyController.instance.UnregisterEnemy(this);
        }
    }
}

public enum EnemyType
{
    Enemy_1 = 0,
    Enemy_2 = 1,

    // Hệ Mộc
    QuyRung,
    BoRac,
    CungThuAm,
    TinhLinhHoiMau,
    ReCayAnThit,
    PhapSuBuaChu,
    CoThuNganNam,

    // Hệ Hỏa
    TinhLinhLua,
    ChoSanDiaNguc,
    HoaQuyNho,
    PhuThuyLua,
    ChienBinhDungNham,
    HoaLongNho,
    HoaThanDietVong,

    // Hệ Thổ
    SlimeBun,
    LinhDat,
    PhapSuPhongAn,
    BoCapDa,
    HatNhamThach,
    PhuThuyKhoaNgoc,
    BossDatNung,

    // Hệ Thủy
    SlimeNuoc,
    ThuyQuaiNho,
    KeTromKyNang,
    PhapSuBang,
    QuaiVatDamLay,
    TinhLinhBangGia,
    ThuyThanSaNga,

    // Hệ Kim
    GolemThep,
    SatThuBongToi,
    KySiSat,
    CungThuXuyenGiap,
    CoMayXayThit,
    BacThaySatThu,
    CoMayVinhCuu
}
