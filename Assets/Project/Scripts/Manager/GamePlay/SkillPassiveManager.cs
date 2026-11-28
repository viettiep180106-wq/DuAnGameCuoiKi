using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SkillPassiveManager : MonoBehaviour
{
    private static SkillPassiveManager _instance;
    public static SkillPassiveManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SkillPassiveManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("SkillPassiveManager", typeof(SkillPassiveManager));
                    _instance = go.GetComponent<SkillPassiveManager>();
                }
            }
            return _instance;
        }
    }
    public static SkillPassiveManager instance => Instance; // Backward compatibility

    [Header("Dependencies")]
    [SerializeField] private Player _player;
    [SerializeField] private GameObject _skillContainerPrefab;

    [Header("Queue Layout")]
    [SerializeField] private float _space = 0f;
    [SerializeField] private float _itemSpacing = 0.32f;
    [SerializeField] private float _startOffsetX = -2.1f;

    // Zero-GC match counter cache
    private readonly Dictionary<TypeStone, int> _cachedMatchCounts = new Dictionary<TypeStone, int>();
    private readonly Queue<SkillContainer> _skillQueue = new Queue<SkillContainer>();
    private readonly List<TypeStone> _matchOrderBuffer = new List<TypeStone>(8);

    private GameManager _cachedGameManager;
    private EnemyController _cachedEnemyController;
    private ActiveSkillPassive _cachedActiveSkillPassive;

    private GameManager CachedGameManager
    {
        get
        {
            if (_cachedGameManager == null)
            {
                _cachedGameManager = GameManager.instance != null ? GameManager.instance : FindFirstObjectByType<GameManager>();
            }
            return _cachedGameManager;
        }
    }

    private EnemyController CachedEnemyController
    {
        get
        {
            if (_cachedEnemyController == null)
            {
                _cachedEnemyController = EnemyController.instance != null ? EnemyController.instance : FindFirstObjectByType<EnemyController>();
            }
            return _cachedEnemyController;
        }
    }

    private ActiveSkillPassive CachedActiveSkillPassive
    {
        get
        {
            if (_cachedActiveSkillPassive == null)
            {
                _cachedActiveSkillPassive = ActiveSkillPassive.instance != null ? ActiveSkillPassive.instance : FindFirstObjectByType<ActiveSkillPassive>();
            }
            return _cachedActiveSkillPassive;
        }
    }

    // Hàng đợi tích lũy skill match khi quái chưa vào tầm đánh
    public struct QueuedMatchAttack
    {
        public TypeStone StoneType;
        public int MatchCount;

        public QueuedMatchAttack(TypeStone type, int count)
        {
            StoneType = type;
            MatchCount = count;
        }
    }

    public struct ConfiscatedPassiveData
    {
        public TypeStone stoneType;
        public int matchCount;
    }
    private ConfiscatedPassiveData? _confiscatedPassive = null;
    private bool _isExecutingConfiscated = false;

    private readonly Queue<QueuedMatchAttack> _pendingMatchAttacks = new Queue<QueuedMatchAttack>();
    private float _nextAttackFireTime = 0f;
    private const float ATTACK_FIRE_INTERVAL = 0.14f;

    public int QueueCount => _skillQueue.Count;
    public int PendingAttackCount => _pendingMatchAttacks.Count;

    private void Awake()
    {
        _instance = this;
        EnsureSkillContainerPrefab();
    }

    public Player GetActivePlayer()
    {
        if (Player.Instance != null) return Player.Instance;
        if (_player != null) return _player;
        _player = FindFirstObjectByType<Player>();
        return _player;
    }

    private void Start()
    {
        _player = GetActivePlayer();

        if (PlayerLoadoutManager.Instance == null)
        {
            GameObject loadoutObj = new GameObject("PlayerLoadoutManager", typeof(PlayerLoadoutManager));
            loadoutObj.transform.SetParent(transform.parent != null ? transform.parent : transform);
        }

        if (CombatTurnManager.Instance != null)
        {
            CombatTurnManager.Instance.OnPlayerTurnStarted -= HandlePlayerTurnStarted;
            CombatTurnManager.Instance.OnPlayerTurnStarted += HandlePlayerTurnStarted;
        }
    }

    private void OnDestroy()
    {
        if (CombatTurnManager.Instance != null)
        {
            CombatTurnManager.Instance.OnPlayerTurnStarted -= HandlePlayerTurnStarted;
        }
    }

    private void HandlePlayerTurnStarted()
    {
        // Khi hết cooldown 2 Round, kỹ năng nội tại bị tịch thu sẽ tự động được giải phóng và thi triển
        if (_confiscatedPassive.HasValue && InterWaveEventManager.Instance != null && InterWaveEventManager.Instance.XoaPassiveCooldown == 0)
        {
            var data = _confiscatedPassive.Value;
            _confiscatedPassive = null;

            Player activePlayer = GetActivePlayer();
            if (DamageTextManager.Instance != null && activePlayer != null)
            {
                DamageTextManager.Instance.SpawnCustomText("[GIẢI PHÓNG NỘI TẠI TỊCH THU!]", new Color(1f, 0.85f, 0.2f), activePlayer.transform.position + Vector3.up * 2.8f, 1.5f);
            }

            _isExecutingConfiscated = true;
            try
            {
                ExecuteSingleAttack(data.stoneType, data.matchCount);
            }
            finally
            {
                _isExecutingConfiscated = false;
            }
        }
    }

    private void EnsureSkillContainerPrefab()
    {
        if (_skillContainerPrefab != null) return;

        var mgr = GameDataManager.Instance;
        if (mgr != null && mgr.SkillContainerPrefab != null)
        {
            _skillContainerPrefab = mgr.SkillContainerPrefab;
            return;
        }

        _skillContainerPrefab = Resources.Load<GameObject>("Prefab/Base/Skill/SkillContainer");

        if (_skillContainerPrefab == null)
        {
            GameObject template = new GameObject("SkillContainerTemplate", typeof(SkillContainer));
            template.SetActive(false);
            template.transform.SetParent(transform);
            _skillContainerPrefab = template;
        }
    }

    private void Update()
    {
        ProcessQueuedAttacks();
    }

    /// <summary>
    /// Xử lý các đòn tấn công tích lũy trong hàng đợi: Chỉ xả đòn khi có quái trong tầm đánh
    /// </summary>
    private void ProcessQueuedAttacks()
    {
        if (_pendingMatchAttacks.Count == 0) return;
        var gm = CachedGameManager;
        if (gm != null && !gm.IsWaveCombatActive) return;
        if (Time.time < _nextAttackFireTime) return;

        Player activePlayer = GetActivePlayer();
        Vector3 playerPos = activePlayer != null ? activePlayer.transform.position : transform.position;

        var asp = CachedActiveSkillPassive;
        float atkRange = asp != null ? asp.atkRange : 4.2f;

        // Chỉ xả đòn tấn công khi có quái nằm trong tầm đánh
        var ec = CachedEnemyController;
        bool hasEnemyInRange = ec != null && ec.HasEnemyInAttackRange(playerPos, atkRange);
        if (!hasEnemyInRange)
        {
            // Quái chưa vào tầm đánh: Giữ nguyên các skill đã match trong hàng đợi, chờ quái tới
            return;
        }

        // Bắn skill tiếp theo trong hàng đợi theo đúng thứ tự match
        QueuedMatchAttack attack = _pendingMatchAttacks.Dequeue();
        ExecuteSingleAttack(attack.StoneType, attack.MatchCount);
        _nextAttackFireTime = Time.time + ATTACK_FIRE_INTERVAL;
    }

    private readonly List<QueuedMatchAttack> _instantMatchBatchBuffer = new List<QueuedMatchAttack>(8);

    /// <summary>
    /// Đọc bàn cờ và đóng gói danh sách đòn match (Zero-GC) trước khi các viên đá bị xóa khỏi grid.
    /// Trả về tổng số viên đá được match trong lượt (dùng để kiểm tra điều kiện >= 6).
    /// </summary>
    public int PrepareMatchAttacks(HashSet<Vector2Int> listMatch, List<List<SlotStoneBase>> grid, List<QueuedMatchAttack> outBatch)
    {
        if (outBatch == null) return 0;
        outBatch.Clear();

        if (listMatch == null || grid == null) return 0;
        if (GameManager.instance != null && !GameManager.instance.IsWaveCombatActive) return 0;
        if (CombatTurnManager.Instance != null && !CombatTurnManager.Instance.IsPlayerTurn) return 0;

        _cachedMatchCounts.Clear();
        _matchOrderBuffer.Clear();
        int totalMatchedStones = 0;

        foreach (Vector2Int coord in listMatch)
        {
            if (coord.x >= 0 && coord.x < grid.Count &&
                coord.y >= 0 && coord.y < grid[coord.x].Count)
            {
                SlotStoneBase slot = grid[coord.x][coord.y];
                if (slot != null && slot.GetStone() != null)
                {
                    TypeStone type = slot.GetStone().nameStone;
                    totalMatchedStones++;
                    if (_cachedMatchCounts.ContainsKey(type))
                    {
                        _cachedMatchCounts[type]++;
                    }
                    else
                    {
                        _cachedMatchCounts[type] = 1;
                        _matchOrderBuffer.Add(type);
                    }
                }
            }
        }

        for (int i = 0; i < _matchOrderBuffer.Count; i++)
        {
            TypeStone type = _matchOrderBuffer[i];
            outBatch.Add(new QueuedMatchAttack(type, _cachedMatchCounts[type]));
        }

        return totalMatchedStones;
    }

    /// <summary>
    /// Thực thi toàn bộ các kỹ năng trong batch đã chuẩn bị trước đó (sau khi zoom camera xong).
    /// </summary>
    public void ExecutePreparedMatchBatch(List<QueuedMatchAttack> batch)
    {
        if (batch == null || batch.Count == 0) return;
        if (GameManager.instance != null && !GameManager.instance.IsWaveCombatActive) return;
        if (CombatTurnManager.Instance != null && !CombatTurnManager.Instance.IsPlayerTurn) return;

        for (int i = 0; i < batch.Count; i++)
        {
            ExecuteSingleAttack(batch[i].StoneType, batch[i].MatchCount);
        }
    }

    /// <summary>
    /// Kích hoạt chuỗi đòn tấn công: Nếu quái đã trong tầm đánh thì bắn ngay, nếu chưa thì tích lũy vào hàng đợi
    /// </summary>
    public void ExecuteInstantMatchAttacks(HashSet<Vector2Int> listMatch, List<List<SlotStoneBase>> grid)
    {
        int total = PrepareMatchAttacks(listMatch, grid, _instantMatchBatchBuffer);
        if (total > 0)
        {
            ExecutePreparedMatchBatch(_instantMatchBatchBuffer);
        }
    }

    /// <summary>
    /// Hủy toàn bộ các skill chưa được kích hoạt khi kết thúc wave / thời điểm reset
    /// </summary>
    public void CancelPendingAttacks()
    {
        _pendingMatchAttacks.Clear();
        _confiscatedPassive = null;
        DOTween.Kill("InstantMatchAttacks");
    }

    public void ExecuteSingleAttack(TypeStone stoneType, int matchCount)
    {
        try
        {
            // Khi kết thúc wave (thời điểm reset) thì các skill chưa được kích hoạt sẽ không được dùng nữa
            if (GameManager.instance != null && !GameManager.instance.IsWaveCombatActive)
            {
                return;
            }

            Player activePlayer = GetActivePlayer();

            if (!_isExecutingConfiscated)
            {
                // 1. Debuff: Khóa Kỹ Năng Passive (Không nhận 1 skill passive khi match, CD 2 round, tồn tại 1 level)
                if (InterWaveEventManager.Instance != null && InterWaveEventManager.Instance.CanTriggerKhoaSkillPassive())
                {
                    InterWaveEventManager.Instance.ConsumeKhoaSkillPassive();
                    if (DamageTextManager.Instance != null && activePlayer != null)
                    {
                        DamageTextManager.Instance.SpawnCustomText("[KHÓA KỸ NĂNG NỘI TẠI!]", new Color(1f, 0.25f, 0.25f), activePlayer.transform.position + Vector3.up * 2.5f, 1.5f);
                    }
                    return; // Bị khóa, không sinh chiêu thức
                }

                // 2. Debuff: Tịch Thu Nội Tại (Kỹ năng bị tịch thu 1 lần, sau khi hết CD 2 round tự động dùng lại)
                if (InterWaveEventManager.Instance != null && InterWaveEventManager.Instance.CanTriggerXoaPassive() && !_confiscatedPassive.HasValue)
                {
                    _confiscatedPassive = new ConfiscatedPassiveData { stoneType = stoneType, matchCount = matchCount };
                    InterWaveEventManager.Instance.ConsumeXoaPassive();
                    if (DamageTextManager.Instance != null && activePlayer != null)
                    {
                        DamageTextManager.Instance.SpawnCustomText("[TỊCH THU NỘI TẠI! CHỜ 2 ROUND]", new Color(1f, 0.55f, 0.1f), activePlayer.transform.position + Vector3.up * 2.5f, 1.5f);
                    }
                    return; // Tịch thu, hoãn thi triển đến khi hết CD
                }
            }

            if (activePlayer != null)
            {
                activePlayer.PlayAttackAnimation();
            }

            PlayerLoadoutManager loadout = PlayerLoadoutManager.Instance;
            SkillPassiveDataSO equippedSkill = loadout != null ? loadout.GetEquippedPassive(stoneType) : null;
            if (equippedSkill == null || equippedSkill.SkillPrefab == null) return;

            // Hiển thị Tên Chiêu Thức Player nổi bật phía trên đầu
            if (DamageTextManager.Instance != null && !string.IsNullOrEmpty(equippedSkill.SkillName) && activePlayer != null)
            {
                Color elemColor = ElementalHelper.GetColor(equippedSkill.Element);
                DamageTextManager.Instance.SpawnCustomText($"[{equippedSkill.SkillName}]", elemColor, activePlayer.transform.position + Vector3.up * 2.5f, 1.4f);
            }

            Vector3 spawnPos = activePlayer != null 
                ? activePlayer.transform.position 
                : transform.position;

            GameObject skillObj = null;
            if (ObjectPoolingManager.Instance != null)
            {
                skillObj = ObjectPoolingManager.Instance.Spawn(
                    equippedSkill.SkillPrefab,
                    spawnPos,
                    Quaternion.identity
                );
            }

            if (skillObj == null && equippedSkill.SkillPrefab != null)
            {
                skillObj = Instantiate(equippedSkill.SkillPrefab, spawnPos, Quaternion.identity);
            }

            if (skillObj != null)
            {
                SkillPassive skill = skillObj.GetComponent<SkillPassive>();
                if (skill != null)
                {
                    skill.transform.position = spawnPos;
                    skill.Initialize(matchCount);
                    skill.Active();
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SkillPassiveManager] Error executing attack for stone {stoneType}: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Backward compatibility: Tính toán và đóng gói kỹ năng
    /// </summary>
    public void CaculatorSkill(HashSet<Vector2Int> listMatch, List<List<SlotStoneBase>> grid)
    {
        ExecuteInstantMatchAttacks(listMatch, grid);
    }

    /// <summary>
    /// Kích hoạt và lấy kỹ năng kế tiếp từ hàng đợi (thông qua SkillContainer)
    /// </summary>
    public SkillPassive GetSkill()
    {
        Player activePlayer = GetActivePlayer();
        Vector3 spawnPosition = activePlayer != null ? activePlayer.transform.position : transform.position;
        return ExecuteNextSkill(spawnPosition);
    }

    /// <summary>
    /// Kích hoạt kỹ năng tiếp theo trong hàng đợi SkillContainer tại vị trí chỉ định và dồn hàng đợi sang trái
    /// </summary>
    public SkillPassive ExecuteNextSkill(Vector3 spawnPosition)
    {
        Player activePlayer = GetActivePlayer();
        if (activePlayer != null)
        {
            activePlayer.PlayFlyAnimation();
        }

        if (_skillQueue.Count == 0) return null;

        SkillContainer container = _skillQueue.Dequeue();

        // Hiệu ứng băng chuyền: Tự động cuộn các item còn lại sang trái để lấp chỗ trống
        int idx = 0;
        foreach (SkillContainer remaining in _skillQueue)
        {
            if (remaining != null && remaining.gameObject.activeSelf)
            {
                float targetX = transform.position.x + _startOffsetX + (idx * _itemSpacing);
                remaining.transform.DOKill();
                remaining.transform.DOMoveX(targetX, 0.2f).SetEase(Ease.OutQuad);
                idx++;
            }
        }

        _space = idx * _itemSpacing;

        if (container == null) return null;

        return container.ExecuteSkill(spawnPosition);
    }

    /// <summary>
    /// Dọn sạch hàng đợi kỹ năng
    /// </summary>
    public void ClearQueue()
    {
        while (_skillQueue.Count > 0)
        {
            SkillContainer c = _skillQueue.Dequeue();
            if (c != null && c.gameObject.activeInHierarchy)
            {
                ObjectPoolingManager.Instance.Despawn(c.gameObject);
            }
        }
        _space = 0f;
    }
}
