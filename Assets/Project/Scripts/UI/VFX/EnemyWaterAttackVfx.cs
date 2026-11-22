using System;
using UnityEngine;
using static EnemyAtlasVfxLayers;

internal sealed class EnemyWaterAttackVfx
{
    internal const float DurationMultiplier = 2f;
    // Release, contact and end in seconds in the approved preview.
    private static readonly Vector3[] PreviewTimes =
    {
        new(.20f,.59f,1.17f), new(.24f,.59f,1.53f), new(.34f,.50f,1.03f), new(.18f,.50f,1.26f),
        new(.21f,.48f,.92f), new(.26f,.65f,1.47f), new(.23f,.55f,1.08f), new(.34f,.68f,1.53f),
        new(.36f,.55f,1.18f), new(.30f,.73f,1.73f), new(.24f,.68f,1.25f), new(.39f,.77f,1.71f),
        new(.36f,.51f,1.18f), new(.38f,.93f,1.88f)
    };

    private readonly EnemyAtlasVfxArt art;
    private readonly EnemyAtlasVfxLayers layers;
    private readonly Action<StoneBase> showBoardCell;
    private EnemyAttackVfxStyle style;
    private EnemyBase caster;
    private Player target;
    private Vector3 timing;
    private Vector2 muzzleOffset;
    private Vector3 source;
    private Vector3 hit;
    private Vector3 feet;
    private Vector3 direction;
    private float scale;
    private float predictedHit;
    private float resolvedAt;
    private float missedAt;
    private int beforeEnergy;
    private int energySpent;
    private float beforeHp;
    private bool beforeImmunity;
    private bool healed;
    private float time;
    private float after;
    private float travel;
    private bool inFlight;

    internal EnemyWaterAttackVfx(Transform root, EnemyAtlasVfxArt art)
    {
        this.art = art;
        layers = new EnemyAtlasVfxLayers(root, art);
        showBoardCell = ShowBoardCell;
    }

    internal static bool Supports(EnemyAttackVfxStyle style) =>
        style is >= EnemyAttackVfxStyle.WaterDrop and <= EnemyAttackVfxStyle.AbyssTsunami;

    internal Action<StoneBase> BoardCellEffect => style is EnemyAttackVfxStyle.FrostArrow or EnemyAttackVfxStyle.FreezeFormation
        ? showBoardCell : null;

    internal float Duration => timing.z * DurationMultiplier;

    internal void Begin(EnemyAttackVfxStyle attack, EnemyBase enemy, Player player, Vector2 offset, float size, int layer, int order)
    {
        style = attack;
        timing = PreviewTimes[(int)attack - (int)EnemyAttackVfxStyle.WaterDrop];
        caster = enemy;
        target = player;
        muzzleOffset = offset;
        scale = size;
        var bounds = EnemyAttackVfx.CharacterBounds(caster);
        float facing = target.transform.position.x <= caster.transform.position.x ? -1f : 1f;
        source = bounds.center + new Vector3(offset.x * facing, offset.y);
        var action = ((int)attack - (int)EnemyAttackVfxStyle.WaterDrop) % 2 == 0
            ? enemy.GetNormalAttackActionType() : enemy.GetSkillActionType();
        predictedHit = action == EnemySkillActionType.MeleeDash ? .30f : timing.y * DurationMultiplier;
        missedAt = float.PositiveInfinity;
        resolvedAt = float.PositiveInfinity;
        energySpent = 0;
        healed = false;
        int groundOrder = player.spriteRenderer ? player.spriteRenderer.sortingOrder - 1 : order - 2;
        layers.SetSorting(layer, order, groundOrder);
    }

    internal float BindProjectile(float age, float flightDuration, float expectedContact)
    {
        predictedHit = age + Mathf.Max(.025f, expectedContact);
        return Mathf.Max(Duration, age + flightDuration + .25f);
    }

    internal float Impact(float age) => Mathf.Max(Duration, age + (timing.z - timing.y) * DurationMultiplier);

    internal float Miss(float age)
    {
        missedAt = age;
        return Mathf.Max(Duration, age + .30f);
    }

    internal void BeforeGameplay()
    {
        if (!target) return;
        beforeEnergy = target.EnergyCores;
        beforeHp = caster.CurrentHP;
        beforeImmunity = target.HasImmunityCrowdControl;
    }

