using UnityEngine;
using UnityEngine.SceneManagement;
using static EnemyEarthFx;
using static EnemyAtlasVfxLayers;

internal sealed class EnemyEarthArmorVfx : MonoBehaviour
{
    private const float Twice = EnemyEarthAttackVfx.DurationMultiplier;
    private EnemyBase owner;
    private EnemyAtlasVfxArt art;
    private EnemyAtlasVfxLayers layers;
    private GameObject worldRoot;
    private EnemyAttackVfxStyle style;
    private float startedAt;
    private float endedAt;
    private float lastHit;

    internal static void Show(EnemyBase enemy, EnemyAttackVfxStyle kind, EnemyAtlasVfxArt art)
    {
        if (!Application.isPlaying || !enemy || !enemy.gameObject.activeInHierarchy || art == null) return;
        if (!enemy.TryGetComponent(out EnemyEarthArmorVfx visual))
            visual = enemy.gameObject.AddComponent<EnemyEarthArmorVfx>();
        visual.owner = enemy;
        visual.art = art;
        visual.style = kind;
        visual.startedAt = Time.time;
        visual.endedAt = float.PositiveInfinity;
        visual.lastHit = float.NegativeInfinity;
        visual.enabled = true;
        visual.Prepare();
    }

    internal static void ShowBossShield(EnemyBase enemy)
    {
        if (!Application.isPlaying || !enemy.enemyData || enemy.enemyData.activeSkillVfx != EnemyAttackVfxStyle.Earthquake) return;
        Show(enemy, EnemyAttackVfxStyle.Earthquake, EnemyEarthVfxArt.Load());
    }

    internal static void OnReduction(EnemyBase enemy)
    {
        if (enemy.type == EnemyType.SlimeBun && enemy.TryGetComponent(out EnemyEarthArmorVfx visual) && visual.isActiveAndEnabled)
            visual.lastHit = Time.time;
    }

    internal static void OnShieldHit(EnemyBase enemy, bool broke)
    {
        if (enemy.type is not (EnemyType.LinhDat or EnemyType.HatNhamThach or EnemyType.BossDatNung)
            || !enemy.TryGetComponent(out EnemyEarthArmorVfx visual) || !visual.isActiveAndEnabled) return;
        visual.lastHit = Time.time;
        if (broke)
        {
            visual.endedAt = Time.time;
            EnemyEarthReactionVfx.PlayBreak(enemy, visual.art, visual.style == EnemyAttackVfxStyle.MagmaArmor);
        }
    }

    private bool StanceIsActive => style switch
    {
        EnemyAttackVfxStyle.MudArmor => owner.damageReductionRounds > 0 && owner.damageReductionPercent > 0f,
        EnemyAttackVfxStyle.ThornArmor => owner.reflectBuffRounds > 0 && owner.temporaryReflectBuff > 0f,
        _ => owner.shieldRounds > 0 && owner.currentShield > 0f
    };

    private void Prepare()
    {
        if (!worldRoot)
        {
            worldRoot = new GameObject("Earth Armor VFX") { hideFlags = HideFlags.DontSave };
            SceneManager.MoveGameObjectToScene(worldRoot, owner.gameObject.scene);
            layers = new EnemyAtlasVfxLayers(worldRoot.transform, art);
        }
        var sprite = owner.spriteRenderer;
        int layer = sprite ? sprite.sortingLayerID : 0;
        int order = sprite ? sprite.sortingOrder : 35;
        layers.SetSorting(layer, order + 1, order - 1);
        worldRoot.SetActive(true);
    }

