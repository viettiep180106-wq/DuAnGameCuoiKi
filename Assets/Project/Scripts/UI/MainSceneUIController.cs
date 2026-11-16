using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class MainSceneUIController : MonoBehaviour
{
    public static MainSceneUIController Instance { get; private set; }

    [Header("Popups")]
    [SerializeField] private SettingsPopup settingsPopup;
    [SerializeField] private PausePopup pausePopup;
    [SerializeField] private BuffRewardPopup buffPopup;

    [Header("Header Buttons")]
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button pauseButton;

    [Header("Main CTA Button (Màn hình 1)")]
    [SerializeField] private Button startStepButton;

    [Header("Board Controls (Màn hình 3)")]
    [SerializeField] private Button exitBoardButton;
    [SerializeField] private GameObject coinCounter;

    [Header("Progress & Stats")]
    [SerializeField] private LevelProgressBarUI progressBarUI;

    private Tween pulseTween;
    private bool isIntroTriggered = false;
    private int _lastTrackedEnergy = -1;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        AutoResolveReferences();

        if (settingsButton != null && settingsPopup != null)
        {
            settingsButton.onClick.AddListener(settingsPopup.OpenPopup);
        }

        if (pauseButton != null && pausePopup != null)
        {
            pauseButton.onClick.AddListener(pausePopup.OpenPauseMenu);
        }

        if (startStepButton != null)
        {
            startStepButton.onClick.RemoveAllListeners();
            startStepButton.onClick.AddListener(OnStartStepButtonClicked);
            startStepButton.gameObject.SetActive(true);
        }

        if (exitBoardButton != null)
        {
            exitBoardButton.onClick.AddListener(OnExitBoardClicked);
        }

        if (coinCounter != null)
        {
            // Lõi năng lượng chỉ hiển thị trên PlayerStatsBar, không hiển thị trên Top dưới progress bar
            coinCounter.SetActive(false);
        }

        // Tự động đảm bảo CameraWavePreview hoạt động trên Main Camera
        if (CameraWavePreview.Instance == null)
        {
            var mainCam = Camera.main;
            if (mainCam != null && mainCam.GetComponent<CameraWavePreview>() == null)
            {
                mainCam.gameObject.AddComponent<CameraWavePreview>();
            }
        }
    }

    private void AutoResolveReferences()
    {
        if (settingsPopup == null) settingsPopup = GetComponentInChildren<SettingsPopup>(true);
        if (pausePopup == null) pausePopup = GetComponentInChildren<PausePopup>(true);
        if (buffPopup == null) buffPopup = GetComponentInChildren<BuffRewardPopup>(true);
        if (progressBarUI == null) progressBarUI = GetComponentInChildren<LevelProgressBarUI>(true);
        if (settingsButton == null) settingsButton = transform.Find("TopHeader/SettingsBtn")?.GetComponent<Button>();
        if (pauseButton == null) pauseButton = transform.Find("TopHeader/ExitBtn")?.GetComponent<Button>();
        if (coinCounter == null) coinCounter = transform.Find("TopHeader/CoinCounter")?.gameObject;

        if (startStepButton == null)
        {
            var btnObj = transform.Find("BottomSection/DialoguePanel/Bottom/StartStepBtn")
                ?? transform.Find("BottomSection/ContentRow/DialoguePanel/Bottom/StartStepBtn");
            if (btnObj != null)
            {
                startStepButton = btnObj.GetComponent<Button>();
            }
            else
            {
                var allButtons = GetComponentsInChildren<Button>(true);
                foreach (var b in allButtons)
                {
                    if (b.name == "StartStepBtn")
                    {
                        startStepButton = b;
                        break;
                    }
                }
            }
        }
    }

    private void Start()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.OnGameStateChanged += HandleGameStateChanged;
        }

        if (Player.Instance != null)
        {
            Player.Instance.OnEnergyCoresChanged += UpdateCoinUI;
            _lastTrackedEnergy = Player.Instance.EnergyCores;
            UpdateCoinUI(_lastTrackedEnergy);
        }

        if (GameManager.instance == null || GameManager.instance.CurrentState == GamePlayState.Lobby)
        {
            if (EnemyController.instance != null)
            {
                EnemyController.instance.ClearAllEnemies();
            }
            ShowStartCombatButton();
        }
    }

    private void Update()
    {
        // Đồng bộ liên tục 100% số lõi năng lượng với PlayerStatsUI, chống lệch số khi chi tiêu ở sự kiện
        if (Player.Instance != null && _lastTrackedEnergy != Player.Instance.EnergyCores)
        {
            _lastTrackedEnergy = Player.Instance.EnergyCores;
            UpdateCoinUI(_lastTrackedEnergy);
        }
    }

    public void OnStartStepButtonClicked()
    {
        if (startStepButton != null)
        {
            pulseTween?.Kill();
            startStepButton.gameObject.SetActive(false);
        }

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.HideDialogue();
        }

        if (GameManager.instance != null && GameManager.instance.CurrentState == GamePlayState.Lobby)
        {
            // Bắt đầu Wave 1 từ màn hình Lobby ban đầu
            GameManager.instance.StartNewWave(1);
        }
        else if (GameManager.instance != null && GameManager.instance.CurrentState == GamePlayState.InterWaveReward)
        {
            // Chuyển sang wave tiếp theo sau khi thắng wave trước và nhấn [BẮT ĐẦU]
            int nextStep = GameManager.instance.CurrentStep + 1;
            GameManager.instance.StartNewWave(nextStep);
        }
        else
        {
            // Trượt bàn cờ 6x6 lên vị trí chiến đấu và kích hoạt trận đấu Turn-based
            if (BoardController.instance != null)
            {
                BoardController.instance.SlideInBoard(0.45f, () =>
                {
                    if (GameManager.instance != null)
                    {
                        GameManager.instance.TriggerCombat();
                    }
                });
            }
            else
            {
                if (GameManager.instance != null)
                {
                    GameManager.instance.TriggerCombat();
                }
            }
        }
    }

    private void StartPreWaveSequence(int currentStep, float duration, Action onComplete)
    {
        // 1. Căn Player về giữa màn hình (Marching Position) và kích hoạt Player Fly
        Player player = Player.Instance != null ? Player.Instance : FindFirstObjectByType<Player>();
        if (player != null)
        {
            player.AlignToMarchingPosition(0.35f);
            var anim = player.GetComponent<PlayerAnimationController>();
            if (anim != null) anim.ActiveAnimation(TypeAnimationAtivity.isFly);
        }

        // 2. Đảm bảo Camera ở tâm ban đầu trong giai đoạn Pre-Wave
        if (CameraWavePreview.Instance != null)
        {
            CameraWavePreview.Instance.PanToPreWaveView();
        }

        // 3. Parallax cuộn nền ngược chiều trong giai đoạn hành tiến
        CombatBackgroundScroller scroller = FindFirstObjectByType<CombatBackgroundScroller>();
        if (scroller != null)
        {
            scroller.StartMarching();
            scroller.SetManualOverride(true, true);
        }

        // 4. Fill thanh tiến trình mượt mà từ 0 -> 100% trong 3.0s và tự cuộn sang node wave tiếp theo
        int totalSteps = GameManager.instance != null ? GameManager.instance.TotalSteps : 10;
        var pBar = LevelProgressBarUI.Instance != null ? LevelProgressBarUI.Instance : FindFirstObjectByType<LevelProgressBarUI>();
        if (pBar != null)
        {
            pBar.ScrollToWave(currentStep, 1.8f);
        }

        Tween fillTween = DOVirtual.Float(0f, 1f, duration, (val) =>
        {
            if (pBar != null)
            {
                pBar.UpdateDistanceProgress(currentStep, val, 1f, totalSteps);
            }
        }).SetEase(Ease.Linear);

        // 5. Đợi đủ 3.0s rồi dừng nền và chuyển sang In-Wave
        DOVirtual.DelayedCall(duration, () =>
        {
            fillTween?.Kill();
            if (pBar != null)
            {
                pBar.UpdateDistanceProgress(currentStep, 1f, 1f, totalSteps);
                pBar.UpdateProgress(currentStep, totalSteps);
                pBar.ScrollToWave(currentStep, 0.4f);
            }

            // DỪNG CUỘN NỀN HOÀN TOÀN KHI VÀO WAVE
            if (scroller != null)
            {
                scroller.StopMarching();
                scroller.SetManualOverride(false, false);
            }

            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// Hiển thị nút CTA [BẮT ĐẦU] để người chơi ấn vào trận sau khi hoàn thành chuẩn bị đầu wave và sự kiện nhận buff/debuff
    /// </summary>
    public void ShowStartCombatButton()
    {
        if (startStepButton == null)
        {
            AutoResolveReferences();
        }

        if (startStepButton != null)
        {
            Transform cur = startStepButton.transform;
            while (cur != null && cur != transform.parent)
            {
                if (!cur.gameObject.activeSelf) cur.gameObject.SetActive(true);
                cur = cur.parent;
            }

            startStepButton.gameObject.SetActive(true);
            startStepButton.transform.localScale = Vector3.one;
            pulseTween?.Kill();
            pulseTween = startStepButton.transform
                .DOScale(Vector3.one * 1.08f, 0.7f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
    }

    /// <summary>
    /// Luồng Game Loop chuẩn xác theo GDD Sheet 'Loop&GamePlay':
    /// - Giai đoạn 1 (Pre-Wave 3s): Player Fly ở giữa màn hình, Nền Parallax cuộn ngược chiều, Thanh tiến trình fill 0 -> 100%.
    /// - Giai đoạn 2 (In-Wave): Nền dừng lại hoàn toàn.
    ///   + Nếu Khám Phá: Player đứng giữa -> Hiện Banner Khám Phá -> Thoại Narrative -> Kích hoạt Sự Kiện (Roll/Altar/Đấu Thú/Rương) -> Chuyển wave.
    ///   + Nếu Chiến Đấu: Player dạt sang rìa trái (quay mặt sang phải) -> Camera Pan sang phải căn giữa 2 phe (Player rìa trái, Enemy rìa phải) -> Banner Chiến Đấu -> Thoại Combat -> Banner Bắt đầu -> Bàn cờ 6x6 trượt lên -> Turn-based 5v1.
    /// </summary>
    public void StartWaveOpeningSequence(int currentStep)
    {
        if (GameManager.instance != null && 
           (GameManager.instance.CurrentState == GamePlayState.GameOverWin || GameManager.instance.CurrentState == GamePlayState.GameOverLose))
        {
            return;
        }

        isIntroTriggered = true;
        if (startStepButton != null && startStepButton.gameObject.activeSelf)
        {
            startStepButton.gameObject.SetActive(false);
        }

        // Đảm bảo dọn sạch quái vật trong suốt giai đoạn Pre-Wave (3.0s bay hành tiến)
        if (EnemyController.instance != null)
        {
            EnemyController.instance.ClearAllEnemies();
        }

        StepData stepData = LevelManager.instance != null ? LevelManager.instance.GetStepData(currentStep) : null;
        bool isDiscovery = stepData != null && stepData.GetWaveType() == WaveType.Discovery;

        // Bước 1: Giai đoạn Pre-Wave 3.0s (Player bay ở giữa, Parallax cuộn nền, Fill thanh tiến trình)
        StartPreWaveSequence(currentStep, 3.0f, () =>
        {
            Player player = Player.Instance != null ? Player.Instance : FindFirstObjectByType<Player>();

            if (isDiscovery)
            {
                // In-Wave Khám Phá: Player đứng nghỉ ở giữa màn hình
                if (player != null)
                {
                    var anim = player.GetComponent<PlayerAnimationController>();
                    if (anim != null) anim.ActiveAnimation(TypeAnimationAtivity.isIdel);
                }

                ShowWaveIntro(() =>
                {
                    StepEventType evt = stepData != null ? stepData.GetEventType() : StepEventType.RuneStone_Roll;
                    if (DialogueManager.Instance != null)
                    {
                        DialogueManager.Instance.PlayDiscoveryNarrative(evt, () =>
                        {
                            TriggerDiscoveryEvent(evt);
                        });
                    }
                    else
                    {
                        TriggerDiscoveryEvent(evt);
                    }
                });
            }
            else
            {
                // In-Wave Chiến Đấu:
                // 1. Chỉ khởi tạo quái vật và bàn cờ khi Player đã hoàn thành 3s bay và bước vào trận đánh
                if (LevelManager.instance != null)
                {
                    LevelManager.instance.SetUpPreMatch(currentStep);
                }

                // 2. Player dạt sang rìa TRÁI màn hình, quay mặt sang PHẢI về phía quái
                if (player != null)
                {
                    player.AlignToCombatPosition(0.45f, () =>
                    {
                        var anim = player.GetComponent<PlayerAnimationController>();
                        if (anim != null) anim.ActiveAnimation(TypeAnimationAtivity.isIdel);
                    });
                }

                // 2. Camera Pan mượt mà sang phải căn giữa 2 phe
                if (CameraWavePreview.Instance != null)
                {
                    CameraWavePreview.Instance.PanToCombatView(() =>
                    {
                        ShowCombatIntroPhase(currentStep);
                    });
                }
                else
                {
                    ShowCombatIntroPhase(currentStep);
                }
            }
        });
    }

    private void TriggerDiscoveryEvent(StepEventType evt)
    {
        Action onDiscoveryComplete = () =>
        {
            // Hoàn thành sự kiện khám phá -> Kết thúc wave thành công, chuyển sang wave kế tiếp
            if (GameManager.instance != null)
            {
                GameManager.instance.HandleWaveCleared();
            }
        };

        if (InterWaveEventManager.Instance != null && evt != StepEventType.None)
        {
            InterWaveEventManager.Instance.TriggerInterWaveEvent(evt, onDiscoveryComplete);
        }
        else if (RuneEventUIController.Instance != null && evt != StepEventType.None)
        {
            RuneEventUIController.Instance.StartRuneEvent(evt, onDiscoveryComplete);
        }
        else
        {
            onDiscoveryComplete();
        }
    }

    private void ShowCombatIntroPhase(int currentStep)
    {
        // 1. Hiện Banner giới thiệu kiểu Wave chiến đấu
        ShowWaveIntro(() =>
        {
            // 2. Khung chat hiển thị lời thoại kịch bản chuẩn bị giao chiến (Pháp Sư & Thánh Vật)
            // LƯU Ý: Bàn cờ CHƯA trượt lên tại đây để người chơi tập trung đọc thoại
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.PlayCombatNarrative(currentStep, () =>
                {
                    OnCombatNarrativeComplete();
                });
            }
            else
            {
                OnCombatNarrativeComplete();
            }
        });
    }

    private void OnCombatNarrativeComplete()
    {
        // 3. Sau khi kết thúc thoại -> Hiện Banner "BẮT ĐẦU CHIẾN ĐẤU!" rực lửa
        if (WaveIntroBannerUI.Instance != null)
        {
            WaveIntroBannerUI.Instance.ShowBattleStartBanner(() =>
            {
                StartCombatAndSlideInBoard();
            });
        }
        else
        {
            StartCombatAndSlideInBoard();
        }
    }

    private void StartCombatAndSlideInBoard()
    {
        // 4. Trượt bàn cờ lên và chính thức bắt đầu trận chiến Turn-based
        if (BoardController.instance != null)
        {
            BoardController.instance.SlideInBoard(0.45f, () =>
            {
                if (GameManager.instance != null && GameManager.instance.CurrentState != GamePlayState.Combat)
                {
                    GameManager.instance.TriggerCombat();
                }
            });
        }
        else
        {
            if (GameManager.instance != null && GameManager.instance.CurrentState != GamePlayState.Combat)
            {
                GameManager.instance.TriggerCombat();
            }
        }
    }

    public void OnExitBoardClicked()
    {
        if (GameManager.instance != null && GameManager.instance.CurrentState == GamePlayState.MatchBoard)
        {
            // Màn hình 4: Bàn cờ trượt xuống ngoài màn hình
            if (BoardController.instance != null)
            {
                BoardController.instance.SlideOutBoard();
            }

            int currentStep = GameManager.instance.CurrentStep;
            StepData stepData = LevelManager.instance != null ? LevelManager.instance.GetStepData(currentStep) : null;
            StepEventType eventType = stepData != null ? stepData.GetEventType() : StepEventType.None;

            if (InterWaveEventManager.Instance != null && eventType != StepEventType.None)
            {
                // Chạy kịch bản Buff / Debuff giữa wave: Sự kiện Phiến Đá Cổ Ngữ
                InterWaveEventManager.Instance.TriggerInterWaveEvent(eventType, () =>
                {
                    // Kết thúc sự kiện -> Người chơi nhấn nút [BẮT ĐẦU] mới vào trận!
                    ShowStartCombatButton();
                });
            }
            else
            {
                // Không có sự kiện -> Người chơi nhấn nút [BẮT ĐẦU] để vào trận
                ShowStartCombatButton();
            }
        }
    }

    private void HandleGameStateChanged(GamePlayState state)
    {
        // Lõi năng lượng chỉ hiển thị trên PlayerStatsBar, không dùng trên Top dưới progress bar
        if (coinCounter != null && coinCounter.activeSelf)
        {
            coinCounter.SetActive(false);
        }
    }

    public void UpdateCoinUI(int amount)
    {
        // Đồng bộ lên PlayerStatsUI trên thanh bar
        if (PlayerStatsUI.Instance != null)
        {
            PlayerStatsUI.Instance.UpdateEnergyUI(amount);
        }
    }

    public void ShowWaveIntro(Action onComplete = null)
    {
        if (WaveIntroBannerUI.Instance != null && GameManager.instance != null)
        {
            int wave = GameManager.instance.CurrentStep;
            int total = GameManager.instance.TotalSteps;

            StepData stepData = LevelManager.instance != null ? LevelManager.instance.GetStepData(wave) : null;

            string difficulty = "Thường";
            string elem = "Hỗn Hợp";

            if (stepData != null)
            {
                if (stepData.GetWaveType() == WaveType.Discovery)
                {
                    difficulty = "KHÁM PHÁ";
                    elem = stepData.GetEventType() switch
                    {
                        StepEventType.RuneStone_Roll => "Roll Chọn Buff",
                        StepEventType.RuneStone_Altar => "Lời Chúc Phúc",
                        StepEventType.BeastBetting => "Cuộc Chiến Đấu Thú",
                        StepEventType.RuneStone_Chest => "Rương Cổ Đại",
                        _ => "Sự Kiện Bí Ẩn"
                    };
                }
                else
                {
                    difficulty = stepData.GetStepDifficulty() switch
                    {
                        StepDifficulty.Easy => "Dễ",
                        StepDifficulty.Normal => "Thường",
                        StepDifficulty.Hard => "Khó",
                        StepDifficulty.VeryHard => "CỰC KHÓ (BOSS)",
                        _ => "Thường"
                    };

                    ElementType enemyElem = stepData.GetEnemyElement();
                    ElementType counterElem = ElementalHelper.GetWeakAgainst(enemyElem);
                    elem = $"{ElementalHelper.GetVietnameseName(enemyElem)} (Khắc: {ElementalHelper.GetVietnameseName(counterElem)})";
                }
            }

            WaveIntroBannerUI.Instance.ShowBanner(wave, total, difficulty, elem, onComplete);
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    [Header("Energy Depleted Notification")]
    [SerializeField] private GameObject energyDepletedToast;

    /// <summary>
    /// Hiển thị thông báo khi người chơi hết Lõi Năng Lượng cho lần match tiếp theo
    /// </summary>
    public void ShowEnergyDepletedNotice()
    {
        if (energyDepletedToast == null)
        {
            Transform existing = transform.Find("EnergyDepletedToast");
            if (existing != null)
            {
                energyDepletedToast = existing.gameObject;
            }
            else
            {
                GameObject toastObj = new GameObject("EnergyDepletedToast", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
                toastObj.transform.SetParent(transform, false);
                var rt = toastObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(640f, 75f);
                rt.anchoredPosition = new Vector2(0f, -170f);

                var img = toastObj.GetComponent<Image>();
                img.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);

                GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtObj.transform.SetParent(toastObj.transform, false);
                var txtRt = txtObj.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.sizeDelta = Vector2.zero;
                txtRt.anchoredPosition = Vector2.zero;

                var tmp = txtObj.GetComponent<TextMeshProUGUI>();
                tmp.text = "[CẢNH BÁO] HẾT NĂNG LƯỢNG! NHẤN [SKIP LƯỢT] ĐỂ ĐỔI LƯỢT";
                tmp.fontSize = 22;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = new Color(1f, 0.85f, 0.35f, 1f);
                tmp.alignment = TextAlignmentOptions.Center;

                energyDepletedToast = toastObj;
            }
        }

        energyDepletedToast.SetActive(true);
        var cg = energyDepletedToast.GetComponent<CanvasGroup>();
        if (cg == null) cg = energyDepletedToast.AddComponent<CanvasGroup>();

        cg.DOKill();
        cg.alpha = 0f;
        energyDepletedToast.transform.DOKill();
        energyDepletedToast.transform.localScale = Vector3.one * 0.85f;

        energyDepletedToast.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        cg.DOFade(1f, 0.25f).OnComplete(() =>
        {
            DOVirtual.DelayedCall(2.2f, () =>
            {
                if (cg != null)
                {
                    cg.DOFade(0f, 0.35f).OnComplete(() =>
                    {
                        if (energyDepletedToast != null) energyDepletedToast.SetActive(false);
                    });
                }
            });
        });
    }

    private void OnDestroy()
    {
        pulseTween?.Kill();
        if (GameManager.instance != null)
        {
            GameManager.instance.OnGameStateChanged -= HandleGameStateChanged;
        }
        if (Player.Instance != null)
        {
            Player.Instance.OnEnergyCoresChanged -= UpdateCoinUI;
        }
    }
}

