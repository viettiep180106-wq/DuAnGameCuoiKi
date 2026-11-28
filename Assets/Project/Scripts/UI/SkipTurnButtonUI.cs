using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Nút "Skip Lượt Chiến Đấu" theo chuẩn GDD 5v1:
/// - Nằm trên HUD chiến đấu (độc lập 100% với nút Skip hội thoại của DialogueManager).
/// - Chỉ kích hoạt trong lượt của Player (PlayerTurn).
/// - Viền nút tự động nhấp nháy phát sáng khi Lõi Năng Lượng < 5 để gợi ý người chơi kết thúc lượt.
/// - Khi click: Gọi CombatTurnManager.Instance.SkipPlayerTurn(), khóa bàn cờ và nhường lượt cho phe quái vật.
/// </summary>
public class SkipTurnButtonUI : MonoBehaviour
{
    public static SkipTurnButtonUI Instance { get; private set; }

    [Header("UI Components")]
    [SerializeField] private Button skipButton;
    [SerializeField] private Image buttonBg;
    [SerializeField] private Image outlineGlow;
    [SerializeField] private TMP_Text labelText;

    [Header("Blink Settings")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.65f, 0.95f, 1f);
    [SerializeField] private Color lowEnergyColor = new Color(0.95f, 0.4f, 0.2f, 1f);

    private Tween _glowTween;

    private void Awake()
    {
        Instance = this;

        if (skipButton == null)
        {
            skipButton = GetComponent<Button>();
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipButtonClicked);
        }
    }

    private void Start()
    {
        if (CombatTurnManager.Instance != null)
        {
            CombatTurnManager.Instance.OnTurnStateChanged += HandleTurnStateChanged;
            UpdateVisibility(CombatTurnManager.Instance.CurrentState);
        }

        if (Player.Instance != null)
        {
            Player.Instance.OnEnergyCoresChanged += HandleEnergyChanged;
            CheckLowEnergyBlink(Player.Instance.EnergyCores);
        }
    }

    private void OnDestroy()
    {
        _glowTween?.Kill();
        if (CombatTurnManager.Instance != null)
        {
            CombatTurnManager.Instance.OnTurnStateChanged -= HandleTurnStateChanged;
        }
        if (Player.Instance != null)
        {
            Player.Instance.OnEnergyCoresChanged -= HandleEnergyChanged;
        }
    }

    private void HandleTurnStateChanged(CombatTurnState state)
    {
        UpdateVisibility(state);
    }

    private void UpdateVisibility(CombatTurnState state)
    {
        bool isPlayerTurn = state == CombatTurnState.PlayerTurn;
        gameObject.SetActive(isPlayerTurn);

        if (skipButton != null)
        {
            skipButton.interactable = isPlayerTurn;
        }

        if (isPlayerTurn && Player.Instance != null)
        {
            CheckLowEnergyBlink(Player.Instance.EnergyCores);
        }
        else
        {
            StopBlinking();
        }
    }

    private void HandleEnergyChanged(int currentEnergy)
    {
        if (gameObject.activeInHierarchy)
        {
            CheckLowEnergyBlink(currentEnergy);
        }
    }

    private void CheckLowEnergyBlink(int energy)
    {
        bool canAfford = CombatTurnManager.Instance == null || CombatTurnManager.Instance.CanPlayerAffordNextMatch();
        if (!canAfford)
        {
            StartPulsing();
        }
        else
        {
            StopPulsing();
        }
    }

    public void StartPulsing()
    {
        StartBlinking();
        transform.DOKill();
        transform.DOScale(1.08f, 0.4f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }

    public void StopPulsing()
    {
        transform.DOKill();
        transform.localScale = Vector3.one;
        StopBlinking();
    }

    private void StartBlinking()
    {
        if (_glowTween != null && _glowTween.IsActive() && _glowTween.IsPlaying()) return;

        if (outlineGlow != null)
        {
            outlineGlow.gameObject.SetActive(true);
            outlineGlow.color = lowEnergyColor;
            _glowTween = outlineGlow.DOFade(0.2f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
        else if (buttonBg != null)
        {
            _glowTween = buttonBg.DOColor(lowEnergyColor, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
    }

    private void StopBlinking()
    {
        _glowTween?.Kill();
        if (outlineGlow != null)
        {
            outlineGlow.DOKill();
            outlineGlow.gameObject.SetActive(false);
        }
        if (buttonBg != null)
        {
            buttonBg.DOKill();
            buttonBg.color = normalColor;
        }
    }

    public void OnSkipButtonClicked()
    {
        if (BoardController.instance != null && BoardController.instance.IsResolvingMatch) return;
        if (CombatCameraController.Instance != null && CombatCameraController.Instance.IsZooming) return;

        if (CombatTurnManager.Instance != null && CombatTurnManager.Instance.IsPlayerTurn)
        {
            // Hiệu ứng click nén nhẹ
            transform.DOScale(0.92f, 0.1f).OnComplete(() =>
            {
                transform.DOScale(1.0f, 0.1f);
                CombatTurnManager.Instance.SkipPlayerTurn();
            });
        }
    }
}
