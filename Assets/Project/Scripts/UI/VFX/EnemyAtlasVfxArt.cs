using UnityEngine;

internal sealed class EnemyAtlasVfxArt
{
    private readonly RectInt[] cells;
    private readonly Texture2D[] atlases;
    private readonly Sprite[] sprites;
    internal readonly Shader Shader;
    internal readonly Sprite Quad;

    private EnemyAtlasVfxArt(Texture2D common, Texture2D bosses, Shader shader, RectInt[] cells)
    {
        atlases = new[] { common, bosses };
        this.cells = cells;
        sprites = new Sprite[cells.Length];
        Shader = shader;
        var white = Texture2D.whiteTexture;
        Quad = Sprite.Create(white, new Rect(0, 0, white.width, white.height), new Vector2(0.5f, 0.5f), white.width,
            0, SpriteMeshType.FullRect);
        Quad.hideFlags = HideFlags.HideAndDontSave;
    }

    internal static EnemyAtlasVfxArt Load(string folder, string prefix, RectInt[] cells,
        Vector2Int? commonSize = null, Vector2Int? bossSize = null)
    {
        Texture2D common = null;
        Texture2D bosses = null;
        Shader shader = null;

        var mgr = GameDataManager.Instance;
        if (mgr != null)
        {
            mgr.TryGetEnemyAtlasVfxAssets(prefix, out common, out bosses, out shader);
        }

        if (!common) common = Resources.Load<Texture2D>(folder + prefix + "Common");
        if (!bosses) bosses = Resources.Load<Texture2D>(folder + prefix + "Bosses");
        if (!shader) shader = Resources.Load<Shader>("VFX/EnemyWater/WaterSprite");

        Vector2Int expectedCommon = commonSize ?? new Vector2Int(1402, 1122);
        Vector2Int expectedBoss = bossSize ?? new Vector2Int(1619, 971);
        if (!common || common.width != expectedCommon.x || common.height != expectedCommon.y ||
            !bosses || bosses.width != expectedBoss.x || bosses.height != expectedBoss.y || !shader || !shader.isSupported)
            return null;
        return new EnemyAtlasVfxArt(common, bosses, shader, cells);
    }

    internal Texture2D Atlas(int id) => atlases[id < 20 ? 0 : 1];

    private Rect PixelRect(int id)
    {
        RectInt cell = cells[id];
        return new Rect(cell.x, Atlas(id).height - cell.yMax, cell.width, cell.height);
    }

    internal Vector4 Uv(int id)
    {
        Rect rect = PixelRect(id);
        var atlas = Atlas(id);
        // Sample inside the cell so bilinear filtering stays in its own sprite.
        return new Vector4((rect.x + .5f) / atlas.width, (rect.y + .5f) / atlas.height,
            (rect.width - 1f) / atlas.width, (rect.height - 1f) / atlas.height);
    }

    internal float Aspect(int id) => (float)cells[id].height / cells[id].width;

    internal Sprite GetSprite(int id)
    {
        if (!sprites[id])
        {
            sprites[id] = Sprite.Create(Atlas(id), PixelRect(id), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprites[id].hideFlags = HideFlags.HideAndDontSave;
        }
        return sprites[id];
    }
}
