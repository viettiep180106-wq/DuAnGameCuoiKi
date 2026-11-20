using UnityEngine;

public sealed class WoodArmorVisual : MonoBehaviour
{
    public enum Kind { Activate, HealOnHit, EmergencyHeal }

    private const float FeetY = -3f;
    private const float LeafLifetime = 0.57f;

    [SerializeField, Min(0.01f)] private float visualScale = 1f;
    [SerializeField] private Sprite sproutSprite;
    [SerializeField] private Sprite riseSprite;
    [SerializeField] private Sprite pulseSprite;
    [SerializeField] private Sprite ringSprite;
    [SerializeField] private Sprite fadeSprite;
    [SerializeField] private Sprite leafSprite;
    [SerializeField] private Material leafMaterial;

    private SpriteRenderer sprout;
    private SpriteRenderer rise;
    private SpriteRenderer pulse;
    private SpriteRenderer ring;
    private SpriteRenderer fade;
    private ParticleSystem leaves;
    private bool playing;
    private bool leavesStarted;
    private float elapsed;
    private Kind kind;

    public float VisualScale => visualScale;
    internal bool IsConfigured => sproutSprite && riseSprite && pulseSprite && ringSprite && fadeSprite
        && leafSprite && leafMaterial;

    public static float Duration(Kind mode) => mode == Kind.HealOnHit ? 0.82f : 1.50f;

    internal void Play(Kind mode)
    {
        if (playing && Priority(mode) < Priority(kind))
            return;

        Initialize();
        kind = mode;
        elapsed = 0f;
        leavesStarted = false;
        playing = true;
        StopLeaves();
        DrawSprites(kind, elapsed, visualScale);
    }

    private static int Priority(Kind mode)
    {
        return mode == Kind.EmergencyHeal ? 2 : mode == Kind.Activate ? 1 : 0;
    }

    private void LateUpdate() => Advance(Time.deltaTime);

    private void Advance(float delta)
    {
        if (!playing)
            return;

        elapsed += delta;
        if (elapsed >= Duration(kind))
        {
            Stop();
            return;
        }

        DrawSprites(kind, elapsed, visualScale);
        if (!leavesStarted && elapsed >= LeafStart(kind))
        {
            leavesStarted = true;
            ConfigureLeaves(kind, visualScale);
            leaves.Play(false);
        }
    }

    private void OnDisable() => Stop();

    internal void Stop()
    {
        playing = false;
        if (!sprout)
            return;

        DrawSprites(Kind.Activate, -1f, visualScale);
        StopLeaves();
    }

    private void StopLeaves() => leaves.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

    private static float LeafStart(Kind mode) => mode == Kind.HealOnHit ? 0.20f : 0.74f;

    private static float GetScale(Kind mode, float scale)
    {
        return scale * (mode == Kind.HealOnHit ? 0.82f : mode == Kind.EmergencyHeal ? 1.22f : 1f);
    }

    private void ConfigureLeaves(Kind mode, float scale)
    {
        leaves.transform.localScale = Vector3.one * GetScale(mode, scale);
        var main = leaves.main;
        main.startSpeed = mode == Kind.EmergencyHeal
            ? new ParticleSystem.MinMaxCurve(7f, 14f) : new ParticleSystem.MinMaxCurve(4f, 11f);
    }

