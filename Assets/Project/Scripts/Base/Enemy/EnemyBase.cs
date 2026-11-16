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
    [Header("----ScriptableObject Data Reference----")]
    public EnemyDataSO enemyData;

    public EnemyType type;
    public bool isBoss = false;
    public float reflectPercent = 0f;
    public float currentAtk = 10f;
    public float attackCooldown = 2f;
    public EnemyCombatType combatType = EnemyCombatType.Melee;
    public float attackRange = 0.85f;

    [Header("Turn-Based Settings (GDD 5v1)")]
    public EnemyRow row = EnemyRow.Front;
    public EnemyRole role = EnemyRole.DPS_FBS;
    public int skillCooldownRound = 0;
    public int maxSkillCooldownRound = 2;
    public float skillMultiplier = 1.5f;
    public float critRate = 0.1f;
    public float critDmg = 1.5f;
    public bool isRevived = false;

    [Header("Combat Buffs & Shields")]
    public float damageReductionPercent = 0f;
    public float currentShield = 0f;
    public int damageReductionRounds = 0;
    public int shieldRounds = 0;

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
        currentAtk = data != null ? data.Atk : 10f;
        _nextAttackTime = Time.time + 0.5f; // Chờ 0.5s sau khi spawn mới tung đòn đầu tiên nếu đã trong tầm
    }

    protected virtual void Update()
    {
        if (HP <= 0) return;
        if (GameManager.instance != null && !GameManager.instance.IsWaveCombatActive) return;

        // Trong chế độ Turn-based: Toàn bộ hành động tấn công được quản lý tuần tự bởi CombatTurnManager
        if (CombatTurnManager.Instance != null && CombatTurnManager.Instance.isActiveAndEnabled)
        {
            return;
        }

        if (_cachedPlayer == null)
        {
            _cachedPlayer = Player.Instance != null ? Player.Instance : FindFirstObjectByType<Player>();
        }
        if (_cachedPlayer == null || _cachedPlayer.CurrentHP <= 0) return;

        // Tung đòn tấn công theo chu kỳ hồi chiêu (quái cận chiến lướt tới đánh rồi lùi về)
        if (Time.time >= _nextAttackTime)
        {
            _nextAttackTime = Time.time + (attackCooldown > 0 ? attackCooldown : 2.5f);
            if (combatType == EnemyCombatType.Melee)
            {
                StartCoroutine(MeleeAttackRoutine(_cachedPlayer));
            }
            else
            {
                PerformCombatAttack(_cachedPlayer);
            }
        }
    }

    private IEnumerator MeleeAttackRoutine(Player targetPlayer)
    {
        if (HP <= 0 || targetPlayer == null || targetPlayer.CurrentHP <= 0) yield break;
        Vector3 originPos = transform.position;
        Vector3 targetPos = new Vector3(targetPlayer.transform.position.x + 2.2f, targetPlayer.transform.position.y, originPos.z);

        transform.DOMove(targetPos, 0.28f).SetEase(Ease.OutQuad);
        yield return new WaitForSeconds(0.30f);

        PerformCombatAttack(targetPlayer);
        yield return new WaitForSeconds(0.30f);

        transform.DOMove(originPos, 0.25f).SetEase(Ease.InQuad);
    }

    /// <summary>
    /// Thực thi lượt hành động của quái trong hệ thống Turn-based 5v1:
    /// - Bước tới nhẹ
    /// - Ưu tiên Kỹ năng (nếu Cooldown == 0) -> Nếu đang CD thì Đánh thường (100% ATK)
    /// - Bước về vị trí ban đầu
    /// </summary>
    public virtual IEnumerator ExecuteTurnActionRoutine()
    {
        if (HP <= 0 || !gameObject.activeInHierarchy) yield break;

        if (_cachedPlayer == null)
        {
            _cachedPlayer = Player.Instance != null ? Player.Instance : FindFirstObjectByType<Player>();
        }
        if (_cachedPlayer == null || _cachedPlayer.CurrentHP <= 0) yield break;

        Vector3 originalPos = transform.position;
        bool isSkill = (skillCooldownRound <= 0);

        if (combatType == EnemyCombatType.Melee)
        {
            // QUÁI CẬN CHIẾN: Lao nhanh sang phía Player (ở bên trái màn hình)
            Vector3 targetPos = new Vector3(_cachedPlayer.transform.position.x + 2.2f, _cachedPlayer.transform.position.y, originalPos.z);
            transform.DOMove(targetPos, 0.28f).SetEase(Ease.OutQuad);
            yield return new WaitForSeconds(0.30f);

            // Chạm đích: Thực hiện Animation Attack và gây sát thương
            if (isSkill)
            {
                PerformCombatSkill(_cachedPlayer);
                skillCooldownRound = maxSkillCooldownRound;
            }
            else
            {
                PerformNormalAttack(_cachedPlayer);
            }

            yield return new WaitForSeconds(0.32f);

            // Lùi về vị trí ban đầu trong đội hình
            transform.DOMove(originalPos, 0.25f).SetEase(Ease.InQuad);
            yield return new WaitForSeconds(0.25f);
        }
        else
        {
            // QUÁI TẦM XA / PHÁP SƯ / HỖ TRỢ: Đứng tại chỗ trong đội hình thi triển
            if (isSkill)
            {
                PerformCombatSkill(_cachedPlayer);
                skillCooldownRound = maxSkillCooldownRound;
            }
            else
            {
                PerformNormalAttack(_cachedPlayer);
            }

            yield return new WaitForSeconds(0.45f);
        }
    }

    protected virtual void PerformNormalAttack(Player targetPlayer)
    {
        Attack(); // Animation
        if (combatType == EnemyCombatType.Melee)
        {
            targetPlayer.TakeDamage(currentAtk, false, currentElement);
        }
        else
        {
            ShootProjectile(targetPlayer, currentAtk);
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

        switch (role)
        {
            case EnemyRole.Tank:
                if (type == EnemyType.SlimeBun || type == EnemyType.GolemThep)
                {
                    damageReductionPercent = 0.35f;
                    damageReductionRounds = 2;
                }
                else if (type == EnemyType.BoCapDa || type == EnemyType.CoMayXayThit)
                {
                    reflectPercent = 0.25f;
                }
                else
                {
                    currentShield = MaxHealth * 0.25f;
                    shieldRounds = 2;
                }
                SpawnSkillVFX(transform.position, currentElement);
                break;

            case EnemyRole.Support:
                if (type == EnemyType.ReCayAnThit || type == EnemyType.PhapSuBuaChu || type == EnemyType.CoThuNganNam)
                {
                    float reviveHp = (type == EnemyType.CoThuNganNam) ? 0.50f : 0.35f;
                    bool revived = TryReviveAlly(reviveHp);
                    if (!revived)
                    {
                        HealAllies(0.15f);
                    }
                }
                else
                {
                    HealAllies(0.15f);
                }
                SpawnSkillVFX(transform.position, currentElement);
                break;

            case EnemyRole.Mage:
                if (type == EnemyType.KeTromKyNang || type == EnemyType.ThuyThanSaNga)
                {
                    if (targetPlayer != null)
                    {
                        targetPlayer.TrySpendEnergy(5);
                        Health(MaxHealth * 0.10f);
                    }
                }
                else if (type == EnemyType.PhapSuBang || type == EnemyType.TinhLinhBangGia)
                {
                    if (BoardController.instance != null)
                    {
                        BoardController.instance.LockRandomStones(2, 2);
                    }
                }
                else
                {
                    if (BoardController.instance != null)
                    {
                        int lockCount = (type == EnemyType.PhuThuyKhoaNgoc || type == EnemyType.BossDatNung) ? 4 : 2;
                        BoardController.instance.LockRandomStones(lockCount, 2);
                    }
                }
                SpawnSkillVFX(transform.position, currentElement);
                float mageDmg = currentAtk * (skillMultiplier > 0 ? skillMultiplier : 0.6f);
                ShootProjectile(targetPlayer, mageDmg);
                break;

            case EnemyRole.DPS_FBS:
            default:
                SpawnSkillVFX(transform.position, currentElement);
                float skillDmg = currentAtk * (skillMultiplier > 0 ? skillMultiplier : 1.8f);
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
                break;
        }
    }

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
    protected virtual void ShootProjectile(Player targetPlayer, float damage = 0f)
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
                proj.Launch(finalDmg, currentElement, spawnPos, targetPos, 22.0f);
            }
        }
        else
        {
            // Fallback nếu không tải được prefab
            targetPlayer.TakeDamage(finalDmg, false, currentElement);
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
        reflectPercent = reflect;
        attackCooldown = attackCd;
        combatType = cType;
        attackRange = atkRange > 0 ? atkRange : (cType == EnemyCombatType.Ranged ? 3.8f : 0.85f);
        role = enemyRole;
        skillMultiplier = sMultiplier;
        maxSkillCooldownRound = sCdRound;
        skillCooldownRound = sCdRound;
        critRate = cRate;
        critDmg = cDmg;
        isRevived = false;
        damageReductionPercent = 0f;
        currentShield = 0f;

        // Cập nhật BaseCharacterDataSO runtime hoặc ghi đè
        if (data != null)
        {
            data.Dev = def;
            // Áp dụng công thức tính block: 1 block = (khoảng cách mép đối diện đến player) / 20
            data.Speed_Trans = BlockMetrics.ConvertSpeedToWorldUnits(speed);
        }

        // Tự động nhuộm màu nguyên tố và chỉnh kích thước quái (Bọ Rác nhỏ, Boss to)
        if (spriteRenderer != null)
        {
            Color elemColor = ElementalHelper.GetColor(elem);
            // Pha nhẹ với màu trắng để không làm mờ chi tiết sprite gốc
            Color finalTint = Color.Lerp(Color.white, elemColor, 0.65f);
            colorOrigin = finalTint;
            spriteRenderer.color = finalTint;
        }

        // Nâng base scale lên 9.5f để quái (PPU = 200, scale 0.85) có chiều cao thế giới đạt ~5.0 units ngang bằng Player (5.15 units)
        transform.localScale = Vector3.one * (9.5f * scale);
        UpdateHPUI(1f);
    }

    /// <summary>
    /// Khởi tạo quái trực tiếp từ ScriptableObject EnemyDataSO theo chuẩn GDD
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

        InitializeStats(
            so.enemyType,
            so.element,
            scaledHp,
            scaledAtk,
            scaledDef,
            so.speed,
            so.reflect,
            so.scale,
            so.attackCd,
            so.combatType,
            so.attackRange,
            so.role,
            so.skillMultiplier,
            so.skillCdRound,
            so.critRate,
            so.critDmg
        );

        isBoss = so.isBoss || (so.category != EnemyCategory.Normal);
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
        if (animController != null)
        {
            animController.ActiveAnimation(TypeAnimationAtivity.isHit);
        }

        // 1. Áp dụng Giảm Sát Thương % từ Kỹ Năng Tank (nếu có)
        if (damageReductionPercent > 0f)
        {
            rawDamage *= (1f - Mathf.Clamp01(damageReductionPercent));
        }

        // 2. Hấp thụ bởi Khiên Máu Ảo (Shield) của Tank/Support (nếu có)
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
        }

        if (rawDamage <= 0f)
        {
            return;
        }

        float hpBefore = HP;
        base.TakeDamage(rawDamage, isCrit, attackerElement);
        float damageTaken = hpBefore - HP;

        // Cơ chế Phản Sát Thương (Tank / Boss Hạt Nham Thạch phản % sát thương nhận vào)
        if (reflectPercent > 0 && damageTaken > 0)
        {
            float reflected = damageTaken * reflectPercent;
            Player player = FindFirstObjectByType<Player>();
            if (player != null)
            {
                player.TakeDamage(reflected, false, currentElement);
            }
        }
    }

    public override void Deid()
    {
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
                float myStat = MaxHP + (data != null ? data.Dev : 0f);
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
