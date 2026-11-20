using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class IceRainStrikeVisualTests
{
    [Test]
    public void EquippedIcePrefab_UsesConfiguredStrikeVisual()
    {
        var projectile = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Project/Resources/Prefab/Base/Skill/Passive/Ice.prefab");
        var settings = new SerializedObject(projectile.GetComponent<SkillIce>());
        var strike = (IceRainStrikeVisual)settings.FindProperty("strikePrefab").objectReferenceValue;
        Assert.That(strike, Is.Not.Null);
        Assert.That(AssetDatabase.GetAssetPath(strike), Does.EndWith("/VFX/IceRainStrike.prefab"));
        Assert.That(settings.FindProperty("boltDelay").floatValue, Is.EqualTo(0.08f));
        var configured = typeof(IceRainStrikeVisual).GetProperty("IsConfigured",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(configured.GetValue(strike), Is.EqualTo(true));
    }

    [Test]
    public void Strike_ResolvesOnceAtContact_AndClearsItsCallbackWhenPooled()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Project/Resources/Prefab/VFX/IceRainStrike.prefab").GetComponent<IceRainStrikeVisual>();
        var poolObject = new GameObject("Ice visual test pool");
        var pool = poolObject.AddComponent<ObjectPoolingManager>();
        var strike = pool.Spawn(prefab, new Vector3(3f, 6f, 985f), Quaternion.identity);
        try
        {
            int hits = 0;
            Invoke(strike, "Play", pool, (Func<bool>)(() => { hits++; return true; }));
            Invoke(strike, "Advance", 0.39f);
            Assert.That(strike.transform.Find("Ice cloud").GetComponent<SpriteRenderer>().enabled, Is.True);
            Assert.That(strike.transform.Find("Ice bolt tip").GetComponent<SpriteRenderer>().enabled, Is.False);
            Invoke(strike, "Advance", 0.10f);
            Assert.That(strike.transform.Find("Ice bolt tip").GetComponent<SpriteRenderer>().enabled, Is.True);
            Assert.That(hits, Is.Zero);
            Invoke(strike, "Advance", 0.12f);
            Assert.That(hits, Is.EqualTo(1));
            Assert.That(strike.transform.Find("Ice contact").GetComponent<SpriteRenderer>().enabled, Is.True);
            Invoke(strike, "Advance", 0.80f);
            Assert.That(strike.gameObject.activeSelf, Is.False);
            Assert.That(hits, Is.EqualTo(1));

            var reused = pool.Spawn(prefab, Vector3.zero, Quaternion.identity);
            Assert.That(reused, Is.SameAs(strike));
            Invoke(reused, "Play", pool, (Func<bool>)(() => false));
            Invoke(reused, "Advance", 0.61f);
            Assert.That(reused.gameObject.activeSelf, Is.False);
            Assert.That(hits, Is.EqualTo(1));
            Assert.That(reused.transform.Find("Ice contact").GetComponent<SpriteRenderer>().enabled, Is.False);
        }
        finally
        {
            if (strike)
                Object.DestroyImmediate(strike.gameObject);
            Object.DestroyImmediate(poolObject);
        }
    }

    private static void Invoke(IceRainStrikeVisual strike, string method, params object[] args)
    {
        typeof(IceRainStrikeVisual).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(strike, args);
    }
}
