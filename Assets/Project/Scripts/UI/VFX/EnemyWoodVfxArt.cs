using UnityEngine;

internal static class EnemyWoodVfxArt
{
    internal const int TalismanPaper = 35;
    internal const int AncientRootCrack = 36;
    private static readonly RectInt[] Cells =
    {
        new(32,53,253,239), new(300,50,260,253), new(552,31,303,288), new(868,30,220,289), new(1095,48,283,266),
        new(31,313,255,228), new(307,330,247,198), new(565,315,272,210), new(839,336,278,186), new(1121,315,253,253),
        new(21,598,306,179), new(317,575,226,207), new(518,573,324,202), new(840,549,270,245), new(1114,565,258,206),
        new(17,796,280,267), new(296,796,267,248), new(557,791,269,269), new(817,838,300,205), new(1104,790,271,267),
        new(20,22,328,302), new(360,25,301,296), new(675,24,320,298), new(1002,32,295,280), new(1305,30,285,293),
        new(27,327,321,280), new(349,322,306,292), new(650,335,331,276), new(976,336,344,277), new(1314,336,276,283),
        new(14,614,334,317), new(346,673,332,263), new(673,609,314,350), new(990,646,331,289), new(1333,619,269,307),
        new(166,360,153,139), // Paper alone, with the moving aura rendered separately.
        new(14,810,334,121) // Broken ground at the base of the ancient root sprite.
    };

    private static EnemyAtlasVfxArt cached;
    internal static EnemyAtlasVfxArt Load() => cached != null && cached.Quad
        ? cached : cached = EnemyAtlasVfxArt.Load("VFX/EnemyWood/", "Wood", Cells);
}
