using System;
using System.Collections.Generic;
using UnityEngine;

public enum BuffCategory
{
    CommonBuff,
    RareBuff,
    Debuff
}

[Serializable]
public class BuffDebuffInfo
{
    public BuffDebuffType type;
    public string displayName;
    [TextArea(2, 4)] public string description;
    public Sprite icon;
    public BuffCategory category;
}

[CreateAssetMenu(fileName = "BuffDebuffDatabase", menuName = "Database/BuffDebuffDatabase")]
public class BuffDebuffDatabase : ScriptableObject
{
    private static BuffDebuffDatabase _instance;
    public static BuffDebuffDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<BuffDebuffDatabase>("Database/BuffDebuffDatabase");
#if UNITY_EDITOR
                if (_instance == null)
                {
                    _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<BuffDebuffDatabase>("Assets/Project/Resources/Database/BuffDebuffDatabase.asset");
                }
#endif
            }
            return _instance;
        }
    }

    [SerializeField] private List<BuffDebuffInfo> entries = new List<BuffDebuffInfo>();

    private Dictionary<BuffDebuffType, BuffDebuffInfo> _lookupMap;
    private List<BuffDebuffInfo> _commonBuffs;
    private List<BuffDebuffInfo> _rareBuffs;
    private List<BuffDebuffInfo> _debuffs;

    public IReadOnlyList<BuffDebuffInfo> AllEntries => entries;

    private void BuildLookupIfNeeded()
    {
        if (_lookupMap != null && _lookupMap.Count == entries.Count) return;

        _lookupMap = new Dictionary<BuffDebuffType, BuffDebuffInfo>();
        _commonBuffs = new List<BuffDebuffInfo>();
        _rareBuffs = new List<BuffDebuffInfo>();
        _debuffs = new List<BuffDebuffInfo>();

        foreach (var entry in entries)
        {
            if (entry == null) continue;
            _lookupMap[entry.type] = entry;

            switch (entry.category)
            {
                case BuffCategory.CommonBuff:
                    _commonBuffs.Add(entry);
                    break;
                case BuffCategory.RareBuff:
                    _rareBuffs.Add(entry);
                    break;
                case BuffCategory.Debuff:
                    _debuffs.Add(entry);
                    break;
            }
        }
    }

    public BuffDebuffInfo Get(BuffDebuffType type)
    {
        BuildLookupIfNeeded();
        if (_lookupMap != null && _lookupMap.TryGetValue(type, out var info))
        {
            return info;
        }
        return null;
    }

    public BuffDebuffInfo GetRandomBuff(bool allowRare = true)
    {
        BuildLookupIfNeeded();
        bool isRare = allowRare && (UnityEngine.Random.value < 0.15f) && _rareBuffs.Count > 0;
        if (isRare)
        {
            return _rareBuffs[UnityEngine.Random.Range(0, _rareBuffs.Count)];
        }
        if (_commonBuffs != null && _commonBuffs.Count > 0)
        {
            return _commonBuffs[UnityEngine.Random.Range(0, _commonBuffs.Count)];
        }
        return null;
    }

    public BuffDebuffInfo GetRandomDebuff()
    {
        BuildLookupIfNeeded();
        if (_debuffs != null && _debuffs.Count > 0)
        {
            return _debuffs[UnityEngine.Random.Range(0, _debuffs.Count)];
        }
        return null;
    }

    public List<BuffDebuffInfo> GetRandomBuffs(int count, bool allowRare = true)
    {
        BuildLookupIfNeeded();
        List<BuffDebuffInfo> pool = new List<BuffDebuffInfo>();
        if (_commonBuffs != null) pool.AddRange(_commonBuffs);
        if (allowRare && _rareBuffs != null) pool.AddRange(_rareBuffs);

        // Shuffle pool
        for (int i = 0; i < pool.Count; i++)
        {
            int r = UnityEngine.Random.Range(i, pool.Count);
            var temp = pool[i];
            pool[i] = pool[r];
            pool[r] = temp;
        }

        List<BuffDebuffInfo> result = new List<BuffDebuffInfo>();
        for (int i = 0; i < Mathf.Min(count, pool.Count); i++)
        {
            result.Add(pool[i]);
        }
        return result;
    }

    public List<BuffDebuffInfo> GetRandomDebuffs(int count)
    {
        BuildLookupIfNeeded();
        List<BuffDebuffInfo> pool = new List<BuffDebuffInfo>();
        if (_debuffs != null) pool.AddRange(_debuffs);

        // Shuffle pool
        for (int i = 0; i < pool.Count; i++)
        {
            int r = UnityEngine.Random.Range(i, pool.Count);
            var temp = pool[i];
            pool[i] = pool[r];
            pool[r] = temp;
        }

        List<BuffDebuffInfo> result = new List<BuffDebuffInfo>();
        for (int i = 0; i < Mathf.Min(count, pool.Count); i++)
        {
            result.Add(pool[i]);
        }
        return result;
    }

#if UNITY_EDITOR
    public void SetEntries(List<BuffDebuffInfo> list)
    {
        entries = list;
        _lookupMap = null;
    }
#endif
}
