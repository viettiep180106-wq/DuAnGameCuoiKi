using UnityEngine;
using UnityEngine.SceneManagement;
using static EnemyWoodFx;
using static EnemyAtlasVfxLayers;

internal sealed class EnemyWoodStatusVfx : MonoBehaviour
{
    internal enum Style { Bark, Attack, Defense, AncientShield, Summon }
    private const float Twice = EnemyWoodAttackVfx.DurationMultiplier;
    private EnemyBase enemy;
    private Player player;
    private EnemyAtlasVfxLayers layers;
    private GameObject worldRoot;
    private float barkAt = float.PositiveInfinity;
    private float attackAt = float.PositiveInfinity;
    private float defenseAt = float.PositiveInfinity;
    private float shieldAt = float.PositiveInfinity;
    private float summonAt = float.PositiveInfinity;
    private float impactAt = float.NegativeInfinity;
    private float brokeAt = float.PositiveInfinity;
    private float poisonAt;
    private int previousPoisonRounds;
    private float poisonTickAt = float.NegativeInfinity;
    private ActiveRoundEffect poison;
    private bool curse;

    internal static void ShowEnemy(EnemyAtlasVfxArt art, EnemyBase actor, Style style)
    {
        if (!Application.isPlaying || !Alive(actor)) return;
        var fx = Prepare(art, actor);
        fx.enemy = actor;
        switch (style)
        {
            case Style.Bark: fx.barkAt = Time.time; fx.brokeAt = float.PositiveInfinity; break;
            case Style.Attack: fx.attackAt = Time.time; break;
            case Style.Defense: fx.defenseAt = Time.time; break;
            case Style.AncientShield: fx.shieldAt = Time.time; break;
            case Style.Summon: fx.summonAt = Time.time; break;
        }
    }

    internal static void ShowPoison(EnemyAtlasVfxArt art, Player actor, bool curse)
    {
        if (!Application.isPlaying || !actor || actor.CurrentHP <= 0 || !actor.gameObject.activeInHierarchy) return;
        ActiveRoundEffect effect = actor.FindRoundEffect(Effect.DoT, ElementType.Wood);
        if (effect == null || effect.RoundsRemaining <= 0) return;
        var fx = Prepare(art, actor);
        fx.player = actor;
        fx.curse = curse || (ReferenceEquals(fx.poison, effect) && fx.curse);
        fx.poison = effect;
        fx.poisonAt = Time.time;
        fx.previousPoisonRounds = effect.RoundsRemaining;
        fx.poisonTickAt = float.NegativeInfinity;
    }

    internal static void OnShieldHit(EnemyBase actor, bool broke)
    {
        if (!actor.TryGetComponent(out EnemyWoodStatusVfx fx) || !fx.isActiveAndEnabled) return;
        if (float.IsPositiveInfinity(fx.barkAt) && float.IsPositiveInfinity(fx.shieldAt)) return;
        fx.impactAt = Time.time;
        if (broke) fx.brokeAt = Time.time;
    }

    internal static void ShowSummon(EnemyBase caster, EnemyBase summoned)
    {
        if (!Application.isPlaying || !caster.enemyData || caster.enemyData.activeSkillVfx != EnemyAttackVfxStyle.AncientRevival ||
            !Alive(summoned) || summoned.type != EnemyType.BoRac) return;
        var art = EnemyWoodVfxArt.Load();
        if (art != null) ShowEnemy(art, summoned, Style.Summon);
    }

    private static EnemyWoodStatusVfx Prepare(EnemyAtlasVfxArt art, BaseCharacter actor)
    {
        if (!actor.TryGetComponent(out EnemyWoodStatusVfx fx))
            fx = actor.gameObject.AddComponent<EnemyWoodStatusVfx>();
        if (!fx.worldRoot)
        {
            fx.worldRoot = new GameObject("Wood Status VFX") { hideFlags = HideFlags.DontSave };
            SceneManager.MoveGameObjectToScene(fx.worldRoot, actor.gameObject.scene);
            fx.layers = new EnemyAtlasVfxLayers(fx.worldRoot.transform, art);
        }
        var sprite = actor.spriteRenderer;
        int order = sprite ? sprite.sortingOrder : 35;
        fx.layers.SetSorting(sprite ? sprite.sortingLayerID : 0, order + 2, order - 1);
        fx.worldRoot.SetActive(true);
        fx.enabled = true;
        return fx;
    }

    private void LateUpdate()
    {
        if (!worldRoot) { enabled = false; return; }
        layers.BeginFrame();
        bool shown = player ? DrawPlayer() : Alive(enemy) && DrawEnemy();
        layers.EndFrame(false);
        if (!shown) enabled = false;
    }

