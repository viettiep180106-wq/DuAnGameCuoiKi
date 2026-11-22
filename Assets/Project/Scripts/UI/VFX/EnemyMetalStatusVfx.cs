using UnityEngine;
using UnityEngine.SceneManagement;
using static EnemyAtlasVfxLayers;
using static EnemyMetalFx;

internal sealed class EnemyMetalStatusVfx : MonoBehaviour
{
    private EnemyBase enemy;
    private Player player;
    private EnemyAttackVfxStyle style;
    private ActiveRoundEffect bleed;
    private int bleedRounds;
    private GameObject root;
    private EnemyAtlasVfxLayers layers;
    private float startedAt;
    private float hitAt = float.NegativeInfinity;
    private float breakAt = float.NegativeInfinity;
    private float expiredAt = float.PositiveInfinity;

    internal static void ShowArmor(EnemyAtlasVfxArt art, EnemyBase actor, EnemyAttackVfxStyle kind)
    {
        if (!Application.isPlaying || actor.CurrentHP <= 0f) return;
        bool active = kind == EnemyAttackVfxStyle.IronBarrier
            ? actor.currentShield > 0f && actor.shieldRounds > 0
            : actor.damageReductionPercent > 0f && actor.damageReductionRounds > 0;
        if (!active) return;
        var fx = Prepare(art, actor);
        fx.enemy = actor;
        fx.style = kind;
    }

    internal static void ShowBleed(EnemyAtlasVfxArt art, Player actor)
    {
        if (!Application.isPlaying || actor.CurrentHP <= 0f) return;
        var applied = actor.ActiveRoundEffects.Find(effect =>
            effect.EffectType == Effect.Bleed && effect.RoundsRemaining > 0);
        if (applied == null) return;
        var fx = Prepare(art, actor);
        fx.player = actor;
        fx.bleed = applied;
        fx.bleedRounds = applied.RoundsRemaining;
    }

    internal static void OnArmorHit(EnemyBase actor, bool shieldHit, bool broke = false)
    {
        if (!actor.TryGetComponent(out EnemyMetalStatusVfx fx) || !fx.isActiveAndEnabled ||
            (fx.style == EnemyAttackVfxStyle.IronBarrier) != shieldHit) return;
        fx.hitAt = Time.time;
        if (broke) fx.breakAt = Time.time;
    }

    private static EnemyMetalStatusVfx Prepare(EnemyAtlasVfxArt art, BaseCharacter actor)
    {
        if (!actor.TryGetComponent(out EnemyMetalStatusVfx fx))
            fx = actor.gameObject.AddComponent<EnemyMetalStatusVfx>();
        if (!fx.root)
        {
            fx.root = new GameObject("Metal Status VFX") { hideFlags = HideFlags.DontSave };
            SceneManager.MoveGameObjectToScene(fx.root, actor.gameObject.scene);
            fx.layers = new EnemyAtlasVfxLayers(fx.root.transform, art);
        }
        var sprite = actor.spriteRenderer;
        int order = sprite ? sprite.sortingOrder : 35;
        fx.layers.SetSorting(sprite ? sprite.sortingLayerID : 0, order + 2, order - 1);
        fx.startedAt = Time.time;
        fx.hitAt = fx.breakAt = float.NegativeInfinity;
        fx.expiredAt = float.PositiveInfinity;
        fx.root.SetActive(true);
        fx.enabled = true;
        return fx;
    }

    private void LateUpdate()
    {
        layers.BeginFrame();
        bool visible = player ? DrawBleed() : enemy && enemy.CurrentHP > 0f && DrawArmor();
        layers.EndFrame(false);
        if (!visible) enabled = false;
    }

