using UnityEngine;
using UnityEngine.UI;

internal sealed class EnemyWaterStatusVfx : MonoBehaviour
{
    private enum Kind { Stone, Energy, Player, Heal }
    private const float Twice = EnemyWaterAttackVfx.DurationMultiplier;
    private Kind kind;
    private int cell;
    private float startedAt;
    private float unlockedAt = -1f;
    private float size;
    private BaseCharacter actor;
    private Player player;
    private StoneBase stone;
    private RectTransform area;
    private Image effect;
    private Image column;
    private Image[] chips;
    private SpriteRenderer worldSprite;

    internal static void ShowStone(EnemyAtlasVfxArt art, StoneBase stone, bool freeze)
    {
        if (!stone || !stone.gameObject.activeInHierarchy || stone.transform is not RectTransform parent) return;
        int cell = freeze ? 28 : 18;
        var existing = Find(parent, Kind.Stone, cell);
        if (!existing)
        {
            // If an already sealed gem is frozen, use one overlay for the stronger effect.
            var seal = freeze ? Find(parent, Kind.Stone, 18) : null;
            if (seal) seal.Retire();
            existing = Create(parent, Kind.Stone, cell, true);
            existing.stone = stone;
            existing.size = Mathf.Max(32f, Mathf.Min(parent.rect.width, parent.rect.height));
            existing.effect = ImageFor(existing.area, art.GetSprite(cell));
            existing.chips = new Image[4];
            for (int i = 0; i < existing.chips.Length; i++)
                existing.chips[i] = ImageFor(existing.area, art.GetSprite(26));
            if (freeze) existing.column = ImageFor(existing.area, art.GetSprite(29));
        }
        existing.unlockedAt = -1f;
        existing.startedAt = Time.time;
        existing.enabled = true;
        existing.DrawStone(0f);
    }

    internal static void PulseEnergy(EnemyAtlasVfxArt art, int cell)
    {
        var hud = PlayerStatsUI.Instance;
        if (!hud || hud.EnergyCoreTargetTransform is not RectTransform parent || !parent.gameObject.activeInHierarchy)
            return;
        var pulse = Find(parent, Kind.Energy, cell);
        if (!pulse)
        {
            pulse = Create(parent, Kind.Energy, cell, true);
            pulse.size = Mathf.Max(30f, Mathf.Min(parent.rect.width, parent.rect.height));
            pulse.effect = ImageFor(pulse.area, art.GetSprite(cell));
        }
        pulse.startedAt = Time.time;
        pulse.DrawEnergy(0f);
    }

    internal static void ShowPlayer(EnemyAtlasVfxArt art, Player actor, int cell)
    {
        if (!actor || !actor.gameObject.activeInHierarchy) return;
        var status = Find(actor.transform, Kind.Player, cell) ?? CreateWorld(art, actor, Kind.Player, cell);
        status.player = actor;
        status.startedAt = Time.time;
        status.DrawWorld(0f);
    }

    internal static void ShowHeal(EnemyBase actor)
    {
        if (!Application.isPlaying || !actor || !actor.gameObject.activeInHierarchy) return;
        var art = EnemyWaterVfxArt.Load();
        if (art == null) return;
        var status = Find(actor.transform, Kind.Heal, 14) ?? CreateWorld(art, actor, Kind.Heal, 14);
        status.startedAt = Time.time;
        status.DrawWorld(0f);
    }

    private static EnemyWaterStatusVfx CreateWorld(EnemyAtlasVfxArt art, BaseCharacter actor, Kind kind, int cell)
    {
        var vfx = Create(actor.transform, kind, cell, false);
        vfx.actor = actor;
        vfx.worldSprite = vfx.gameObject.AddComponent<SpriteRenderer>();
        vfx.worldSprite.sprite = art.GetSprite(cell);
        var renderer = actor.spriteRenderer;
        vfx.worldSprite.sortingLayerID = renderer ? renderer.sortingLayerID : 0;
        vfx.worldSprite.sortingOrder = renderer ? renderer.sortingOrder + 3 : 39;
        return vfx;
    }

    private static EnemyWaterStatusVfx Find(Transform parent, Kind kind, int cell)
    {
        // Search direct children only so an Energy pulse cannot reuse a board overlay.
        var child = parent.Find(Name(kind, cell));
        if (child && child.gameObject.activeSelf) return child.GetComponent<EnemyWaterStatusVfx>();
        return null;
    }

    private static EnemyWaterStatusVfx Create(Transform parent, Kind kind, int cell, bool ui)
    {
        var root = ui ? new GameObject(Name(kind, cell), typeof(RectTransform)) : new GameObject(Name(kind, cell));
        root.hideFlags = HideFlags.DontSave;
        root.transform.SetParent(parent, false);
        var vfx = root.AddComponent<EnemyWaterStatusVfx>();
        vfx.kind = kind;
        vfx.cell = cell;
        vfx.area = root.transform as RectTransform;
        if (vfx.area)
        {
            vfx.area.anchorMin = vfx.area.anchorMax = new Vector2(.5f, .5f);
            vfx.area.anchoredPosition = Vector2.zero;
            vfx.area.sizeDelta = Vector2.zero;
            vfx.area.SetAsLastSibling();
        }
        return vfx;
    }

    private static string Name(Kind kind, int cell) => $"Water VFX {kind} {cell}";

