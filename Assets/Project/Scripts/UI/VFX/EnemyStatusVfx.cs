using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Kind = EnemyStatusAnimation.Kind;

internal sealed class EnemyStatusVfx : MonoBehaviour, EnemyStatusAnimation.ICanvas
{
    private struct Status
    {
        public bool visible;
        public float started;
        public float ended;
        public float lastTick;
    }

    private static readonly Dictionary<Rect, Sprite> Sprites = new();
    private static Texture2D atlas;
    private static Material burnMaterial;
    private static Material ringMaterial;
    private static Sprite burnQuad;

    private readonly Status[] states = new Status[(int)Kind.DefenseDown + 1];
    private readonly List<SpriteRenderer> layers = new();
    private readonly Vector3[] orbitPoints = new Vector3[40];
    private EnemyBase enemy;
    private MaterialPropertyBlock burnProperties;
    private SortingGroup group;
    private SpriteRenderer flame;
    private LineRenderer ring;
    private int spritesUsed;
    private int order;
    private float depth;

    internal static void Show(BaseCharacter character, Kind kind)
    {
        if (!Application.isPlaying || character is not EnemyBase target || !target ||
            !target.isActiveAndEnabled || target.CurrentHP <= 0f) return;

        if (!atlas) atlas = Resources.Load<Texture2D>(EnemyStatusAnimation.AtlasResource);
        if (!atlas) return;
        if (!target.TryGetComponent(out EnemyStatusVfx visual))
            visual = target.gameObject.AddComponent<EnemyStatusVfx>();
        visual.enemy = target;

        ref Status state = ref visual.states[(int)kind];
        if (!state.visible || !float.IsPositiveInfinity(state.ended) || kind == Kind.DefenseDown)
        {
            state = new Status
            {
                visible = true,
                started = Time.time,
                ended = float.PositiveInfinity,
                lastTick = float.NegativeInfinity
            };
        }
        visual.enabled = true;
    }

    internal static void Finish(BaseCharacter character, Kind kind)
    {
        if (character && character.TryGetComponent(out EnemyStatusVfx visual))
            End(ref visual.states[(int)kind], Time.time);
    }

    internal static void BurnTick(BaseCharacter character)
    {
        if (character && character.TryGetComponent(out EnemyStatusVfx visual))
            visual.states[(int)Kind.Burn].lastTick = Time.time;
    }

    private void LateUpdate()
    {
        if (!enemy) { enabled = false; return; }

        float now = Time.time;
        bool active = false;
        for (int i = 0; i < states.Length; i++)
        {
            ref Status state = ref states[i];
            if (enemy.CurrentHP <= 0f) End(ref state, now);
            if (state.visible && now - state.ended >= EnemyStatusAnimation.Outro)
                state.visible = false;
            active |= state.visible;
        }
        if (!active) { enabled = false; return; }

        if (!group)
        {
            var root = new GameObject("Player Skill Status VFX") { hideFlags = HideFlags.DontSave };
            SceneManager.MoveGameObjectToScene(root, gameObject.scene);
            group = root.AddComponent<SortingGroup>();
        }
        var actor = enemy.spriteRenderer;
        group.sortingLayerID = actor ? actor.sortingLayerID : 0;
        group.sortingOrder = actor ? actor.sortingOrder + 2 : 2;
        group.gameObject.SetActive(true);
        var bounds = EnemyAttackVfx.CharacterBounds(enemy);
        float unit = Mathf.Max(.75f, bounds.size.y);
        depth = bounds.center.z;
        group.transform.position = new Vector3(bounds.center.x, bounds.min.y, depth);
        Vector2 feet = new(bounds.center.x, -bounds.min.y);
        Vector2 head = feet - Vector2.up * unit * 1.12f;
        spritesUsed = 0;
        order = 0;
        if (flame) flame.enabled = false;
        if (ring) ring.enabled = false;

        DrawStatus(Kind.Freeze, feet, head, unit, now);
        DrawStatus(Kind.Burn, feet, head, unit, now);
        DrawStatus(Kind.Stun, feet, head, unit, now);
        if (states[(int)Kind.Stun].visible) head.x += unit * .66f;
        DrawStatus(Kind.DefenseDown, feet, head, unit, now);

        for (int i = spritesUsed; i < layers.Count; i++) layers[i].enabled = false;
    }

    private void DrawStatus(Kind kind, Vector2 feet, Vector2 head, float unit, float now)
    {
        Status state = states[(int)kind];
        if (!state.visible) return;
        EnemyStatusAnimation.Draw(this, kind, feet, head, unit, now - state.started, now - state.ended,
            Mathf.Exp(-Mathf.Max(0f, now - state.lastTick) * 15f));
    }

