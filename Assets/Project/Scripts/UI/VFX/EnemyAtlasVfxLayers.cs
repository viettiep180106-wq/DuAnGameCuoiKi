using System;
using UnityEngine;

internal sealed class EnemyAtlasVfxLayers : IDisposable
{
    internal static readonly Color Water = new(.35f, .87f, 1f);
    internal static readonly Color Foam = new(.73f, .98f, .98f);
    internal static readonly Color Ice = new(.54f, .82f, 1f);
    internal static readonly Color Mud = new(.47f, .59f, .27f);
    internal static readonly Color Heal = new(.52f, .96f, .63f);
    private static readonly int AtlasId = Shader.PropertyToID("_Atlas");
    private static readonly int RectId = Shader.PropertyToID("_WaterRect");
    private static readonly int EffectId = Shader.PropertyToID("_WaterFx");
    private static readonly int JetId = Shader.PropertyToID("_WaterJet");

    private readonly Transform root;
    private readonly EnemyAtlasVfxArt art;
    private readonly Material spriteMaterial;
    private readonly MaterialPropertyBlock properties = new();
    private SpriteRenderer[] sprites = new SpriteRenderer[16];
    private ParticleSystem.Particle[] particles = new ParticleSystem.Particle[96];
    private ParticleSystem system;
    private ParticleSystemRenderer particleRenderer;
    private Material particleMaterial;
    private int spriteCount;
    private int particleCount;
    private int sortingLayer;
    private int order;
    private int groundOrder;

    internal EnemyAtlasVfxLayers(Transform root, EnemyAtlasVfxArt art)
    {
        this.root = root;
        this.art = art;
        spriteMaterial = new Material(art.Shader) { hideFlags = HideFlags.HideAndDontSave };
    }

    internal void SetSorting(int layer, int foreground, int ground)
    {
        sortingLayer = layer;
        order = foreground;
        groundOrder = ground;
    }

    internal void BeginFrame()
    {
        spriteCount = 0;
        particleCount = 0;
    }

    internal void EnsureCapacity(int spriteCapacity, int particleCapacity)
    {
        if (spriteCapacity > sprites.Length)
            Array.Resize(ref sprites, spriteCapacity);
        if (particleCapacity <= particles.Length) return;
        Array.Resize(ref particles, particleCapacity);
        if (system)
        {
            var main = system.main;
            main.maxParticles = particles.Length;
        }
    }

    internal void Draw(int cell, Vector3 point, float width, float opacity, float time = 0f, float height = 0f,
        float angle = 0f, float warp = 0f, float reveal = 1f, bool radial = false, bool ground = false, bool flip = false,
        bool rise = false)
    {
        if (opacity <= .004f || width < .02f || spriteCount >= sprites.Length) return;
        float mode = rise ? 8f : radial ? 3f : reveal < .999f ? 2f : warp > 0f ? 1f : 0f;
        properties.Clear();
        properties.SetTexture(AtlasId, art.Atlas(cell));
        properties.SetVector(RectId, art.Uv(cell));
        properties.SetVector(EffectId, new Vector4(mode, reveal, time, warp));
        TransformSprite(point, width, height > 0f ? height : width * art.Aspect(cell), angle,
            new Color(1f, 1f, 1f, opacity), ground, flip);
    }

    internal void Ring(Vector3 point, float age, float end, float diameter, float flatten, Color tint, bool ground = true)
    {
        if (age < 0f || age >= end || spriteCount >= sprites.Length) return;
        float progress = Mathf.Clamp01(age / end);
        float swell = Mathf.Lerp(.23f, 1f, 1f - (1f - progress) * (1f - progress));
        tint.a = Mathf.Min(1f, progress * 12f) * (1f - progress) * .85f;
        properties.Clear();
        properties.SetVector(EffectId, new Vector4(4f, 1f, 0f, 0f));
        TransformSprite(point, diameter * swell, diameter * swell * flatten, 0f, tint, ground, false);
    }

    internal void Beam(int cell, Vector3 from, Vector3 to, float thickness, float opacity, float time, float warp)
    {
        if (opacity <= .004f || spriteCount >= sprites.Length) return;
        Vector3 delta = to - from;
        properties.Clear();
        properties.SetTexture(AtlasId, art.Atlas(cell));
        properties.SetVector(RectId, art.Uv(cell));
        properties.SetVector(EffectId, new Vector4(7f, art.Aspect(cell), time, warp));
        properties.SetVector(JetId, cell == 22 ? new Vector4(.08f, .10f, .89f, .80f) : new Vector4(.055f, .12f, .91f, .88f));
        TransformSprite(from + delta * .5f, delta.magnitude / .86f, thickness, Angle(delta),
            new Color(1f, 1f, 1f, opacity), false, false);
    }

    internal void Talisman(Vector3 center, Vector3 direction, float scale, float opacity, float time, float reach)
    {
        if (opacity <= .004f || spriteCount + 2 > sprites.Length) return;
        float width = scale * Mathf.Lerp(4.8f, 8.4f, reach);
        properties.Clear();
        properties.SetVector(EffectId, new Vector4(9f, 1f, time, 0f));
        TransformSprite(center - direction * width * .22f, width, scale * 3.5f, Angle(direction),
            new Color(1f, 1f, 1f, opacity), false, false);

        const int paper = EnemyWoodVfxArt.TalismanPaper;
        properties.Clear();
        properties.SetTexture(AtlasId, art.Atlas(paper));
        properties.SetVector(RectId, art.Uv(paper));
        properties.SetVector(EffectId, new Vector4(10f, 1f, 0f, 0f));
        TransformSprite(center, scale * 2.68f, scale * 2.43f, 0f,
            new Color(1f, 1f, 1f, opacity), false, false);
    }

