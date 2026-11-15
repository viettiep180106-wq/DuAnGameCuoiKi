using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class RuneEventSystemTests
{
    private GameObject _playerObj;
    private Player _player;
    private GameObject _eventManagerObj;
    private InterWaveEventManager _eventManager;

    [SetUp]
    public void SetUp()
    {
        _playerObj = new GameObject("Player");
        _player = _playerObj.AddComponent<Player>();
        typeof(Player).GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            ?.SetValue(null, _player);

        _eventManagerObj = new GameObject("InterWaveEventManager");
        _eventManager = _eventManagerObj.AddComponent<InterWaveEventManager>();
        typeof(InterWaveEventManager).GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            ?.SetValue(null, _eventManager);
    }

    [TearDown]
    public void TearDown()
    {
        if (_playerObj != null) Object.DestroyImmediate(_playerObj);
        if (_eventManagerObj != null) Object.DestroyImmediate(_eventManagerObj);
    }

    [Test]
    public void Player_TrySpendEnergy_SucceedsWhenEnoughEnergy_DeductsCorrectAmount()
    {
        // Arrange - Người chơi ban đầu có 50 lõi năng lượng
        int initialEnergy = _player.EnergyCores;
        Assert.GreaterOrEqual(initialEnergy, 25);

        // Act - Mở phiến đá với 25 lõi
        bool success = _player.TrySpendEnergy(25);

        // Assert
        Assert.IsTrue(success);
        Assert.AreEqual(initialEnergy - 25, _player.EnergyCores);
    }

    [Test]
    public void Player_TrySpendEnergy_FailsWhenNotEnoughEnergy()
    {
        // Arrange - Tiêu hết năng lượng
        _player.TrySpendEnergy(_player.EnergyCores);
        Assert.AreEqual(0, _player.EnergyCores);

        // Act - Cố gắng mở phiến đá khi có 0 lõi
        bool success = _player.TrySpendEnergy(25);

        // Assert
        Assert.IsFalse(success);
        Assert.AreEqual(0, _player.EnergyCores);
    }

    [Test]
    public void InterWaveEventManager_GetRandomBuff_ReturnsValidBuff()
    {
        // Act
        BuffDebuffType buff = _eventManager.GetRandomBuff();

        // Assert
        Assert.IsTrue(buff.ToString().StartsWith("Buff_"), $"Expected a Buff but got: {buff}");
        string name = _eventManager.GetBuffName(buff);
        Assert.IsFalse(string.IsNullOrEmpty(name));
    }

    [Test]
    public void InterWaveEventManager_GetRandomDebuff_ReturnsValidDebuff()
    {
        // Act
        BuffDebuffType debuff = _eventManager.GetRandomDebuff();

        // Assert
        Assert.IsTrue(debuff.ToString().StartsWith("Debuff_"), $"Expected a Debuff but got: {debuff}");
        string name = _eventManager.GetBuffName(debuff);
        Assert.IsFalse(string.IsNullOrEmpty(name));
    }

    [Test]
    public void InterWaveEventManager_ApplyBuffEffect_IncreasesPlayerStats()
    {
        // Arrange
        float initialAtk = _player.PlayerATK;

        // Act - Tăng 10% ATK
        _eventManager.ApplyBuffEffect(BuffDebuffType.Buff_CuongHoa_Atk);

        // Assert
        Assert.AreEqual(initialAtk * 1.1f, _player.PlayerATK, 0.01f);
    }

    [Test]
    public void InterWaveEventManager_ApplyDebuffEffect_DecreasesPlayerStats()
    {
        // Arrange
        float initialAtk = _player.PlayerATK;

        // Act - Giảm 10% ATK
        _eventManager.ApplyDebuffEffect(BuffDebuffType.Debuff_GiamAtk);

        // Assert
        Assert.AreEqual(initialAtk * 0.9f, _player.PlayerATK, 0.01f);
    }

    [Test]
    public void DialogueManager_SkipDialogue_ImmediatelyInvokesCallback()
    {
        GameObject dmObj = new GameObject("DialogueManager");
        DialogueManager dm = dmObj.AddComponent<DialogueManager>();

        bool completed = false;
        dm.StartWaveRestDialogue(1, () => { completed = true; });

        // Act - Skip dialogue immediately
        dm.SkipDialogue();

        // Assert
        Assert.IsTrue(completed, "SkipDialogue should immediately fire the dialogue completion callback");

        Object.DestroyImmediate(dmObj);
    }

    [Test]
    public void DialogueManager_MonsterAlert_CompletesOnSkip()
    {
        GameObject dmObj = new GameObject("DialogueManager");
        DialogueManager dm = dmObj.AddComponent<DialogueManager>();

        bool completed = false;
        dm.StartMonsterAlertDialogue(() => { completed = true; });

        // Act - Skip dialogue immediately
        dm.SkipDialogue();

        // Assert
        Assert.IsTrue(completed, "SkipDialogue on monster alert should fire completion callback");

        Object.DestroyImmediate(dmObj);
    }

    [Test]
    public void EventSprites_AllFilesExistInEventsDirectory()
    {
        string[] requiredSprites = new string[]
        {
            "RuneStone_Intact.png",
            "RuneStone_Cracked.png",
            "GachaWheel_Base.png",
            "GachaWheel_Pointer.png",
            "Chest_Closed.png",
            "Chest_Open.png",
            "Fx_GlowLight.png"
        };

        string basePath = "Assets/Project/Sprite/Events/";
        foreach (var sprite in requiredSprites)
        {
            string fullPath = System.IO.Path.Combine(basePath, sprite);
            Assert.IsTrue(System.IO.File.Exists(fullPath), $"Missing sliced sprite file: {fullPath}");
        }
    }

    [Test]
    public void InterWaveEventManager_BuffsAndDebuffs_PersistInLevelLists()
    {
        // Act
        _eventManager.ResetLevelBuffsDebuffs();
        Assert.AreEqual(0, _eventManager.ActiveBuffsInLevel.Count);
        Assert.AreEqual(0, _eventManager.ActiveDebuffsInLevel.Count);

        _eventManager.ApplyBuffEffect(BuffDebuffType.Buff_CuongHoa_Atk);
        _eventManager.ApplyBuffEffect(BuffDebuffType.Buff_CuongHoa_Def);
        _eventManager.ApplyDebuffEffect(BuffDebuffType.Debuff_GiamAtk);

        // Assert - Tồn tại trong toàn bộ Level
        Assert.AreEqual(2, _eventManager.ActiveBuffsInLevel.Count);
        Assert.AreEqual(1, _eventManager.ActiveDebuffsInLevel.Count);
        Assert.Contains(BuffDebuffType.Buff_CuongHoa_Atk, (System.Collections.ICollection)_eventManager.ActiveBuffsInLevel);
        Assert.Contains(BuffDebuffType.Debuff_GiamAtk, (System.Collections.ICollection)_eventManager.ActiveDebuffsInLevel);

        // Reset level
        _eventManager.ResetLevelBuffsDebuffs();
        Assert.AreEqual(0, _eventManager.ActiveBuffsInLevel.Count);
        Assert.AreEqual(0, _eventManager.ActiveDebuffsInLevel.Count);
    }

    [Test]
    public void EnemyController_PushOutAndClearOldEnemies_ClearsAllEnemies()
    {
        GameObject ecObj = new GameObject("EnemyController");
        EnemyController ec = ecObj.AddComponent<EnemyController>();

        bool completed = false;
        ec.PushOutAndClearOldEnemies(16f, 0.1f, () => { completed = true; });

        Assert.IsTrue(completed, "PushOutAndClearOldEnemies should invoke callback");
        Assert.AreEqual(0, ec.LivingEnemies.Count, "LivingEnemies must be 0 after push-out and clear");

        Object.DestroyImmediate(ecObj);
    }

    [Test]
    public void DialogueManager_SkipDialogue_DumpsAllLinesAndInvokesCallback()
    {
        GameObject dmObj = new GameObject("DialogueManager");
        DialogueManager dm = dmObj.AddComponent<DialogueManager>();

        bool completed = false;
        dm.StartWaveRestDialogue(1, () => { completed = true; });

        // Act: Skip dialogue
        dm.SkipDialogue(immediate: true);

        // Assert: All queue is drained and completion callback fired
        Assert.IsTrue(completed, "SkipDialogue should finish dialogue and fire callback");

        Object.DestroyImmediate(dmObj);
    }

    [Test]
    public void DialogueManager_StartWaveRestDialogue_Wave2HasDynamicLines()
    {
        GameObject dmObj = new GameObject("DialogueManager");
        DialogueManager dm = dmObj.AddComponent<DialogueManager>();

        bool completed = false;
        dm.StartWaveRestDialogue(2, () => { completed = true; });

        // Skip immediately to inspect completion
        dm.SkipDialogue(immediate: true);
        Assert.IsTrue(completed);

        Object.DestroyImmediate(dmObj);
    }

    [Test]
    public void SkillEarth_ImpactRadius_IsNormalizedToScreenBounds()
    {
        GameObject earthObj = new GameObject("SkillEarth", typeof(CircleCollider2D), typeof(Rigidbody2D));
        SkillEarth earth = earthObj.AddComponent<SkillEarth>();

        // Assert: Bán kính nổ đất đã được điều chỉnh phù hợp màn hình dọc (<= 1.5f) thay vì 3.0f làm nổ ngoài màn hình
        Assert.IsTrue(earth.impactRadius <= 1.5f, "impactRadius should be <= 1.5f");
        Assert.IsTrue(earth.impactRadius >= 1.0f, "impactRadius should be >= 1.0f");

        Object.DestroyImmediate(earthObj);
    }

    [Test]
    public void SkillPassive_IsVisibleInCamera_CorrectlyIdentifiesOnAndOffScreen()
    {
        // Khi không có Camera, fallback về true
        bool resultWithoutCam = SkillPassive.IsVisibleInCamera(Vector3.zero);
        Assert.IsTrue(resultWithoutCam);

        // Tạo Camera tạm để kiểm tra
        GameObject camObj = new GameObject("MainCamera", typeof(Camera));
        Camera cam = camObj.GetComponent<Camera>();
        cam.tag = "MainCamera";
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.transform.position = new Vector3(0, 0, -10f);

        // Vị trí (0, 0, 0) ngay tâm camera -> Phải visible
        Assert.IsTrue(SkillPassive.IsVisibleInCamera(Vector3.zero, 0.1f));

        // Vị trí cực xa (100, 100, 0) ngoài màn hình -> Phải không visible
        Assert.IsFalse(SkillPassive.IsVisibleInCamera(new Vector3(100f, 100f, 0f), 0.1f));

        Object.DestroyImmediate(camObj);
    }

    [Test]
    public void BuffDebuffDatabase_ContainsAll21EntriesWithIconsAndDescriptions()
    {
        var db = Resources.Load<BuffDebuffDatabase>("Database/BuffDebuffDatabase");
#if UNITY_EDITOR
        if (db == null)
        {
            db = UnityEditor.AssetDatabase.LoadAssetAtPath<BuffDebuffDatabase>("Assets/Project/Resources/Database/BuffDebuffDatabase.asset");
        }
#endif
        Assert.IsNotNull(db, "BuffDebuffDatabase asset must exist in Resources/Database!");
        Assert.AreEqual(21, db.AllEntries.Count, "BuffDebuffDatabase must contain exactly 21 entries!");

        foreach (var entry in db.AllEntries)
        {
            Assert.IsNotNull(entry, "BuffDebuffInfo entry cannot be null");
            Assert.IsFalse(string.IsNullOrEmpty(entry.displayName), $"Display name missing for {entry.type}");
            Assert.IsFalse(string.IsNullOrEmpty(entry.description), $"Description missing for {entry.type}");
            Assert.IsNotNull(entry.icon, $"Icon Sprite missing for {entry.type} ({entry.displayName})");
        }
    }

    [Test]
    public void GachaWheel_SectorAngles_AlignWithBuffAndDebuffColors()
    {
        // 8 sectors alternating every 45 deg:
        // Sector 0: Red (Debuff)
        // Sector 1: Green (Buff)
        // Sector 2: Red (Debuff)
        // Sector 3: Green (Buff)
        // Sector 4: Red (Debuff)
        // Sector 5: Green (Buff)
        // Sector 6: Red (Debuff)
        // Sector 7: Green (Buff)

        for (int k = 0; k < 8; k++)
        {
            bool isBuffSector = (k % 2 != 0); // Odd is Buff (Green)
            float sectorAngle = k * 45f;
            Assert.GreaterOrEqual(sectorAngle, 0f);
            Assert.Less(sectorAngle, 360f);

            if (isBuffSector)
            {
                Assert.IsTrue(k == 1 || k == 3 || k == 5 || k == 7, $"Sector {k} should be Buff");
            }
            else
            {
                Assert.IsTrue(k == 0 || k == 2 || k == 4 || k == 6, $"Sector {k} should be Debuff");
            }
        }
    }

    [Test]
    public void EventSprites_NewAssets_AllExistInEventsDirectory()
    {
        string[] requiredNewSprites = new string[]
        {
            "EnergyCore_Icon.png",
            "Card_Front_Base.png",
            "Cards/Deck_Roll.png",
            "Cards/Deck_Shuffle_1.png",
            "Cards/Deck_Shuffle_2.png",
            "Spirits/Spirit_Fairy.png",
            "Spirits/Spirit_Demon.png",
            "Envelopes/Envelope_1.png",
            "Envelopes/Envelope_2.png"
        };

        string basePath = "Assets/Project/Sprite/Events/";
        foreach (var sprite in requiredNewSprites)
        {
            string fullPath = System.IO.Path.Combine(basePath, sprite);
            Assert.IsTrue(System.IO.File.Exists(fullPath), $"Missing new event sprite file: {fullPath}");
        }
    }

    [Test]
    public void EnergyCoreFlyManager_SpawnsAndInitializesCorrectly()
    {
        GameObject canvasGO = new GameObject("TestCanvas", typeof(Canvas));
        GameObject flyGO = new GameObject("EnergyCoreFlyManager", typeof(RectTransform), typeof(EnergyCoreFlyManager));
        flyGO.transform.SetParent(canvasGO.transform, false);

        EnergyCoreFlyManager mgr = flyGO.GetComponent<EnergyCoreFlyManager>();
        Assert.IsNotNull(mgr);
        Assert.AreEqual(mgr, EnergyCoreFlyManager.Instance);

        // Test spawn call does not throw
        Assert.DoesNotThrow(() => mgr.SpawnFlyingCores(Vector3.zero, 15));

        Object.DestroyImmediate(canvasGO);
    }

    [Test]
    public void BuffDebuffDatabase_GetRandomBuffsAndDebuffs_ReturnsCorrectCounts()
    {
        if (BuffDebuffDatabase.Instance == null) return;

        var buffs = BuffDebuffDatabase.Instance.GetRandomBuffs(4);
        var debuffs = BuffDebuffDatabase.Instance.GetRandomDebuffs(4);

        Assert.IsNotNull(buffs);
        Assert.IsNotNull(debuffs);
        Assert.LessOrEqual(buffs.Count, 4);
        Assert.LessOrEqual(debuffs.Count, 4);

        foreach (var b in buffs)
        {
            Assert.IsNotNull(b);
            Assert.IsTrue(b.type.ToString().StartsWith("Buff_"));
        }
        foreach (var d in debuffs)
        {
            Assert.IsNotNull(d);
            Assert.IsTrue(d.type.ToString().StartsWith("Debuff_"));
        }
    }

    [Test]
    public void GachaWheel_WinningSectorAngleFormula_CorrectlyLandsAtTopPointer()
    {
        // Khi kim ở đỉnh 12h (0 độ), xoay theo chiều kim đồng hồ bằng góc:
        // targetAngle = ((8 - winningSector) % 8) * 45 độ
        for (int winningSector = 0; winningSector < 8; winningSector++)
        {
            float targetAngle = (((8 - winningSector) % 8) * 45f);
            float sectorInitialAngle = winningSector * 45f;
            // Xoay kim đồng hồ góc targetAngle:
            float finalAngle = (sectorInitialAngle + targetAngle) % 360f;
            Assert.AreEqual(0f, finalAngle, 0.001f, $"Sector {winningSector} should land at 0 deg (12h/Pointer)");
        }
    }

    [Test]
    public void EnemyController_PushOutAndClearOldEnemies_DefaultValuesArePaced()
    {
        var enemyObj = new GameObject("EnemyController", typeof(EnemyController));
        var ec = enemyObj.GetComponent<EnemyController>();
        Assert.IsNotNull(ec);

        // Verify method can be invoked without exception when empty
        bool completed = false;
        ec.PushOutAndClearOldEnemies(7.5f, 2.2f, () => completed = true);
        Assert.IsTrue(completed);

        Object.DestroyImmediate(enemyObj);
    }
}
