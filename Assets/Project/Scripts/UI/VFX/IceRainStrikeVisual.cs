using System;
using UnityEngine;

public sealed class IceRainStrikeVisual : MonoBehaviour, IPoolable
{
    public const float CloudLead = 0.40f;
    public const float FallDuration = 0.20f;
    public const float ImpactTime = CloudLead + FallDuration;
    public const float ImpactDuration = 0.72f;

    private const float FallHeight = 22f;
    private const float CloudTrail = 0.085f;
    private const float ShatterStart = 0.055f;
    private const float MistStart = 0.19f;

    [SerializeField, Min(1f)] private float boltLength = 17f;
    [SerializeField, Min(1f)] private float impactWidth = 11f;
    [SerializeField, Min(0.01f)] private float shardSpread = 1f;
    [SerializeField] private Sprite cloudSprite;
    [SerializeField] private Sprite vortexSprite;
    [SerializeField] private Sprite trailSprite;
    [SerializeField] private Sprite boltSprite;
    [SerializeField] private Sprite impactSprite;
    [SerializeField] private Sprite mistSprite;
    [SerializeField] private Sprite firstShardSprite;
    [SerializeField] private Sprite secondShardSprite;
    [SerializeField] private Material firstShardMaterial;
    [SerializeField] private Material secondShardMaterial;

    private SpriteRenderer cloud;
    private SpriteRenderer swirl;
    private SpriteRenderer leftWisps;
    private SpriteRenderer rightWisps;
    private SpriteRenderer trail;
    private SpriteRenderer tip;
    private SpriteRenderer impact;
    private SpriteRenderer mist;
    private ShardEmitter firstShards;
    private ShardEmitter secondShards;
    private ObjectPoolingManager pool;
    private Func<bool> onImpact;
    private bool playing;
    private bool shardsStarted;
    private float elapsed;

    internal bool IsConfigured => cloudSprite && vortexSprite && trailSprite && boltSprite && impactSprite
        && mistSprite && firstShardSprite && secondShardSprite && firstShardMaterial && secondShardMaterial;

    internal void Play(ObjectPoolingManager owner, Func<bool> impactCallback)
    {
        Initialize();
        pool = owner;
        onImpact = impactCallback;
        elapsed = 0f;
        shardsStarted = false;
        playing = true;
        DrawSprites(0f, boltLength, impactWidth);
    }

    private void LateUpdate() => Advance(Time.deltaTime);

    private void Advance(float deltaTime)
    {
        if (!playing)
            return;

        elapsed += deltaTime;
        if (onImpact != null && elapsed >= ImpactTime)
        {
            var callback = onImpact;
            onImpact = null;
            if (!callback())
            {
                Release();
                return;
            }
        }

        if (elapsed >= ImpactTime + ImpactDuration)
        {
            Release();
            return;
        }

        DrawSprites(elapsed, boltLength, impactWidth);
        if (!shardsStarted && elapsed >= ImpactTime + ShatterStart)
        {
            shardsStarted = true;
            firstShards.Play(impactWidth, shardSpread);
            secondShards.Play(impactWidth, shardSpread);
        }
    }

    private void Release()
    {
        playing = false;
        if (pool)
            pool.Despawn(gameObject);
        else
            Destroy(gameObject);
    }

    public void OnSpawn() { }

    public void OnDespawn()
    {
        playing = false;
        onImpact = null;
        pool = null;
        if (!cloud)
            return;

        DrawSprites(-1f, boltLength, impactWidth);
        firstShards.Stop();
        secondShards.Stop();
    }

    private void Initialize()
    {
        if (cloud)
            return;

        cloud = CreateSprite("Ice cloud", cloudSprite, 56);
        swirl = CreateSprite("Cold air swirl", vortexSprite, 55);
        leftWisps = CreateSprite("Left gathering mist", mistSprite, 55);
        rightWisps = CreateSprite("Right gathering mist", mistSprite, 55);
        rightWisps.flipX = true;
        leftWisps.transform.localRotation = Quaternion.Euler(0f, 0f, 24f);
        rightWisps.transform.localRotation = Quaternion.Euler(0f, 0f, -24f);
        trail = CreateSprite("Falling trail", trailSprite, 51);
        tip = CreateSprite("Ice bolt tip", boltSprite, 52);
        impact = CreateSprite("Ice contact", impactSprite, 53);
        mist = CreateSprite("Ice mist", mistSprite, 50);
        uint seed = unchecked((uint)GetInstanceID());
        firstShards = new ShardEmitter(CreateChild("Ice shards A"), firstShardMaterial, firstShardSprite, seed);
        secondShards = new ShardEmitter(CreateChild("Ice shards B"), secondShardMaterial, secondShardSprite, seed ^ 7919u);
    }

    private GameObject CreateChild(string childName)
    {
        var child = new GameObject(childName);
        child.transform.SetParent(transform, false);
        return child;
    }

    private SpriteRenderer CreateSprite(string spriteName, Sprite art, int order)
    {
        var renderer = CreateChild(spriteName).AddComponent<SpriteRenderer>();
        renderer.sprite = art;
        renderer.sortingOrder = order;
        return renderer;
    }

    private static void Place(SpriteRenderer renderer, Vector3 center, float width, float height, float alpha)
    {
        renderer.enabled = alpha > 0.001f;
        if (!renderer.enabled)
            return;

        Vector3 size = renderer.sprite.bounds.size;
        renderer.transform.localScale = new Vector3(width / size.x, height / size.y, 1f);
        renderer.transform.localPosition = center;
        renderer.color = new Color(1f, 1f, 1f, alpha);
    }

#if UNITY_EDITOR
    public void RenderPreview(float age, float length, float width, float spread)
    {
        Initialize();
        DrawSprites(age, length, width);
        float shardAge = age - ImpactTime - ShatterStart;
        firstShards.Scrub(shardAge, width, spread);
        secondShards.Scrub(shardAge, width, spread);
    }
#endif

