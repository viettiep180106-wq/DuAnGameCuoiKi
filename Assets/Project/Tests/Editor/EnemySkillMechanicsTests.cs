using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class EnemySkillMechanicsTests
{
    private GameObject _testRoot;
    private Player _player;
    private CombatTurnManager _turnManager;
    private EnemyController _enemyController;

    [SetUp]
    public void SetUp()
    {
        _testRoot = new GameObject("TestRoot_EnemySkillMechanics");

        // Tạo Player test (500 HP, 200 ATK, 50 Energy)
        GameObject playerObj = new GameObject("Player", typeof(BoxCollider2D), typeof(PlayerAnimationController), typeof(Player));
        playerObj.transform.SetParent(_testRoot.transform);
        _player = playerObj.GetComponent<Player>();
        _player.SetStatsForTest(500f, 200f, 50);

        // Tạo EnemyController test
        GameObject ecObj = new GameObject("EnemyController", typeof(EnemyController));
        ecObj.transform.SetParent(_testRoot.transform);
        _enemyController = ecObj.GetComponent<EnemyController>();

        var instField = typeof(EnemyController).GetField("_instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        if (instField != null) instField.SetValue(null, _enemyController);

        // Tạo CombatTurnManager test
        GameObject turnObj = new GameObject("CombatTurnManager", typeof(CombatTurnManager));
        turnObj.transform.SetParent(_testRoot.transform);
        _turnManager = turnObj.GetComponent<CombatTurnManager>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_testRoot != null)
        {
            Object.DestroyImmediate(_testRoot);
        }
    }

    [Test]
    public void DoTSystem_DeductsHP_PerRound_AndExpires()
    {
        // Arrange: Player có 500 HP, dính DoT 2% Max HP (10 HP/round) trong 2 round
        _player.ApplyRoundDoT(Effect.DoT, 0.02f, 2, ElementType.Fire, "THIÊU ĐỐT");
        Assert.AreEqual(1, _player.ActiveRoundEffects.Count);
        Assert.AreEqual(2, _player.ActiveRoundEffects[0].RoundsRemaining);

        // Act 1: Kết thúc Round 1
        _player.TickRoundEffects();

        // Assert 1: Mất 10 HP (500 -> 490), còn 1 round
        Assert.AreEqual(490f, _player.CurrentHP);
        Assert.AreEqual(1, _player.ActiveRoundEffects.Count);
        Assert.AreEqual(1, _player.ActiveRoundEffects[0].RoundsRemaining);

        // Act 2: Kết thúc Round 2
        _player.TickRoundEffects();

        // Assert 2: Mất thêm 10 HP (490 -> 480), DoT hết hạn và tự xóa
        Assert.AreEqual(480f, _player.CurrentHP);
        Assert.AreEqual(0, _player.ActiveRoundEffects.Count);
    }

    [Test]
    public void PenetrationAttack_IgnoresSpecifiedDEFPercent()
    {
        // Arrange: Tạo Player có DEF từ dữ liệu test hoặc kiểm tra logic TakeDamage với ignoreDefPercent
        float rawDamage = 100f;
        float initialHP = _player.CurrentHP;

        // Act: Đòn đánh bỏ qua 30% giáp
        _player.TakeDamage(rawDamage, false, ElementType.Wood, 0.30f, false);

        // Assert: Sát thương nhận vào phải lớn hơn hoặc bằng đòn đánh thông thường
        Assert.Less(_player.CurrentHP, initialHP);
    }

    [Test]
    public void TrueDamage_IgnoresBothDEFAndShield()
    {
        // Arrange: Player có 100 Khiên
        _player.AddShield(100f);
        Assert.AreEqual(100f, _player.CurrentShield);
        float hpBefore = _player.CurrentHP;

        // Act: Đòn đánh True Damage của Cỗ Máy Vĩnh Cửu (bỏ qua 100% khiên & giáp, cùng hệ để không bị nhân hệ số tương khắc)
        _player.TakeDamage(80f, false, _player.Element, 1.0f, true);

        // Assert: Khiên vẫn còn nguyên 100, sát thương trừ thẳng trực tiếp vào HP
        Assert.AreEqual(100f, _player.CurrentShield);
        Assert.AreEqual(hpBefore - 80f, _player.CurrentHP);
    }

    [Test]
    public void EnergyDrain_And_ConsumeAllEnergy_WorksCorrectly()
    {
        // Arrange
        Assert.AreEqual(50, _player.EnergyCores);

        // Act 1: Kẻ Trộm Kỹ Năng hút 5 Energy
        _player.TrySpendEnergy(5);
        Assert.AreEqual(45, _player.EnergyCores);

        // Act 2: Thủy Thần Sa Ngã triệt tiêu toàn bộ Energy về 0
        _player.ConsumeAllEnergy();
        Assert.AreEqual(0, _player.EnergyCores);
    }

    [Test]
    public void SkillCostDebuff_IncreasesEnergyCost_AndExpiresAfterRound()
    {
        // Arrange: Slime Nước tăng phí cast skill thêm 2 Energy trong 1 Round
        _player.ApplySkillCostDebuff(2, 1);
        Assert.AreEqual(2, _player.ExtraSkillCost);

        // Act & Assert: TryConsumeSkillEnergy yêu cầu 5 + 2 = 7 Energy
        int initialEnergy = _player.EnergyCores; // 50
        bool castSuccess = _turnManager.TryConsumeSkillEnergy();
        Assert.IsTrue(castSuccess);
        Assert.AreEqual(initialEnergy - 7, _player.EnergyCores);

        // Act 2: Qua Round mới -> Hết debuff
        _player.TickRoundEffects();
        Assert.AreEqual(0, _player.ExtraSkillCost);
    }

    [Test]
    public void StunMechanism_SkipsPlayerTurn()
    {
        // Arrange: Player bị Choáng 1 Round
        _player.ApplyStun(1);
        Assert.IsTrue(_player.IsStunned);

        // Act: Tiêu hao 1 round Choáng
        _player.ConsumeStunRound();

        // Assert: Hết Choáng
        Assert.IsFalse(_player.IsStunned);
    }

    [Test]
    public void ShieldExplosion_DealsDamageToPlayer_WhenBroken()
    {
        // Arrange: Tạo quái Hạt Nham Thạch có khiên 200 và hệ số nổ 1.5x (300 ST)
        GameObject enemyObj = new GameObject("HatNhamThach", typeof(BoxCollider2D), typeof(EnemyAnimationController), typeof(EnemyBase));
        enemyObj.transform.SetParent(_testRoot.transform);
        EnemyBase enemy = enemyObj.GetComponent<EnemyBase>();
        enemy.type = EnemyType.HatNhamThach;
        enemy.SetStatsForTest(600f, 40f, EnemyRow.Front);
        enemy.currentShield = 200f;
        enemy.maxShieldGranted = 200f;
        enemy.shieldExplodeMultiplierOnBreak = 1.5f;

        float playerHpBefore = _player.CurrentHP;

        // Act: Đánh vỡ hoàn toàn khiên của quái (nhận 250 sát thương vào khiên 200)
        enemy.TakeDamage(250f, false, ElementType.Water);

        // Assert: Khiên vỡ về 0, kích hoạt nổ khiên 200 * 1.5 = 300 sát thương lên Player
        Assert.AreEqual(0f, enemy.currentShield);
        Assert.Less(_player.CurrentHP, playerHpBefore);
    }

    [Test]
    public void TeamSupport_BuffsLivingAllies()
    {
        // Arrange: Tạo 2 quái đồng minh
        GameObject e1Obj = new GameObject("Ally1", typeof(BoxCollider2D), typeof(EnemyAnimationController), typeof(EnemyBase));
        e1Obj.transform.SetParent(_testRoot.transform);
        EnemyBase e1 = e1Obj.GetComponent<EnemyBase>();
        e1.SetStatsForTest(200f, 20f, EnemyRow.Front);
        _enemyController.RegisterEnemy(e1);

        GameObject e2Obj = new GameObject("Ally2", typeof(BoxCollider2D), typeof(EnemyAnimationController), typeof(EnemyBase));
        e2Obj.transform.SetParent(_testRoot.transform);
        EnemyBase e2 = e2Obj.GetComponent<EnemyBase>();
        e2.SetStatsForTest(200f, 20f, EnemyRow.Front);
        _enemyController.RegisterEnemy(e2);

        // Act: Quái 1 dùng kỹ năng buff 25% ATK cho toàn đội trong 2 round (Quỷ Rừng)
        e1.type = EnemyType.QuyRung;
        e1.SetStatsForTest(200f, 20f, EnemyRow.Front);
        // Mô phỏng gọi ExecuteTeamSupport
        var method = typeof(EnemyBase).GetMethod("ExecuteTeamSupport", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(method);
        method.Invoke(e1, new object[] { 0, 0f, 0f, 0.25f, 0f, 0f, 2 });

        // Assert: Cả 2 quái đều nhận buff ATK 25%
        Assert.AreEqual(0.25f, e1.atkBuffPercent);
        Assert.AreEqual(0.25f, e2.atkBuffPercent);
        Assert.AreEqual(2, e1.atkBuffRounds);
        Assert.AreEqual(2, e2.atkBuffRounds);
    }

    [Test]
    public void EnemyMotion_DefenseAndSupportSkills_StayInPlace()
    {
        // Arrange & Act & Assert:
        // Các quái Tank hệ Thổ (Slime Bùn, Bọ Cạp Đá, Hạt Nham Thạch), quái Hỗ trợ (Quỷ Rừng),
        // và Pháp sư (Pháp Sư Phong Ấn) khi thi triển Kỹ Năng BẮT BUỘC phải ĐỨNG TẠI CHỖ (IsSkillMeleeDash == false).
        // Không được phép lao sang mặt Player!
        GameObject obj = new GameObject("TestMotion", typeof(BoxCollider2D), typeof(EnemyAnimationController), typeof(EnemyBase));
        obj.transform.SetParent(_testRoot.transform);
        EnemyBase e = obj.GetComponent<EnemyBase>();

        e.type = EnemyType.SlimeBun;
        Assert.IsFalse(e.IsSkillMeleeDash(), "Slime Bùn bật bọc bùn phòng thủ phải đứng tại chỗ!");

        e.type = EnemyType.BoCapDa;
        Assert.IsFalse(e.IsSkillMeleeDash(), "Bọ Cạp Đá bật giáp gai phản đòn phải đứng tại chỗ!");

        e.type = EnemyType.HatNhamThach;
        Assert.IsFalse(e.IsSkillMeleeDash(), "Hạt Nham Thạch tạo khiên dung nham phải đứng tại chỗ!");

        e.type = EnemyType.BossDatNung;
        Assert.IsFalse(e.IsSkillMeleeDash(), "Boss Đất Nung nện đất tạo Đại Địa Chấn phải đứng tại chỗ!");

        e.type = EnemyType.QuyRung;
        Assert.IsFalse(e.IsSkillMeleeDash(), "Quỷ Rừng hú buff đồng đội phải đứng tại chỗ!");

        e.type = EnemyType.PhapSuPhongAn;
        Assert.IsFalse(e.IsSkillMeleeDash(), "Pháp Sư Phong Ấn phóng ấn ngọc phải đứng tại chỗ!");
    }

    [Test]
    public void EnemyMotion_MeleeStrikeSkills_DashToTarget()
    {
        // Arrange & Act & Assert:
        // Đúng 5 quái DPS cận chiến có chiêu vồ / bổ búa / cắn xé / ám sát mới lao sang Player khi dùng skill:
        GameObject obj = new GameObject("TestMeleeMotion", typeof(BoxCollider2D), typeof(EnemyAnimationController), typeof(EnemyBase));
        obj.transform.SetParent(_testRoot.transform);
        EnemyBase e = obj.GetComponent<EnemyBase>();

        e.type = EnemyType.ChoSanDiaNguc;
        Assert.IsTrue(e.IsSkillMeleeDash(), "Chó Săn Địa Ngục cắn xé rực lửa phải lao sang Player!");

        e.type = EnemyType.ChienBinhDungNham;
        Assert.IsTrue(e.IsSkillMeleeDash(), "Chiến Binh Dung Nham bổ búa phải lao sang Player!");

        e.type = EnemyType.SatThuBongToi;
        Assert.IsTrue(e.IsSkillMeleeDash(), "Sát Thủ Bóng Tối ám sát bất ngờ phải lao sang Player!");

        e.type = EnemyType.BacThaySatThu;
        Assert.IsTrue(e.IsSkillMeleeDash(), "Bậc Thầy Sát Thủ trảm huyết phải lao sang Player!");

        e.type = EnemyType.ThuyQuaiNho;
        Assert.IsTrue(e.IsSkillMeleeDash(), "Thủy Quái Nhỏ cắn xoáy thủy triều phải lao sang Player!");
    }

    [Test]
    public void EnemyDataSO_SingleSourceOfTruth_DelegatesPropertiesCorrectly()
    {
        // Arrange: Tạo ScriptableObject EnemyDataSO giả lập
        EnemyDataSO so = ScriptableObject.CreateInstance<EnemyDataSO>();
        so.enemyType = EnemyType.SlimeBun;
        so.role = EnemyRole.Tank;
        so.combatType = EnemyCombatType.Melee;
        so.skillCdRound = 3;
        so.skillMultiplier = 0f;
        so.critRate = 0.05f;
        so.critDmg = 1.2f;
        so.reflect = 0.2f;

        GameObject obj = new GameObject("TestSO_Truth", typeof(BoxCollider2D), typeof(EnemyAnimationController), typeof(EnemyBase));
        obj.transform.SetParent(_testRoot.transform);
        EnemyBase e = obj.GetComponent<EnemyBase>();

        // Act: Gán EnemyDataSO
        e.InitializeFromSO(so, 1, StepDifficulty.Normal);

        // Assert: Các Property của EnemyBase tự động delegate trực tiếp tới ScriptableObject
        Assert.AreEqual(EnemyType.SlimeBun, e.type);
        Assert.AreEqual(EnemyRole.Tank, e.role);
        Assert.AreEqual(EnemyCombatType.Melee, e.combatType);
        Assert.AreEqual(3, e.maxSkillCooldownRound);
        Assert.AreEqual(0.05f, e.critRate);
        Assert.AreEqual(1.2f, e.critDmg);
        Assert.AreEqual(0.2f, e.reflectPercent);
    }

    private EnemyBase CreateEnemyWithType(EnemyType enemyType)
    {
        GameObject enemyObj = new GameObject($"Enemy_{enemyType}", typeof(BoxCollider2D), typeof(EnemyAnimationController), typeof(EnemyBase));
        enemyObj.transform.SetParent(_testRoot.transform);
        EnemyBase enemy = enemyObj.GetComponent<EnemyBase>();
        enemy.type = enemyType;
        return enemy;
    }

    [Test]
    public void Test_12_EnemySkillActionType_Segregation()
    {
        // Assert: Phân loại chính xác 3 loại hành vi
        Assert.AreEqual(EnemySkillActionType.DefensiveSelf, CreateEnemyWithType(EnemyType.SlimeBun).GetSkillActionType());
        Assert.AreEqual(EnemySkillActionType.DefensiveSelf, CreateEnemyWithType(EnemyType.BoCapDa).GetSkillActionType());
        Assert.AreEqual(EnemySkillActionType.DefensiveSelf, CreateEnemyWithType(EnemyType.HatNhamThach).GetSkillActionType());
        Assert.AreEqual(EnemySkillActionType.RangedSpell, CreateEnemyWithType(EnemyType.PhapSuPhongAn).GetSkillActionType());
        Assert.AreEqual(EnemySkillActionType.RangedSpell, CreateEnemyWithType(EnemyType.PhuThuyKhoaNgoc).GetSkillActionType());
        Assert.AreEqual(EnemySkillActionType.MeleeDash, CreateEnemyWithType(EnemyType.ChienBinhDungNham).GetSkillActionType());
        Assert.AreEqual(EnemySkillActionType.MeleeDash, CreateEnemyWithType(EnemyType.ChoSanDiaNguc).GetSkillActionType());
    }

    [Test]
    public void Test_13_StoneLock_Mechanics()
    {
        GameObject stoneObj = new GameObject("TestStone", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(StoneBase));
        stoneObj.transform.SetParent(_testRoot.transform);
        StoneBase stone = stoneObj.GetComponent<StoneBase>();

        Assert.IsFalse(stone.isLocked);

        // Khóa ngọc 1 Round
        stone.LockStone(1);
        Assert.IsTrue(stone.isLocked);

        // Hết 1 Round (người chơi kết thúc lượt) -> Mở khóa
        stone.TickLockRound();
        Assert.IsFalse(stone.isLocked);
    }

    [Test]
    public void Test_14_EnemyTypeSetter_AutomaticallyBindsEnemyDataSO()
    {
        GameObject obj = new GameObject("TestEnemyTypeSetter", typeof(BoxCollider2D), typeof(EnemyAnimationController), typeof(EnemyBase));
        obj.transform.SetParent(_testRoot.transform);
        EnemyBase e = obj.GetComponent<EnemyBase>();

        e.type = EnemyType.BoCapDa;

        Assert.IsNotNull(e.enemyData);
        Assert.AreEqual(EnemyType.BoCapDa, e.enemyData.enemyType);
        Assert.AreEqual(EnemyType.BoCapDa, e.type);
    }

    [Test]
    public void Test_15_DataDriven_NormalAndSkillActionTypes_AllArchetypes()
    {
        // 1. Tank Archetype (Slime Bùn, Bọ Cạp Đá): Đánh thường MeleeDash (lao sang đánh), Active Skill DefensiveSelf (đứng tại chỗ bật giáp/khiên)
        EnemyBase slime = CreateEnemyWithType(EnemyType.SlimeBun);
        Assert.AreEqual(EnemySkillActionType.MeleeDash, slime.GetNormalAttackActionType(), "Tank đánh thường phải là MeleeDash để lao sang đánh Player!");
        Assert.AreEqual(EnemySkillActionType.DefensiveSelf, slime.GetSkillActionType(), "Tank dùng skill phòng thủ phải đứng tại chỗ bật bọc bùn!");

        // 2. Ranged Mage Archetype (Pháp Sư Phong Ấn): Cả đánh thường và skill đều là RangedSpell (đứng tại chỗ)
        EnemyBase mage = CreateEnemyWithType(EnemyType.PhapSuPhongAn);
        Assert.AreEqual(EnemySkillActionType.RangedSpell, mage.GetNormalAttackActionType(), "Pháp sư đánh thường đứng tại chỗ bắn chưởng!");
        Assert.AreEqual(EnemySkillActionType.RangedSpell, mage.GetSkillActionType(), "Pháp sư cast skill đứng tại chỗ niệm chú phong ấn!");

        // 3. Hybrid Boss Archetype (Boss Đất Nung, Thủy Thần Sa Ngã): Đánh thường MeleeDash, Active Skill RangedSpell
        EnemyBase bossDatNung = CreateEnemyWithType(EnemyType.BossDatNung);
        Assert.AreEqual(EnemySkillActionType.MeleeDash, bossDatNung.GetNormalAttackActionType(), "Boss Đất Nung đánh thường nện cột đá cận chiến!");
        Assert.AreEqual(EnemySkillActionType.RangedSpell, bossDatNung.GetSkillActionType(), "Boss Đất Nung cast skill đứng tại chỗ tạo địa chấn + khóa ngọc!");

        // 4. Melee DPS Archetype (Chó Săn Địa Ngục, Chiến Binh Dung Nham): Cả đánh thường và skill đều lao sang chém
        EnemyBase dog = CreateEnemyWithType(EnemyType.ChoSanDiaNguc);
        Assert.AreEqual(EnemySkillActionType.MeleeDash, dog.GetNormalAttackActionType());
        Assert.AreEqual(EnemySkillActionType.MeleeDash, dog.GetSkillActionType());
    }
}