    private bool DrawArmor()
    {
        bool isShield = style == EnemyAttackVfxStyle.IronBarrier;
        bool active = isShield ? enemy.shieldRounds > 0 && enemy.currentShield > 0f :
            enemy.damageReductionRounds > 0 && enemy.damageReductionPercent > 0f;
        float now = Time.time;
        if (!active && float.IsPositiveInfinity(expiredAt)) expiredAt = now;
        float left = active ? 1f : 1f-Smooth(0f,.29f,now-expiredAt);
        float shatter = now-breakAt;
        bool breaking = isShield && shatter >= 0f && shatter < .52f;
        if (left <= .004f && !breaking) return false;

        Bounds bounds = EnemyAttackVfx.CharacterBounds(enemy);
        float unit = Mathf.Clamp(bounds.size.y / 5.6f,.53f,2.4f);
        float t = now-startedAt;
        float swell = Smooth(0f,.45f,t);
        float flashAge = now-hitAt;
        float flash = Pulse(flashAge,0f,.04f,.09f,.31f);
        Vector3 floor = Feet(bounds);
        Vector3 front = bounds.center;
        if (Player.Instance)
            front.x += (Player.Instance.transform.position.x <= enemy.transform.position.x ? -1f : 1f) * bounds.size.x * .48f;

        if (style == EnemyAttackVfxStyle.SteelFortress)
        {
            float ring = Pulse(t,0f,.16f,.51f,1f) * left;
            layers.Draw(4,floor+Vector3.up*unit*.25f,unit*Mathf.Lerp(3.6f,9.46f,swell),ring,t,
                height:unit*Mathf.Lerp(1.08f,3.30f,swell),angle:Mathf.Sin(t*4f)*2.7f,ground:true);
            Badge(3,bounds,t,unit,7.55f);
            Ward(2,floor,unit,Mathf.Lerp(4.76f,12.38f,swell),Mathf.Lerp(2.42f,7.84f,swell),
                swell*left*(.56f+flash*.37f),t,flash);
        }
        else if (isShield)
        {
            Badge(13,bounds,t,unit,5.88f);
            float rise = Pulse(t,.02f,.16f,.60f,.90f);
            layers.Draw(14,floor+Vector3.up*unit*2.9f,unit*5.73f,rise*.46f,t,warp:.066f);
            for (int i = 0; i < 13; i++)
            {
                float progress=Mathf.Repeat(t*1.7f+Seed(i+11),1f);
                Vector3 particle=floor+new Vector3((Seed(i)-.5f)*unit*5.08f,progress*unit*7.26f);
                layers.Particle(particle,unit*(.2f+.15f*Seed(i+3)),Gold,rise*Mathf.Sin(progress*Mathf.PI)*.88f);
            }
            layers.Draw(12,bounds.center,unit*Mathf.Lerp(3.88f,8.30f,swell)*(1f+flash*.06f),
                swell * (breaking ? 1f-Smooth(0f,.16f,shatter) : left) * (.57f+flash*.36f),t,
                height:unit*Mathf.Lerp(3.69f,6.92f,swell),warp:.02f+flash*.033f);
            if (breaking) BurstArt(layers,9,front,shatter,unit*8.61f,Gold,.43f);
        }
        else
        {
            layers.Ring(floor,t,.62f,unit*11.08f,.22f,Silver);
            Ward(22,floor,unit,Mathf.Lerp(4.46f,10.50f,swell),Mathf.Lerp(3.42f,8.12f,swell),
                swell*left*(.81f+.18f*flash),t,flash);
        }

        Color tint = isShield ? Gold : style == EnemyAttackVfxStyle.CounterCage ? Crimson : Cyan;
        Flash(layers,6,front,flashAge,unit*1.95f,tint,.25f);
        layers.Ring(front,flashAge,.38f,unit*5.8f*flash+.1f,.83f,tint,ground:false);
        return true;
    }

    private void Badge(int cell, Bounds bounds, float age, float unit, float top)
    {
        float raise = Smooth(.04f,.44f,age);
        Vector3 center = new(bounds.center.x, bounds.min.y+unit*Mathf.Lerp(2.7f,top,raise), bounds.center.z);
        layers.Draw(cell,center,unit*Mathf.Lerp(1.46f,3.2f,raise),Pulse(age,.07f,.26f,.55f,1.01f));
    }

    private void Ward(int cell, Vector3 floor, float unit, float width, float height, float alpha, float time, float flash)
    {
        layers.Draw(cell,floor+Vector3.up*(unit*height*.49f),unit*width*(1f+flash*.05f),alpha,time,
            height:unit*height,warp:.015f+flash*.033f);
    }