    internal void AfterGameplay(float age)
    {
        if (!target) return;
        resolvedAt = age;
        if (style is EnemyAttackVfxStyle.EnergySteal or EnemyAttackVfxStyle.CorrosiveSwamp
            or EnemyAttackVfxStyle.AbyssTrident or EnemyAttackVfxStyle.AbyssTsunami)
            energySpent = Mathf.Max(0, beforeEnergy - target.EnergyCores);
        healed = caster && caster.CurrentHP > beforeHp;
        if (energySpent > 0)
            EnemyWaterStatusVfx.PulseEnergy(art, style == EnemyAttackVfxStyle.EnergySteal ? 12 : 31);
        if (style == EnemyAttackVfxStyle.SlowWaterJet && target.ExtraSkillCostRounds > 0)
            EnemyWaterStatusVfx.ShowPlayer(art, target, 4);
        if (style == EnemyAttackVfxStyle.CorrosiveSwamp && target.DefDebuffRounds > 0)
            EnemyWaterStatusVfx.ShowPlayer(art, target, 24);
        if (style == EnemyAttackVfxStyle.AbyssTsunami && !beforeImmunity && target.IsStunned)
            EnemyWaterStatusVfx.ShowPlayer(art, target, 34);
    }

    private void ShowBoardCell(StoneBase stone) =>
        EnemyWaterStatusVfx.ShowStone(art, stone, style == EnemyAttackVfxStyle.FreezeFormation);

    internal void Render(float age, float contact, float end, Vector3 impact, Vector3 ground, Vector3 away)
    {
        bool contacted = !float.IsPositiveInfinity(contact);
        after = contacted ? Mathf.Max(0f, age - contact) * (timing.z - timing.y) / Mathf.Max(.04f, end - contact) : -1f;
        // Retain real collision timing while keeping the full VFX envelope on longer and shorter flights.
        time = contacted ? timing.y + after : timing.y * Mathf.Clamp01(age / Mathf.Max(.025f, predictedHit));
        travel = Progress(timing.x, timing.y, time);
        inFlight = !contacted && age < missedAt && time >= timing.x;
        hit = impact;
        feet = ground;
        direction = away;
        if (!contacted && caster && caster.gameObject.activeInHierarchy)
        {
            var bounds = EnemyAttackVfx.CharacterBounds(caster);
            source = bounds.center + new Vector3(muzzleOffset.x * direction.x, muzzleOffset.y);
        }

        layers.BeginFrame();
        switch (style)
        {
            case EnemyAttackVfxStyle.WaterDrop:
            case EnemyAttackVfxStyle.SlowWaterJet: DrawSlime(); break;
            case EnemyAttackVfxStyle.WaterFinSlash:
            case EnemyAttackVfxStyle.TidalBite: DrawBeast(); break;
            case EnemyAttackVfxStyle.WaterDagger:
            case EnemyAttackVfxStyle.EnergySteal: DrawThief((age - resolvedAt) / DurationMultiplier); break;
            case EnemyAttackVfxStyle.IceShards:
            case EnemyAttackVfxStyle.FrostArrow: DrawMage(); break;
            case EnemyAttackVfxStyle.SwampLash:
            case EnemyAttackVfxStyle.CorrosiveSwamp: DrawSwamp(); break;
            case EnemyAttackVfxStyle.IceCyclone:
            case EnemyAttackVfxStyle.FreezeFormation: DrawSpirit(); break;
            case EnemyAttackVfxStyle.AbyssTrident:
            case EnemyAttackVfxStyle.AbyssTsunami: DrawSeaGod(); break;
        }
        layers.EndFrame(style is EnemyAttackVfxStyle.IceShards or EnemyAttackVfxStyle.FrostArrow
            or EnemyAttackVfxStyle.IceCyclone or EnemyAttackVfxStyle.FreezeFormation);
    }

