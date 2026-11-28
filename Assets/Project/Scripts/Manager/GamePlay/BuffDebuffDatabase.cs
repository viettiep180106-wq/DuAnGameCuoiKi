using System;
using System.Collections.Generic;
using UnityEngine;

public enum BuffCategory
{
    CommonBuff,
    RareBuff,
    Debuff
}

public enum BuffSubCategory
{
    CuongHoa,       // 40% (ATK, DEF, MaxHP, Crit)
    TaiTao,         // 35% (Energy, HealHP 10%, HealHP 20%)
    PassiveThuong,  // 25% (Shield50, Shield25_Reflect, DoubleSkill, DoubleStoneEffect, Reflect100, LamMoiSkill)
    Hiem,           // 5% tổng (StackAtkCrit, DoubleNextWave, DmgToHeal, VampireDouble)
    Debuff          // Debuff các loại
}

[Serializable]
public class BuffDebuffInfo
{
    public BuffDebuffType type;
    public string displayName;
    [TextArea(2, 4)] public string description;
    public Sprite icon;
    public BuffCategory category;
    public BuffSubCategory subCategory;
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
                var mgr = GameDataManager.Instance;
                if (mgr != null && mgr.BuffDebuffDatabase != null)
                {
                    _instance = mgr.BuffDebuffDatabase;
                }
                else
                {
                    _instance = Resources.Load<BuffDebuffDatabase>("Database/BuffDebuffDatabase");
                }
            }
            return _instance;
        }
    }

    [SerializeField] private List<BuffDebuffInfo> entries = new List<BuffDebuffInfo>();

    private Dictionary<BuffDebuffType, BuffDebuffInfo> _lookupMap;
    private List<BuffDebuffInfo> _commonBuffs;
    private List<BuffDebuffInfo> _cuongHoaBuffs;
    private List<BuffDebuffInfo> _taiTaoBuffs;
    private List<BuffDebuffInfo> _passiveThuongBuffs;
    private List<BuffDebuffInfo> _rareBuffs;
    private List<BuffDebuffInfo> _debuffs;

    public IReadOnlyList<BuffDebuffInfo> AllEntries => entries;

    private void BuildLookupIfNeeded()
    {
        if (_lookupMap != null && _lookupMap.Count == entries.Count) return;

        _lookupMap = new Dictionary<BuffDebuffType, BuffDebuffInfo>();
        _commonBuffs = new List<BuffDebuffInfo>();
        _cuongHoaBuffs = new List<BuffDebuffInfo>();
        _taiTaoBuffs = new List<BuffDebuffInfo>();
        _passiveThuongBuffs = new List<BuffDebuffInfo>();
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
                    if (entry.subCategory == BuffSubCategory.CuongHoa) _cuongHoaBuffs.Add(entry);
                    else if (entry.subCategory == BuffSubCategory.TaiTao) _taiTaoBuffs.Add(entry);
                    else if (entry.subCategory == BuffSubCategory.PassiveThuong) _passiveThuongBuffs.Add(entry);
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

    /// <summary>
    /// Quay Buff ngẫu nhiên theo đúng tỉ lệ GDD:
    /// - 5% Buff Hiếm
    /// - 95% Buff Thường (40% Cường Hóa, 35% Tái Tạo, 25% Passive Thường)
    /// </summary>
    public BuffDebuffInfo GetRandomBuff(bool allowRare = true)
    {
        BuildLookupIfNeeded();
        bool isRare = allowRare && (UnityEngine.Random.value < 0.05f) && _rareBuffs.Count > 0;
        if (isRare)
        {
            return _rareBuffs[UnityEngine.Random.Range(0, _rareBuffs.Count)];
        }

        // Tỉ lệ phân nhóm Buff thường: 40% Cường Hóa, 35% Tái Tạo, 25% Passive Thường
        float roll = UnityEngine.Random.value;
        if (roll < 0.40f && _cuongHoaBuffs.Count > 0)
        {
            return _cuongHoaBuffs[UnityEngine.Random.Range(0, _cuongHoaBuffs.Count)];
        }
        else if (roll < 0.75f && _taiTaoBuffs.Count > 0)
        {
            return _taiTaoBuffs[UnityEngine.Random.Range(0, _taiTaoBuffs.Count)];
        }
        else if (_passiveThuongBuffs.Count > 0)
        {
            return _passiveThuongBuffs[UnityEngine.Random.Range(0, _passiveThuongBuffs.Count)];
        }

        // Fallback
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

    /// <summary>
    /// Rút 3 thẻ bài cho minigame Lật bài tuân thủ nghiêm ngặt quy tắc GDD:
    /// - Số lượng thẻ bài trùng tối đa trong 1 lần roll là 2 thẻ (tối đa 1 cặp trùng, KHÔNG BAO GIỜ có 3 thẻ giống hệt nhau).
    /// </summary>
    public List<BuffDebuffType> GetThreeRollCards()
    {
        BuildLookupIfNeeded();
        List<BuffDebuffType> cards = new List<BuffDebuffType>(3);

        for (int i = 0; i < 3; i++)
        {
            BuffDebuffInfo buff = GetRandomBuff(true);
            BuffDebuffType bType = buff != null ? buff.type : BuffDebuffType.Buff_CuongHoa_Atk;

            // Nếu 2 thẻ đầu đã giống nhau thì thẻ thứ 3 BẮT BUỘC phải khác
            if (i == 2 && cards[0] == cards[1] && bType == cards[0])
            {
                int attempts = 0;
                while (bType == cards[0] && attempts < 15)
                {
                    buff = GetRandomBuff(true);
                    if (buff != null) bType = buff.type;
                    attempts++;
                }
            }

            cards.Add(bType);
        }

        return cards;
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
