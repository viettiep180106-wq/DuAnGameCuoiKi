using UnityEngine;
using TMPro;

public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance { get; private set; }

    [SerializeField] private GameObject damageTextPrefab;
    private GameObject runtimeFallbackPrefab;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        CreateFallbackPrefabIfNeeded();
    }

    private void CreateFallbackPrefabIfNeeded()
    {
        if (damageTextPrefab != null) return;

        // Tạo runtime prefab mẫu nếu người dùng chưa kịp kéo prefab trong Unity Inspector
        runtimeFallbackPrefab = new GameObject("DamageTextItem_Runtime");
        runtimeFallbackPrefab.AddComponent<DamageTextItem>();
        var tmp = runtimeFallbackPrefab.AddComponent<TextMeshPro>();
        tmp.fontSize = 5;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.sortingOrder = 50;

        runtimeFallbackPrefab.SetActive(false);
        damageTextPrefab = runtimeFallbackPrefab;
    }

    public void SpawnDamage(float damage, bool isCrit, ElementType element, Vector3 worldPos)
    {
        if (damageTextPrefab == null) CreateFallbackPrefabIfNeeded();

        GameObject obj = ObjectPoolingManager.Instance.Spawn(damageTextPrefab, worldPos, Quaternion.identity);
        DamageTextItem item = obj.GetComponent<DamageTextItem>();
        if (item != null)
        {
            item.Show(damage, isCrit, element, worldPos);
        }
    }

    public void SpawnCustomText(string message, Color color, Vector3 worldPos, float scaleMultiplier = 1.2f)
    {
        if (damageTextPrefab == null) CreateFallbackPrefabIfNeeded();

        GameObject obj = ObjectPoolingManager.Instance.Spawn(damageTextPrefab, worldPos, Quaternion.identity);
        DamageTextItem item = obj.GetComponent<DamageTextItem>();
        if (item != null)
        {
            item.ShowCustom(message, color, worldPos, scaleMultiplier);
        }
    }
}