    private void DrawSlime()
    {
        if (style == EnemyAttackVfxStyle.WaterDrop)
        {
            Flight(0, 3.9f, Water, .027f, arc: .45f);
            Splash(1, hit + Vector3.down * .45f, 5.4f, Water, 24, 9.5f);
            layers.Ring(feet, after - .02f, .53f, 4.25f * scale, .28f, Water);
            return;
        }

        Charge(Water, .82f);
        Jet(2, 2.75f, .038f);
        if (after < 0f) return;
        float puddle = Pulse(after, 0f, .14f, .57f, .93f);
        layers.Draw(3, feet, Scale(Mathf.Lerp(1.35f, 8.05f, Smooth(0, .22f, after))), puddle,
            -after, height: Scale(2.45f), warp: .036f, ground: true);
        layers.Orbit(feet, -after, Scale(2.75f), Scale(.71f), Water, puddle * .60f);
        layers.Ring(feet, after, .58f, Scale(4.9f), .28f, Water);
        layers.Burst(hit, after, 25, Scale(9.2f), Scale(.46f), Water, heading: 114f);
    }

    private void DrawBeast()
    {
        if (style == EnemyAttackVfxStyle.WaterFinSlash)
        {
            Weapon(5, 5.9f, Foam, 20f);
            Splash(6, hit + Vector3.down * Scale(.75f), 5.4f, Foam, 27, 12f, heading: 119f);
            return;
        }

        float motion = Pulse(time, .075f, .23f, timing.y - .04f, timing.y + .09f);
        Vector3 advance = source - direction * Scale(.65f);
        layers.Draw(8, advance, Scale(8.25f), motion, time, angle: direction.x < 0 ? 10f : -10f,
            warp: .046f, flip: direction.x < 0);
        layers.Orbit(advance, -time, Scale(2.65f), Scale(1.25f), Foam, motion * .68f);
        for (int i = 0; i < 12 && motion > .004f; i++)
        {
            var wake = advance - direction * Scale(.22f * i) + Vector3.up * Mathf.Sin(time * 16f + i) * Scale(.5f);
            layers.Particle(wake, Scale(.22f + Seed(i) * .24f), Foam, motion * (.86f - i * .05f));
        }

        float biteAge = time - timing.y;
        float jaw = Pulse(biteAge, -.15f, -.074f, .025f, .17f);
        float jawSize = Scale(Mathf.Lerp(6.6f, 4.2f, Smooth(-.09f, .022f, biteAge)));
        layers.Draw(7, hit, jawSize, jaw, time, angle: biteAge * 365f, warp: .018f);
        layers.Orbit(hit, -time * 1.8f, jawSize * .5f, jawSize * .37f, Foam, jaw * .72f);
        if (after < 0f) return;
        float grow = Smooth(0f, .2f, after);
        layers.Draw(9, feet, Scale(Mathf.Lerp(1.8f, 10.1f, grow)), Pulse(after, 0f, .038f, .16f, .52f),
            after, height: Scale(Mathf.Lerp(.85f, 2.8f, grow)), warp: .035f, ground: true);
        layers.Ring(feet, after, .66f, Scale(8.6f), .26f, Foam);
        layers.Burst(feet, after, 38, Scale(13.5f), Scale(.50f), Foam, life: .49f);
    }

    private void DrawThief(float castAge)
    {
        bool active = style == EnemyAttackVfxStyle.EnergySteal;
        Flight(10, active ? 3.35f : 4.6f, Water, .018f, artworkAngle: 11f);
        Splash(11, hit, active ? 3.6f : 4.35f, Water, active ? 12 : 17, 8.8f, fade: .28f);
        if (!active || float.IsNegativeInfinity(castAge)) return;

        Vector3 bottle = source - direction * Scale(.8f) + Vector3.down * Scale(.35f);
        Vector3 rune = hit + Vector3.up * Scale(.45f);
        float pull = Pulse(castAge, 0f, .07f, .42f, .67f);
        layers.Draw(12, rune, Scale(Mathf.Lerp(1.3f, 4.1f, Smooth(0f, .19f, castAge))), pull * (energySpent > 0 ? .96f : .43f),
            castAge, angle: 12f - castAge * 112f, warp: .024f);
        layers.Orbit(rune, castAge, Scale(1.45f), Scale(1.32f), Water, pull * .61f);
        if (energySpent > 0)
        {
            for (int i = 0; i < 22; i++)
            {
                float leg = Progress(0f, .42f, castAge - i * .011f);
                if (leg <= 0f || leg >= 1f) continue;
                Vector3 point = Vector3.Lerp(rune, bottle, leg) + Vector3.up * (Mathf.Sin(leg * Mathf.PI) * Scale(2.5f)
                    + Mathf.Sin(leg * 22f + i * .61f) * Scale(.42f));
                layers.Particle(point, Scale(.24f + Seed(i) * .22f), Water, pull * .91f);
            }
        }
        layers.Draw(13, bottle, Scale(2.65f), Pulse(castAge, .075f, .21f, .53f, .77f), angle: Mathf.Sin(castAge * 8f) * 7f);
        if (!healed) return;
        float healAge = castAge - .43f;
        float alpha = Pulse(healAge, 0f, .095f, .22f, .65f);
        Vector3 center = bottle + Vector3.up * (Mathf.Max(0f, healAge) * Scale(3.1f));
        layers.Draw(14, center, Scale(4.45f), alpha, healAge, angle: Mathf.Sin(healAge * 10f) * 10f, warp: .029f);
        layers.Orbit(center, healAge, Scale(1.45f), Scale(1.9f), Heal, alpha);
    }

