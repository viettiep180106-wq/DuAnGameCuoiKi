using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class CombatCameraAndFeelTests
{
    private GameObject _testRoot;
    private Camera _testCamera;
    private CombatCameraController _cameraController;
    private Player _player;
    private Player _previousPlayer;
    private CombatTurnManager _turnManager;
    private SkillPassiveManager _skillManager;

    [SetUp]
    public void SetUp()
    {
        _testRoot = new GameObject("TestRoot_CombatCameraAndFeel");

        GameObject camObj = new GameObject("TestCamera");
        camObj.transform.SetParent(_testRoot.transform);
        camObj.transform.position = new Vector3(-5.693f, -3.437f, -10f);
        _testCamera = camObj.AddComponent<Camera>();
        _testCamera.orthographic = true;
        _testCamera.orthographicSize = 49.11628f;
        _testCamera.aspect = 9f / 16f;

        _cameraController = camObj.AddComponent<CombatCameraController>();
        _cameraController.Initialize(_testCamera, new Vector3(-5.693f, -3.437f, -10f), -5.693f);

        _previousPlayer = Player.Instance;
        GameObject playerObj = new GameObject("TestPlayer", typeof(BoxCollider2D), typeof(PlayerAnimationController), typeof(Player));
        playerObj.transform.SetParent(_testRoot.transform);
        _player = playerObj.GetComponent<Player>();
        _player.SetStatsForTest(500f, 200f, 50);
        Player.SetInstanceForTest(_player);

        GameObject turnObj = new GameObject("TestCombatTurnManager", typeof(CombatTurnManager));
        turnObj.transform.SetParent(_testRoot.transform);
        _turnManager = turnObj.GetComponent<CombatTurnManager>();
        _turnManager.StartCombatWave();

        GameObject spmObj = new GameObject("TestSkillPassiveManager", typeof(SkillPassiveManager));
        spmObj.transform.SetParent(_testRoot.transform);
        _skillManager = spmObj.GetComponent<SkillPassiveManager>();
    }

    [TearDown]
    public void TearDown()
    {
        Player.SetInstanceForTest(_previousPlayer);
        if (_testRoot != null)
        {
            Object.DestroyImmediate(_testRoot);
        }
    }

    private EnemyBase CreateEnemy(EnemyType enemyType, bool dynamicBoss = false)
    {
        GameObject obj = new GameObject($"Enemy_{enemyType}", typeof(BoxCollider2D), typeof(EnemyAnimationController), typeof(EnemyBase));
        obj.transform.SetParent(_testRoot.transform);
        EnemyBase enemy = obj.GetComponent<EnemyBase>();
        enemy.type = enemyType;
        enemy.isBoss = dynamicBoss;
        return enemy;
    }

    [Test]
    public void FinalBoss_OffensiveActiveSkills_ReturnHeavyShakeTier()
    {
        EnemyType[] offensiveFinalBosses =
        {
            EnemyType.HoaThanDietVong,
            EnemyType.CoMayVinhCuu,
            EnemyType.BossDatNung,
            EnemyType.ThuyThanSaNga
        };

        foreach (EnemyType bossType in offensiveFinalBosses)
        {
            EnemyBase boss = CreateEnemy(bossType);
            Assert.IsTrue(boss.IsFinalBoss, $"{bossType} phải được nhận diện là FinalBoss qua EnemyDataSO.category.");
            Assert.IsTrue(boss.IsOffensiveActiveSkill(), $"{bossType} là chiêu active tấn công Player.");
            Assert.AreEqual(CameraShakeTier.Heavy, boss.GetSkillShakeTier(isSkill: true),
                $"{bossType} khi dùng active skill tấn công phải trả về CameraShakeTier.Heavy.");
        }
    }

    [Test]
    public void FinalBoss_SupportActiveSkill_CoThuNganNam_ReturnsNoneShakeTier()
    {
        EnemyBase treeBoss = CreateEnemy(EnemyType.CoThuNganNam);

        Assert.IsTrue(treeBoss.IsFinalBoss);
        Assert.IsFalse(treeBoss.IsOffensiveActiveSkill(), "Cổ Thụ Ngàn Năm dùng chiêu hồi sinh/khiên đội hình, không gây damage lên Player.");
        Assert.AreEqual(CameraShakeTier.None, treeBoss.GetSkillShakeTier(isSkill: true));
    }

    [Test]
    public void NonFinalBoss_Enemies_ReturnLightShakeTier_EvenWhenIsBossFlagSetOnLastWave()
    {
        // Quái thường đặt ở Wave cuối bị EnemyController gắn enemy.isBoss = true -> KHÔNG được rung Heavy như FinalBoss
        EnemyBase normalInBossWave = CreateEnemy(EnemyType.ChoSanDiaNguc, dynamicBoss: true);
        Assert.IsFalse(normalInBossWave.IsFinalBoss);
        Assert.IsTrue(normalInBossWave.IsOffensiveActiveSkill());
        Assert.AreEqual(CameraShakeTier.Light, normalInBossWave.GetSkillShakeTier(isSkill: true));

        // WaveBoss cũng chỉ rung Light
        EnemyBase waveBoss = CreateEnemy(EnemyType.ChienBinhDungNham);
        Assert.IsFalse(waveBoss.IsFinalBoss);
        Assert.AreEqual(CameraShakeTier.Light, waveBoss.GetSkillShakeTier(isSkill: true));

        EnemyBase rangedNormal = CreateEnemy(EnemyType.TinhLinhLua);
        Assert.AreEqual(CameraShakeTier.Light, rangedNormal.GetSkillShakeTier(isSkill: true));

        // Quái phòng thủ / hỗ trợ -> Không rung
        Assert.AreEqual(CameraShakeTier.None, CreateEnemy(EnemyType.GolemThep).GetSkillShakeTier(isSkill: true));
        Assert.AreEqual(CameraShakeTier.None, CreateEnemy(EnemyType.HatNhamThach).GetSkillShakeTier(isSkill: true));
        Assert.AreEqual(CameraShakeTier.None, CreateEnemy(EnemyType.TinhLinhHoiMau).GetSkillShakeTier(isSkill: true));
    }

    [Test]
    public void NormalAttack_NeverTriggersHeavyShake_EvenForFinalBoss()
    {
        EnemyBase boss = CreateEnemy(EnemyType.HoaThanDietVong);
        Assert.AreEqual(CameraShakeTier.Light, boss.GetSkillShakeTier(isSkill: false));
    }

    [Test]
    public void EnemyZoomScope_FiltersActiveSkillsCorrectly()
    {
        EnemyBase finalBoss = CreateEnemy(EnemyType.BossDatNung);
        EnemyBase normalAttackEnemy = CreateEnemy(EnemyType.TinhLinhLua);
        EnemyBase supportBoss = CreateEnemy(EnemyType.CoThuNganNam);

        // Mặc định AllActiveSkills: mọi chiêu active đều zoom, đánh thường không zoom
        Assert.IsFalse(finalBoss.ShouldZoomOnActiveSkill(false));
        Assert.IsTrue(finalBoss.ShouldZoomOnActiveSkill(true));
        Assert.IsTrue(normalAttackEnemy.ShouldZoomOnActiveSkill(true));
        Assert.IsTrue(supportBoss.ShouldZoomOnActiveSkill(true));

        // Đổi Scope sang FinalBossOnly
        var scopeField = typeof(CombatCameraController).GetField("enemyZoomScope", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(scopeField);
        scopeField.SetValue(_cameraController, EnemyZoomScope.FinalBossOnly);

        Assert.IsTrue(finalBoss.ShouldZoomOnActiveSkill(true));
        Assert.IsFalse(normalAttackEnemy.ShouldZoomOnActiveSkill(true));

        // Đổi Scope sang OffensiveActiveOnly
        scopeField.SetValue(_cameraController, EnemyZoomScope.OffensiveActiveOnly);
        Assert.IsTrue(normalAttackEnemy.ShouldZoomOnActiveSkill(true));
        Assert.IsFalse(supportBoss.ShouldZoomOnActiveSkill(true));
    }

    [Test]
    public void CombatCameraController_FocusOffsetAndReset_PreservesCameraState()
    {
        Vector3 playerWorldPos = new Vector3(-20f, 6.8f, 990f);
        float targetOrtho = 49.11628f * 0.58f;

        Vector3 focusPos = _cameraController.ComputeClampedFocusPosition(playerWorldPos, targetOrtho);
        Assert.Less(focusPos.y, playerWorldPos.y, "Tâm camera zoom phải nằm thấp hơn nhân vật (focusYOffset < 0) để chừa khoảng trống cho bàn cờ 6x6.");

        _cameraController.ResetImmediate(toCombatRest: true);
        Assert.AreEqual(49.11628f, _cameraController.CurrentOrthoSize, 0.001f);
        Assert.AreEqual(49.11628f, _testCamera.orthographicSize, 0.001f);
        Assert.AreEqual(Vector3.zero, _cameraController.CurrentShakeOffset);
        Assert.IsFalse(_cameraController.IsZooming);
    }

    [Test]
    public void SkillPassiveManager_PrepareMatchAttacks_CountsSingleAndCombinedMatches()
    {
        List<List<SlotStoneBase>> grid = BuildTestGrid(6, 6);

        // Trường hợp 1: Match 6 viên cùng 1 loại (Fire)
        var singleTypeMatch = new HashSet<Vector2Int>();
        for (int x = 0; x < 6; x++)
        {
            grid[x][0].GetStone().nameStone = TypeStone.Fire;
            singleTypeMatch.Add(new Vector2Int(x, 0));
        }

        var batch = new List<SkillPassiveManager.QueuedMatchAttack>();
        int countSingle = _skillManager.PrepareMatchAttacks(singleTypeMatch, grid, batch);
        Assert.AreEqual(6, countSingle);
        Assert.AreEqual(1, batch.Count);
        Assert.AreEqual(TypeStone.Fire, batch[0].StoneType);
        Assert.AreEqual(6, batch[0].MatchCount);

        // Trường hợp 2: Match >= 6 từ 2 loại (3 Fire + 3 Ice = 6)
        var multiTypeMatch = new HashSet<Vector2Int>();
        for (int x = 0; x < 3; x++)
        {
            grid[x][1].GetStone().nameStone = TypeStone.Fire;
            multiTypeMatch.Add(new Vector2Int(x, 1));

            grid[x][2].GetStone().nameStone = TypeStone.Ice;
            multiTypeMatch.Add(new Vector2Int(x, 2));
        }

        int countMulti = _skillManager.PrepareMatchAttacks(multiTypeMatch, grid, batch);
        Assert.AreEqual(6, countMulti);
        Assert.AreEqual(2, batch.Count);

        // Trường hợp 3: Match < 6 (chỉ 3 viên Wood)
        var smallMatch = new HashSet<Vector2Int>();
        for (int x = 0; x < 3; x++)
        {
            grid[x][3].GetStone().nameStone = TypeStone.Wood;
            smallMatch.Add(new Vector2Int(x, 3));
        }

        int countSmall = _skillManager.PrepareMatchAttacks(smallMatch, grid, batch);
        Assert.AreEqual(3, countSmall);
        Assert.AreEqual(1, batch.Count);
    }

    [Test]
    public void EnemyProjectile_Launch_TracksShakeTierAndResetsOnDespawn()
    {
        GameObject projObj = new GameObject("TestProjectile", typeof(CircleCollider2D), typeof(Rigidbody2D), typeof(EnemyProjectile));
        projObj.transform.SetParent(_testRoot.transform);
        EnemyProjectile proj = projObj.GetComponent<EnemyProjectile>();

        proj.Launch(100f, ElementType.Fire, Vector3.zero, Vector3.left * 5f, 20f, 0f, false, CameraShakeTier.Heavy);

        var shakeField = typeof(EnemyProjectile).GetField("_shakeTier", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(shakeField);
        Assert.AreEqual(CameraShakeTier.Heavy, (CameraShakeTier)shakeField.GetValue(proj));

        proj.OnDespawn();
        Assert.AreEqual(CameraShakeTier.None, (CameraShakeTier)shakeField.GetValue(proj));
    }

    private List<List<SlotStoneBase>> BuildTestGrid(int cols, int rows)
    {
        var grid = new List<List<SlotStoneBase>>(cols);
        for (int x = 0; x < cols; x++)
        {
            var colList = new List<SlotStoneBase>(rows);
            for (int y = 0; y < rows; y++)
            {
                GameObject slotObj = new GameObject($"Slot_{x}_{y}", typeof(RectTransform), typeof(SlotStoneBase));
                slotObj.transform.SetParent(_testRoot.transform);
                SlotStoneBase slot = slotObj.GetComponent<SlotStoneBase>();

                GameObject stoneObj = new GameObject($"Stone_{x}_{y}", typeof(RectTransform), typeof(StoneBase));
                stoneObj.transform.SetParent(slotObj.transform);
                StoneBase stone = stoneObj.GetComponent<StoneBase>();
                stone.nameStone = TypeStone.Wood;

                slot.SetStone(stone);
                colList.Add(slot);
            }
            grid.Add(colList);
        }
        return grid;
    }
}