    private void Initialize()
    {
        if (sprout)
            return;

        pulse = CreateSprite("Restoration glow", pulseSprite, 19);
        sprout = CreateSprite("Sprout at feet", sproutSprite, 21);
        rise = CreateSprite("Vitality rising", riseSprite, 22);
        ring = CreateSprite("Healing ring", ringSprite, 23);
        fade = CreateSprite("Dissipating glow", fadeSprite, 24);
        leaves = CreateLeaves(leafSprite.rect.width / leafSprite.rect.height);
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

#if UNITY_EDITOR
    public void RenderPreview(Kind mode, float time, float scale)
    {
        Initialize();
        DrawSprites(mode, time, scale);
        StopLeaves();
        float leafAge = time - LeafStart(mode);
        if (leafAge < 0f || leafAge > LeafLifetime)
            return;

        ConfigureLeaves(mode, scale);
        leaves.Simulate(Mathf.Max(0.001f, leafAge), withChildren: false, restart: true, fixedTimeStep: false);
    }
#endif

    private ParticleSystem CreateLeaves(float aspect)
    {
        var root = CreateChild("Scattering leaves");
        var system = root.AddComponent<ParticleSystem>();
        system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        system.useAutoRandomSeed = false;
        system.randomSeed = 12861;
        var main = system.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.6f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.57f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(0.42f * aspect, 0.74f * aspect);
        main.startSizeY = new ParticleSystem.MinMaxCurve(0.42f, 0.74f);
        main.gravityModifier = -0.06f;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 14;
        var emission = system.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)14) });
        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 1.6f;
        var spin = system.rotationOverLifetime;
        spin.enabled = true;
        spin.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
        var color = system.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0.92f, 0f), new GradientAlphaKey(0.80f, 0.38f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;
        var renderer = root.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = leafMaterial;
        renderer.sortingOrder = 25;
        return system;
    }

    private void DrawSprites(Kind mode, float time, float scale)
    {
        bool onHit = mode == Kind.HealOnHit;
        bool emergency = mode == Kind.EmergencyHeal;
        float size = GetScale(mode, scale);
        float lift = Smooth(0.16f, 0.62f, time);
        float sproutHeight = size * Mathf.Lerp(4.2f, 9.1f, Smooth(0.00f, 0.40f, time));
        Draw(sprout, new Vector3(0, FeetY + sproutHeight * 0.48f), 8.4f * size, sproutHeight,
            onHit ? 0f : Envelope(time, 0.00f, 0.17f, 0.31f, 0.64f) * 0.85f);
        Draw(rise, new Vector3(0, FeetY + size * Mathf.Lerp(2.2f, 5.1f, lift)),
            6.8f * size, size * Mathf.Lerp(5.8f, 10.6f, lift),
            onHit ? 0f : Envelope(time, 0.15f, 0.39f, 0.47f, 0.79f) * 0.88f,
            Mathf.Sin(time * 6.7f) * 8f);

        float sincePulse = time - (onHit ? 0f : 0.46f);
        float expansion = 1f - Mathf.Pow(1f - Mathf.Clamp01(sincePulse / 0.23f), 3f);
        float pulseDiameter = size * Mathf.Lerp(4.8f, 11.7f, expansion);
        Draw(pulse, new Vector3(0, 0.28f), pulseDiameter, pulseDiameter,
            Envelope(sincePulse, 0f, 0.10f, 0.16f, 0.43f) * (emergency ? 0.98f : 0.79f));

        float sinceRing = time - (onHit ? 0.075f : 0.59f);
        float ringDiameter = size * Mathf.Lerp(7.2f, 12.7f, Smooth(0f, 0.38f, sinceRing));
        Draw(ring, new Vector3(0, 0.27f), ringDiameter, ringDiameter,
            Envelope(sinceRing, 0f, 0.17f, 0.28f, onHit ? 0.52f : 0.62f) * 0.87f,
            -36f * Mathf.Max(0f, sinceRing));
        float sinceFade = time - (onHit ? 0.29f : 0.87f);
        float drifting = Smooth(0f, 0.62f, sinceFade);
        float fadeDiameter = size * Mathf.Lerp(11.5f, 14f, drifting);
        Draw(fade, new Vector3(0, 0.27f + drifting * size), fadeDiameter, fadeDiameter,
            Envelope(sinceFade, 0f, 0.13f, 0.21f, onHit ? 0.53f : 0.63f),
            -45f * Mathf.Max(0f, sinceFade));
    }

    private static float Smooth(float start, float end, float time)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, time));
    }

    private static float Envelope(float time, float start, float peak, float hold, float end)
    {
        return Smooth(start, peak, time) * (1f - Smooth(hold, end, time));
    }

    private static void Draw(SpriteRenderer renderer, Vector3 center, float width, float height, float alpha, float rotation = 0f)
    {
        renderer.enabled = alpha > 0.001f;
        if (!renderer.enabled)
            return;

        Vector3 bounds = renderer.sprite.bounds.size;
        renderer.transform.localScale = new Vector3(width / bounds.x, height / bounds.y, 1f);
        renderer.transform.localPosition = center;
        renderer.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        renderer.color = new Color(1f, 1f, 1f, alpha);
    }
}
