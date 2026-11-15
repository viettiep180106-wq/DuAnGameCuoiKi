using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Điều khiển toàn bộ giao diện và animation kịch bản Sự Kiện Phiến Đá Cổ Ngữ (Buff & Debuff)
/// Gồm 5 minigame: Vòng quay Gacha, Lật bài 3 lá, Mở rương 2 phong thư, Lời chúc phúc Tinh linh, Lời nguyền Quỷ.
/// </summary>
public class RuneEventUIController : MonoBehaviour
{
    private static RuneEventUIController _instance;
    public static RuneEventUIController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<RuneEventUIController>(FindObjectsInactive.Include);
                if (_instance != null && !_instance.gameObject.activeSelf)
                {
                    _instance.gameObject.SetActive(true);
                }
            }
            return _instance;
        }
    }

    [Header("Root & Overlay")]
    [SerializeField] private GameObject _eventRoot;
    [SerializeField] private CanvasGroup _overlayCanvasGroup;
    [SerializeField] private Image _glowLightImage;
    [SerializeField] private TMP_Text _instructionText;

    [Header("Phase 1: Phiến Đá Cổ Ngữ")]
    [SerializeField] private GameObject _stonePanel;
    [SerializeField] private Image _stoneImage;
    [SerializeField] private Button _btnOpen25;
    [SerializeField] private Button _btnSkip;
    [SerializeField] private TMP_Text _stoneNoticeText;
    [SerializeField] private Sprite _spriteStoneIntact;
    [SerializeField] private Sprite _spriteStoneCracked;

    [Header("Phase 2 - Minigame 1: Vòng Quay Gacha")]
    [SerializeField] private GameObject _gachaPanel;
    [SerializeField] private RectTransform _wheelTransform;
    [SerializeField] private RectTransform _pointerTransform;
    [SerializeField] private Button _btnStartGacha;
    [SerializeField] private Button _btnCloseGacha;
    [SerializeField] private TMP_Text _gachaResultText;
    [SerializeField] private GameObject _gachaResultContainer;
    [SerializeField] private Image _gachaResultIcon;
    [SerializeField] private TMP_Text _gachaResultTitle;
    [SerializeField] private TMP_Text _gachaResultDesc;
    [SerializeField] private Image[] _wheelSectorIcons = new Image[8];
    private BuffDebuffInfo[] _activeWheelSectors = new BuffDebuffInfo[8];

    [Header("Phase 2 - Minigame 2: Roll Bài (3 Lá)")]
    [SerializeField] private GameObject _rollPanel;
    [SerializeField] private RectTransform _deckTransform;
    [SerializeField] private Image _deckImage;
    [SerializeField] private Button _btnRoll5;
    [SerializeField] private Button _btnCloseRoll;
    [SerializeField] private RectTransform[] _cardTransforms;
    [SerializeField] private Image[] _cardBackgroundImages;
    [SerializeField] private Image[] _cardImages;
    [SerializeField] private TMP_Text[] _cardTexts;
    [SerializeField] private Button[] _cardButtons;
    [SerializeField] private TMP_Text _rollCostText;
    [SerializeField] private Sprite _spriteDeckRoll;
    [SerializeField] private Sprite _spriteDeckShuffle1;
    [SerializeField] private Sprite _spriteDeckShuffle2;
    [SerializeField] private Sprite _spriteCardFrontBase;

    [Header("Phase 2 - Minigame 3: Mở Rương & 2 Phong Thư")]
    [SerializeField] private GameObject _chestPanel;
    [SerializeField] private Image _chestImage;
    [SerializeField] private Button _btnChestOpen;
    [SerializeField] private TMP_Text _chestTapPrompt;
    [SerializeField] private GameObject _envelopeContainer;
    [SerializeField] private Button _btnEnvelopeBuff;
    [SerializeField] private Button _btnEnvelopeDebuff;
    [SerializeField] private Sprite _spriteChestClosed;
    [SerializeField] private Sprite _spriteChestOpen;
    [SerializeField] private Sprite _spriteEnvelope1;
    [SerializeField] private Sprite _spriteEnvelope2;

    [Header("Phase 2 - Minigame 4 & 5: Tinh Linh & Linh Hồn Quỷ")]
    [SerializeField] private GameObject _spiritPanel;
    [SerializeField] private Image _spiritImage;
    [SerializeField] private TMP_Text _spiritTitleText;
    [SerializeField] private TMP_Text _spiritEffectText;
    [SerializeField] private Sprite _spriteSpiritFairy;
    [SerializeField] private Sprite _spriteSpiritDemon;

    [Header("Warning Confirmation Popup")]
    [SerializeField] private GameObject _warningPopup;
    [SerializeField] private Button _btnConfirmExit;
    [SerializeField] private Button _btnCancelExit;

    // Runtime state
    private StepEventType _currentEventType;
    private Action _onEventCompleted;
    private Tween _stoneShakeTween;
    private bool _isFirstRoll = true;
    private BuffDebuffType[] _currentCardBuffs = new BuffDebuffType[3];
    private bool _hasInteractedWithActivity = false;

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

        BindButtons();
        EnsureDefaultSprites();
        if (_eventRoot != null) _eventRoot.SetActive(false);
        if (_warningPopup != null) _warningPopup.SetActive(false);
    }

    private void BindButtons()
    {
        if (_btnOpen25 != null) _btnOpen25.onClick.AddListener(OnOpenStoneClicked);
        if (_btnSkip != null) _btnSkip.onClick.AddListener(OnSkipEventClicked);
        if (_btnStartGacha != null) _btnStartGacha.onClick.AddListener(OnStartGachaClicked);
        if (_btnCloseGacha != null) _btnCloseGacha.onClick.AddListener(PromptExitWarning);
        if (_btnRoll5 != null) _btnRoll5.onClick.AddListener(OnRollCardsClicked);
        if (_btnCloseRoll != null) _btnCloseRoll.onClick.AddListener(PromptExitWarning);
        if (_btnChestOpen != null) _btnChestOpen.onClick.AddListener(OnChestClicked);
        if (_btnEnvelopeBuff != null) _btnEnvelopeBuff.onClick.AddListener(() => OnEnvelopeSelected(true));
        if (_btnEnvelopeDebuff != null) _btnEnvelopeDebuff.onClick.AddListener(() => OnEnvelopeSelected(false));

        if (_btnConfirmExit != null) _btnConfirmExit.onClick.AddListener(ConfirmEarlyExit);
        if (_btnCancelExit != null) _btnCancelExit.onClick.AddListener(CancelEarlyExit);

        if (_cardButtons != null)
        {
            for (int i = 0; i < _cardButtons.Length; i++)
            {
                int idx = i;
                if (_cardButtons[i] != null)
                {
                    _cardButtons[i].onClick.AddListener(() => OnCardSelected(idx));
                }
            }
        }
    }

    private void EnsureDefaultSprites()
    {
#if UNITY_EDITOR
        string basePath = "Assets/Project/Sprite/Events/";
        if (_spriteStoneIntact == null) _spriteStoneIntact = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(basePath + "RuneStone_Intact.png");
        if (_spriteStoneCracked == null) _spriteStoneCracked = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(basePath + "RuneStone_Cracked.png");
        if (_spriteChestClosed == null) _spriteChestClosed = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(basePath + "Chest_Closed.png");
        if (_spriteChestOpen == null) _spriteChestOpen = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(basePath + "Chest_Open.png");
        if (_spriteSpiritFairy == null) _spriteSpiritFairy = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(basePath + "Spirits/Spirit_Fairy.png");
        if (_spriteSpiritDemon == null) _spriteSpiritDemon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(basePath + "Spirits/Spirit_Demon.png");
        if (_spriteDeckRoll == null) _spriteDeckRoll = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(basePath + "Cards/Deck_Roll.png");
        if (_spriteDeckShuffle1 == null) _spriteDeckShuffle1 = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(basePath + "Cards/Deck_Shuffle_1.png");
        if (_spriteDeckShuffle2 == null) _spriteDeckShuffle2 = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(basePath + "Cards/Deck_Shuffle_2.png");
        if (_spriteCardFrontBase == null) _spriteCardFrontBase = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(basePath + "Card_Front_Base.png");
        if (_spriteEnvelope1 == null) _spriteEnvelope1 = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(basePath + "Envelopes/Envelope_1.png");
        if (_spriteEnvelope2 == null) _spriteEnvelope2 = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(basePath + "Envelopes/Envelope_2.png");
#endif
    }

    /// <summary>
    /// Kích hoạt chuỗi sự kiện Phiến Đá Cổ Ngữ
    /// </summary>
    public void StartRuneEvent(StepEventType eventType, Action onCompleted)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        _currentEventType = eventType;
        _onEventCompleted = onCompleted;
        _hasInteractedWithActivity = false;
        _isFirstRoll = true;

        if (_eventRoot != null) _eventRoot.SetActive(true);
        if (_warningPopup != null) _warningPopup.SetActive(false);

        // Đóng các panel con
        HideAllSubPanels();

        // 1. Màn hình tối lại (Dark overlay fade-in)
        if (_overlayCanvasGroup != null)
        {
            _overlayCanvasGroup.alpha = 0f;
            _overlayCanvasGroup.DOFade(0.85f, 0.4f);
        }

        // 2. Hiện phiến đá nguyên vẹn kèm hiệu ứng lắc lư chu kỳ 1s
        if (_stonePanel != null)
        {
            _stonePanel.SetActive(true);
            _stonePanel.transform.localScale = Vector3.zero;
            _stonePanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }

        if (_stoneImage != null && _spriteStoneIntact != null)
        {
            _stoneImage.sprite = _spriteStoneIntact;
        }

        var stoneAnim = _stoneImage != null ? _stoneImage.GetComponent<Animator>() : null;
        if (stoneAnim != null)
        {
            stoneAnim.Play("Idle", 0, 0f);
        }

        if (_stoneNoticeText != null)
        {
            _stoneNoticeText.text = "Phát hiện một phiến đá có cổ ngữ ma pháp!\nSử dụng 25 Lõi Năng Lượng để khai mở?";
        }

        StartStoneIdleShake();

        // Gửi lời thoại nhân vật mở đầu
        DialogueManager.Instance?.AddDirectDialogue("Pháp Sư", "Ồ! Một phiến đá cổ ngữ phát sáng lập lòe! Có vẻ chứa đựng ma pháp bí ẩn!", true);
    }

    private void StartStoneIdleShake()
    {
        _stoneShakeTween?.Kill();
        if (_stoneImage != null)
        {
            Transform t = _stoneImage.transform;
            t.localRotation = Quaternion.identity;

            // Mỗi 1s lại lắc lư 1 lần
            Sequence seq = DOTween.Sequence();
            seq.Append(t.DOShakeRotation(0.35f, new Vector3(0, 0, 9f), 10, 90f));
            seq.AppendInterval(1.0f);
            seq.SetLoops(-1);
            _stoneShakeTween = seq;
        }
    }

    private void HideAllSubPanels()
    {
        if (_stonePanel != null) _stonePanel.SetActive(false);
        if (_gachaPanel != null) _gachaPanel.SetActive(false);
        if (_rollPanel != null) _rollPanel.SetActive(false);
        if (_chestPanel != null) _chestPanel.SetActive(false);
        if (_spiritPanel != null) _spiritPanel.SetActive(false);
        if (_glowLightImage != null) _glowLightImage.gameObject.SetActive(false);
    }

    // --- PHASE 1 BUTTONS ---
    private void OnSkipEventClicked()
    {
        _stoneShakeTween?.Kill();
        DialogueManager.Instance?.AddDirectDialogue("Pháp Sư", "Thôi, tiết kiệm năng lượng chuẩn bị nghênh chiến quái vật!", true);
        CloseEventUI();
    }

    private void OnOpenStoneClicked()
    {
        // Kiểm tra lõi năng lượng
        if (Player.Instance != null && !Player.Instance.TrySpendEnergy(25))
        {
            if (_stoneNoticeText != null)
            {
                _stoneNoticeText.text = "<color=red>Không đủ 25 Lõi Năng Lượng để khai mở!</color>";
                _stoneNoticeText.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
            }
            DialogueManager.Instance?.AddDirectDialogue("Pháp Sư", "Úi chà, không đủ 25 lõi năng lượng rồi! Phải tích thêm thôi!", true);
            return;
        }

        _stoneShakeTween?.Kill();

        // Chạy animation vỡ đá + bừng sáng Glow Light
        StartCoroutine(CrackStoneAndTransitionRoutine());
    }

    private IEnumerator CrackStoneAndTransitionRoutine()
    {
        // 1. Chạy animation phiến đá vỡ tuần tự theo 5 frame hoặc đổi sprite sang phiến đá nứt
        var anim = _stoneImage != null ? _stoneImage.GetComponent<Animator>() : null;
        if (anim != null)
        {
            anim.SetTrigger("Break");
            _stoneImage.transform.DOShakePosition(0.6f, 15f, 14);
        }
        else if (_stoneImage != null && _spriteStoneCracked != null)
        {
            _stoneImage.sprite = _spriteStoneCracked;
            _stoneImage.transform.DOShakePosition(0.6f, 15f, 14);
        }

        // 2. Luồng sáng Glow Light bừng lên giữa màn hình
        if (_glowLightImage != null)
        {
            _glowLightImage.gameObject.SetActive(true);
            Transform gt = _glowLightImage.transform;
            gt.localScale = Vector3.zero;
            _glowLightImage.color = new Color(1f, 1f, 1f, 1f);

            gt.DOScale(Vector3.one * 2.5f, 0.6f).SetEase(Ease.OutQuad);
            _glowLightImage.DOFade(0f, 0.6f).SetDelay(0.3f);
        }

        // Chờ trọn vẹn animation vỡ đá 5 frame diễn ra (1.0s)
        yield return new WaitForSeconds(1.0f);

        // 3. Ẩn phiến đá nứt mượt mà
        if (_stonePanel != null)
        {
            _stonePanel.transform.DOScale(Vector3.zero, 0.35f).SetEase(Ease.InBack);
        }

        yield return new WaitForSeconds(0.35f);
        if (_stonePanel != null) _stonePanel.SetActive(false);
        if (_glowLightImage != null) _glowLightImage.gameObject.SetActive(false);

        // 4. Kích hoạt Minigame tương ứng (scale từ 0 lên 1)
        TriggerActivityMinigame();
    }

    private void TriggerActivityMinigame()
    {
        switch (_currentEventType)
        {
            case StepEventType.RuneStone_Gacha:
                OpenGachaMinigame();
                break;

            case StepEventType.RuneStone_Roll:
                OpenRollCardsMinigame();
                break;

            case StepEventType.RuneStone_Chest:
                OpenChestMinigame();
                break;

            case StepEventType.RuneStone_Altar:
            default:
                // Ngẫu nhiên: 50% Lời chúc phúc (Tinh linh), 50% Lời nguyền (Quỷ)
                if (UnityEngine.Random.value < 0.6f)
                {
                    OpenBlessingMinigame();
                }
                else
                {
                    OpenCurseMinigame();
                }
                break;
        }
    }

    // ==========================================
    // MINIGAME 1: VÒNG QUAY GACHA (3 BUFF / 3 DEBUFF)
    // ==========================================
    private void OpenGachaMinigame()
    {
        if (_gachaPanel == null) return;

        _gachaPanel.SetActive(true);
        _gachaPanel.transform.localScale = Vector3.zero;
        _gachaPanel.transform.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack);

        if (_btnStartGacha != null) _btnStartGacha.interactable = true;
        if (_wheelTransform != null) _wheelTransform.localEulerAngles = Vector3.zero;
        if (_pointerTransform != null) _pointerTransform.localRotation = Quaternion.identity;
        if (_gachaResultContainer != null) _gachaResultContainer.SetActive(false);
        if (_gachaResultText != null) _gachaResultText.text = "Nhấn [START GACHA] để bắt đầu quay!";

        if (_gachaWheelRoutine != null)
        {
            StopCoroutine(_gachaWheelRoutine);
            _gachaWheelRoutine = null;
        }

        if (_wheelSectorIcons != null)
        {
            foreach (var icon in _wheelSectorIcons)
            {
                if (icon != null)
                {
                    icon.transform.DOKill();
                    icon.transform.localScale = Vector3.one;
                }
            }
        }

        // Khởi tạo ngẫu nhiên 4 Buff trên các sector xanh lá (1, 3, 5, 7) và 4 Debuff trên các sector đỏ (0, 2, 4, 6)
        PopulateWheelSectors();

        DialogueManager.Instance?.AddDirectDialogue("Thánh Vật", "Tada! Bàn xoay ma thuật xuất hiện! 4 Buff may mắn và 4 Debuff đan xen! Thử vận may nào!", false);
    }

    private void PopulateWheelSectors()
    {
        if (BuffDebuffDatabase.Instance == null) return;

        List<BuffDebuffInfo> buffs = BuffDebuffDatabase.Instance.GetRandomBuffs(4);
        List<BuffDebuffInfo> debuffs = BuffDebuffDatabase.Instance.GetRandomDebuffs(4);

        int[] buffSectors = { 1, 3, 5, 7 };
        int[] debuffSectors = { 0, 2, 4, 6 };

        for (int i = 0; i < 4; i++)
        {
            int sBuff = buffSectors[i];
            if (i < buffs.Count)
            {
                _activeWheelSectors[sBuff] = buffs[i];
                if (_wheelSectorIcons != null && sBuff < _wheelSectorIcons.Length && _wheelSectorIcons[sBuff] != null)
                {
                    _wheelSectorIcons[sBuff].sprite = buffs[i].icon;
                    _wheelSectorIcons[sBuff].enabled = buffs[i].icon != null;
                }
            }

            int sDebuff = debuffSectors[i];
            if (i < debuffs.Count)
            {
                _activeWheelSectors[sDebuff] = debuffs[i];
                if (_wheelSectorIcons != null && sDebuff < _wheelSectorIcons.Length && _wheelSectorIcons[sDebuff] != null)
                {
                    _wheelSectorIcons[sDebuff].sprite = debuffs[i].icon;
                    _wheelSectorIcons[sDebuff].enabled = debuffs[i].icon != null;
                }
            }
        }
    }

    private Coroutine _gachaWheelRoutine;

    private void OnStartGachaClicked()
    {
        if (_btnStartGacha != null) _btnStartGacha.interactable = false;
        _hasInteractedWithActivity = true;
        if (_gachaResultContainer != null) _gachaResultContainer.SetActive(false);

        // Bàn xoay 8 ô (45 độ mỗi ô):
        // Sector 0 (0 deg, Top/12h): Đỏ (Debuff)
        // Sector 1 (45 deg, Top-Right): Xanh (Buff)
        // Sector 2 (90 deg, Right): Đỏ (Debuff)
        // Sector 3 (135 deg, Bottom-Right): Xanh (Buff)
        // Sector 4 (180 deg, Bottom): Đỏ (Debuff)
        // Sector 5 (225 deg, Bottom-Left): Xanh (Buff)
        // Sector 6 (270 deg, Left): Đỏ (Debuff)
        // Sector 7 (315 deg, Top-Left): Xanh (Buff)

        bool isBuff = UnityEngine.Random.value < 0.5f;
        int[] buffSectors = { 1, 3, 5, 7 };
        int[] debuffSectors = { 0, 2, 4, 6 };
        int winningSector = isBuff 
            ? buffSectors[UnityEngine.Random.Range(0, buffSectors.Length)] 
            : debuffSectors[UnityEngine.Random.Range(0, debuffSectors.Length)];

        BuffDebuffInfo info = (_activeWheelSectors != null && winningSector < _activeWheelSectors.Length)
            ? _activeWheelSectors[winningSector]
            : null;

        if (info == null && BuffDebuffDatabase.Instance != null)
        {
            info = isBuff ? BuffDebuffDatabase.Instance.GetRandomBuff() : BuffDebuffDatabase.Instance.GetRandomDebuff();
        }

        // Quay theo chiều kim đồng hồ để sector trúng đích dừng tại vị trí kim (12h):
        // Góc quay = ((8 - winningSector) % 8) * 45 độ + (360 * 6)
        float targetAngle = (((8 - winningSector) % 8) * 45f) + (360f * 6f);

        if (_gachaWheelRoutine != null) StopCoroutine(_gachaWheelRoutine);
        _gachaWheelRoutine = StartCoroutine(SpinGachaWheelRoutine(targetAngle, winningSector, isBuff, info));
    }

    private IEnumerator SpinGachaWheelRoutine(float targetAngle, int winningSector, bool isBuff, BuffDebuffInfo info)
    {
        float duration = 4.0f;
        float elapsed = 0f;
        float currentAngle = 0f;
        float lastPegAngle = 0f;

        if (_wheelTransform != null)
        {
            _wheelTransform.DOKill();
            _wheelTransform.localEulerAngles = Vector3.zero;
        }

        if (_pointerTransform != null)
        {
            _pointerTransform.DOKill();
            _pointerTransform.localRotation = Quaternion.identity;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // EaseOutCubic: f(t) = 1 - (1 - t)^3 (mượt mà 60fps từ nhanh đến chậm dần)
            float easeT = 1f - Mathf.Pow(1f - t, 3f);
            currentAngle = easeT * targetAngle;

            if (_wheelTransform != null)
            {
                _wheelTransform.localEulerAngles = new Vector3(0f, 0f, -currentAngle);
            }

            // Ticking peg detection: mỗi 45 độ (tương ứng mỗi ranh giới sector đi qua kim chỉ)
            float pegStep = 45f;
            if (currentAngle - lastPegAngle >= pegStep)
            {
                lastPegAngle = Mathf.Floor(currentAngle / pegStep) * pegStep;
                if (_pointerTransform != null)
                {
                    _pointerTransform.DOKill();
                    _pointerTransform.localRotation = Quaternion.Euler(0, 0, 13f);
                    float snapTime = Mathf.Clamp(0.06f * (1f - easeT * 0.6f), 0.035f, 0.12f);
                    _pointerTransform.DOLocalRotate(Vector3.zero, snapTime).SetEase(Ease.OutQuad);
                }
            }

            yield return null;
        }

        // Snap dừng chính xác tuyệt đối tại góc đích
        if (_wheelTransform != null)
        {
            _wheelTransform.localEulerAngles = new Vector3(0f, 0f, -targetAngle);
        }

        if (_pointerTransform != null)
        {
            _pointerTransform.DOKill();
            _pointerTransform.localRotation = Quaternion.identity;
            _pointerTransform.DOPunchScale(Vector3.one * 0.25f, 0.35f);
        }

        // Hiệu ứng ăn mừng: Icon ô trúng thưởng nảy scale 1.35x
        if (_wheelSectorIcons != null && winningSector < _wheelSectorIcons.Length && _wheelSectorIcons[winningSector] != null)
        {
            var winIcon = _wheelSectorIcons[winningSector];
            winIcon.transform.DOKill();
            winIcon.transform.DOScale(Vector3.one * 1.35f, 0.22f).SetLoops(3, LoopType.Yoyo).OnComplete(() =>
            {
                winIcon.transform.localScale = Vector3.one;
            });
        }

        yield return new WaitForSeconds(0.4f);
        _gachaWheelRoutine = null;
        OnGachaWheelStopped(winningSector, isBuff, info);
    }

    private void OnGachaWheelStopped(int sector, bool isBuff, BuffDebuffInfo info)
    {
        if (_pointerTransform != null)
        {
            _pointerTransform.DOKill();
            _pointerTransform.localRotation = Quaternion.identity;
            _pointerTransform.DOPunchScale(Vector3.one * 0.25f, 0.35f);
        }

        if (info == null && BuffDebuffDatabase.Instance != null)
        {
            info = isBuff ? BuffDebuffDatabase.Instance.GetRandomBuff() : BuffDebuffDatabase.Instance.GetRandomDebuff();
        }

        BuffDebuffType resultType = info != null ? info.type : (isBuff ? BuffDebuffType.Buff_CuongHoa_Atk : BuffDebuffType.Debuff_GiamAtk);
        string displayName = info != null ? info.displayName : GetBuffName(resultType);
        string description = info != null ? info.description : "";

        if (isBuff)
        {
            ApplyBuff(resultType);
            DialogueManager.Instance?.AddDirectDialogue("Thánh Vật", $"Tuyệt vời! Bàn xoay dừng lại ở ô phước lành: nhận [{displayName}]!", false);
        }
        else
        {
            ApplyDebuff(resultType);
            DialogueManager.Instance?.AddDirectDialogue("Pháp Sư", $"Ui da! Xui quá, quay trúng ngay [{displayName}]! Lần sau phải rửa tay mới được!", true);
        }

        if (_gachaResultContainer != null)
        {
            _gachaResultContainer.SetActive(true);
            _gachaResultContainer.transform.localScale = Vector3.zero;
            _gachaResultContainer.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }

        if (_gachaResultIcon != null)
        {
            if (info != null && info.icon != null)
            {
                _gachaResultIcon.sprite = info.icon;
                _gachaResultIcon.enabled = true;
            }
            else
            {
                _gachaResultIcon.enabled = false;
            }
        }

        if (_gachaResultTitle != null)
        {
            _gachaResultTitle.text = isBuff 
                ? $"<color=#55ff55>BUFF: {displayName}</color>" 
                : $"<color=#ff5555>DEBUFF: {displayName}</color>";
        }

        if (_gachaResultDesc != null)
        {
            _gachaResultDesc.text = description;
        }

        if (_gachaResultText != null)
        {
            _gachaResultText.text = isBuff 
                ? $"<color=green>CHÚC MỪNG: [{displayName}]</color>" 
                : $"<color=red>XUI XẺO: [{displayName}]</color>";
        }

        DOVirtual.DelayedCall(2.2f, CloseEventUI);
    }

    // ==========================================
    // MINIGAME 2: ROLL BÀI (3 LÁ BÀI)
    // ==========================================
    private void OpenRollCardsMinigame()
    {
        if (_rollPanel == null) return;

        _rollPanel.SetActive(true);
        _rollPanel.transform.localScale = Vector3.zero;
        _rollPanel.transform.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack);

        _isFirstRoll = true;
        UpdateRollCostUI();

        // Ban đầu úp bộ bài giữa màn hình
        if (_deckTransform != null)
        {
            _deckTransform.gameObject.SetActive(true);
            _deckTransform.localScale = Vector3.one;
            if (_deckImage != null && _spriteDeckRoll != null)
            {
                _deckImage.sprite = _spriteDeckRoll;
            }
        }

        if (_cardTransforms != null)
        {
            foreach (var ct in _cardTransforms) if (ct != null) ct.gameObject.SetActive(false);
        }

        DialogueManager.Instance?.AddDirectDialogue("Thánh Vật", "Bộ bài cổ ngữ xuất hiện! Lần chia đầu tiên hoàn toàn MIỄN PHÍ! Chọn lấy 1 lá bài thần kỳ!", false);
    }

    private void UpdateRollCostUI()
    {
        if (_rollCostText != null)
        {
            _rollCostText.text = _isFirstRoll ? "ROLL (Miễn phí)" : "ROLL (5 Lõi)";
        }
    }

    private void OnRollCardsClicked()
    {
        if (!_isFirstRoll)
        {
            if (Player.Instance != null && !Player.Instance.TrySpendEnergy(5))
            {
                if (_instructionText != null) _instructionText.text = "<color=red>Không đủ 5 lõi để đổi bài!</color>";
                return;
            }
        }

        _isFirstRoll = false;
        _hasInteractedWithActivity = true;
        UpdateRollCostUI();

        StartCoroutine(ShuffleAndDealCardsRoutine());
    }

    private IEnumerator ShuffleAndDealCardsRoutine()
    {
        if (_btnRoll5 != null) _btnRoll5.interactable = false;

        // Nếu bài đang mở sẵn (do re-roll), thu nhỏ bài về giữa trước khi xào
        if (_cardTransforms != null)
        {
            bool hasVisibleCard = false;
            foreach (var ct in _cardTransforms)
            {
                if (ct != null && ct.gameObject.activeSelf)
                {
                    hasVisibleCard = true;
                    ct.DOScale(Vector3.zero, 0.2f);
                    ct.DOAnchorPosX(0f, 0.2f);
                }
            }
            if (hasVisibleCard)
            {
                yield return new WaitForSeconds(0.2f);
                foreach (var ct in _cardTransforms)
                {
                    if (ct != null) ct.gameObject.SetActive(false);
                }
            }
        }

        // Animation xào bài lượn sóng & đổi 3 frame: Deck_Roll -> Shuffle_1 -> Shuffle_2
        if (_deckTransform != null)
        {
            _deckTransform.gameObject.SetActive(true);
            _deckTransform.localScale = Vector3.one;

            for (int k = 0; k < 2; k++)
            {
                if (_deckImage != null && _spriteDeckShuffle1 != null) _deckImage.sprite = _spriteDeckShuffle1;
                _deckTransform.DOPunchRotation(new Vector3(0, 0, 10f), 0.12f, 6);
                yield return new WaitForSeconds(0.12f);

                if (_deckImage != null && _spriteDeckShuffle2 != null) _deckImage.sprite = _spriteDeckShuffle2;
                _deckTransform.DOPunchPosition(new Vector3(10f, -5f, 0), 0.12f, 6);
                yield return new WaitForSeconds(0.12f);
            }

            if (_deckImage != null && _spriteDeckRoll != null) _deckImage.sprite = _spriteDeckRoll;
            yield return new WaitForSeconds(0.15f);
        }

        // Sinh 3 buff ngẫu nhiên (có tỉ lệ ra buff hiếm)
        for (int i = 0; i < 3; i++)
        {
            _currentCardBuffs[i] = InterWaveEventManager.Instance != null 
                ? InterWaveEventManager.Instance.GetRandomBuff() 
                : BuffDebuffType.Buff_CuongHoa_Atk;
        }

        // Chia 3 lá bài trải ngang cân đối (-260, 0, +260)
        float[] targetX = { -260f, 0f, 260f };
        for (int i = 0; i < _cardTransforms.Length && i < 3; i++)
        {
            RectTransform ct = _cardTransforms[i];
            if (ct == null) continue;

            ct.gameObject.SetActive(true);
            ct.anchoredPosition = Vector2.zero;
            ct.localScale = Vector3.zero;

            ct.DOAnchorPosX(targetX[i], 0.35f).SetEase(Ease.OutQuad);
            ct.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);

            // Gán background mặt ngửa lá bài (Card_Front_Base.png viền vàng)
            if (_cardBackgroundImages != null && i < _cardBackgroundImages.Length && _cardBackgroundImages[i] != null)
            {
                if (_spriteCardFrontBase != null)
                {
                    _cardBackgroundImages[i].sprite = _spriteCardFrontBase;
                }
            }
            else
            {
                var bgImg = ct.GetComponent<Image>();
                if (bgImg != null && _spriteCardFrontBase != null)
                {
                    bgImg.sprite = _spriteCardFrontBase;
                }
            }

            BuffDebuffInfo cardInfo = BuffDebuffDatabase.Instance != null 
                ? BuffDebuffDatabase.Instance.Get(_currentCardBuffs[i]) 
                : null;

            if (_cardImages != null && i < _cardImages.Length && _cardImages[i] != null)
            {
                if (cardInfo != null && cardInfo.icon != null)
                {
                    _cardImages[i].sprite = cardInfo.icon;
                    _cardImages[i].enabled = true;
                }
            }

            if (_cardTexts != null && i < _cardTexts.Length && _cardTexts[i] != null)
            {
                if (cardInfo != null)
                {
                    _cardTexts[i].text = $"<b>{cardInfo.displayName}</b>\n<size=75%>{cardInfo.description}</size>";
                }
                else
                {
                    _cardTexts[i].text = GetBuffName(_currentCardBuffs[i]);
                }
            }

            if (_cardButtons != null && i < _cardButtons.Length && _cardButtons[i] != null)
            {
                _cardButtons[i].interactable = true;
            }
        }

        if (_deckTransform != null) _deckTransform.gameObject.SetActive(false);
        if (_btnRoll5 != null) _btnRoll5.interactable = true;
        if (_instructionText != null) _instructionText.text = "Chọn 1 trong 3 lá bài để nhận phước lành!";
    }

    private void OnCardSelected(int cardIndex)
    {
        if (cardIndex < 0 || cardIndex >= _currentCardBuffs.Length) return;

        BuffDebuffType selectedBuff = _currentCardBuffs[cardIndex];
        ApplyBuff(selectedBuff);

        BuffDebuffInfo cardInfo = BuffDebuffDatabase.Instance != null 
            ? BuffDebuffDatabase.Instance.Get(selectedBuff) 
            : null;
        string selName = cardInfo != null ? cardInfo.displayName : GetBuffName(selectedBuff);

        // Hiệu ứng lá bài được chọn phóng to, các lá còn lại biến mất
        for (int i = 0; i < _cardTransforms.Length; i++)
        {
            if (_cardTransforms[i] == null) continue;
            if (i == cardIndex)
            {
                _cardTransforms[i].DOScale(Vector3.one * 1.15f, 0.3f);
            }
            else
            {
                _cardTransforms[i].DOScale(Vector3.zero, 0.25f);
            }
        }

        DialogueManager.Instance?.AddDirectDialogue("Pháp Sư", $"Ta chọn lá bài: [{selName}]! Sức mạnh đã được gia tăng!", true);
        DOVirtual.DelayedCall(1.5f, CloseEventUI);
    }

    // ==========================================
    // MINIGAME 3: MỞ RƯƠNG (CHEST & 2 PHONG THƯ)
    // ==========================================
    private void OpenChestMinigame()
    {
        if (_chestPanel == null) return;

        _chestPanel.SetActive(true);
        _chestPanel.transform.localScale = Vector3.zero;
        _chestPanel.transform.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack);

        if (_chestImage != null && _spriteChestClosed != null)
        {
            _chestImage.sprite = _spriteChestClosed;
            _chestImage.transform.localScale = Vector3.one;
            _chestImage.transform.DOShakeRotation(1f, new Vector3(0, 0, 7f)).SetLoops(-1, LoopType.Yoyo);
        }

        var chestAnim = _chestImage != null ? _chestImage.GetComponent<Animator>() : null;
        if (chestAnim != null)
        {
            chestAnim.Play("Idle", 0, 0f);
        }

        if (_chestTapPrompt != null) _chestTapPrompt.text = "Chạm vào rương để mở!";
        if (_envelopeContainer != null) _envelopeContainer.SetActive(false);

        DialogueManager.Instance?.AddDirectDialogue("Thánh Vật", "Một chiếc rương cổ bí ẩn! Mau mở ra xem bên trong có điều kỳ diệu gì!", false);
    }

    private void OnChestClicked()
    {
        if (_btnChestOpen != null) _btnChestOpen.interactable = false;
        _hasInteractedWithActivity = true;

        if (_chestImage != null) _chestImage.transform.DOKill();

        StartCoroutine(OpenChestRoutine());
    }

    private IEnumerator OpenChestRoutine()
    {
        // 1. Chạy animation mở rương tuần tự theo 5 frame phát sáng rực rỡ
        var chestAnim = _chestImage != null ? _chestImage.GetComponent<Animator>() : null;
        if (chestAnim != null)
        {
            chestAnim.SetTrigger("Open");
            _chestImage.transform.DOPunchScale(Vector3.one * 0.15f, 0.6f);
        }
        else if (_chestImage != null && _spriteChestOpen != null)
        {
            _chestImage.sprite = _spriteChestOpen;
            _chestImage.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f);
        }

        // Chờ trọn vẹn animation mở nắp rương và ánh sáng tỏa ra (1.25s)
        yield return new WaitForSeconds(1.25f);

        // 2. Rương thu nhỏ (scale 1 -> 0), 2 phong thư hiện ra (scale 0 -> 1)
        if (_chestImage != null)
        {
            _chestImage.transform.DOScale(Vector3.zero, 0.35f).SetEase(Ease.InBack);
        }

        if (_chestTapPrompt != null) _chestTapPrompt.text = "Chọn một trong hai phong thư!";

        yield return new WaitForSeconds(0.35f);

        if (_envelopeContainer != null)
        {
            if (_btnEnvelopeBuff != null && _spriteEnvelope1 != null)
            {
                var img = _btnEnvelopeBuff.GetComponent<Image>();
                if (img != null) img.sprite = _spriteEnvelope1;
            }
            if (_btnEnvelopeDebuff != null && _spriteEnvelope2 != null)
            {
                var img = _btnEnvelopeDebuff.GetComponent<Image>();
                if (img != null) img.sprite = _spriteEnvelope2;
            }

            _envelopeContainer.SetActive(true);
            _envelopeContainer.transform.localScale = Vector3.zero;
            _envelopeContainer.transform.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack);
        }

        DialogueManager.Instance?.AddDirectDialogue("Thánh Vật", "Tada! Hai phong thư định mệnh: Một thiên thần ban phúc, một ác ma giáng hoạ! Chọn đi bạn ơi!", false);
    }

    private void OnEnvelopeSelected(bool isBuffEnvelope)
    {
        if (_btnEnvelopeBuff != null) _btnEnvelopeBuff.interactable = false;
        if (_btnEnvelopeDebuff != null) _btnEnvelopeDebuff.interactable = false;

        if (isBuffEnvelope)
        {
            BuffDebuffType buff = InterWaveEventManager.Instance != null 
                ? InterWaveEventManager.Instance.GetRandomBuff() 
                : BuffDebuffType.Buff_CuongHoa_Atk;
            ApplyBuff(buff);

            BuffDebuffInfo info = BuffDebuffDatabase.Instance != null ? BuffDebuffDatabase.Instance.Get(buff) : null;
            string dName = info != null ? info.displayName : GetBuffName(buff);
            string dDesc = info != null ? info.description : "";

            if (_chestTapPrompt != null) _chestTapPrompt.text = $"<color=green>Phong thư Thiên Thần: Nhận [{dName}]!\n<size=80%>{dDesc}</size></color>";
            DialogueManager.Instance?.AddDirectDialogue("Pháp Sư", $"Hehe, mắt nhìn của ta chuẩn đét! Phong thư Thiên Thần ban tặng [{dName}]!", true);
        }
        else
        {
            BuffDebuffType debuff = InterWaveEventManager.Instance != null 
                ? InterWaveEventManager.Instance.GetRandomDebuff() 
                : BuffDebuffType.Debuff_GiamAtk;
            ApplyDebuff(debuff);

            BuffDebuffInfo info = BuffDebuffDatabase.Instance != null ? BuffDebuffDatabase.Instance.Get(debuff) : null;
            string dName = info != null ? info.displayName : GetBuffName(debuff);
            string dDesc = info != null ? info.description : "";

            if (_chestTapPrompt != null) _chestTapPrompt.text = $"<color=red>Phong thư Ác Ma: Dính [{dName}]!\n<size=80%>{dDesc}</size></color>";
            DialogueManager.Instance?.AddDirectDialogue("Pháp Sư", $"Á đù! Bốc nhầm phong thư Ác Ma rồi! Dính ngay [{dName}]! Căng thật đấy!", true);
        }

        DOVirtual.DelayedCall(1.8f, CloseEventUI);
    }

    // ==========================================
    // MINIGAME 4: LỜI CHÚC PHÚC (TINH LINH)
    // ==========================================
    private void OpenBlessingMinigame()
    {
        if (_spiritPanel == null) return;

        _spiritPanel.SetActive(true);
        _spiritPanel.transform.localScale = Vector3.zero;
        _spiritPanel.transform.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack);

        if (_spiritImage != null && _spriteSpiritFairy != null)
        {
            _spiritImage.sprite = _spriteSpiritFairy;
            // Hiệu ứng bay bổng bồng bềnh nhẹ nhàng
            Transform t = _spiritImage.transform;
            t.DOKill();
            t.localPosition = Vector3.zero;
            t.DOLocalMoveY(20f, 1.2f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }

        BuffDebuffType buff = InterWaveEventManager.Instance != null 
            ? InterWaveEventManager.Instance.GetRandomBuff() 
            : BuffDebuffType.Buff_CuongHoa_Atk;
        ApplyBuff(buff);

        BuffDebuffInfo info = BuffDebuffDatabase.Instance != null ? BuffDebuffDatabase.Instance.Get(buff) : null;
        string dName = info != null ? info.displayName : GetBuffName(buff);
        string dDesc = info != null ? info.description : "";

        if (_spiritTitleText != null) _spiritTitleText.text = "<color=#80ff80>LỜI CHÚC PHÚC CỦA TINH LINH</color>";
        if (_spiritEffectText != null) _spiritEffectText.text = $"Nhận phước lành ánh sáng: [{dName}]!\n<size=80%>{dDesc}</size>";

        DialogueManager.Instance?.AddDirectDialogue("Tinh Linh", $"Xin chào pháp sư quả cảm! Ta ban tặng ngươi [{dName}] để tiếp tục hành trình!", false);
        DOVirtual.DelayedCall(2.2f, CloseEventUI);
    }

    // ==========================================
    // MINIGAME 5: LỜI NGUYỀN (LINH HỒN QUỶ)
    // ==========================================
    private void OpenCurseMinigame()
    {
        if (_spiritPanel == null) return;

        _spiritPanel.SetActive(true);
        _spiritPanel.transform.localScale = Vector3.zero;
        _spiritPanel.transform.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack);

        if (_spiritImage != null && _spriteSpiritDemon != null)
        {
            _spiritImage.sprite = _spriteSpiritDemon;
            // Hiệu ứng rùng rợn rung lắc
            Transform t = _spiritImage.transform;
            t.DOKill();
            t.DOShakeScale(1.5f, 0.15f, 6).SetLoops(-1);
        }

        BuffDebuffType debuff = InterWaveEventManager.Instance != null 
            ? InterWaveEventManager.Instance.GetRandomDebuff() 
            : BuffDebuffType.Debuff_GiamAtk;
        ApplyDebuff(debuff);

        BuffDebuffInfo info = BuffDebuffDatabase.Instance != null ? BuffDebuffDatabase.Instance.Get(debuff) : null;
        string dName = info != null ? info.displayName : GetBuffName(debuff);
        string dDesc = info != null ? info.description : "";

        if (_spiritTitleText != null) _spiritTitleText.text = "<color=#ff5050>LỜI NGUYỀN CỦA QUỶ DỮ</color>";
        if (_spiritEffectText != null) _spiritEffectText.text = $"Ngươi bị trúng lời nguyền bóng tối: [{dName}]!\n<size=80%>{dDesc}</size>";

        DialogueManager.Instance?.AddDirectDialogue("Quỷ Dữ", $"Khehehe! Kẻ yếu đuối kia, hãy nhận lấy sự đày đọa: [{dName}]!", false);
        DOVirtual.DelayedCall(2.2f, CloseEventUI);
    }

    // ==========================================
    // POPUP CẢNH BÁO THOÁT SỚM
    // ==========================================
    private void PromptExitWarning()
    {
        if (_warningPopup != null)
        {
            _warningPopup.SetActive(true);
            _warningPopup.transform.localScale = Vector3.zero;
            _warningPopup.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        }
    }

    private void CancelEarlyExit()
    {
        if (_warningPopup != null)
        {
            _warningPopup.transform.DOScale(Vector3.zero, 0.2f).OnComplete(() => _warningPopup.SetActive(false));
        }
    }

    private void ConfirmEarlyExit()
    {
        if (_warningPopup != null) _warningPopup.SetActive(false);
        DialogueManager.Instance?.AddDirectDialogue("Pháp Sư", "Đã bỏ dở sự kiện! 25 Lõi Năng Lượng đã không cánh mà bay!", true);
        CloseEventUI();
    }

    private void OnDestroy()
    {
        if (_gachaWheelRoutine != null) StopCoroutine(_gachaWheelRoutine);
        _stoneShakeTween?.Kill();
        if (_wheelTransform != null) _wheelTransform.DOKill();
        if (_pointerTransform != null) _pointerTransform.DOKill();
        if (_deckTransform != null) _deckTransform.DOKill();
        if (_chestImage != null) _chestImage.transform.DOKill();
        if (_spiritImage != null) _spiritImage.transform.DOKill();
        if (_overlayCanvasGroup != null) _overlayCanvasGroup.DOKill();
    }

    private void CloseEventUI()
    {
        if (_gachaWheelRoutine != null) StopCoroutine(_gachaWheelRoutine);
        _stoneShakeTween?.Kill();
        if (_wheelTransform != null) _wheelTransform.DOKill();
        if (_pointerTransform != null) _pointerTransform.DOKill();
        if (_deckTransform != null) _deckTransform.DOKill();
        if (_chestImage != null) _chestImage.transform.DOKill();
        if (_spiritImage != null) _spiritImage.transform.DOKill();

        if (_overlayCanvasGroup != null)
        {
            _overlayCanvasGroup.DOFade(0f, 0.35f).OnComplete(() =>
            {
                if (_eventRoot != null) _eventRoot.SetActive(false);
                _onEventCompleted?.Invoke();
            });
        }
        else
        {
            if (_eventRoot != null) _eventRoot.SetActive(false);
            _onEventCompleted?.Invoke();
        }
    }

    private void ApplyBuff(BuffDebuffType buff)
    {
        if (InterWaveEventManager.Instance != null)
        {
            InterWaveEventManager.Instance.ApplyBuffEffect(buff);
        }
    }

    private void ApplyDebuff(BuffDebuffType debuff)
    {
        if (InterWaveEventManager.Instance != null)
        {
            InterWaveEventManager.Instance.ApplyDebuffEffect(debuff);
        }
    }

    private string GetBuffName(BuffDebuffType b)
    {
        return InterWaveEventManager.Instance != null ? InterWaveEventManager.Instance.GetBuffName(b) : b.ToString();
    }
}
