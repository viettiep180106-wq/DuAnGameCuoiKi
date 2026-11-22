using System;
using System.Collections.Generic;
using UnityEngine;
using static EnemyEarthFx;
using static EnemyAtlasVfxLayers;

internal sealed class EnemyEarthAttackVfx
{
    internal const float DurationMultiplier = 2f;
    private static readonly Vector3[] PreviewTimes =
    {
        new(.29f,.53f,1.13f), new(.23f,.89f,2.48f), new(.25f,.49f,1.12f), new(.24f,1.08f,2.55f),
        new(.23f,.58f,1.16f), new(.30f,.73f,2.34f), new(.31f,.49f,1.04f), new(.27f,.97f,2.33f),
        new(.27f,.62f,1.33f), new(.25f,1.31f,2.68f), new(.22f,.63f,1.28f), new(.29f,.72f,2.48f),
        new(.30f,.58f,1.33f), new(.37f,.91f,2.77f)
    };

    private readonly EnemyAtlasVfxArt art;
    private readonly EnemyAtlasVfxLayers layers;
    private readonly List<StoneBase> locked = new(5);
    private readonly Action<StoneBase> rememberLock;
    private EnemyAttackVfxStyle style;
    private EnemyBase caster;
    private Player target;
    private Vector3 timing;
    private Vector2 muzzleOffset;
    private Vector3 source;
    private Vector3 sourceFeet;
    private float visualScale;
    private float nextContact;
    private float miss;
    private bool immune;
    private float phase;
    private float hitAge;

    internal EnemyEarthAttackVfx(Transform root, EnemyAtlasVfxArt art)
    {
        this.art = art;
        layers = new EnemyAtlasVfxLayers(root, art);
        rememberLock = locked.Add;
    }

    internal static bool Supports(EnemyAttackVfxStyle style) =>
        style is >= EnemyAttackVfxStyle.MudSlam and <= EnemyAttackVfxStyle.Earthquake;

    internal bool UsesArmor => style is EnemyAttackVfxStyle.MudArmor or EnemyAttackVfxStyle.StoneGuard
        or EnemyAttackVfxStyle.ThornArmor or EnemyAttackVfxStyle.MagmaArmor;

    internal Action<StoneBase> BoardCellEffect => style is EnemyAttackVfxStyle.StoneSeal
        or EnemyAttackVfxStyle.GreaterStoneSeal or EnemyAttackVfxStyle.Earthquake ? rememberLock : null;

    internal float Duration => UsesArmor ? .55f : timing.z * DurationMultiplier;

    internal void Begin(EnemyAttackVfxStyle kind, EnemyBase enemy, Player player, Vector2 offset, float scale, int layer, int order)
    {
        style = kind;
        caster = enemy;
        target = player;
        muzzleOffset = offset;
        visualScale = scale;
        int index = (int)kind - (int)EnemyAttackVfxStyle.MudSlam;
        timing = PreviewTimes[index];
        var action = index % 2 == 0 ? caster.GetNormalAttackActionType() : caster.GetSkillActionType();
        nextContact = action == EnemySkillActionType.MeleeDash ? .30f : timing.y * DurationMultiplier;
        miss = float.PositiveInfinity;
        immune = false;
        locked.Clear();
        FollowCaster(player.transform.position.x <= caster.transform.position.x ? -1f : 1f);
        int behind = player.spriteRenderer ? player.spriteRenderer.sortingOrder - 1 : order - 2;
        layers.SetSorting(layer, order, behind);
    }

    internal float BindProjectile(float age, float duration, float estimatedContact)
    {
        nextContact = age + Mathf.Max(.025f, estimatedContact);
        return Mathf.Max(Duration, age + duration + .25f);
    }

    internal float Impact(float age) => UsesArmor ? age + .12f :
        Mathf.Max(Duration, age + (timing.z - timing.y) * DurationMultiplier);

    internal float Miss(float age)
    {
        miss = age;
        return Mathf.Max(Duration, age + .30f);
    }

    internal void BeforeGameplay() => immune = target && target.HasImmunityCrowdControl;

