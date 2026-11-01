using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
public class BoardController : MonoBehaviour
{
    public static BoardController instance;
    public Action<Direction,StoneBase>OnSwapStone;
    [Header("----Setting Base----")]
    [SerializeField]private int rows = 5;
    [SerializeField]private int cols = 5;
    [SerializeField]private int maxY = 5;
    [SerializeField]private bool canTrans = true;
    public RectTransform center;
    Dictionary<TypeStone,StoneBase> prefabs = new Dictionary<TypeStone,StoneBase>();
    public SlotStoneBase prefabSlot;
    [SerializeField]private List<List<SlotStoneBase>> grid;
    [SerializeField]private float height;
    [SerializeField]private float width ;
    Dictionary<Vector2Int,TypeStone> listData = new Dictionary<Vector2Int, TypeStone>();
    Vector2 sizeSlot;
    bool isRevert = false;
    private void Awake()
    {  
        instance = this;
        LoadPrefabFromResource();
    }


    private void OnEnable()
    {
        OnSwapStone += Trans;
    }
    private void OnDisable()
    {
        OnSwapStone -= Trans;
    }
    public void LoadPrefabFromResource()
    {
        string path = "Prefab/Base/Stone";
        StoneBase[] request = Resources.LoadAll<StoneBase>(path);
        foreach(StoneBase stone in request)
        {
            prefabs.Add(stone.nameStone,stone);
        }
    }
    public void InitDataBoard(Dictionary<Vector2Int,TypeStone> data,Vector2Int Size)
    {
        listData = data;
        rows = Size.x; cols = Size.x;
        maxY = Size.y;
        SetUpBoard();
    }
    public void SetUpBoard()
    {
        float sizeXSlot = (float)width/cols;
        float sizeYSlot = (float)height/rows;
        sizeSlot = new Vector2(sizeXSlot,sizeYSlot);
        grid = new List<List<SlotStoneBase>>(rows);
        for (int x = 0; x < cols; x++)
        {
            List<SlotStoneBase> colList = new List<SlotStoneBase>(rows);
            for (int y = 0; y < rows; y++)
            {
                float posX = sizeXSlot*x - (width/2) + sizeXSlot/2 ;
                float posY = sizeYSlot*y-(height/2) + sizeYSlot/2;
                SlotStoneBase prefab = Instantiate(prefabSlot, center);
                prefab.SlotItem.sizeDelta = sizeSlot;
                prefab.SlotItem.anchoredPosition = new Vector2(posX, posY);
                colList.Add(prefab);
            }
            grid.Add(colList);
        }
        UpdateStoneToBoard();
    }
    public void UpdateStoneToBoard()
    {
        for (int x = 0; x < cols; x++)
        {
            for(int y = 0; y < rows; y++)
            {
                Vector2 posTarget = grid[x][y].SlotItem.anchoredPosition;
                grid[x][y].SetStone(InitStone(posTarget,new Vector2Int(x,y),0.5f));
            }
        }
    }
    public StoneBase InitStone(Vector2 posTarget,Vector2Int idx,float time)
    {
        Vector2Int index = idx;
        if (!listData.ContainsKey(index))
        {
            if(isRevert == true)
            {
                int y = rows-1;
                while(!listData.ContainsKey(new Vector2Int(index.x, y)) && y< maxY)
                {
                    y++;
                    if(listData.ContainsKey(new Vector2Int(index.x,y)) && y < maxY)
                    {
                        index = new Vector2Int(index.x,y);
                        isRevert = false;
                        break;
                    }
                }
            }
            else return null;
        }
        Vector2 posInit = grid[idx.x][rows-1].SlotItem.anchoredPosition +new Vector2(0,sizeSlot.y);
        StoneBase stone = ObjectPoolingManager.Instance.Spawn(prefabs[listData[index]],posInit,Quaternion.identity,center);
        stone.item.sizeDelta = sizeSlot;stone.SetOriginIndex(idx.x,idx.y);
        stone.StoneTrans(posTarget,time);
        listData.Remove(index);
        Debug.Log(index);
        return stone;
    }
    public IEnumerator MatchAllBoard()
    {
        HashSet<Vector2Int> listMatch = new HashSet<Vector2Int>(0);
        for(int x = 0; x < cols; x++)
        {
            for(int y = 0; y < rows; y++)
            {
                TypeStone name = grid[x][y].GetStone().nameStone;
                if(x + 2<cols)
                {
                    if(name == grid[x+1][y].GetStone().nameStone && name == grid[x + 2][y].GetStone().nameStone)
                    {
                        listMatch.Add(new Vector2Int(x,y));
                        listMatch.Add(new Vector2Int(x+1,y));
                        listMatch.Add(new Vector2Int(x+2,y));
                       
                    }
                }
                if(y + 2<rows)
                {
                    if(name == grid[x][y+1].GetStone().nameStone && name == grid[x][y+2].GetStone().nameStone)
                    {
                        listMatch.Add(new Vector2Int(x,y));
                        listMatch.Add(new Vector2Int(x,y+1));
                        listMatch.Add(new Vector2Int(x,y+2));
                    }
                }
            }
        }
        if(listMatch.Count == 0)
        {
            canTrans = true;
            yield break;
        }
        else
        {
            SkillPassiveManager.instance.CaculatorSkill(listMatch,grid);
        }
        foreach(Vector2Int e in listMatch)
        {
            SlotStoneBase slot = grid[e.x][e.y];
            if (slot.GetStone() != null)
            {
                slot.GetStone().FinishTask();
                slot.SetStone(null);
            }
        }
        yield return new WaitForSeconds(0.5f);
        float maxTime = 0;
        for(int x = 0; x < cols; x++)
        {
            int SlotEmpty = 0;
            for(int y = 0; y < rows; y++)
            {
                StoneBase stoneCurrent = grid[x][y].GetStone();
                if(stoneCurrent != null)
                {
                    if(SlotEmpty < y)
                    {
                        grid[x][y].SetStone(null);
                        if((y-SlotEmpty)*0.2f > maxTime)maxTime = (y-SlotEmpty)*0.2f;
                        stoneCurrent.StoneTrans(grid[x][SlotEmpty].SlotItem.anchoredPosition,(y-SlotEmpty)*0.2f);
                        stoneCurrent.SetOriginIndex(x,SlotEmpty);
                        grid[x][SlotEmpty].SetStone(stoneCurrent);
                    }
                    SlotEmpty++;
                }
            }
            for(int t = SlotEmpty; t < rows; t++)
            {
                if((rows-t)*0.2f > maxTime)maxTime = (rows-t)*0.2f;
                isRevert = true;
                grid[x][t].SetStone(InitStone(grid[x][t].SlotItem.anchoredPosition,new Vector2Int(x,t),(rows-t)*0.2f));
            }
        }
        yield return new WaitForSeconds(maxTime+0.05f);
        StartCoroutine(MatchAllBoard());
    }
    public void Trans(Direction type,StoneBase stone)
    {
        if(canTrans == false)return;
        if(CheckTrans(type,stone.GetOriginIndex()) == false)return;
        else {
            canTrans = false;
            StartCoroutine(TryMatch(type,stone));
        }
    }


