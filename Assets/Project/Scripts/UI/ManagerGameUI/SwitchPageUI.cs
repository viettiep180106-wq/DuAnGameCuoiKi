using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

public class SwitchPageUI : MonoBehaviour
{
    [System.Serializable]
    public class PageUI
    {
        public Toggle tabToggle;
        public RectTransform pageRect; // Kéo GameObject Page vào đây

        [HideInInspector] public RectTransform toggleRect;
        [HideInInspector] public RectTransform iconRect;
        [HideInInspector] public RectTransform bgRect;
        [HideInInspector] public LayoutElement layoutElement;
        [HideInInspector] public float initialIconY;
    }

    [Header("Pages Settings")]
    public List<PageUI> pages = new List<PageUI>();

    [Header("Page Slide Tween")]
    [Tooltip("Thời gian trượt trang và hiệu ứng chuyển tab")]
    public float tweenDuration = 0.4f;
    public Ease easeType = Ease.OutCubic;

    [Header("Tab Size & Animation Settings")]
    public Vector2 activeTabSize = new Vector2(220f, 200f);
    public Vector2 inactiveTabSize = new Vector2(180f, 200f);
    public Vector3 activeTabScale = new Vector3(1.1f, 1.1f, 1f);
    public Vector3 inactiveTabScale = Vector3.one;

    [Header("Icon & BG Settings")]
    public Vector2 activeIconSize = new Vector2(170f, 170f);
    public Vector2 inactiveIconSize = new Vector2(120f, 120f);
    public float activeIconOffsetY = 50f;

    private float slideDistance;
    private int currentActiveIndex = -1;
    private bool isMovingForward = true;
    private RectTransform containerRect;

    private void Start()
    {
        slideDistance = Screen.width;

        if (pages.Count > 0 && pages[0].tabToggle != null && pages[0].tabToggle.transform.parent != null)
        {
            containerRect = pages[0].tabToggle.transform.parent as RectTransform;
        }

        for (int i = 0; i < pages.Count; i++)
        {
            int index = i;
            PageUI page = pages[i];

            // Cache các component của từng Tab để tránh gọi Find/GetComponent nhiều lần
            page.toggleRect = page.tabToggle.GetComponent<RectTransform>();
            Transform iconTrans = page.tabToggle.transform.Find("icon");
            Transform bgTrans = page.tabToggle.transform.Find("BG");

            if (iconTrans != null)
            {
                page.iconRect = iconTrans.GetComponent<RectTransform>();
                page.initialIconY = page.iconRect.anchoredPosition.y;
            }

            if (bgTrans != null)
            {
                page.bgRect = bgTrans.GetComponent<RectTransform>();
            }

            // Tự động lấy hoặc thêm LayoutElement để hoạt động mượt với HorizontalLayoutGroup
            page.layoutElement = page.tabToggle.GetComponent<LayoutElement>();
            if (page.layoutElement == null)
            {
                page.layoutElement = page.tabToggle.gameObject.AddComponent<LayoutElement>();
            }

            bool isStartingPage = page.tabToggle.isOn;
            if (isStartingPage)
            {
                currentActiveIndex = index;
                page.pageRect.anchoredPosition = new Vector2(0, page.pageRect.anchoredPosition.y);
                page.pageRect.gameObject.SetActive(true);
            }
            else
            {
                page.pageRect.gameObject.SetActive(false);
            }

            // Khởi tạo trạng thái UI ban đầu ngay lập tức (duration = 0) cho từng tab
            ApplyTabVisualState(index, isStartingPage, 0f);

            // Đăng ký event
            page.tabToggle.onValueChanged.AddListener((isOn) => OnToggleChanged(index, isOn));
        }

        // Nếu chưa có tab nào bật sẵn, mặc định bật tab đầu tiên
        if (currentActiveIndex == -1 && pages.Count > 0)
        {
            currentActiveIndex = 0;
            pages[0].tabToggle.isOn = true;
            pages[0].pageRect.anchoredPosition = new Vector2(0, pages[0].pageRect.anchoredPosition.y);
            pages[0].pageRect.gameObject.SetActive(true);
            ApplyTabVisualState(0, true, 0f);
        }
    }

