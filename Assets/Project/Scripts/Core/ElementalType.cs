using System;
using UnityEngine;

public enum ElementType
{
    Metal = 0,  // Kim (Vàng / Bạc / Trắng)
    Wood = 1,   // Mộc (Xanh lá)
    Water = 2,  // Thủy (Xanh dương)
    Fire = 3,   // Hỏa (Đỏ / Cam)
    Earth = 4   // Thổ (Nâu / Vàng đất)
}

public static class ElementalHelper
{
    /// <summary>
    /// Tính hệ số khắc chế giữa hệ tấn công và hệ phòng thủ theo Ngũ Hành GDD:
    /// Kim -> Mộc -> Thổ -> Thủy -> Hỏa -> Kim (Khắc tăng 50% = 1.5x, Bị khắc giảm 20% = 0.8x, Trung tính = 1.0x)
    /// </summary>
    public static float GetMultiplier(ElementType attacker, ElementType defender)
    {
        if (attacker == defender) return 1.0f;

        switch (attacker)
        {
            case ElementType.Metal:
                if (defender == ElementType.Wood) return 1.5f;
                if (defender == ElementType.Fire) return 0.8f;
                break;
            case ElementType.Wood:
                if (defender == ElementType.Earth) return 1.5f;
                if (defender == ElementType.Metal) return 0.8f;
                break;
            case ElementType.Earth:
                if (defender == ElementType.Water) return 1.5f;
                if (defender == ElementType.Wood) return 0.8f;
                break;
            case ElementType.Water:
                if (defender == ElementType.Fire) return 1.5f;
                if (defender == ElementType.Earth) return 0.8f;
                break;
            case ElementType.Fire:
                if (defender == ElementType.Metal) return 1.5f;
                if (defender == ElementType.Water) return 0.8f;
                break;
        }

        return 1.0f;
    }

    /// <summary>
    /// Lấy màu sắc đại diện cho từng hệ nguyên tố
    /// </summary>
    public static Color GetColor(ElementType element)
    {
        return element switch
        {
            ElementType.Metal => new Color(0.9f, 0.9f, 0.95f, 1f), // Trắng Bạc Kim
            ElementType.Wood  => new Color(0.2f, 0.85f, 0.3f, 1f),  // Xanh Lá Mộc
            ElementType.Water => new Color(0.2f, 0.6f, 1f, 1f),     // Xanh Lam Thủy
            ElementType.Fire  => new Color(1f, 0.3f, 0.15f, 1f),    // Đỏ Lửa Hỏa
            ElementType.Earth => new Color(0.85f, 0.6f, 0.25f, 1f), // Nâu Đất Thổ
            _ => Color.white
        };
    }

    /// <summary>
    /// Tên tiếng Việt của hệ theo GDD
    /// </summary>
    public static string GetVietnameseName(ElementType element)
    {
        return element switch
        {
            ElementType.Metal => "Kim",
            ElementType.Wood  => "Mộc",
            ElementType.Water => "Thủy",
            ElementType.Fire  => "Hỏa",
            ElementType.Earth => "Thổ",
            _ => "Vô Hệ"
        };
    }

    /// <summary>
    /// Hệ bị khắc chế bởi hệ này (Ai sợ hệ này)
    /// </summary>
    public static ElementType GetTargetCountered(ElementType element)
    {
        return element switch
        {
            ElementType.Metal => ElementType.Wood,
            ElementType.Wood  => ElementType.Earth,
            ElementType.Earth => ElementType.Water,
            ElementType.Water => ElementType.Fire,
            ElementType.Fire  => ElementType.Metal,
            _ => element
        };
    }

    /// <summary>
    /// Hệ khắc chế hệ này (Hệ này sợ ai)
    /// </summary>
    public static ElementType GetWeakAgainst(ElementType element)
    {
        return element switch
        {
            ElementType.Metal => ElementType.Fire,
            ElementType.Wood  => ElementType.Metal,
            ElementType.Earth => ElementType.Wood,
            ElementType.Water => ElementType.Earth,
            ElementType.Fire  => ElementType.Water,
            _ => element
        };
    }
}

