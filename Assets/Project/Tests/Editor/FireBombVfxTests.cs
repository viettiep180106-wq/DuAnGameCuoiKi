using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class FireBombVfxTests
{
    [Test]
    public void FireProjectile_ReferencesTwoFlightSpritesAndASeparateBurstPrefab()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Project/Resources/Prefab/Base/Skill/Passive/fire.prefab");
        var serialized = new SerializedObject(prefab.GetComponent<SkillFire>());
        Assert.That(serialized.FindProperty("emberSprite").objectReferenceValue.name, Is.EqualTo("Fire_01_Ember"));
        Assert.That(serialized.FindProperty("fireballSprite").objectReferenceValue.name, Is.EqualTo("Fire_02_Orb"));
        Assert.That(((Sprite)serialized.FindProperty("emberSprite").objectReferenceValue).pixelsPerUnit,
            Is.EqualTo(640f / 3f).Within(0.001f));
        Assert.That(((Sprite)serialized.FindProperty("fireballSprite").objectReferenceValue).pixelsPerUnit,
            Is.EqualTo(640f / 3f).Within(0.001f));
        var burst = (FireBombBurst)serialized.FindProperty("burstPrefab").objectReferenceValue;
        Assert.That(AssetDatabase.GetAssetPath(burst), Does.EndWith("/VFX/FireBombBurst.prefab"));
        Assert.That(prefab.GetComponent<TrailRenderer>(), Is.Not.Null);
    }

    [Test]
    public void FireBurst_ResetsItsSpriteAndParticlesWhenReused()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Project/Resources/Prefab/VFX/FireBombBurst.prefab");
        var instance = Object.Instantiate(prefab);
        try
        {
            var burst = instance.GetComponent<FireBombBurst>();
            var renderer = instance.GetComponent<SpriteRenderer>();
            var particles = instance.GetComponentsInChildren<ParticleSystem>();
            Assert.That(particles, Has.Length.EqualTo(2));

            Invoke(burst, "Awake");
            Invoke(burst, "Play", 2.6f, null);
            Assert.That(instance.transform.localScale.x, Is.EqualTo(2.6f / 1.7f * 3f).Within(0.001f));
            Assert.That(renderer.sprite.name, Is.EqualTo("Fire_03_Flash"));
            Invoke(burst, "Advance", 0.1f);
            Assert.That(renderer.sprite.name, Is.EqualTo("Fire_04_Burst"));
            foreach (var system in particles)
            {
                Assert.That(system.GetComponent<ParticleSystemRenderer>().sharedMaterial.shader.isSupported, Is.True);
                system.Simulate(0.08f);
                Assert.That(system.particleCount, Is.GreaterThan(0));
            }
            Invoke(burst, "Advance", 0.3f);
            Assert.That(renderer.sprite.name, Is.EqualTo("Fire_05_Smoke"));
            Assert.That(renderer.color.a, Is.LessThan(1f));

            burst.OnDespawn();
            Assert.That(renderer.enabled, Is.False);
            foreach (var system in particles)
                Assert.That(system.particleCount, Is.Zero);

            Invoke(burst, "Play", 2.6f, null);
            Assert.That(renderer.enabled, Is.True);
            Assert.That(renderer.sprite.name, Is.EqualTo("Fire_03_Flash"));
            Assert.That(renderer.color.a, Is.EqualTo(1f));
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static void Invoke(FireBombBurst component, string method, params object[] args)
    {
        typeof(FireBombBurst).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(component, args);
    }
}