    internal void DrawEffect(int cell, Vector3 center, float width, float height, Vector4 effect, Color tint,
        float angle = 0f, bool ground = false, bool flip = false)
    {
        if (tint.a <= .004f || width < .02f || height < .02f || spriteCount >= sprites.Length) return;
        properties.Clear();
        properties.SetTexture(AtlasId, art.Atlas(cell));
        properties.SetVector(RectId, art.Uv(cell));
        properties.SetVector(EffectId, effect);
        TransformSprite(center, width, height, angle, tint, ground, flip);
    }

    private void TransformSprite(Vector3 point, float width, float height, float angle, Color tint, bool ground, bool flip)
    {
        int index = spriteCount++;
        var renderer = sprites[index];
        if (!renderer)
        {
            renderer = Child("Atlas Sprite").AddComponent<SpriteRenderer>();
            renderer.sprite = art.Quad;
            renderer.sharedMaterial = spriteMaterial;
            sprites[index] = renderer;
        }
        renderer.sortingLayerID = sortingLayer;
        renderer.sortingOrder = ground ? groundOrder : order + index;
        renderer.color = tint;
        renderer.transform.SetPositionAndRotation(point, Quaternion.Euler(0f, 0f, angle));
        renderer.transform.localScale = new Vector3(flip ? -width : width, height, 1f);
        renderer.SetPropertyBlock(properties);
        renderer.enabled = true;
    }

    internal void Burst(Vector3 origin, float age, int count, float speed, float size, Color tint,
        float life = .40f, float heading = 90f, float spread = 145f, float gravity = 40f)
    {
        if (age < 0f || age >= life) return;
        for (int i = 0; i < count; i++)
        {
            float seed = Seed(i + 9);
            float t = age / Mathf.Lerp(life * .42f, life, Seed(i + 21));
            if (t >= 1f) continue;
            float angle = heading + (Seed(i + 72) - .5f) * spread;
            Vector3 direction = Direction(angle);
            Vector3 point = origin + direction * (speed * Mathf.Lerp(.28f, 1f, seed) * age)
                - Vector3.up * (.5f * gravity * age * age);
            Particle(point, size * Mathf.Lerp(.32f, 1f, seed) * (1f - t * .65f), tint,
                Mathf.Min(1f, t * 10f) * (1f - t), angle + seed * 123f * age);
        }
    }

    internal void Orbit(Vector3 center, float time, float radiusX, float radiusY, Color tint, float alpha, int count = 13)
    {
        if (alpha <= .004f) return;
        for (int i = 0; i < count; i++)
        {
            float arc = time * 11f + i * .65f;
            var position = center + new Vector3(Mathf.Cos(arc) * radiusX, Mathf.Sin(arc) * radiusY);
            Particle(position, .22f + Seed(i) * .23f, tint, alpha * Mathf.Lerp(.2f, 1f, Seed(i + 7)));
        }
    }

    internal void Particle(Vector3 position, float size, Color tint, float alpha, float rotation = 0f)
    {
        if (alpha <= .004f || size <= .02f || particleCount >= particles.Length) return;
        tint.a = Mathf.Clamp01(alpha);
        particles[particleCount++] = new ParticleSystem.Particle
        {
            position = position, startSize = size, startColor = tint, rotation = rotation,
            startLifetime = 1f, remainingLifetime = 1f
        };
    }

    internal void EndFrame(bool crystals)
    {
        for (int i = spriteCount; i < sprites.Length; i++)
            if (sprites[i]) sprites[i].enabled = false;
        if (particleCount > 0 && !system) PrepareParticles();
        if (!system) return;
        if (particleCount > 0 && !system.isPaused)
        {
            system.Play(false);
            system.Pause(false);
        }
        system.SetParticles(particles, particleCount);
        particleRenderer.enabled = particleCount > 0;
        particleRenderer.sortingLayerID = sortingLayer;
        particleRenderer.sortingOrder = order + 7;
        particleMaterial.SetVector(EffectId, new Vector4(crystals ? 6f : 5f, 1f, 0f, 0f));
    }

    private void PrepareParticles()
    {
        var child = Child("Atlas Particles");
        system = child.AddComponent<ParticleSystem>();
        var main = system.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = particles.Length;
        var emission = system.emission;
        emission.enabled = false;
        var shape = system.shape;
        shape.enabled = false;
        particleRenderer = child.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleMaterial = new Material(art.Shader) { hideFlags = HideFlags.HideAndDontSave };
        particleRenderer.sharedMaterial = particleMaterial;
    }

    private GameObject Child(string name)
    {
        var child = new GameObject(name) { hideFlags = HideFlags.DontSave };
        child.transform.SetParent(root, false);
        return child;
    }

    internal static float Seed(int index) => Mathf.Repeat(Mathf.Sin(index * 127.1f + 17.9f) * 43758.54f, 1f);
    internal static Vector3 Direction(float degrees) => Quaternion.Euler(0f, 0f, degrees) * Vector3.right;
    internal static float Angle(Vector3 direction) => Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

    public void Dispose()
    {
        UnityEngine.Object.Destroy(spriteMaterial);
        if (particleMaterial) UnityEngine.Object.Destroy(particleMaterial);
    }
}
