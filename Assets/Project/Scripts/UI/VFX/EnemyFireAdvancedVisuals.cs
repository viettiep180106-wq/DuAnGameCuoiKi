using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class EnemyFireAdvancedVisuals : IDisposable
{
    private const int MeteorCount = 10;
    private const int TrailSamples = 30;
    private const float MeteorArtAngle = -135f;
    private static readonly Vector2 MeteorStonePivot = new(0.40f, 0.35f);
    private const float RoundSpacing = 0.66f;
    private static readonly int AtlasId = Shader.PropertyToID("_Atlas");
    private static readonly int AtlasRectId = Shader.PropertyToID("_AtlasRect");
    private static readonly int EffectId = Shader.PropertyToID("_AdvancedEffect");
    private static readonly int SmokeTimeId = Shader.PropertyToID("_SmokeTime");

    private enum Piece
    {
        Cracks, MagmaImpact, LavaPool,
        DragonBall, DragonImpact, Breath, BurningGround, SmokePlume,
        BlueFlash, Meteor, BlueWave, BlueBurn, Rock,
        HammerCrescent, MagmaCrescent, SwordSlash
    }

    // Pixel rectangles in the transparent atlas, measured from the upper-left corner.
    private static readonly RectInt[] Cells =
    {
        new(360, 120, 262, 182), new(986, 42, 294, 273), new(1284, 42, 311, 275),
        new(28, 332, 309, 269), new(354, 331, 267, 245), new(627, 333, 342, 275),
        new(970, 397, 327, 201), new(1297, 333, 286, 256),
        new(351, 620, 277, 302), new(633, 618, 302, 305),
        new(936, 618, 401, 316), new(1341, 612, 254, 326),
        new(1168, 56, 64, 77),
        new(23, 24, 326, 299), new(628, 0, 350, 327), new(19, 610, 326, 304)
    };

    private static readonly float[] MeteorOffsets = { 0f, 3.2f, -2.9f, 5.4f, -4.4f, 6.1f, -5.5f, 1.85f, -1.4f, 4.4f };
    private static readonly SwingPath HammerSwing = new(new(1.80f, 4.90f), new(-0.55f, 6.75f), new(-2.58f, 2.62f),
        Piece.HammerCrescent, new(0.85f, 0.55f), 5.4f, 10);
    private static readonly SwingPath MagmaSwing = new(new(2.08f, 4.89f), new(-0.18f, 7.35f), new(-2.61f, 1.17f),
        Piece.MagmaCrescent, new(0.77f, 0.80f), 8.1f, 11);
    private static readonly SwingPath SwordSwing = new(new(4.15f, 6.44f), new(-2.65f, 8.05f), new(-6.08f, 1.33f),
        Piece.SwordSlash, new(0.09f, 0.12f), 6.7f, 12);

    private readonly Transform root;
    private readonly Sprite[] sprites = new Sprite[Cells.Length];
    private readonly Vector4[] uvRects = new Vector4[Cells.Length];
    private readonly SpriteRenderer[] layers = new SpriteRenderer[12];
    private readonly MaterialPropertyBlock properties = new();
    private readonly Material effectMaterial;
    private ParticleLayer smoke;
    private ParticleLayer rocks;
    private ParticleLayer meteors;
    private ParticleLayer blueSparks;
    private Mesh swingMesh;
    private MeshRenderer swingRenderer;
    private readonly Vector3[] trailVertices = new Vector3[TrailSamples * 2];
    private int sortingLayer;
    private int sortingOrder = 18;
    private int GroundSortingOrder { get; set; } = 13;

    public static int PreviewRoundCount(int enemy, int mode) => mode == 0 ? 0 : enemy == 4 ? 2 : enemy == 6 ? 3 : 0;
    public static float PreviewRoundTime(int enemy, float impact, int index) =>
        impact + (enemy == 4 ? 0.78f : 1.05f) + index * RoundSpacing;

    public EnemyFireAdvancedVisuals(Transform root, EnemyAttackVfxArt artwork)
    {
        this.root = root;
        var texture = artwork.AdvancedAtlas;
        effectMaterial = new Material(artwork.AdvancedShader) { hideFlags = HideFlags.HideAndDontSave };
        effectMaterial.SetTexture(AtlasId, texture);
        for (int i = 0; i < Cells.Length; i++)
        {
            RectInt cell = Cells[i];
            var rect = new Rect(cell.x, texture.height - cell.yMax, cell.width, cell.height);
            sprites[i] = UnityEngine.Sprite.Create(texture, rect, Vector2.one * 0.5f, 128f, 0, SpriteMeshType.FullRect);
            sprites[i].name = ((Piece)i).ToString();
            sprites[i].hideFlags = HideFlags.HideAndDontSave;
            uvRects[i] = new Vector4(rect.x / texture.width, rect.y / texture.height,
                rect.width / texture.width, rect.height / texture.height);
        }
        smokeShader = artwork.SmokeShader;
    }

    private readonly Shader smokeShader;

    private void EnsureAttackLayers()
    {
        if (swingRenderer)
            return;
        smoke = new ParticleLayer(Child("Fire smoke"), smokeShader, 35, sortingOrder + 7);
        rocks = CreateSpriteParticles("Lava fragments", Piece.Rock, 12, sortingOrder + 8);
        meteors = CreateSpriteParticles("Blue meteors", Piece.Meteor, MeteorCount, sortingOrder + 13);
        meteors.EnableLifetimeStream();
        blueSparks = new ParticleLayer(Child("Blue impact sparks"), effectMaterial.shader, 24, sortingOrder + 9);
        blueSparks.Material.SetVector(EffectId, new Vector4(8f, 0f, 1f, 0f));

        var trailObject = Child("Weapon swing trail");
        swingMesh = CreateTrailMesh();
        trailObject.AddComponent<MeshFilter>().sharedMesh = swingMesh;
        swingRenderer = trailObject.AddComponent<MeshRenderer>();
        swingRenderer.sharedMaterial = effectMaterial;
        swingRenderer.sortingOrder = sortingOrder + 5;
    }

    public void Render(EnemyAttackVfxStyle style, float time, float scale, EnemyAttackVfxFrame frame,
        Vector3 stance, Vector3 source, Vector3 hitPoint, Vector3 feet, float weaponScale = 1f,
        bool simulateRounds = false, float groundStart = float.NaN)
    {
        EnsureAttackLayers();
        Reset();
        float strikeTime = float.IsPositiveInfinity(frame.Contact) ? frame.FlightEnd : frame.Contact;
        switch (style)
        {
            case EnemyAttackVfxStyle.HammerStrike:
            case EnemyAttackVfxStyle.MagmaHammer:
                DrawWarrior(style == EnemyAttackVfxStyle.MagmaHammer, time, frame, strikeTime, scale,
                    stance, feet, weaponScale, simulateRounds);
                break;
            case EnemyAttackVfxStyle.DragonFireball:
            case EnemyAttackVfxStyle.DragonBreath:
                DrawDragon(style == EnemyAttackVfxStyle.DragonBreath, time, frame, scale, source, hitPoint, feet, groundStart);
                break;
            case EnemyAttackVfxStyle.DoomSlash:
                DrawSwing(SwordSwing, stance, time, frame.Release, strikeTime, scale, weaponScale);
                DrawBlueImpact(stance + SwordSwing.End * weaponScale + new Vector3(0.55f, 0.45f) * scale,
                    time - frame.Contact - 0.019f, scale);
                break;
            case EnemyAttackVfxStyle.BlueMeteorShower:
                DrawMeteorShower(time, strikeTime, !float.IsPositiveInfinity(frame.Contact), scale, feet);
                if (simulateRounds)
                    DrawPreviewBurn(true, time, frame.Contact, scale, hitPoint);
                break;
        }
        Flush(time);
    }

    public void SetSorting(int layer, int order, int groundOrder)
    {
        sortingLayer = layer;
        sortingOrder = order;
        GroundSortingOrder = groundOrder;
    }

    public void Clear()
    {
        foreach (var layer in layers)
            if (layer)
                layer.enabled = false;
        if (swingRenderer)
        {
            Reset();
            Flush(0f);
        }
    }

    private void Reset()
    {
        foreach (var layer in layers)
            if (layer)
                layer.enabled = false;
        smoke.Count = 0;
        rocks.Count = 0;
        meteors.Count = 0;
        blueSparks.Count = 0;
        swingRenderer.enabled = false;
        swingRenderer.sortingLayerID = sortingLayer;
        swingRenderer.sortingOrder = sortingOrder + 5;
    }

    private void Flush(float time)
    {
        smoke.Material.SetFloat(SmokeTimeId, time);
        meteors.Material.SetVector(EffectId, new Vector4(9f, time, 1f, 0.12f));
        smoke.Flush(sortingLayer, sortingOrder + 7);
        rocks.Flush(sortingLayer, sortingOrder + 8);
        meteors.Flush(sortingLayer, sortingOrder + 13);
        blueSparks.Flush(sortingLayer, sortingOrder + 9);
    }

    internal static Vector3 HammerGroundPoint(Vector3 stance, Vector3 feet, float weaponScale, bool active)
    {
        SwingPath path = active ? MagmaSwing : HammerSwing;
        return new Vector3(stance.x + path.End.x * weaponScale, feet.y, feet.z);
    }

    private void DrawWarrior(bool active, float time, EnemyAttackVfxFrame frame, float strikeTime, float scale,
        Vector3 stance, Vector3 feet, float weaponScale, bool simulateRounds)
    {
        float age = time - frame.Contact;
        SwingPath path = active ? MagmaSwing : HammerSwing;
        DrawSwing(path, stance, time, frame.Release, strikeTime, scale, weaponScale);
        Vector3 strike = HammerGroundPoint(stance, feet, weaponScale, active);

        if (!active)
        {
            Sprite(0, Piece.Cracks, strike + Vector3.up * (0.64f * scale), 5.6f * scale,
                Envelope(age, 0f, 0.027f, 0.17f, 0.62f), 0f, 3, time, 1f - Smooth(0.04f, 0.33f, age),
                GroundSortingOrder);
            return;
        }

        Sprite(4, Piece.MagmaImpact, strike + Vector3.up * (2.03f * scale),
            scale * Mathf.Lerp(3.3f, 6.7f, Smooth(0f, 0.13f, age)),
            Envelope(age, 0f, 0.023f, 0.08f, 0.26f), 0f, 2, age, order: GroundSortingOrder + 1);
        DrawRocks(age, scale, strike);
        if (simulateRounds)
            DrawPreviewBurn(false, time, frame.Contact, scale, strike);
    }

    private void DrawRocks(float hitAge, float scale, Vector3 feet)
    {
        for (int i = 0; i < 12; i++)
        {
            float age = hitAge - i * 0.009f;
            float upSpeed = Mathf.Lerp(12f, 19f, Variation(i + 17));
            const float gravity = 48f;
            float life = 2f * upSpeed / gravity;
            if (age <= 0f || age >= life)
                continue;

            float sideways = (Variation(i + 9) * 2f - 1f) * 11f;
            float height = age * (upSpeed - 0.5f * gravity * age);
            Vector3 position = feet + scale * new Vector3(sideways * age, 0.35f + height);
            float size = scale * Mathf.Lerp(0.48f, 1.30f, Variation(i + 46));
            var color = new Color(1f, 0.92f, 0.79f, Envelope(age, 0f, 0.035f, life * 0.82f, life));
            rocks.Add(position, size, age, life, color, i * 77f + age * (155f - 24f * i));
        }
    }

    private void DrawDragon(bool breathAttack, float time, EnemyAttackVfxFrame frame, float scale,
        Vector3 source, Vector3 hitPoint, Vector3 feet, float groundStart)
    {
        float age = time - frame.Contact;
        if (!breathAttack)
        {
            float phase = Mathf.InverseLerp(frame.Release, frame.FlightEnd, time);
            if (time >= frame.Release && time < frame.Contact)
            {
                Vector3 travel = Vector3.Lerp(source, hitPoint, phase);
                float rotation = Angle(hitPoint - source) + 143f + Mathf.Sin(time * 27f) * 10f;
                Sprite(2, Piece.DragonBall, travel, 4.85f * scale, 1f, rotation, 2, time);
            }

            Sprite(3, Piece.DragonImpact, hitPoint, scale * Mathf.Lerp(1.8f, 6.6f, Smooth(0f, 0.17f, age)),
                Envelope(age, 0f, 0.023f, 0.09f, 0.29f), age * 42f, 2, age);
            DrawSmoke(hitPoint, age, scale, false);
            return;
        }

        float breath = time - frame.Release;
        float opacity = Envelope(breath, 0f, 0.073f, 0.48f, 0.60f);
        if (opacity > 0f)
        {
            Vector3 aim = hitPoint - source;
            float length = Smooth(0f, 0.13f, breath) * (1f + 0.060f * Mathf.Sin(breath * 43f));
            float width = aim.magnitude * 1.09f * length;
            float height = 6.25f * scale * (0.94f + 0.10f * Mathf.Sin(breath * 36f));
            // Keep the narrow upper-left tip of the painted cone at the mouth while its width changes.
            Vector3 apexToCenter = new(width * 0.43f, -height * 0.36f);
            float rotation = Angle(aim) - Angle(apexToCenter);
            Vector3 center = source + Quaternion.Euler(0f, 0f, rotation) * apexToCenter;
            Draw(1, Piece.Breath, center, width, height, opacity, rotation, 2, breath, 0.12f);
        }

        age = time - (float.IsNaN(groundStart) ? frame.Contact : groundStart);
        float groundWidth = scale * Mathf.Lerp(3.6f, 11.5f, Smooth(0f, 0.34f, age));
        Draw(0, Piece.BurningGround, feet + Vector3.up * (0.82f * scale), groundWidth, scale * 4f,
            Envelope(age, 0f, 0.07f, 0.56f, 1.04f), 0f, 3, time, 0.15f);
        float plume = age - 0.30f;
        Sprite(2, Piece.SmokePlume, feet + scale * Vector3.up * (2.0f + Mathf.Max(0f, plume) * 3.25f),
            scale * Mathf.Lerp(3.4f, 6.8f, Smooth(0f, 0.62f, plume)),
            Envelope(plume, 0f, 0.19f, 0.35f, 0.96f) * 0.40f, 23f, 3, plume);
        DrawSmoke(feet + Vector3.up * (1.5f * scale), plume, scale, true);
    }

    private void DrawSmoke(Vector3 origin, float age, float scale, bool rising)
    {
        for (int i = 0; i < (rising ? 29 : 22); i++)
        {
            float phaseOffset = rising ? 0.023f : 0.009f;
            float local = age - i * phaseOffset;
            float seed = Variation(i + 83);
            float life = Mathf.Lerp(0.46f, rising ? 0.87f : 0.64f, seed);
            if (local <= 0f || local >= life)
                continue;

            float radius = (1f - Mathf.Exp(-local * 4.2f)) * (rising ? 3f : 4f);
            float angle = i * 2.399963f;
            float lift = local * (rising ? 7.2f + seed * 3.1f : 2.6f + seed * 2.1f);
            Vector3 offset = new(radius * Mathf.Cos(angle), lift + radius * Mathf.Sin(angle) * 0.54f);
            float size = scale * (0.78f + radius * 1.02f) * (0.84f + seed * 0.32f);
            var color = rising ? new Color(0.34f, 0.30f, 0.34f) : Color.Lerp(
                new Color(0.84f, 0.37f, 0.12f), new Color(0.51f, 0.50f, 0.57f), Smooth(0.07f, 0.38f, local));
            color.a = Envelope(local, 0f, 0.065f, 0.13f, life) * (rising ? 0.65f : 0.56f);
            smoke.Add(origin + offset * scale, size, local, life, color, i * 57f + local * 41f);
        }
    }

    private void DrawPreviewBurn(bool blue, float time, float impact, float scale, Vector3 anchor)
    {
        int enemy = blue ? 6 : 4;
        int rounds = PreviewRoundCount(enemy, 1);
        float lastTick = PreviewRoundTime(enemy, impact, rounds - 1);
        float pulse = RoundPulse(enemy, time, impact, rounds);
        DrawBurn(blue, time, scale, anchor, pulse, Envelope(time, impact + (blue ? 0.046f : 0f),
            impact + (blue ? 0.22f : 0.10f), lastTick + 0.14f, lastTick + 0.44f));
    }

    internal void DrawBurn(bool blue, float time, float scale, Vector3 anchor, float pulse, float opacity)
    {
        if (blue)
        {
            Sprite(10, Piece.BlueBurn, anchor + Vector3.down * (0.19f * scale),
                scale * (4.65f + 0.30f * Mathf.Sin(time * 18f) + pulse * 0.62f),
                opacity, Mathf.Sin(time * 12f) * 3f, 3, time, 0.23f + pulse);
        }
        else
        {
            Sprite(0, Piece.LavaPool, anchor + Vector3.up * (1.05f * scale), 8.6f * scale,
                opacity, 0f, 3, time, 0.18f + pulse, GroundSortingOrder);
        }
    }

    internal void RenderArmorCue(Vector3 anchor, float age, float scale)
    {
        EnsureAttackLayers();
        Reset();
        DrawBlueImpact(anchor, age, scale);
        Flush(age);
    }

    private void DrawMeteorShower(float time, float impact, bool landedOnTarget, float scale, Vector3 feet)
    {
        for (int i = 0; i < MeteorCount; i++)
        {
            float seed = Variation(i + 113);
            float landed = impact + i * 0.086f;
            float fallDuration = Mathf.Lerp(0.34f, 0.50f, seed);
            float start = landed - fallDuration;
            Vector3 point = feet + new Vector3(MeteorOffsets[i], (i % 4) * 0.48f);
            Vector3 entry = point + new Vector3(4.75f + seed * 2.30f, 16f + seed * 6f);
            float age = time - landed;
            if (time >= start && (age < 0f || !landedOnTarget))
            {
                float phase = Mathf.Clamp((time - start) / fallDuration, 0f, landedOnTarget ? 1f : 0.96f);
                Vector3 displacement = point - entry;
                float drop = phase * (0.32f + 0.68f * phase);
                Vector3 stone = entry + new Vector3(displacement.x * phase, displacement.y * drop);
                Vector3 heading = new(displacement.x, displacement.y * (0.32f + 1.36f * phase));
                float worldRotation = Angle(heading) - MeteorArtAngle;
                float size = scale * Mathf.Lerp(4.0f, 6.85f, seed);
                Vector3 centerToStone = (Vector3)(MeteorStonePivot - Vector2.one * 0.5f) * size;
                Vector3 position = stone - Quaternion.Euler(0f, 0f, worldRotation) * centerToStone;
                // Billboard particles use clockwise angles in the preview camera.
                meteors.Add(position, size, time - start, fallDuration,
                    new Color(0.89f, 0.98f, 1f, Smooth(0f, 0.14f, phase)), -worldRotation);
            }

            if (!landedOnTarget)
                continue;
            float wave = scale * Mathf.Lerp(1.28f, i == 0 ? 6.5f : 5.1f, Smooth(0f, 0.24f, age));
            Vector3 center = point + Vector3.up * (0.34f * wave);
            Draw(i, Piece.BlueWave, center, wave, wave * 0.61f,
                Envelope(age, 0f, 0.016f, 0.17f, 0.43f), seed * 8f - 4f, 4, age,
                1f - Smooth(0.028f, 0.22f, age));
        }
    }

    private static Mesh CreateTrailMesh()
    {
        var mesh = new Mesh { name = "Weapon swing ribbon", hideFlags = HideFlags.HideAndDontSave };
        mesh.MarkDynamic();
        mesh.vertices = new Vector3[TrailSamples * 2];
        var uv = new Vector2[TrailSamples * 2];
        var indices = new int[(TrailSamples - 1) * 6];
        for (int i = 0; i < TrailSamples; i++)
        {
            int vertex = i * 2;
            float along = i / (TrailSamples - 1f);
            uv[vertex] = new Vector2(along, 0f);
            uv[vertex + 1] = new Vector2(along, 1f);
            if (i == TrailSamples - 1)
                continue;

            int triangle = i * 6;
            indices[triangle] = vertex;
            indices[triangle + 1] = vertex + 1;
            indices[triangle + 2] = vertex + 2;
            indices[triangle + 3] = vertex + 2;
            indices[triangle + 4] = vertex + 1;
            indices[triangle + 5] = vertex + 3;
        }
        mesh.uv = uv;
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);
        return mesh;
    }

    private void DrawSwing(SwingPath path, Vector3 stance, float time, float release, float impact, float scale,
        float weaponScale)
    {
        float head = Smooth(release, impact, time);
        float tail = Smooth(impact - 0.024f, impact + 0.15f, time);
        float opacity = Envelope(time, release, release + 0.031f, impact + 0.014f, impact + 0.15f);
        if (opacity <= 0.001f || head <= tail + 0.002f)
            return;

        bool blue = path.Art == Piece.SwordSlash;
        float width = 0.19f * path.ArtWidth * scale;
        Vector3 tip = Vector3.zero;
        Vector3 direction = Vector3.right;
        for (int i = 0; i < TrailSamples; i++)
        {
            float along = i / (TrailSamples - 1f);
            float t = Mathf.Lerp(tail, head, along);
            Vector3 turn = Vector3.Lerp(path.Start, path.Control, t);
            Vector3 next = Vector3.Lerp(path.Control, path.End, t);
            tip = stance + Vector3.Lerp(turn, next, t) * weaponScale;
            direction = (next - turn).normalized;
            Vector3 normal = new(-direction.y, direction.x);
            float profile = Mathf.Lerp(0.30f, 1f, Mathf.Sin(along * Mathf.PI));
            Vector3 wide = normal * (0.5f * width * profile);
            trailVertices[i * 2] = tip - wide;
            trailVertices[i * 2 + 1] = tip + wide;
        }
        swingMesh.vertices = trailVertices;
        swingMesh.RecalculateBounds();
        properties.SetVector(EffectId, new Vector4(blue ? 7f : 6f, time - release, 1f, opacity * 0.72f));
        swingRenderer.SetPropertyBlock(properties);
        swingRenderer.enabled = true;

        var art = Cells[(int)path.Art];
        Vector2 size = new(path.ArtWidth * scale, path.ArtWidth * scale * art.height / art.width);
        Vector3 centerToTip = Vector2.Scale(path.ArtTip - Vector2.one * 0.5f, size);
        float rotation = Angle(direction) - Angle(centerToTip);
        Vector3 artworkCenter = tip - Quaternion.Euler(0f, 0f, rotation) * centerToTip;
        Draw(11, path.Art, artworkCenter, size.x, size.y, opacity, rotation, path.RevealMode, head,
            0.21f + 0.12f * Mathf.Sin(time * 34f), 24);
    }

    private void DrawBlueImpact(Vector3 center, float age, float scale)
    {
        if (age < 0f || age >= 0.30f)
            return;

        float width = scale * Mathf.Lerp(1.2f, 3.15f, Smooth(0f, 0.10f, age));
        Sprite(1, Piece.BlueFlash, center, width, Envelope(age, 0f, 0.027f, 0.055f, 0.165f),
            0f, 5, Unit(age / 0.165f), 1f);
        for (int i = 0; i < 22; i++)
        {
            float seed = Variation(i + 211);
            float local = age - 0.027f * seed;
            float life = Mathf.Lerp(0.15f, 0.27f, seed);
            if (local <= 0f || local >= life)
                continue;

            float angle = i * 2.399963f + seed * 0.32f;
            Vector3 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
            float distance = Mathf.Lerp(0.75f, 2.45f, seed) * (1f - Mathf.Exp(-local * 13f));
            Vector3 position = center + scale * (direction * distance + Vector3.down * (local * local * 6f));
            float size = scale * Mathf.Lerp(0.41f, 0.88f, seed) * (1f - 0.56f * Unit(local / life));
            Color tint = Color.Lerp(new Color(0.62f, 0.86f, 1f), new Color(0.76f, 1f, 0.97f), seed);
            tint.a = Envelope(local, 0f, 0.017f, 0.064f, life);
            blueSparks.Add(position, size, local, life, tint, angle * Mathf.Rad2Deg);
        }
    }

    private static float RoundPulse(int enemy, float time, float impact, int rounds)
    {
        float pulse = 0f;
        for (int i = 0; i < rounds; i++)
        {
            float age = time - PreviewRoundTime(enemy, impact, i);
            pulse = Mathf.Max(pulse, Envelope(age, 0f, 0.043f, 0.085f, 0.23f));
        }
        return pulse;
    }

    private void Sprite(int layer, Piece piece, Vector3 point, float width, float alpha, float rotation,
        int shaderMode = 0, float phase = 0f, float pulse = 0f, int order = -1)
    {
        var rect = Cells[(int)piece];
        Draw(layer, piece, point, width, width * rect.height / rect.width, alpha, rotation, shaderMode, phase, pulse, order);
    }

    private void Draw(int layer, Piece piece, Vector3 point, float width, float height, float alpha,
        float rotation, int shaderMode, float phase, float pulse, int order = -1)
    {
        if (alpha <= 0.001f || width <= 0.001f || height <= 0.001f)
            return;
        var renderer = layers[layer];
        if (!renderer)
        {
            renderer = Child("Attack sprite").AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = effectMaterial;
            layers[layer] = renderer;
        }
        renderer.sortingOrder = order >= 0 ? order : sortingOrder + layer;
        renderer.sortingLayerID = sortingLayer;
        int index = (int)piece;
        renderer.sprite = sprites[index];
        renderer.color = new Color(1f, 1f, 1f, alpha);
        renderer.transform.SetPositionAndRotation(point, Quaternion.Euler(0f, 0f, rotation));
        Vector3 size = renderer.sprite.bounds.size;
        renderer.transform.localScale = new Vector3(width / size.x, height / size.y, 1f);
        properties.SetVector(AtlasRectId, uvRects[index]);
        properties.SetVector(EffectId, new Vector4(shaderMode, phase, 0f, pulse));
        renderer.SetPropertyBlock(properties);
        renderer.enabled = true;
    }

    private ParticleLayer CreateSpriteParticles(string name, Piece piece, int capacity, int order)
    {
        var particles = new ParticleLayer(Child(name), effectMaterial.shader, capacity, order);
        particles.Material.SetTexture(AtlasId, effectMaterial.GetTexture(AtlasId));
        particles.Material.SetVector(AtlasRectId, uvRects[(int)piece]);
        particles.Material.SetVector(EffectId, new Vector4(0f, 0f, 1f, 0.17f));
        return particles;
    }

    private GameObject Child(string name)
    {
        var child = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
        child.transform.SetParent(root, false);
        return child;
    }

    private static float Variation(int index) => Mathf.Repeat(Mathf.Sin(index * 12.9898f + 0.41f) * 43758.54f, 1f);
    private static float Angle(Vector3 vector) => Mathf.Atan2(vector.y, vector.x) * Mathf.Rad2Deg;
    private static float Unit(float value) => Mathf.Clamp01(value);
    private static float Smooth(float from, float to, float time) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, time));
    private static float Envelope(float time, float start, float peak, float hold, float end) =>
        Smooth(start, peak, time) * (1f - Smooth(hold, end, time));

    public void Dispose()
    {
        foreach (var sprite in sprites)
            DestroyAsset(sprite);
        DestroyAsset(swingMesh);
        DestroyAsset(effectMaterial);
        smoke?.Dispose();
        rocks?.Dispose();
        meteors?.Dispose();
        blueSparks?.Dispose();
    }

    private static void DestroyAsset(UnityEngine.Object asset)
    {
        if (!asset)
            return;
        if (Application.isPlaying)
            UnityEngine.Object.Destroy(asset);
        else
            UnityEngine.Object.DestroyImmediate(asset);
    }

    private readonly struct SwingPath
    {
        public readonly Vector3 Start;
        public readonly Vector3 Control;
        public readonly Vector3 End;
        public readonly Piece Art;
        public readonly Vector2 ArtTip;
        public readonly float ArtWidth;
        public readonly int RevealMode;

        public SwingPath(Vector3 start, Vector3 control, Vector3 end, Piece art, Vector2 artTip,
            float artWidth, int revealMode)
        {
            Start = start;
            Control = control;
            End = end;
            Art = art;
            ArtTip = artTip;
            ArtWidth = artWidth;
            RevealMode = revealMode;
        }
    }

    private sealed class ParticleLayer : IDisposable
    {
        private readonly ParticleSystem system;
        private readonly ParticleSystemRenderer renderer;
        private readonly ParticleSystem.Particle[] buffer;

        public Material Material { get; }
        public int Count { get; set; }

        public ParticleLayer(GameObject root, Shader shader, int capacity, int order)
        {
            Material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            system = root.AddComponent<ParticleSystem>();
            buffer = new ParticleSystem.Particle[capacity];
            var main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = capacity;
            var emission = system.emission;
            emission.enabled = false;
            var shape = system.shape;
            shape.enabled = false;
            renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = Material;
            renderer.sortingOrder = order;
            system.Play(false);
            system.Pause(false);
        }

        public void Add(Vector3 position, float size, float age, float life, Color color, float rotation)
        {
            // Evaluate trajectories at the selected time so scrubbing is deterministic.
            buffer[Count++] = new ParticleSystem.Particle
            {
                position = position,
                startSize = size,
                startLifetime = life,
                remainingLifetime = life - age,
                startColor = color,
                rotation = rotation,
                randomSeed = (uint)Count
            };
        }

        public void Flush(int layer, int order)
        {
            if (Count > 0 && !system.isPaused)
            {
                system.Play(false);
                system.Pause(false);
            }
            system.SetParticles(buffer, Count);
            renderer.sortingLayerID = layer;
            renderer.sortingOrder = order;
            renderer.enabled = Count > 0;
        }

        public void EnableLifetimeStream()
        {
            renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream>
            {
                ParticleSystemVertexStream.Position,
                ParticleSystemVertexStream.Color,
                ParticleSystemVertexStream.UV,
                ParticleSystemVertexStream.AgePercent,
                ParticleSystemVertexStream.StableRandomX
            });
        }

        public void Dispose() => DestroyAsset(Material);
    }
}
