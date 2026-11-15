using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

[TestFixture]
public class LevelDataSystemTests
{
    private LevelDataSO _levelSO;

    [SetUp]
    public void SetUp()
    {
        _levelSO = AssetDatabase.LoadAssetAtPath<LevelDataSO>("Assets/Project/Resources/ScriptObject/LevelData/LevelDataSO_lv_001.asset");
    }

    [Test]
    public void LevelDataSO_IsLoaded_NotNull()
    {
        Assert.IsNotNull(_levelSO, "LevelDataSO_lv_001.asset should exist at Assets/Project/Resources/ScriptObject/LevelData/LevelDataSO_lv_001.asset");
        Assert.IsNotNull(_levelSO.GetData(), "LevelData should not be null");
    }

    [Test]
    public void LevelDataSO_HasFixed6x6BoardSize()
    {
        Assert.AreEqual(new Vector2Int(6, 6), _levelSO.GetData().GetSize(), "Kích thước bàn cờ Level 1 phải chuẩn 6x6 theo GDD");
    }

    [Test]
    public void LevelDataSO_HasExact10Waves()
    {
        var steps = _levelSO.GetData().GetListStepData();
        Assert.AreEqual(10, steps.Count, "Level 1 should have exactly 10 waves configured in LevelDataSO_lv_001");
    }

    [Test]
    public void StepData_HasTargetTypeStones_AlwaysContainsCounterStone()
    {
        var steps = _levelSO.GetData().GetListStepData();
        foreach (var step in steps)
        {
            var targets = step.GetTargetTypeStones();
            Assert.IsNotNull(targets, "Target stones should not be null");
            Assert.GreaterOrEqual(targets.Count, 1, "Each wave should have at least 1 target stone");

            TypeStone requiredCounter = StepData.GetCounterStoneForElement(step.GetEnemyElement());
            Assert.IsTrue(targets.Contains(requiredCounter), $"Wave {step.GetStep()} (Hệ {step.GetEnemyElement()}) MUST contain counter stone {requiredCounter}");
        }
    }

    [Test]
    public void StepData_CounterStone_StrictlyFollowsNguHanhGDD()
    {
        // Ngũ Hành khắc chế:
        // Hỏa khắc Kim -> Quái Hỏa bị Thủy khắc (Đá Ice/Water)
        Assert.AreEqual(TypeStone.Ice, StepData.GetCounterStoneForElement(ElementType.Fire));

        // Kim khắc Mộc -> Quái Kim bị Hỏa khắc (Đá Fire)
        Assert.AreEqual(TypeStone.Fire, StepData.GetCounterStoneForElement(ElementType.Metal));

        // Mộc khắc Thổ -> Quái Mộc bị Kim khắc (Đá Light)
        Assert.AreEqual(TypeStone.Light, StepData.GetCounterStoneForElement(ElementType.Wood));

        // Thổ khắc Thủy -> Quái Thổ bị Mộc khắc (Đá Wood)
        Assert.AreEqual(TypeStone.Wood, StepData.GetCounterStoneForElement(ElementType.Earth));

        // Thủy khắc Hỏa -> Quái Thủy bị Thổ khắc (Đá Earth)
        Assert.AreEqual(TypeStone.Earth, StepData.GetCounterStoneForElement(ElementType.Water));
    }

