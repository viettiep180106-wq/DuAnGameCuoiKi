using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
public class ObjectPoolingManager : MonoBehaviour
{
   public static ObjectPoolingManager Instance{get;private set;}
   private readonly Dictionary<GameObject,IObjectPool<GameObject>> pools = new();
   private readonly Dictionary<GameObject,GameObject> instanceToPrefabMap = new();
    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else Instance = this;
    }
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!pools.ContainsKey(prefab))
        {
            CreatePool(prefab);
        }
        GameObject instance = pools[prefab].Get();
        instance.transform.SetLocalPositionAndRotation(position,rotation);
        if(parent != null)instance.transform.SetParent(parent);
        instanceToPrefabMap[instance] = prefab;
        if(instance.TryGetComponent<IPoolable>(out var poolable))
        {
            poolable.OnSpawn();
        }
        return instance;
    }
    public T Spawn<T>(T prefabComponent, Vector3 position, Quaternion rotation, Transform parent = null)where T : Component
    {
        GameObject instance = Spawn(prefabComponent.gameObject,position,rotation,parent);
        return instance.GetComponent<T>();
    }
    public void Despawn(GameObject instance)
    {
        if(instanceToPrefabMap.TryGetValue(instance,out var prefab))
        {
            if(instance.TryGetComponent<IPoolable>(out var poolable))
            {
                poolable.OnDespawn();
            }
            pools[prefab].Release(instance);
            instanceToPrefabMap.Remove(instance);
        }
        else
        {
            Destroy(instance);
        }
    }
    private void CreatePool(GameObject prefab)
    {
        IObjectPool<GameObject> pool = new ObjectPool<GameObject>(
            createFunc: ()=> Instantiate(prefab),
            actionOnGet :obj=>obj.SetActive(true),
            actionOnRelease : obj => obj.SetActive(false),
            actionOnDestroy : Destroy,
            collectionCheck : false,
            defaultCapacity : 10,
            maxSize : 100
        );
        pools.Add(prefab,pool);
    }
}
