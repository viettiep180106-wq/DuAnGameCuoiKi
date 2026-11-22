using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

internal sealed class EnemyAttackVfx : MonoBehaviour
{
    private static readonly List<EnemyAttackVfx> available = new();
    private static readonly List<EnemyAttackVfx> availableWater = new();
    private static readonly List<EnemyAttackVfx> availableEarth = new();
    private static readonly List<EnemyAttackVfx> availableWood = new();
    private static readonly List<EnemyAttackVfx> availableMetal = new();
    private const int PoolCapacity = 6;

    private EnemyAttackVfxVisuals visuals;
    private EnemyFireAdvancedAttackVfx advanced;
    private EnemyWaterAttackVfx water;
    private EnemyEarthAttackVfx earth;
    private EnemyWoodAttackVfx wood;
    private EnemyMetalAttackVfx metal;
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
    internal Action<StoneBase> BoardCellEffect => earth?.BoardCellEffect ?? water?.BoardCellEffect;
    internal bool HasCustomArmor => earth?.UsesArmor == true || wood?.UsesArmor == true || metal?.UsesArmor == true;
    internal bool HasCustomMetal => metal != null;
    internal bool HasCustomTeamSupport => wood?.UsesTeam == true;
    internal void WoodPoisonApplied() => wood?.RecordPoison();