    private void DrawMage()
    {
        Charge(Ice, .70f);
        bool active = style == EnemyAttackVfxStyle.FrostArrow;
        Flight(active ? 17 : 15, active ? 6.55f : 4.96f, Ice, active ? .028f : .011f, artworkAngle: active ? 0f : 41f);
        Splash(16, hit, active ? 4.4f : 5.56f, Ice, active ? 25 : 23, 10.5f, spread: 360f, fade: active ? .27f : .34f);
        if (active && after >= 0f)
        {
            layers.Ring(feet, after, .58f, Scale(4f), .28f, Ice);
            layers.Draw(19, feet, Scale(Mathf.Lerp(1.5f, 4.2f, Smooth(0f, .21f, after))), Pulse(after, 0f, .04f, .15f, .44f),
                after, height: Scale(1.45f), warp: .018f, ground: true);
        }
    }

    private void DrawSwamp()
    {
        if (style == EnemyAttackVfxStyle.SwampLash)
        {
            Weapon(20, 6.8f, Mud, 37f);
            Splash(21, hit + Vector3.down * Scale(.86f), 6.0f, Mud, 25, 12.5f, heading: 124f);
            return;
        }

        Charge(Mud, .85f);
        Jet(22, Mathf.Lerp(2.3f, 4.15f, travel), .052f);
        if (after < 0f) return;
        float mud = Pulse(after, 0f, .19f, .59f, 1f);
        float grow = Smooth(0f, .42f, after);
        layers.Draw(23, feet + Vector3.up * Scale(.85f), Scale(Mathf.Lerp(1.8f, 9.5f, grow)), mud, after * .52f,
            height: Scale(Mathf.Lerp(1.7f, 4.55f, grow)), warp: .034f, ground: true);
        layers.Burst(feet, after, 26, Scale(12.5f), Scale(.60f), Mud, gravity: Scale(54f));
        for (int i = 0; i < 15 && mud > .004f; i++)
        {
            float seed = Seed(i + 65);
            float lift = Mathf.Repeat(after * (1.25f + seed * .8f) + i * .13f, 1f);
            var point = feet + new Vector3((Seed(i + 310) - .5f) * Scale(6.6f), lift * Scale(1.5f + seed));
            layers.Particle(point, Scale((.34f + seed * .24f) * Mathf.Lerp(.44f, 1f, lift)), Mud, mud * Mathf.Sin(lift * Mathf.PI));
        }
    }

    private void DrawSpirit()
    {
        if (style == EnemyAttackVfxStyle.IceCyclone)
        {
            Vector3 center = Flight(25, 4.85f, Ice, .049f, arc: .66f, artworkAngle: 180f, spin: 63f);
            if (inFlight) layers.Orbit(center, -time * 2.2f, Scale(1.78f), Scale(1.12f), Ice, .82f);
            Splash(26, hit, 5.8f, Ice, 28, 11.0f, spread: 360f);
            return;
        }

        float cast = time - timing.x;
        Vector3 rune = source - direction * Scale(1.5f) + Vector3.up * Scale(3.5f);
        float reveal = Smooth(-.32f, -.021f, cast);
        layers.Draw(27, rune, Scale(5.9f), Pulse(cast, -.34f, -.24f, .14f, .41f), time,
            height: Scale(2.58f), angle: Mathf.Sin(time * 4f) * 7f, reveal: reveal, radial: true);
        Flight(17, 5.15f, Ice, .025f);
        Splash(26, hit, 4.2f, Ice, 18, 8.8f, spread: 360f);
    }

