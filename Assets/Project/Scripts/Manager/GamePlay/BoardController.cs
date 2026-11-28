using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening;
using UnityEngine.UI;
public class BoardController : MonoBehaviour
{
    public static BoardController instance;
    public Action<Direction, StoneBase> OnSwapStone;

    [Header("----Setting Base----")]
    [SerializeField] private int rows = 6;
    [SerializeField] private int cols = 6;
    [SerializeField] private int maxY = 6;
    [SerializeField] private bool canTrans = true;
    public RectTransform center;

    private Dictionary<TypeStone, StoneBase> prefabs = new Dictionary<TypeStone, StoneBase>();
    public SlotStoneBase prefabSlot;
    [SerializeField] private List<List<SlotStoneBase>> grid;
    [SerializeField] private float height = 720f;
    [SerializeField] private float width = 720f;
    [SerializeField] private float boardFrameSize = 740f; // Kích thước khung viền nền bàn cờ (Image)
    [SerializeField] private float boardFrameWidth = 880f; // Bề ngang toàn màn hình (Full Width)
    [SerializeField] private float boardFrameHeight = 740f; // Bề dọc khung viền bàn cờ

    private Dictionary<Vector2Int, TypeStone> listData = new Dictionary<Vector2Int, TypeStone>();
    private List<TypeStone> activeElements = new List<TypeStone>();
    private List<TypeStone> targetTypeStones = new List<TypeStone>();
    private Vector2 sizeSlot;
    private bool isRevert = false;

    // Zero-GC Reusable Buffers
    private readonly List<TypeStone> _targetCandidatesBuffer = new List<TypeStone>(8);
    private readonly List<TypeStone> _remainCandidatesBuffer = new List<TypeStone>(8);
    private readonly List<StoneBase> _unlockedStonesBuffer = new List<StoneBase>(36);

    private readonly TypeStone[] defaultElements = new TypeStone[] 
    { 
        TypeStone.Fire, 
        TypeStone.Ice, 
        TypeStone.Wood, 
        TypeStone.Earth, 
        TypeStone.Light 
    };

    [Header("----Container Layering----")]
    [SerializeField] private RectTransform slotsContainer;
    [SerializeField] private RectTransform stonesContainer;
    private readonly MatchStoneVfxGraph matchVfxGraph = new();
    private MatchStoneVfx matchVfx;

    [Header("----Combat Feel (Match >= 6 Zoom)----")]
    [SerializeField] private int bigMatchZoomThreshold = 6;
    [SerializeField] private float bigMatchZoomDuration = 1.0f;
    [SerializeField] private bool zoomOnCascadeMatch = false;

    private bool _isResolvingMatch = false;
    private readonly List<SkillPassiveManager.QueuedMatchAttack> _pendingMatchBatchBuffer = new List<SkillPassiveManager.QueuedMatchAttack>(8);

    public bool IsResolvingMatch => _isResolvingMatch;

    private void Awake()
    {  
        instance = this;
        if (center != null)
        {
            center.anchoredPosition = new Vector2(center.anchoredPosition.x, -1600f);
        }

        EnsureContainers();
        LoadPrefabFromResource();
    }

    public void EnsureSlotPrefab()
    {
        if (prefabSlot == null)
        {
            prefabSlot = Resources.Load<SlotStoneBase>("Prefab/Base/Slot/SLot");
        }
    }

