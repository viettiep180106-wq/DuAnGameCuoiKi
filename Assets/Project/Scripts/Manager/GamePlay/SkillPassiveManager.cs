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
    }

    private void EnsureSkillContainerPrefab()
    {
        if (_skillContainerPrefab != null) return;

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
        var gm = GameManager.instance != null ? GameManager.instance : FindFirstObjectByType<GameManager>();
        if (gm != null && !gm.IsWaveCombatActive) return;
        if (Time.time < _nextAttackFireTime) return;

        Player activePlayer = GetActivePlayer();
        Vector3 playerPos = activePlayer != null ? activePlayer.transform.position : transform.position;

        var asp = ActiveSkillPassive.instance != null ? ActiveSkillPassive.instance : FindFirstObjectByType<ActiveSkillPassive>();
        float atkRange = asp != null ? asp.atkRange : 4.2f;

        // Chỉ xả đòn tấn công khi có quái nằm trong tầm đánh
        var ec = EnemyController.instance != null ? EnemyController.instance : FindFirstObjectByType<EnemyController>();
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

    /// <summary>
    /// Kích hoạt chuỗi đòn tấn công: Nếu quái đã trong tầm đánh thì bắn ngay, nếu chưa thì tích lũy vào hàng đợi
    /// </summary>
    public void ExecuteInstantMatchAttacks(HashSet<Vector2Int> listMatch, List<List<SlotStoneBase>> grid)
    {
        if (listMatch == null || grid == null) return;
        if (GameManager.instance != null && !GameManager.instance.IsWaveCombatActive) return;

        // Nếu CombatTurnManager đang chạy: Chỉ cho phép tung chiêu trong lượt của người chơi
        if (CombatTurnManager.Instance != null && !CombatTurnManager.Instance.IsPlayerTurn) return;

        _cachedMatchCounts.Clear();
        List<TypeStone> matchOrder = new List<TypeStone>();

        foreach (Vector2Int coord in listMatch)
        {
            if (coord.x >= 0 && coord.x < grid.Count &&
                coord.y >= 0 && coord.y < grid[coord.x].Count)
            {
                SlotStoneBase slot = grid[coord.x][coord.y];
                if (slot != null && slot.GetStone() != null)
                {
                    TypeStone type = slot.GetStone().nameStone;
                    if (_cachedMatchCounts.ContainsKey(type))
                    {
                        _cachedMatchCounts[type]++;
                    }
                    else
                    {
                        _cachedMatchCounts[type] = 1;
                        matchOrder.Add(type);
                    }
                }
            }
        }

        // Tấn công tức thì (Instant Attack) theo chuẩn GDD:
        // - Bỏ hoàn toàn Action Queue delay
        // - Tiêu hao đúng 5 Energy / 1 skill
        for (int i = 0; i < matchOrder.Count; i++)
        {
            TypeStone type = matchOrder[i];
            int count = _cachedMatchCounts[type];

            ExecuteSingleAttack(type, count);
        }
    }

    /// <summary>
    /// Hủy toàn bộ các skill chưa được kích hoạt khi kết thúc wave / thời điểm reset
    /// </summary>
    public void CancelPendingAttacks()
    {
        _pendingMatchAttacks.Clear();
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
            if (activePlayer != null)
            {
                activePlayer.PlayAttackAnimation();
            }

            PlayerLoadoutManager loadout = PlayerLoadoutManager.Instance;
            SkillPassiveDataSO equippedSkill = loadout != null ? loadout.GetEquippedPassive(stoneType) : null;
            if (equippedSkill == null || equippedSkill.SkillPrefab == null) return;

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
                    skill.transform.localScale = new Vector3(3.5f, 3.5f, 3.5f);
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
