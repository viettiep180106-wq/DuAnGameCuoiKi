using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CombatTurnState
{
    NotStarted,
    PlayerTurn,
    EnemyTurn,
    RoundEnd,
    BattleWon,
    BattleLost
}

/// <summary>
/// Trình quản lý hệ thống chiến đấu Turn-based 5v1 theo chuẩn GDD:
/// - Chu kỳ 1 Round = Lượt Player -> Lượt toàn bộ Quái còn sống -> Kết thúc Round (giảm 1 CD).
/// - Mỗi skill của Player tiêu hao đúng 5 Energy.
/// - Nút "Skip Lượt" kết thúc lượt của Player và chuyển sang phe Enemy.
/// Tuân thủ Zero-GC và chuẩn coding unity-core.md.
/// </summary>
public class CombatTurnManager : MonoBehaviour
{
    public static CombatTurnManager Instance { get; private set; }

    public const int ENERGY_COST_PER_SKILL = 5;

    [Header("Turn State")]
    [SerializeField] private CombatTurnState _currentState = CombatTurnState.NotStarted;
    [SerializeField] private int _currentRound = 1;
    [SerializeField] private int _remainingEnemyTurns = 0;
    [SerializeField] private int _playerMatchesThisTurn = 0;

    public CombatTurnState CurrentState => _currentState;
    public int CurrentRound => _currentRound;
    public int RemainingEnemyTurns => _remainingEnemyTurns;
    public int PlayerMatchesThisTurn => _playerMatchesThisTurn;

    public const int BASE_MATCH_COST = 5;

    public bool IsPlayerTurn => _currentState == CombatTurnState.PlayerTurn;
    public bool IsEnemyTurn => _currentState == CombatTurnState.EnemyTurn;
    public bool CanPlayerCastSkill => IsPlayerTurn && CanPlayerAffordNextMatch();

    // Events
    public event Action<int> OnRoundChanged;
    public event Action<CombatTurnState> OnTurnStateChanged;
    public event Action<int> OnRemainingEnemyTurnsChanged;
    public event Action OnPlayerTurnStarted;
    public event Action OnEnemyTurnStarted;
    public event Action OnRoundEnded;

    private Coroutine _enemyTurnCoroutine;
    [SerializeField]private Player _cachedPlayer;
    [SerializeField]private EnemyController _cachedEnemyController;
    [SerializeField]private SkipTurnButtonUI _cachedSkipButton;

    private Player CachedPlayer => Player.Instance != null ? Player.Instance : (_cachedPlayer != null ? _cachedPlayer : (_cachedPlayer = FindFirstObjectByType<Player>()));
    private EnemyController CachedEnemyController => EnemyController.instance != null ? EnemyController.instance : (_cachedEnemyController != null ? _cachedEnemyController : (_cachedEnemyController = FindFirstObjectByType<EnemyController>()));
    private SkipTurnButtonUI CachedSkipButton => _cachedSkipButton != null ? _cachedSkipButton : (_cachedSkipButton = FindFirstObjectByType<SkipTurnButtonUI>());

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Bắt đầu trận chiến Turn-based của một Wave
    /// </summary>
    public void StartCombatWave()
    {
        _currentRound = 1;
        _playerMatchesThisTurn = 0;
        OnRoundChanged?.Invoke(_currentRound);

        SetState(CombatTurnState.PlayerTurn);

        // Mở khóa bàn cờ Match-3 cho lượt người chơi
        if (BoardController.instance != null)
        {
            BoardController.instance.UnlockBoardInteractions();
        }
        HighlightSkipTurnButton(false);

        OnPlayerTurnStarted?.Invoke();
    }

    /// <summary>
    /// Tính toán chi phí năng lượng cho lần match tiếp theo trong lượt của Player (GDD Sheet Loop&GamePlay Bảng 2):
    /// - 3 lần match đầu tiên trong lượt: Luôn MIỄN PHÍ (0 Energy).
    /// - Từ lần match thứ 4 trở đi: Bắt đầu từ 5 Energy, sau mỗi lần match chi phí TĂNG GẤP ĐÔI (5 -> 10 -> 20 -> 40...).
    /// </summary>
    public int GetNextMatchEnergyCost()
    {
        if (_playerMatchesThisTurn < 3) return 0; // 3 lần đầu luôn Free
        int paidStep = _playerMatchesThisTurn - 3; // 0: lần thứ 4 (5), 1: lần thứ 5 (10), 2: lần thứ 6 (20)...
        return BASE_MATCH_COST * (1 << Mathf.Min(paidStep, 10));
    }

