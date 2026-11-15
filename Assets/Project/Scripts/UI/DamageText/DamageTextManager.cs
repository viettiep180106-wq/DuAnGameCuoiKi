using UnityEngine;
using TMPro;

public class DamageTextManager : MonoBehaviour
{
    private static DamageTextManager _instance;
    public static DamageTextManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<DamageTextManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("DamageTextManager", typeof(DamageTextManager));
                    _instance = go.GetComponent<DamageTextManager>();
                }
            }
            return _instance;
        }
    }

    [SerializeField] private GameObject damageTextPrefab;
    private GameObject runtimeFallbackPrefab;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        CreateFallbackPrefabIfNeeded();
    }

    private void CreateFallbackPrefabIfNeeded()
    {
        if (damageTextPrefab != null) return;

        // Tạo runtime prefab mẫu với font size và cấu hình hiển thị chuẩn xác
        runtimeFallbackPrefab = new GameObject("DamageTextItem_Runtime");
        runtimeFallbackPrefab.AddComponent<DamageTextItem>();
        var tmp = runtimeFallbackPrefab.AddComponent<TextMeshPro>();
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = Color.black;
        
        var mr = runtimeFallbackPrefab.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sortingLayerName = "Default";
            mr.sortingOrder = 50;
        }

        runtimeFallbackPrefab.SetActive(false);
        damageTextPrefab = runtimeFallbackPrefab;
    }

    public void SpawnDamage(float damage, bool isCrit, ElementType element, Vector3 worldPos)
    {
        if (damageTextPrefab == null) CreateFallbackPrefabIfNeeded();

        // Đảm bảo Z luôn ở 985f (nổi bật trước nhân vật ở Z=990, an toàn trong camera farClipPlane)
        Vector3 safePos = new Vector3(worldPos.x, worldPos.y, 985f);
        GameObject obj = ObjectPoolingManager.Instance.Spawn(damageTextPrefab, safePos, Quaternion.identity);
        if (obj != null)
        {
            DamageTextItem item = obj.GetComponent<DamageTextItem>();
            if (item != null)
            {
                item.Show(damage, isCrit, element, safePos);
            }
        }
    }

    public void SpawnCustomText(string message, Color color, Vector3 worldPos, float scaleMultiplier = 1.35f)
    {
        if (damageTextPrefab == null) CreateFallbackPrefabIfNeeded();

        Vector3 safePos = new Vector3(worldPos.x, worldPos.y, 985f);
        GameObject obj = ObjectPoolingManager.Instance.Spawn(damageTextPrefab, safePos, Quaternion.identity);
        if (obj != null)
        {
            DamageTextItem item = obj.GetComponent<DamageTextItem>();
            if (item != null)
            {
                item.ShowCustom(message, color, safePos, scaleMultiplier);
            }
        }
    }
}

