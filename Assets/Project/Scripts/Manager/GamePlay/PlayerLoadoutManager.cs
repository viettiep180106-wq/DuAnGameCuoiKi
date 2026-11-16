using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLoadoutManager : MonoBehaviour
{
    private static PlayerLoadoutManager _instance;
    public static PlayerLoadoutManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = UnityEngine.Object.FindFirstObjectByType<PlayerLoadoutManager>(FindObjectsInactive.Include);
                if (_instance == null)
                {
                    GameObject go = new GameObject("PlayerLoadoutManager", typeof(PlayerLoadoutManager));
                    _instance = go.GetComponent<PlayerLoadoutManager>();
                    _instance.InitializeLoadout();
                }
            }
            return _instance;
        }
    }

    [Header("Default Equipped Passives")]
    [SerializeField] private List<SkillPassiveDataSO> _equippedPassives = new List<SkillPassiveDataSO>();

    [Header("Active Skills Status")]
    [SerializeField] private List<ActiveSkillDataSO> _allActiveSkills = new List<ActiveSkillDataSO>();
    [SerializeField] private List<ActiveSkillType> _unlockedActiveSkills = new List<ActiveSkillType>();

    // Mapping nhanh từ TypeStone -> SkillPassiveDataSO
    private readonly Dictionary<TypeStone, SkillPassiveDataSO> _stoneToSkillMap = new Dictionary<TypeStone, SkillPassiveDataSO>();

    public event Action<TypeStone, SkillPassiveDataSO> OnPassiveSkillEquipped;
    public event Action<ActiveSkillDataSO> OnActiveSkillUnlocked;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        InitializeLoadout();
    }

    /// <summary>
    /// Khởi tạo loadout ban đầu nếu chưa có dữ liệu
    /// </summary>
    public void InitializeLoadout()
    {
        _stoneToSkillMap.Clear();

        // 1. Ánh xạ các kỹ năng Passive đang trang bị
        foreach (SkillPassiveDataSO skill in _equippedPassives)
        {
            if (skill != null)
            {
                _stoneToSkillMap[skill.StoneType] = skill;
            }
        }

        // 2. Nếu thiếu kỹ năng cho các hệ đá cơ bản, tự động nạp fallback mặc định kèm Icon
        EnsureDefaultPassiveSkills();

        // 3. Khởi tạo danh sách Active Skills mặc định (nếu rỗng)
        EnsureDefaultActiveSkills();
    }

    private void EnsureDefaultPassiveSkills()
    {
        TypeStone[] requiredStones = { TypeStone.Fire, TypeStone.Ice, TypeStone.Wood, TypeStone.Earth, TypeStone.Wind };
        foreach (TypeStone stone in requiredStones)
        {
            if (!_stoneToSkillMap.ContainsKey(stone))
            {
                SkillPassiveDataSO defaultSkill = CreateDefaultPassiveData(stone);
                if (defaultSkill != null)
                {
                    _stoneToSkillMap[stone] = defaultSkill;
                    _equippedPassives.Add(defaultSkill);
                }
            }
        }
    }

    private SkillPassiveDataSO CreateDefaultPassiveData(TypeStone stone)
    {
        string prefabName = stone switch
        {
            TypeStone.Fire => "fire",
            TypeStone.Ice => "Ice",
            TypeStone.Water => "Ice",
            TypeStone.Wood => "wood",
            TypeStone.Earth => "earth",
            TypeStone.Wind => "wind",
            TypeStone.Light => "wind",
            _ => "fire"
        };

        GameObject prefab = Resources.Load<GameObject>($"Prefab/Base/Skill/Passive/{prefabName}");

        string spriteName = stone switch
        {
            TypeStone.Fire => "Fire",
            TypeStone.Ice or TypeStone.Water => "Ice",
            TypeStone.Wood => "Wood",
            TypeStone.Earth => "Earth",
            _ => "Wind"
        };

        Sprite icon = null;
#if UNITY_EDITOR
        icon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Project/Sprite/{spriteName}.png");
#endif

        SkillPassiveDataSO so = ScriptableObject.CreateInstance<SkillPassiveDataSO>();
        ElementType elem = stone switch
        {
            TypeStone.Fire => ElementType.Fire,
            TypeStone.Ice or TypeStone.Water => ElementType.Water,
            TypeStone.Wood => ElementType.Wood,
            TypeStone.Earth => ElementType.Earth,
            _ => ElementType.Metal
        };

        string skillDisplayName = stone switch
        {
            TypeStone.Fire => "Bom Cảm Tử",
            TypeStone.Ice or TypeStone.Water => "Mưa Băng",
            TypeStone.Wood => "Mộc Giáp",
            TypeStone.Earth => "Địa Tầng",
            _ => "Khiên Kim Cang"
        };

        so.Initialize($"skill_default_{stone}", skillDisplayName, stone, elem, prefab, icon, 25f);
        return so;
    }

    private void EnsureDefaultActiveSkills()
    {
        if (_allActiveSkills.Count == 0)
        {
            // Nạp 4 kỹ năng Active chuẩn theo GDD
            _allActiveSkills.Add(CreateActiveSkillData("skill_meteor", "Thiên Thạch Ma Pháp", ActiveSkillType.MeteorStrike, ElementType.Fire, 12f, 2.5f, 0.6f));
            _allActiveSkills.Add(CreateActiveSkillData("skill_freeze", "Băng Hà Phong Ấn", ActiveSkillType.AbsoluteFreeze, ElementType.Water, 18f, 2.0f, 0.5f));
            _allActiveSkills.Add(CreateActiveSkillData("skill_shield", "Khiên Thánh Vực", ActiveSkillType.SacredShield, ElementType.Wood, 15f, 1.5f, 0.45f));
            _allActiveSkills.Add(CreateActiveSkillData("skill_blade", "Kiếm Khí Bão Táp", ActiveSkillType.BladeStorm, ElementType.Metal, 10f, 2.8f, 0.5f));
        }

        // Ban đầu người chơi bắt đầu với 1 kỹ năng Active cơ bản (BladeStorm)
        if (_unlockedActiveSkills.Count == 0)
        {
            _unlockedActiveSkills.Add(ActiveSkillType.BladeStorm);
        }
    }

    private ActiveSkillDataSO CreateActiveSkillData(string id, string name, ActiveSkillType type, ElementType elem, float cd, float dmgMul, float dropRate)
    {
        ActiveSkillDataSO so = ScriptableObject.CreateInstance<ActiveSkillDataSO>();
        Sprite icon = null;
#if UNITY_EDITOR
        string spriteName = elem switch
        {
            ElementType.Fire => "Fire",
            ElementType.Water => "Ice",
            ElementType.Wood => "Wood",
            ElementType.Earth => "Earth",
            _ => "Ligth"
        };
        icon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Project/Sprite/{spriteName}.png");
#endif
        so.Initialize(id, name, type, elem, cd, dmgMul, dropRate);
        return so;
    }

    /// <summary>
    /// Lấy dữ liệu kỹ năng Passive đang được trang bị cho loại đá
    /// </summary>
    public SkillPassiveDataSO GetEquippedPassive(TypeStone stoneType)
    {
        if (_stoneToSkillMap.TryGetValue(stoneType, out SkillPassiveDataSO skill))
        {
            return skill;
        }

        // Tương thích các hệ tương đồng
        if (stoneType == TypeStone.Water && _stoneToSkillMap.TryGetValue(TypeStone.Ice, out var ice)) return ice;
        if (stoneType == TypeStone.Light && _stoneToSkillMap.TryGetValue(TypeStone.Wind, out var wind)) return wind;

        // Fallback đầu tiên tìm thấy
        foreach (var s in _stoneToSkillMap.Values) return s;
        return null;
    }

    /// <summary>
    /// Trang bị kỹ năng Passive mới cho hệ đá tương ứng (Được gọi từ Home/Loadout)
    /// </summary>
    public void EquipPassiveSkill(SkillPassiveDataSO newSkill)
    {
        if (newSkill == null) return;

        _stoneToSkillMap[newSkill.StoneType] = newSkill;

        int existingIdx = _equippedPassives.FindIndex(s => s != null && s.StoneType == newSkill.StoneType);
        if (existingIdx >= 0)
        {
            _equippedPassives[existingIdx] = newSkill;
        }
        else
        {
            _equippedPassives.Add(newSkill);
        }

        OnPassiveSkillEquipped?.Invoke(newSkill.StoneType, newSkill);
    }

    /// <summary>
    /// Kiểm tra xem một kỹ năng Active đã được mở khóa (hoặc nhặt từ Boss) chưa
    /// </summary>
    public bool IsActiveSkillUnlocked(ActiveSkillType type)
    {
        return _unlockedActiveSkills.Contains(type);
    }

    /// <summary>
    /// Mở khóa kỹ năng Active mới khi nhặt được từ Boss Wave
    /// </summary>
    public bool UnlockActiveSkill(ActiveSkillType type)
    {
        if (!_unlockedActiveSkills.Contains(type))
        {
            _unlockedActiveSkills.Add(type);

            ActiveSkillDataSO data = _allActiveSkills.Find(s => s != null && s.SkillType == type);
            OnActiveSkillUnlocked?.Invoke(data);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Thử roll rơi kỹ năng Active khi tiêu diệt Boss Wave theo tỷ lệ GDD
    /// </summary>
    public ActiveSkillDataSO TryDropActiveSkillFromBoss(ElementType bossElement)
    {
        List<ActiveSkillDataSO> lockedSkills = _allActiveSkills.FindAll(s => s != null && !_unlockedActiveSkills.Contains(s.SkillType));
        if (lockedSkills.Count == 0) return null;

        ActiveSkillDataSO candidate = lockedSkills.Find(s => s.Element == bossElement) ?? lockedSkills[UnityEngine.Random.Range(0, lockedSkills.Count)];

        float roll = UnityEngine.Random.value;
        if (roll <= candidate.DropRateFromBoss)
        {
            UnlockActiveSkill(candidate.SkillType);
            return candidate;
        }

        return null;
    }

    public IReadOnlyList<SkillPassiveDataSO> EquippedPassives => _equippedPassives;
    public IReadOnlyList<ActiveSkillType> UnlockedActiveSkills => _unlockedActiveSkills;
}
