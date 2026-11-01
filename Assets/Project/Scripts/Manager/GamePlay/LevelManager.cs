using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Events;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;
    public LevelDataSO levelData;
    [SerializeField]private LevelData data;
    [SerializeField]private List<StepData> ListStep = new List<StepData>();
    public UnityAction<int> OnStepStart;
    public UnityAction<int> OnStepEnd;
    public UnityAction<int> OnEquipPreMatch;
    private void Awake()
    {
        instance = this;
        if(levelData != null)
        {
            SetLevel(levelData.GetData());
        }
    }
    public void SetLevel(LevelData lv)
    {
        data = lv;
        ListStep = data.GetListStepData();
    }
    private void OnEnable()
    {
        OnStepStart+= SetUpStartStep;
        OnStepEnd+= SetUpEndStep;
        OnEquipPreMatch +=SetUpPreMatch;
    }
    public void SetUpStartStep(int step)
    {
        if(ListStep.Count<step)return;
        EnemyController.instance.StartActive();
        ActiveSkillPassive.instance.ActiveSkill();
        StatusMatchUI.instance.TransAll(true);
    }
    public void SetUpEndStep(int step)
    {
        
    }
    public void SetUpPreMatch(int step)
    {
        if(ListStep.Count<step)return;
        EnemyController.instance.SetUpEnemyInStep(ListStep[step-1].GetListEnemy());
        Vector2Int size = new Vector2Int(data.GetSize().x,ListStep[step-1].GetHeight());
        BoardController.instance.InitDataBoard(ListStep[step-1].GetListStone(),new Vector2Int(data.GetSize().x,ListStep[step-1].GetHeight()));
        StatusMatchUI.instance.TransAll(false);
    }
    private void OnDisable()
    {
        OnStepStart -=SetUpStartStep;
        OnStepEnd -= SetUpEndStep;
        OnEquipPreMatch -= SetUpPreMatch;
    }
}
