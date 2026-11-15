using NUnit.Framework;
using UnityEngine;

public class ElementalSystemTests
{
    [Test]
    public void ElementalCounter_KimBeatsMoc_Returns1Point5()
    {
        // Arrange
        ElementType attacker = ElementType.Metal;
        ElementType defender = ElementType.Wood;

        // Act
        float multiplier = ElementalHelper.GetMultiplier(attacker, defender);

        // Assert
        Assert.AreEqual(1.5f, multiplier, "Kim phải khắc Mộc với hệ số 1.5x!");
    }

    [Test]
    public void ElementalCounter_MocBeatsTho_Returns1Point5()
    {
        // Arrange & Act & Assert
        Assert.AreEqual(1.5f, ElementalHelper.GetMultiplier(ElementType.Wood, ElementType.Earth));
        Assert.AreEqual(1.5f, ElementalHelper.GetMultiplier(ElementType.Earth, ElementType.Water));
        Assert.AreEqual(1.5f, ElementalHelper.GetMultiplier(ElementType.Water, ElementType.Fire));
        Assert.AreEqual(1.5f, ElementalHelper.GetMultiplier(ElementType.Fire, ElementType.Metal));
    }

    [Test]
    public void ElementalCounter_Disadvantaged_Returns0Point8()
    {
        // Arrange & Act & Assert (Bị khắc thì giảm còn 0.8x)
        Assert.AreEqual(0.8f, ElementalHelper.GetMultiplier(ElementType.Wood, ElementType.Metal));
        Assert.AreEqual(0.8f, ElementalHelper.GetMultiplier(ElementType.Metal, ElementType.Fire));
        Assert.AreEqual(0.8f, ElementalHelper.GetMultiplier(ElementType.Fire, ElementType.Water));
    }

    [Test]
    public void Level1_BossHatNhamThach_StatsMatchGDD()
    {
        // Arrange
        EnemyConfig bossConfig = EnemyStatsDatabase.GetConfig(EnemyType.HatNhamThach);

        // Assert
        Assert.AreEqual(ElementType.Earth, bossConfig.Element);
        Assert.AreEqual(1200f, bossConfig.HP, "Theo GDD mới sheet Level Difficult Enemy&Game, HP Hạt Nham Thạch là 1200!");
        Assert.AreEqual(40f, bossConfig.ATK);
        Assert.AreEqual(120f, bossConfig.DEF);
        Assert.AreEqual(0.10f, bossConfig.Reflect, "Boss Hạt Nham Thạch phải phản 10% sát thương!");
        Assert.IsTrue(bossConfig.IsBoss);
    }

    [Test]
    public void Level1_10WavesData_ConfiguredProperly()
    {
        // Arrange - Đọc trực tiếp từ ScriptableObject LevelDataSO_lv_001
        LevelDataSO so = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelDataSO>("Assets/Project/Resources/ScriptObject/LevelData/LevelDataSO_lv_001.asset");
        Assert.IsNotNull(so, "LevelDataSO_lv_001 phải tồn tại!");
        LevelData lv1 = so.GetData();

        // Assert
        Assert.AreEqual(10, lv1.GetListStepData().Count, "Level 1 phải có đúng 10 Waves!");
        Assert.AreEqual(new Vector2Int(6, 6), lv1.GetSize(), "Kích thước bàn cờ Level 1 phải là 6x6 theo GDD!");
    }
}

