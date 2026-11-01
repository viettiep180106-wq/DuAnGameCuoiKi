using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;
using System.Linq;




#if UNITY_EDITOR
using UnityEditor;
#endif
[ExecuteAlways]
public class ScriptsBoardStone : MonoBehaviour
{
    [SerializeField]private SpriteRenderer sprite;
    [SerializeField]private Transform board;
    private Dictionary<Vector2Int,TypeStone> list= new Dictionary<Vector2Int, TypeStone>();
    private Dictionary<Vector2,Vector2Int>ListPos = new Dictionary<Vector2, Vector2Int>();
    public List<StoneItem> listStone = new List<StoneItem>();
    private InforSetUp inforSetUp = null;
    private float widthCell = 0;
    private float heightCell = 0;
    float width =0 ;
    float height =0;
    public void ConvertStoneItemToBoard(Dictionary<Vector2Int,TypeStone> dataListStone,StoneItem prefab)
    {
        list = dataListStone;
        for(int x = 0; x < inforSetUp.size.x; x++)
        {
            for(int y = 0; y < inforSetUp.heightGirdStoneStep; y++)
            {
                StoneItem item = Instantiate(prefab);
                Vector2Int idx = new Vector2Int(x,y);
                item.stone = new Stone(list[idx],idx);
                item.transform.position = GetPosFromIndex(idx);
                SetUpItem(item.gameObject); 
            }
        }

    }
    public void UpdateGird(InforSetUp infor)
    {
        inforSetUp = infor;
        width = sprite.size.x;
        widthCell = width/infor.size.x;
        height = widthCell*infor.heightGirdStoneStep;
        heightCell = widthCell;
        ListPos.Clear();
        for(int x = 0; x < infor.size.x; x++)
        {
            for(int y = 0; y < infor.heightGirdStoneStep; y++)
            {
                float posX = x*widthCell- width/2 + widthCell/2; 
                float posY = y*heightCell- height/2 + heightCell/2;
                ListPos.Add(new Vector2(posX,posY),new Vector2Int(x,y));
            }
        }
        if (listStone.Count > 0 && list.Count>0)
        {
            foreach(StoneItem stone in listStone)
            {
                Vector2 pos = GetPosFromIndex(stone.stone.GetIndex());
                stone.transform.position = new Vector3(pos.x,pos.y,0);
                stone.sprite.size = new Vector2(widthCell,widthCell);
            }
        }
    }
    private void OnDrawGizmos()
    {
        if(inforSetUp == null)return;
        int NumX = inforSetUp.size.x;
        int NumY = inforSetUp.heightGirdStoneStep;
        Vector2 posOrigin = board.position;
        Gizmos.color = Color.green;
        for(int x = 0; x <= NumX; x++)
        {
            Vector2 start = posOrigin+new Vector2(x*widthCell-width/2,NumY*widthCell/2);
            Vector2 end = posOrigin + new Vector2(x*widthCell-width/2,-NumY*widthCell/2);
            Gizmos.DrawLine(start,end);
        }
        for(int y = 0; y <= NumY; y++)
        {
            Vector2 start =posOrigin + new Vector2(NumX*heightCell/2,y*heightCell-height/2);
            Vector2 end = posOrigin+ new Vector2(-NumX*heightCell/2,y*heightCell-height/2);
            Gizmos.DrawLine(start,end);
        }
    }
    public void ResetBoard()
    {
        foreach(StoneItem item in listStone)
        {
            if (item != null)
            DestroyImmediate(item.gameObject);
        }
        listStone.Clear(); 
        ListPos.Clear();
        list.Clear();
    }
# if UNITY_EDITOR
    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }
    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }
    private void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;
        if(e.type == EventType.DragUpdated)
        {
            GameObject target = HandleUtility.PickGameObject(e.mousePosition,false);
            // cho phép thả trong board
            if(target == gameObject)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                e.Use();// đánh dấu đã xủ lý
            }
        }
        else if (e.type == EventType.DragPerform)
        {
            GameObject target = HandleUtility.PickGameObject(e.mousePosition,false);
            if(target == gameObject)
            {
                DragAndDrop.AcceptDrag();
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                Vector3 pos = ray.origin;
                pos.z = board.position.z;
                foreach (Object Item in DragAndDrop.objectReferences)
                {
                    GameObject prefab = Item as GameObject;
                    if(prefab != null)
                    {
                        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                        if(instance != null)
                        {
                            Undo.RegisterCreatedObjectUndo(instance,"Spawn Prefab");
                            instance.transform.position = pos;
                            SetUpItem(instance);
                        }
                    }
                    
                }
                e.Use();
            }
        }
    }
#endif 
    public void SetUpItem(GameObject obj)
    {
       
        item it = obj.GetComponent<item>();
        if(it is EnemyItem)
        {
            DestroyImmediate(obj);
            return;
        }
        else
        {
            obj.transform.SetParent(board,true);
            Debug.Log("OK");
            it.sprite.drawMode = SpriteDrawMode.Sliced;
            it.sprite.size = new Vector2(widthCell,widthCell);
            it.Snapping(ListPos);
            listStone.Add(it as StoneItem);
        }
    }
    public Vector2Int GetIndexFromPos(Vector2 pos)
    {
        if(ListPos.ContainsKey(pos) == false)
        {
            Debug.Log($"{pos.x},{pos.y} không tồn tại index");
            return new Vector2Int(0,0);
        }
        return ListPos[pos];
    }
    public Vector2 GetPosFromIndex(Vector2Int index)
    {
        return ListPos.FirstOrDefault(x => x.Value == index).Key;
    }
    public Dictionary<Vector2Int,TypeStone> GetDataToSave()
    {
        for(int i = 0; i < listStone.Count; i++)
        {
            Vector2Int index = GetIndexFromPos(listStone[i].transform.position);
            list[index]= listStone[i].stone.GetName();
        }
        return list;
    }
}
