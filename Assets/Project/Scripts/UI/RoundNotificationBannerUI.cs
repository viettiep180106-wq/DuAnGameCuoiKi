using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Micro-Banner thông báo chuyển Round và bắt đầu lượt người chơi (GDD Turn-based 5v1 UI/UX):
/// - Tông màu: Đỏ Son viền Vàng Kim (Imperial Crimson & Gold).
/// - Hiển thị nhanh gọn trong 0.65s (Slide/Scale-in 0.2s -> Hold 0.25s -> Fade-out 0.2s).
/// - Tự động mở khóa bàn cờ và kích hoạt lượt người chơi mượt mà.
/// </summary>
public class RoundNotificationBannerUI : MonoBehaviour
{
    private static RoundNotificationBannerUI _instance;
    public static RoundNotificationBannerUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<RoundNotificationBannerUI>();
                if (_instance == null)
                {
                    Canvas canvas = FindFirstObjectByType<Canvas>();
                    if (canvas != null)
                    {
                        GameObject bannerObj = new GameObject("RoundNotificationBanner", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(RoundNotificationBannerUI));
                        bannerObj.transform.SetParent(canvas.transform, false);
                        var rt = bannerObj.GetComponent<RectTransform>();
                        rt.anchorMin = new Vector2(0.5f, 0.5f);
                        rt.anchorMax = new Vector2(0.5f, 0.5f);
                        rt.pivot = new Vector2(0.5f, 0.5f);
                        rt.sizeDelta = new Vector2(640f, 110f);
                        rt.anchoredPosition = new Vector2(0f, 220f);
                        _instance = bannerObj.GetComponent<RoundNotificationBannerUI>();
                    }
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("UI Components")]
    [SerializeField] private RectTransform bannerRect;
    [SerializeField] private CanvasGroup bannerCanvasGroup;
    [SerializeField] private Image bannerBackground;
    [SerializeField] private TMP_Text roundTitleText;
    [SerializeField] private TMP_Text roundSubText;

    [Header("Font Settings")]
    [SerializeField] private TMP_FontAsset bannerFont;

    [Header("Color Theme")]
    [SerializeField] private Color bannerColor = new Color(0.72f, 0.11f, 0.11f, 0.95f); // Đỏ son chiến trận
    [SerializeField] private Color titleGoldColor = new Color(1f, 0.88f, 0.25f, 1f);     // Vàng hoàng kim
    [SerializeField] private Color subTextColor = new Color(0.95f, 0.95f, 1f, 0.9f);

    [Header("Duration Settings")]
    [Tooltip("Tổng thời gian hiển thị banner (mặc định 2.5s theo yêu cầu)")]
    [SerializeField] private float totalDisplayDuration = 2.5f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        EnsureComponentsExist();

