using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
[CreateAssetMenu(fileName = "LevelDataSO",menuName = "ScriptableObjects/LevelDataSO")]
public class LevelDataSO : ScriptableObject
{
    [SerializeField]private LevelData levelData;
    public LevelData GetData()
    {
        return levelData;
    }
    public void SetData(LevelData data)
    {
        levelData = data;
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
#endif
    }
}
[Serializable]
public class LevelData
{
    [SerializeField] private int level = 0;
    [SerializeField] private Vector2Int sizeGird;
    [SerializeField] private List<StepData> listStep = new List<StepData>();

    public LevelData(int lv, Vector2Int size, List<StepData> list)
    {
        level = lv;
        sizeGird = size;
        listStep = list; 
    }

    public int GetLevel() => level;
    public Vector2Int GetSize() => sizeGird;
    public List<StepData> GetListStepData() => listStep;
}
[Serializable]
public enum StepDifficulty
{
    Easy,       // Dễ
    Normal,     // Thường
    Hard,       // Khó
    VeryHard    // Cực khó / Boss
}

[Serializable]
public enum WaveType
{
    Combat,     // Wave chiến đấu (5 quái, 2 hàng trước sau)
    Discovery   // Wave khám phá (Roll, Chúc phúc, Đấu thú, Rương bí ẩn, Gacha...)
}

[Serializable]
public enum FormationType
{
    None,
    FullSingleElement,      // Đội hình Full 1 thuộc tính (Kiếm năng lượng)
    Balanced,               // Đội hình Cân bằng công thủ
    HealthDrain,            // Đội hình Tiêu hao máu
    EnergyStall,            // Đội hình Giằng co (Tiêu hao năng lượng)
    CrowdControlFinisher    // Đội hình Khó chịu (Khóa đá, kết liễu Player)
}

[Serializable]
public class StepData
{
    [SerializeField] private int step = 1;
    [SerializeField] private WaveType waveType = WaveType.Combat;
    [SerializeField] private StepDifficulty stepDifficulty = StepDifficulty.Normal;
    [SerializeField] private FormationType formationType = FormationType.None;
    [SerializeField] private List<Enemy> listEnemy = new List<Enemy>();
    [SerializeField] private StepEventType eventType = StepEventType.None;
    [SerializeField] private int waveEnergyReward = 40;

    // Constructor chuẩn hóa hiện đại theo GDD 50 Level Game (không còn text mục tiêu, loại đá hay thuộc tính quái trong data)
    public StepData(int stepIndex, WaveType wType, StepDifficulty diff, FormationType fType, List<Enemy> list2, StepEventType evt = StepEventType.None, int energyReward = 0)
    {
        step = stepIndex;
        waveType = wType;
        stepDifficulty = diff;
        formationType = fType;
        listEnemy = list2 != null ? list2 : new List<Enemy>();
        eventType = evt;
        waveEnergyReward = (waveType == WaveType.Discovery) ? energyReward : (energyReward > 0 ? energyReward : GetDefaultEnergyReward(diff));
    }

    public WaveType GetWaveType() => waveType;
    public void SetWaveType(WaveType type) => waveType = type;
    public FormationType GetFormationType() => formationType;
    public void SetFormationType(FormationType form) => formationType = form;
    public bool IsDiscoveryWave => waveType == WaveType.Discovery || (listEnemy == null || listEnemy.Count == 0);

    public StepDifficulty GetStepDifficulty() => stepDifficulty;
    public StepDifficulty GetDifficulty() => stepDifficulty;
    public void SetStepDifficulty(StepDifficulty diff)
    {
        stepDifficulty = diff;
        waveEnergyReward = GetDefaultEnergyReward(diff);
    }

