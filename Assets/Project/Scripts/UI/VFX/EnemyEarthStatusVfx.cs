using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static EnemyEarthFx;

internal sealed class EnemyEarthStatusVfx : MonoBehaviour
{
    private enum Kind { Seal, Stone, AttackDebuff, Stun }
    private const float Twice = EnemyEarthAttackVfx.DurationMultiplier;
    private Kind kind;
    private float startedAt;
    private float releasedAt = float.PositiveInfinity;
    private float size;
    private float delay;
    private RectTransform plane;
    private Image emblem;
    private Image lockIcon;
    private Image[] links;
    private StoneBase stone;
    private StoneBase[] cells;
    private Player target;
    private SpriteRenderer marker;

    internal static void ShowSeal(EnemyAtlasVfxArt art, IReadOnlyList<StoneBase> locked, EnemyAttackVfxStyle style)
    {
        bool small = style == EnemyAttackVfxStyle.StoneSeal;
        bool boss = style == EnemyAttackVfxStyle.Earthquake;
        var board = BoardController.instance;
        if (board && board.center && board.center.gameObject.activeInHierarchy)
        {
            var rune = MakeUi(board.center, Kind.Seal);
            rune.size = Mathf.Max(64f, Mathf.Min(board.center.rect.width, board.center.rect.height)) * (small ? .53f : .81f);
            rune.emblem = PutImage(rune.plane, art.GetSprite(small ? 12 : 27));
            if (!small && locked.Count > 1)
            {
                rune.cells = new StoneBase[locked.Count];
                rune.links = new Image[locked.Count - 1];
                for (int i = 0; i < locked.Count; i++) rune.cells[i] = locked[i];
                for (int i = 0; i < rune.links.Length; i++) rune.links[i] = PutImage(rune.plane, art.GetSprite(28));
            }
        }
        for (int i = 0; i < locked.Count; i++)
        {
            var cell = locked[i];
            if (!cell || !cell.isLocked || cell.transform is not RectTransform parent || !parent.gameObject.activeInHierarchy) continue;
            foreach (Transform child in parent)
                if (child.gameObject.activeSelf && child.TryGetComponent(out EnemyEarthStatusVfx old) && old.kind == Kind.Stone)
                    old.Retire();
            var status = MakeUi(parent, Kind.Stone);
            status.stone = cell;
            status.delay = .085f + i * .036f;
            status.size = Mathf.Max(30f, Mathf.Min(parent.rect.width, parent.rect.height));
            status.emblem = PutImage(status.plane, art.GetSprite(small ? 13 : 28));
            status.lockIcon = PutImage(status.plane, art.GetSprite(boss ? 34 : 14));
        }
    }

    internal static void ShowPlayer(EnemyAtlasVfxArt art, Player target, bool stunned)
    {
        if (!target || !target.gameObject.activeInHierarchy) return;
        Kind kind = stunned ? Kind.Stun : Kind.AttackDebuff;
        string name = "Earth VFX " + kind;
        var old = target.transform.Find(name);
        if (old && old.gameObject.activeSelf && old.TryGetComponent(out EnemyEarthStatusVfx existing))
        {
            existing.startedAt = Time.time;
            return;
        }
        var root = new GameObject(name) { hideFlags = HideFlags.DontSave };
        root.transform.SetParent(target.transform, false);
        var status = root.AddComponent<EnemyEarthStatusVfx>();
        status.startedAt = Time.time;
        status.kind = kind;
        status.target = target;
        status.marker = root.AddComponent<SpriteRenderer>();
        status.marker.sprite = art.GetSprite(stunned ? 34 : 29);
        var sprite = target.spriteRenderer;
        status.marker.sortingLayerID = sprite ? sprite.sortingLayerID : 0;
        status.marker.sortingOrder = sprite ? sprite.sortingOrder + 4 : 39;
        status.marker.enabled = false;
    }

    private static EnemyEarthStatusVfx MakeUi(RectTransform parent, Kind mode)
    {
        var root = new GameObject("Earth VFX " + mode, typeof(RectTransform)) { hideFlags = HideFlags.DontSave };
        root.transform.SetParent(parent, false);
        var state = root.AddComponent<EnemyEarthStatusVfx>();
        state.kind = mode;
        state.startedAt = Time.time;
        state.plane = (RectTransform)root.transform;
        state.plane.anchorMin = state.plane.anchorMax = Vector2.one * .5f;
        state.plane.anchoredPosition = Vector2.zero;
        state.plane.sizeDelta = Vector2.zero;
        state.plane.SetAsLastSibling();
        return state;
    }

