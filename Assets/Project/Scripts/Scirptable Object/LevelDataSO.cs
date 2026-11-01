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
    }
#endif
}
[Serializable]
public class LevelData
{
    [SerializeField]private int level =0;
    [SerializeField]private Vector2Int sizeGird;
    [SerializeField]private List<StepData> listStep = new List<StepData>();
    public LevelData(int lv,Vector2Int size,List<StepData> list)
    {
        level = lv;
        sizeGird = size;
        listStep = list; 
    }
    public int GetLevel()
    {
        return level;
    }
    public Vector2Int GetSize()
    {
        return sizeGird;
    }
    public List<StepData> GetListStepData()
    {
        return listStep;
    }
}
[Serializable]
public class StoneSaveData
{
    public Vector2Int position;
    public TypeStone type;

    public StoneSaveData(Vector2Int pos, TypeStone type)
    {
        this.position = pos;
        this.type = type;
    }
}
[Serializable]
public class StepData
{
    [SerializeField]private int step;
    [SerializeField]private float distance;
    [SerializeField]private int height;
    [SerializeField]private List<StoneSaveData> listStone = new List<StoneSaveData>();
    [SerializeField]private List<Enemy>listEnemy;
    public StepData(int stepIndex,int hg,float dis, Dictionary<Vector2Int,TypeStone>dictStone, List<Enemy>list2)
    {
        step = stepIndex;
        height = hg;
        distance = dis;
        listStone.Clear();
        foreach (var kvp in dictStone)
        {
            listStone.Add(new StoneSaveData(kvp.Key, kvp.Value));
        }
        listEnemy = list2;
    }
    public Dictionary<Vector2Int, TypeStone> GetListStone()
    {
        Dictionary<Vector2Int, TypeStone> dict = new Dictionary<Vector2Int, TypeStone>();
        foreach (var item in listStone)
        {
            if(!dict.ContainsKey(item.position))
                dict.Add(item.position, item.type);
        }
        return dict;
    }
    public void SetStep(int idx)
    {
        step = idx;
    }
    public int GetStep()
    {
        return step;
    }
    public float GetDistance()
    {
        return distance;
    }
    public List<Enemy> GetListEnemy()
    {
        return listEnemy;
    }
    public int GetHeight()
    {
        return height;
    }
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
    [SerializeField]private EnemyType name;
    [SerializeField]private int Amount;
    [SerializeField]private float distnacseToPlayer;
    public Enemy(EnemyType ten,int num,float distance)
    {
        name = ten;
        Amount = num;
        distnacseToPlayer = distance;
    }
    public EnemyType GetName()
    {
        return name;
    }
    public int GetNum()
    {
        return Amount;
    }
    public float GetDistance()
    {
        return distnacseToPlayer;
    }
}