    private void LateUpdate()
    {
        if (!owner || owner.CurrentHP <= 0f || !worldRoot)
        {
            enabled = false;
            return;
        }
        float now = Time.time;
        if (!StanceIsActive && float.IsPositiveInfinity(endedAt)) endedAt = now;
        float fade = 1f - Smooth(0f, .31f, (now - endedAt) / Twice);
        if (fade <= .002f) { enabled = false; return; }

        float age = (now - startedAt) / Twice;
        float strikeAge = (now - lastHit) / Twice;
        float strike = Pulse(strikeAge, 0f, .030f, .079f, .26f);
        var bounds = EnemyAttackVfx.CharacterBounds(owner);
        float unit = Mathf.Clamp(bounds.size.y / 5.6f, .55f, 2.6f);
        var floor = new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * .04f, bounds.center.z);
        var top = bounds.center + Vector3.up * (bounds.extents.y + unit * 1.29f);
        float side = Player.Instance && Player.Instance.transform.position.x > owner.transform.position.x ? 1f : -1f;
        float build = Smooth(0f, .38f, age);
        layers.BeginFrame();
        switch (style)
        {
            case EnemyAttackVfxStyle.MudArmor:
                layers.Draw(3, floor + Vector3.up * unit * .54f, unit * (7.2f + .17f * Mathf.Sin(age * 18f)),
                    build * fade * .95f, age, height: unit * 1.9f, warp: .055f, ground: true);
                Shell(2, floor, build, fade * .62f, strike, age, unit * 7.8f, unit * 5.55f, .044f);
                layers.Draw(4, top, unit * (1.92f + .44f * strike), build * fade * (.57f + strike * .43f));
                layers.Orbit(floor + Vector3.up * unit * 1.61f, age, unit * 2.25f, unit * 1.35f, WetClay,
                    build * fade * (.32f + strike * .43f));
                layers.Burst(Vector3.Lerp(floor, top, .34f) + Vector3.right * side * unit * 1.7f,
                    strikeAge, 14, unit * 6.5f, unit * .39f, WetClay, life: .37f);
                break;
            case EnemyAttackVfxStyle.StoneGuard:
                Shell(7, floor, build, fade * .58f, strike, age, unit * 8.2f, unit * 5.42f, .012f);
                Shell(8, floor + Vector3.right * side * unit * 1.75f,
                    Smooth(.08f,.45f,age), fade * .92f, strike, age, unit * 6.75f, unit * 4.18f, .011f);
                layers.Draw(9, top, unit * (1.96f + .30f * strike), build * fade);
                layers.Burst(floor + new Vector3(side * unit * 1.92f, unit * 1.7f), strikeAge, 13, unit * 7f,
                    unit * .45f, FiredClay, life: .38f);
                break;
            case EnemyAttackVfxStyle.ThornArmor:
                Shell(17, floor, build, fade * .80f, strike, age, unit * 8.42f, unit * 5.22f, .012f);
                layers.Burst(floor, age, 18, unit * 8f, unit * .50f, Rocky, life: .47f);
                break;
            case EnemyAttackVfxStyle.MagmaArmor:
                Shell(22, floor, build, fade * .68f, strike, age, unit * 8.5f, unit * 6f, .038f);
                layers.Orbit(floor + Vector3.up * unit * 1.9f, age * 1.4f, unit * 2.6f, unit * 1.53f,
                    Hot, build * fade * .78f);
                var impact = floor + new Vector3(side * unit * 1.9f, unit * 1.65f);
                if (owner.currentShield > 0f)
                    Burst(layers, 23, impact, strikeAge, unit, 3.6f, Hot, 13, 6.9f, .25f);
                break;
            case EnemyAttackVfxStyle.Earthquake:
                layers.Draw(33, bounds.center + Vector3.up * unit * .3f, unit * Mathf.Lerp(3f, 6.6f, build),
                    build * fade * Mathf.Max(.40f, 1f - Smooth(.28f, .71f, age) + strike * .35f), age,
                    reveal: build, radial: true);
                break;
        }
        layers.Ring(floor, age, .64f, unit * 8.6f, .20f, style == EnemyAttackVfxStyle.MagmaArmor ? Hot : Gold);
        layers.EndFrame(false);
    }

    private void Shell(int sprite, Vector3 floor, float build, float alpha, float strike, float age,
        float width, float height, float warp)
    {
        float tall = height * Mathf.Lerp(.21f, 1f, build) * (1f - strike * .10f);
        width *= Mathf.Lerp(.44f, 1f, build) * (1f + strike * .055f);
        var point = floor + new Vector3(Mathf.Sin(age * 36f) * strike * width * .017f, tall * .5f);
        layers.Draw(sprite, point, width, build * alpha, age, height: tall,
            warp: warp + strike * .038f, reveal: build, rise: true);
    }

    private void OnDisable()
    {
        if (worldRoot) worldRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        layers?.Dispose();
        if (worldRoot) Destroy(worldRoot);
    }
}
