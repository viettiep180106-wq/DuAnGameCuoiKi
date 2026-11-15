using System;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyCategory
{
    Normal,     // Quái Thường
    WaveBoss,   // Boss Wave
    FinalBoss   // Final Boss (Trùm Cuối)
}

[CreateAssetMenu(fileName = "EnemyData_", menuName = "ScriptableObjects/Character/EnemyDataSO")]
public class EnemyDataSO : ScriptableObject
{
    [Header("----Enemy Identity (GDD Specs)----")]
    public EnemyType enemyType;
    public string enemyName;
    public ElementType element = ElementType.Fire;
    public EnemyCategory category = EnemyCategory.Normal;
    public EnemyRole role = EnemyRole.DPS_FBS;
    public EnemyRow defaultRow = EnemyRow.Front;

    [Header("----Base Stats (Level 1)----")]
    public float maxHP = 160f;
    public float atk = 50f;
    public float def = 50f;
    [Range(0f, 1f)] public float critRate = 0.10f;
    public float critDmg = 1.50f;

    [Header("----Combat & Movement----")]
    public float speed = 0.40f;
    public float attackCd = 2f;
    public float reflect = 0f;
    public float scale = 0.85f;
    public bool isBoss = false;
    public EnemyCombatType combatType = EnemyCombatType.Melee;
    public float attackRange = 0.85f;

    [Header("----Active Skill (Cooldown Round)----")]
    public string activeSkillName;
    public float skillMultiplier = 1.50f;
    public int skillCdRound = 2;
    [TextArea(2, 4)] public string activeSkillDesc;

    [Header("----Passive Skill & Normal Attack----")]
    [TextArea(2, 4)] public string passiveSkillDesc;
    [TextArea(2, 4)] public string normalAttackDesc;

    [Header("----Prefab Reference (Optional)----")]
    public EnemyBase enemyPrefab;

    public EnemyConfig ToEnemyConfig()
    {
        return new EnemyConfig
        {
            Name = enemyName,
            Element = element,
            Role = role,
            DefaultRow = defaultRow,
            HP = maxHP,
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
            IsBoss = isBoss || (category != EnemyCategory.Normal),
            CombatType = combatType,
            AttackRange = attackRange
        };
    }

    // Static dictionary for caching runtime assets
    private static readonly Dictionary<EnemyType, EnemyDataSO> _loadedAssets = new();
    private static bool _initialized = false;

    public static EnemyDataSO Get(EnemyType type)
    {
        if (!_initialized || _loadedAssets.Count == 0)
        {
            LoadAllFromResources();
        }

        if (_loadedAssets.TryGetValue(type, out var so) && so != null)
        {
            return so;
        }

        return null;
    }

    public static void LoadAllFromResources()
    {
        _loadedAssets.Clear();
        EnemyDataSO[] all = Resources.LoadAll<EnemyDataSO>("ScriptObject/Character/Enemies");
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && !_loadedAssets.ContainsKey(all[i].enemyType))
            {
                _loadedAssets[all[i].enemyType] = all[i];
            }
        }
        _initialized = true;
    }

    public static IReadOnlyDictionary<EnemyType, EnemyDataSO> AllLoaded => _loadedAssets;
}
