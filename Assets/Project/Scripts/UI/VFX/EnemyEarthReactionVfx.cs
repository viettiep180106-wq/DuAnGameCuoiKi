using UnityEngine;
using UnityEngine.SceneManagement;
using static EnemyEarthFx;
using static EnemyAtlasVfxLayers;

internal sealed class EnemyEarthReactionVfx : MonoBehaviour
{
    private enum Reaction { StoneBreak, LavaBreak, LavaTravel, Thorns }
    private EnemyAtlasVfxLayers layers;
    private Reaction kind;
    private Vector3 from;
    private Vector3 to;
    private float scale;
    private float startedAt;

    internal static void PlayBreak(EnemyBase caster, EnemyAtlasVfxArt art, bool lava) =>
        StartReaction(caster, null, art, lava ? Reaction.LavaBreak : Reaction.StoneBreak);

    internal static bool TryPlayLavaTravel(EnemyBase caster, Player target)
    {
        if (!Application.isPlaying || caster.type != EnemyType.HatNhamThach || !target || !caster.enemyData ||
            caster.enemyData.activeSkillVfx != EnemyAttackVfxStyle.MagmaArmor) return false;
        var art = EnemyEarthVfxArt.Load();
        if (art == null) return false;
        StartReaction(caster, target, art, Reaction.LavaTravel);
        return true;
    }

    internal static void PlayThorns(EnemyBase caster, Player target)
    {
        if (!Application.isPlaying || caster.type != EnemyType.BoCapDa || !target || caster.reflectBuffRounds <= 0 ||
            caster.temporaryReflectBuff <= 0f || !caster.enemyData || caster.enemyData.activeSkillVfx != EnemyAttackVfxStyle.ThornArmor)
            return;
        var art = EnemyEarthVfxArt.Load();
        if (art != null) StartReaction(caster, target, art, Reaction.Thorns);
    }

    private static void StartReaction(EnemyBase caster, Player target, EnemyAtlasVfxArt art, Reaction kind)
    {
        var root = new GameObject("Earth Hit Reaction VFX") { hideFlags = HideFlags.DontSave };
        SceneManager.MoveGameObjectToScene(root, caster.gameObject.scene);
        var fx = root.AddComponent<EnemyEarthReactionVfx>();
        fx.kind = kind;
        fx.startedAt = Time.time;
        fx.layers = new EnemyAtlasVfxLayers(root.transform, art);
        Bounds source = EnemyAttackVfx.CharacterBounds(caster);
        Bounds end = target ? EnemyAttackVfx.CharacterBounds(target) : source;
        fx.scale = Mathf.Clamp(end.size.y / 5.6f, .7f, 2.15f);
        fx.from = new Vector3(source.center.x, source.min.y + source.size.y * .07f, source.center.z);
        fx.to = new Vector3(end.center.x, end.min.y + end.size.y * .09f, end.center.z);
        var renderer = target ? target.spriteRenderer : caster.spriteRenderer;
        int layer = renderer ? renderer.sortingLayerID : 0;
        int order = renderer ? renderer.sortingOrder : 35;
        fx.layers.SetSorting(layer, Mathf.Max(36, order + 2), order - 1);
    }

    private void LateUpdate()
    {
        float age = (Time.time - startedAt) / EnemyEarthAttackVfx.DurationMultiplier;
        if (age >= (kind == Reaction.LavaTravel ? 1.16f : .92f)) { Destroy(gameObject); return; }
        layers.BeginFrame();
        switch (kind)
        {
            case Reaction.StoneBreak:
                Burst(layers, 6, from, age, scale, 5.2f, FiredClay, 23, 10.3f, .47f);
                layers.Ring(from, age, .50f, scale * 6.7f, .21f, Gold);
                break;
            case Reaction.LavaBreak:
                Burst(layers, 24, from, age, scale, 10.3f, Hot, 45, 17.8f, .66f);
                layers.Ring(from, age, .77f, scale * 15f, .21f, Hot);
                layers.Ring(from, age - .10f, .63f, scale * 10.2f, .16f, Gold);
                break;
            case Reaction.LavaTravel:
                float travel = Smooth(.07f, .34f, age);
                Vector3 heat = Vector3.Lerp(from, to, travel) + Vector3.up * scale * (1.4f + Mathf.Sin(travel * Mathf.PI) * .84f);
                layers.Draw(23, heat, scale * 4.72f, Pulse(age,.076f,.17f,.28f,.41f), age, angle: Angle(to - from) + 103f,
                    warp: .041f);
                Burst(layers, 21, to, age - .26f, scale, 5.14f, Hot, 22, 11f, .45f);
                break;
            case Reaction.Thorns:
                float reach = Smooth(0f, .25f, age);
                Vector3 earth = Vector3.Lerp(from, to, reach);
                float height = scale * Mathf.Lerp(1.8f, 3.98f, reach);
                layers.Draw(18, earth + Vector3.up * height * .52f, scale * Mathf.Lerp(2.75f, 6.67f, reach),
                    Pulse(age,0f,.043f,.23f,.44f), age, height: height, reveal: reach, rise: true, warp: .012f);
                layers.Draw(19, to + Vector3.up * scale * 3f, scale * Mathf.Lerp(1.9f, 4.7f, Smooth(.19f,.36f,age)),
                    Pulse(age,.18f,.258f,.34f,.56f), age, angle: age * 82f);
                layers.Burst(to, age - .22f, 25, scale * 11.5f, scale * .43f, Rocky, life: .48f);
                break;
        }
        layers.EndFrame(true);
    }

    private void OnDestroy() => layers?.Dispose();
}