    private static Image ImageFor(RectTransform parent, Sprite sprite)
    {
        var child = new GameObject("Water Overlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        child.hideFlags = HideFlags.DontSave;
        child.transform.SetParent(parent, false);
        var image = child.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        image.preserveAspect = false;
        return image;
    }

    private void LateUpdate()
    {
        float age = (Time.time - startedAt) / Twice;
        switch (kind)
        {
            case Kind.Stone: DrawStone(age); break;
            case Kind.Energy: DrawEnergy(age); break;
            default: DrawWorld(age); break;
        }
    }

    private void DrawStone(float age)
    {
        if (!stone) { Retire(); return; }
        bool freeze = cell == 28;
        bool remains = freeze ? stone.isFrozen : stone.isLocked;
        if (!remains && unlockedAt < 0f) unlockedAt = Time.time;
        float thaw = unlockedAt < 0f ? 0f : Mathf.Clamp01((Time.time - unlockedAt) / (.45f * Twice));
        if (thaw >= 1f) { Retire(); return; }

        float lag = freeze ? Mathf.Max(0, stone.GetOriginIndex().x) * .026f : 0f;
        float localAge = age - lag;
        float settle = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(localAge / .23f));
        float flash = Mathf.Sin(settle * Mathf.PI);
        Place(effect, size * Mathf.Lerp(.24f, 1.15f, settle), size * Mathf.Lerp(.24f, 1.18f, settle),
            Vector2.zero, 0f, Mathf.Clamp01(settle * .72f + flash * .45f) * (1f - thaw));
        if (column)
        {
            float strike = Mathf.Clamp01(localAge / .48f);
            Place(column, size * 1.04f, size * Mathf.Lerp(2.65f, 1.15f, strike), Vector2.up * size * (1.06f - strike),
                0f, localAge > 0f && remains ? Mathf.Sin(strike * Mathf.PI) * .81f : 0f);
        }
        float burst = remains ? Mathf.Clamp01(localAge / .37f) : thaw;
        for (int i = 0; i < chips.Length; i++)
        {
            Vector2 dir = EnemyAtlasVfxLayers.Direction(36f + i * 90f);
            Place(chips[i], size * .32f * (1f - burst *.42f), size * .26f * (1f - burst *.28f),
                dir * size * Mathf.Lerp(.18f, .75f, burst), i * 90f + burst * 53f, Mathf.Sin(burst * Mathf.PI) * .8f);
        }
    }

    private void DrawEnergy(float age)
    {
        float t = Mathf.Clamp01(age / .59f);
        if (t >= 1f) { Retire(); return; }
        float width = size * Mathf.Lerp(.62f, 1.72f, Mathf.SmoothStep(0f, 1f, t));
        Place(effect, width, width * .88f, Vector2.zero, -96f * t, Mathf.Sin(t * Mathf.PI) * .82f);
    }

    private void DrawWorld(float age)
    {
        bool visible = kind == Kind.Heal ? age < .82f : player && player.CurrentHP > 0 && (cell switch
        {
            4 => player.ExtraSkillCostRounds > 0,
            24 => player.DefDebuffRounds > 0,
            _ => player.IsStunned
        });
        if (!visible || !actor || actor.CurrentHP <= 0f) { Retire(); return; }
        Bounds bounds = EnemyAttackVfx.CharacterBounds(actor);
        float height = Mathf.Max(.4f, bounds.size.y);
        Vector3 point = bounds.center;
        point.x += kind == Kind.Heal ? 0f : height * (cell == 4 ? -.28f : cell == 24 ? .29f : 0f);
        point.y = kind == Kind.Heal ? bounds.min.y + height * (.33f + age * .67f)
            : bounds.max.y + height * (.22f + .035f * Mathf.Sin(age * 7f));
        float progress = Mathf.Clamp01(age / .82f);
        float width = height * (kind == Kind.Heal ? .73f : .33f) * Mathf.Lerp(.72f, 1f, Mathf.Clamp01(age * 10f));
        float alpha = kind == Kind.Heal ? .92f * Mathf.Sin(progress * Mathf.PI) : .91f * Mathf.Clamp01(age * 15f);
        float ppuWidth = worldSprite.sprite.bounds.size.x;
        Vector3 parentScale = transform.parent.lossyScale;
        transform.position = point;
        transform.rotation = Quaternion.Euler(0f, 0f, cell == 34 ? 8f * Mathf.Sin(age * 12f) : 0f);
        transform.localScale = new Vector3(width / (ppuWidth * Mathf.Max(.001f, Mathf.Abs(parentScale.x))),
            width / (ppuWidth * Mathf.Max(.001f, Mathf.Abs(parentScale.y))), 1f);
        worldSprite.color = new Color(1f, 1f, 1f, alpha);
    }

    private static void Place(Image image, float width, float height, Vector2 position, float angle, float alpha)
    {
        image.enabled = alpha > .005f;
        if (!image.enabled) return;
        var rect = image.rectTransform;
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = position;
        rect.localRotation = Quaternion.Euler(0f, 0f, angle);
        image.color = new Color(1f, 1f, 1f, alpha);
    }

    private void Retire()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        if (Application.isPlaying && (kind == Kind.Stone || kind == Kind.Heal) && gameObject.activeSelf)
            Destroy(gameObject);
    }
}
