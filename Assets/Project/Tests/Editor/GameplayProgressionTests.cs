using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class GameplayProgressionTests
{
    [Test]
    public void DifficultyMultiplier_MatchesGDD()
    {
        Assert.AreEqual(1.2f, EnemyStatsDatabase.GetDifficultyMultiplier(StepDifficulty.Easy));
        Assert.AreEqual(1.35f, EnemyStatsDatabase.GetDifficultyMultiplier(StepDifficulty.Normal));
        Assert.AreEqual(1.5f, EnemyStatsDatabase.GetDifficultyMultiplier(StepDifficulty.Hard));
        Assert.AreEqual(1.5f, EnemyStatsDatabase.GetDifficultyMultiplier(StepDifficulty.VeryHard));
    }

    [Test]
    public void ScaleFormula_StrictlyFollowsGDD_PowerFormula()
    {
        // Formula: Scale = Multiplier^n, where n = (Level - 1) / 15
        
        // Level 1: n = 0 -> Scale = 1.0
        var cfgLv1 = EnemyStatsDatabase.GetScaledConfig(EnemyType.SlimeBun, 1, StepDifficulty.Easy);
        var baseCfg = EnemyStatsDatabase.GetConfig(EnemyType.SlimeBun);
        Assert.AreEqual(baseCfg.HP, cfgLv1.HP, 0.01f);
        Assert.AreEqual(baseCfg.ATK, cfgLv1.ATK, 0.01f);
        Assert.AreEqual(baseCfg.DEF, cfgLv1.DEF, 0.01f);

        // Level 16: n = 1 -> Scale = 1.2 (Easy)
        var cfgLv16Easy = EnemyStatsDatabase.GetScaledConfig(EnemyType.SlimeBun, 16, StepDifficulty.Easy);
        Assert.AreEqual(baseCfg.HP * 1.2f, cfgLv16Easy.HP, 0.1f);
        Assert.AreEqual(baseCfg.ATK * 1.2f, cfgLv16Easy.ATK, 0.1f);

        // Level 16: n = 1 -> Scale = 1.35 (Normal)
        var cfgLv16Norm = EnemyStatsDatabase.GetScaledConfig(EnemyType.SlimeBun, 16, StepDifficulty.Normal);
        Assert.AreEqual(baseCfg.HP * 1.35f, cfgLv16Norm.HP, 0.1f);

        // Level 31: n = 2 -> Scale = 1.2^2 = 1.44 (Easy)
        var cfgLv31Easy = EnemyStatsDatabase.GetScaledConfig(EnemyType.SlimeBun, 31, StepDifficulty.Easy);
        Assert.AreEqual(baseCfg.HP * 1.44f, cfgLv31Easy.HP, 0.1f);

        // Level 31: n = 2 -> Scale = 1.5^2 = 2.25 (Hard)
        var cfgLv31Hard = EnemyStatsDatabase.GetScaledConfig(EnemyType.SlimeBun, 31, StepDifficulty.Hard);
        Assert.AreEqual(baseCfg.HP * 2.25f, cfgLv31Hard.HP, 0.1f);
    }

    [Test]
    public void All35Monsters_AreConfiguredWithValidStatsAndRoles()
    {
        var allTypes = System.Enum.GetValues(typeof(EnemyType));
        int customCount = 0;

        foreach (EnemyType type in allTypes)
        {
            if (type == EnemyType.Enemy_1 || type == EnemyType.Enemy_2) continue;

            customCount++;
            var cfg = EnemyStatsDatabase.GetConfig(type);
            Assert.Greater(cfg.HP, 0f, $"{type} must have HP > 0");
            Assert.Greater(cfg.ATK, 0f, $"{type} must have ATK > 0");
            Assert.GreaterOrEqual(cfg.DEF, 0f, $"{type} must have DEF >= 0");
            Assert.Greater(cfg.Scale, 0f, $"{type} must have Scale > 0");
        }

        Assert.AreEqual(35, customCount, "Hệ thống phải có đủ chính xác 35 quái vật theo 5 hệ nguyên tố!");
    }

    [Test]
    public void Tank_DamageReductionAndShield_AbsorbDamageAccurately()
    {
        GameObject go = new GameObject("TestTank", typeof(EnemyBase), typeof(EnemyAnimationController), typeof(BoxCollider2D));
        EnemyBase tank = go.GetComponent<EnemyBase>();
        tank.SetStatsForTest(1000f, 50f, EnemyRow.Front);

        // Giảm ST 40%
        tank.damageReductionPercent = 0.40f;
        // Đòn đánh thô 100 dmg -> Giảm còn 60 dmg
        tank.TakeDamage(100f, false, ElementType.Wood);
        Assert.AreEqual(940f, tank.CurrentHP, 0.01f, "Giảm 40% sát thương: 100 dmg -> nhận 60 dmg");

        // Khiên ảo 100 máu
        tank.currentShield = 100f;
        tank.damageReductionPercent = 0f;
        // Đòn đánh 60 dmg -> Khiên hấp thụ hoàn toàn, máu không đổi
        tank.TakeDamage(60f, false, ElementType.Wood);
        Assert.AreEqual(940f, tank.CurrentHP, 0.01f, "Khiên ảo 100 hấp thụ toàn bộ 60 dmg, HP giữ nguyên");
        Assert.AreEqual(40f, tank.currentShield, 0.01f, "Khiên ảo còn lại 40");

        // Đòn đánh 60 dmg tiếp theo -> Khiên vỡ 40, 20 dmg trừ vào HP
        tank.TakeDamage(60f, false, ElementType.Wood);
        Assert.AreEqual(920f, tank.CurrentHP, 0.01f, "Khiên vỡ, 20 dmg thừa xuyên vào HP");
        Assert.AreEqual(0f, tank.currentShield, 0.01f, "Khiên ảo về 0");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Support_ReviveAlly_RestoresDeadAllyWithSpecifiedHp()
    {
        GameObject ctrlObj = new GameObject("EnemyController", typeof(EnemyController));
        EnemyController ec = ctrlObj.GetComponent<EnemyController>();

        GameObject allyObj = new GameObject("DeadAlly", typeof(EnemyBase), typeof(EnemyAnimationController), typeof(BoxCollider2D));
        EnemyBase ally = allyObj.GetComponent<EnemyBase>();
        ally.SetStatsForTest(500f, 30f, EnemyRow.Front);
        ally.Element = ElementType.Wood;
        ally.TakeDamage(600f, false, ElementType.Metal); // Chết
        Assert.LessOrEqual(ally.CurrentHP, 0f);

        ec.RecordDeadEnemy(ally);
        Assert.AreEqual(1, ec.DeadEnemiesInWave.Count);

        // Hồi sinh quái hệ Mộc với 30% HP
        bool revived = ec.TryReviveDeadAlly(ElementType.Wood, 0.30f);
        Assert.IsTrue(revived, "Phải hồi sinh thành công quái hệ Mộc");
        Assert.AreEqual(150f, ally.CurrentHP, 0.01f, "500 * 0.3 = 150 HP");
        Assert.IsTrue(ally.isRevived, "Phải đánh dấu isRevived = true");
        Assert.AreEqual(0, ec.DeadEnemiesInWave.Count, "Quái đã hồi sinh phải được xóa khỏi danh sách tử trận");

        Object.DestroyImmediate(allyObj);
        Object.DestroyImmediate(ctrlObj);
    }

    [Test]
    public void All6VFXTemplates_ExistInResources()
    {
        string[] templateNames = new string[]
        {
            "VFX_Template_Fire",
            "VFX_Template_Water",
            "VFX_Template_Wood",
            "VFX_Template_Metal",
            "VFX_Template_Earth",
            "VFX_Template_ActiveSkill"
        };

        foreach (string name in templateNames)
        {
            GameObject prefab = Resources.Load<GameObject>($"Prefab/VFX/{name}");
            Assert.IsNotNull(prefab, $"Prefab/VFX/{name} must exist in Resources");
            var ps = prefab.GetComponent<ParticleSystem>();
            Assert.IsNotNull(ps, $"{name} must have ParticleSystem component");
        }
    }
}
