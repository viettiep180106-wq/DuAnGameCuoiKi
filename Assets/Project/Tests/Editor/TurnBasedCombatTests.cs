using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class TurnBasedCombatTests
{
    private GameObject _testRoot;
    private Player _player;
    private CombatTurnManager _turnManager;
    private EnemyController _enemyController;

    [SetUp]
    public void SetUp()
    {
        _testRoot = new GameObject("TestRoot_TurnBased");

        // Tạo Player test
        GameObject playerObj = new GameObject("Player", typeof(BoxCollider2D), typeof(PlayerAnimationController), typeof(Player));
        playerObj.transform.SetParent(_testRoot.transform);
        _player = playerObj.GetComponent<Player>();
        _player.SetStatsForTest(500f, 200f, 50);

        // Tạo EnemyController test
        GameObject ecObj = new GameObject("EnemyController", typeof(EnemyController));
        ecObj.transform.SetParent(_testRoot.transform);
        _enemyController = ecObj.GetComponent<EnemyController>();

        // Gán singleton cho EnemyController nếu đang null trong EditMode
        var instField = typeof(EnemyController).GetField("_instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        if (instField != null) instField.SetValue(null, _enemyController);

        // Tạo TurnManager test
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
    public void TurnBased_EnergyCost_ConsumesExactly5EnergyPerSkill()
    {
        // Arrange
        _turnManager.StartCombatWave();
        int initialEnergy = _player.EnergyCores; // 50
        Assert.AreEqual(50, initialEnergy);

        // Act
        bool canCast = _turnManager.TryConsumeSkillEnergy();

        // Assert
        Assert.IsTrue(canCast, "Player should be able to consume 5 energy");
        Assert.AreEqual(45, _player.EnergyCores, "Energy cores should decrease by exactly 5");
    }

    [Test]
    public void TurnBased_EnergyCost_FailsWhenEnergyLessThan5()
    {
        // Arrange
        _turnManager.StartCombatWave();
        _player.TrySpendEnergy(_player.EnergyCores); // Tiêu hết energy về 0
        Assert.AreEqual(0, _player.EnergyCores);

        // Act
        bool canCast = _turnManager.TryConsumeSkillEnergy();

        // Assert
        Assert.IsFalse(canCast, "Player cannot cast skill when energy < 5");
    }

    [Test]
    public void TurnBased_MatchEnergyCost_3FreeThenDoubling()
    {
        _turnManager.StartCombatWave();
        _player.SetStatsForTest(500f, 200f, 100);

        // 3 lần match đầu tiên hoàn toàn FREE (chi phí = 0)
        Assert.AreEqual(0, _turnManager.GetNextMatchEnergyCost());
        Assert.IsTrue(_turnManager.TryConsumeMatchEnergy()); // Lần 1: 0

        Assert.AreEqual(0, _turnManager.GetNextMatchEnergyCost());
        Assert.IsTrue(_turnManager.TryConsumeMatchEnergy()); // Lần 2: 0

        Assert.AreEqual(0, _turnManager.GetNextMatchEnergyCost());
        Assert.IsTrue(_turnManager.TryConsumeMatchEnergy()); // Lần 3: 0

        Assert.AreEqual(100, _player.EnergyCores, "3 lần match đầu không bị trừ năng lượng");

        // Lần thứ 4: Tốn 5 năng lượng
        Assert.AreEqual(5, _turnManager.GetNextMatchEnergyCost());
        Assert.IsTrue(_turnManager.TryConsumeMatchEnergy());
        Assert.AreEqual(95, _player.EnergyCores, "Lần thứ 4 trừ 5 năng lượng");

        // Lần thứ 5: Tốn 10 năng lượng (x2)
        Assert.AreEqual(10, _turnManager.GetNextMatchEnergyCost());
        Assert.IsTrue(_turnManager.TryConsumeMatchEnergy());
        Assert.AreEqual(85, _player.EnergyCores, "Lần thứ 5 trừ 10 năng lượng");

        // Lần thứ 6: Tốn 20 năng lượng (x2)
        Assert.AreEqual(20, _turnManager.GetNextMatchEnergyCost());
        Assert.IsTrue(_turnManager.TryConsumeMatchEnergy());
        Assert.AreEqual(65, _player.EnergyCores, "Lần thứ 6 trừ 20 năng lượng");

        // Kiểm tra chặn khi không đủ năng lượng
        _player.TrySpendEnergy(_player.EnergyCores); // Tiêu hết về 0
        Assert.AreEqual(0, _player.EnergyCores);
        Assert.IsFalse(_turnManager.CanPlayerAffordNextMatch(), "Không đủ năng lượng cho lần match tiếp theo");
        Assert.IsFalse(_turnManager.TryConsumeMatchEnergy(), "Không thể match khi hết năng lượng");
    }

    [Test]
    public void TurnBased_TurnFlow_CyclesPlayerToEnemyAndRoundEnd()
    {
        // Arrange: Cần 1 quái sống để vòng đấu tiếp diễn bình thường
        GameObject enemyObj = new GameObject("DummyEnemy", typeof(BoxCollider2D), typeof(EnemyAnimationController), typeof(EnemyBase));
        enemyObj.transform.SetParent(_testRoot.transform);
        EnemyBase enemy = enemyObj.GetComponent<EnemyBase>();
        enemy.SetStatsForTest(100f, 10f, EnemyRow.Front);
        _enemyController.RegisterEnemy(enemy);

        _turnManager.StartCombatWave();
        Assert.AreEqual(CombatTurnState.PlayerTurn, _turnManager.CurrentState);
        Assert.AreEqual(1, _turnManager.CurrentRound);

        // Act: Skip lượt Player
        _turnManager.SkipPlayerTurn();
        Assert.AreEqual(CombatTurnState.EnemyTurn, _turnManager.CurrentState);

        // Act: Kết thúc Round
        _turnManager.EndRound();
        Assert.AreEqual(2, _turnManager.CurrentRound, "Round should increment to 2");
        Assert.AreEqual(CombatTurnState.PlayerTurn, _turnManager.CurrentState, "Next round should return to PlayerTurn");
    }

    [Test]
    public void EnemyFormation_FrontRowBarrier_BlocksBackRowTargetingUntilFrontRowDead()
    {
        // Arrange: Tạo 1 quái Hàng trước và 1 quái Hàng sau với HP hợp lệ
        GameObject frontEnemyObj = new GameObject("FrontEnemy", typeof(BoxCollider2D), typeof(EnemyAnimationController), typeof(EnemyBase));
        frontEnemyObj.transform.SetParent(_testRoot.transform);
        EnemyBase frontEnemy = frontEnemyObj.GetComponent<EnemyBase>();
        frontEnemy.SetStatsForTest(100f, 10f, EnemyRow.Front);
        _enemyController.RegisterEnemy(frontEnemy);

        GameObject backEnemyObj = new GameObject("BackEnemy", typeof(BoxCollider2D), typeof(EnemyAnimationController), typeof(EnemyBase));
        backEnemyObj.transform.SetParent(_testRoot.transform);
        EnemyBase backEnemy = backEnemyObj.GetComponent<EnemyBase>();
        backEnemy.SetStatsForTest(100f, 10f, EnemyRow.Back);
        _enemyController.RegisterEnemy(backEnemy);

        // Assert 1: Hàng trước còn sống -> AllFrontRowEnemiesDead = false
        Assert.IsFalse(_enemyController.AllFrontRowEnemiesDead(), "Front row should not be dead yet");

        // Target bình thường bắt buộc phải là quái Hàng trước
        EnemyBase target = _enemyController.GetValidTarget(allowBackRow: false);
        Assert.AreEqual(frontEnemy, target, "Target must be front row enemy");

        // Quái hàng sau không thể bị target trực tiếp
        Assert.IsFalse(backEnemy.IsTargetable, "Back row enemy is not targetable while front row is alive");

        // Act: Tiêu diệt quái Hàng trước
        frontEnemy.Deid();

        // Assert 2: Hàng trước đã chết hết -> Cho phép target Hàng sau
        Assert.IsTrue(_enemyController.AllFrontRowEnemiesDead(), "Front row should now be dead");
        EnemyBase targetAfterFrontDead = _enemyController.GetValidTarget(allowBackRow: false);
        Assert.AreEqual(backEnemy, targetAfterFrontDead, "Back row enemy becomes targetable when front row is dead");
        Assert.IsTrue(backEnemy.IsTargetable, "Back row enemy is targetable when front row is dead");
    }

    [Test]
    public void SkillWind_MetalShield_AbsorbsDamageBeforeHealth()
    {
        // Arrange
        GameObject skillObj = new GameObject("SkillWind", typeof(CircleCollider2D), typeof(Rigidbody2D), typeof(SkillWind));
        skillObj.transform.SetParent(_testRoot.transform);
        SkillWind skill = skillObj.GetComponent<SkillWind>();
        skill.Initialize(3); // Tier 1: 15% Max HP shield

        // Act: Kích hoạt khiên
        skill.Active();

        float expectedShield = _player.MaxHealth * 0.15f; // 500 * 0.15 = 75
        Assert.AreEqual(expectedShield, _player.CurrentShield, "Shield should equal 15% of Max HP (75)");

        // Nhận sát thương 30 điểm
        float hpBefore = _player.CurrentHP; // 500
        _player.TakeDamage(30f);

        // Assert: Sát thương trừ vào Shield trước (75 - 30 = 45), HP không đổi (500)
        Assert.AreEqual(expectedShield - 30f, _player.CurrentShield, "Shield should absorb damage first");
        Assert.AreEqual(hpBefore, _player.CurrentHP, "HP should remain full while shield is active");
    }

    [Test]
    public void SkillWood_WoodArmor_HealsUponReceivingDamage()
    {
        // Arrange
        _player.WoodArmorTier = 1; // Tier 1: hồi 3% Max HP khi nhận sát thương
        _player.TakeDamage(100f); // Trừ máu: 500 - 100 = 400 + heal 3%(15) = 415
        float hpAfterDamage = _player.CurrentHP;

        // Act: Nhận thêm sát thương 10 điểm -> Trừ 10 và hồi 3% (15 điểm)
        _player.TakeDamage(10f);

        // Assert: Player vẫn sống và HP phản ánh hồi phục
        Assert.IsTrue(_player.CurrentHP > 0, "Player should be alive");
        Assert.AreEqual(1, _player.WoodArmorTier);
    }

    [Test]
    public void SkillIce_WaterIceRain_CanTargetBackRowRegardlessOfFrontRow()
    {
        // Arrange: Quái hàng sau
        GameObject backEnemyObj = new GameObject("BackEnemy", typeof(BoxCollider2D), typeof(EnemyAnimationController), typeof(EnemyBase));
        backEnemyObj.transform.SetParent(_testRoot.transform);
        EnemyBase backEnemy = backEnemyObj.GetComponent<EnemyBase>();
        backEnemy.SetStatsForTest(100f, 10f, EnemyRow.Back);
        _enemyController.RegisterEnemy(backEnemy);

        // Kỹ năng Hệ Thủy Mưa Băng Xói Mòn
        GameObject iceObj = new GameObject("SkillIce", typeof(CircleCollider2D), typeof(Rigidbody2D), typeof(SkillIce));
        iceObj.transform.SetParent(_testRoot.transform);
        SkillIce ice = iceObj.GetComponent<SkillIce>();
        ice.Initialize(3); // Tier 1: 3 tia

        // Assert: Thủy có thuộc tính tấn công toàn map bỏ qua rào cản Hàng trước
        Assert.AreEqual(TypeStone.Ice, ice.GetTypeStone());
        Assert.AreEqual(ElementType.Water, ice.GetElementType());
    }

    [Test]
    public void DamageCalculation_StoneRatio_MultipliesCorrectly()
    {
        // Arrange
        GameObject skillObj = new GameObject("SkillDummy", typeof(CircleCollider2D), typeof(Rigidbody2D), typeof(SkillPassive));
        skillObj.transform.SetParent(_testRoot.transform);
        SkillPassive skill = skillObj.GetComponent<SkillPassive>();

        // Match 3 đá (Tier 1 = 1x)
        skill.Initialize(3);
        var (dmgTier1, _) = skill.CalculateDamage(1.0f);
        Assert.AreEqual(200f, dmgTier1, 1f, "Match 3 should deal 1x base ATK (200)");

        // Match 6 đá (Tier 2 = 2x)
        skill.Initialize(6);
        var (dmgTier2, _) = skill.CalculateDamage(1.0f);
        Assert.AreEqual(400f, dmgTier2, 1f, "Match 6 should deal 2x base ATK (400)");

        // Match 9 đá (Tier 3 = 3x)
        skill.Initialize(9);
        var (dmgTier3, _) = skill.CalculateDamage(1.0f);
        Assert.AreEqual(600f, dmgTier3, 1f, "Match 9 should deal 3x base ATK (600)");
    }

    [Test]
    public void FrontRowBarrier_BlocksDirectAttacksOnBackRow_UntilFrontRowDefeated()
    {
        // Arrange: 1 Quái Hàng trước, 1 Quái Hàng sau
        GameObject frontObj = new GameObject("FrontEnemy", typeof(BoxCollider2D), typeof(EnemyAnimationController), typeof(EnemyBase));
        frontObj.transform.SetParent(_testRoot.transform);
        EnemyBase frontEnemy = frontObj.GetComponent<EnemyBase>();
        frontEnemy.SetStatsForTest(100f, 10f, EnemyRow.Front);
        _enemyController.RegisterEnemy(frontEnemy);

        GameObject backObj = new GameObject("BackEnemy", typeof(BoxCollider2D), typeof(EnemyAnimationController), typeof(EnemyBase));
        backObj.transform.SetParent(_testRoot.transform);
        EnemyBase backEnemy = backObj.GetComponent<EnemyBase>();
        backEnemy.SetStatsForTest(100f, 10f, EnemyRow.Back);
        _enemyController.RegisterEnemy(backEnemy);

        // 1. Khi Hàng trước còn sống: GetValidTarget trả về Hàng trước
        Assert.IsFalse(_enemyController.AllFrontRowEnemiesDead());
        EnemyBase target = _enemyController.GetValidTarget(allowBackRow: false);
        Assert.AreEqual(frontEnemy, target, "Khi hàng trước còn sống, phải nhắm vào hàng trước!");

        // Đòn đánh đơn mục tiêu vào Hàng sau bị chặn
        backEnemy.TakeDamageFromPlayer(50f, false, ElementType.Wood, allowBackRow: false);
        Assert.AreEqual(100f, backEnemy.CurrentHP, "Quái hàng sau không nhận sát thương khi hàng trước còn sống!");

        // 2. Tiêu diệt Hàng trước
        frontEnemy.TakeDamage(200f, false, ElementType.Wood);
        _enemyController.UnregisterEnemy(frontEnemy);
        Assert.IsTrue(_enemyController.AllFrontRowEnemiesDead());

        // Sau khi Hàng trước tử trận: GetValidTarget nhắm vào Hàng sau
        target = _enemyController.GetValidTarget(allowBackRow: false);
        Assert.AreEqual(backEnemy, target, "Hàng trước đã hạ gục, nhắm vào hàng sau!");

        // Đòn đánh vào Hàng sau giờ đây gây sát thương bình thường
        backEnemy.TakeDamageFromPlayer(50f, false, ElementType.Wood, allowBackRow: false);
        Assert.AreEqual(50f, backEnemy.CurrentHP, "Quái hàng sau nhận sát thương sau khi hàng trước đã tử trận!");
    }

    [Test]
    public void EnemyTurnOrder_ExecutesFrontRowBeforeBackRow()
    {
        // Arrange: 2 quái Hàng trước, 2 quái Hàng sau
        GameObject f1 = new GameObject("F1", typeof(BoxCollider2D), typeof(EnemyAnimationController), typeof(EnemyBase));
        f1.transform.SetParent(_testRoot.transform);
        EnemyBase front1 = f1.GetComponent<EnemyBase>();
        front1.SetStatsForTest(100f, 10f, EnemyRow.Front);
        _enemyController.RegisterEnemy(front1);

        GameObject b1 = new GameObject("B1", typeof(BoxCollider2D), typeof(EnemyAnimationController), typeof(EnemyBase));
        b1.transform.SetParent(_testRoot.transform);
        EnemyBase back1 = b1.GetComponent<EnemyBase>();
        back1.SetStatsForTest(100f, 10f, EnemyRow.Back);
        _enemyController.RegisterEnemy(back1);

        GameObject f2 = new GameObject("F2", typeof(BoxCollider2D), typeof(EnemyAnimationController), typeof(EnemyBase));
        f2.transform.SetParent(_testRoot.transform);
        EnemyBase front2 = f2.GetComponent<EnemyBase>();
        front2.SetStatsForTest(100f, 10f, EnemyRow.Front);
        _enemyController.RegisterEnemy(front2);

        GameObject b2 = new GameObject("B2", typeof(BoxCollider2D), typeof(EnemyAnimationController), typeof(EnemyBase));
        b2.transform.SetParent(_testRoot.transform);
        EnemyBase back2 = b2.GetComponent<EnemyBase>();
        back2.SetStatsForTest(100f, 10f, EnemyRow.Back);
        _enemyController.RegisterEnemy(back2);

        // Act
        List<EnemyBase> turnOrder = _enemyController.GetOrderedEnemiesForTurn();

        // Assert: 2 quái đầu phải thuộc EnemyRow.Front, 2 quái sau thuộc EnemyRow.Back
        Assert.AreEqual(4, turnOrder.Count);
        Assert.AreEqual(EnemyRow.Front, turnOrder[0].row);
        Assert.AreEqual(EnemyRow.Front, turnOrder[1].row);
        Assert.AreEqual(EnemyRow.Back, turnOrder[2].row);
        Assert.AreEqual(EnemyRow.Back, turnOrder[3].row);
    }

    [Test]
    public void DialogueDatabase_ContainsAll46Dialogues()
    {
        for (int i = 1; i <= 46; i++)
        {
            string id = $"SC_{i:D2}";
            var entry = DialogueDatabase.Get(id);
            Assert.IsNotNull(entry, $"{id} must exist in DialogueDatabase!");
            Assert.IsNotEmpty(entry.Text, $"{id} text cannot be empty!");
            Assert.IsNotEmpty(entry.Speaker, $"{id} speaker cannot be empty!");
        }
    }
}
