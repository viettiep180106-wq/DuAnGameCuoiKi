using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class EarthSlamVisual : MonoBehaviour, IPoolable
{
    public const float FallDuration = 0.40f;
    public const float ImpactDuration = 1.22f;
    private const float ShardStart = 0.11f;
    private static readonly Vector3 FeetOffset = Vector3.down * 2.2f;

    [SerializeField, Min(0.1f)] private float boulderSize = 12f;
    [SerializeField, Min(0.1f)] private float impactWidth = 17f;
    [SerializeField, Min(0.01f)] private float particleSpread = 1f;
    [SerializeField] private Sprite meteorSprite;
    [SerializeField] private Sprite contactSprite;
    [SerializeField] private Sprite impactSprite;
    [SerializeField] private Sprite burstSprite;
    [SerializeField] private Sprite fractureSprite;
    [SerializeField] private Sprite fractureFlashSprite;
    [SerializeField] private Sprite residueSprite;
    [SerializeField] private Sprite shardSprite;
    [SerializeField] private Shader flashShader;
    [SerializeField] private Shader smokeShader;
    [SerializeField] private Material particleTemplate;

    private SpriteRenderer meteor;
    private SpriteRenderer contact;
    private SpriteRenderer impact;
    private SpriteRenderer burst;
    private SpriteRenderer fracture;
    private SpriteRenderer fractureFlash;
    private SpriteRenderer residue;
    private readonly List<LineRenderer> cracks = new();
    private readonly List<LineRenderer> crackGlow = new();
    private readonly LineRenderer[] streaks = new LineRenderer[3];
    private readonly List<Vector3> targets = new();
    private Material streakMaterial;
    private Material flashMaterial;
    private Material shardMaterial;
    private ImpactSmoke impactSmoke;
    private ParticleSystem shards;
    private ObjectPoolingManager pool;
    private bool playingImpact;
    private bool shardsStarted;
    private float fallHeight;
    private float impactTime;

    internal bool IsConfigured => meteorSprite && contactSprite && impactSprite && burstSprite && fractureSprite
        && fractureFlashSprite && residueSprite && shardSprite && flashShader && smokeShader && particleTemplate;

    internal void BeginFall(ObjectPoolingManager owner, float height)
    {
        Initialize();
        pool = owner;
        fallHeight = height;
        UpdateFall(0f);
    }

    internal void UpdateFall(float time)
    {
        DrawFall(time, transform.position + FeetOffset, fallHeight, boulderSize);
    }

    internal void MarkTarget(Vector3 position) => targets.Add(position);

    internal void BeginImpact()
    {
        EnsureCracks(targets.Count);
        playingImpact = true;
        shardsStarted = false;
        impactTime = 0f;
        UpdateFall(FallDuration);
        DrawImpactFrame();
    }

    private void LateUpdate() => AdvanceImpact(Time.deltaTime);

    private void AdvanceImpact(float delta)
    {
        if (!playingImpact)
            return;

        impactTime += delta;
        if (impactTime >= ImpactDuration)
        {
            Release();
            return;
        }

        DrawImpactFrame();
        if (!shardsStarted && impactTime >= ShardStart)
        {
            shardsStarted = true;
            ConfigureShards(transform.position + FeetOffset + Vector3.up * (impactWidth * 0.12f), particleSpread);
            shards.Play(false);
        }
    }

    private void DrawImpactFrame()
    {
        Vector3 center = transform.position + FeetOffset;
        DrawImpact(impactTime, center, boulderSize, impactWidth);
        DrawCracks(impactTime, center, targets);
        impactSmoke.Draw(center, impactTime, impactWidth, particleSpread);
    }

    private void ConfigureShards(Vector3 position, float spread)
    {
        shards.transform.position = position;
        var main = shards.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(8.5f * spread, 20f * spread);
    }

    internal void Release()
    {
        if (pool)
            pool.Despawn(gameObject);
        else
            Destroy(gameObject);
    }

    public void OnSpawn() { }

    public void OnDespawn()
    {
        playingImpact = false;
        pool = null;
        targets.Clear();
        if (!meteor)
            return;

        UpdateFall(FallDuration);
        impactTime = -1f;
        DrawImpactFrame();
        shards.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void Initialize()
    {
        if (meteor)
            return;

        streakMaterial = CreateMaterial(particleTemplate, Texture2D.whiteTexture);
        flashMaterial = new Material(flashShader) { hideFlags = HideFlags.HideAndDontSave };
        shardMaterial = CreateMaterial(particleTemplate, shardSprite.texture);
        contact = CreateSprite("Ground contact", contactSprite, 25);
        residue = CreateSprite("Settled stone", residueSprite, 26);
        meteor = CreateSprite("Falling meteor", meteorSprite, 35);
        impact = CreateSprite("Boulder contact", impactSprite, 36);
        burst = CreateSprite("Breaking boulder", burstSprite, 34);
        fracture = CreateSprite("Fractured ground", fractureSprite, 33);
        fractureFlash = CreateSprite("Expanding fracture flash", fractureFlashSprite, 38);
        fractureFlash.sharedMaterial = flashMaterial;
        for (int i = 0; i < streaks.Length; i++)
            streaks[i] = CreateLine("Meteor streak", 34);

        impactSmoke = new ImpactSmoke(this, smokeShader);
        shards = CreateShardEmitter(shardSprite);
    }

    private void EnsureCracks(int count)
    {
        while (cracks.Count < count)
        {
            cracks.Add(CreateLine("Fracture", 32));
            crackGlow.Add(CreateLine("Fracture glow", 31));
        }
    }

    private void OnDestroy()
    {
        if (!streakMaterial)
            return;

        DestroyGenerated(streakMaterial);
        DestroyGenerated(flashMaterial);
        DestroyGenerated(shardMaterial);
        impactSmoke.Dispose();
    }

    private static void DestroyGenerated(UnityEngine.Object generated)
    {
        if (Application.isPlaying)
            Destroy(generated);
        else
            DestroyImmediate(generated);
    }

#if UNITY_EDITOR
    public void RenderPreview(float time, IReadOnlyList<Vector3> previewTargets, float height,
        float rockSize, float width, float spread)
    {
        Initialize();
        EnsureCracks(previewTargets.Count);
        Vector3 center = transform.position + FeetOffset;
        float age = time - FallDuration;
        DrawFall(time, center, height, rockSize);
        DrawImpact(age, center, rockSize, width);
        DrawCracks(age, center, previewTargets);
        impactSmoke.Draw(center, age, width, spread);
        ScrubParticles(shards, center + Vector3.up * (width * 0.12f), age - ShardStart, spread, 8.5f, 20f);
    }
#endif

    private static Material CreateMaterial(Material template, Texture texture)
    {
        var result = new Material(template) { hideFlags = HideFlags.HideAndDontSave };
        result.SetTexture("_BaseMap", texture);
        return result;
    }

    private GameObject CreateObject(string name)
    {
        var root = new GameObject(name);
        root.transform.SetParent(transform, false);
        return root;
    }

    private SpriteRenderer CreateSprite(string name, Sprite sprite, int order)
    {
        var renderer = CreateObject(name).AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        renderer.enabled = false;
        return renderer;
    }

    private LineRenderer CreateLine(string name, int order)
    {
        var line = CreateObject(name).AddComponent<LineRenderer>();
        line.sharedMaterial = streakMaterial;
        line.sortingOrder = order;
        line.numCornerVertices = 2;
        line.numCapVertices = 2;
        line.useWorldSpace = true;
        line.enabled = false;
        return line;
    }

    private ParticleSystem CreateShardEmitter(Sprite sprite)
    {
        var root = CreateObject("Flying rock");
        var system = root.AddComponent<ParticleSystem>();
        system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        system.useAutoRandomSeed = false;
        system.randomSeed = 3001u;
        var main = system.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.72f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.31f, 0.66f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-2.4f, 2.4f);
        main.gravityModifier = 1.65f;
        main.startSize3D = true;
        float aspect = sprite.rect.width / sprite.rect.height;
        const float size = 1.05f;
        main.startSizeX = new ParticleSystem.MinMaxCurve(size * aspect * 0.55f, size * aspect * 1.32f);
        main.startSizeY = new ParticleSystem.MinMaxCurve(size * 0.55f, size * 1.32f);
        main.maxParticles = 16;
        var emission = system.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)16) });
        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.arc = 180f;
        shape.radius = 0.9f;
        var rotation = system.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
        var fade = system.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.75f, 0.26f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;
        var renderer = root.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = shardMaterial;
        renderer.sortingOrder = 37;
        return system;
    }

    private void DrawFall(float time, Vector3 center, float height, float rockSize)
    {
        float progress = Mathf.Clamp01(time / FallDuration);
        float fall = progress <= 0f ? 0f : Mathf.Pow(2f, 10f * (progress - 1f));
        bool falling = time < FallDuration;
        Vector3 point = center + Vector3.up * (height * (1f - fall) + rockSize * 0.43f);
        Place(meteor, point, rockSize, rockSize, falling ? 1f : 0f);
        meteor.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-14f, 4f, progress));
        for (int i = 0; i < streaks.Length; i++)
        {
            var line = streaks[i];
            line.enabled = falling && time > 0.025f;
            if (!line.enabled)
                continue;
            float x = (i - 1) * rockSize * 0.23f;
            Vector3 start = point + new Vector3(x, rockSize * 0.21f);
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, start + new Vector3(x * 0.24f, rockSize * Mathf.Lerp(0.3f, 1.5f, progress)));
            line.startWidth = rockSize * (i == 1 ? 0.11f : 0.058f);
            line.endWidth = 0f;
            line.startColor = new Color(1f, 0.53f, 0.23f, 0.82f);
            line.endColor = new Color(0.49f, 0.19f, 0.08f, 0f);
        }
    }

    private void DrawImpact(float age, Vector3 ground, float rockSize, float width)
    {
        float groundWidth = width * Mathf.Lerp(0.40f, 1.16f, Ease(0f, 0.21f, age));
        Place(contact, ground, groundWidth, groundWidth * 0.20f, Envelope(age, 0f, 0.038f, 0.13f, 0.33f));
        float flashWidth = Mathf.Max(rockSize * 1.78f, width * 0.89f);
        AtGround(impact, ground, flashWidth, flashWidth * 0.72f, Envelope(age, 0f, 0.025f, 0.073f, 0.21f));
        float breakSpread = Ease(0.10f, 0.36f, age);
        AtGround(burst, ground, width * Mathf.Lerp(0.79f, 1.16f, breakSpread),
            width * Mathf.Lerp(0.67f, 0.71f, breakSpread), Envelope(age, 0.09f, 0.18f, 0.25f, 0.47f));
        float fractureSpread = Ease(0.27f, 0.60f, age);
        AtGround(fracture, ground, width * Mathf.Lerp(0.84f, 1.25f, fractureSpread),
            width * 0.39f, Envelope(age, 0.26f, 0.41f, 0.56f, 0.87f));
        float flashAge = age - 0.28f;
        float flashExpansion = 1f - Mathf.Pow(1f - Mathf.Clamp01(flashAge / 0.29f), 3f);
        float shockWidth = width * Mathf.Lerp(0.28f, 1.70f, flashExpansion);
        Place(fractureFlash, ground + Vector3.up * 0.15f, shockWidth, shockWidth * 0.29f,
            Envelope(flashAge, 0f, 0.042f, 0.10f, 0.32f) * 0.93f);
        AtGround(residue, ground, width * 1.10f, width * 0.41f, Envelope(age, 0.54f, 0.74f, 0.86f, 1.18f));
    }

    private void DrawCracks(float age, Vector3 center, IReadOnlyList<Vector3> targets)
    {
        float reach = Ease(0.025f, 0.33f, age);
        float opacity = Envelope(age, 0.015f, 0.12f, 0.41f, 0.92f);
        for (int i = 0; i < cracks.Count; i++)
        {
            var line = cracks[i];
            var glow = crackGlow[i];
            line.enabled = glow.enabled = opacity > 0.001f && i < targets.Count;
            if (!line.enabled)
                continue;
            Vector3 delta = targets[i] + FeetOffset - center;
            delta.z = 0f;
            Vector3 tangent = new Vector3(-delta.y, delta.x).normalized;
            line.positionCount = glow.positionCount = 6;
            for (int p = 0; p < 6; p++)
            {
                float progress = reach * p / 5f;
                float bend = Mathf.Sin(progress * Mathf.PI) * Mathf.Sin((i + 1) * 3f + progress * 19f) * 0.52f;
                Vector3 point = center + delta * progress + tangent * bend;
                line.SetPosition(p, point);
                glow.SetPosition(p, point);
            }
            line.startWidth = 0.30f;
            line.endWidth = 0.12f;
            line.startColor = new Color(1f, 0.80f, 0.36f, opacity);
            line.endColor = new Color(1f, 0.43f, 0.12f, opacity);
            glow.startWidth = 0.72f;
            glow.endWidth = 0.28f;
            glow.startColor = new Color(1f, 0.37f, 0.08f, opacity * 0.34f);
            glow.endColor = new Color(0.88f, 0.25f, 0.07f, opacity * 0.25f);
        }
    }

