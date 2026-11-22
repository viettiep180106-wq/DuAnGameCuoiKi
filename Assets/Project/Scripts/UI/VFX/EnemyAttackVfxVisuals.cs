using System;
using UnityEngine;

public enum EnemyAttackVfxStyle
{
    None = 0,
    FireBolt = 1,
    FireBeam = 2,
    Bite = 3,
    FlameBite = 4,
    HeatArrow = 5,
    DarkFireball = 6,
    MagicOrb = 7,
    FirePillar = 8,
    HammerStrike = 9,
    MagmaHammer = 10,
    DragonFireball = 11,
    DragonBreath = 12,
    DoomSlash = 13,
    BlueMeteorShower = 14
}

public readonly struct EnemyAttackVfxFrame
{
    public readonly float Release;
    public readonly float FlightEnd;
    public readonly float Contact;
    public readonly float Windup;

    public EnemyAttackVfxFrame(float release, float flightEnd, float contact, float windup)
    {
        Release = release;
        FlightEnd = flightEnd;
        Contact = contact;
        Windup = windup;
    }
}

public sealed class EnemyAttackVfxVisuals : IDisposable
{
    public const float NormalImpactDelay = 0.075f;
    private static readonly int EffectId = Shader.PropertyToID("_PreviewEffect");
    private static readonly int SmokeTimeId = Shader.PropertyToID("_SmokeTime");

    private enum Effect
    {
        Drop, Spark, Beam, Charge, Radial,
        Slash, Jaw, BurningSlash, GroundWave,
        HeatArrow, FireRing, DarkOrb, Explosion,
        MagicOrb, Stardust, Sigil, Pillar, Scorch
    }

    private readonly Transform root;
    private readonly EnemyAttackVfxArt artwork;
    private readonly Material effectMaterial;
    private readonly SpriteRenderer[] layers = new SpriteRenderer[16];
    private readonly MaterialPropertyBlock properties = new();
    private Material smokeMaterial;
    private ParticleSystem smoke;
    private ParticleSystemRenderer smokeRenderer;
    private ParticleSystem.Particle[] smokeParticles;
    private int smokeCount;
    private Sprite upperJaw;
    private Sprite lowerJaw;

    private Vector3 HitPoint;
    private Vector3 Feet;
    private Vector3 awayFromCaster;
    private int sortingLayer;
    private int sortingOrder;

    public EnemyAttackVfxVisuals(Transform root, EnemyAttackVfxArt artwork)
    {
        this.root = root;
        this.artwork = artwork;
        effectMaterial = new Material(artwork.EffectShader) { hideFlags = HideFlags.HideAndDontSave };
    }

    public void Render(EnemyAttackVfxStyle style, float time, float scale, EnemyAttackVfxFrame timing,
        Vector3 source, Vector3 hitPoint, Vector3 feet, Vector3 away, int layer, int order)
    {
        HitPoint = hitPoint;
        Feet = feet;
        awayFromCaster = away;
        sortingLayer = layer;
        sortingOrder = order;
        foreach (var renderer in layers)
            if (renderer)
                renderer.enabled = false;

        smokeCount = 0;
        switch (style)
        {
            case EnemyAttackVfxStyle.FireBolt: DrawSpirit(0, time, scale, timing, source); break;
            case EnemyAttackVfxStyle.FireBeam: DrawSpirit(1, time, scale, timing, source); break;
            case EnemyAttackVfxStyle.Bite: DrawHound(0, time, scale, timing); break;
            case EnemyAttackVfxStyle.FlameBite: DrawHound(1, time, scale, timing); break;
            case EnemyAttackVfxStyle.HeatArrow: DrawImp(0, time, scale, timing, source); break;
            case EnemyAttackVfxStyle.DarkFireball: DrawImp(1, time, scale, timing, source); break;
            case EnemyAttackVfxStyle.MagicOrb: DrawWitch(0, time, scale, timing, source); break;
            case EnemyAttackVfxStyle.FirePillar: DrawWitch(1, time, scale, timing, source); break;
        }

        if (smoke)
        {
            if (smokeCount > 0 && !smoke.isPaused)
            {
                smoke.Play(false);
                smoke.Pause(false);
            }
            smoke.SetParticles(smokeParticles, smokeCount);
            smokeRenderer.enabled = smokeCount > 0;
            smokeRenderer.sortingLayerID = layer;
            smokeRenderer.sortingOrder = order - 1;
            smokeMaterial.SetFloat(SmokeTimeId, time);
        }
    }