    private static Image PutImage(RectTransform root, Sprite sprite)
    {
        var obj = new GameObject("Earth Sprite", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)) { hideFlags = HideFlags.DontSave };
        obj.transform.SetParent(root, false);
        var image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        image.enabled = false;
        return image;
    }

    private void LateUpdate()
    {
        float age = (Time.time - startedAt) / Twice;
        switch (kind)
        {
            case Kind.Seal: DrawSeal(age); break;
            case Kind.Stone: DrawLock(age); break;
            default: DrawMarker(age); break;
        }
    }

    private void DrawSeal(float age)
    {
        if (age >= 1.05f) { Retire(); return; }
        float reveal = Smooth(0f, .30f, age);
        float opacity = Pulse(age,.015f,.25f,.34f,.69f);
        var rect = emblem.rectTransform;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(size * Mathf.Lerp(.23f, 1f, reveal), size * Mathf.Lerp(.23f, .83f, reveal));
        rect.localRotation = Quaternion.Euler(0f, 0f, -34f * (1f - reveal));
        // The Filled Image sweeps the actual rune as its outer seal expands.
        emblem.type = Image.Type.Filled;
        emblem.fillMethod = Image.FillMethod.Radial360;
        emblem.fillAmount = reveal;
        Tint(emblem, opacity);
        if (links == null) return;
        for (int i = 0; i < links.Length; i++)
        {
            if (!cells[i] || !cells[i + 1] || !cells[i].isLocked || !cells[i + 1].isLocked ||
                !cells[i].gameObject.activeInHierarchy || !cells[i + 1].gameObject.activeInHierarchy)
            {
                links[i].enabled = false;
                continue;
            }
            Vector2 a = plane.InverseTransformPoint(cells[i].transform.position);
            Vector2 b = plane.InverseTransformPoint(cells[i + 1].transform.position);
            float phase = age - i * .043f;
            Vector2 end = Vector2.Lerp(a, b, Smooth(.19f,.49f,phase));
            Vector2 delta = end - a;
            Place(links[i], delta.magnitude, size * .055f, (a + end) * .5f,
                Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, Pulse(phase,.18f,.32f,.58f,.86f) * .75f);
        }
    }

    private void DrawLock(float age)
    {
        if (!stone || !stone.isLocked || stone.isFrozen)
        {
            if (float.IsPositiveInfinity(releasedAt)) releasedAt = Time.time;
        }
        float fade = 1f - Smooth(0f,.28f,(Time.time - releasedAt) / Twice);
        if (fade <= .002f) { Retire(); return; }
        float t = age - delay;
        float pull = Smooth(0f,.29f,t);
        float alpha = pull * fade;
        Place(emblem, size * Mathf.Lerp(1.61f, 1.04f, pull), size * Mathf.Lerp(1.32f, .91f, pull), Vector2.zero,
            35f * (1f - pull), alpha * .86f);
        Place(lockIcon, size * .59f, size * .61f, new Vector2(size * .21f, -size * .19f), 0f,
            Smooth(.16f, .34f, t) * fade * (.92f + .08f * Mathf.Sin(age * 7f)));
    }

    private void DrawMarker(float age)
    {
        bool stillActive = target && target.CurrentHP > 0f && (kind == Kind.Stun ? target.IsStunned : target.AtkDebuffRounds > 0);
        if (!stillActive) { Retire(); return; }
        Bounds bounds = EnemyAttackVfx.CharacterBounds(target);
        float size = bounds.size.y;
        Vector3 anchor = bounds.center + Vector3.up * (bounds.extents.y + size * (.27f + .037f * Mathf.Sin(age * 6f)));
        float fade = Smooth(0f, .20f, age);
        float width = size * .38f * Mathf.Lerp(.63f, 1f, fade);
        float factor = width / marker.sprite.bounds.size.x;
        Vector3 parentScale = transform.parent.lossyScale;
        transform.localScale = new Vector3(factor / Mathf.Max(.001f, Mathf.Abs(parentScale.x)),
            factor / Mathf.Max(.001f, Mathf.Abs(parentScale.y)), 1f);
        transform.SetPositionAndRotation(anchor, Quaternion.Euler(0f, 0f, 7f * Mathf.Sin(age * 8f)));
        marker.color = new Color(1f, 1f, 1f, fade);
        marker.enabled = true;
    }

    private static void Place(Image image, float width, float height, Vector2 point, float rotation, float opacity)
    {
        var rect = image.rectTransform;
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = point;
        rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
        Tint(image, opacity);
    }

    private static void Tint(Image image, float opacity)
    {
        image.color = new Color(1f, 1f, 1f, opacity);
        image.enabled = opacity > .004f;
    }

    private void Retire()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        if (Application.isPlaying && gameObject.activeSelf)
            Destroy(gameObject);
    }
}
