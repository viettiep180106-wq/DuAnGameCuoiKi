using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class LevelMaker : MonoBehaviour
{
    public static LevelMaker instance;
    [Header("----Setting Base----")]
    public LevelDataSO data;
    public ScriptsBoardStone boardStone;
    public SriptsBoardEnemy boardEnemy;
    public EnemyItem itemEnemy;
    public StoneItem itemStone;
    private LevelData dataLevel;
    private List<StepData> listStep = new List<StepData>();
    private InforSetUp inforSetUp;
    private bool isLoadLevel = false;
    private bool isLoadStep = false;
    public void SaveDataLevel()
    {
        if(listStep.Count == 0)
        {
            Debug.Log("Level chưa có data lên không được lưu");
            return;
        }
        LevelData newDataLevel = new LevelData(inforSetUp.level,inforSetUp.size,listStep);
        data.SetData(newDataLevel);
        Debug.Log("Lưu data Level thành công!");
        isLoadLevel = false;
    }
    public void LoadDataLevel()
    {
        if(boardEnemy.listEnemy.Count>0 || boardStone.listStone.Count > 0)
        {
            ResetStep();
        }
        if(data == null)
        {
            Debug.Log("Chưa có LevelDataSO trong data!");
            return;
        }
        dataLevel = data.GetData();
        if(dataLevel.GetLevel() == 0)
        {
            Debug.Log("Level này chưa có dữ liệu");
            inforSetUp = new InforSetUp(0,0,dataLevel.GetSize(),0,0);
            return;
        }
        inforSetUp = new InforSetUp(dataLevel.GetLevel(),0,dataLevel.GetSize(),0,0);
        listStep = dataLevel.GetListStepData();
        isLoadLevel =true;
        Debug.Log("Level load thành công!");
    }
    public void LoadDataStep(int step)
    {
        if(boardEnemy.listEnemy.Count>0 || boardStone.listStone.Count > 0)
        {
            ResetStep();
        }
        if(isLoadLevel == false)
        {
            Debug.Log("data chưa được load level!");
            return;
        }
        bool check = false;
        foreach(StepData stepData in listStep)
        {
            if (stepData.GetStep() == step)
            {
                inforSetUp.step = step;inforSetUp.heightGirdStoneStep = stepData.GetHeight();
                inforSetUp.distance = stepData.GetDistance();
                SetInforSetUp(inforSetUp);
                boardStone.ConvertStoneItemToBoard(stepData.GetListStone(),itemStone);
                boardEnemy.ConvertEnemyItemToBoard(stepData.GetListEnemy(),itemEnemy);
                check = true;
                break;
            }
        }
        if(check == false)
        {
            Debug.Log("Step này chưa được tạo");
        }
        isLoadStep = check;
    }
    public void SaveDataStep()
    {
        List<Enemy> listEnemy = boardEnemy.GetDataToSave();
        Dictionary<Vector2Int,TypeStone> listStone = boardStone.GetDataToSave();
        if(listEnemy.Count == 0 || listStone.Count == 0)
        {
            if(isLoadStep== true)
            {
                for(int i =0;i<listStep.Count;i++)
                {
                    if (i+1 == inforSetUp.step)
                    {
                        listStep.RemoveAt(i);
                        if (i < listStep.Count - 1)
                        {
                            listStep[i].SetStep(i+1);
                        }
                        break;
                    }
                }
            }
            Debug.Log("Step không có dữ liệu nên không được lưu");
            return;
        }
        StepData dataStep = new StepData(inforSetUp.step,inforSetUp.heightGirdStoneStep,inforSetUp.distance,listStone,listEnemy);
        if(inforSetUp.step-1 < listStep.Count)
        {
            listStep[inforSetUp.step-1]= dataStep;
        }
        else
        {
            listStep.Add(dataStep);
        }
        Debug.Log("Lưu Step thành công");
        isLoadStep = false;
    }
    public void TestLevel()
    {
        Debug.Log("Bắt đầu test level");
    }
    public void ResetStep()
    {
        boardEnemy.ResetBoard();
        boardStone.ResetBoard();
        Debug.Log("Load lại kích thước và tọa độ item trong gird");
    }
    public void SetInforSetUp(InforSetUp infor)
    {
        boardStone.UpdateGird(infor);
        boardEnemy.UpdateGrid();
        if(isLoadLevel == false)inforSetUp = infor;
        else Debug.Log($"Set up step {infor.step} của level {infor.level} thành công! ");
    }
    public InforSetUp GetInforSetUp()
    {
        return inforSetUp;
    }
}
[Serializable]
public class InforSetUp
{
    public int level = 0;
    public int step = 0;
    public Vector2Int size;
    public int heightGirdStoneStep;
    public float distance;
    public InforSetUp(int lv, int st,Vector2Int sizeGird, int height,float dis)
    {
        level = lv;
        step = st;
        size = sizeGird;
        heightGirdStoneStep = height;
        distance = dis;
    }
}