    private void DrawSprites(float age, float length, float width)
    {
        Vector3 sky = Vector3.up * FallHeight;
        DrawCloud(sky, age, width);
        float fallAge = age - CloudLead;
        float fall = Mathf.Clamp01(fallAge / FallDuration);
        float sinceImpact = age - ImpactTime;
        bool falling = fallAge >= 0f && sinceImpact < 0f;
        Vector3 point = sky * (1f - fall * fall);
        float visibleTrail = Mathf.Lerp(length * 0.20f, length, fall);
        Place(trail, point + Vector3.up * (visibleTrail * 0.53f), length * 0.16f,
            visibleTrail, falling ? 0.85f : 0f);
        Place(tip, point + Vector3.up * (length * 0.14f), length * 0.18f,
            length * 0.34f, falling ? 1f : 0f);

        float expansion = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(sinceImpact / 0.12f));
        float contactWidth = width * Mathf.Lerp(0.28f, 1f, expansion);
        float contactHeight = contactWidth * 0.86f;
        float impactAlpha = sinceImpact < 0f ? 0f : 1f - Mathf.InverseLerp(0.11f, 0.32f, sinceImpact);
        Place(impact, Vector3.up * (contactHeight * 0.35f),
            contactWidth, contactHeight, impactAlpha);
        float mistAlpha = Mathf.InverseLerp(MistStart, 0.31f, sinceImpact)
            * (1f - Mathf.InverseLerp(0.38f, ImpactDuration, sinceImpact));
        float drift = Mathf.InverseLerp(MistStart, ImpactDuration, sinceImpact);
        Place(mist, Vector3.up * (width * (0.20f + 0.16f * drift)),
            width * Mathf.Lerp(0.74f, 1.25f, drift), width * 0.69f, mistAlpha * 0.74f);
    }

    private void DrawCloud(Vector3 sky, float age, float width)
    {
        float progress = age / CloudLead;
        float forming = Mathf.SmoothStep(0f, 1f, progress);
        float release = 1f - Mathf.InverseLerp(CloudLead, CloudLead + CloudTrail, age);
        float bodyWidth = Mathf.Lerp(0.32f, 0.88f, forming) * width;
        float cloudAlpha = Mathf.InverseLerp(0.14f, 0.73f, progress) * release;
        Place(cloud, sky + Vector3.up * (bodyWidth * 0.215f),
            bodyWidth, bodyWidth * 0.37f, cloudAlpha);

        float gather = Mathf.InverseLerp(0f, 0.27f, progress)
            * (1f - Mathf.InverseLerp(0.73f * CloudLead, CloudLead + 0.025f, age));
        float offset = Mathf.Lerp(width * 0.69f, width * 0.07f, forming);
        float lift = Mathf.Lerp(-width * 0.31f, 0f, forming);
        Vector3 mistCenter = sky + Vector3.up * lift;
        Place(leftWisps, mistCenter - Vector3.right * offset, width * 0.64f,
            width * 0.33f, gather * 0.78f);
        Place(rightWisps, mistCenter + Vector3.right * offset, width * 0.58f,
            width * 0.31f, gather * 0.69f);
        Place(swirl, sky + Vector3.up * (width * 0.075f),
            width * Mathf.Lerp(0.87f, 0.30f, forming), width * Mathf.Lerp(0.52f, 0.27f, forming),
            gather * 0.70f);
        swirl.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-24f, 15f, forming));
    }

    private sealed class ShardEmitter
    {
        private readonly ParticleSystem particles;
        private readonly Sprite art;

        public ShardEmitter(GameObject root, Material material, Sprite art, uint seed)
        {
            this.art = art;
            particles = root.AddComponent<ParticleSystem>();
            particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.useAutoRandomSeed = false;
            particles.randomSeed = seed | 1u;
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.6f;
            main.maxParticles = 6;
            main.startSize3D = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.30f, 0.57f);
            main.startRotation = new ParticleSystem.MinMaxCurve(-1.4f, 1.4f);
            main.gravityModifier = 0.85f;
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)6) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.arc = 180f;
            var rotation = particles.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-7f, 7f);
            var fade = particles.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.36f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 54;
            renderer.maxParticleSize = 0.5f;
        }

        private void Configure(float width, float spread)
        {
            particles.transform.localPosition = Vector3.up * (width * 0.11f);
            var shape = particles.shape;
            shape.radius = width * 0.055f;
            var main = particles.main;
            float aspect = art.rect.width / art.rect.height;
            main.startSizeX = new ParticleSystem.MinMaxCurve(width * aspect * 0.07f, width * aspect * 0.25f);
            main.startSizeY = new ParticleSystem.MinMaxCurve(width * 0.07f, width * 0.25f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(width * spread * 0.7f, width * spread * 1.9f);
        }

        public void Play(float width, float spread)
        {
            Configure(width, spread);
            particles.Play(false);
        }

        public void Stop() => particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

#if UNITY_EDITOR
        public void Scrub(float age, float width, float spread)
        {
            Stop();
            if (age < 0f || age >= 0.58f)
                return;

            Configure(width, spread);
            particles.Simulate(Mathf.Max(0.001f, age), withChildren: false, restart: true, fixedTimeStep: false);
        }
#endif
    }
}
