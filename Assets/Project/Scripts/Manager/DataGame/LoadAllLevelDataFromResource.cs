using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Phụ trách nạp toàn bộ LevelDataSO từ thư mục Resources tại PrepareScene
/// rồi bàn giao mảng dữ liệu sang GameDataManager lưu trữ xuyên suốt các scene.
/// </summary>
public class LoadAllLevelDataFromResource : MonoBehaviour
{
    public const string DefaultLevelFolder = "ScriptObject/LevelData";
    public const string DefaultLevelPrefix = "ScriptObject/LevelData/LevelDataSO_lv_";

    public static LoadAllLevelDataFromResource instance;

    [Header("Level Resource Config")]
    [SerializeField] private string levelFolderPath = DefaultLevelFolder;
    [SerializeField] private string levelAssetPrefix = DefaultLevelPrefix;
    [SerializeField] private int startLevelIndex = 1;
    [SerializeField] private int stopAfterConsecutiveMisses = 2;
    [SerializeField] private int maxLevelProbe = 50;

    [Header("Loaded Output (Transferred to GameDataManager)")]
    [SerializeField] private LevelDataSO[] loadedLevels = Array.Empty<LevelDataSO>();
    private LevelDataSO dataSO;

    public LevelDataSO[] LoadedLevels => loadedLevels;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public LevelData GetLevelData()
    {
        return dataSO != null ? dataSO.GetData() : null;
    }

    public void LoadLevel(int lvCurrent)
    {
        StartCoroutine(LoadFromResource(lvCurrent));
    }

    /// <summary>
    /// Nạp bất đồng bộ toàn bộ LevelDataSO từ Resources và chuyển mảng sang GameDataManager.
    /// </summary>
    public IEnumerator LoadAllAndTransferAsync(GameDataManager targetManager, Action<float> onProgress = null)
    {
        if (targetManager != null && targetManager.HasLevelData)
        {
            loadedLevels = CopyArray(targetManager.LevelDataArray);
            dataSO = loadedLevels.Length > 0 ? loadedLevels[0] : null;
            onProgress?.Invoke(1f);
            yield break;
        }

        List<LevelDataSO> results = new List<LevelDataSO>(8);
        int consecutiveMisses = 0;

        for (int lv = Mathf.Max(1, startLevelIndex); lv <= Mathf.Max(1, maxLevelProbe); lv++)
        {
            string path = $"{levelAssetPrefix}{lv:D3}";
            ResourceRequest request = Resources.LoadAsync<LevelDataSO>(path);
            while (!request.isDone)
            {
                float stepFraction = Mathf.Clamp01((lv - startLevelIndex + request.progress) / Mathf.Max(1, stopAfterConsecutiveMisses + results.Count + 1));
                onProgress?.Invoke(Mathf.Min(0.95f, stepFraction));
                yield return null;
            }

            if (request.asset is LevelDataSO levelAsset)
            {
                results.Add(levelAsset);
                consecutiveMisses = 0;
            }
            else
            {
                consecutiveMisses++;
                if (results.Count > 0 && consecutiveMisses >= Mathf.Max(1, stopAfterConsecutiveMisses))
                {
                    break;
                }
            }
        }

        // Dự phòng: nếu tên file không đánh số liên tục theo mẫu _lv_001, quét toàn bộ thư mục một lần.
        if (results.Count == 0 && !string.IsNullOrEmpty(levelFolderPath))
        {
            LevelDataSO[] fallbackAll = Resources.LoadAll<LevelDataSO>(levelFolderPath);
            if (fallbackAll != null && fallbackAll.Length > 0)
            {
                results.AddRange(fallbackAll);
            }
        }

        SortLevelsAscending(results);
        loadedLevels = results.ToArray();
        dataSO = loadedLevels.Length > 0 ? loadedLevels[0] : null;

        if (targetManager != null)
        {
            targetManager.SetLevelDataArray(loadedLevels);
        }

        onProgress?.Invoke(1f);
    }

    /// <summary>
    /// Nạp đồng bộ dùng cho fallback khi khởi chạy trực tiếp từ MainScene/HomeScene trong quá trình dev.
    /// </summary>
    public static LevelDataSO[] LoadAllLevelsSync(string folderPath = DefaultLevelFolder)
    {
        LevelDataSO[] raw = Resources.LoadAll<LevelDataSO>(folderPath);
        if (raw == null || raw.Length == 0)
        {
            return Array.Empty<LevelDataSO>();
        }

        List<LevelDataSO> sorted = new List<LevelDataSO>(raw.Length);
        for (int i = 0; i < raw.Length; i++)
        {
            if (raw[i] != null)
            {
                sorted.Add(raw[i]);
            }
        }

        SortLevelsAscending(sorted);
        return sorted.ToArray();
    }

    private IEnumerator LoadFromResource(int lv)
    {
        int normalizedLevel = Mathf.Max(1, lv);
        string path = $"{levelAssetPrefix}{normalizedLevel:D3}";
        ResourceRequest request = Resources.LoadAsync<LevelDataSO>(path);
        yield return request;

        if (request.asset is LevelDataSO loaded)
        {
            dataSO = loaded;
        }
        else
        {
            Debug.LogWarning($"[LoadAllLevelDataFromResource] Không tìm thấy LevelDataSO tại '{path}'.");
        }
    }

    private static void SortLevelsAscending(List<LevelDataSO> list)
    {
        list.Sort((a, b) =>
        {
            int levelA = ResolveSortOrder(a);
            int levelB = ResolveSortOrder(b);
            int cmp = levelA.CompareTo(levelB);
            if (cmp != 0) return cmp;
            return string.CompareOrdinal(a != null ? a.name : string.Empty, b != null ? b.name : string.Empty);
        });
    }

    private static int ResolveSortOrder(LevelDataSO levelSO)
    {
        if (levelSO == null) return int.MaxValue;

        LevelData data = levelSO.GetData();
        if (data != null && data.GetLevel() > 0)
        {
            return data.GetLevel();
        }

        // Nếu asset mẫu chưa điền trường level (> 0), tách số hậu tố từ tên file (vd: LevelDataSO_lv_002 -> 2)
        // để không đẩy nhầm asset trống lên trước Level 1.
        string name = levelSO.name;
        int idx = name.LastIndexOf('_');
        if (idx >= 0 && idx + 1 < name.Length && int.TryParse(name.Substring(idx + 1), out int parsed) && parsed > 0)
        {
            return parsed;
        }

        return int.MaxValue - 1;
    }

    private static LevelDataSO[] CopyArray(IReadOnlyList<LevelDataSO> source)
    {
        if (source == null || source.Count == 0)
        {
            return Array.Empty<LevelDataSO>();
        }

        LevelDataSO[] copy = new LevelDataSO[source.Count];
        for (int i = 0; i < source.Count; i++)
        {
            copy[i] = source[i];
        }
        return copy;
    }
}
