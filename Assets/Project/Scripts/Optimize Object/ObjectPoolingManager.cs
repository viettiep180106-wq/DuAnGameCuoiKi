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
        set => _instance = value;
    }

    private readonly Dictionary<GameObject, IObjectPool<GameObject>> _pools = new();
    private readonly Dictionary<GameObject, GameObject> _instanceToPrefabMap = new();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (prefab == null) return null;

        if (!_pools.TryGetValue(prefab, out var pool))
        {
            pool = CreatePool(prefab);
        }

        GameObject instance = null;
        try
        {
            instance = pool.Get();
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
        _instanceToPrefabMap[instance] = prefab;
        if (instance.TryGetComponent<IPoolable>(out var poolable))
        {
            poolable.OnSpawn();
        }
        return instance;
    }

    public T Spawn<T>(T prefabComponent, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
    {
        if (prefabComponent == null) return null;
        GameObject instance = Spawn(prefabComponent.gameObject, position, rotation, parent);
        return instance != null ? instance.GetComponent<T>() : null;
    }

    public void Despawn(GameObject instance)
    {
        if (instance == null) return;

        if (_instanceToPrefabMap.TryGetValue(instance, out var prefab))
        {
            if (instance.TryGetComponent<IPoolable>(out var poolable))
            {
                poolable.OnDespawn();
            }
            if (_pools.TryGetValue(prefab, out var pool))
            {
                pool.Release(instance);
            }
            else
            {
                if (Application.isPlaying) Destroy(instance);
                else DestroyImmediate(instance);
            }
            _instanceToPrefabMap.Remove(instance);
        }
        else
        {
            if (Application.isPlaying) Destroy(instance);
            else DestroyImmediate(instance);
        }
    }

    public void Despawn<T>(T component) where T : Component
    {
        if (component != null) Despawn(component.gameObject);
    }

    private IObjectPool<GameObject> CreatePool(GameObject prefab)
    {
        if (prefab == null) return null;
        IObjectPool<GameObject> pool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(prefab),
            actionOnGet: obj => { if (obj != null) obj.SetActive(true); },
            actionOnRelease: obj => { if (obj != null) obj.SetActive(false); },
            actionOnDestroy: obj => { 
                if (obj != null) {
                    if (Application.isPlaying) Destroy(obj);
                    else DestroyImmediate(obj);
                }
            },
            collectionCheck: false,
            defaultCapacity: 10,
            maxSize: 100
        );
        _pools[prefab] = pool;
        return pool;
    }
}