    /// <summary>
    /// Kiểm tra người chơi có đủ năng lượng cho lần match tiếp theo hay không
    /// </summary>
    public bool CanPlayerAffordNextMatch()
    {
        int cost = GetNextMatchEnergyCost();
        if (cost == 0) return true;
        Player player = CachedPlayer;
        return player == null || player.EnergyCores >= cost;
    }

    /// <summary>
    /// Tiêu hao năng lượng khi combo match đá được thực hiện thành công
    /// </summary>
    public bool TryConsumeMatchEnergy()
    {
        if (!IsPlayerTurn) return false;

        int cost = GetNextMatchEnergyCost();
        if (cost == 0)
        {
            _playerMatchesThisTurn++;
            CheckAndHandleEnergyDepletion();
            return true;
        }

        Player player = CachedPlayer;
        if (player != null)
        {
            if (player.TrySpendEnergy(cost))
            {
                _playerMatchesThisTurn++;
                CheckAndHandleEnergyDepletion();
                return true;
            }
            else
            {
                CheckAndHandleEnergyDepletion();
                return false;
            }
        }

        _playerMatchesThisTurn++;
        return true; // Fallback unit test
    }

    /// <summary>
    /// Kiểm tra và xử lý khi người chơi không còn đủ năng lượng cho lần match tiếp theo:
    /// Khóa bàn cờ ngay lập tức, hiển thị thông báo và nhấp nháy nút [SKIP LƯỢT]
    /// </summary>
    public void CheckAndHandleEnergyDepletion()
    {
        if (!CanPlayerAffordNextMatch())
        {
            if (BoardController.instance != null)
            {
                BoardController.instance.LockBoardInteractions();
            }

            if (MainSceneUIController.Instance != null)
            {
                MainSceneUIController.Instance.ShowEnergyDepletedNotice();
            }

            HighlightSkipTurnButton(true);
        }
    }

    public void HighlightSkipTurnButton(bool highlight)
    {
        var skipBtn = CachedSkipButton;
        if (skipBtn != null)
        {
            if (highlight) skipBtn.StartPulsing();
            else skipBtn.StopPulsing();
        }
    }

    /// <summary>
    /// Thử tiêu hao 5 Energy khi người chơi thực hiện 1 đòn tấn công / skill
    /// </summary>
    public bool TryConsumeSkillEnergy()
    {
        Player player = CachedPlayer;
        if (player == null || player.EnergyCores < BASE_MATCH_COST) return false;
        return player.TrySpendEnergy(BASE_MATCH_COST);
    }

    /// <summary>
    /// Người chơi nhấn nút "Skip Lượt" để kết thúc lượt của mình và chuyển sang phe Enemy
    /// </summary>
    public void SkipPlayerTurn()
    {
        if (_currentState != CombatTurnState.PlayerTurn) return;

        // Khóa bàn cờ Match-3
        if (BoardController.instance != null)
        {
            BoardController.instance.LockBoardInteractions();
        }

        StartEnemyTurn();
    }

    /// <summary>
    /// Bắt đầu lượt của phe quái vật (Enemy Turn)
    /// </summary>
    public void StartEnemyTurn()
    {
        SetState(CombatTurnState.EnemyTurn);
        OnEnemyTurnStarted?.Invoke();

        if (_enemyTurnCoroutine != null)
        {
            StopCoroutine(_enemyTurnCoroutine);
        }

        _enemyTurnCoroutine = StartCoroutine(EnemyTurnRoutine());
    }

