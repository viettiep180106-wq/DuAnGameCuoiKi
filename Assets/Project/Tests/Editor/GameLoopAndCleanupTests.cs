using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class GameLoopAndCleanupTests
{
    private GameObject _playerObj;
    private Player _player;

    [SetUp]
    public void SetUp()
    {
        _playerObj = new GameObject("Player");
        _playerObj.AddComponent<BoxCollider2D>();
        var sr = _playerObj.AddComponent<SpriteRenderer>();
        _player = _playerObj.AddComponent<Player>();
        _player.spriteRenderer = sr;
        Player.SetInstanceForTest(_player);
    }

    [TearDown]
    public void TearDown()
    {
        Player.SetInstanceForTest(null);
        if (_playerObj != null)
        {
            Object.DestroyImmediate(_playerObj);
        }
    }

    [Test]
    public void Test_Player_FacesRight_TowardsEnemies()
    {
        // Act: Đặt vị trí Combat Position (rìa trái)
        _player.AlignToCombatPosition();

        var sr = _player.GetComponent<SpriteRenderer>();
        Assert.IsNotNull(sr);
        Assert.IsTrue(sr.flipX, "Player sprite gốc quay trái -> flipX PHẢI bằng true để quay mặt sang PHẢI về phía quái");
        Assert.AreEqual(new Vector3(-1.35f, -0.7f, 0f), _player.transform.localPosition, "Vị trí chiến đấu của Player phải ở bên trong khung hình (-1.35, -0.7)");

        // Act: Đặt vị trí Marching Position (ở giữa)
        _player.AlignToMarchingPosition(0f);
        Assert.IsTrue(sr.flipX, "Trong giai đoạn di chuyển, Player vẫn phải quay mặt sang PHẢI theo hướng hành tiến");
        Assert.AreEqual(new Vector3(0f, -0.7f, 0f), _player.transform.localPosition, "Vị trí hành tiến của Player phải ở tâm (0, -0.7)");
    }

    [Test]
    public void Test_CombatBackgroundScroller_StopsDuringCombat_OnlyScrollsDuringMarching()
    {
        // Arrange
        GameObject bgObj = new GameObject("Background");
        var scroller = bgObj.AddComponent<CombatBackgroundScroller>();

        // Act & Assert 1: Mặc định khi chưa kích hoạt Marching -> KHÔNG cuộn nền
        Assert.IsFalse(scroller.ShouldScroll(), "Khi không trong giai đoạn Marching, nền phải đứng yên hoàn toàn");

        // Act & Assert 2: Bật Marching -> Cuộn nền
        scroller.StartMarching();
        Assert.IsTrue(scroller.ShouldScroll(), "Khi bắt đầu hành tiến (Marching), nền phải bắt đầu cuộn");

        // Act & Assert 3: Dừng Marching -> Đứng yên
        scroller.StopMarching();
        Assert.IsFalse(scroller.ShouldScroll(), "Khi kết thúc hành tiến, nền phải dừng cuộn lập tức");

        Object.DestroyImmediate(bgObj);
    }

    [Test]
    public void Test_StepData_NoLongerStoresDistanceOrHeight_CleanLevelData()
    {
        // Arrange: Tạo StepData theo constructor mới chuẩn hóa
        var enemyList = new List<Enemy>
        {
            new Enemy(EnemyType.SlimeBun, 2, EnemyRow.Front),
            new Enemy(EnemyType.BoCapDa, 1, EnemyRow.Back)
        };

        var step = new StepData(1, WaveType.Combat, StepDifficulty.Easy, FormationType.FullSingleElement,
            enemyList, StepEventType.None, 18);

        // Assert
        Assert.AreEqual(1, step.GetStep());
        Assert.AreEqual(WaveType.Combat, step.GetWaveType());
        Assert.AreEqual(18, step.GetWaveEnergyReward());
        Assert.AreEqual(2, step.GetListEnemy().Count);
        Assert.AreEqual(EnemyRow.Front, step.GetListEnemy()[0].GetRow());
        Assert.AreEqual(EnemyRow.Back, step.GetListEnemy()[1].GetRow());
    }

    [Test]
    public void Test_EnergyCoreFlyManager_CreditsPlayerDirectly_WithoutSpawningObjects()
    {
        // Arrange
        GameObject mgrObj = new GameObject("EnergyCoreFlyManager", typeof(RectTransform), typeof(EnergyCoreFlyManager));
        var mgr = mgrObj.GetComponent<EnergyCoreFlyManager>();
        _player.SetStatsForTest(500f, 200f, 50);

        int initialChildCount = mgrObj.transform.childCount;

        // Act: Thưởng 20 lõi
        mgr.SpawnFlyingCores(Vector3.zero, 20);

        // Assert: Năng lượng được cộng thẳng vào Player, không sinh thêm object con
        Assert.AreEqual(70, _player.EnergyCores, "Năng lượng phải được cộng trực tiếp vào Player (50 + 20 = 70)");
        Assert.AreEqual(initialChildCount, mgrObj.transform.childCount, "Không được sinh ra GameObject UI con gây rác màn hình");

        Object.DestroyImmediate(mgrObj);
    }

    [Test]
    public void Test_BoardController_TargetSlideInY_IsPositionedBelowStatsBar()
    {
        // Arrange
        GameObject ctrlObj = new GameObject("Controller", typeof(RectTransform));
        GameObject boardObj = new GameObject("Board", typeof(RectTransform));
        boardObj.transform.SetParent(ctrlObj.transform, false);

        var bc = ctrlObj.AddComponent<BoardController>();
        bc.center = boardObj.GetComponent<RectTransform>();

        // Assert: Target Y phải chính xác là -540f (ngay dưới PlayerStatsBar)
        Assert.AreEqual(-540f, bc.GetTargetSlideInY(), "Vị trí đích khi trượt bàn cờ lên phải là y = -540f");

        Object.DestroyImmediate(ctrlObj);
    }
}
