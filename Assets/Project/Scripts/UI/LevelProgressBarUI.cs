using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class LevelProgressBarUI : MonoBehaviour
{
    public static LevelProgressBarUI Instance { get; private set; }
    [Header("Scroll View Components")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform viewportRect;
    [SerializeField] private RectTransform contentRect;
    [SerializeField] private RectTransform milestoneContainer;
    [SerializeField] private GameObject milestonePrefab;
    [SerializeField] private Image trackBgImage;
    [SerializeField] private Image trackFillImage;
    [SerializeField] private TMP_Text currentWaveText;

    [Header("Layout Settings")]
    [Tooltip("Khoảng cách giữa các wave. 150f đảm bảo hiển thị 3 - 4 wave cùng lúc trong viewport")]
    [SerializeField] private float stepSpacing = 150f;
    [SerializeField] private float viewportWidth = 460f;
    [SerializeField] private float viewportHeight = 65f;
    [SerializeField] private float trackHeight = 22f;
    [SerializeField] private float milestoneSize = 56f;
    [SerializeField] private Sprite fillSprite;
    [SerializeField] private Sprite bgSprite;

    [Header("Milestone Sprites")]
    [SerializeField] private Sprite whiteCircleSprite;
    [SerializeField] private Sprite capybaraMarkerSprite;
    [SerializeField] private Sprite skullMarkerSprite;

    [Header("Player Avatar Marker (Đầu mút tiến trình)")]
    [SerializeField] private Image playerAvatarMarker;
    [SerializeField] private Sprite playerAvatarSprite;

    [Header("Milestone Colors")]
    [SerializeField] private Color completedColor = new Color(0.18f, 0.78f, 0.38f, 1f); // Xanh lá hoàn thành
    [SerializeField] private Color currentColor = new Color(1f, 0.82f, 0.15f, 1f);    // Vàng cam nổi bật
    [SerializeField] private Color pendingColor = new Color(0.35f, 0.38f, 0.42f, 0.9f); // Xám chờ
    [SerializeField] private Color bossColor = new Color(1f, 0.3f, 0.25f, 1f);        // Đỏ Boss
    [SerializeField] private Color trackBgColor = new Color(0.08f, 0.10f, 0.12f, 1f);   // NỀN ĐEN theo yêu cầu người dùng
    [SerializeField] private Color trackFillColor = new Color(0.18f, 0.91f, 0.65f, 1f);  // Xanh ngọc phát sáng

    [Header("Element Colors")]
    [SerializeField] private bool colorByElement = true;
    [SerializeField] private Color metalColor = new Color(0.92f, 0.92f, 0.95f, 1f); // Kim: Trắng bạc
    [SerializeField] private Color woodColor = new Color(0.20f, 0.82f, 0.38f, 1f);  // Mộc: Xanh lá
    [SerializeField] private Color waterColor = new Color(0.15f, 0.60f, 0.98f, 1f); // Thủy: Xanh lam
    [SerializeField] private Color fireColor = new Color(1f, 0.35f, 0.15f, 1f);     // Hỏa: Đỏ cam
    [SerializeField] private Color earthColor = new Color(0.95f, 0.65f, 0.18f, 1f); // Thổ: Vàng nâu

    private readonly List<Image> milestoneImages = new List<Image>();
    private readonly List<TMP_Text> milestoneTexts = new List<TMP_Text>();
    private readonly List<Image> milestoneMarkers = new List<Image>();
    private int totalStepsCount = 10;
    private int lastScrollWave = -1;

#if UNITY_EDITOR
    private void OnValidate()
    {
        TryLoadDefaultSprites();
    }
#endif

    public void TryLoadDefaultSprites()
    {
#if UNITY_EDITOR
        if (fillSprite == null)
        {
            fillSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/slider gameplay_fill_green.png");
        }
        if (bgSprite == null)
        {
            bgSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/GeneratedUI/track_progressbar_bg.png");
            if (bgSprite == null)
            {
                bgSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/slider gameplay_fill_green.png");
            }
        }
        if (whiteCircleSprite == null)
        {
            whiteCircleSprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        }
        if (capybaraMarkerSprite == null)
        {
            capybaraMarkerSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/GeneratedUI/icon_milestone_capybara.png");
        }
        if (skullMarkerSprite == null)
        {
            skullMarkerSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/GeneratedUI/icon_milestone_skull.png");
        }
        if (playerAvatarSprite == null)
        {
            playerAvatarSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/GeneratedUI/avatar_circle_capybara.png");
        }
#endif
        if (fillSprite == null)
        {
            // Fallback tạo Sprite từ Texture trắng nếu không tìm thấy asset
            fillSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        }
        if (bgSprite == null)
        {
            bgSprite = fillSprite;
        }
    }

    public Color GetElementColor(ElementType element)
    {
        return element switch
        {
            ElementType.Metal => metalColor,
            ElementType.Wood => woodColor,
            ElementType.Water => waterColor,
            ElementType.Fire => fireColor,
            ElementType.Earth => earthColor,
            _ => Color.white
        };
    }

    public ElementType GetWaveElement(int waveNum)
    {
        if (LevelManager.instance != null)
        {
            var stepData = LevelManager.instance.GetStepData(waveNum);
            if (stepData != null)
            {
                return stepData.GetEnemyElement();
            }
        }
        return ElementType.Wood;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        TryLoadDefaultSprites();
        EnsureScrollViewHierarchy();
    }

    private void Start()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.OnWaveProgressChanged += UpdateProgress;
            GameManager.instance.OnStepDistanceProgress += UpdateDistanceProgress;

            totalStepsCount = Mathf.Max(1, GameManager.instance.TotalSteps);
            InitMilestones(totalStepsCount);
            if (GameManager.instance.CurrentState == GamePlayState.Lobby)
            {
                SetLobbyInitialState();
            }
            else
            {
                UpdateProgress(GameManager.instance.CurrentStep, totalStepsCount);
            }
        }
    }

    private void OnDestroy()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.OnWaveProgressChanged -= UpdateProgress;
            GameManager.instance.OnStepDistanceProgress -= UpdateDistanceProgress;
        }

        if (contentRect != null) contentRect.DOKill();
        if (trackFillImage != null) trackFillImage.DOKill();
    }

    /// <summary>
    /// Đảm bảo cấu trúc phân cấp ScrollView / Viewport / Content / Track / Milestones chuẩn xác
    /// </summary>
    public void EnsureScrollViewHierarchy()
    {
        RectTransform myRect = GetComponent<RectTransform>();
        if (myRect != null && myRect.rect.width > 100f)
        {
            viewportWidth = myRect.rect.width;
        }

        // 1. Text thông báo wave
        if (currentWaveText == null)
        {
            Transform wt = transform.Find("WaveText");
            if (wt != null) currentWaveText = wt.GetComponent<TMP_Text>();
        }

        if (currentWaveText != null)
        {
            RectTransform wtRect = currentWaveText.GetComponent<RectTransform>();
            wtRect.anchorMin = new Vector2(0.5f, 1f);
            wtRect.anchorMax = new Vector2(0.5f, 1f);
            wtRect.pivot = new Vector2(0.5f, 1f);
            wtRect.anchoredPosition = new Vector2(0f, 26f);
            wtRect.sizeDelta = new Vector2(360f, 26f);
            currentWaveText.fontSize = 18f;
            currentWaveText.fontStyle = FontStyles.Bold;
            currentWaveText.alignment = TextAlignmentOptions.Center;
        }

        // 2. Tạo hoặc cấu hình ScrollView
        Transform svTrans = transform.Find("ScrollView");
        GameObject svObj;
        if (svTrans == null)
        {
            svObj = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            svObj.transform.SetParent(transform, false);
        }
        else
        {
            svObj = svTrans.gameObject;
        }

        RectTransform svRect = svObj.GetComponent<RectTransform>();
        svRect.anchorMin = new Vector2(0f, 0f);
        svRect.anchorMax = new Vector2(1f, 1f);
        svRect.offsetMin = new Vector2(0f, -8f);
        svRect.offsetMax = new Vector2(0f, -8f);
        svRect.pivot = new Vector2(0.5f, 0.5f);

        scrollRect = svObj.GetComponent<ScrollRect>();
        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = false;
        scrollRect.scrollSensitivity = 15f;

        // 3. Viewport (có RectMask2D để giới hạn chỉ thấy 3 - 4 wave)
        Transform vpTrans = svObj.transform.Find("Viewport");
        GameObject vpObj;
        if (vpTrans == null)
        {
            vpObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            vpObj.transform.SetParent(svObj.transform, false);
        }
        else
        {
            vpObj = vpTrans.gameObject;
            if (vpObj.GetComponent<RectMask2D>() == null)
            {
                vpObj.AddComponent<RectMask2D>();
            }
        }

        viewportRect = vpObj.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportRect.anchoredPosition = Vector2.zero;
        viewportRect.pivot = new Vector2(0.5f, 0.5f);
        scrollRect.viewport = viewportRect;

        // 4. Content
        Transform contentTrans = vpObj.transform.Find("Content");
        GameObject contentObj;
        if (contentTrans == null)
        {
            contentObj = new GameObject("Content", typeof(RectTransform));
            contentObj.transform.SetParent(vpObj.transform, false);
        }
        else
        {
            contentObj = contentTrans.gameObject;
        }

        contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 0.5f);
        contentRect.anchorMax = new Vector2(0f, 0.5f);
        contentRect.pivot = new Vector2(0f, 0.5f);
        scrollRect.content = contentRect;

        // Di dời Milestones cũ nếu có vào trong Content
        Transform oldMilestones = transform.Find("Milestones");
        if (oldMilestones != null && oldMilestones.parent != contentObj.transform)
        {
            var oldLayout = oldMilestones.GetComponent<HorizontalLayoutGroup>();
            if (oldLayout != null) DestroyImmediate(oldLayout);

            oldMilestones.SetParent(contentObj.transform, false);
            milestoneContainer = oldMilestones.GetComponent<RectTransform>();
        }

        if (milestoneContainer == null)
        {
            Transform mTrans = contentObj.transform.Find("Milestones");
            if (mTrans == null)
            {
                GameObject mObj = new GameObject("Milestones", typeof(RectTransform));
                mObj.transform.SetParent(contentObj.transform, false);
                milestoneContainer = mObj.GetComponent<RectTransform>();
            }
            else
            {
                milestoneContainer = mTrans.GetComponent<RectTransform>();
            }
        }

        milestoneContainer.anchorMin = new Vector2(0f, 0.5f);
        milestoneContainer.anchorMax = new Vector2(0f, 0.5f);
        milestoneContainer.pivot = new Vector2(0f, 0.5f);

        // 5. Thanh ray nền và thanh Fill
        EnsureTrackAndFill(contentObj.transform);
    }

    private void EnsureTrackAndFill(Transform contentParent)
    {
        Transform orphanTrack = transform.Find("ProgressBarTrack");
        if (orphanTrack != null && orphanTrack.parent != contentParent)
        {
            DestroyImmediate(orphanTrack.gameObject);
        }

        Transform trackTrans = contentParent.Find("ProgressBarTrack");
        GameObject trackObj;
        if (trackTrans == null)
        {
            trackObj = new GameObject("ProgressBarTrack", typeof(RectTransform), typeof(Image));
            trackObj.transform.SetParent(contentParent, false);
        }
        else
        {
            trackObj = trackTrans.gameObject;
        }

        RectTransform trackRect = trackObj.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0f, 0.5f);
        trackRect.anchorMax = new Vector2(0f, 0.5f);
        trackRect.pivot = new Vector2(0f, 0.5f);

        trackBgImage = trackObj.GetComponent<Image>();
        if (bgSprite != null) trackBgImage.sprite = bgSprite;
        trackBgImage.type = Image.Type.Sliced;
        trackBgImage.color = trackBgColor;

        Transform fillTrans = trackObj.transform.Find("ProgressFill");
        GameObject fillObj;
        if (fillTrans == null)
        {
            fillObj = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(trackObj.transform, false);
        }
        else
        {
            fillObj = fillTrans.gameObject;
        }

        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;

        trackFillImage = fillObj.GetComponent<Image>();
        if (fillSprite != null) trackFillImage.sprite = fillSprite;
        trackFillImage.type = Image.Type.Filled;
        trackFillImage.fillMethod = Image.FillMethod.Horizontal;
        trackFillImage.fillOrigin = 0; // Chạy chuẩn từ Trái sang Phải (0 -> 1)
        trackFillImage.color = trackFillColor;

        trackObj.transform.SetSiblingIndex(0);
        if (milestoneContainer != null)
        {
            milestoneContainer.SetSiblingIndex(1);
        }

        // 5. Player Avatar Marker (Chạy ở đầu mút thanh fill)
        Transform avatarTrans = contentRect.Find("PlayerAvatarMarker");
        GameObject avatarObj;
        if (avatarTrans == null)
        {
            avatarObj = new GameObject("PlayerAvatarMarker", typeof(RectTransform), typeof(Image));
            avatarObj.transform.SetParent(contentRect, false);
        }
        else
        {
            avatarObj = avatarTrans.gameObject;
        }

        playerAvatarMarker = avatarObj.GetComponent<Image>();
        if (playerAvatarSprite != null) playerAvatarMarker.sprite = playerAvatarSprite;
        playerAvatarMarker.preserveAspect = true;

        RectTransform avRt = playerAvatarMarker.GetComponent<RectTransform>();
        avRt.anchorMin = new Vector2(0f, 0.5f);
        avRt.anchorMax = new Vector2(0f, 0.5f);
        avRt.pivot = new Vector2(0.5f, 0.5f);
        avRt.sizeDelta = new Vector2(52f, 52f);
        avatarObj.transform.SetAsLastSibling();
    }

    /// <summary>
    /// Khởi tạo và căn chỉnh các điểm mốc (Milestones) theo khoảng cách chuẩn stepSpacing
    /// </summary>
    public void InitMilestones(int totalSteps)
    {
        totalStepsCount = Mathf.Max(1, totalSteps);
        EnsureScrollViewHierarchy();

        float effectiveVpWidth = (viewportRect != null && viewportRect.rect.width > 50f) 
            ? viewportRect.rect.width 
            : viewportWidth;

        // Tự động bảo vệ stepSpacing để luôn luôn hiển thị 3 đến 4 wave cùng lúc
        if (stepSpacing > 200f || stepSpacing < 80f)
        {
            stepSpacing = Mathf.Clamp(effectiveVpWidth / 3.8f, 130f, 160f);
        }

        float paddingSide = effectiveVpWidth * 0.5f;
        float startPointX = paddingSide - stepSpacing;
        float trackTotalLength = totalStepsCount * stepSpacing;
        float totalContentWidth = trackTotalLength + (paddingSide * 2f);

        if (contentRect != null)
        {
            contentRect.sizeDelta = new Vector2(totalContentWidth, viewportHeight);
        }

        // Thanh ray nền đen bắt đầu từ Điểm Xuất Phát (startPointX) đến Node cuối (Boss)
        if (trackBgImage != null)
        {
            RectTransform trackRect = trackBgImage.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0f, 0.5f);
            trackRect.anchorMax = new Vector2(0f, 0.5f);
            trackRect.pivot = new Vector2(0f, 0.5f);
            trackRect.anchoredPosition = new Vector2(startPointX, 0f);
            trackRect.sizeDelta = new Vector2(trackTotalLength, trackHeight);
            trackBgImage.color = trackBgColor;
        }

        // Thanh Fill chiếm trọn vẹn trackBgImage (ProgressFill là con của ProgressBarTrack)
        if (trackFillImage != null)
        {
            RectTransform fillRect = trackFillImage.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = Vector2.zero;
        }

        if (milestoneContainer != null)
        {
            milestoneContainer.sizeDelta = new Vector2(totalContentWidth, viewportHeight);
            milestoneContainer.anchoredPosition = Vector2.zero;

            // Nút Start Point (Điểm xuất phát trước Wave 1)
            Transform startNodeTrans = milestoneContainer.Find("Node_Start");
            GameObject startNodeObj;
            if (startNodeTrans == null)
            {
                startNodeObj = new GameObject("Node_Start", typeof(RectTransform), typeof(Image));
                startNodeObj.transform.SetParent(milestoneContainer, false);
            }
            else
            {
                startNodeObj = startNodeTrans.gameObject;
            }
            RectTransform startRt = startNodeObj.GetComponent<RectTransform>();
            startRt.anchorMin = new Vector2(0f, 0.5f);
            startRt.anchorMax = new Vector2(0f, 0.5f);
            startRt.pivot = new Vector2(0.5f, 0.5f);
            startRt.anchoredPosition = new Vector2(startPointX, 0f);
            startRt.sizeDelta = new Vector2(milestoneSize * 0.82f, milestoneSize * 0.82f);
            Image startImg = startNodeObj.GetComponent<Image>();
            if (whiteCircleSprite != null) startImg.sprite = whiteCircleSprite;
            startImg.color = new Color(0.25f, 0.32f, 0.42f, 1f);

            TMP_Text startTxt = startNodeObj.GetComponentInChildren<TMP_Text>();
            if (startTxt == null)
            {
                GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtObj.transform.SetParent(startNodeObj.transform, false);
                startTxt = txtObj.GetComponent<TextMeshProUGUI>();
            }
            startTxt.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            startTxt.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            startTxt.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            startTxt.rectTransform.sizeDelta = new Vector2(50f, 30f);
            startTxt.rectTransform.anchoredPosition = Vector2.zero;
            startTxt.fontSize = 11f;
            startTxt.fontStyle = FontStyles.Bold;
            startTxt.alignment = TextAlignmentOptions.Center;
            startTxt.text = "START";
            startTxt.color = Color.white;
        }

        milestoneImages.Clear();
        milestoneTexts.Clear();
        milestoneMarkers.Clear();

        List<Transform> existingNodes = new List<Transform>();
        if (milestoneContainer != null)
        {
            foreach (Transform child in milestoneContainer)
            {
                existingNodes.Add(child);
            }
        }

        for (int i = 0; i < totalStepsCount; i++)
        {
            GameObject nodeObj;
            if (i < existingNodes.Count)
            {
                nodeObj = existingNodes[i].gameObject;
                nodeObj.SetActive(true);
            }
            else
            {
                if (milestonePrefab != null)
                {
                    nodeObj = Instantiate(milestonePrefab, milestoneContainer);
                }
                else
                {
                    nodeObj = new GameObject($"Node_{i + 1}", typeof(RectTransform), typeof(Image));
                    nodeObj.transform.SetParent(milestoneContainer, false);
                }
            }

            nodeObj.name = $"Node_{i + 1}";
            RectTransform nodeRect = nodeObj.GetComponent<RectTransform>();
            nodeRect.anchorMin = new Vector2(0f, 0.5f);
            nodeRect.anchorMax = new Vector2(0f, 0.5f);
            nodeRect.pivot = new Vector2(0.5f, 0.5f);

            float nodeX = paddingSide + (i * stepSpacing);
            nodeRect.anchoredPosition = new Vector2(nodeX, 0f);

            bool isBoss = (i == totalStepsCount - 1);
            float currentSize = isBoss ? milestoneSize * 1.08f : milestoneSize;
            nodeRect.sizeDelta = new Vector2(currentSize, currentSize);

            Image nodeImg = nodeObj.GetComponent<Image>();
            if (nodeImg != null)
            {
                nodeImg.preserveAspect = true;
                milestoneImages.Add(nodeImg);
            }

            // Top Marker (Capybara / Skull icon trên đầu mốc)
            Transform markerTrans = nodeObj.transform.Find("TopMarker");
            GameObject markerObj;
            if (markerTrans == null)
            {
                markerObj = new GameObject("TopMarker", typeof(RectTransform), typeof(Image));
                markerObj.transform.SetParent(nodeObj.transform, false);
            }
            else
            {
                markerObj = markerTrans.gameObject;
            }

            RectTransform markerRt = markerObj.GetComponent<RectTransform>();
            markerRt.anchorMin = new Vector2(0.5f, 0.5f);
            markerRt.anchorMax = new Vector2(0.5f, 0.5f);
            markerRt.pivot = new Vector2(0.5f, 0.5f);
            markerRt.anchoredPosition = new Vector2(0f, 40f);
            markerRt.sizeDelta = new Vector2(40f, 36f);

            Image markerImg = markerObj.GetComponent<Image>();
            markerImg.preserveAspect = true;
            milestoneMarkers.Add(markerImg);

            TMP_Text nodeText = nodeObj.GetComponentInChildren<TMP_Text>();
            if (nodeText == null)
            {
                GameObject textObj = new GameObject("WaveNum", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(nodeObj.transform, false);
                nodeText = textObj.GetComponent<TextMeshProUGUI>();
            }

            RectTransform textRect = nodeText.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(50f, 50f);
            textRect.anchoredPosition = Vector2.zero;

            nodeText.margin = Vector4.zero;
            nodeText.alignment = TextAlignmentOptions.Center;
            nodeText.verticalAlignment = VerticalAlignmentOptions.Middle;
            nodeText.horizontalAlignment = HorizontalAlignmentOptions.Center;
            nodeText.fontStyle = FontStyles.Bold;
            nodeText.fontSize = 24f;
            nodeText.text = $"{i + 1}";
            milestoneTexts.Add(nodeText);
        }

        for (int i = totalStepsCount; i < existingNodes.Count; i++)
        {
            existingNodes[i].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Đưa thanh tiến trình về trạng thái Lobby ban đầu (vạch 0, avatar ở mốc START)
    /// </summary>
    public void SetLobbyInitialState()
    {
        totalStepsCount = Mathf.Max(1, GameManager.instance != null ? GameManager.instance.TotalSteps : 10);
        if (currentWaveText != null)
        {
            currentWaveText.text = "Chuẩn bị vào trận";
        }
        if (trackFillImage != null)
        {
            trackFillImage.DOKill();
            trackFillImage.fillAmount = 0f;
        }
        UpdateAvatarPosition(0f);
        ScrollToStart(0f);
        UpdateMilestoneVisuals(0, 0f);
    }

    public void ScrollToStart(float duration = 0.5f)
    {
        if (contentRect == null) return;
        contentRect.DOKill();
        float targetX = stepSpacing;
        if (duration <= 0f || !Application.isPlaying)
        {
            contentRect.anchoredPosition = new Vector2(targetX, contentRect.anchoredPosition.y);
        }
        else
        {
            contentRect.DOAnchorPosX(targetX, duration).SetEase(Ease.OutCubic);
        }
    }

    public void UpdateAvatarPosition(float overallFill)
    {
        if (playerAvatarMarker == null || trackBgImage == null) return;
        RectTransform avRt = playerAvatarMarker.rectTransform;
        float trackStartX = trackBgImage.rectTransform.anchoredPosition.x;
        float trackWidth = trackBgImage.rectTransform.sizeDelta.x;
        float currentX = trackStartX + (trackWidth * overallFill);
        avRt.anchoredPosition = new Vector2(currentX, 0f);
        playerAvatarMarker.transform.SetAsLastSibling();
    }

    /// <summary>
    /// Tự động cuộn ScrollView để icon của wave hiện tại luôn nằm vào CHÍNH GIỮA viewport
    /// </summary>
    public void ScrollToWave(int waveIndex, float duration = 0.5f)
    {
        if (contentRect == null || totalStepsCount <= 1) return;

        waveIndex = Mathf.Clamp(waveIndex, 1, totalStepsCount);
        lastScrollWave = waveIndex;

        float targetX = -(waveIndex - 1) * stepSpacing;
        float maxScroll = -(totalStepsCount - 1) * stepSpacing;
        targetX = Mathf.Clamp(targetX, maxScroll, 0f);

        contentRect.DOKill();
        if (duration <= 0f || !Application.isPlaying)
        {
            contentRect.anchoredPosition = new Vector2(targetX, contentRect.anchoredPosition.y);
        }
        else
        {
            contentRect.DOAnchorPosX(targetX, duration).SetEase(Ease.OutCubic);
        }
    }

    /// <summary>
    /// Cập nhật trạng thái hiển thị của các mốc và tiến độ khi sang wave mới
    /// </summary>
    public void UpdateProgress(int currentWave, int totalWaves)
    {
        totalStepsCount = Mathf.Max(1, totalWaves);

        if (currentWaveText != null)
        {
            currentWaveText.text = $"Ải {currentWave} / {totalStepsCount}";
        }

        // Fill bar chuẩn từ 0 đến 1:
        float targetFill = totalStepsCount > 0 
            ? Mathf.Clamp01((float)currentWave / (float)totalStepsCount) 
            : 0f;

        if (trackFillImage != null)
        {
            if (!Application.isPlaying)
            {
                trackFillImage.fillAmount = targetFill;
            }
            else
            {
                trackFillImage.DOKill();
                trackFillImage.DOFillAmount(targetFill, 0.4f).SetEase(Ease.OutCubic);
            }
        }

        UpdateAvatarPosition(targetFill);

        // Tự động cuộn icon wave hiện tại vào giữa Viewport
        ScrollToWave(currentWave, 0.5f);

        UpdateMilestoneVisuals(currentWave, 0f);
    }

    /// <summary>
    /// Cập nhật fill bar theo quãng đường người chơi di chuyển thực tế (chuẩn từ 0 đến 1, Zero-GC per frame)
    /// </summary>
    public void UpdateDistanceProgress(int currentWave, float currentDistance, float targetDistance, int totalWaves)
    {
        totalStepsCount = Mathf.Max(1, totalWaves);
        float stepFraction = targetDistance > 0 ? Mathf.Clamp01(currentDistance / targetDistance) : 0f;

        if (currentWaveText != null)
        {
            if (currentWave <= 1 && stepFraction < 0.99f && GameManager.instance != null && GameManager.instance.CurrentState == GamePlayState.Lobby)
            {
                currentWaveText.text = "Chuẩn bị vào trận";
            }
            else
            {
                currentWaveText.text = $"Ải {currentWave} / {totalStepsCount}";
            }
        }

        // Fill bar chạy từ 0 đến 1 trên toàn bộ tiến trình (N segments):
        float overallFill = totalStepsCount > 0 
            ? Mathf.Clamp01(((currentWave - 1) + stepFraction) / (float)totalStepsCount) 
            : 0f;

        if (trackFillImage != null)
        {
            // Gán trực tiếp mỗi frame để tối ưu hiệu năng và cập nhật mượt mà realtime (không tạo rác GC Tween)
            trackFillImage.fillAmount = overallFill;
        }

        UpdateAvatarPosition(overallFill);

        if (lastScrollWave != currentWave)
        {
            ScrollToWave(currentWave, 0.45f);
        }

        UpdateMilestoneVisuals(currentWave, stepFraction);
    }

    private void UpdateMilestoneVisuals(int currentWave, float stepFraction)
    {
        for (int i = 0; i < milestoneImages.Count; i++)
        {
            int waveNum = i + 1;
            Image img = milestoneImages[i];
            if (img == null) continue;

            bool isBoss = (waveNum == totalStepsCount);
            Transform t = img.transform;
            TMP_Text txt = i < milestoneTexts.Count ? milestoneTexts[i] : null;
            Image marker = i < milestoneMarkers.Count ? milestoneMarkers[i] : null;
            RectTransform txtRt = txt != null ? txt.GetComponent<RectTransform>() : null;

            if (whiteCircleSprite != null)
            {
                img.sprite = whiteCircleSprite;
            }

            // Reset triệt để RectTransform, Margin và Alignment của WaveNum để căn giữa 100%
            if (txt != null && txtRt != null)
            {
                txtRt.anchorMin = new Vector2(0.5f, 0.5f);
                txtRt.anchorMax = new Vector2(0.5f, 0.5f);
                txtRt.pivot = new Vector2(0.5f, 0.5f);
                txtRt.sizeDelta = new Vector2(50f, 50f);
                txtRt.anchoredPosition = Vector2.zero;
                txt.margin = Vector4.zero;
                txt.text = $"{waveNum}";
                txt.alignment = TextAlignmentOptions.Center;
                txt.verticalAlignment = VerticalAlignmentOptions.Middle;
                txt.horizontalAlignment = HorizontalAlignmentOptions.Center;
                txt.gameObject.SetActive(true);
            }

            // Thuộc tính ngũ hành của wave
            ElementType element = GetWaveElement(waveNum);
            Color elemColor = colorByElement ? GetElementColor(element) : Color.white;

            if (waveNum < currentWave)
            {
                // Mốc đã vượt qua: Hòa sắc nhẹ giữa màu hệ và màu xanh hoàn thành
                img.color = colorByElement ? Color.Lerp(elemColor, completedColor, 0.5f) : completedColor;
                t.localScale = Vector3.one;

                if (txt != null)
                {
                    txt.fontSize = 24f;
                    txt.fontStyle = FontStyles.Bold;
                    txt.color = Color.white;
                }

                if (marker != null)
                {
                    marker.gameObject.SetActive(false);
                }
            }
            else if (waveNum == currentWave)
            {
                // Mốc hiện tại: Màu rực rỡ theo thuộc tính của wave
                img.color = elemColor;

                float scalePulse = 1.08f + (stepFraction * 0.08f);
                t.localScale = Vector3.one * scalePulse;

                if (txt != null)
                {
                    txt.fontSize = 26f;
                    txt.fontStyle = FontStyles.Bold;
                    // Tự động tính độ sáng (luminance) để đảm bảo độ tương phản cao nhất
                    float lum = (elemColor.r * 0.299f) + (elemColor.g * 0.587f) + (elemColor.b * 0.114f);
                    txt.color = lum > 0.65f ? new Color(0.15f, 0.10f, 0.05f, 1f) : Color.white;
                }

                if (marker != null)
                {
                    marker.gameObject.SetActive(true);
                    if (capybaraMarkerSprite != null) marker.sprite = capybaraMarkerSprite;
                    marker.color = Color.white;
                }
            }
            else
            {
                // Mốc chờ (Pending): Màu thuộc tính mờ / xám chờ để nhận biết trước hệ của ải sắp tới
                Color pendingElemColor = colorByElement ? Color.Lerp(elemColor, pendingColor, 0.65f) : pendingColor;
                img.color = pendingElemColor;
                t.localScale = Vector3.one;

                if (txt != null)
                {
                    txt.fontSize = 22f;
                    txt.fontStyle = FontStyles.Bold;
                    txt.color = new Color(0.9f, 0.95f, 1f, 0.9f);
                }

                if (marker != null)
                {
                    if (isBoss)
                    {
                        marker.gameObject.SetActive(true);
                        if (skullMarkerSprite != null) marker.sprite = skullMarkerSprite;
                        marker.color = Color.white;
                    }
                    else
                    {
                        marker.gameObject.SetActive(false);
                    }
                }
            }
        }
    }
}