    /// <summary>
    /// Luồng hành động tuần tự của các quái vật theo GDD:
    /// - Hàng trước trước -> Hàng sau sau.
    /// - Trong cùng hàng: Ưu tiên theo Tốc độ hành động.
    /// - Ưu tiên Skill (CD=0) > Đánh thường (100% ATK).
    /// </summary>
    private IEnumerator EnemyTurnRoutine()
    {
        var ec = CachedEnemyController;
        if (ec != null)
        {
            // Lấy danh sách quái còn sống được sắp xếp theo đúng thứ tự hàng và tốc độ
            List<EnemyBase> turnOrder = ec.GetOrderedEnemiesForTurn();
            _remainingEnemyTurns = turnOrder.Count;
            OnRemainingEnemyTurnsChanged?.Invoke(_remainingEnemyTurns);

            for (int i = 0; i < turnOrder.Count; i++)
            {
                EnemyBase enemy = turnOrder[i];
                if (enemy != null && enemy.gameObject.activeInHierarchy && enemy.CurrentHP > 0)
                {
                    // Thực thi 1 hành động của quái (Skill hoặc Đánh thường)
                    yield return enemy.ExecuteTurnActionRoutine();

                    _remainingEnemyTurns = Mathf.Max(0, turnOrder.Count - 1 - i);
                    OnRemainingEnemyTurnsChanged?.Invoke(_remainingEnemyTurns);

                    // Khoảng nghỉ ngắn giữa các lượt hành động của từng quái
                    yield return new WaitForSeconds(0.25f);

                    // Nếu Player đã tử trận trong lượt này thì dừng trận
                    Player player = CachedPlayer;
                    if (player != null && player.CurrentHP <= 0)
                    {
                        SetState(CombatTurnState.BattleLost);
                        yield break;
                    }
                }
            }
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        // Toàn bộ quái còn sống đã hành động xong -> Kết thúc Round
        EndRound();
    }

    /// <summary>
    /// Kết thúc 1 Round và chuẩn bị sang Round mới
    /// </summary>
    public void EndRound()
    {
        SetState(CombatTurnState.RoundEnd);
        OnRoundEnded?.Invoke();

        // 1. Giảm Cooldown 1 Round cho toàn bộ kỹ năng của quái
        var ec = CachedEnemyController;
        if (ec != null)
        {
            ec.TickRoundCooldowns();
        }

        // 2. Kiểm tra điều kiện thắng / thua
        if (ec != null && ec.GetLivingEnemyCount() == 0)
        {
            EndBattle(true);
            return;
        }

        Player player = CachedPlayer;
        if (player != null && player.CurrentHP <= 0)
        {
            EndBattle(false);
            return;
        }

        // 3. Làm mới Khiên Kim Cang (Hệ Kim) theo chu kỳ Round
        if (player != null)
        {
            player.TickRoundMetalShield();
        }

        // 4. Giảm thời hạn khóa ngọc Match-3 theo chu kỳ Round
        if (BoardController.instance != null)
        {
            BoardController.instance.TickStoneLocks();
        }

        // 5. Bắt đầu Round mới: Reset số lần match của Player để lại có 3 lần free
        _currentRound++;
        _playerMatchesThisTurn = 0;
        OnRoundChanged?.Invoke(_currentRound);

        SetState(CombatTurnState.PlayerTurn);

        // Mở khóa lại bàn cờ Match-3 cho lượt của người chơi
        if (BoardController.instance != null)
        {
            BoardController.instance.UnlockBoardInteractions();
        }
        HighlightSkipTurnButton(false);

        OnPlayerTurnStarted?.Invoke();
    }

    /// <summary>
    /// Kết thúc toàn bộ trận chiến khi thắng hoặc dọn sạch quái
    /// </summary>
    public void EndBattle(bool isWin)
    {
        if (_enemyTurnCoroutine != null)
        {
            StopCoroutine(_enemyTurnCoroutine);
            _enemyTurnCoroutine = null;
        }

        SetState(isWin ? CombatTurnState.BattleWon : CombatTurnState.BattleLost);

        if (BoardController.instance != null)
        {
            BoardController.instance.LockBoardInteractions();
        }

        if (isWin)
        {
            if (GameManager.instance != null)
            {
                GameManager.instance.HandleWaveCleared();
            }
        }
        else
        {
            if (GameManager.instance != null)
            {
                GameManager.instance.LoseGame(true);
            }
        }
    }

    private void SetState(CombatTurnState newState)
    {
        _currentState = newState;
        OnTurnStateChanged?.Invoke(_currentState);
    }
}