    /// <summary>
    /// Thuộc tính nguyên tố của đợt quái: Lấy trực tiếp từ hệ của quái đầu tiên trong danh sách (chuẩn GDD)
    /// </summary>
    public ElementType GetEnemyElement()
    {
        if (listEnemy != null && listEnemy.Count > 0)
        {
            return EnemyStatsDatabase.GetEnemyElement(listEnemy[0].GetName());
        }
        return ElementType.Wood;
    }

    /// <summary>
    /// Ánh xạ đá tương khắc theo Ngũ Hành GDD:
    /// Quái Hỏa (Fire) -> Khắc chế là Thủy (Ice/Water)
    /// Quái Kim (Metal) -> Khắc chế là Hỏa (Fire)
    /// Quái Mộc (Wood) -> Khắc chế là Kim (Light)
    /// Quái Thổ (Earth) -> Khắc chế là Mộc (Wood)
    /// Quái Thủy (Water) -> Khắc chế là Thổ (Earth)
    /// </summary>
    public static TypeStone GetCounterStoneForElement(ElementType elem)
    {
        return elem switch
        {
            ElementType.Fire => TypeStone.Ice,
            ElementType.Metal => TypeStone.Fire,
            ElementType.Wood => TypeStone.Light,
            ElementType.Earth => TypeStone.Wood,
            ElementType.Water => TypeStone.Earth,
            _ => TypeStone.Light
        };
    }

    /// <summary>
    /// Tỷ lệ rơi đá mặc định luôn cân bằng 5 loại đá cơ bản theo GDD
    /// </summary>
    public List<TypeStone> GetTargetTypeStones()
    {
        return new List<TypeStone> { TypeStone.Light, TypeStone.Wood, TypeStone.Ice, TypeStone.Fire, TypeStone.Earth };
    }

    public List<TypeStone> GetAvailableElements() => GetTargetTypeStones();

    /// <summary>
    /// Định mức số lõi năng lượng tự động theo độ khó step từ sheet Currency_Process trong GDD
    /// </summary>
    public static int GetDefaultEnergyReward(StepDifficulty diff)
    {
        return diff switch
        {
            StepDifficulty.Easy => 30,
            StepDifficulty.Normal => 40,
            StepDifficulty.Hard => 60,
            StepDifficulty.VeryHard => 80,
            _ => 40
        };
    }

    public Dictionary<Vector2Int, TypeStone> GetListStone()
    {
        return new Dictionary<Vector2Int, TypeStone>();
    }

    public StepEventType GetEventType() => eventType;
    public void SetEventType(StepEventType evt) => eventType = evt;

    public int GetWaveEnergyReward()
    {
        if (waveType == WaveType.Discovery)
        {
            return waveEnergyReward;
        }
        if (waveEnergyReward <= 0)
        {
            waveEnergyReward = GetDefaultEnergyReward(stepDifficulty);
        }
        return waveEnergyReward;
    }

    public void SetWaveEnergyReward(int reward)
    {
        waveEnergyReward = reward;
    }

    public void SetStep(int idx) => step = idx;
    public int GetStep() => step;
    public List<Enemy> GetListEnemy() => listEnemy;
}
[Serializable]
public class Stone
{
    [SerializeField]private TypeStone name;
    [SerializeField]private Vector2Int index;
    public Stone(TypeStone ten,Vector2Int idx)
    {
        name = ten;
        index = idx;
    }
    public TypeStone GetName()
    {
        return name;
    }
    public Vector2Int GetIndex()
    {
        return index;
    }
}
[Serializable]
public class Enemy
{
    [SerializeField] private EnemyType name;
    [SerializeField] private int Amount = 1;
    [SerializeField] private EnemyRow row = EnemyRow.Front;

    public Enemy(EnemyType ten, int num, EnemyRow assignedRow = EnemyRow.Front)
    {
        name = ten;
        Amount = num;
        row = assignedRow;
    }

    public EnemyType GetName() => name;
    public int GetNum() => Amount;
    public EnemyRow GetRow() => row;
    public void SetRow(EnemyRow r) => row = r;
}