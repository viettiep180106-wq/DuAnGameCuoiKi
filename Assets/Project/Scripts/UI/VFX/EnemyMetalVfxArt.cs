using UnityEngine;

internal static class EnemyMetalVfxArt
{
    private static readonly RectInt[] Cells =
    {
        new(56,22,361,215), new(458,63,184,154), new(675,12,362,250), new(1066,28,181,215), new(1322,26,265,215),
        new(110,267,247,193), new(432,251,237,214), new(689,278,328,183), new(1041,252,241,211), new(1338,263,242,191),
        new(55,530,365,123), new(437,478,226,219), new(705,486,283,209), new(1046,472,228,236), new(1346,475,245,227),
        new(70,745,337,128), new(476,734,158,147), new(680,718,347,208), new(1030,722,269,186), new(1349,706,251,227),
        new(48,15,294,286), new(389,24,280,259), new(710,24,309,272), new(1043,14,279,276), new(1354,54,308,220),
        new(56,339,265,251), new(396,339,262,249), new(701,333,310,263), new(1040,327,296,279), new(1395,345,217,262),
        new(24,619,330,290), new(368,639,319,265), new(701,643,331,259), new(1053,635,268,269), new(1326,634,349,266)
    };

    private static EnemyAtlasVfxArt cached;

    internal static EnemyAtlasVfxArt Load() => cached != null && cached.Quad
        ? cached : cached = EnemyAtlasVfxArt.Load("VFX/EnemyMetal/", "Metal", Cells, new(1676, 938), new(1678, 937));
}