    private static void End(ref Status state, float now)
    {
        if (state.visible && float.IsPositiveInfinity(state.ended)) state.ended = now;
    }

    public void Draw(Rect art, Vector2 anchor, float width, float alpha, float angle, bool bottom, float heightScale)
    {
        if (alpha <= .001f) return;
        if (!Sprites.TryGetValue(art, out var sprite))
        {
            var uv = EnemyStatusAnimation.AtlasUv(art);
            var region = new Rect(uv.x * atlas.width, uv.y * atlas.height, uv.width * atlas.width, uv.height * atlas.height);
            sprite = Sprite.Create(atlas, region, Vector2.one * .5f, 100f, 0, SpriteMeshType.FullRect);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Sprites.Add(art, sprite);
        }
        if (spritesUsed == layers.Count) layers.Add(NewSprite("Status sprite"));
        var renderer = layers[spritesUsed++];
        renderer.sprite = sprite;
        renderer.color = new Color(1f, 1f, 1f, alpha);
        float height = width * art.height / art.width * heightScale;
        if (bottom) anchor.y -= height * .5f;
        Place(renderer, anchor, width, height, angle);
    }

    public void DrawFlames(Vector2 feet, float unit, Vector4 motion)
    {
        if (!burnMaterial)
        {
            var shader = Resources.Load<Shader>(EnemyStatusAnimation.BurnShaderResource);
            if (shader && shader.isSupported)
                burnMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }
        if (!burnMaterial)
        {
            Draw(EnemyStatusAnimation.BurnFallback, feet, unit * .44f, motion.z, 0f, true, 1f);
            return;
        }
        if (!flame)
        {
            var white = Texture2D.whiteTexture;
            if (!burnQuad)
            {
                burnQuad = Sprite.Create(white, new Rect(0f, 0f, white.width, white.height), Vector2.one * .5f,
                    white.width, 0, SpriteMeshType.FullRect);
                burnQuad.hideFlags = HideFlags.HideAndDontSave;
            }
            flame = NewSprite("Burn flames");
            flame.sprite = burnQuad;
            flame.sharedMaterial = burnMaterial;
        }
        burnProperties ??= new MaterialPropertyBlock();
        burnProperties.SetVector("_Motion", motion);
        flame.SetPropertyBlock(burnProperties);
        float height = unit * .58f;
        Place(flame, feet - Vector2.up * height * .44f, unit * .68f, height, 0f);
    }

    public void DrawOrbit(Vector2 center, float unit, float tilt, float opacity)
    {
        if (!ringMaterial)
        {
            var shader = Shader.Find("Sprites/Default");
            if (!shader) return;
            ringMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }
        if (!ring)
        {
            ring = new GameObject("Stun orbit").AddComponent<LineRenderer>();
            ring.transform.SetParent(group.transform, false);
            ring.sharedMaterial = ringMaterial;
            ring.loop = true;
            ring.positionCount = orbitPoints.Length;
            ring.shadowCastingMode = ShadowCastingMode.Off;
            ring.receiveShadows = false;
        }
        for (int i = 0; i < orbitPoints.Length; i++)
            orbitPoints[i] = World(EnemyStatusAnimation.OrbitPoint(center, unit, i * 2f * Mathf.PI / orbitPoints.Length, tilt));
        ring.SetPositions(orbitPoints);
        ring.widthMultiplier = unit * .008f;
        ring.startColor = ring.endColor = new Color(1f, .72f, .20f, opacity);
        ring.sortingOrder = order++;
        ring.enabled = true;
    }

    private SpriteRenderer NewSprite(string objectName)
    {
        var renderer = new GameObject(objectName).AddComponent<SpriteRenderer>();
        renderer.transform.SetParent(group.transform, false);
        return renderer;
    }

    private Vector3 World(Vector2 screenPoint) => new(screenPoint.x, -screenPoint.y, depth);

    private void Place(SpriteRenderer renderer, Vector2 point, float width, float height, float angle)
    {
        renderer.sortingOrder = order++;
        var size = renderer.sprite.bounds.size;
        renderer.transform.localScale = new Vector3(width / size.x, height / size.y, 1f);
        renderer.transform.SetPositionAndRotation(World(point), Quaternion.Euler(0f, 0f, -angle));
        renderer.enabled = true;
    }

    private void OnDisable()
    {
        Array.Clear(states, 0, states.Length);
        if (group) group.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (group) Destroy(group.gameObject);
    }
}