    public void EnsureContainers()
    {
        EnsureSlotPrefab();
        if (center == null) return;

        // Tự động bảo vệ và phục hồi kích thước chuẩn nếu bị serialize = 0
        if (width <= 0f) width = 720f;
        if (height <= 0f) height = 720f;
        if (boardFrameSize <= 0f) boardFrameSize = 740f;
        if (boardFrameWidth <= 0f) boardFrameWidth = 880f;
        if (boardFrameHeight <= 0f) boardFrameHeight = 740f;

        // Image bàn có kích thước phủ kín bề ngang màn hình (Full Width 880px)
        center.sizeDelta = new Vector2(boardFrameWidth, Mathf.Max(boardFrameSize, boardFrameHeight));

        if (slotsContainer == null)
        {
            Transform existingSlots = center.Find("SlotsContainer");
            if (existingSlots != null)
            {
                slotsContainer = existingSlots.GetComponent<RectTransform>();
            }
            else
            {
                GameObject slotsObj = new GameObject("SlotsContainer", typeof(RectTransform));
                slotsContainer = slotsObj.GetComponent<RectTransform>();
                slotsContainer.SetParent(center, false);
                slotsContainer.anchorMin = new Vector2(0.5f, 0.5f);
                slotsContainer.anchorMax = new Vector2(0.5f, 0.5f);
                slotsContainer.pivot = new Vector2(0.5f, 0.5f);
                slotsContainer.sizeDelta = new Vector2(width, height);
                slotsContainer.anchoredPosition = Vector2.zero;
                slotsContainer.localPosition = Vector3.zero;
                slotsContainer.localScale = Vector3.one;
            }
        }

        if (stonesContainer == null)
        {
            Transform existingStones = center.Find("StonesContainer");
            if (existingStones != null)
            {
                stonesContainer = existingStones.GetComponent<RectTransform>();
            }
            else
            {
                GameObject stonesObj = new GameObject("StonesContainer", typeof(RectTransform));
                stonesContainer = stonesObj.GetComponent<RectTransform>();
                stonesContainer.SetParent(center, false);
            }
        }

        // Bắt buộc chuẩn hóa vị trí, scale = 1, z = 0 và sizeDelta cho cả 2 container
        ResetContainerTransform(slotsContainer);
        ResetContainerTransform(stonesContainer);

        // slotsContainer luôn luôn là lớp dưới (Sibling 0)
        slotsContainer.SetSiblingIndex(0);
        // stonesContainer luôn luôn là lớp trên (Sibling 1)
        stonesContainer.SetSiblingIndex(1);
    }

    private void ResetContainerTransform(RectTransform container)
    {
        if (container == null) return;
        container.anchorMin = new Vector2(0.5f, 0.5f);
        container.anchorMax = new Vector2(0.5f, 0.5f);
        container.pivot = new Vector2(0.5f, 0.5f);
        container.sizeDelta = new Vector2(width, height);
        container.anchoredPosition = Vector2.zero;
        container.localPosition = Vector3.zero;
        container.localScale = Vector3.one;
        container.localRotation = Quaternion.identity;
    }

    private void OnEnable()
    {
        OnSwapStone += Trans;
    }

    private void OnDisable()
    {
        OnSwapStone -= Trans;
        _isResolvingMatch = false;
        _pendingMatchBatchBuffer.Clear();
        if (matchVfx) matchVfx.Clear();
    }

    public void LoadPrefabFromResource()
    {
        prefabs.Clear();
        var mgr = GameDataManager.Instance;
        if (mgr != null && mgr.TryCopyStonePrefabsTo(prefabs))
        {
            return;
        }

        string path = "Prefab/Base/Stone";
        StoneBase[] request = Resources.LoadAll<StoneBase>(path);
        foreach (StoneBase stone in request)
        {
            if (!prefabs.ContainsKey(stone.nameStone))
            {
                prefabs.Add(stone.nameStone, stone);
            }
        }
    }

    private StoneBase GetStonePrefab(TypeStone type)
    {
        if (prefabs.TryGetValue(type, out var p)) return p;
        if (type == TypeStone.Water && prefabs.TryGetValue(TypeStone.Ice, out var ice)) return ice;
        if (type == TypeStone.Ice && prefabs.TryGetValue(TypeStone.Water, out var water)) return water;
        if (type == TypeStone.Light && prefabs.TryGetValue(TypeStone.Wind, out var wind)) return wind;

        foreach (var any in prefabs.Values) return any;
        return null;
    }

    public void InitDataBoard(List<TypeStone> elements, Vector2Int Size)
    {
        ClearOldBoard();
        if (prefabs.Count == 0) LoadPrefabFromResource();
        activeElements = (elements != null && elements.Count > 0)
            ? new List<TypeStone>(elements)
            : new List<TypeStone>(defaultElements);
        targetTypeStones = new List<TypeStone>(activeElements);
        listData.Clear();
        // Cố định kích thước nxm là 6x6 theo yêu cầu người dùng
        cols = 6;
        rows = 6;
        maxY = rows;
        SetUpBoard();
    }

