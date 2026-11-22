using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

internal sealed class EnemyAttackVfx : MonoBehaviour
{
    private static readonly List<EnemyAttackVfx> available = new();
    private const int PoolCapacity = 6;

    private EnemyAttackVfxVisuals visuals;
    private EnemyFireAdvancedAttackVfx advanced;
    private EnemyAttackVfxStyle style;
    private Vector3 muzzle;
    private Vector3 hitPoint;
    private Vector3 feet;
    private Vector3 awayFromCaster;
    private float scale;
    private float startedAt;
    private float release;
    private float flightEnd;
    private float windup;
    private float contact;
    private float timeout;
    private int sortingLayer;
    private int sortingOrder;

    internal bool HasProjectile { get; private set; }

    internal static EnemyAttackVfx Begin(EnemyBase caster, Player target, bool active)
    {
        if (!Application.isPlaying || !caster.enemyData || !target)
            return null;

        EnemyDataSO data = caster.enemyData;
        EnemyAttackVfxStyle attackStyle = active ? data.activeSkillVfx : data.normalAttackVfx;
        if (attackStyle == EnemyAttackVfxStyle.None)
            return null;

        var art = EnemyAttackVfxArt.Load();
        if (!art || !art.IsReady)
            return null;
        bool usesAdvanced = attackStyle is >= EnemyAttackVfxStyle.HammerStrike and <= EnemyAttackVfxStyle.BlueMeteorShower;
        if (usesAdvanced && !art.HasAdvancedArt)
            return null;

        var scene = caster.gameObject.scene;
        EnemyAttackVfx effect = null;
        for (int i = available.Count - 1; i >= 0; i--)
        {
            var candidate = available[i];
            if (candidate.gameObject.scene != scene || (candidate.advanced != null) != usesAdvanced)
                continue;
            effect = candidate;
            available.RemoveAt(i);
            break;
        }

        if (!effect)
        {
            var root = new GameObject("Enemy Attack VFX") { hideFlags = HideFlags.DontSave };
            SceneManager.MoveGameObjectToScene(root, scene);
            effect = root.AddComponent<EnemyAttackVfx>();
            if (usesAdvanced)
                effect.advanced = new EnemyFireAdvancedAttackVfx(root.transform, art);
            else
                effect.visuals = new EnemyAttackVfxVisuals(root.transform, art);
        }

        effect.style = attackStyle;
        effect.startedAt = Time.time;
        effect.contact = float.PositiveInfinity;
        effect.release = attackStyle switch
        {
            EnemyAttackVfxStyle.FireBeam => 0.30f,
            EnemyAttackVfxStyle.FlameBite => 0.10f,
            _ => 0f
        };
        effect.windup = attackStyle == EnemyAttackVfxStyle.FlameBite ? 0.20f : 0.30f;
        effect.flightEnd = 0.30f;
        effect.timeout = 1f;
        effect.HasProjectile = false;

        float direction = target.transform.position.x <= caster.transform.position.x ? -1f : 1f;
        effect.awayFromCaster = Vector3.right * direction;
        Bounds casterBounds = CharacterBounds(caster);
        Vector2 offset = data.attackVfxMuzzleOffset * casterBounds.size.y;
        effect.muzzle = casterBounds.center + new Vector3(offset.x * direction, offset.y);
        effect.SetTargetAnchors(target);
        effect.scale = Mathf.Max(0.1f, data.attackVfxScale)
            * Mathf.Clamp(CharacterBounds(target).size.y / 5.6f, 0.85f, 2f);
        var targetRenderer = target.spriteRenderer;
        effect.sortingLayer = targetRenderer ? targetRenderer.sortingLayerID : 0;
        effect.sortingOrder = Mathf.Max(36, (targetRenderer ? targetRenderer.sortingOrder : 0) + 2);
        effect.advanced?.Begin(attackStyle, caster, target, effect.feet, effect.scale, effect.sortingLayer, effect.sortingOrder);
        effect.gameObject.SetActive(true);
        return effect;
    }

    internal void BindProjectile(float flightDuration, float expectedContact)
    {
        HasProjectile = true;
        flightEnd = Mathf.Max(0.065f, expectedContact);
        timeout = flightDuration + 0.5f;
        if (advanced?.PlaysThroughMiss == true)
            timeout = Mathf.Max(timeout, 1.89f);
        if (style == EnemyAttackVfxStyle.FireBeam)
            release = Mathf.Min(0.30f, flightEnd * 0.55f);
        if (style == EnemyAttackVfxStyle.FirePillar)
            windup = Mathf.Min(0.30f, flightEnd * 0.92f);
    }

    internal bool Impact(Player target)
    {
        if (!isActiveAndEnabled || !float.IsPositiveInfinity(contact))
            return false;

        if (target)
            SetTargetAnchors(target);
        contact = Time.time - startedAt;
        if (style == EnemyAttackVfxStyle.FirePillar)
            windup = Mathf.Min(windup, Mathf.Max(0.001f, contact));
        timeout = contact + (style == EnemyAttackVfxStyle.FirePillar ? 1.16f : 0.60f);
        if (advanced != null)
        {
            advanced.Impact(target, feet);
            timeout = advanced.EndAfter(contact);
        }
        return true;
    }

    internal void CancelIfInFlight()
    {
        if (!isActiveAndEnabled || !float.IsPositiveInfinity(contact))
            return;
        if (advanced?.PlaysThroughMiss == true)
            timeout = Mathf.Min(timeout, 1.89f);
        else
            Release();
    }

    private void SetTargetAnchors(Player target)
    {
        Bounds bounds = CharacterBounds(target);
        hitPoint = bounds.center + Vector3.up * (bounds.size.y * 0.08f) - awayFromCaster * (bounds.size.x * 0.08f);
        feet = new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.07f, bounds.center.z);
    }

    private static Bounds CharacterBounds(BaseCharacter character)
    {
        var renderer = character.spriteRenderer;
        if (renderer && renderer.sprite)
            return renderer.bounds;

        var collider = character.GetComponent<Collider2D>();
        return collider && collider.bounds.size.y > 0f
            ? collider.bounds
            : new Bounds(character.transform.position, new Vector3(4f, 5.6f, 0f));
    }

    private void LateUpdate()
    {
        float age = Time.time - startedAt;
        if (age >= timeout)
        {
            Release();
            return;
        }
        Render(age);
    }

    private void Render(float age)
    {
        var frame = new EnemyAttackVfxFrame(release, flightEnd, contact, windup);
        if (advanced != null)
            advanced.Render(age, frame, muzzle, hitPoint, feet);
        else
            visuals.Render(style, age, scale, frame, muzzle, hitPoint, feet, awayFromCaster, sortingLayer, sortingOrder);
    }

    private void Release()
    {
        advanced?.Clear();
        gameObject.SetActive(false);
        if (available.Count < PoolCapacity)
            available.Add(this);
        else
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        available.Remove(this);
        visuals?.Dispose();
        advanced?.Dispose();
    }
}