    [Test]
    public void LevelDataSO_WaveStructure_StrictlyMatchesGDDTable1()
    {
        var steps = _levelSO.GetData().GetListStepData();
        Assert.AreEqual(10, steps.Count);

        // Wave 1: Chiến đấu, Dễ, Đội hình Full 1 thuộc tính Đất, Reward: 18 lõi
        Assert.AreEqual(1, steps[0].GetStep());
        Assert.AreEqual(WaveType.Combat, steps[0].GetWaveType());
        Assert.AreEqual(StepDifficulty.Easy, steps[0].GetStepDifficulty());
        Assert.AreEqual(FormationType.FullSingleElement, steps[0].GetFormationType());
        Assert.AreEqual(ElementType.Earth, steps[0].GetEnemyElement());
        Assert.AreEqual(18, steps[0].GetWaveEnergyReward());
        Assert.IsFalse(steps[0].IsDiscoveryWave);

        // Wave 2: Khám phá, Roll, Reward: 0 lõi
        Assert.AreEqual(2, steps[1].GetStep());
        Assert.AreEqual(WaveType.Discovery, steps[1].GetWaveType());
        Assert.AreEqual(StepEventType.RuneStone_Roll, steps[1].GetEventType());
        Assert.AreEqual(0, steps[1].GetWaveEnergyReward());
        Assert.IsTrue(steps[1].IsDiscoveryWave);

        // Wave 3: Chiến đấu, Dễ, Cân bằng, Reward: 12 lõi
        Assert.AreEqual(3, steps[2].GetStep());
        Assert.AreEqual(WaveType.Combat, steps[2].GetWaveType());
        Assert.AreEqual(FormationType.Balanced, steps[2].GetFormationType());
        Assert.AreEqual(12, steps[2].GetWaveEnergyReward());

        // Wave 4: Chiến đấu, Dễ, Full Mộc, Reward: 18 lõi
        Assert.AreEqual(4, steps[3].GetStep());
        Assert.AreEqual(WaveType.Combat, steps[3].GetWaveType());
        Assert.AreEqual(ElementType.Wood, steps[3].GetEnemyElement());
        Assert.AreEqual(18, steps[3].GetWaveEnergyReward());

        // Wave 5: Khám phá, Chúc phúc, Reward: 0 lõi
        Assert.AreEqual(5, steps[4].GetStep());
        Assert.AreEqual(WaveType.Discovery, steps[4].GetWaveType());
        Assert.AreEqual(StepEventType.RuneStone_Altar, steps[4].GetEventType());
        Assert.IsTrue(steps[4].IsDiscoveryWave);

        // Wave 7: Khám phá, Đấu thú, Reward: 0 lõi
        Assert.AreEqual(7, steps[6].GetStep());
        Assert.AreEqual(WaveType.Discovery, steps[6].GetWaveType());
        Assert.AreEqual(StepEventType.BeastBetting, steps[6].GetEventType());
        Assert.IsTrue(steps[6].IsDiscoveryWave);

        // Wave 8: Chiến đấu, Dễ, Full Kim, Reward: 18 lõi
        Assert.AreEqual(8, steps[7].GetStep());
        Assert.AreEqual(WaveType.Combat, steps[7].GetWaveType());
        Assert.AreEqual(ElementType.Metal, steps[7].GetEnemyElement());
        Assert.AreEqual(18, steps[7].GetWaveEnergyReward());

        // Wave 10: Chiến đấu, Thường (Boss Hạt Nham Thạch), Reward: 18 lõi
        Assert.AreEqual(10, steps[9].GetStep());
        Assert.AreEqual(WaveType.Combat, steps[9].GetWaveType());
        Assert.AreEqual(EnemyType.HatNhamThach, steps[9].GetListEnemy()[0].GetName());
        Assert.AreEqual(18, steps[9].GetWaveEnergyReward());
    }

    [Test]
    public void BoardController_GetGDDStoneDrop_ProbabilityMatchesGDDFormula()
    {
        GameObject boardObj = new GameObject("BoardController", typeof(BoardController));
        BoardController bc = boardObj.GetComponent<BoardController>();

        // Theo yêu cầu người dùng: tỉ lệ mặc định random luôn là cho 5 loại đá (20% mỗi loại)
        int totalTrials = 10000;
        int lightCount = 0;

        for (int i = 0; i < totalTrials; i++)
        {
            TypeStone stone = bc.GetGDDStoneDrop();
            if (stone == TypeStone.Light)
            {
                lightCount++;
            }
        }

        float lightRatio = (float)lightCount / totalTrials;
        // Expected p = 0.20 (20%). Allow +/- 3% statistical variance in 10000 trials
        Assert.AreEqual(0.20f, lightRatio, 0.03f, "Mỗi loại đá trong 5 hệ có tỷ lệ rơi cân bằng ~20% theo yêu cầu GDD");

        Object.DestroyImmediate(boardObj);
    }
}
