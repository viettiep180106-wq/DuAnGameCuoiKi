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

    // Mapping nhanh từ TypeStone -> SkillPassiveDataSO
    private readonly Dictionary<TypeStone, SkillPassiveDataSO> _stoneToSkillMap = new Dictionary<TypeStone, SkillPassiveDataSO>();

    public event Action<TypeStone, SkillPassiveDataSO> OnPassiveSkillEquipped;

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

    public IReadOnlyList<SkillPassiveDataSO> EquippedPassives => _equippedPassives;
}
