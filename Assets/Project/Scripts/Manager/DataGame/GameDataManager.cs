using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Trung tâm quản lý dữ liệu dùng chung và điều phối chuyển scene (DontDestroyOnLoad).
/// Tài nguyên tĩnh được gắn trực tiếp trên Scene/Prefab để build an toàn không cần code Editor;
/// dữ liệu động được preload ở PrepareScene để các màn chơi vào trận ngay không bị khựng.
/// </summary>
public class GameDataManager : MonoBehaviour
{
    public const string ManagerPrefabResourcePath = "Prefab/Manager/GameDataManager";

    public static GameDataManager instance;
    public static bool HasInstance => instance != null;
    public static GameDataManager Instance => instance != null ? instance : EnsureRuntimeInstance();

    [Header("Level State & Data Array")]
    public int levelCurrent = 0;
    [SerializeField] private LevelDataSO[] levelDataArray = Array.Empty<LevelDataSO>();

    [Header("Core ScriptableObject Databases (Scene/Prefab Wired)")]
    [SerializeField] private BuffDebuffDatabase buffDebuffDatabase;
    [SerializeField] private DialogueDatabaseSO dialogueDatabase;
    [SerializeField] private EnemyDatabaseSO enemyDatabase;
    [SerializeField] private PlayerDataSO defaultPlayerData;

    [Header("UI Font (Scene/Prefab Wired - Avoids Editor-Only AssetDatabase)")]
    [SerializeField] private TMP_FontAsset bannerFont;

    [Header("Gameplay & Enemy Prefabs")]
    [SerializeField] private GameObject skillContainerPrefab;
    [SerializeField] private GameObject enemyProjectilePrefab;
    [SerializeField] private EnemyBase defaultEnemyPrefab;
    [SerializeField] private StoneBase[] stonePrefabs = Array.Empty<StoneBase>();

    [Header("VFX Prefabs, Atlases & Shaders")]
    [SerializeField] private EnemyAttackVfxArt enemyAttackVfxArt;
    [SerializeField] private GameObject[] enemySkillVfxPrefabs = Array.Empty<GameObject>();
    [SerializeField] private GameObject[] matchStoneVfxPrefabs = Array.Empty<GameObject>();
    [SerializeField] private Shader waterSpriteShader;
    [SerializeField] private Texture2D earthCommonAtlas;
    [SerializeField] private Texture2D earthBossesAtlas;
    [SerializeField] private Texture2D metalCommonAtlas;
    [SerializeField] private Texture2D metalBossesAtlas;
    [SerializeField] private Texture2D waterCommonAtlas;
    [SerializeField] private Texture2D waterBossesAtlas;
    [SerializeField] private Texture2D woodCommonAtlas;
    [SerializeField] private Texture2D woodBossesAtlas;
    [SerializeField] private Texture2D enemyStatusAtlas;
    [SerializeField] private Shader enemyStatusBurnShader;

    [Header("Scene Flow Config")]
    [SerializeField] private string homeSceneName = "HomeScene";
    [SerializeField] private string mainSceneName = "MainScene";

    private EnemyDataSO[] _enemyDataArray = Array.Empty<EnemyDataSO>();
    private readonly Dictionary<EnemyType, EnemyDataSO> _enemyDataMap = new Dictionary<EnemyType, EnemyDataSO>(40);
    private readonly Dictionary<EnemyType, EnemyBase> _enemyPrefabMap = new Dictionary<EnemyType, EnemyBase>(40);
    private readonly Dictionary<TypeStone, StoneBase> _stonePrefabMap = new Dictionary<TypeStone, StoneBase>(8);
    private readonly Dictionary<ElementType, GameObject> _enemySkillVfxMap = new Dictionary<ElementType, GameObject>(6);
    private readonly RawImage[] _matchStoneVfxCache = new RawImage[10];

    private bool _isPreloaded;
    private bool _isTransitioningScene;

