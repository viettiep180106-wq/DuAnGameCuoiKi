using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EnemyController : MonoBehaviour
{
    private static EnemyController _instance;
    public static EnemyController Instance => _instance;
    public static EnemyController instance => _instance; // Backward compatibility

    [SerializeField] public Transform target;
    [SerializeField] private Dictionary<EnemyType, EnemyBase> listEnemy = new Dictionary<EnemyType, EnemyBase>();
    public int numEnemy = 0;
    public int totalEnemiesInWave = 0;
    public float InitialWaveTotalStat { get; private set; } = 0f;

    private EnemyBase defaultEnemyPrefab;
    private readonly List<EnemyBase> _livingEnemies = new List<EnemyBase>();
    private readonly List<EnemyBase> _deadEnemiesInWave = new List<EnemyBase>();

    public IReadOnlyList<EnemyBase> LivingEnemies => _livingEnemies;
    public IReadOnlyList<EnemyBase> DeadEnemiesInWave => _deadEnemiesInWave;

    public void RecordDeadEnemy(EnemyBase enemy)
    {
        if (enemy != null && !_deadEnemiesInWave.Contains(enemy))
        {
            _deadEnemiesInWave.Add(enemy);
        }
    }

    public bool TryReviveDeadAlly(ElementType targetElement, float hpPercent)
    {
        for (int i = _deadEnemiesInWave.Count - 1; i >= 0; i--)
        {
            EnemyBase candidate = _deadEnemiesInWave[i];
            if (candidate != null && candidate.Element == targetElement && candidate.CurrentHP <= 0)
            {
                _deadEnemiesInWave.RemoveAt(i);
                float revHp = Mathf.Max(1f, candidate.MaxHealth * hpPercent);
                candidate.ReviveWithHP(revHp);
                RegisterEnemy(candidate);
                numEnemy++;
                Debug.Log($"<color=#00ff88>✦ [REVIVE] {candidate.type} đã được hồi sinh với {hpPercent * 100}% HP!</color>");
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Hồi sinh toàn bộ đồng minh tử trận (Kỹ năng tối thượng của Cổ Thụ Ngàn Năm)
    /// </summary>
    public int ReviveAllDeadAllies(ElementType targetElement, float hpPercent)
    {
        int revivedCount = 0;
        for (int i = _deadEnemiesInWave.Count - 1; i >= 0; i--)
        {
            EnemyBase candidate = _deadEnemiesInWave[i];
            if (candidate != null && candidate.CurrentHP <= 0)
            {
                _deadEnemiesInWave.RemoveAt(i);
                float revHp = Mathf.Max(1f, candidate.MaxHealth * hpPercent);
                candidate.ReviveWithHP(revHp);
                RegisterEnemy(candidate);
                numEnemy++;
                revivedCount++;
            }
        }
        return revivedCount;
    }

    private void Awake()
    {
        _instance = this;
        LoadPrefabFromResource();
    }

    public void LoadPrefabFromResource()
    {
        listEnemy.Clear();
        string path = "Prefab/Base/Enemy";
        EnemyBase[] request = Resources.LoadAll<EnemyBase>(path);
        foreach (EnemyBase enemy in request)
        {
            if (!listEnemy.ContainsKey(enemy.type))
            {
                listEnemy.Add(enemy.type, enemy);
            }
            if (defaultEnemyPrefab == null)
            {
                defaultEnemyPrefab = enemy;
            }
        }
    }

    private EnemyBase GetEnemyPrefab(EnemyType enemyType)
    {
        if (listEnemy.TryGetValue(enemyType, out var prefab)) return prefab;
        return defaultEnemyPrefab;
    }

    public void RegisterEnemy(EnemyBase enemy)
    {
        if (enemy != null && !_livingEnemies.Contains(enemy))
        {
            _livingEnemies.Add(enemy);
        }
    }

    public void UnregisterEnemy(EnemyBase enemy)
    {
        if (enemy != null)
        {
            _livingEnemies.Remove(enemy);
        }
    }

    private int _spawnedFrontCount = 0;
    private int _spawnedBackCount = 0;

    public void SetUpEnemyInStep(List<Enemy> dataList)
    {
        ClearAllEnemies();
        _deadEnemiesInWave.Clear();

        numEnemy = 0;
        totalEnemiesInWave = 0;
        InitialWaveTotalStat = 0f;
        _spawnedFrontCount = 0;
        _spawnedBackCount = 0;

        int currentLevel = GameManager.instance != null ? GameManager.instance.CurrentLevel : 1;
        StepDifficulty diff = StepDifficulty.Normal;
        if (LevelManager.instance != null)
        {
            StepData currentStep = LevelManager.instance.GetStepData(GameManager.instance != null ? GameManager.instance.CurrentStep : 1);
            if (currentStep != null)
            {
                diff = currentStep.GetDifficulty();
            }
        }

        foreach (Enemy e in dataList)
        {
            totalEnemiesInWave += e.GetNum();
            EnemyConfig conf = EnemyStatsDatabase.GetScaledConfig(e.GetName(), currentLevel, diff);
            InitialWaveTotalStat += (conf.HP + conf.DEF) * e.GetNum();
            StartCoroutine(SpawnEnemyInStep(e.GetNum(), e.GetName(), e.GetRow()));
        }
    }

    public IEnumerator SpawnEnemyInStep(int amount, EnemyType enemyType, EnemyRow row, float distance = 0f)
    {
        for (int i = 0; i < amount; i++)
        {
            Spawn(enemyType, row);
            numEnemy++;
            if (i < amount - 1)
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    public void Spawn(EnemyType enemyType, EnemyRow assignedRow = EnemyRow.Front)
    {
        Spawn(enemyType, 0f, assignedRow);
    }

    public void Spawn(EnemyType enemyType, float distance, EnemyRow assignedRow = EnemyRow.Front)
    {
        EnemyBase prefab = GetEnemyPrefab(enemyType);
        if (prefab == null) return;

        GameObject enemyObj = ObjectPoolingManager.Instance.Spawn(prefab.gameObject, Vector3.zero, Quaternion.identity);
        EnemyBase enemy = enemyObj.GetComponent<EnemyBase>();
        enemy.transform.SetParent(transform);

        // Đánh dấu quái Boss nếu thuộc Boss Wave hoặc hệ quái Boss
        bool isCurrentWaveBoss = GameManager.instance != null && GameManager.instance.CurrentStep == GameManager.instance.TotalSteps;
        enemy.isBoss = isCurrentWaveBoss || (enemyType == EnemyType.HatNhamThach);

        // Khởi tạo chỉ số chuẩn theo GDD có áp dụng Scale theo Level và Độ khó
        int currentLevel = GameManager.instance != null ? GameManager.instance.CurrentLevel : 1;
        StepDifficulty diff = StepDifficulty.Normal;
        if (LevelManager.instance != null)
        {
            StepData currentStep = LevelManager.instance.GetStepData(GameManager.instance != null ? GameManager.instance.CurrentStep : 1);
            if (currentStep != null)
            {
                diff = currentStep.GetDifficulty();
            }
        }

        EnemyDataSO soData = EnemyDataSO.Get(enemyType);
        if (soData != null)
        {
            enemy.InitializeFromSO(soData, currentLevel, diff);
        }
        else
        {
            EnemyConfig config = EnemyStatsDatabase.GetScaledConfig(enemyType, currentLevel, diff);
            enemy.InitializeStats(
                enemyType,
                config.Element,
                config.HP,
                config.ATK,
                config.DEF,
                config.Speed,
                config.Reflect,
                config.Scale,
                config.AttackCd,
                config.CombatType,
                config.AttackRange,
                config.Role,
                config.SkillMultiplier,
                config.SkillCdRound,
                config.CritRate,
                config.CritDmg
            );
        }

        RegisterEnemy(enemy);
        enemy.row = assignedRow;

        // Định vị đội hình 2 hàng bên PHẢI: nâng cao tọa độ Y ngang tầm Player, mỗi quái cùng hàng cách nhau đúng 5 units
        float posX;
        float posY;
        float baseY = 2.5f; // Nâng cao vị trí đứng của quái, tách biệt hoàn toàn khỏi player stat bar
        if (assignedRow == EnemyRow.Front)
        {
            // Hàng trước (2 quái): Khoảng cách Y giữa 2 quái đúng bằng 5 units (+2.5f và -2.5f)
            bool frontSlot = _spawnedFrontCount < 2;
            posX = frontSlot? 2.5f : 7.5f;
            posY = frontSlot? ((baseY + 2.5f)*_spawnedFrontCount) : (- 2.5f);
            _spawnedFrontCount++;
        }
        else
        {
            // Hàng sau (3 quái): Thế cánh cung, khoảng cách Y giữa mỗi quái liền kề đúng bằng 5 units (+5f, 0f, -5f)
            int backSlot = _spawnedBackCount % 3;
            if (backSlot == 0)
            {
                posX = 7.5f;
                posY = baseY + 5.0f;
            }
            else if (backSlot == 1)
            {
                posX = 7.5f;
                posY = baseY;
            }
            else
            {
                posX = 7.5f;
                posY = baseY - 5.0f;
            }
            _spawnedBackCount++;
        }

        enemy.transform.localPosition = new Vector3(posX, posY, 0f);

        // Quay mặt sang TRÁI (hướng về phía Player) và phân lớp Layer 2.5D theo trục Y
        if (enemy.TryGetComponent<SpriteRenderer>(out var sr))
        {
            sr.flipX = true;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 15 ;
        }

        // Quái đứng cố định trên mặt đất trong suốt trận đấu, không di chuyển/drift vào Player
    }

    public void StartActive()
    {
        // Quái đứng cố định tại vị trí đội hình sẵn sàng chiến đấu
    }

    /// <summary>
    /// Đẩy lùi toàn bộ quái còn sống ra xa khi kết thúc quãng đường của wave để gộp vào wave sau (Zero-GC)
    /// </summary>
    public void PushBackLivingEnemies(float pushDistance = 5f)
    {
        for (int i = 0; i < _livingEnemies.Count; i++)
        {
            EnemyBase enemy = _livingEnemies[i];
            if (enemy != null && enemy.gameObject.activeInHierarchy && enemy.CurrentHP > 0)
            {
                enemy.StopAllCoroutines();
                Vector3 pushedPos = enemy.transform.position + new Vector3(-pushDistance, 0, 0);
                enemy.Trans(pushedPos, DG.Tweening.Ease.OutQuad);
            }
        }
    }

    /// <summary>
    /// Đẩy quái wave cũ ra ngoài rìa màn hình với tốc độ vừa phải khoảng 3s, sau đó bỏ vào pool (không tồn tại sang wave sau)
    /// </summary>
    public void PushOutAndClearOldEnemies(float pushDistance = 10f, float duration = 3.0f, System.Action onComplete = null)
    {
        _livingEnemies.RemoveAll(e => e == null || !e.gameObject.activeInHierarchy);

        if (_livingEnemies.Count == 0)
        {
            ClearAllEnemies();
            onComplete?.Invoke();
            return;
        }

        int pendingCount = _livingEnemies.Count;
        for (int i = 0; i < _livingEnemies.Count; i++)
        {
            EnemyBase enemy = _livingEnemies[i];
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                enemy.StopAllCoroutines();
                Vector3 pushedPos = enemy.transform.position + new Vector3(-pushDistance, 0, 0);
                enemy.transform.DOMove(pushedPos, duration)
                    .SetEase(DG.Tweening.Ease.InOutQuad)
                    .OnComplete(() =>
                    {
                        pendingCount--;
                        if (pendingCount <= 0)
                        {
                            ClearAllEnemies();
                            onComplete?.Invoke();
                        }
                    });
            }
            else
            {
                pendingCount--;
                if (pendingCount <= 0)
                {
                    ClearAllEnemies();
                    onComplete?.Invoke();
                }
            }
        }
    }

    public void ClearAllEnemies()
    {
        for (int i = _livingEnemies.Count - 1; i >= 0; i--)
        {
            EnemyBase enemy = _livingEnemies[i];
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                ObjectPoolingManager.Instance.Despawn(enemy.gameObject);
            }
        }
        _livingEnemies.Clear();
        _deadEnemiesInWave.Clear();
        numEnemy = 0;
    }

    /// <summary>
    /// Lấy tọa độ X của con quái dẫn đầu bầy quái (hoặc quái gần người chơi nhất) để camera lia preview chính xác
    /// </summary>
    public float GetLeadEnemyX(float fallbackDistance)
    {
        float playerX = target != null ? target.position.x : (Player.Instance != null ? Player.Instance.transform.position.x : 2.0f);
        
        float maxX = float.MinValue;
        bool found = false;

        for (int i = 0; i < _livingEnemies.Count; i++)
        {
            var e = _livingEnemies[i];
            if (e != null && e.gameObject.activeInHierarchy && e.CurrentHP > 0)
            {
                if (e.transform.position.x > maxX)
                {
                    maxX = e.transform.position.x;
                    found = true;
                }
            }
        }

        if (found)
        {
            return maxX;
        }

        return playerX - fallbackDistance;
    }

    /// <summary>
    /// Kiểm tra xem có bất kỳ con quái nào còn sống nằm trong tầm đánh (range) 2D của người chơi hay không
    /// </summary>
    public bool HasEnemyInAttackRange(Vector3 playerCenter, float range)
    {
        Vector2 p2 = new Vector2(playerCenter.x, playerCenter.y);
        float sqrRange = range * range;

        // 1. Kiểm tra qua danh sách quái sống (2D Distance)
        for (int i = 0; i < _livingEnemies.Count; i++)
        {
            var e = _livingEnemies[i];
            if (e != null && e.gameObject.activeInHierarchy && e.CurrentHP > 0)
            {
                Vector2 e2 = new Vector2(e.transform.position.x, e.transform.position.y);
                float sqrDist = (e2 - p2).sqrMagnitude;
                if (sqrDist <= sqrRange)
                {
                    return true;
                }
            }
        }

        // 2. Fallback kiểm tra vật lý 2D (bắt collider quái trong tầm đánh)
        int enemyLayerMask = (ActiveSkillPassive.instance != null && ActiveSkillPassive.instance.enemyLayer.value != 0) 
            ? ActiveSkillPassive.instance.enemyLayer.value 
            : (1 << 3); // Mặc định layer 3: enemy
        Collider2D hit = Physics2D.OverlapCircle(p2, range, enemyLayerMask);
        if (hit != null)
        {
            EnemyBase eb = hit.GetComponent<EnemyBase>();
            if (eb != null && eb.CurrentHP > 0) return true;
        }

        return false;
    }

    /// <summary>
    /// Tìm con quái còn sống gần nhất trong phạm vi tối đa (2D Distance)
    /// </summary>
    public EnemyBase GetClosestLivingEnemy(Vector3 center, float maxRange = float.MaxValue)
    {
        EnemyBase closest = null;
        Vector2 c2 = new Vector2(center.x, center.y);
        float minSqr = maxRange * maxRange;

        for (int i = 0; i < _livingEnemies.Count; i++)
        {
            var e = _livingEnemies[i];
            if (e != null && e.gameObject.activeInHierarchy && e.CurrentHP > 0)
            {
                Vector2 e2 = new Vector2(e.transform.position.x, e.transform.position.y);
                float sqrDist = (e2 - c2).sqrMagnitude;
                if (sqrDist <= minSqr)
                {
                    minSqr = sqrDist;
                    closest = e;
                }
            }
        }

        return closest;
    }

    /// <summary>
    /// Kiểm tra xem toàn bộ quái ở Hàng trước đã tử trận hết hay chưa (Rào cản Hàng trước theo GDD)
    /// </summary>
    public bool AllFrontRowEnemiesDead()
    {
        for (int i = 0; i < _livingEnemies.Count; i++)
        {
            var e = _livingEnemies[i];
            if (e != null && e.gameObject.activeInHierarchy && e.CurrentHP > 0 && e.row == EnemyRow.Front)
            {
                return false; // Còn ít nhất 1 quái hàng trước còn sống
            }
        }
        return true;
    }

    /// <summary>
    /// Đếm tổng số lượng quái còn sống trên sân
    /// </summary>
    public int GetLivingEnemyCount()
    {
        int count = 0;
        for (int i = 0; i < _livingEnemies.Count; i++)
        {
            var e = _livingEnemies[i];
            if (e != null && e.gameObject.activeInHierarchy && e.CurrentHP > 0)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Lấy danh sách quái còn sống được sắp xếp theo đúng thứ tự hành động trong Turn-based 5v1:
    /// - Hàng trước hành động trước -> Hàng sau hành động sau
    /// - Trong cùng hàng: Ưu tiên tốc độ hành động cao nhất
    /// </summary>
    public List<EnemyBase> GetOrderedEnemiesForTurn()
    {
        List<EnemyBase> frontRow = new List<EnemyBase>();
        List<EnemyBase> backRow = new List<EnemyBase>();

        for (int i = 0; i < _livingEnemies.Count; i++)
        {
            var e = _livingEnemies[i];
            if (e != null && e.gameObject.activeInHierarchy && e.CurrentHP > 0)
            {
                if (e.row == EnemyRow.Front) frontRow.Add(e);
                else backRow.Add(e);
            }
        }

        // Sắp xếp theo Tốc độ hành động (Speed giảm dần)
        frontRow.Sort((a, b) => {
            float speedA = a.data != null ? a.data.Speed_Trans : 0.25f;
            float speedB = b.data != null ? b.data.Speed_Trans : 0.25f;
            return speedB.CompareTo(speedA);
        });

        backRow.Sort((a, b) => {
            float speedA = a.data != null ? a.data.Speed_Trans : 0.25f;
            float speedB = b.data != null ? b.data.Speed_Trans : 0.25f;
            return speedB.CompareTo(speedA);
        });

        List<EnemyBase> ordered = new List<EnemyBase>(frontRow.Count + backRow.Count);
        ordered.AddRange(frontRow);
        ordered.AddRange(backRow);
        return ordered;
    }

    /// <summary>
    /// Giảm 1 Round hồi chiêu cho toàn bộ kỹ năng của quái vật sau khi kết thúc 1 Round
    /// </summary>
    public void TickRoundCooldowns()
    {
        for (int i = 0; i < _livingEnemies.Count; i++)
        {
            var e = _livingEnemies[i];
            if (e != null && e.gameObject.activeInHierarchy && e.CurrentHP > 0)
            {
                e.skillCooldownRound = Mathf.Max(0, e.skillCooldownRound - 1);
                e.TickRoundBuffsAndShields();
            }
        }
    }

    /// <summary>
    /// Lấy quái mục tiêu hợp lệ tuân thủ rào cản Hàng trước:
    /// - Bắt buộc phải hạ hết Hàng trước mới đánh được Hàng sau
    /// - Trừ khi kỹ năng có flag allowBackRow (AoE hoặc xuyên hàng)
    /// </summary>
    public EnemyBase GetValidTarget(bool allowBackRow = false)
    {
        bool frontDead = AllFrontRowEnemiesDead();

        // 1. Nếu hàng trước còn sống và không được phép đánh xuyên hàng: Chỉ nhắm hàng trước
        if (!frontDead && !allowBackRow)
        {
            for (int i = 0; i < _livingEnemies.Count; i++)
            {
                var e = _livingEnemies[i];
                if (e != null && e.gameObject.activeInHierarchy && e.CurrentHP > 0 && e.row == EnemyRow.Front)
                {
                    return e;
                }
            }
        }

        // 2. Nếu hàng trước đã chết hoặc kỹ năng cho phép xuyên hàng: Lấy quái còn sống bất kỳ
        for (int i = 0; i < _livingEnemies.Count; i++)
        {
            var e = _livingEnemies[i];
            if (e != null && e.gameObject.activeInHierarchy && e.CurrentHP > 0)
            {
                return e;
            }
        }

        return null;
    }
}
