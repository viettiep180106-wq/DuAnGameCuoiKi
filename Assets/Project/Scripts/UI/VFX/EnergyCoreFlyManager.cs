using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Quản lý hiệu ứng viên Lõi Năng Lượng rơi ra khi tiêu diệt quái và tự động bay vòng cung về thanh Stats Bar.
/// </summary>
public class EnergyCoreFlyManager : MonoBehaviour
{
    private static EnergyCoreFlyManager _instance;
    public static EnergyCoreFlyManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<EnergyCoreFlyManager>(FindObjectsInactive.Include);
            }
            return _instance;
        }
    }

    [Header("Configuration")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Sprite energyCoreSprite;
    [SerializeField] private RectTransform containerRect;

    private Camera _mainCamera;
    private readonly Queue<GameObject> _pool = new Queue<GameObject>();

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

        _mainCamera = Camera.main;

        if (targetCanvas == null)
        {
            targetCanvas = GetComponentInParent<Canvas>();
            if (targetCanvas == null)
            {
                targetCanvas = FindFirstObjectByType<Canvas>();
            }
        }

        if (containerRect == null)
        {
            containerRect = GetComponent<RectTransform>();
            if (containerRect == null && targetCanvas != null)
            {
                containerRect = targetCanvas.GetComponent<RectTransform>();
            }
        }

        if (energyCoreSprite == null)
        {
#if UNITY_EDITOR
            energyCoreSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Events/EnergyCore_Icon.png");
#endif
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    /// <summary>
    /// Cộng trực tiếp Lõi Năng Lượng vào Player và kích hoạt hiệu ứng juice trên PlayerStatsBar
    /// (Không sinh các object UI nhỏ gây rác màn hình theo yêu cầu GDD)
    /// </summary>
    /// <param name="worldPos">Tọa độ chết của quái (World Space)</param>
    /// <param name="totalEnergy">Số lõi năng lượng nhận được</param>
    public void SpawnFlyingCores(Vector3 worldPos, int totalEnergy)
    {
        if (totalEnergy <= 0) return;

        if (Player.Instance != null)
        {
            Player.Instance.AddEnergy(totalEnergy);
        }
        if (PlayerStatsUI.Instance != null)
        {
            PlayerStatsUI.Instance.PlayEnergyCoreCollectJuice();
        }
    }

    private IEnumerator FlyCoreRoutine(Vector3 worldSpawnPos, int rewardAmount, float delay)
    {
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        if (_mainCamera == null) _mainCamera = Camera.main;
        if (targetCanvas == null) targetCanvas = FindFirstObjectByType<Canvas>();

        RectTransform canvasRect = containerRect != null ? containerRect : (targetCanvas != null ? targetCanvas.GetComponent<RectTransform>() : null);
        if (canvasRect == null)
        {
            // Fallback: cộng trực tiếp nếu không tìm thấy canvas
            if (Player.Instance != null) Player.Instance.AddEnergy(rewardAmount);
            yield break;
        }

        // Chuyển world position sang canvas local point
        Camera camForScreen = (targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : _mainCamera;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(_mainCamera, worldSpawnPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, camForScreen, out Vector2 startLocalPos);

        // Lấy hoặc tạo GameObject từ pool
        GameObject coreObj = GetCoreObject(canvasRect);
        RectTransform coreRect = coreObj.GetComponent<RectTransform>();

        // Tách nhẹ các viên xung quanh điểm rơi để tránh đè khít lên nhau
        Vector2 spread = new Vector2(Random.Range(-18f, 18f), Random.Range(-12f, 12f));
        Vector2 initialPos = startLocalPos + spread;
        coreRect.anchoredPosition = initialPos;
        coreRect.localScale = Vector3.one * 0.3f;

        Image coreImg = coreObj.GetComponent<Image>();
        coreImg.color = Color.white;

        // 1. Nảy nhẹ ra tại chỗ (0.15s) để người chơi nhìn rõ viên ngọc xuất hiện từ quái
        coreRect.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutQuad);

        // 2. Xác định đích đến: Icon Lõi Năng Lượng trên PlayerStatsBar
        Transform targetTrans = PlayerStatsUI.Instance != null ? PlayerStatsUI.Instance.EnergyCoreTargetTransform : null;
        Vector2 targetLocalPos;

        if (targetTrans != null)
        {
            Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(camForScreen, targetTrans.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, targetScreen, camForScreen, out targetLocalPos);
        }
        else
        {
            // Fallback vị trí thanh bar
            targetLocalPos = new Vector2(220f, -170f);
        }

        // 3. Bay THẲNG trực tiếp về phía icon lõi năng lượng của PlayerStatsBar (~0.9s, gia tốc nhanh dần)
        float flyDuration = Random.Range(0.85f, 1.05f);
        coreRect.DORotate(new Vector3(0, 0, Random.Range(-30f, 30f)), flyDuration);
        coreRect.DOAnchorPos(targetLocalPos, flyDuration)
            .SetEase(Ease.InQuad) // Bay nhanh dần tự nhiên theo lực hút từ tính
            .OnComplete(() =>
            {
                // 4. Tiếp đất: Kích hoạt ngay hiệu ứng juice nảy số + phát sáng vàng trên bar
                if (PlayerStatsUI.Instance != null)
                {
                    PlayerStatsUI.Instance.PlayEnergyCoreCollectJuice();
                }
                else if (targetTrans != null)
                {
                    targetTrans.DOKill();
                    targetTrans.DOPunchScale(Vector3.one * 0.35f, 0.22f, 7, 0.8f);
                }

                if (Player.Instance != null)
                {
                    Player.Instance.AddEnergy(rewardAmount);
                }

                coreRect.DOScale(Vector3.zero, 0.1f).OnComplete(() => RecycleCore(coreObj));
            });
    }

    private GameObject GetCoreObject(RectTransform parent)
    {
        GameObject obj;
        if (_pool.Count > 0)
        {
            obj = _pool.Dequeue();
            obj.SetActive(true);
        }
        else
        {
            obj = new GameObject("FlyingEnergyCore", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(parent, false);

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(38f, 46f);

            Image img = obj.GetComponent<Image>();
            img.sprite = energyCoreSprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }
        return obj;
    }

    private void RecycleCore(GameObject obj)
    {
        obj.SetActive(false);
        _pool.Enqueue(obj);
    }
}