        if (bannerCanvasGroup != null)
        {
            bannerCanvasGroup.alpha = 0f;
            bannerCanvasGroup.blocksRaycasts = false;
        }
    }

    private void EnsureComponentsExist()
    {
        if (bannerRect == null)
        {
            bannerRect = GetComponent<RectTransform>();
        }

        if (bannerRect == null)
        {
            // Tự động gắn RectTransform nếu chưa có
            bannerRect = gameObject.AddComponent<RectTransform>();
        }

        if (bannerCanvasGroup == null)
        {
            bannerCanvasGroup = GetComponent<CanvasGroup>();
            if (bannerCanvasGroup == null) bannerCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (bannerBackground == null)
        {
            bannerBackground = GetComponent<Image>();
        }

        // Tự động tìm hoặc sinh Text con nếu chưa được bind trong Inspector
        if (roundTitleText == null)
        {
            roundTitleText = transform.Find("Title")?.GetComponent<TMP_Text>();
        }

        if (roundSubText == null)
        {
            roundSubText = transform.Find("SubTitle")?.GetComponent<TMP_Text>();
        }
    }

    /// <summary>
    /// Hiển thị Banner Đỏ thông báo chuyển Round và bắt đầu lượt người chơi (Kéo dài 2.5s theo chuẩn GDD)
    /// </summary>
    public void ShowRoundBanner(int roundNumber, Action onComplete = null)
    {
        EnsureRuntimeHierarchyIfNeeded();

        if (roundTitleText != null)
        {
            roundTitleText.text = $"ROUND {roundNumber} – LƯỢT CỦA BẠN!";
        }

        if (roundSubText != null)
        {
            roundSubText.text = "3 LƯỢT ĐẦU MIỄN PHÍ – HÃY XUẤT CHIÊU!";
        }

        bannerRect.DOKill();
        if (bannerCanvasGroup != null) bannerCanvasGroup.DOKill();

        gameObject.SetActive(true);
        if (bannerCanvasGroup != null)
        {
            bannerCanvasGroup.alpha = 0f;
            bannerCanvasGroup.blocksRaycasts = false;
        }

        // Pha 1: Pop-in uy lực (0.35s)
        bannerRect.localScale = Vector3.one * 0.80f;
        bannerRect.DOScale(Vector3.one * 1.06f, 0.22f).SetEase(Ease.OutBack).OnComplete(() =>
        {
            bannerRect.DOScale(Vector3.one, 0.13f);
        });

        if (bannerCanvasGroup != null)
        {
            bannerCanvasGroup.DOFade(1f, 0.20f);
        }

        // Pha 2: Hiệu ứng thở nhẹ ánh kim trong giai đoạn giữ banner (1.75s)
        DOVirtual.DelayedCall(0.35f, () =>
        {
            bannerRect.DOScale(Vector3.one * 1.03f, 0.85f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutSine);
        });

        // Pha 3: Duy trì đủ 2.1s rồi Fade-out mượt mà trong 0.4s (Tổng thời lượng đúng 2.5s)
        float holdDuration = Mathf.Max(1.0f, totalDisplayDuration - 0.4f);
        DOVirtual.DelayedCall(holdDuration, () =>
        {
            if (bannerCanvasGroup != null)
            {
                bannerCanvasGroup.DOFade(0f, 0.40f).OnComplete(() =>
                {
                    bannerRect.DOKill();
                    bannerRect.localScale = Vector3.one;
                    onComplete?.Invoke();
                });
            }
            else
            {
                bannerRect.DOKill();
                bannerRect.localScale = Vector3.one;
                onComplete?.Invoke();
            }
        });
    }

    private void EnsureRuntimeHierarchyIfNeeded()
    {
        EnsureComponentsExist();

        if (bannerFont == null)
        {
            bannerFont = Resources.Load<TMP_FontAsset>("Fonts/Nunito_Black_SDF");
#if UNITY_EDITOR
            if (bannerFont == null)
            {
                bannerFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Project/Fonts/Nunito_Black_SDF.asset");
            }
#endif
        }

        if (bannerBackground == null)
        {
            bannerBackground = gameObject.AddComponent<Image>();
            bannerBackground.color = bannerColor;
        }

        // Tạo Title Text nếu chưa có
        if (roundTitleText == null)
        {
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(transform, false);
            var rt = titleObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.42f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            var tmp = titleObj.GetComponent<TextMeshProUGUI>();
            if (bannerFont != null) tmp.font = bannerFont;
            tmp.fontSize = 32;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = titleGoldColor;
            tmp.outlineWidth = 0.16f;
            tmp.outlineColor = Color.black;
            tmp.alignment = TextAlignmentOptions.Center;
            roundTitleText = tmp;
        }
        else if (bannerFont != null && roundTitleText.font != bannerFont)
        {
            roundTitleText.font = bannerFont;
        }

        // Tạo SubText nếu chưa có
        if (roundSubText == null)
        {
            GameObject subObj = new GameObject("SubTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            subObj.transform.SetParent(transform, false);
            var rt = subObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0.42f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            var tmp = subObj.GetComponent<TextMeshProUGUI>();
            if (bannerFont != null) tmp.font = bannerFont;
            tmp.fontSize = 20;
            tmp.fontStyle = FontStyles.Normal;
            tmp.color = subTextColor;
            tmp.outlineWidth = 0.14f;
            tmp.outlineColor = Color.black;
            tmp.alignment = TextAlignmentOptions.Center;
            roundSubText = tmp;
        }
        else if (bannerFont != null && roundSubText.font != bannerFont)
        {
            roundSubText.font = bannerFont;
        }
    }
}