    private bool DrawBleed()
    {
        if (player.CurrentHP <= 0f || bleed == null) return false;
        int remaining = player.ActiveRoundEffects.Contains(bleed) ? bleed.RoundsRemaining : 0;
        if (remaining < bleedRounds) hitAt = Time.time;
        bleedRounds = remaining;
        float age = Time.time-startedAt;
        float tick = Time.time-hitAt;
        float alpha = Smooth(.09f,.24f,age) * (remaining>0 ? 1f : 1f-Smooth(.16f,.55f,tick));
        if (remaining <= 0 && tick >= .55f) return false;
        float flash = Pulse(tick,0f,.08f,.17f,.37f);
        Bounds bounds = EnemyAttackVfx.CharacterBounds(player);
        float unit = Mathf.Clamp(bounds.size.y/5.6f,.60f,2.2f);
        Vector3 center = new(bounds.center.x,bounds.max.y+unit*(.89f+Mathf.Sin(age*8f)*.1f),bounds.center.z);
        layers.Draw(29,center,unit*(1.62f+.50f*flash),alpha*.89f,age,warp:flash*.022f);
        layers.Burst(center,tick,6,unit*3.7f,unit*.23f,Crimson,.42f,heading:-90f,spread:37f,gravity:10f);
        return true;
    }

    private void OnDisable()
    {
        if (root) root.SetActive(false);
        bleed = null;
    }

    private void OnDestroy()
    {
        layers?.Dispose();
        if (root) Destroy(root);
    }
}

internal sealed class EnemyMetalCounterVfx : MonoBehaviour
{
    private EnemyAtlasVfxLayers layers;
    private Vector3 source;
    private Vector3 impact;
    private Vector3 feet;
    private float startedAt;
    private float scale;

    internal static void Play(EnemyBase enemy, Player player)
    {
        if (!Application.isPlaying || !player || !enemy || !enemy.enemyData || enemy.enemyData.activeSkillVfx != EnemyAttackVfxStyle.CounterCage ||
            enemy.reflectBuffRounds <= 0 || enemy.temporaryReflectBuff <= 0f) return;
        var art = EnemyMetalVfxArt.Load();
        if (art == null) return;
        var root = new GameObject("Metal Counter VFX") { hideFlags = HideFlags.DontSave };
        SceneManager.MoveGameObjectToScene(root, enemy.gameObject.scene);
        var fx = root.AddComponent<EnemyMetalCounterVfx>();
        fx.layers = new EnemyAtlasVfxLayers(root.transform, art);
        var playerSprite = player.spriteRenderer;
        int order = playerSprite ? playerSprite.sortingOrder : 35;
        fx.layers.SetSorting(playerSprite ? playerSprite.sortingLayerID : 0, Mathf.Max(36,order+2),order-1);
        var from = EnemyAttackVfx.CharacterBounds(enemy);
        var to = EnemyAttackVfx.CharacterBounds(player);
        var facing = to.center.x <= from.center.x ? -1f : 1f;
        fx.source = from.center + new Vector3(facing*from.extents.x*.74f,from.size.y*.085f);
        fx.impact = to.center;
        fx.feet = Feet(to);
        fx.scale = Mathf.Clamp(to.size.y/5.6f,.58f,2.2f);
        fx.startedAt = Time.time;
    }

    private void LateUpdate()
    {
        float t = Time.time-startedAt;
        if (t > .86f) { Destroy(gameObject); return; }
        layers.BeginFrame();
        float fraction = Progress(.06f,.31f,t);
        float alpha = Pulse(t,.04f,.083f,.29f,.367f);
        Vector3 axis = (impact-source).normalized;
        for (int i = 0; i < 5; i++)
        {
            float k = Mathf.Max(0f,fraction-i*.096f);
            Vector3 tip = Vector3.Lerp(source,impact,k) + Vector3.up*(Mathf.Sin(k*Mathf.PI)*(i-2)*1.05f*scale);
            Streak(layers,tip-axis*(2.9f*scale),tip,scale*.27f,alpha*.51f,t,Crimson);
            layers.Draw(23,tip,scale*Mathf.Lerp(1.65f,2.6f,k)*(1f-i*.12f),alpha*(1f-i*.16f),
                angle:(6f+i*25f)*(axis.x<0f?-1f:1f),flip:axis.x<0f);
        }
        float hit = t-.31f;
        BurstArt(layers,24,impact,hit,8.7f*scale,Crimson,.34f);
        layers.Ring(feet,hit,.44f,11.76f*scale,.23f,Crimson);
        layers.EndFrame(false);
    }

    private void OnDestroy() => layers?.Dispose();
}