    public bool IsPreloaded => _isPreloaded;
    public bool HasLevelData => levelDataArray != null && levelDataArray.Length > 0;
    public IReadOnlyList<LevelDataSO> LevelDataArray => levelDataArray;
    public BuffDebuffDatabase BuffDebuffDatabase => buffDebuffDatabase;
    public DialogueDatabaseSO DialogueDatabase => dialogueDatabase;
    public EnemyDatabaseSO EnemyDatabase => enemyDatabase;
    public PlayerDataSO DefaultPlayerData => defaultPlayerData;
    public TMP_FontAsset BannerFont => bannerFont;
    public GameObject SkillContainerPrefab => skillContainerPrefab;
    public GameObject EnemyProjectilePrefab => enemyProjectilePrefab;
    public EnemyBase DefaultEnemyPrefab => defaultEnemyPrefab;
    public EnemyAttackVfxArt EnemyAttackVfxArt => enemyAttackVfxArt;
    public Texture2D EnemyStatusAtlas => enemyStatusAtlas;
    public Shader EnemyStatusBurnShader => enemyStatusBurnShader;
    public string HomeSceneName => homeSceneName;
    public string MainSceneName => mainSceneName;

    public LevelDataSO CurrentLevelData => GetLevelData(levelCurrent > 0 ? levelCurrent : 1);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapAfterSceneLoad()
    {
        if (instance == null)
        {
            EnsureRuntimeInstance();
        }
    }

    private static GameDataManager EnsureRuntimeInstance()
    {
        if (!Application.isPlaying) return null;
        if (instance != null) return instance;

        GameDataManager existing = FindFirstObjectByType<GameDataManager>(FindObjectsInactive.Include);
        if (existing != null)
        {
            existing.InitializeSingletonIfNeeded();
            existing.EnsurePreloadedSync();
            return instance;
        }

        GameObject prefabAsset = Resources.Load<GameObject>(ManagerPrefabResourcePath);
        GameDataManager created = null;
        if (prefabAsset != null)
        {
            GameObject go = Instantiate(prefabAsset);
            go.name = nameof(GameDataManager);
            created = go.GetComponent<GameDataManager>();
        }

        if (created == null)
        {
            GameObject go = new GameObject(nameof(GameDataManager));
            created = go.AddComponent<GameDataManager>();
        }

        created.InitializeSingletonIfNeeded();
        created.EnsurePreloadedSync();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        InitializeSingletonIfNeeded();
        BuildStaticLookups();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _isTransitioningScene = false;
    }