    private void OnToggleChanged(int index, bool isOn)
    {
        pages[index].pageRect.DOKill();

        if (isOn)
        {
            if (currentActiveIndex == index) return;

            isMovingForward = index > currentActiveIndex;
            bool wasActive = pages[index].pageRect.gameObject.activeSelf;

            pages[index].pageRect.gameObject.SetActive(true);

            if (!wasActive)
            {
                float startPosX = isMovingForward ? slideDistance : -slideDistance;
                pages[index].pageRect.anchoredPosition = new Vector2(startPosX, pages[index].pageRect.anchoredPosition.y);
            }

            pages[index].pageRect.DOAnchorPosX(0, tweenDuration).SetEase(easeType);
            currentActiveIndex = index;
        }
        else
        {
            float endPosX = isMovingForward ? -slideDistance : slideDistance;

            pages[index].pageRect.DOAnchorPosX(endPosX, tweenDuration)
                .SetEase(easeType)
                .OnComplete(() =>
                {
                    pages[index].pageRect.gameObject.SetActive(false);
                });
        }

        // Cập nhật hiệu ứng riêng cho đúng tab vừa thay đổi trạng thái
        SetStatusToggleUI(index, isOn);
    }

    public void SetStatusToggleUI(int idx, bool isOn)
    {
        if (idx < 0 || idx >= pages.Count) return;
        ApplyTabVisualState(idx, isOn, tweenDuration);
    }

    private void ApplyTabVisualState(int idx, bool isOn, float duration)
    {
        PageUI page = pages[idx];

        Vector2 targetTabSize = isOn ? activeTabSize : inactiveTabSize;
        Vector3 targetTabScale = isOn ? activeTabScale : inactiveTabScale;
        Vector2 targetIconSize = isOn ? activeIconSize : inactiveIconSize;
        float targetIconY = isOn ? (page.initialIconY + activeIconOffsetY) : page.initialIconY;
        Vector3 targetBgScale = isOn ? Vector3.zero : Vector3.one;

        // Dừng các tween cũ trên tab này để tránh xung đột khi bấm liên tục
        if (page.toggleRect != null) page.toggleRect.DOKill();
        if (page.layoutElement != null) page.layoutElement.DOKill();
        if (page.iconRect != null) page.iconRect.DOKill();
        if (page.bgRect != null) page.bgRect.DOKill();

        if (duration <= 0f)
        {
            if (page.toggleRect != null)
            {
                page.toggleRect.sizeDelta = targetTabSize;
                page.toggleRect.localScale = targetTabScale;
            }
            if (page.layoutElement != null)
            {
                page.layoutElement.preferredWidth = targetTabSize.x;
                page.layoutElement.preferredHeight = targetTabSize.y;
            }
            if (page.iconRect != null)
            {
                page.iconRect.sizeDelta = targetIconSize;
                page.iconRect.anchoredPosition = new Vector2(page.iconRect.anchoredPosition.x, targetIconY);
            }
            if (page.bgRect != null)
            {
                page.bgRect.localScale = targetBgScale;
            }
            if (containerRect != null)
            {
                LayoutRebuilder.MarkLayoutForRebuild(containerRect);
            }
            return;
        }

        // 1. Phóng to kích thước của chính Option (hỗ trợ cả RectTransform lẫn HorizontalLayoutGroup)
        if (page.toggleRect != null)
        {
            page.toggleRect.DOSizeDelta(targetTabSize, duration)
                .SetEase(easeType)
                .OnUpdate(() =>
                {
                    if (containerRect != null)
                    {
                        LayoutRebuilder.MarkLayoutForRebuild(containerRect);
                    }
                });

            page.toggleRect.DOScale(targetTabScale, duration).SetEase(easeType);
        }

        if (page.layoutElement != null)
        {
            page.layoutElement.DOPreferredSize(targetTabSize, duration).SetEase(easeType);
        }

        // 2. Hiệu ứng Icon phóng to và nhô lên theo tọa độ gốc (không bị cộng dồn lệch vị trí)
        if (page.iconRect != null)
        {
            page.iconRect.DOSizeDelta(targetIconSize, duration).SetEase(easeType);
            page.iconRect.DOAnchorPosY(targetIconY, duration).SetEase(easeType);
        }

        // 3. Hiệu ứng ẩn/hiện BG
        if (page.bgRect != null)
        {
            page.bgRect.DOScale(targetBgScale, duration).SetEase(easeType);
        }
    }

    private void OnDestroy()
    {
        foreach (var page in pages)
        {
            if (page.tabToggle != null)
            {
                page.tabToggle.onValueChanged.RemoveAllListeners();
            }
        }
    }
}