    private bool DrawEnemy()
    {
        float now = Time.time;
        bool hasShield = enemy.shieldRounds > 0 && enemy.currentShield > 0f;
        bool bark = hasShield && !float.IsPositiveInfinity(barkAt);
        bool shield = hasShield && !float.IsPositiveInfinity(shieldAt);
        bool attack = enemy.atkBuffRounds > 0 && enemy.atkBuffPercent > 0f && !float.IsPositiveInfinity(attackAt);
        bool defense = enemy.defBuffRounds > 0 && enemy.defBuffPercent > 0f && !float.IsPositiveInfinity(defenseAt);
        if (!bark) barkAt = float.PositiveInfinity;
        if (!shield) shieldAt = float.PositiveInfinity;
        if (!attack) attackAt = float.PositiveInfinity;
        if (!defense) defenseAt = float.PositiveInfinity;
        float shatter = (now - brokeAt) / Twice;
        bool breakDust = shatter >= 0f && shatter < .47f;
        float sprout = (now - summonAt) / Twice - .19f;
        bool summoned = !float.IsPositiveInfinity(summonAt) && sprout < .74f;
        if (!summoned) summonAt = float.PositiveInfinity;
        if (!(bark || shield || defense || attack || breakDust || summoned)) return false;

        var bounds = EnemyAttackVfx.CharacterBounds(enemy);
        float unit = Mathf.Clamp(bounds.size.y / 5.6f, .55f, 2.4f);
        var floor = Feet(bounds);
        float hit = (now - impactAt) / Twice;
        float flash = Pulse(hit,0f,.048f,.079f,.26f);
        if (bark)
        {
            float a = (now - barkAt) / Twice;
            float build = Smooth(0f,.37f,a);
            Ward(8,floor,unit*7.3f,unit*2.55f,build,.89f,a,0f);
            Ward(7,floor,unit*7.5f,unit*5.20f,build,.64f+flash*.26f,a,flash);
            layers.Draw(9,bounds.center+Vector3.up*unit*3.16f,unit*(2.01f+flash*.52f),build*.83f,a);
        }
        if (shield && !bark)
            Ward(33,floor,unit*7.3f,unit*4.6f,Smooth(.18f,.57f,(now-shieldAt)/Twice),.55f+flash*.24f,(now-shieldAt)/Twice,flash);
        if (defense)
            Ward(28,floor,unit*6.7f,unit*5.3f,Smooth(.13f,.50f,(now-defenseAt)/Twice),.62f,(now-defenseAt)/Twice,0f);
        if (attack)
        {
            float a=(now-attackAt)/Twice-.22f;
            float wind=Pulse(a,0f,.19f,.75f,1.28f);
            layers.Draw(4,floor+Vector3.up*unit*.62f,unit*4.55f,wind, a,height:unit*2.1f,warp:.042f,ground:true);
            layers.Orbit(floor,a,unit*2.10f,unit*.67f,Leaf,wind*.68f);
            float rise=Smooth(.18f,.69f,a);
            layers.Draw(3,floor+Vector3.up*unit*Mathf.Lerp(1.6f,6.04f,rise),unit*Mathf.Lerp(2.1f,1.14f,rise),
                .65f*Smooth(.17f,.32f,a),a);
        }
        if (breakDust)
            layers.Burst(floor+Vector3.up*unit*2.2f,shatter,23,unit*10f,unit*.45f,Grain,life:.47f);
        if (summoned)
            Revival(layers,34,floor,sprout,unit);
        return true;
    }

    private void Ward(int cell, Vector3 floor, float width, float height, float build, float opacity, float age, float strike)
    {
        float grown = height*Mathf.Lerp(.24f,1f,build)*(1f-strike*.12f);
        layers.Draw(cell,floor+new Vector3(Mathf.Sin(age*36f)*strike*.29f,grown*.5f),
            width*Mathf.Lerp(.40f,1f,build),build*opacity,age,height:grown,warp:.015f+strike*.030f,
            rise:true,reveal:build);
    }

    private bool DrawPlayer()
    {
        if (player.CurrentHP <= 0f || poison == null || poison.RoundsRemaining <= 0 || !player.ActiveRoundEffects.Contains(poison))
            return false;
        float now = Time.time;
        if (poison.RoundsRemaining != previousPoisonRounds)
        {
            if (poison.RoundsRemaining < previousPoisonRounds) poisonTickAt = now;
            previousPoisonRounds = poison.RoundsRemaining;
        }
        float age = (now-poisonAt)/Twice;
        float pulse=Pulse((now-poisonTickAt)/Twice,0f,.061f,.109f,.37f);
        var bounds=EnemyAttackVfx.CharacterBounds(player);
        float unit=Mathf.Clamp(bounds.size.y/5.6f,.61f,2.2f);
        Vector3 center=bounds.center+Vector3.up*(bounds.extents.y+unit*(1.23f+.19f*Mathf.Sin(age*6f)));
        layers.Draw(curse?29:14,center,unit*(curse?2.21f:1.73f)*(1f+.27f*pulse),
            Smooth(.04f,.19f,age)*(curse?.98f:.75f),age,angle:curse?Mathf.Sin(age*3f)*15f:0f,warp:curse?.012f:.032f);
        layers.Orbit(center,age,unit*.86f,unit*.53f,Violet,Smooth(.04f,.27f,age)*(.48f+.44f*pulse),8);
        return true;
    }

    private void OnDisable()
    {
        if (worldRoot) worldRoot.SetActive(false);
        if (gameObject.activeInHierarchy) return;
        barkAt = attackAt = defenseAt = shieldAt = summonAt = brokeAt = float.PositiveInfinity;
        impactAt = poisonTickAt = float.NegativeInfinity;
        poison = null;
        curse = false;
    }

    private void OnDestroy()
    {
        layers?.Dispose();
        if (worldRoot) Destroy(worldRoot);
    }
}
