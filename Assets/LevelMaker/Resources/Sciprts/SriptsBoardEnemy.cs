using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif
[ExecuteAlways]
public class SriptsBoardEnemy : MonoBehaviour
{
    public SpriteRenderer sprite;
    public Transform board;
    public int cols_rows = 3;
    public List<EnemyItem> listEnemy = new List<EnemyItem>();
    private float width ;
    private float height ;
    private float widthCell ;
    private float heightCell ;
    public void ConvertEnemyItemToBoard(List<Enemy> list, EnemyItem prefab)
    {
        int t = 0;
        Vector3 pos = board.transform.position;
        width = sprite.size.x;
        height = sprite.size.y;
        widthCell = width/cols_rows;
        heightCell = widthCell;
        for(int x = 0; x < cols_rows; x++)
        {
            for(int y = 0; y < cols_rows; y++)
            {
                if(t>= list.Count)return;
                float posX = x*widthCell-width/2 + widthCell/2;
                float posY = y*heightCell-height/2 + heightCell/2;
                EnemyItem item = Instantiate(prefab);
                item.enemy = new Enemy(list[t].GetName(),list[t].GetNum(),list[t].GetDistance());
                item.transform.position =new Vector3(posX,posY,0) +pos;
                SetUpItem(item.gameObject);
                t++;
            }
        }
    }
    public void UpdateGrid()
    {
        Vector3 pos = board.transform.position;
        width = sprite.size.x;
        height = sprite.size.y;
        widthCell = width/cols_rows;
        heightCell = widthCell;
        if (listEnemy.Count > 0)
        {
            int t =0;
            for(int x = 0; x < cols_rows; x++)
            {
                for(int y = 0; y < cols_rows; y++)
                {
                    float posX = x*widthCell-width/2 + widthCell/2;
                    float posY = y*height-height/2 + heightCell/2;
                    listEnemy[t].transform.position = pos+new Vector3(posX,posY,0);
                    listEnemy[t].sprite.size = new Vector2(widthCell,heightCell);
                }
            }
        }
    }
    private void OnDrawGizmos()
    { 
        if(width==0 || height==0 || widthCell==0 || heightCell==0)return;
        Vector2 posOrigin = board.transform.position;
        for(int i = 0; i <= cols_rows; i++)
        {
            Vector2 start = posOrigin+ new Vector2(i*widthCell - width/2,height/2);
            Vector2 end = posOrigin+new Vector2(i*widthCell - width/2,-height/2);
            Gizmos.DrawLine(start,end);
            Vector2 start1 =posOrigin+ new Vector2(width/2,i*heightCell - height/2);
            Vector2 end1 =posOrigin+ new Vector2(-width/2,i*heightCell - height/2);
            Gizmos.DrawLine(start1,end1);
        }
    }
#if UNITY_EDITOR
    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI1;
    }
    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI1;
    }
    private void OnSceneGUI1(SceneView sceneView)
    {
        Event e = Event.current;
        if(e.type == EventType.DragUpdated)
        {
            GameObject target = HandleUtility.PickGameObject(e.mousePosition,false);
            if(target == gameObject)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                e.Use();
            }
        }
        if(e.type == EventType.DragPerform)
        {
            GameObject target = HandleUtility.PickGameObject(e.mousePosition,false);
            if(target == gameObject)
            {
                DragAndDrop.AcceptDrag();
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                Vector3 pos = ray.origin;
                pos.z = transform.position.z;
                foreach(Object Item in DragAndDrop.objectReferences)
                {
                    GameObject obj = Item as GameObject;
                    if(obj != null)
                    {
                        GameObject instance =(GameObject) PrefabUtility.InstantiatePrefab(obj);
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
        if(it is StoneItem)
        {
            DestroyImmediate(obj);
            return;
        }
        else
        {
            obj.transform.SetParent(board,true);
            SpriteRenderer sprite = it.sprite;
            sprite.drawMode = SpriteDrawMode.Sliced;
            sprite.size = new Vector2(widthCell,heightCell);
            listEnemy.Add(it as EnemyItem);
        }
        
    }
    public List<Enemy> GetDataToSave()
    {
        List<Enemy> list = new List<Enemy>();
        foreach(EnemyItem item in listEnemy)
        {
            Enemy e = item.enemy;
            list.Add(e);
        }
        return list;
    }
    public void ResetBoard()
    {
        foreach(EnemyItem item in listEnemy)
        {
            if(item!=null)DestroyImmediate(item.gameObject);
        }
        listEnemy.Clear();
    }
}