    internal void AfterGameplay()
    {
        if (UsesArmor)
            EnemyEarthArmorVfx.Show(caster, style, art);
        if (locked.Count > 0)
            EnemyEarthStatusVfx.ShowSeal(art, locked, style);
        if (target && style == EnemyAttackVfxStyle.GreaterStoneSeal && target.AtkDebuffRounds > 0)
            EnemyEarthStatusVfx.ShowPlayer(art, target, false);
        if (target && style == EnemyAttackVfxStyle.Earthquake && !immune && target.IsStunned)
            EnemyEarthStatusVfx.ShowPlayer(art, target, true);
    }

    private void FollowCaster(float direction)
    {
        var bounds = EnemyAttackVfx.CharacterBounds(caster);
        source = bounds.center + new Vector3(direction * muzzleOffset.x, muzzleOffset.y);
        sourceFeet = new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * .06f, bounds.center.z);
    }

    internal void Render(float age, float contact, float end, Vector3 hit, Vector3 feet, Vector3 facing)
    {
        if (UsesArmor) return;
        bool hitReached = !float.IsPositiveInfinity(contact);
        hitAge = hitReached ? Mathf.Max(0f, age - contact) * (timing.z - timing.y) / Mathf.Max(.04f, end - contact) : -1f;
        phase = hitReached ? timing.y + hitAge : timing.y * Mathf.Clamp01(age / nextContact);
        bool moving = !hitReached && age < miss;
        if (moving && caster && caster.gameObject.activeInHierarchy) FollowCaster(facing.x);

        layers.BeginFrame();
        switch (style)
        {
            case EnemyAttackVfxStyle.MudSlam:
                Sweep(0, feet, facing, WetClay, 6.55f, -44f, 2.8f);
                Burst(layers, 1, feet, hitAge, visualScale, 6f, WetClay, 24, 9f, .35f);
                layers.Draw(1, feet, S(Mathf.Lerp(3.1f, 6.0f, Smooth(0,.18f,hitAge))), Pulse(hitAge,.06f,.18f,.31f,.59f),
                    hitAge, height: S(.76f), warp: .033f, ground: true);
                break;
            case EnemyAttackVfxStyle.ClaySpear:
                Sweep(5, hit - Vector3.up * S(.62f), facing, FiredClay, 7.2f, 35f, 2.45f);
                Burst(layers, 6, hit - Vector3.up * S(.6f), hitAge, visualScale, 5.45f, FiredClay, 28, 12.4f, .45f);
                break;
            case EnemyAttackVfxStyle.RuneGravel:
            case EnemyAttackVfxStyle.StoneSeal:
                if (moving) Flight(10, hit, style == EnemyAttackVfxStyle.StoneSeal ? 3.2f : 4.05f, Rocky, 119f, .85f);
                Burst(layers, 11, hit - Vector3.up * S(.77f), hitAge, visualScale, 4.9f, Rocky, 24, 10.2f, .42f);
                break;
            case EnemyAttackVfxStyle.StoneSting:
                Sweep(15, feet, facing, Rocky, 6.75f, 53f, 3.05f);
                Burst(layers, 16, feet, hitAge, visualScale, 6.18f, Sandy, 28, 12.1f, .45f);
                layers.Ring(feet, hitAge, .46f, S(5.9f), .22f, Sandy);
                break;
            case EnemyAttackVfxStyle.RollingMagma:
                Roll(feet, facing, moving);
                Burst(layers, 21, feet, hitAge, visualScale, 8.1f, Hot, 34, 13.9f, .53f);
                layers.Ring(feet, hitAge, .55f, S(8.7f), .24f, Hot);
                break;
            case EnemyAttackVfxStyle.SeismicOrb:
            case EnemyAttackVfxStyle.GreaterStoneSeal:
                if (moving) Flight(25, hit, style == EnemyAttackVfxStyle.GreaterStoneSeal ? 4.2f : 5.7f, Sandy, -510f, .62f);
                Burst(layers, 26, feet, hitAge, visualScale, 6.85f, Sandy, 31, 12.1f, .48f);
                layers.Ring(feet, hitAge, .57f, S(8.2f), .25f, Sandy);
                break;
            case EnemyAttackVfxStyle.EarthPillar:
                Pillar(feet, facing);
                Burst(layers, 31, feet, hitAge, visualScale, 8.75f, Sandy, 39, 15.2f, .58f);
                layers.Ring(feet, hitAge, .66f, S(10f), .21f, Gold);
                break;
            case EnemyAttackVfxStyle.Earthquake:
                if (moving) FaultLine(feet);
                Burst(layers, 32, feet, hitAge, visualScale, 12.6f, Rocky, 45, 19.2f, .74f);
                layers.Ring(feet, hitAge, .85f, S(15.1f), .24f, Gold);
                layers.Ring(feet, hitAge - .12f, .56f, S(10.2f), .20f, Sandy);
                break;
        }
        layers.EndFrame(true);
    }

    private void Sweep(int cell, Vector3 end, Vector3 facing, Color color, float width, float twist, float rise)
    {
        float alpha = Pulse(phase, timing.x, timing.x + .039f, timing.y + .034f, timing.y + .18f);
        if (alpha <= .004f) return;
        float progress = Smooth(timing.x, timing.y, phase);
        Vector3 start = source - facing * S(1.3f) + Vector3.up * S(1.4f);
        Vector3 control = source + facing * S(1.55f) + Vector3.up * S(rise);
        Vector3 Trace(float t) => Vector3.Lerp(Vector3.Lerp(start, control, t), Vector3.Lerp(control, end, t), t);
        Vector3 tip = Trace(progress);
        Vector3 travel = tip - Trace(Mathf.Max(0f, progress - .043f));
        Vector3 pivot = tip - travel.normalized * S(1.37f);
        pivot.y = Mathf.Max(pivot.y, end.y + S(.52f));
        layers.Draw(cell, pivot, S(width), alpha, phase, angle: Angle(travel) + twist,
            warp: cell == 0 ? .047f : .021f, reveal: Mathf.Clamp01(.17f + progress), radial: true);
        for (int i = 1; i <= 19; i++)
        {
            float u = i / 19f;
            var p = Trace(progress * u) + Vector3.up * S(Seed(i) * .36f);
            layers.Particle(p, S(.11f + u * .19f), color, alpha * (.21f + u * .56f), Angle(travel));
        }
    }

    private void Flight(int cell, Vector3 hit, float size, Color color, float spin, float rise)
    {
        float travel = Mathf.InverseLerp(timing.x, timing.y, phase);
        if (travel <= 0f) return;
        Vector3 Curve(float fraction) => Vector3.Lerp(source, hit, fraction) + Vector3.up * (Mathf.Sin(Mathf.PI * fraction) * S(rise));
        Vector3 center = Curve(travel);
        float alpha = Smooth(0f, .12f, travel);
        layers.Draw(cell, center, S(size * Mathf.Lerp(.65f, 1f, travel)), alpha, phase, angle: (phase - timing.x) * spin, warp: .035f);
        for (int i = 0; i < 15; i++)
        {
            float ghost = travel - i * .024f;
            if (ghost <= 0f) continue;
            var p = Curve(ghost) + Vector3.up * S(Mathf.Sin(i * 1.2f + travel * 11f) * i * .038f);
            layers.Particle(p, S(.38f - i * .017f), color, alpha * (1f - i / 16f) * .75f, phase * 300f + i * 21f);
        }
        if (cell == 25) layers.Orbit(center, -phase, S(1.68f), S(.69f), Sandy, alpha * .71f);
        else if (style == EnemyAttackVfxStyle.RuneGravel)
            layers.Draw(cell, Curve(Mathf.Max(0f, travel - .095f)) + Vector3.up * S(1.5f * Mathf.Sin(Mathf.PI * travel)),
                S(1.9f), alpha * .77f, phase, angle: phase * -225f);
    }

    private void Roll(Vector3 feet, Vector3 facing, bool moving)
    {
        if (!moving || phase < timing.x) return;
        float progress = Mathf.InverseLerp(timing.x, timing.y, phase);
        Vector3 ball = Vector3.Lerp(sourceFeet, feet, progress) + Vector3.up * S(1.65f + Mathf.Sin(Mathf.PI * progress) * .55f);
        float alpha = Smooth(0f, .12f, progress);
        layers.Draw(20, ball, S(Mathf.Lerp(4.7f, 6.2f, progress)), alpha, phase, angle: facing.x * progress * 168f,
            warp: .026f, flip: facing.x < 0f);
        for (int i = 0; i < 23; i++)
        {
            var point = ball - facing * S(i * .19f) + Vector3.up * S(Seed(i + 91) * i * .092f - .9f);
            layers.Particle(point, S(.30f + Seed(i) * .43f), i % 3 == 0 ? Hot : Rocky, alpha * (1f - i / 24f) * .81f);
        }
    }

    private void Pillar(Vector3 feet, Vector3 direction)
    {
        float progress = Mathf.InverseLerp(timing.x, timing.y, phase);
        float alpha = Pulse(phase, timing.x, timing.x + .08f, timing.y + .045f, timing.y + .27f);
        Vector3 bottom = feet - direction * ((1f - progress) * S(1.9f)) + Vector3.up * ((1f - progress * progress) * S(9.4f));
        layers.Draw(30, bottom + Vector3.up * S(4.1f), S(Mathf.Lerp(5.9f, 8.1f, progress)), alpha, phase,
            height: S(8.3f), angle: Mathf.Lerp(direction.x * 13f, 0f, progress), warp: .019f);
        if (alpha > .004f)
            layers.Burst(bottom + Vector3.up * S(3.6f), Mathf.Repeat(phase - timing.x, .16f), 12,
                S(8f), S(.40f), Sandy, life: .21f, gravity: S(12f));
    }

    private void FaultLine(Vector3 feet)
    {
        float advance = Mathf.InverseLerp(timing.x, timing.y, phase);
        if (advance <= 0f) return;
        for (int i = 0; i < 5; i++)
        {
            float progress = advance - i * .13f;
            if (progress <= 0f) continue;
            Vector3 current = Vector3.Lerp(sourceFeet, feet, progress);
            float height = S(Mathf.Lerp(.85f, 4.8f, progress));
            float width = S(Mathf.Lerp(3.3f, 9.1f, progress));
            float alpha = (1f - i * .16f) * Smooth(0,.17f,progress);
            layers.Draw(32, current + Vector3.up * height * .5f, width, alpha, phase,
                height: height, warp: .025f, rise: true, reveal: Smooth(0f,.18f,progress), ground: true);
            layers.Ring(current, Mathf.Repeat(phase - timing.x + i * .06f, .24f), .24f, width * .93f, .17f, Sandy);
        }
    }

    private float S(float value) => value * visualScale;

    internal void Clear()
    {
        caster = null;
        target = null;
        locked.Clear();
        layers.BeginFrame();
        layers.EndFrame(true);
    }

    internal void Dispose() => layers.Dispose();
}

