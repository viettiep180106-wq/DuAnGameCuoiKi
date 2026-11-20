using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class ShieldSpriteVisual : MonoBehaviour
{
    private enum State { Cast, Idle, Charge, Impact, Shatter, Hidden }

    private static readonly int StateId = Shader.PropertyToID("_State");
    private static readonly int ProgressId = Shader.PropertyToID("_Progress");
    private static readonly int ElapsedId = Shader.PropertyToID("_Elapsed");

    [SerializeField] private Sprite castSprite;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite chargeSprite;
    [SerializeField] private Sprite impactSprite;
    [SerializeField] private Sprite shatterSprite;

    private Player owner;
    private SpriteRenderer shieldRenderer;
    private MaterialPropertyBlock properties;
    private State state = State.Hidden;
    private bool breakAfterImpact;
    private float stateTime;
    private float elapsed;

    internal bool IsReady => isActiveAndEnabled && shieldRenderer && shieldRenderer.sharedMaterial
        && castSprite && idleSprite && chargeSprite && impactSprite && shatterSprite;

    internal bool IsProtecting => state != State.Hidden && state != State.Shatter && !breakAfterImpact;

    private void Awake()
    {
        owner = GetComponentInParent<Player>();
        shieldRenderer = GetComponent<SpriteRenderer>();
        properties = new MaterialPropertyBlock();
        shieldRenderer.enabled = false;
    }

    private void OnEnable()
    {
        if (owner)
            owner.OnShieldChanged += OnShieldChanged;
    }

    private void OnDisable()
    {
        if (owner)
            owner.OnShieldChanged -= OnShieldChanged;
        Hide();
    }

    internal void PlayGrant()
    {
        if (!IsProtecting)
        {
            breakAfterImpact = false;
            elapsed = 0f;
            Show(State.Cast, castSprite);
        }
        else if (state != State.Cast)
        {
            Show(State.Charge, chargeSprite);
        }
    }

    internal void PlayImpact(bool broken)
    {
        breakAfterImpact = broken;
        Show(State.Impact, impactSprite);
    }

    private void OnShieldChanged(float amount)
    {
        if (amount <= 0f)
            Hide();
    }

    private void Hide()
    {
        state = State.Hidden;
        breakAfterImpact = false;
        if (shieldRenderer)
            shieldRenderer.enabled = false;
    }

    private void Show(State next, Sprite sprite)
    {
        state = next;
        stateTime = 0f;
        shieldRenderer.sprite = sprite;
        ApplyShaderProperties(0f);
        shieldRenderer.enabled = true;
    }

    private void Update() => AdvanceTime(Time.deltaTime);

    private void AdvanceTime(float delta)
    {
        if (state == State.Hidden)
            return;

        stateTime += delta;
        elapsed += delta;

        switch (state)
        {
            case State.Cast:
                Advance(0.48f, State.Charge, chargeSprite);
                break;
            case State.Charge:
                Advance(0.37f, State.Idle, idleSprite);
                break;
            case State.Impact:
                if (breakAfterImpact)
                    Advance(0.36f, State.Shatter, shatterSprite, 0.57f);
                else
                    Advance(0.36f, State.Idle, idleSprite);
                break;
            case State.Shatter:
                if (stateTime >= 0.68f)
                    Hide();
                else
                    ApplyShaderProperties(stateTime / 0.68f);
                break;
            case State.Idle:
                ApplyShaderProperties(0f);
                break;
        }
    }

    private void Advance(float duration, State next, Sprite sprite, float endProgress = 1f)
    {
        float progress = stateTime / duration;
        if (progress >= endProgress)
            Show(next, sprite);
        else
            ApplyShaderProperties(progress);
    }

    private void ApplyShaderProperties(float progress)
    {
        properties.SetFloat(StateId, (int)state);
        properties.SetFloat(ProgressId, progress);
        properties.SetFloat(ElapsedId, elapsed);
        shieldRenderer.SetPropertyBlock(properties);
    }
}