    private void DrawSeaGod()
    {
        if (style == EnemyAttackVfxStyle.AbyssTrident)
        {
            Weapon(30, 7.55f, Water, 49f);
            layers.Burst(hit, after, 24, Scale(10f), Scale(.43f), Water, heading: 118f);
            if (energySpent > 0 && after >= 0f)
            {
                Vector3 center = hit + Vector3.down * Scale(.95f);
                float alpha = Pulse(after, .018f, .10f, .20f, .50f);
                layers.Draw(31, center, Scale(Mathf.Lerp(2.1f, 5.1f, Smooth(0f, .19f, after))), alpha,
                    after, angle: after * 375f, warp: .032f);
                layers.Orbit(center, -after * 1.8f, Scale(2.1f), Scale(.92f), Water, alpha);
            }
            return;
        }

        float wave = Pulse(time, timing.x - .075f, timing.x + .11f, timing.y, timing.y + .24f);
        if (wave > .004f && (inFlight || after >= 0f || time < timing.x))
        {
            Vector3 start = new Vector3(source.x, feet.y, source.z) + direction * Scale(.35f);
            Vector3 crest = Vector3.Lerp(start, feet, travel);
            float lift = Scale(Mathf.Lerp(.90f, 6.5f, Smooth(0f, .73f, travel)));
            layers.Draw(33, crest + direction * Scale(1.2f), Scale(Mathf.Lerp(3.4f, 10.7f, travel)), wave * .66f, time,
                height: Scale(Mathf.Lerp(1.1f, 2.8f, travel)), warp: .045f, ground: true);
            layers.Draw(32, crest - direction * Scale(1.0f) + Vector3.up * lift * .49f, Scale(Mathf.Lerp(4.55f, 11.15f, travel)),
                wave, time, height: lift * 1.31f, angle: direction.x < 0 ? 3.5f : -3.5f, warp: .071f, flip: direction.x < 0);
            for (int i = 0; i < 30; i++)
            {
                float seed = Seed(i + 95);
                float drift = Mathf.Repeat(time * (.96f + seed * .46f) + i * .131f, 1f);
                Vector3 point = crest - direction * Scale((seed - .38f) * 6.6f + drift * 1.74f)
                    + Vector3.up * (lift * (.23f + .36f * seed) + Mathf.Sin(drift * Mathf.PI) * Scale(.86f));
                layers.Particle(point, Scale(.19f + Seed(i + 205) * .36f), Foam, wave * Mathf.Sin(drift * Mathf.PI));
            }
        }
        if (after < 0f) return;
        float grow = Smooth(0f, .27f, after);
        float whirl = Pulse(after, 0f, .065f, .36f, .86f);
        layers.Draw(33, feet + Vector3.up * Scale(.84f), Scale(Mathf.Lerp(3.0f, 13.7f, grow)), whirl, -after,
            height: Scale(Mathf.Lerp(1.9f, 5.65f, grow)), warp: .055f, ground: true);
        layers.Orbit(feet, -after * 1.4f, Scale(4.3f), Scale(1.4f), Foam, whirl * .74f, 19);
        layers.Ring(feet, after - .03f, .79f, Scale(12.0f), .27f, Foam);
        layers.Ring(feet, after - .10f, .68f, Scale(8.9f), .31f, Water);
        layers.Burst(feet, after, 46, Scale(18.8f), Scale(.49f), Foam, life: .53f, gravity: Scale(48f));
    }

    private void Charge(Color tint, float strength)
    {
        float charge = Pulse(time, .045f, timing.x * .72f, timing.x, timing.x + .075f);
        layers.Orbit(source, -time * 1.75f, Scale(.45f + charge * .55f), Scale(.58f), tint, charge * strength, 11);
        layers.Ring(source, time, timing.x + .065f, Scale(1.85f), .92f, tint, ground: false);
    }

