using UnityEngine;

internal static class EnemyEarthVfxArt
{
    private static readonly RectInt[] Cells =
    {
        new(31,39,260,243), new(318,72,214,191), new(557,59,302,225), new(878,55,257,236), new(1156,68,217,218),
        new(26,299,276,237), new(315,316,233,213), new(550,311,307,225), new(863,324,275,209), new(1144,289,248,253),
        new(31,544,264,237), new(315,553,229,232), new(568,542,272,257), new(838,548,295,235), new(1148,546,249,255),
        new(29,816,280,231), new(313,808,237,233), new(554,815,303,229), new(861,821,282,223), new(1152,803,241,254),
        new(12,20,305,279), new(339,19,294,274), new(645,51,328,242), new(981,37,291,255), new(1285,21,322,282),
        new(14,326,307,272), new(337,338,321,254), new(643,310,339,290), new(982,337,304,249), new(1320,328,280,249),
        new(11,605,307,355), new(324,654,317,308), new(642,663,340,298), new(982,595,319,376), new(1327,696,258,264),
    };

    private static EnemyAtlasVfxArt cached;

    internal static EnemyAtlasVfxArt Load() => cached != null && cached.Quad
        ? cached : cached = EnemyAtlasVfxArt.Load("VFX/EnemyEarth/", "Earth", Cells);
}
