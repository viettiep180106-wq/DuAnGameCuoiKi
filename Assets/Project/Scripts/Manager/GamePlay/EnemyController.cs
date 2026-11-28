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
        int targetIndex = -1;
        // Ưu tiên 1: Quái cùng hệ tử trận
        for (int i = _deadEnemiesInWave.Count - 1; i >= 0; i--)
        {
            EnemyBase candidate = _deadEnemiesInWave[i];
            if (candidate != null && candidate.Element == targetElement && candidate.CurrentHP <= 0)
            {
                targetIndex = i;
                break;
            }
        }

        // Ưu tiên 2: Nếu không có quái cùng hệ, hồi sinh đồng minh bất kỳ vừa tử trận
        if (targetIndex < 0)
        {
            for (int i = _deadEnemiesInWave.Count - 1; i >= 0; i--)
            {
                EnemyBase candidate = _deadEnemiesInWave[i];
                if (candidate != null && candidate.CurrentHP <= 0)
                {
                    targetIndex = i;
                    break;
                }
            }
        }

        if (targetIndex >= 0)
        {
            EnemyBase candidate = _deadEnemiesInWave[targetIndex];
            _deadEnemiesInWave.RemoveAt(targetIndex);
            float revHp = Mathf.Max(1f, candidate.MaxHealth * hpPercent);
            candidate.ReviveWithHP(revHp);
            RegisterEnemy(candidate);
            numEnemy++;
            Debug.Log($"<color=#00ff88>✦ [REVIVE] {candidate.type} đã được hồi sinh với {hpPercent * 100}% HP tại vị trí {candidate.FormationLocalPosition}!</color>");
            return true;
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
        var mgr = GameDataManager.Instance;
        if (mgr != null && mgr.TryCopyEnemyPrefabsTo(listEnemy, ref defaultEnemyPrefab))
        {
            return;
        }

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
        EnemyDataSO so = EnemyDataSO.Get(enemyType);
        if (so != null && so.enemyPrefab != null)
        {
            return so.enemyPrefab;
        }
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

        GameObject enemyObj = ObjectPoolingManager.Instance.Spawn(prefab.gameObject, Vector3.zero, Quaternion.identity, transform);
        EnemyBase enemy = enemyObj.GetComponent<EnemyBase>();
        if (enemy.DefaultScale != Vector3.zero)
        {
            enemy.transform.localScale = enemy.DefaultScale;
        }

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

        // Định vị đội hình 2 hàng bên PHẢI:
        // - Trục tâm đội hình nâng lên baseY = 5.0f (tăng 2.5 đơn vị, đối xứng chuẩn với Player)
        // - Khoảng cách delta Y = 6.0f (tăng từ 5.0 theo phản hồi)
        float baseY = 2.5f;
        float deltaY = 6.5f;

        // 2 Slot Hàng trước (Front Row):
        Vector3 slotF0 = new Vector3(2f, baseY - deltaY * 0.5f, 0f); // (2f, 2.0f, 0f)
        Vector3 slotF1 = new Vector3(2f, baseY + deltaY * 0.5f, 0f); // (2f, 8.0f, 0f)

        // 3 Slot Hàng sau (Back Row):
        Vector3 slotB1 = new Vector3(10f, baseY, 0f);                 // (8f, 5.0f, 0f) - Ưu tiên tâm giữa
        Vector3 slotB0 = new Vector3(10f, baseY + deltaY, 0f);        // (8f, 11.0f, 0f) - Trên
        Vector3 slotB2 = new Vector3(10f, baseY - deltaY, 0f);        // (8f, -1.0f, 0f) - Dưới

        // Kiểm tra xem các slot đã bị chiếm giữ bởi quái đang sống chưa
        bool isF0Occupied = IsSlotOccupied(slotF0, enemy);
        bool isF1Occupied = IsSlotOccupied(slotF1, enemy);
        bool isB1Occupied = IsSlotOccupied(slotB1, enemy);
        bool isB0Occupied = IsSlotOccupied(slotB0, enemy);
        bool isB2Occupied = IsSlotOccupied(slotB2, enemy);

        Vector3 chosenPos;
        EnemyRow actualRow = assignedRow;

        if (assignedRow == EnemyRow.Front)
        {
            // Ưu tiên điền hàng trước
            if (!isF0Occupied)
            {
                chosenPos = slotF0;
            }
            else if (!isF1Occupied)
            {
                chosenPos = slotF1;
            }
            // Nếu hàng trước đã đầy (vượt quá 2 quái) -> Tự động tràn sang hàng sau
            else if (!isB1Occupied)
            {
                chosenPos = slotB1;
                actualRow = EnemyRow.Back;
            }
            else if (!isB0Occupied)
            {
                chosenPos = slotB0;
                actualRow = EnemyRow.Back;
            }
            else if (!isB2Occupied)
            {
                chosenPos = slotB2;
                actualRow = EnemyRow.Back;
            }
            else
            {
                chosenPos = new Vector3(10.0f, baseY, 0f);
                actualRow = EnemyRow.Back;
            }
            _spawnedFrontCount++;
        }
        else
        {
            // Ưu tiên điền hàng sau: Ưu tiên slot tâm B1 -> B0 -> B2
            if (!isB1Occupied)
            {
                chosenPos = slotB1;
            }
            else if (!isB0Occupied)
            {
                chosenPos = slotB0;
            }
            else if (!isB2Occupied)
            {
                chosenPos = slotB2;
            }
            // Nếu hàng sau đã đầy (vượt quá 3 quái) -> Tự động tràn về hàng trước
            else if (!isF0Occupied)
            {
                chosenPos = slotF0;
                actualRow = EnemyRow.Front;
            }
            else if (!isF1Occupied)
            {
                chosenPos = slotF1;
                actualRow = EnemyRow.Front;
            }
            else
            {
                chosenPos = new Vector3(10.0f, baseY, 0f);
            }
            _spawnedBackCount++;
        }

        enemy.row = actualRow;
        enemy.transform.localPosition = chosenPos;
        enemy.FormationLocalPosition = chosenPos;

        // Quay mặt sang TRÁI (hướng về phía Player) và phân lớp Layer 2.5D theo trục Y
        if (enemy.TryGetComponent<SpriteRenderer>(out var sr))
        {
            sr.flipX = true;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 15 ;
        }

        // Quái đứng cố định trên mặt đất trong suốt trận đấu, không di chuyển/drift vào Player
    }

    /// <summary>
    /// Kiểm tra xem một slot tọa độ trong đội hình đã có quái đang sống chiếm giữ hay chưa
    /// </summary>
    private bool IsSlotOccupied(Vector3 slot, EnemyBase currentEnemy)
    {
        for (int i = 0; i < _livingEnemies.Count; i++)
        {
            var le = _livingEnemies[i];
            if (le != null && le != currentEnemy && le.gameObject.activeInHierarchy && le.CurrentHP > 0)
            {
                Vector3 checkPos = le.FormationLocalPosition != Vector3.zero ? le.FormationLocalPosition : le.transform.localPosition;
                if (Vector3.Distance(checkPos, slot) < 1.0f)
                {
                    return true;
                }
            }
        }
        return false;
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
        frontRow.Sort((a, b) => b.MoveSpeed.CompareTo(a.MoveSpeed));
        backRow.Sort((a, b) => b.MoveSpeed.CompareTo(a.MoveSpeed));

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
