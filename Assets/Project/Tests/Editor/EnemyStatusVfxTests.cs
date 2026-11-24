using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

public class EnemyStatusVfxTests
{
    [UnityTest]
    public IEnumerator PlayerStatuses_CreateRealLayersAndClearThemOnReuse()
    {
        yield return new EnterPlayMode();

        var target = new GameObject("Status VFX test enemy");
        var skillObject = new GameObject("Status VFX test Earth skill", typeof(BoxCollider2D));
        var white = Texture2D.whiteTexture;
        Sprite actorSprite = Sprite.Create(white, new Rect(0, 0, white.width, white.height), Vector2.one * .5f, white.width);
        try
        {
            target.SetActive(false);
            var renderer = target.AddComponent<SpriteRenderer>();
            renderer.sprite = actorSprite;
            var enemy = target.AddComponent<EnemyBase>();
            enemy.spriteRenderer = renderer;
            target.AddComponent<DoTEffect>();
            target.AddComponent<FreezeEffect>();
            target.AddComponent<StunEffect>();
            enemy.SetStatsForTest(5000f, 0f);
            target.SetActive(true);

            var freeze = new EffectPayLoad { EffectType = Effect.Freeze, duration = .6f, slowRate = .5f, value = 1f };
            enemy.PlayEffect(freeze);
            Assert.That(enemy.GetComponent("EnemyStatusVfx"), Is.Null);

            enemy.PlayEffect(new EffectPayLoad { EffectType = Effect.DoT, duration = .32f, tickRate = .08f, value = 5f });
            yield return WaitForGameTime(.12f);
            Assert.That(enemy.CurrentHP, Is.LessThan(5000f));
            var visual = enemy.GetComponent("EnemyStatusVfx");
            Assert.That(visual, Is.Not.Null);
            var group = (SortingGroup)visual.GetType().GetField("group", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(visual);
            var flame = group.transform.Find("Burn flames").GetComponent<SpriteRenderer>();
            Assert.That(flame.enabled, Is.True);
            Assert.That(flame.sharedMaterial.shader.isSupported, Is.True);

            enemy.moveTwen = target.transform.DOMoveX(10f, 6f);
            enemy.PlayEffect(freeze);
            Assert.That(enemy.moveTwen.timeScale, Is.EqualTo(.5f));
            enemy.PlayEffect(new EffectPayLoad { EffectType = Effect.Stun, duration = .32f });
            Assert.That(enemy.moveTwen.IsPlaying(), Is.False);

            var earth = skillObject.AddComponent<SkillEarth>();
            earth.Initialize(6f);
            enemy.currentDef = 100f;
            typeof(SkillEarth).GetMethod("ApplyCleaveSlam", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(earth, new object[] { new List<EnemyBase> { enemy } });
            Assert.That(enemy.CurrentDEF, Is.EqualTo(80f).Within(.001f));

            yield return WaitForGameTime(.14f);
            Assert.That(group.GetComponentInChildren<LineRenderer>().enabled, Is.True);
            var layers = group.GetComponentsInChildren<SpriteRenderer>();
            Assert.That(System.Array.Exists(layers, layer => layer.enabled && layer != flame && layer.transform.position.y < 0f), Is.True);
            Assert.That(System.Array.Exists(layers, layer => layer.enabled && layer.transform.position.y > .5f), Is.True);

            yield return WaitForGameTime(1.3f);
            Assert.That(enemy.moveTwen.timeScale, Is.EqualTo(1f));
            Assert.That(enemy.moveTwen.IsPlaying(), Is.True);
            Assert.That(flame.enabled, Is.False);
            Assert.That(group.GetComponentInChildren<LineRenderer>().enabled, Is.False);
            Assert.That(group.gameObject.activeSelf, Is.True);

            enemy.OnDespawn();
            target.SetActive(false);
            Assert.That(group.gameObject.activeSelf, Is.False);
            target.SetActive(true);
            yield return null;
            Assert.That(group.gameObject.activeSelf, Is.False);
        }
        finally
        {
            target.GetComponent<EnemyBase>().OnDespawn();
            Object.Destroy(target);
            Object.Destroy(skillObject);
            Object.Destroy(actorSprite);
        }
        yield return new ExitPlayMode();
    }

    private static IEnumerator WaitForGameTime(float duration)
    {
        float deadline = Time.time + duration;
        while (Time.time < deadline) yield return null;
        yield return null;
    }
}