    private void InitializeSingletonIfNeeded()
    {
        if (instance == null)
        {
            instance = this;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
    }

    /// <summary>
    /// Nhận mảng LevelDataSO sau khi LoadAllLevelDataFromResource hoàn tất tải ở PrepareScene.
    /// </summary>
    public void SetLevelDataArray(LevelDataSO[] levels)
    {
        levelDataArray = levels ?? Array.Empty<LevelDataSO>();
    }

    public void SelectLevel(int levelNumber)
    {
        levelCurrent = Mathf.Max(1, levelNumber);
    }

    public LevelDataSO GetLevelData(int levelNumber)
    {
        if (levelDataArray == null || levelDataArray.Length == 0)
        {
            return null;
        }

        int targetLevel = Mathf.Max(1, levelNumber);
        for (int i = 0; i < levelDataArray.Length; i++)
        {
            LevelDataSO candidate = levelDataArray[i];
            if (candidate != null && candidate.GetData() != null && candidate.GetData().GetLevel() == targetLevel)
            {
                return candidate;
            }
        }

        int fallbackIndex = Mathf.Clamp(targetLevel - 1, 0, levelDataArray.Length - 1);
        return levelDataArray[fallbackIndex];
    }

    /// <summary>
    /// Preload bất đồng bộ theo giai đoạn tại PrepareScene (không khóa main thread giữa trận).
    /// </summary>
    public IEnumerator PreloadRemainingDataAsync(Action<float, string> onProgress = null)
    {
        if (_isPreloaded)
        {
            onProgress?.Invoke(1f, "Dữ liệu đã sẵn sàng");
            yield break;
        }

        onProgress?.Invoke(0.05f, "Đang đồng bộ cơ sở dữ liệu...");
        EnsureCoreFallbacksSync();
        BuildStaticLookups();
        yield return null;

        onProgress?.Invoke(0.35f, "Đang nạp dữ liệu quái vật...");
        LoadEnemiesAndPrefabs();
        yield return null;

        onProgress?.Invoke(0.65f, "Đang khởi tạo hiệu ứng chiến đấu...");
        EnemyEarthVfxArt.Load();
        EnemyMetalVfxArt.Load();
        yield return null;

        onProgress?.Invoke(0.82f, "Đang khởi tạo shader & trạng thái...");
        EnemyWaterVfxArt.Load();
        EnemyWoodVfxArt.Load();
        EnemyAttackVfxArt.Load();
        EnemyStatusVfx.PreloadAssets();
        yield return null;

        _isPreloaded = true;
        onProgress?.Invoke(1f, "Hoàn tất chuẩn bị!");
    }

    /// <summary>
    /// Nạp đồng bộ khi chạy trực tiếp scene con mà không đi qua PrepareScene.
    /// </summary>
    public void EnsurePreloadedSync()
    {
        if (_isPreloaded) return;

        if (!HasLevelData)
        {
            SetLevelDataArray(LoadAllLevelDataFromResource.LoadAllLevelsSync());
        }

        EnsureCoreFallbacksSync();
        BuildStaticLookups();
        LoadEnemiesAndPrefabs();
        WarmupVfxCaches();
        _isPreloaded = true;
    }

    private void EnsureCoreFallbacksSync()
    {
        if (buffDebuffDatabase == null)
        {
            buffDebuffDatabase = Resources.Load<BuffDebuffDatabase>("Database/BuffDebuffDatabase");
        }
        if (dialogueDatabase == null)
        {
            dialogueDatabase = Resources.Load<DialogueDatabaseSO>("Database/DialogueDatabase");
        }
        if (enemyDatabase == null)
        {
            enemyDatabase = Resources.Load<EnemyDatabaseSO>("Database/EnemyDatabase");
        }
        if (defaultPlayerData == null)
        {
            defaultPlayerData = Resources.Load<PlayerDataSO>("ScriptObject/Character/Player/PlayerData_Default");
        }
        if (skillContainerPrefab == null)
        {
            skillContainerPrefab = Resources.Load<GameObject>("Prefab/Base/Skill/SkillContainer");
        }
        if (enemyProjectilePrefab == null)
        {
            enemyProjectilePrefab = Resources.Load<GameObject>("Prefab/Base/Enemy/EnemyProjectile");
        }
        if (defaultEnemyPrefab == null)
        {
            defaultEnemyPrefab = Resources.Load<EnemyBase>("Prefab/Base/Enemy/Enemy");
        }
        if (stonePrefabs == null || stonePrefabs.Length == 0)
        {
            stonePrefabs = Resources.LoadAll<StoneBase>("Prefab/Base/Stone") ?? Array.Empty<StoneBase>();
        }
        if (enemyAttackVfxArt == null)
        {
            enemyAttackVfxArt = Resources.Load<EnemyAttackVfxArt>("ScriptObject/VFX/EnemyFireAttackArt");
        }
        if (enemyStatusAtlas == null)
        {
            enemyStatusAtlas = Resources.Load<Texture2D>(EnemyStatusAnimation.AtlasResource);
        }
        if (enemyStatusBurnShader == null)
        {
            enemyStatusBurnShader = Resources.Load<Shader>(EnemyStatusAnimation.BurnShaderResource);
        }
        if (waterSpriteShader == null)
        {
            waterSpriteShader = Resources.Load<Shader>("VFX/EnemyWater/WaterSprite");
        }
        if (bannerFont == null)
        {
            bannerFont = Resources.Load<TMP_FontAsset>("Fonts/Nunito_Black_SDF");
        }
        if (enemySkillVfxPrefabs == null || enemySkillVfxPrefabs.Length == 0)
        {
            enemySkillVfxPrefabs = Resources.LoadAll<GameObject>("Prefab/VFX") ?? Array.Empty<GameObject>();
        }
        if (matchStoneVfxPrefabs == null || matchStoneVfxPrefabs.Length == 0)
        {
            matchStoneVfxPrefabs = Resources.LoadAll<GameObject>(MatchStoneVfxProfile.ResourceFolder) ?? Array.Empty<GameObject>();
        }
    }

    private void BuildStaticLookups()
    {
        _stonePrefabMap.Clear();
        if (stonePrefabs != null)
        {
            for (int i = 0; i < stonePrefabs.Length; i++)
            {
                StoneBase stone = stonePrefabs[i];
                if (stone != null && !_stonePrefabMap.ContainsKey(stone.nameStone))
                {
                    _stonePrefabMap.Add(stone.nameStone, stone);
                }
            }
        }

        _enemySkillVfxMap.Clear();
        if (enemySkillVfxPrefabs != null)
        {
            ElementType[] elements = (ElementType[])Enum.GetValues(typeof(ElementType));
            for (int i = 0; i < enemySkillVfxPrefabs.Length; i++)
            {
                GameObject vfx = enemySkillVfxPrefabs[i];
                if (vfx == null) continue;

                for (int e = 0; e < elements.Length; e++)
                {
                    if (string.Equals(vfx.name, $"VFX_Template_{elements[e]}", StringComparison.Ordinal))
                    {
                        _enemySkillVfxMap[elements[e]] = vfx;
                        break;
                    }
                }
            }
        }

        Array.Clear(_matchStoneVfxCache, 0, _matchStoneVfxCache.Length);
        if (matchStoneVfxPrefabs != null)
        {
            ElementType[] elements = (ElementType[])Enum.GetValues(typeof(ElementType));
            for (int i = 0; i < matchStoneVfxPrefabs.Length; i++)
            {
                GameObject prefab = matchStoneVfxPrefabs[i];
                if (prefab == null || !prefab.TryGetComponent(out RawImage image)) continue;

                for (int e = 0; e < elements.Length; e++)
                {
                    ElementType element = elements[e];
                    if (string.Equals(prefab.name, MatchStoneVfxProfile.PrefabName(element, false), StringComparison.Ordinal))
                    {
                        _matchStoneVfxCache[GetMatchVfxIndex(element, false)] = image;
                        break;
                    }
                    if (string.Equals(prefab.name, MatchStoneVfxProfile.PrefabName(element, true), StringComparison.Ordinal))
                    {
                        _matchStoneVfxCache[GetMatchVfxIndex(element, true)] = image;
                        break;
                    }
                }
            }
        }
    }

    private void LoadEnemiesAndPrefabs()
    {
        _enemyDataMap.Clear();
        _enemyPrefabMap.Clear();

        _enemyDataArray = Resources.LoadAll<EnemyDataSO>("ScriptObject/Character/Enemies") ?? Array.Empty<EnemyDataSO>();
        bool missingPrefabOnAnySO = false;

        for (int i = 0; i < _enemyDataArray.Length; i++)
        {
            EnemyDataSO so = _enemyDataArray[i];
            if (so == null) continue;

            if (!_enemyDataMap.ContainsKey(so.enemyType))
            {
                _enemyDataMap.Add(so.enemyType, so);
            }

            if (so.enemyPrefab != null)
            {
                if (!_enemyPrefabMap.ContainsKey(so.enemyType))
                {
                    _enemyPrefabMap.Add(so.enemyType, so.enemyPrefab);
                }
            }
            else
            {
                missingPrefabOnAnySO = true;
            }
        }

        // Chỉ quét toàn bộ thư mục prefab quái nếu thiếu prefab mặc định hoặc có EnemyDataSO chưa gắn enemyPrefab.
        if (defaultEnemyPrefab == null || missingPrefabOnAnySO || _enemyPrefabMap.Count == 0)
        {
            EnemyBase[] loadedPrefabs = Resources.LoadAll<EnemyBase>("Prefab/Base/Enemy");
            if (loadedPrefabs != null)
            {
                for (int i = 0; i < loadedPrefabs.Length; i++)
                {
                    EnemyBase enemy = loadedPrefabs[i];
                    if (enemy == null) continue;

                    if (!_enemyPrefabMap.ContainsKey(enemy.type))
                    {
                        _enemyPrefabMap.Add(enemy.type, enemy);
                    }
                    if (defaultEnemyPrefab == null)
                    {
                        defaultEnemyPrefab = enemy;
                    }
                }
            }
        }
    }

    private void WarmupVfxCaches()
    {
        EnemyEarthVfxArt.Load();
        EnemyMetalVfxArt.Load();
        EnemyWaterVfxArt.Load();
        EnemyWoodVfxArt.Load();
        EnemyAttackVfxArt.Load();
        EnemyStatusVfx.PreloadAssets();
    }

    public bool TryGetEnemyData(EnemyType type, out EnemyDataSO data)
    {
        return _enemyDataMap.TryGetValue(type, out data) && data != null;
    }

    public void CopyEnemyDataTo(Dictionary<EnemyType, EnemyDataSO> destination)
    {
        if (destination == null) return;
        foreach (var kvp in _enemyDataMap)
        {
            if (kvp.Value != null && !destination.ContainsKey(kvp.Key))
            {
                destination.Add(kvp.Key, kvp.Value);
            }
        }
    }

    public bool TryCopyEnemyPrefabsTo(Dictionary<EnemyType, EnemyBase> destination, ref EnemyBase defaultPrefab)
    {
        if (destination == null || _enemyPrefabMap.Count == 0) return false;

        foreach (var kvp in _enemyPrefabMap)
        {
            if (kvp.Value != null && !destination.ContainsKey(kvp.Key))
            {
                destination.Add(kvp.Key, kvp.Value);
            }
        }

        if (defaultPrefab == null)
        {
            defaultPrefab = defaultEnemyPrefab;
        }
        return destination.Count > 0;
    }

    public bool TryCopyStonePrefabsTo(Dictionary<TypeStone, StoneBase> destination)
    {
        if (destination == null || _stonePrefabMap.Count == 0) return false;

        foreach (var kvp in _stonePrefabMap)
        {
            if (kvp.Value != null && !destination.ContainsKey(kvp.Key))
            {
                destination.Add(kvp.Key, kvp.Value);
            }
        }
        return destination.Count > 0;
    }

    public bool TryGetEnemySkillVfxPrefab(ElementType element, out GameObject prefab)
    {
        if (_enemySkillVfxMap.TryGetValue(element, out prefab) && prefab != null)
        {
            return true;
        }

        prefab = Resources.Load<GameObject>($"Prefab/VFX/VFX_Template_{element}");
        if (prefab != null)
        {
            _enemySkillVfxMap[element] = prefab;
            return true;
        }
        return false;
    }

    public bool TryGetMatchStoneVfxPrefab(ElementType element, bool link, out RawImage prefab)
    {
        int index = GetMatchVfxIndex(element, link);
        if (index >= 0 && index < _matchStoneVfxCache.Length)
        {
            prefab = _matchStoneVfxCache[index];
            return prefab != null;
        }

        prefab = null;
        return false;
    }

    public bool TryGetEnemyAtlasVfxAssets(string prefix, out Texture2D common, out Texture2D bosses, out Shader shader)
    {
        shader = waterSpriteShader;
        switch (prefix)
        {
            case "Earth":
                common = earthCommonAtlas;
                bosses = earthBossesAtlas;
                break;
            case "Metal":
                common = metalCommonAtlas;
                bosses = metalBossesAtlas;
                break;
            case "Water":
                common = waterCommonAtlas;
                bosses = waterBossesAtlas;
                break;
            case "Wood":
                common = woodCommonAtlas;
                bosses = woodBossesAtlas;
                break;
            default:
                common = null;
                bosses = null;
                break;
        }

        return common != null && bosses != null && shader != null;
    }

    public void LoadHomeScene()
    {
        StartSceneTransition(homeSceneName);
    }

    public void LoadMainScene(int level = -1)
    {
        if (level > 0)
        {
            SelectLevel(level);
        }
        StartSceneTransition(mainSceneName);
    }

    public void RestartCurrentScene()
    {
        StartSceneTransition(SceneManager.GetActiveScene().name);
    }

    public Coroutine StartSceneTransition(string sceneName)
    {
        if (_isTransitioningScene || string.IsNullOrEmpty(sceneName)) return null;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"[GameDataManager] Scene '{sceneName}' chưa nằm trong Build Settings.");
            return null;
        }

        // Luôn chạy coroutine trên chính GameDataManager (DontDestroyOnLoad) để không bị hủy giữa chừng khi scene nguồn unload.
        return StartCoroutine(LoadSceneAsyncInternal(sceneName));
    }

    private IEnumerator LoadSceneAsyncInternal(string sceneName)
    {
        _isTransitioningScene = true;
        Time.timeScale = 1f;

        try
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            if (op != null)
            {
                while (!op.isDone)
                {
                    yield return null;
                }
            }
        }
        finally
        {
            _isTransitioningScene = false;
        }
    }

    private static int GetMatchVfxIndex(ElementType element, bool link)
    {
        return (int)element * 2 + (link ? 1 : 0);
    }
}
