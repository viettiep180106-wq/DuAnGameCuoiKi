using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;

    [Header("Data")]
    public LevelDataSO levelData;
    [SerializeField] private LevelData data;
    [SerializeField] private List<StepData> ListStep = new List<StepData>();

    public UnityAction<int> OnStepStart;
    public UnityAction<int> OnStepEnd;
    public UnityAction<int> OnEquipPreMatch;

    public int TotalSteps => ListStep != null ? ListStep.Count : 10;
    public int CurrentLevelNumber => (levelData != null && levelData.GetData() != null) ? levelData.GetData().GetLevel() : 1;

    private void Awake()
    {
        instance = this;
        if (levelData == null)
        {
            levelData = Resources.Load<LevelDataSO>("ScriptObject/LevelData/LevelDataSO_lv_001");
        }

        if (levelData != null && levelData.GetData() != null)
        {
            SetLevel(levelData.GetData());
            Debug.Log($"[LevelManager] Đã nạp 100% dữ liệu từ ScriptableObject: {levelData.name} ({ListStep.Count} waves)");
        }
        else
        {
            Debug.LogError("[LevelManager] Không tìm thấy dữ liệu LevelDataSO!");
        }
    }

    public void SetLevel(LevelData lv)
    {
        data = lv;
        ListStep = data.GetListStepData();
    }

    private void OnEnable()
    {
        OnStepStart += SetUpStartStep;
        OnStepEnd += SetUpEndStep;
        OnEquipPreMatch += SetUpPreMatch;
    }

    public void SetUpStartStep(int step)
    {
        if (ListStep.Count < step) return;

        if (EnemyController.instance != null)
        {
            EnemyController.instance.StartActive();
        }

        if (StatusMatchUI.instance != null)
        {
            StatusMatchUI.instance.TransAll(true);
        }

        if (BoardController.instance != null)
        {
            BoardController.instance.SlideInBoard();
        }
    }

    public void SetUpEndStep(int step)
    {
    }

    public void SetUpPreMatch(int step)
    {
        if (ListStep.Count < step) return;

        if (EnemyController.instance != null)
        {
            EnemyController.instance.SetUpEnemyInStep(ListStep[step - 1].GetListEnemy());
        }

        Vector2Int size = data != null ? data.GetSize() : new Vector2Int(4, 4);

        if (BoardController.instance != null)
        {
            var targetStones = ListStep[step - 1].GetTargetTypeStones();
            BoardController.instance.InitDataBoard(targetStones, new Vector2Int(size.x,size.y));
            // SlideInBoard được điều khiển tuần tự sau khi Camera Wave Preview và Banner hoàn tất
        }

        if (StatusMatchUI.instance != null)
        {
            StatusMatchUI.instance.TransAll(false);
        }
    }

    public StepData GetStepData(int step)
    {
        if (step >= 1 && step <= ListStep.Count)
        {
            return ListStep[step - 1];
        }
        return null;
    }

    private void OnDisable()
    {
        OnStepStart -= SetUpStartStep;
        OnStepEnd -= SetUpEndStep;
        OnEquipPreMatch -= SetUpPreMatch;
    }
}