    private Vector3 Flight(int cell, float width, Color tint, float warp, float arc = 0f, float artworkAngle = 0f, float spin = 0f)
    {
        if (!inFlight) return source;
        Vector3 point = Vector3.Lerp(source, hit, travel) + Vector3.up * Mathf.Sin(travel * Mathf.PI) * Scale(arc);
        float angle = Angle(hit - source) - artworkAngle;
        layers.Draw(cell, point, Scale(width) * (0.86f + .14f * Smooth(0f, .26f, travel) + .027f * Mathf.Sin(time * 25f)),
            Smooth(0f, .085f, travel), time, angle: angle + spin * time, warp: warp);
        for (int i = 1; i <= 10; i++)
        {
            float offset = travel - i * .026f;
            if (offset <= 0f) continue;
            var wake = Vector3.Lerp(source, hit, offset) + Vector3.up * Scale(Mathf.Sin(offset * Mathf.PI) * arc
                + Mathf.Sin(i * 2.2f + travel * 15f) * i * .029f);
            layers.Particle(wake, Scale(.36f - .024f * i), tint, .75f - .057f * i, angle);
        }
        return point;
    }

    private void Jet(int cell, float height, float warp)
    {
        if (!inFlight) return;
        float front = 1f - (1f - travel) * (1f - travel);
        Vector3 path = (hit - source) * front;
        if (path.sqrMagnitude <= .002f) return;
        float alpha = Smooth(0f, .105f, travel);
        float angle = Angle(path);
        layers.Beam(cell, source, source + path, Scale(height) * (1f + Mathf.Sin(time * 29f) * .09f), alpha, time, warp);
        for (int i = 0; i < 19; i++)
        {
            float phase = Mathf.Repeat(time * (1.6f + Seed(i) * .22f) + i * .141f, 1f);
            Vector3 point = source + path * phase + Vector3.up * Mathf.Sin(phase * 8f + i) * Scale(.22f * phase);
            layers.Particle(point, Scale(.19f + Seed(i + 7) * .19f), cell == 22 ? Mud : Foam,
                alpha * Mathf.Sin(phase * Mathf.PI) * .86f, angle);
        }
    }

    private void Splash(int cell, Vector3 center, float width, Color tint, int count, float speed, float heading = 92f,
        float spread = 160f, float fade = .37f)
    {
        if (after < 0f) return;
        float growth = Smooth(0f, .19f, after);
        float alpha = Pulse(after, 0f, .034f, .12f, fade);
        layers.Draw(cell, center + Vector3.up * Scale(.53f * growth), Scale(width * Mathf.Lerp(.26f, 1f, growth)),
            alpha, after, warp: .023f);
        layers.Burst(center, after, count, Scale(speed), Scale(.55f), tint, life: .47f, heading: heading, spread: spread);
    }

    private void Weapon(int cell, float width, Color tint, float bladeAngle)
    {
        float motion = Smooth(timing.x, timing.y, time);
        float opacity = Pulse(time, timing.x, timing.x + .035f, timing.y + .02f, timing.y + .19f);
        if (opacity <= .004f) return;
        Vector3 start = source - direction * Scale(1.1f) + Vector3.up * Scale(1.3f);
        Vector3 control = source + direction * Scale(1.9f) + Vector3.up * Scale(3.05f);
        Vector3 aim = hit + direction * Scale(.35f) + Vector3.down * Scale(.70f);
        Vector3 Curve(float p) => Vector3.Lerp(Vector3.Lerp(start, control, p), Vector3.Lerp(control, aim, p), p);
        Vector3 front = Curve(motion);
        Vector3 tangent = front - Curve(Mathf.Max(0f, motion - .055f));
        float angle = Angle(tangent);
        layers.Draw(cell, front - tangent.normalized * Scale(1.26f), Scale(width), opacity, time,
            angle: angle + bladeAngle, warp: .018f, reveal: Mathf.Clamp01(.15f + motion), radial: true);
        for (int i = 1; i <= 17; i++)
        {
            float part = i / 17f;
            layers.Particle(Curve(part * motion), Scale(Mathf.Lerp(.065f, .27f, part)), tint, opacity * (.18f + part * .54f), angle);
        }
    }

    internal void Clear()
    {
        caster = null;
        target = null;
        layers.BeginFrame();
        layers.EndFrame(false);
    }

    internal void Dispose() => layers.Dispose();
    private float Scale(float value) => value * scale;
    private static float Progress(float start, float end, float value) => Mathf.InverseLerp(start, end, value);
    private static float Smooth(float start, float end, float value) => Mathf.SmoothStep(0f, 1f, Progress(start, end, value));
    private static float Pulse(float value, float start, float peak, float hold, float end) =>
        Smooth(start, peak, value) * (1f - Smooth(hold, end, value));
}
