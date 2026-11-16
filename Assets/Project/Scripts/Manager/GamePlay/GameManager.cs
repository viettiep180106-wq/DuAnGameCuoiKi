using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public UnityAction<bool> OnWinGame;
    public UnityAction<bool> OnLoseGame;

    [Header("UI Popups")]
    public GameObject Win;
    public GameObject Lose;
    public Image LockBoard;

    [Header("Match Settings")]
    [SerializeField] private FillBarUI TimeLimit;
    [SerializeField] private float matchTime = 25f;
    [SerializeField] private int step = 1;
    [SerializeField] private int maxSteps = 10;

    [Header("Buttons")]
    [SerializeField] private Button StartStep;
    [SerializeField] private Button RollBuff;

    public GamePlayState CurrentState { get; private set; } = GamePlayState.Lobby;
    public int CurrentLevel => LevelManager.instance != null ? LevelManager.instance.CurrentLevelNumber : 1;
    public int CurrentStep => step;
    public int TotalSteps => maxSteps;
    public bool IsWaveCombatActive => isWaveCombatActive;

    [Header("Distance Progression")]
    [SerializeField] private float stepTraveledDistance = 0f;
    [SerializeField] private float stepTargetDistance = 6f;

    public event Action<int, int> OnWaveProgressChanged;
    public event Action<int, float, float, int> OnStepDistanceProgress;
    public event Action<GamePlayState> OnGameStateChanged;

    private bool isWaveCombatActive = false;
    private bool isHandlingWaveClear = false;

    private void Awake()
    {
        Application.runInBackground = true;
        Time.timeScale = 1f;
        instance = this;

        if (StartStep == null)
        {
            var btnObj = GameObject.Find("StartStepBtn");
            if (btnObj != null)
            {
                StartStep = btnObj.GetComponent<Button>();
            }
        }

        if (StartStep != null)
        {
            StartStep.onClick.RemoveAllListeners();
            StartStep.onClick.AddListener(OnStartStepClicked);
        }

        OnLoseGame += LoseGame;
        OnWinGame += WinGame;
    }

    private void Start()
    {
        if (LevelManager.instance != null)
        {
            maxSteps = LevelManager.instance.TotalSteps;
        }

        // Khởi đầu game theo chuẩn GDD: Player ở giữa màn hình (Lobby), hiện nút [BẮT ĐẦU]
        SetState(GamePlayState.Lobby);

        Player player = Player.Instance != null ? Player.Instance : FindFirstObjectByType<Player>();
        if (player != null)
        {
            player.AlignToMarchingPosition(0f);
            var anim = player.GetComponent<PlayerAnimationController>();
            if (anim != null) anim.ActiveAnimation(TypeAnimationAtivity.isFly);
        }

        if (MainSceneUIController.Instance != null)
        {
            MainSceneUIController.Instance.ShowStartCombatButton();
        }
    }

    private void Update()
    {
        if (CurrentState == GamePlayState.GameOverWin || CurrentState == GamePlayState.GameOverLose)
        {
            return;
        }

        // Lưu ý theo chuẩn GDD Turn-based mới: 
        // Tuyệt đối không tự động clear wave theo quãng đường thời gian tại đây.
        // Wave chiến đấu CHỈ kết thúc khi toàn bộ quái trong đội hình bị tiêu diệt!
    }

    public void StartNewWave(int waveIndex)
    {
        if (CurrentState == GamePlayState.GameOverWin || CurrentState == GamePlayState.GameOverLose)
        {
            return;
        }

        step = waveIndex;
        isWaveCombatActive = false;
        isHandlingWaveClear = false;

        OnWaveProgressChanged?.Invoke(step, maxSteps);

        if (LockBoard != null)
        {
            LockBoard.gameObject.SetActive(false);
        }

        if (MainSceneUIController.Instance != null)
        {
            MainSceneUIController.Instance.StartWaveOpeningSequence(step);
        }
        else
        {
            if (LevelManager.instance != null)
            {
                LevelManager.instance.OnEquipPreMatch?.Invoke(step);
            }
            if (BoardController.instance != null)
            {
                BoardController.instance.SlideInBoard();
            }
            SetState(GamePlayState.MatchBoard);
        }
    }

    public void OnStartStepClicked()
    {
        if (CurrentState == GamePlayState.GameOverWin || CurrentState == GamePlayState.GameOverLose) return;

        if (MainSceneUIController.Instance != null)
        {
            MainSceneUIController.Instance.OnStartStepButtonClicked();
            return;
        }

        if (CurrentState == GamePlayState.Lobby)
        {
            StartNewWave(1);
            return;
        }

        StartCombat();
    }

    public void TriggerCombat()
    {
        StartCombat();
    }

    public void StartCombat()
    {
        SetState(GamePlayState.Combat);
        isWaveCombatActive = true;

        // Giữ vững tiến trình Wave hiện tại khi bước vào trận đấu, không reset về 0
        OnWaveProgressChanged?.Invoke(step, maxSteps);

        if (LockBoard != null)
        {
            LockBoard.gameObject.SetActive(false);
        }

        if (TimeLimit != null && TimeLimit.uiImage != null)
        {
            TimeLimit.uiImage.DOKill();
        }

        // Bàn cờ mở khóa tương tác và trượt lên khớp vị trí DialoguePanel
        if (BoardController.instance != null)
        {
            BoardController.instance.UnlockBoardInteractions();
            BoardController.instance.SlideInBoard();
        }

        // Kích hoạt trạng thái bay chiến đấu của người chơi khi nhấn button bắt đầu wave (isFly)
        Player player = Player.Instance != null ? Player.Instance : FindFirstObjectByType<Player>();
        if (player != null)
        {
            var anim = player.GetComponent<PlayerAnimationController>();
            if (anim != null)
            {
                anim.ActiveAnimation(TypeAnimationAtivity.isFly);
            }
        }

        // Quái vật di chuyển về phía người chơi
        if (EnemyController.instance != null)
        {
            EnemyController.instance.StartActive();
        }

        if (LevelManager.instance != null)
        {
            LevelManager.instance.OnStepStart?.Invoke(step);
        }

        // Khởi động hệ thống chiến đấu Turn-based 5v1 theo chuẩn GDD
        if (CombatTurnManager.Instance == null)
        {
            GameObject ctmObj = new GameObject("CombatTurnManager", typeof(CombatTurnManager));
            ctmObj.transform.SetParent(transform.parent != null ? transform.parent : transform);
        }
        CombatTurnManager.Instance.StartCombatWave();
    }

    public void HandleWaveCleared()
    {
        if (CurrentState == GamePlayState.GameOverWin || CurrentState == GamePlayState.GameOverLose)
        {
            return;
        }

        isWaveCombatActive = false;

        // 1. Các skill chưa được kích hoạt sẽ không được dùng nữa
        if (SkillPassiveManager.instance != null)
        {
            SkillPassiveManager.instance.CancelPendingAttacks();
        }

        // 2. Khóa bàn cờ (stone trên bàn không thể match) & trượt bàn cờ xuống
        if (BoardController.instance != null)
        {
            BoardController.instance.LockBoardInteractions();
            BoardController.instance.SlideOutBoard();
        }

        // 3. Chuyển Player về trạng thái đứng nghỉ (isIdel) khi kết thúc wave
        Player player = Player.Instance != null ? Player.Instance : FindFirstObjectByType<Player>();
        if (player != null)
        {
            var anim = player.GetComponent<PlayerAnimationController>();
            if (anim != null)
            {
                anim.ActiveAnimation(TypeAnimationAtivity.isIdel);
            }
        }

        void ProceedAfterEnemiesCleared()
        {
            if (LevelManager.instance != null)
            {
                LevelManager.instance.OnStepEnd?.Invoke(step);
            }

            void OnVictoryDialogueCompleted()
            {
                if (step >= maxSteps)
                {
                    // Vượt qua toàn bộ Wave của màn chơi -> CHIẾN THẮNG MÀN CHƠI!
                    OnWinGame?.Invoke(true);
                }
                else
                {
                    // Vượt qua Wave 1..9 -> Thưởng Lõi Năng Lượng & dừng lại chờ người chơi nhấn [BẮT ĐẦU]
                    if (Player.Instance != null)
                    {
                        Player.Instance.AddEnergy(10);
                    }

                    SetState(GamePlayState.InterWaveReward);

                    // Hiển thị nút CTA [BẮT ĐẦU] để người chơi chủ động bấm sang wave tiếp theo
                    if (MainSceneUIController.Instance != null)
                    {
                        MainSceneUIController.Instance.ShowStartCombatButton();
                    }
                }
            }

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.PlayVictoryNarrative(step, OnVictoryDialogueCompleted);
            }
            else
            {
                OnVictoryDialogueCompleted();
            }
        }

        // Đẩy quái wave cũ ra ngoài rìa màn hình với tốc độ khoảng 3s rồi bỏ vào pool
        if (EnemyController.instance != null)
        {
            EnemyController.instance.PushOutAndClearOldEnemies(10f, 3.0f, ProceedAfterEnemiesCleared);
        }
        else
        {
            ProceedAfterEnemiesCleared();
        }
    }

    public void SetState(GamePlayState newState)
    {
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);
        GameStateEvents.OnStateChanged?.Invoke(newState);
    }

    public void PlayTime(float duration)
    {
        if (TimeLimit != null)
        {
            TimeLimit.SetFillInstant(1f);
            TimeLimit.DoFill(0f, duration);
        }
    }

    public void LoseGame(bool set)
    {
        SetState(GamePlayState.GameOverLose);
        isWaveCombatActive = false;
        isHandlingWaveClear = false;

        // Ngắt toàn bộ hoạt động gameplay và sự kiện chuyển wave khi game kết thúc
        StopAllCoroutines();
        CancelInvoke();

        if (LevelManager.instance != null)
        {
            LevelManager.instance.StopAllCoroutines();
        }
        if (EnemyController.instance != null)
        {
            EnemyController.instance.StopAllCoroutines();
        }
        if (ActiveSkillPassive.instance != null)
        {
            ActiveSkillPassive.instance.StopCombat();
        }
        if (InterWaveEventManager.Instance != null)
        {
            InterWaveEventManager.Instance.StopAllCoroutines();
        }
        if (CameraWavePreview.Instance != null)
        {
            CameraWavePreview.Instance.ResetCameraPosition();
        }
        if (BoardController.instance != null)
        {
            BoardController.instance.StopAllCoroutines();
        }
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StopAllCoroutines();
            DialogueManager.Instance.HideDialogue();
        }

        if (Lose != null) Lose.SetActive(set);
    }

    public void WinGame(bool set)
    {
        SetState(GamePlayState.GameOverWin);
        isWaveCombatActive = false;
        isHandlingWaveClear = false;

        // Dừng toàn bộ hoạt động gameplay khi chiến thắng
        StopAllCoroutines();
        CancelInvoke();

        if (LevelManager.instance != null)
        {
            LevelManager.instance.StopAllCoroutines();
        }
        if (EnemyController.instance != null)
        {
            EnemyController.instance.StopAllCoroutines();
        }
        if (ActiveSkillPassive.instance != null)
        {
            ActiveSkillPassive.instance.StopCombat();
        }
        if (InterWaveEventManager.Instance != null)
        {
            InterWaveEventManager.Instance.StopAllCoroutines();
        }
        if (BoardController.instance != null)
        {
            BoardController.instance.StopAllCoroutines();
        }

        if (Win != null) Win.SetActive(set);
    }

    public void PauseGame(bool pause)
    {
        Time.timeScale = pause ? 0f : 1f;
    }
}