#if UNITY_EDITOR
    private static void ScrubParticles(ParticleSystem system, Vector3 position, float age, float spread,
        float minSpeed, float maxSpeed)
    {
        system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (age < 0f || age >= 0.72f)
            return;
        system.transform.position = position;
        var main = system.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed * spread, maxSpeed * spread);
        system.Simulate(Mathf.Max(0.001f, age), withChildren: false, restart: true, fixedTimeStep: false);
    }

#endif

    private static float Ease(float from, float to, float age) =>
        Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, age));

    private static float Envelope(float age, float start, float peak, float hold, float end) =>
        Ease(start, peak, age) * (1f - Ease(hold, end, age));

    private static void AtGround(SpriteRenderer sprite, Vector3 ground, float width, float height, float alpha) =>
        Place(sprite, ground + Vector3.up * (height * 0.44f), width, height, alpha);

    private static void Place(SpriteRenderer sprite, Vector3 position, float width, float height, float alpha)
    {
        sprite.enabled = alpha > 0.001f;
        if (!sprite.enabled)
            return;
        Vector3 size = sprite.sprite.bounds.size;
        sprite.transform.localScale = new Vector3(width / size.x, height / size.y, 1f);
        sprite.transform.position = position;
        sprite.color = new Color(1f, 1f, 1f, alpha);
    }

    private sealed class ImpactSmoke : IDisposable
    {
        private static readonly int MotionId = Shader.PropertyToID("_SmokeMotion");
        private readonly Material material;
        private readonly Sprite quad;
        private readonly Puff[] puffs = new Puff[20];
        private readonly MaterialPropertyBlock properties = new();

        public ImpactSmoke(EarthSlamVisual visual, Shader shader)
        {
            var texture = Texture2D.whiteTexture;
            quad = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
            quad.hideFlags = HideFlags.HideAndDontSave;
            material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            var random = new System.Random(65237);
            for (int i = 0; i < puffs.Length; i++)
            {
                bool lifts = i >= 10;
                int side = i % 2 == 0 ? -1 : 1;
                var renderer = visual.CreateSprite("Impact smoke puff", quad, i % 5 < 2 ? 35 : 29);
                renderer.sharedMaterial = material;
                puffs[i] = new Puff(renderer, lifts, side, Range(random, 0f, 60f),
                    Range(random, 0.035f, 0.12f) + (lifts ? 0.17f : 0f),
                    Range(random, 0.62f, 0.93f), Range(random, 0.66f, 1.22f),
                    Range(random, 0.72f, 1.27f));
            }
        }

        public void Draw(Vector3 center, float time, float width, float spread)
        {
            foreach (Puff puff in puffs)
            {
                float age = (time - puff.Start) / puff.Lifetime;
                if (age <= 0f || age >= 1f)
                {
                    puff.Renderer.enabled = false;
                    continue;
                }

                float travel = (1f - Mathf.Exp(-3.1f * age)) * puff.Speed;
                float outward = width * spread * (puff.Lifts ? 0.26f : 0.45f) * travel;
                float lift = width * (puff.Lifts ? 0.29f : 0.085f) * Mathf.Pow(age, 0.82f) * puff.Speed;
                float diameter = width * Mathf.Lerp(0.15f, puff.Lifts ? 0.46f : 0.56f, Mathf.Sqrt(age)) * puff.Size;
                float alpha = Envelope(age, 0f, 0.19f, 0.35f, 1f) * (puff.Lifts ? 0.40f : 0.49f);
                Vector3 position = center + new Vector3(puff.Side * outward, lift + diameter * 0.12f);
                Place(puff.Renderer, position, diameter, diameter * (puff.Lifts ? 0.82f : 0.49f), alpha);
                Color tint = Color.Lerp(new Color(0.66f, 0.48f, 0.32f), new Color(0.51f, 0.44f, 0.39f), age);
                tint.a = alpha;
                puff.Renderer.color = tint;
                properties.SetVector(MotionId, new Vector4(age, puff.Seed, puff.Side, 0f));
                puff.Renderer.SetPropertyBlock(properties);
            }
        }

        private static float Range(System.Random random, float min, float max) =>
            Mathf.Lerp(min, max, (float)random.NextDouble());

        public void Dispose()
        {
            DestroyGenerated(material);
            DestroyGenerated(quad);
        }

        private readonly struct Puff
        {
            public readonly SpriteRenderer Renderer;
            public readonly bool Lifts;
            public readonly int Side;
            public readonly float Seed;
            public readonly float Start;
            public readonly float Lifetime;
            public readonly float Speed;
            public readonly float Size;

            public Puff(SpriteRenderer renderer, bool lifts, int side, float seed, float start,
                float lifetime, float speed, float size)
            {
                Renderer = renderer;
                Lifts = lifts;
                Side = side;
                Seed = seed;
                Start = start;
                Lifetime = lifetime;
                Speed = speed;
                Size = size;
            }
        }
    }
}