    /// <summary>
    /// Tỷ lệ rơi đá ngẫu nhiên mặc định luôn cân bằng đều 20% cho cả 5 loại đá (Kim, Mộc, Thủy, Hỏa, Thổ)
    /// </summary>
    public TypeStone GetGDDStoneDrop(List<TypeStone> targets = null)
    {
        return defaultElements[UnityEngine.Random.Range(0, defaultElements.Length)];
    }

    

    public void ClearOldBoard()
    {
        _isResolvingMatch = false;
        _pendingMatchBatchBuffer.Clear();
        if (matchVfx) matchVfx.Clear();
        if (grid != null)
        {
            for (int x = 0; x < grid.Count; x++)
            {
                for (int y = 0; y < grid[x].Count; y++)
                {
                    if (grid[x][y] != null)
                    {
                        var stone = grid[x][y].GetStone();
                        if (stone != null && stone.gameObject.activeInHierarchy)
                        {
                            if (ObjectPoolingManager.Instance != null)
                            {
                                ObjectPoolingManager.Instance.Despawn(stone.gameObject);
                            }
                        }
                    }
                }
            }
            grid.Clear();
        }


        // Dọn dẹp triệt để stonesContainer: chỉ despawn các đá đang active, KHÔNG destroy các đá đã nằm trong pool (activeSelf == false)
        if (stonesContainer != null)
        {
            for (int i = stonesContainer.childCount - 1; i >= 0; i--)
            {
                var child = stonesContainer.GetChild(i).gameObject;
                if (child != null && child.activeSelf)
                {
                    if (ObjectPoolingManager.Instance != null)
                    {
                        ObjectPoolingManager.Instance.Despawn(child);
                    }
                }
            }
        }
    }

    public void SetUpBoard()
    {
        EnsureSlotPrefab();
        EnsureContainers();

        if (prefabSlot == null)
        {
            Debug.LogError("[BoardController] Không tìm thấy prefabSlot để khởi tạo bàn cờ!");
            return;
        }

        // Tự động bảo vệ và phục hồi kích thước chuẩn nếu width/height <= 0
        if (width <= 0f) width = 720f;
        if (height <= 0f) height = 720f;

        // Công thức tính size 1 slot = 1 item stone động: kích thước vùng bàn chia cho số hàng/cột (không cố định)
        float cellWidth = width / cols;
        float cellHeight = height / rows;
        float cellSize = Mathf.Min(cellWidth, cellHeight);
        sizeSlot = new Vector2(cellSize, cellSize);

        Transform slotParent = slotsContainer != null ? slotsContainer : center;

        float totalGridW = cols * cellSize;
        float totalGridH = rows * cellSize;
        float startX = -totalGridW / 2f + cellSize / 2f;
        float startY = -totalGridH / 2f + cellSize / 2f;

        grid = new List<List<SlotStoneBase>>(cols);
        for (int x = 0; x < cols; x++)
        {
            List<SlotStoneBase> colList = new List<SlotStoneBase>(rows);
            for (int y = 0; y < rows; y++)
            {
                float posX = startX + (x * cellSize);
                float posY = startY + (y * cellSize);
                SlotStoneBase prefab = Instantiate(prefabSlot, slotParent);
                if (prefab != null)
                {
                    if (prefab.SlotItem != null)
                    {
                        prefab.SlotItem.sizeDelta = sizeSlot;
                        prefab.SlotItem.anchoredPosition = new Vector2(posX, posY);
                    }
                    prefab.transform.localPosition = new Vector3(posX, posY, 0f);
                    prefab.transform.localScale = Vector3.one;
                    colList.Add(prefab);
                }
            }
            grid.Add(colList);
        }
        UpdateStoneToBoard();
    }