    internal static EnemyAttackVfx Begin(EnemyBase caster, Player target, bool active)
    {
        if (!Application.isPlaying || !caster.enemyData || !target)
            return null;

        EnemyDataSO data = caster.enemyData;
        EnemyAttackVfxStyle attackStyle = active ? data.activeSkillVfx : data.normalAttackVfx;
        if (attackStyle == EnemyAttackVfxStyle.None)
            return null;

        bool usesWater = EnemyWaterAttackVfx.Supports(attackStyle);
        bool usesEarth = EnemyEarthAttackVfx.Supports(attackStyle);
        bool usesWood = EnemyWoodAttackVfx.Supports(attackStyle);
        bool usesMetal = EnemyMetalAttackVfx.Supports(attackStyle);
        var atlas = usesMetal ? EnemyMetalVfxArt.Load() : usesWater ? EnemyWaterVfxArt.Load() :
            usesEarth ? EnemyEarthVfxArt.Load() : usesWood ? EnemyWoodVfxArt.Load() : null;
        bool usesAtlas = usesWater || usesEarth || usesWood || usesMetal;
        var art = usesAtlas ? null : EnemyAttackVfxArt.Load();
        if (usesAtlas ? atlas == null : !art || !art.IsReady)
            return null;
        bool usesAdvanced = attackStyle is >= EnemyAttackVfxStyle.HammerStrike and <= EnemyAttackVfxStyle.BlueMeteorShower;
        if (usesAdvanced && !art.HasAdvancedArt)
            return null;

        var scene = caster.gameObject.scene;
        var pool = usesMetal ? availableMetal : usesWood ? availableWood : usesEarth ? availableEarth : usesWater ? availableWater : available;
        EnemyAttackVfx effect = null;
        for (int i = pool.Count - 1; i >= 0; i--)
        {
            var candidate = pool[i];
            if (candidate.gameObject.scene != scene || (candidate.advanced != null) != usesAdvanced)
                continue;
            effect = candidate;
            pool.RemoveAt(i);
            break;
        }

        if (!effect)
        {
            var root = new GameObject("Enemy Attack VFX") { hideFlags = HideFlags.DontSave };
            SceneManager.MoveGameObjectToScene(root, scene);
            effect = root.AddComponent<EnemyAttackVfx>();
            if (usesMetal)
                effect.metal = new EnemyMetalAttackVfx(root.transform, atlas);
            else if (usesWood)
                effect.wood = new EnemyWoodAttackVfx(root.transform, atlas);
            else if (usesEarth)
                effect.earth = new EnemyEarthAttackVfx(root.transform, atlas);
            else if (usesWater)
                effect.water = new EnemyWaterAttackVfx(root.transform, atlas);
            else if (usesAdvanced)
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
        if (effect.water != null)
        {
            effect.water.Begin(attackStyle, caster, target, offset, effect.scale, effect.sortingLayer, effect.sortingOrder);
            effect.timeout = effect.water.Duration;
        }
        else if (effect.earth != null)
        {
            effect.earth.Begin(attackStyle, caster, target, offset, effect.scale, effect.sortingLayer, effect.sortingOrder);
            effect.timeout = effect.earth.Duration;
        }
        else if (effect.wood != null)
        {
            effect.wood.Begin(attackStyle, caster, target, offset, effect.scale, effect.sortingLayer, effect.sortingOrder);
            effect.timeout = effect.wood.Duration;
        }
        else if (effect.metal != null)
        {
            effect.metal.Begin(attackStyle, caster, target, offset, effect.scale, effect.awayFromCaster,
                effect.sortingLayer, effect.sortingOrder);
            effect.timeout = effect.metal.Duration;
        }
        effect.gameObject.SetActive(true);
        return effect;
    }

    internal void BindProjectile(float flightDuration, float expectedContact)
    {
        HasProjectile = true;
        if (metal != null)
        {
            timeout = metal.BindProjectile(Time.time - startedAt, flightDuration, expectedContact);
            return;
        }
        if (wood != null)
        {
            timeout = wood.BindProjectile(Time.time - startedAt, flightDuration, expectedContact);
            return;
        }
        if (earth != null)
        {
            timeout = earth.BindProjectile(Time.time - startedAt, flightDuration, expectedContact);
            return;
        }
        if (water != null)
        {
            timeout = water.BindProjectile(Time.time - startedAt, flightDuration, expectedContact);
            return;
        }
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
        if (metal != null)
        {
            timeout = metal.Impact(contact);
            return true;
        }
        if (wood != null)
        {
            timeout = wood.Impact(contact);
            return true;
        }
        if (earth != null)
        {
            timeout = earth.Impact(contact);
            return true;
        }
        if (water != null)
        {
            timeout = water.Impact(contact);
            return true;
        }
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
        if (metal != null)
            timeout = metal.Miss(Time.time - startedAt);
        else if (wood != null)
            timeout = wood.Miss(Time.time - startedAt);
        else if (earth != null)
            timeout = earth.Miss(Time.time - startedAt);
        else if (water != null)
            timeout = water.Miss(Time.time - startedAt);
        else if (advanced?.PlaysThroughMiss == true)
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

    internal static Bounds CharacterBounds(BaseCharacter character)
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
        if (metal != null)
            metal.Render(age, contact, hitPoint, feet);
        else if (wood != null)
            wood.Render(age, contact, timeout, hitPoint, feet, awayFromCaster);
        else if (earth != null)
            earth.Render(age, contact, timeout, hitPoint, feet, awayFromCaster);
        else if (water != null)
            water.Render(age, contact, timeout, hitPoint, feet, awayFromCaster);
        else if (advanced != null)
            advanced.Render(age, frame, muzzle, hitPoint, feet);
        else
            visuals.Render(style, age, scale, frame, muzzle, hitPoint, feet, awayFromCaster, sortingLayer, sortingOrder);
    }

    private void Release()
    {
        advanced?.Clear();
        water?.Clear();
        earth?.Clear();
        wood?.Clear();
        metal?.Clear();
        gameObject.SetActive(false);
        var pool = Pool;
        if (pool.Count < PoolCapacity)
            pool.Add(this);
        else
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        Pool.Remove(this);
        visuals?.Dispose();
        advanced?.Dispose();
        water?.Dispose();
        earth?.Dispose();
        wood?.Dispose();
        metal?.Dispose();
    }

    private List<EnemyAttackVfx> Pool => metal != null ? availableMetal : wood != null ? availableWood :
        earth != null ? availableEarth : water != null ? availableWater : available;

    internal void BeforeGameplay()
    {
        water?.BeforeGameplay();
        earth?.BeforeGameplay();
        wood?.BeforeGameplay();
    }

    internal void AfterGameplay()
    {
        water?.AfterGameplay(Time.time - startedAt);
        earth?.AfterGameplay();
        wood?.AfterGameplay();
        metal?.AfterGameplay();
    }
}