    private GameObject CreateChild(string name)
    {
        var child = new GameObject(name) { hideFlags = HideFlags.DontSave };
        child.transform.SetParent(root, false);
        return child;
    }

    private void PrepareSmoke()
    {
        var smokeObject = CreateChild("Smoke Particles");
        smoke = smokeObject.AddComponent<ParticleSystem>();
        smokeParticles = new ParticleSystem.Particle[48];
        var main = smoke.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = smokeParticles.Length;
        var emission = smoke.emission;
        emission.enabled = false;
        var shape = smoke.shape;
        shape.enabled = false;
        smokeRenderer = smokeObject.GetComponent<ParticleSystemRenderer>();
        smokeMaterial = new Material(artwork.SmokeShader) { hideFlags = HideFlags.HideAndDontSave };
        smokeRenderer.sharedMaterial = smokeMaterial;
        smoke.Play(false);
        smoke.Pause(false);
    }

    private static Sprite CreateJaw(Sprite jaw, float y, float height)
    {
        Rect rect = jaw.rect;
        var result = UnityEngine.Sprite.Create(jaw.texture, new Rect(rect.x, rect.y + y, rect.width, height),
            new Vector2(0.5f, 0.5f), jaw.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        result.hideFlags = HideFlags.HideAndDontSave;
        return result;
    }

    private void DrawSpirit(int mode, float time, float scale, EnemyAttackVfxFrame timing, Vector3 source)
    {
        float hit = time - timing.Contact;
        if (mode == 0)
        {
            Projectile(0, Effect.Drop, time, timing, source, scale * 2.6f, 26f);
            OneShot(3, Effect.Spark, HitPoint, hit - NormalImpactDelay, 0.15f, scale * 5.2f, true);
            return;
        }

        float charge = Unit(time / timing.Release);
        Sprite(0, Effect.Charge, source, scale * Mathf.Lerp(4.9f, 1.2f, charge),
            Envelope(time, 0f, 0.04f, timing.Release - 0.03f, timing.Release), -time * 530f);
        if (time >= timing.Release && time < timing.Contact)
        {
            float flight = time - timing.Release;
            Vector3 front = Travel(time, timing, source);
            Vector3 beam = front - source;
            Vector3 direction = (HitPoint - source).normalized;
            float angle = Angle(direction);
            float alpha = Smooth(0f, 0.028f, flight);
            float tipSize = Mathf.Min(beam.magnitude, scale * (2.7f + 0.45f * Mathf.Sin(flight * 58f)));
            float tipSway = Mathf.Sin(flight * 43f) * 0.19f * scale;
            Draw(1, artwork.Effects[(int)Effect.Beam], source + beam * 0.5f,
                beam.magnitude, scale * 2.15f, alpha, angle, 1, flight);
            Sprite(2, Effect.Drop, front - direction * (tipSize * 0.40f) + Vector3.up * tipSway,
                tipSize, alpha * 0.92f, angle + 26f + 9f * Mathf.Sin(flight * 49f), 6, flight);
        }
        OneShot(4, Effect.Radial, HitPoint, hit, 0.33f, scale * 7.4f, false);
    }

    private void DrawHound(int mode, float time, float scale, EnemyAttackVfxFrame timing)
    {
        float hit = time - timing.Contact;
        DrawBiteSmoke(hit, scale);
        if (mode == 0)
        {
            Sprite(2, Effect.Slash, HitPoint, scale * 6.3f, Envelope(hit, 0f, 0.025f, 0.14f, 0.29f),
                -10f, 2, Unit(hit / 0.20f));
            return;
        }

        if (!upperJaw)
        {
            var jaws = artwork.Effects[(int)Effect.Jaw];
            float middle = jaws.rect.height * 0.5f;
            lowerJaw = CreateJaw(jaws, 0f, middle);
            upperJaw = CreateJaw(jaws, middle, jaws.rect.height - middle);
        }

        float snap = time - timing.Release;
        float closing = Smooth(timing.Windup * 0.525f, timing.Windup, snap);
        float gap = scale * Mathf.Lerp(2.05f, 0.65f, closing);
        float jawAlpha = Envelope(snap, 0f, 0.045f, 0.20f, 0.29f);
        float width = scale * Mathf.Lerp(5.1f, 7.1f, Smooth(0f, 0.10f, snap));
        DrawNatural(1, upperJaw, HitPoint + Vector3.up * gap, width, jawAlpha, 0f);
        DrawNatural(2, lowerJaw, HitPoint - Vector3.up * gap, width, jawAlpha, 0f);
        Sprite(5, Effect.BurningSlash, HitPoint, scale * 7.1f,
            Envelope(hit, 0f, 0.05f, 0.18f, 0.39f), Mathf.Sin(hit * 55f) * 3.5f, 3, hit * 2.4f);
        GroundRing(0, Effect.GroundWave, Feet, hit, 0.30f, scale * 10f);
    }

    private void DrawImp(int mode, float time, float scale, EnemyAttackVfxFrame timing, Vector3 source)
    {
        float hit = time - timing.Contact;
        if (mode == 0)
        {
            Projectile(1, Effect.HeatArrow, time, timing, source, scale * 3.55f);
            GroundRing(2, Effect.FireRing, HitPoint, hit - NormalImpactDelay, 0.15f, scale * 5.9f, 1f);
            return;
        }

        DrawOrbSmoke(time, scale, timing, source);
        if (time >= timing.Release && time < timing.Contact)
        {
            Sprite(6, Effect.DarkOrb, Travel(time, timing, source), scale * 6.4f, 1f,
                Angle(source - HitPoint), 7, time - timing.Release);
        }
        if (hit >= 0f && hit < 0.53f)
            DrawImpBurst(hit, scale);
    }

    private void DrawImpBurst(float hit, float scale)
    {
        var quad = artwork.Effects[(int)Effect.Explosion];
        float flashSize = scale * Mathf.Lerp(2.7f, 11.6f, Smooth(0f, 0.12f, hit));
        Draw(0, quad, HitPoint, flashSize, flashSize,
            1f - Smooth(0.035f, 0.18f, hit), 0f, 8, Unit(hit / 0.18f));
        float centerWidth = scale * Mathf.Lerp(1.4f, 5.45f, Smooth(0f, 0.13f, hit));
        Draw(1, quad, HitPoint + Vector3.up * (hit * scale * 2.8f), centerWidth, centerWidth * 0.89f,
            Envelope(hit, 0f, 0.034f, 0.09f, 0.245f), hit * 60f, 9, Unit(hit / 0.245f), 0.38f);

        for (int i = 0; i < 9; i++)
        {
            float seed = ParticleVariation(i + 74);
            float age = hit - i * 0.0055f;
            float life = Mathf.Lerp(0.25f, 0.45f, seed);
            if (age <= 0f || age >= life)
                continue;

            float phase = age / life;
            float angle = i * (2f * Mathf.PI / 9f) + (seed - 0.5f) * 0.46f;
            Vector3 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
            float distance = (1f - Mathf.Exp(-age * 12f)) * Mathf.Lerp(2.6f, 5.35f, seed);
            Vector3 position = HitPoint + scale * (direction * distance + Vector3.up * (age * age * 6f));
            float width = scale * Mathf.Lerp(1.65f, 3.65f, Smooth(0f, 0.15f, age));
            float aspect = Mathf.Lerp(0.67f, 1.03f, phase);
            Draw(2 + i, quad, position, width, width * aspect,
                Envelope(phase, 0f, 0.10f, 0.43f, 1f), angle * Mathf.Rad2Deg + phase * 28f, 9, phase, seed);
        }

        for (int i = 0; i < 5; i++)
        {
            float seed = ParticleVariation(i + 108);
            float age = hit - 0.018f - seed * 0.027f;
            float life = Mathf.Lerp(0.29f, 0.47f, seed);
            if (age <= 0f || age >= life)
                continue;

            float angle = i * (2f * Mathf.PI / 5f) + seed * 0.71f;
            Vector3 velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * Mathf.Lerp(14f, 25f, seed);
            Vector3 position = HitPoint + scale * (velocity * (age / (1f + 1.9f * age)) + Vector3.down * (12f * age * age));
            float size = scale * Mathf.Lerp(1.02f, 0.35f, age / life);
            Draw(11 + i, quad, position, size, size * 0.44f,
                Envelope(age, 0f, 0.025f, 0.09f, life), Angle(velocity + Vector3.down * (24f * age)),
                9, age / life, seed);
        }
    }

    private void DrawBiteSmoke(float hit, float scale)
    {
        Vector3 awayFromHound = awayFromCaster;
        Vector3 origin = Feet + awayFromHound * 1.55f + Vector3.up * 0.24f;
        for (int i = 0; i < 18; i++)
        {
            float seed = ParticleVariation(i);
            float age = hit - (i * 0.006f + seed * 0.024f);
            float lifetime = Mathf.Lerp(0.30f, 0.48f, seed);
            if (age <= 0f || age >= lifetime)
                continue;

            float life = Unit(age / lifetime);
            float expansion = 1f - (1f - life) * (1f - life);
            float lift = 1.65f + 6.5f * ParticleVariation(i + 23);
            Vector3 offset = awayFromHound * (expansion * (2.9f + seed * 3.2f))
                + Vector3.up * (age * lift * (1f - expansion * 0.35f));
            float size = scale * (0.6f + expansion * 3.2f) * Mathf.Lerp(0.72f, 1.23f, seed);
            float alpha = Envelope(age, 0f, 0.045f, 0.12f, lifetime) * 0.53f;
            AddSmokeParticle(origin + offset * scale, size, age, lifetime,
                new Color(0.67f, 0.52f, 0.43f, alpha), seed * 360f - expansion * 42f, i);
        }
    }

    private void DrawOrbSmoke(float time, float scale, EnemyAttackVfxFrame timing, Vector3 source)
    {
        const float interval = 0.014f;
        Vector3 behindOrb = (source - HitPoint).normalized;
        int first = Mathf.Max(0, Mathf.FloorToInt((time - timing.Release - 0.45f) / interval));
        float lastBirth = Mathf.Min(time, timing.Contact);
        for (int i = first; timing.Release + i * interval < lastBirth; i++)
        {
            float born = timing.Release + i * interval;
            float age = time - born;
            float seed = ParticleVariation(i + 59);
            float lifetime = Mathf.Lerp(0.28f, 0.45f, seed);
            if (age <= 0f || age >= lifetime)
                continue;

            float spread = Unit(age / lifetime);
            float drift = (Mathf.Sin(i * 0.39f + age * 9f) + (seed - 0.5f) * 1.3f) * spread * 1.52f;
            Vector3 position = Travel(born, timing, source) + scale
                * (behindOrb * (1.15f + age * (1.6f + seed * 1.2f)) + Vector3.up * (age * 4.8f + drift));
            float size = scale * (1.10f + spread * 3.25f) * Mathf.Lerp(0.72f, 1.24f, seed);
            float alpha = Envelope(age, 0f, 0.038f, 0.075f, lifetime) * 0.51f;
            Color color = Color.Lerp(new Color(0.58f, 0.38f, 0.30f), new Color(0.34f, 0.30f, 0.35f), spread);
            color.a = alpha;
            AddSmokeParticle(position, size, age, lifetime, color, seed * 360f + spread * 62f, i);
        }
    }

    private void AddSmokeParticle(Vector3 position, float size, float age, float lifetime,
        Color color, float rotation, int index)
    {
        // Sampling by age keeps the same smoke visible during preview scrubbing.
        if (!smoke)
            PrepareSmoke();
        smokeParticles[smokeCount++] = new ParticleSystem.Particle
        {
            position = position,
            startSize = size,
            startLifetime = lifetime,
            remainingLifetime = lifetime - age,
            startColor = color,
            rotation = rotation,
            randomSeed = (uint)index + 1
        };
    }

    private static float ParticleVariation(int index) =>
        Mathf.Repeat(Mathf.Sin(index * 12.9898f + 0.41f) * 43758.54f, 1f);

    private void DrawWitch(int mode, float time, float scale, EnemyAttackVfxFrame timing, Vector3 source)
    {
        float hit = time - timing.Contact;
        if (mode == 0)
        {
            Projectile(1, Effect.MagicOrb, time, timing, source, scale * 3.6f, spin: 580f, arc: 1.3f);
            Sprite(2, Effect.Stardust, HitPoint + Vector3.up * (Mathf.Max(0f, hit) * 4.4f),
                scale * Mathf.Lerp(2.2f, 6.15f, Smooth(0f, 0.24f, hit)),
                Envelope(hit, 0f, 0.055f, 0.15f, 0.42f), hit * 19f);
            return;
        }

        var sigil = artwork.Effects[(int)Effect.Sigil];
        float sigilWidth = scale * 9.4f;
        float sigilHeight = sigilWidth * sigil.rect.height / sigil.rect.width;
        Vector3 sigilPosition = Feet + Vector3.up * (0.057f * sigilHeight);
        Draw(0, sigil, sigilPosition, sigilWidth, sigilHeight,
            Envelope(time, 0f, 0.045f, timing.Contact + 0.045f, timing.Contact + 0.18f), 0f, 4, Unit(time / timing.Windup));
        if (hit < 0f)
            return;

        float height = scale * 10.7f * Smooth(0f, 0.15f, hit);
        float fade = 1f - Smooth(0.55f, 0.68f, hit);
        float width = scale * (5.4f + 0.7f * Mathf.Sin(hit * 49f)) * fade;
        float pillarAlpha = Envelope(hit, 0f, 0.035f, 0.55f, 0.68f);
        var pillar = artwork.Effects[(int)Effect.Pillar];
        Draw(2, pillar, Feet + Vector3.up * (height * 0.49f), width, height, pillarAlpha, 0f, 5, hit);
        Draw(3, pillar, Feet + Vector3.up * (height * 0.53f), width * 0.68f,
            height * 1.07f, pillarAlpha * 0.43f, 0f, 5, hit + 0.12f);
        for (int i = 0; i < 7; i++)
        {
            float rise = Mathf.Repeat(hit * 3.4f + i / 7f, 1f);
            float wrap = Mathf.Sin(hit * 23f + i * 2.4f);
            Vector3 tongue = Feet + new Vector3(wrap * width * 0.42f, height * (0.64f + rise * 0.55f), 0f);
            Sprite(4 + i, Effect.Drop, tongue, scale * (0.65f + (1f - rise) * 0.9f),
                pillarAlpha * Envelope(rise, 0f, 0.12f, 0.38f, 1f) * 0.83f, 110f + wrap * 27f, 6, hit);
        }
        Draw(1, artwork.Effects[(int)Effect.Scorch], Feet, scale * 8.8f, scale * 2.4f,
            Envelope(hit, 0.56f, 0.65f, 0.71f, 1.15f) * 0.9f, 0f);
    }

    private void Projectile(int layer, Effect art, float time, EnemyAttackVfxFrame timing, Vector3 source, float width,
        float angleOffset = 0f, float spin = 0f, float arc = 0f)
    {
        if (time < timing.Release || time >= timing.Contact)
            return;
        Vector3 position = Travel(time, timing, source, arc);
        Sprite(layer, art, position, width, 1f, Angle(HitPoint - source) + angleOffset + (time - timing.Release) * spin);
    }

    private Vector3 Travel(float time, EnemyAttackVfxFrame timing, Vector3 start, float arc = 0f)
    {
        float t = Mathf.InverseLerp(timing.Release, timing.FlightEnd, time);
        return Vector3.Lerp(start, HitPoint, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * arc);
    }

    private void OneShot(int layer, Effect art, Vector3 point, float age, float length, float size, bool contract)
    {
        float grow = Smooth(0f, length * 0.35f, age);
        float shrink = contract ? 1f - Smooth(length * 0.5f, length, age) * 0.55f : 1f;
        Sprite(layer, art, point, size * Mathf.Lerp(0.22f, 1f, grow) * shrink,
            Envelope(age, 0f, length * 0.19f, length * 0.33f, length), age * 26f);
    }

    private void GroundRing(int layer, Effect art, Vector3 point, float age, float duration, float size, float aspect = 0.35f)
    {
        float width = size * Mathf.Lerp(0.29f, 1f, Smooth(0f, duration * 0.64f, age));
        Draw(layer, artwork.Effects[(int)art], point, width, width * aspect,
            Envelope(age, 0f, duration * 0.18f, duration * 0.40f, duration), 0f);
    }

    private void Sprite(int layer, Effect art, Vector3 point, float width, float alpha, float rotation,
        int shaderMode = 0, float progress = 0f) =>
        DrawNatural(layer, artwork.Effects[(int)art], point, width, alpha, rotation, shaderMode, progress);

    private void DrawNatural(int layer, Sprite sprite, Vector3 point, float width, float alpha, float rotation,
        int shaderMode = 0, float progress = 0f) =>
        Draw(layer, sprite, point, width, width * sprite.rect.height / sprite.rect.width, alpha, rotation, shaderMode, progress);

    private void Draw(int layer, Sprite sprite, Vector3 point, float width, float height, float alpha, float rotation,
        int shaderMode = 0, float progress = 0f, float variation = 0f)
    {
        if (alpha <= 0.001f || width <= 0.001f || height <= 0.001f)
            return;
        var renderer = layers[layer];
        if (!renderer)
        {
            renderer = CreateChild("Attack VFX").AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = effectMaterial;
            layers[layer] = renderer;
        }
        renderer.sortingLayerID = sortingLayer;
        renderer.sortingOrder = sortingOrder + layer;
        renderer.enabled = true;
        renderer.sprite = sprite;
        renderer.transform.position = point;
        renderer.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        var nativeSize = sprite.bounds.size;
        renderer.transform.localScale = new Vector3(width / nativeSize.x, height / nativeSize.y, 1f);
        renderer.color = new Color(1f, 1f, 1f, alpha);
        properties.SetVector(EffectId, new Vector4(shaderMode, progress, variation, 0f));
        renderer.SetPropertyBlock(properties);
    }

    private static float Angle(Vector3 direction) => Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    private static float Unit(float value) => Mathf.Clamp01(value);
    private static float Smooth(float start, float end, float time) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, time));
    private static float Envelope(float time, float start, float peak, float hold, float end) =>
        Smooth(start, peak, time) * (1f - Smooth(hold, end, time));

    public void Dispose()
    {
        Destroy(effectMaterial);
        Destroy(smokeMaterial);
        Destroy(upperJaw);
        Destroy(lowerJaw);
    }

    private static void Destroy(UnityEngine.Object asset)
    {
        if (!asset)
            return;
        if (Application.isPlaying)
            UnityEngine.Object.Destroy(asset);
        else
            UnityEngine.Object.DestroyImmediate(asset);
    }
}