    public IEnumerator TryMatch(Direction type,StoneBase stone)
    {
        Vector2Int Index = stone.GetOriginIndex();
        StoneBase stoneB = null;
        switch (type)
        {
            case Direction.right :
            stoneB = grid[Index.x+1][Index.y].GetStone();
            break;
            case Direction.left :
            stoneB = grid[Index.x-1][Index.y].GetStone();
            break;
            case Direction.up :
            stoneB = grid[Index.x][Index.y+1].GetStone();
            break;
            case Direction.down :
            stoneB = grid[Index.x][Index.y-1].GetStone();
            break;
        }
        Vector2Int IndexB = stoneB.GetOriginIndex();
        TrySwap(stone,stoneB);
        grid[Index.x][Index.y].SwapStoneToSlot(grid[IndexB.x][ IndexB.y]);
        yield return new WaitForSeconds(0.5f);
        if(CheckMatch(stone.nameStone,IndexB.x,IndexB.y) == true|| CheckMatch(stoneB.nameStone, Index.x, Index.y)==true)
        {
            StartCoroutine(MatchAllBoard());
        }
        else
        {
            TrySwap(stone,stoneB);
            grid[Index.x][Index.y].SwapStoneToSlot(grid[IndexB.x][ IndexB.y]);
            yield return new WaitForSeconds(0.5f);
            canTrans = true;
        }
    }
    public void TrySwap(StoneBase stoneA, StoneBase stoneB)
    {
        Vector2 posA = stoneA.item.anchoredPosition;
        Vector2 posB = stoneB.item.anchoredPosition;
        stoneA.StoneTrans(posB,0.5f);
        stoneB.StoneTrans(posA,0.5f);
    }
    public bool CheckMatch(TypeStone nameStone,int x, int y)
    {
        int demUp = 0;
        int demDown = 0;
        int demRight = 0;
        int demLeft =0;
        for(int i = 1; i <= 2; i++)
        {
            if (x - i>= 0 )
            {
                if(grid[x-i][y].GetStone() == null)return false;
                if(nameStone == grid[x-i][y].GetStone().nameStone)demLeft++;
                else demLeft--;
            }
            if (x + i < cols)
            {
                if(grid[x+i][y].GetStone() == null)return false;
                if(nameStone == grid[x+i][y].GetStone().nameStone)demRight++;
                else demRight--;
            }
            if(y+i < rows)
            {
                if(grid[x][y+i].GetStone() == null)return false;
                if(nameStone == grid[x][y+i].GetStone().nameStone)demUp++;
                else demUp--;
            }
            if (y - i >= 0)
            {
                if(grid[x][y-i].GetStone() == null)return false;
                if(nameStone == grid[x][y-i].GetStone().nameStone)demDown++;
                else demDown--;
            }
            if((demDown==1 &&  demUp== 1)||(demLeft==1 && demRight ==1))return true;
        }
        if(demDown == 2 || demLeft==2 ||demRight == 2||demUp==2)return true;
        else {
            return false;
        }
       
    }
    public bool CheckTrans(Direction type,Vector2 Index)
    {
        if(type is Direction.right)
        {
            if(Index.x == cols - 1)return false;
        }
        if(type is Direction.left)
        {
            if(Index.x == 0)return false;
        }
        if(type is Direction.up)
        {
            if(Index.y == rows-1)return false;
        }
        if(type is Direction.down)
        {
            if(Index.y == 0)return false;
        }
        return true;
    }
   
}