internal static class EnemyEarthFx
{
    internal static readonly Color WetClay = new(.48f, .33f, .15f);
    internal static readonly Color FiredClay = new(.77f, .44f, .24f);
    internal static readonly Color Rocky = new(.57f, .49f, .40f);
    internal static readonly Color Sandy = new(.79f, .64f, .47f);
    internal static readonly Color Hot = new(1f, .34f, .10f);
    internal static readonly Color Gold = new(1f, .70f, .31f);

    internal static float Smooth(float start, float end, float t) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, t));
    internal static float Pulse(float t, float start, float peak, float hold, float end) =>
        Smooth(start, peak, t) * (1f - Smooth(hold, end, t));

    internal static void Burst(EnemyAtlasVfxLayers layers, int cell, Vector3 position, float age, float scale, float size,
        Color tint, int count, float speed, float fade)
    {
        if (age < 0f) return;
        float growth = Mathf.Lerp(.28f, 1f, Smooth(0f,.18f,age));
        float height = size * scale * growth * .66f;
        layers.Draw(cell, position + Vector3.up * height * .45f, size * scale * growth,
            Pulse(age,0f,.031f,.12f,fade), age, height: height, warp: .023f);
        layers.Burst(position, age, count, speed * scale, .61f * scale, tint, life: fade + .16f, gravity: scale * 42f);
        for (int i = 0; i < 15; i++)
        {
            float seed = Seed(i + 101);
            float flight = Mathf.InverseLerp(i * .009f, .48f + seed * .44f, age);
            var point = position + new Vector3((Seed(i + 21) - .5f) * (2.5f + size * flight),
                (.60f + seed * 2.1f) * flight) * scale;
            layers.Particle(point, (.26f + flight * (.50f + seed * .30f)) * scale, tint, Mathf.Sin(flight * Mathf.PI) * .43f);
        }
    }
}
