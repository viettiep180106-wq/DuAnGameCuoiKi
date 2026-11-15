using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
public class ObjectPoolingManager : MonoBehaviour
{
   private static ObjectPoolingManager _instance;
   public static ObjectPoolingManager Instance
   {
       get
       {
           if (_instance == null)
           {
               _instance = UnityEngine.Object.FindFirstObjectByType<ObjectPoolingManager>(FindObjectsInactive.Include);
               if (_instance == null)
               {
                   GameObject go = new GameObject("ObjectPoolingManager", typeof(ObjectPoolingManager));
                   _instance = go.GetComponent<ObjectPoolingManager>();
               }
           }
           return _instance;
       }
   }
   private readonly Dictionary<GameObject,IObjectPool<GameObject>> pools = new();
   private readonly Dictionary<GameObject,GameObject> instanceToPrefabMap = new();
    private void Awake()
    {
        if(_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else _instance = this;
    }
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (prefab == null) return null;

        if (!pools.ContainsKey(prefab))
        {
            CreatePool(prefab);
        }

        GameObject instance = null;
        try
        {
            instance = pools[prefab].Get();
        }
        catch (System.Exception)
        {
            // Nếu object trong pool bị destroy ngoài ý muốn, tạo mới an toàn
            instance = Instantiate(prefab);
        }

        if (instance == null)
        {
            instance = Instantiate(prefab);
        }

        if (parent != null)
        {
            instance.transform.SetParent(parent, false);
        }
        instance.transform.SetLocalPositionAndRotation(position, rotation);
        instanceToPrefabMap[instance] = prefab;
        if(instance.TryGetComponent<IPoolable>(out var poolable))
        {
            poolable.OnSpawn();
        }
        return instance;
    }
    public T Spawn<T>(T prefabComponent, Vector3 position, Quaternion rotation, Transform parent = null)where T : Component
    {
        if (prefabComponent == null) return null;
        GameObject instance = Spawn(prefabComponent.gameObject,position,rotation,parent);
        return instance != null ? instance.GetComponent<T>() : null;
    }
    public void Despawn(GameObject instance)
    {
        if (instance == null) return;

        if(instanceToPrefabMap.TryGetValue(instance,out var prefab))
        {
            if(instance.TryGetComponent<IPoolable>(out var poolable))
            {
                poolable.OnDespawn();
            }
            if (pools.ContainsKey(prefab))
            {
                pools[prefab].Release(instance);
            }
            else
            {
                Destroy(instance);
            }
            instanceToPrefabMap.Remove(instance);
        }
        else
        {
            Destroy(instance);
        }
    }
    private void CreatePool(GameObject prefab)
    {
        if (prefab == null) return;
        IObjectPool<GameObject> pool = new ObjectPool<GameObject>(
            createFunc: ()=> Instantiate(prefab),
            actionOnGet : obj => { if (obj != null) obj.SetActive(true); },
            actionOnRelease : obj => { if (obj != null) obj.SetActive(false); },
            actionOnDestroy : obj => { if (obj != null) Destroy(obj); },
            collectionCheck : false,
            defaultCapacity : 10,
            maxSize : 100
        );
        pools[prefab] = pool;
    }
}