    /// <summary>
    /// Thuật toán khởi tạo bàn cờ không Match-3 (No Match-3 Init Algorithm) chuẩn GDD.
    /// Quét 1 lượt tuyến tính O(R x C) = 36 bước, kiểm tra 2 ô liền kề bên trái và 2 ô liền kề phía dưới
    /// để loại trừ nguy cơ Match-3 ngang và dọc. Đảm bảo 100% không Match-3 khi bắt đầu trận đấu.
    /// </summary>
    public TypeStone[,] GenerateNoMatch3Matrix(int widthCols, int heightRows, List<TypeStone> targetStones = null)
    {
        TypeStone[,] matrix = new TypeStone[widthCols, heightRows];
        List<TypeStone> candidatePool = new List<TypeStone>(5);
        HashSet<TypeStone> blacklist = new HashSet<TypeStone>();

        for (int x = 0; x < widthCols; x++)
        {
            for (int y = 0; y < heightRows; y++)
            {
                blacklist.Clear();

                // 1. Nhận diện rủi ro Hàng Ngang (Check Horizontal Match)
                if (x >= 2)
                {
                    TypeStone left1 = matrix[x - 1, y];
                    TypeStone left2 = matrix[x - 2, y];
                    if (left1 == left2)
                    {
                        blacklist.Add(left1);
                    }
                }

                // 2. Nhận diện rủi ro Hàng Dọc (Check Vertical Match)
                if (y >= 2)
                {
                    TypeStone down1 = matrix[x, y - 1];
                    TypeStone down2 = matrix[x, y - 2];
                    if (down1 == down2)
                    {
                        blacklist.Add(down1);
                    }
                }

                // 3. Lọc danh sách khả dụng (Filter Available Candidates)
                candidatePool.Clear();
                for (int i = 0; i < defaultElements.Length; i++)
                {
                    TypeStone stone = defaultElements[i];
                    if (!blacklist.Contains(stone))
                    {
                        candidatePool.Add(stone);
                    }
                }

                // Theo nguyên lý Dirichlet: Blacklist tối đa 2 loại, candidatePool luôn >= 3 loại (không bao giờ rỗng)
                if (candidatePool.Count == 0)
                {
                    candidatePool.AddRange(defaultElements);
                }

                // 4. Random chốt kết quả (Commit Random Cell)
                TypeStone chosen = PickCandidateStone(candidatePool, targetStones);
                matrix[x, y] = chosen;
            }
        }

        return matrix;
    }

