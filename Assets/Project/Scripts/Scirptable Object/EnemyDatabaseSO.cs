using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemyConfigEntry
{
    public EnemyType type;
    public string name;
    public ElementType element;
    public EnemyRole role;
    public EnemyRow defaultRow;
    public float hp = 160f;
    public float atk = 50f;
    public float def = 50f;
    public float critRate = 0.10f;
    public float critDmg = 1.50f;
    public string activeSkillName;
    public float skillMultiplier = 1.50f;
    public int skillCdRound = 2;
    [TextArea(2, 3)] public string activeSkillDesc;
    [TextArea(2, 3)] public string passiveSkillDesc;
    public float speed = 0.40f;
    public float attackCd = 2f;
    public float reflect = 0f;
    public float scale = 0.85f;
    public bool isBoss = false;
    public EnemyCombatType combatType = EnemyCombatType.Melee;
    public float attackRange = 0.85f;

    public EnemyConfig ToEnemyConfig()
    {
        return new EnemyConfig
        {
            Name = name,
            Element = element,
            Role = role,
            DefaultRow = defaultRow,
            HP = hp,
            ATK = atk,
            DEF = def,
            CritRate = critRate,
            CritDmg = critDmg,
            ActiveSkillName = activeSkillName,
            SkillMultiplier = skillMultiplier,
            SkillCdRound = skillCdRound,
            ActiveSkillDesc = activeSkillDesc,
            PassiveSkillDesc = passiveSkillDesc,
            Speed = speed,
            AttackCd = attackCd,
            Reflect = reflect,
            Scale = scale,
            IsBoss = isBoss,
            CombatType = combatType,
            AttackRange = attackRange
        };
    }

    public static EnemyConfigEntry FromEnemyConfig(EnemyType t, EnemyConfig c)
    {
        return new EnemyConfigEntry
        {
            type = t,
            name = c.Name,
            element = c.Element,
            role = c.Role,
            defaultRow = c.DefaultRow,
            hp = c.HP,
            atk = c.ATK,
            def = c.DEF,
            critRate = c.CritRate,
            critDmg = c.CritDmg,
            activeSkillName = c.ActiveSkillName,
            skillMultiplier = c.SkillMultiplier,
            skillCdRound = c.SkillCdRound,
            activeSkillDesc = c.ActiveSkillDesc,
            passiveSkillDesc = c.PassiveSkillDesc,
            speed = c.Speed,
            attackCd = c.AttackCd,
            reflect = c.Reflect,
            scale = c.Scale,
            isBoss = c.IsBoss,
            combatType = c.CombatType,
            attackRange = c.AttackRange
        };
    }
}

[CreateAssetMenu(fileName = "EnemyDatabase", menuName = "ScriptableObjects/Database/EnemyDatabase")]
public class EnemyDatabaseSO : ScriptableObject
{
    private static EnemyDatabaseSO _instance;
    public static EnemyDatabaseSO Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<EnemyDatabaseSO>("Database/EnemyDatabase");
#if UNITY_EDITOR
                if (_instance == null)
                {
                    _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyDatabaseSO>("Assets/Project/Resources/Database/EnemyDatabase.asset");
                }
#endif
            }
            return _instance;
        }
    }

    [Header("Monster Database (Designer Configurable)")]
    [SerializeField] private List<EnemyConfigEntry> enemies = new List<EnemyConfigEntry>();

    private Dictionary<EnemyType, EnemyConfigEntry> _lookupCache;

    public List<EnemyConfigEntry> Enemies => enemies;

    public void SetEnemiesForBuild(List<EnemyConfigEntry> list)
    {
        enemies = list;
        _lookupCache = null;
    }

    private void BuildLookupIfNeeded()
    {
        if (_lookupCache == null)
        {
            _lookupCache = new Dictionary<EnemyType, EnemyConfigEntry>();
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null && !_lookupCache.ContainsKey(enemies[i].type))
                {
                    _lookupCache[enemies[i].type] = enemies[i];
                }
            }
        }
    }

    public bool TryGetConfig(EnemyType type, out EnemyConfig config)
    {
        BuildLookupIfNeeded();
        if (_lookupCache != null && _lookupCache.TryGetValue(type, out var entry))
        {
            config = entry.ToEnemyConfig();
            return true;
        }
        config = default;
        return false;
    }

    public List<EnemyType> GetEnemiesByElement(ElementType element)
    {
        var list = new List<EnemyType>();
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null && enemies[i].element == element)
            {
                list.Add(enemies[i].type);
            }
        }
        return list;
    }
}
