using UnityEngine;

internal static class EnemyWaterVfxArt
{
    private static readonly RectInt[] Cells =
    {
        new(12,64,269,196), new(303,65,253,200), new(571,22,279,258), new(850,79,266,184), new(1152,24,231,253),
        new(12,299,274,251), new(299,313,250,238), new(566,304,272,253), new(840,318,272,232), new(1121,330,269,205),
        new(9,625,284,150), new(302,578,247,234), new(595,574,224,244), new(840,581,267,230), new(1120,590,270,195),
        new(7,825,281,262), new(296,834,260,248), new(565,899,276,125), new(866,850,223,222), new(1115,863,277,204),
        new(7,10,315,288), new(328,61,293,227), new(624,35,357,245), new(981,10,336,275), new(1335,29,242,260),
        new(8,311,287,297), new(315,310,335,310), new(658,309,295,300), new(955,379,355,209), new(1317,302,296,298),
        new(7,649,312,278), new(321,644,296,290), new(609,628,373,304), new(959,640,363,289), new(1312,636,300,285)
    };

    private static EnemyAtlasVfxArt cached;

    internal static EnemyAtlasVfxArt Load() => cached != null && cached.Quad
        ? cached : cached = EnemyAtlasVfxArt.Load("VFX/EnemyWater/", "Water", Cells);
}