    private TypeStone PickCandidateStone(List<TypeStone> candidates, List<TypeStone> targets)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return defaultElements[UnityEngine.Random.Range(0, defaultElements.Length)];
        }

        if (targets == null || targets.Count == 0)
        {
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        // Tách danh sách ứng viên thành nhóm mục tiêu (targets) và nhóm còn lại (remains)
        _targetCandidatesBuffer.Clear();
        _remainCandidatesBuffer.Clear();

        for (int i = 0; i < candidates.Count; i++)
        {
            TypeStone c = candidates[i];
            if (targets.Contains(c)) _targetCandidatesBuffer.Add(c);
            else _remainCandidatesBuffer.Add(c);
        }

        // Áp dụng tỷ lệ rơi ưu tiên theo GDD (Mục tiêu 60% - Còn lại 40%)
        if (_targetCandidatesBuffer.Count > 0 && _remainCandidatesBuffer.Count > 0)
        {
            bool pickTarget = UnityEngine.Random.value < 0.60f;
            if (pickTarget)
            {
                return _targetCandidatesBuffer[UnityEngine.Random.Range(0, _targetCandidatesBuffer.Count)];
            }
            return _remainCandidatesBuffer[UnityEngine.Random.Range(0, _remainCandidatesBuffer.Count)];
        }

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    /// <summary>
    /// Khóa ngẫu nhiên một số viên đá trên bàn cờ Match-3 (Kỹ năng của quái Pháp sư)
    /// </summary>
    public void LockRandomStones(int count, int rounds = 1) => LockRandomStones(count, rounds, null);

    internal void LockRandomStones(int count, int rounds, Action<StoneBase> onLocked)
    {
        if (grid == null) return;
        _unlockedStonesBuffer.Clear();
        for (int x = 0; x < cols; x++)
        {
            if (x >= grid.Count) continue;
            for (int y = 0; y < rows; y++)
            {
                if (y >= grid[x].Count) continue;
                if (grid[x][y] != null && grid[x][y].GetStone() != null)
                {
                    StoneBase s = grid[x][y].GetStone();
                    if (!s.isLocked) _unlockedStonesBuffer.Add(s);
                }
            }
        }

        for (int i = 0; i < count && _unlockedStonesBuffer.Count > 0; i++)
        {
            int randIdx = UnityEngine.Random.Range(0, _unlockedStonesBuffer.Count);
            StoneBase chosen = _unlockedStonesBuffer[randIdx];
            chosen.LockStone(rounds);
            _unlockedStonesBuffer.RemoveAt(randIdx);
            onLocked?.Invoke(chosen);
        }
    }

    /// <summary>
    /// Đóng băng một hàng đá trên bàn cờ Match-3 (Kỹ năng của Tinh Linh Băng Giá)
    /// </summary>
    public void FreezeRow(int rowIndex, int rounds = 2) => FreezeRow(rowIndex, rounds, null);

    internal void FreezeRow(int rowIndex, int rounds, Action<StoneBase> onFrozen)
    {
        if (grid == null || rowIndex < 0 || rowIndex >= rows) return;
        for (int x = 0; x < cols; x++)
        {
            if (x < grid.Count && rowIndex < grid[x].Count && grid[x][rowIndex] != null && grid[x][rowIndex].GetStone() != null)
            {
                StoneBase stone = grid[x][rowIndex].GetStone();
                stone.FreezeStone(rounds);
                onFrozen?.Invoke(stone);
            }
        }
    }

    /// <summary>
    /// Giảm thời hạn khóa/đóng băng của toàn bộ đá sau mỗi Round
    /// </summary>
    public void TickStoneLocks()
    {
        if (grid == null) return;
        for (int x = 0; x < cols; x++)
        {
            if (x >= grid.Count) continue;
            for (int y = 0; y < rows; y++)
            {
                if (y >= grid[x].Count) continue;
                if (grid[x][y] != null && grid[x][y].GetStone() != null)
                {
                    grid[x][y].GetStone().TickLockRound();
                }
            }
        }
    }

    public void UpdateStoneToBoard()
    {
        // Khởi tạo ma trận 6x6 ngẫu nhiên 100% KHÔNG CÓ MATCH-3 theo thuật toán GDD
        TypeStone[,] noMatchMatrix = GenerateNoMatch3Matrix(cols, rows, targetTypeStones);

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector2 posTarget = grid[x][y].SlotItem.anchoredPosition;
                TypeStone stoneType = noMatchMatrix[x, y];
                grid[x][y].SetStone(InitStoneWithType(posTarget, new Vector2Int(x, y), stoneType, 0f));
            }
        }
    }

    public StoneBase InitStone(Vector2 posTarget, Vector2Int idx, float time)
    {
        TypeStone stoneTypeToSpawn;
        if (listData.ContainsKey(idx))
        {
            stoneTypeToSpawn = listData[idx];
            listData.Remove(idx);
        }
        else
        {
            stoneTypeToSpawn = GetGDDStoneDrop(targetTypeStones);
        }
        return InitStoneWithType(posTarget, idx, stoneTypeToSpawn, time);
    }

    public StoneBase InitStoneWithType(Vector2 posTarget, Vector2Int idx, TypeStone stoneTypeToSpawn, float time)
    {
        EnsureContainers();

        StoneBase prefab = GetStonePrefab(stoneTypeToSpawn);
        if (prefab == null) return null;

        Transform stoneParent = stonesContainer != null ? stonesContainer : center;
        Vector2 posInit = (time > 0f)
            ? (grid[idx.x][rows - 1].SlotItem.anchoredPosition + new Vector2(0, sizeSlot.y))
            : posTarget;

        Vector3 spawnPos = new Vector3(posInit.x, posInit.y, 0f);
        StoneBase stone = (ObjectPoolingManager.Instance != null)
            ? ObjectPoolingManager.Instance.Spawn(prefab, spawnPos, Quaternion.identity, stoneParent)
            : Instantiate(prefab, spawnPos, Quaternion.identity, stoneParent);

        if (stone.transform.parent != stoneParent)
        {
            stone.transform.SetParent(stoneParent, false);
        }
        // Luôn đảm bảo đá nổi trên các ô slot, z = 0 và scale = 1
        stone.transform.SetAsLastSibling();
        stone.transform.localPosition = spawnPos;
        stone.transform.localScale = Vector3.one;

        if (stone.item != null)
        {
            stone.item.sizeDelta = sizeSlot;
            stone.item.anchoredPosition = posInit;
            stone.item.localScale = Vector3.one;
        }

        stone.SetOriginIndex(idx.x, idx.y);

        if (time > 0f)
        {
            stone.StoneTrans(posTarget, time);
        }
        else
        {
            if (stone.item != null)
            {
                stone.item.DOKill();
                stone.item.anchoredPosition = posTarget;
            }
            stone.transform.localPosition = new Vector3(posTarget.x, posTarget.y, 0f);
        }

        return stone;
    }

    public IEnumerator MatchAllBoard() => MatchAllBoard(null, false);

    private IEnumerator MatchAllBoard(Vector2Int? origin, bool isCascade = false)
    {
        _isResolvingMatch = true;
        HashSet<Vector2Int> listMatch = new HashSet<Vector2Int>();
        matchVfxGraph.Clear();
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (grid[x][y].GetStone() == null) continue;
                TypeStone name = grid[x][y].GetStone().nameStone;

                // Kiểm tra hàng ngang
                if (x + 2 < cols)
                {
                    if (grid[x + 1][y].GetStone() != null && grid[x + 2][y].GetStone() != null)
                    {
                        if (name == grid[x + 1][y].GetStone().nameStone && name == grid[x + 2][y].GetStone().nameStone)
                        {
                            listMatch.Add(new Vector2Int(x, y));
                            listMatch.Add(new Vector2Int(x + 1, y));
                            listMatch.Add(new Vector2Int(x + 2, y));
                            matchVfxGraph.AddRun(new Vector2Int(x, y), Vector2Int.right, 3);
                        }
                    }
                }

                // Kiểm tra hàng dọc
                if (y + 2 < rows)
                {
                    if (grid[x][y + 1].GetStone() != null && grid[x][y + 2].GetStone() != null)
                    {
                        if (name == grid[x][y + 1].GetStone().nameStone && name == grid[x][y + 2].GetStone().nameStone)
                        {
                            listMatch.Add(new Vector2Int(x, y));
                            listMatch.Add(new Vector2Int(x, y + 1));
                            listMatch.Add(new Vector2Int(x, y + 2));
                            matchVfxGraph.AddRun(new Vector2Int(x, y), Vector2Int.up, 3);
                        }
                    }
                }
            }
        }

        if (listMatch.Count == 0)
        {
            _isResolvingMatch = false;
            canTrans = true;
            yield break;
        }

        int totalMatchedStones = listMatch.Count;
        _pendingMatchBatchBuffer.Clear();
        if (SkillPassiveManager.instance != null)
        {
            try
            {
                totalMatchedStones = SkillPassiveManager.instance.PrepareMatchAttacks(listMatch, grid, _pendingMatchBatchBuffer);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BoardController] Lỗi khi chuẩn bị tấn công kỹ năng: {ex.Message}");
            }
        }

        if (!matchVfx) matchVfx = MatchStoneVfx.Create(center);
        bool playedBoardVfx = false;
        if (matchVfx)
        {
            matchVfxGraph.Resolve(listMatch, origin);
            playedBoardVfx = matchVfx.TryPlay(matchVfxGraph, grid);
        }

        foreach (Vector2Int e in listMatch)
        {
            SlotStoneBase slot = grid[e.x][e.y];
            if (slot.GetStone() != null)
            {
                slot.GetStone().FinishTask(playedBoardVfx);
                slot.SetStone(null);
            }
        }

        yield return new WaitForSeconds(0.3f);
        float maxTime = 0;

        for (int x = 0; x < cols; x++)
        {
            int slotEmpty = 0;
            for (int y = 0; y < rows; y++)
            {
                StoneBase stoneCurrent = grid[x][y].GetStone();
                if (stoneCurrent != null)
                {
                    if (slotEmpty < y)
                    {
                        grid[x][y].SetStone(null);
                        float moveDuration = (y - slotEmpty) * 0.15f;
                        if (moveDuration > maxTime) maxTime = moveDuration;
                        stoneCurrent.StoneTrans(grid[x][slotEmpty].SlotItem.anchoredPosition, moveDuration);
                        stoneCurrent.SetOriginIndex(x, slotEmpty);
                        grid[x][slotEmpty].SetStone(stoneCurrent);
                    }
                    slotEmpty++;
                }
            }

            for (int t = slotEmpty; t < rows; t++)
            {
                float fallDuration = (rows - t) * 0.15f;
                if (fallDuration > maxTime) maxTime = fallDuration;
                grid[x][t].SetStone(InitStone(grid[x][t].SlotItem.anchoredPosition, new Vector2Int(x, t), fallDuration));
            }
        }

        yield return new WaitForSeconds(maxTime + 0.05f);

        bool shouldPlayZoom = totalMatchedStones >= bigMatchZoomThreshold
            && (!isCascade || zoomOnCascadeMatch)
            && CombatCameraController.Instance != null;

        if (shouldPlayZoom)
        {
            Player activePlayer = SkillPassiveManager.instance != null
                ? SkillPassiveManager.instance.GetActivePlayer()
                : (Player.Instance != null ? Player.Instance : FindFirstObjectByType<Player>());
            if (activePlayer != null)
            {
                yield return CombatCameraController.Instance.PlayFocusZoomRoutine(activePlayer.transform, bigMatchZoomDuration);
            }
        }

        if (GameManager.instance != null && !GameManager.instance.IsWaveCombatActive)
        {
            _pendingMatchBatchBuffer.Clear();
            _isResolvingMatch = false;
            yield break;
        }

        if (SkillPassiveManager.instance != null && _pendingMatchBatchBuffer.Count > 0)
        {
            try
            {
                SkillPassiveManager.instance.ExecutePreparedMatchBatch(_pendingMatchBatchBuffer);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BoardController] Lỗi khi thực thi tấn công kỹ năng: {ex.Message}");
            }
            _pendingMatchBatchBuffer.Clear();
        }

        StartCoroutine(MatchAllBoard(null, true));
    }

    public void Trans(Direction type, StoneBase stone)
    {
        if (!canTrans) return;
        if (stone != null && stone.isLocked)
        {
            stone.transform.DOShakePosition(0.2f, 6f);
            return;
        }

        // Kiểm tra chi phí Lõi Năng Lượng trước khi cho phép swap (GDD Rule)
        if (CombatTurnManager.Instance != null && !CombatTurnManager.Instance.CanPlayerAffordNextMatch())
        {
            CombatTurnManager.Instance.CheckAndHandleEnergyDepletion();
            return;
        }

        if (!CheckTrans(type, stone.GetOriginIndex())) return;

        canTrans = false;
        _isResolvingMatch = true;
        StartCoroutine(TryMatch(type, stone));
    }

    /// <summary>
    /// Khóa toàn bộ tương tác trên bàn cờ khi kết thúc wave / thời điểm reset
    /// </summary>
    public void LockBoardInteractions()
    {
        canTrans = false;
        if (GameManager.instance != null && GameManager.instance.LockBoard != null)
        {
            GameManager.instance.LockBoard.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Mở khóa tương tác bàn cờ khi bắt đầu vào trận
    /// </summary>
    public void UnlockBoardInteractions()
    {
        canTrans = true;
        _isResolvingMatch = false;
        if (GameManager.instance != null && GameManager.instance.LockBoard != null)
        {
            GameManager.instance.LockBoard.gameObject.SetActive(false);
        }
    }

    public IEnumerator TryMatch(Direction type, StoneBase stone)
    {
        Vector2Int Index = stone.GetOriginIndex();
        StoneBase stoneB = null;

        switch (type)
        {
            case Direction.right:
                stoneB = grid[Index.x + 1][Index.y].GetStone();
                break;
            case Direction.left:
                stoneB = grid[Index.x - 1][Index.y].GetStone();
                break;
            case Direction.up:
                stoneB = grid[Index.x][Index.y + 1].GetStone();
                break;
            case Direction.down:
                stoneB = grid[Index.x][Index.y - 1].GetStone();
                break;
        }

        if (stoneB == null || stoneB.isLocked)
        {
            if (stoneB != null && stoneB.isLocked)
            {
                stoneB.transform.DOShakePosition(0.2f, 6f);
            }
            _isResolvingMatch = false;
            canTrans = true;
            yield break;
        }

        Vector2Int IndexB = stoneB.GetOriginIndex();
        TrySwap(stone, stoneB);
        grid[Index.x][Index.y].SwapStoneToSlot(grid[IndexB.x][IndexB.y]);

        yield return new WaitForSeconds(0.35f);

        bool firstStoneMatched = CheckMatch(stone.nameStone, IndexB.x, IndexB.y);
        if (firstStoneMatched || CheckMatch(stoneB.nameStone, Index.x, Index.y))
        {
            if (CombatTurnManager.Instance != null)
            {
                CombatTurnManager.Instance.TryConsumeMatchEnergy();
            }
            StartCoroutine(MatchAllBoard(firstStoneMatched ? IndexB : Index, false));
        }
        else
        {
            TrySwap(stone, stoneB);
            grid[Index.x][Index.y].SwapStoneToSlot(grid[IndexB.x][IndexB.y]);
            yield return new WaitForSeconds(0.35f);
            _isResolvingMatch = false;
            canTrans = true;
        }
    }

    public void TrySwap(StoneBase stoneA, StoneBase stoneB)
    {
        if (stoneA != null) stoneA.transform.SetAsLastSibling();
        if (stoneB != null) stoneB.transform.SetAsLastSibling();
        Vector2 posA = stoneA.item.anchoredPosition;
        Vector2 posB = stoneB.item.anchoredPosition;
        stoneA.StoneTrans(posB, 0.3f);
        stoneB.StoneTrans(posA, 0.3f);
    }

    public bool CheckMatch(TypeStone nameStone, int x, int y)
    {
        int demUp = 0, demDown = 0, demRight = 0, demLeft = 0;
        for (int i = 1; i <= 2; i++)
        {
            if (x - i >= 0)
            {
                if (grid[x - i][y].GetStone() == null) return false;
                if (nameStone == grid[x - i][y].GetStone().nameStone) demLeft++;
                else demLeft--;
            }
            if (x + i < cols)
            {
                if (grid[x + i][y].GetStone() == null) return false;
                if (nameStone == grid[x + i][y].GetStone().nameStone) demRight++;
                else demRight--;
            }
            if (y + i < rows)
            {
                if (grid[x][y + i].GetStone() == null) return false;
                if (nameStone == grid[x][y + i].GetStone().nameStone) demUp++;
                else demUp--;
            }
            if (y - i >= 0)
            {
                if (grid[x][y - i].GetStone() == null) return false;
                if (nameStone == grid[x][y - i].GetStone().nameStone) demDown++;
                else demDown--;
            }
            if ((demDown == 1 && demUp == 1) || (demLeft == 1 && demRight == 1)) return true;
        }

        return (demDown == 2 || demLeft == 2 || demRight == 2 || demUp == 2);
    }

    public bool CheckTrans(Direction type, Vector2 Index)
    {
        if (type == Direction.right && Index.x >= cols - 1) return false;
        if (type == Direction.left && Index.x <= 0) return false;
        if (type == Direction.up && Index.y >= rows - 1) return false;
        if (type == Direction.down && Index.y <= 0) return false;
        return true;
    }

    /// <summary>
    /// Hiệu ứng trượt bàn cờ lên giữa màn hình khớp đúng vị trí DialoguePanel (GDD Item B17)
    /// </summary>
    public void SlideInBoard(float duration = 0.45f, Action onComplete = null)
    {
        if (center != null)
        {
            center.DOKill();
            float targetY = GetTargetSlideInY();
            center.DOAnchorPosY(targetY, duration).SetEase(Ease.OutBack).OnComplete(() => onComplete?.Invoke());
        }
    }

    /// <summary>
    /// Tính toán vị trí Y khi trượt bàn cờ lên: Cố định chuẩn xác tại y = -580f ngay bên dưới PlayerStatsBar
    /// </summary>
    public float GetTargetSlideInY()
    {
        return -510f;
    }

    /// <summary>
    /// Hiệu ứng trượt bàn cờ xuống dưới biến mất khi vào giai đoạn combat (GDD Item B21)
    /// </summary>
    public void SlideOutBoard(float duration = 0.4f, Action onComplete = null)
    {
        if (center != null)
        {
            center.DOKill();
            center.DOAnchorPosY(-1600f, duration).SetEase(Ease.InCubic).OnComplete(() =>
            {
                onComplete?.Invoke();
            });
        }
    }
}
