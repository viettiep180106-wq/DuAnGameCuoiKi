using UnityEngine;

internal sealed class EnemyFireAdvancedAttackVfx
{
    private readonly EnemyAttackVfxArt artwork;
    private readonly EnemyFireAdvancedVisuals visuals;
    private EnemyAttackVfxStyle style;
    private EnemyBase caster;
    private Player target;
    private Vector3 stance;
    private Vector3 castFeet;
    private float weaponScale;
    private float scale;
    private bool burnShown;

    internal EnemyFireAdvancedAttackVfx(Transform root, EnemyAttackVfxArt artwork)
    {
        this.artwork = artwork;
        visuals = new EnemyFireAdvancedVisuals(root, artwork);
    }

    private bool UsesHammer => style is EnemyAttackVfxStyle.HammerStrike or EnemyAttackVfxStyle.MagmaHammer;
    internal bool PlaysThroughMiss => style == EnemyAttackVfxStyle.DragonBreath;

    internal void Begin(EnemyAttackVfxStyle attackStyle, EnemyBase enemy, Player player, Vector3 feet,
        float visualScale, int layer, int order)
    {
        style = attackStyle;
        caster = enemy;
        target = player;
        stance = WeaponOrigin();
        castFeet = feet;
        scale = visualScale;
        var sprite = caster.spriteRenderer;
        weaponScale = sprite && sprite.sprite ? Mathf.Clamp(sprite.bounds.size.y / 6.4f, 0.35f, 3f) : scale;
        burnShown = false;
        int groundOrder = player.spriteRenderer ? player.spriteRenderer.sortingOrder - 1 : order - 2;
        visuals.SetSorting(layer, order, groundOrder);
    }

    internal void Impact(Player player, Vector3 feet)
    {
        target = player;
        if (UsesHammer)
            stance = WeaponOrigin();
        if (style == EnemyAttackVfxStyle.MagmaHammer)
        {
            Vector3 ground = EnemyFireAdvancedVisuals.HammerGroundPoint(stance, feet, weaponScale, true);
            EnemyFireStatusVfx.ShowBurn(player, artwork, false, ground, scale);
        }
    }

    internal float EndAfter(float contact) => style switch
    {
        EnemyAttackVfxStyle.HammerStrike => contact + 0.64f,
        EnemyAttackVfxStyle.MagmaHammer => contact + 0.91f,
        EnemyAttackVfxStyle.DragonFireball => contact + 0.87f,
        EnemyAttackVfxStyle.DragonBreath => Mathf.Max(contact + 0.2f, 1.89f),
        EnemyAttackVfxStyle.BlueMeteorShower => contact + 1.23f,
        _ => contact + 0.60f
    };

    internal void Render(float time, EnemyAttackVfxFrame frame, Vector3 source, Vector3 hitPoint, Vector3 feet)
    {
        bool hit = !float.IsPositiveInfinity(frame.Contact);
        float swingScale = weaponScale;
        float release = frame.Release;
        if (UsesHammer)
        {
            release = 0.10f;
            if (!hit)
                stance = WeaponOrigin();
        }
        else if (style == EnemyAttackVfxStyle.DoomSlash)
        {
            float strikeTime = hit ? frame.Contact : frame.FlightEnd;
            release = Mathf.Max(0f, strikeTime - 0.15f);
            stance = feet + new Vector3(5.4f, 0.24f) * scale;
            swingScale = scale;
        }
        else if (style == EnemyAttackVfxStyle.DragonBreath)
        {
            release = 0.15f;
            source = DragonMouth(source);
            feet = castFeet;
        }
        else if (style == EnemyAttackVfxStyle.BlueMeteorShower && !burnShown)
        {
            // The existing ranged skill applies its burn at cast, even when its projectile misses later.
            burnShown = EnemyFireStatusVfx.ShowBurn(target, artwork, true, Vector3.zero, scale);
        }

        float groundStart = style == EnemyAttackVfxStyle.DragonBreath ? 0f : float.NaN;
        visuals.Render(style, time, scale, new EnemyAttackVfxFrame(release, frame.FlightEnd, frame.Contact, frame.Windup),
            stance, source, hitPoint, feet, swingScale, groundStart: groundStart);
    }

    private Vector3 WeaponOrigin()
    {
        if (!caster)
            return stance;
        return caster.spriteRenderer ? caster.spriteRenderer.transform.position : caster.transform.position;
    }

    private Vector3 DragonMouth(Vector3 fallback)
    {
        if (!caster || !caster.spriteRenderer || !caster.spriteRenderer.sprite)
            return fallback;

        var renderer = caster.spriteRenderer;
        Vector2 point = renderer.sprite.name switch
        {
            "Atack_2_0" => new(0.183f, 0.720f),
            "Atack_2_2" => new(0.367f, 0.442f),
            _ => new(0.378f, 0.477f)
        };
        float height = renderer.bounds.size.y;
        return renderer.transform.position + new Vector3(point.x * height * (renderer.flipX ? -1f : 1f),
            point.y * height);
    }

    internal void Clear()
    {
        visuals.Clear();
        caster = null;
        target = null;
    }

    internal void Dispose() => visuals.Dispose();
}
