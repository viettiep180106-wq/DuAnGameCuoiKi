using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class FireBombBurst : MonoBehaviour, IPoolable
{
    [SerializeField] private Sprite flashSprite;
    [SerializeField] private Sprite burstSprite;
    [SerializeField] private Sprite smokeSprite;
    [SerializeField] private ParticleSystem sparks;
    [SerializeField] private ParticleSystem smoke;
    [SerializeField, Min(0.01f)] private float visualScale = 1f;

    private const float BurstStart = 0.065f;
    private const float SmokeStart = 0.22f;
    private const float End = 0.72f;
    private SpriteRenderer spriteRenderer;
    private ObjectPoolingManager pool;
    private float elapsed;
    private bool playing;

    private void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

    internal void Play(float splashRadius, ObjectPoolingManager owner)
    {
        pool = owner;
        transform.localScale = Vector3.one * (Mathf.Max(0.1f, splashRadius) / 1.7f * visualScale);
        elapsed = 0f;
        spriteRenderer.sprite = flashSprite;
        spriteRenderer.color = Color.white;
        spriteRenderer.enabled = true;
        playing = true;
    }

    private void Update() => Advance(Time.deltaTime);

    private void Advance(float deltaTime)
    {
        if (!playing)
            return;

        float previous = elapsed;
        elapsed += deltaTime;
        if (previous < BurstStart && elapsed >= BurstStart)
        {
            spriteRenderer.sprite = burstSprite;
            sparks.Play();
            smoke.Play();
        }

        if (elapsed >= SmokeStart)
        {
            spriteRenderer.sprite = smokeSprite;
            spriteRenderer.color = new Color(1f, 1f, 1f, 1f - Mathf.InverseLerp(SmokeStart, 0.57f, elapsed));
        }

        if (elapsed >= End)
        {
            playing = false;
            if (pool)
                pool.Despawn(gameObject);
            else
                Destroy(gameObject);
        }
    }

    public void OnSpawn() { }

    public void OnDespawn()
    {
        playing = false;
        pool = null;
        spriteRenderer.enabled = false;
        sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
