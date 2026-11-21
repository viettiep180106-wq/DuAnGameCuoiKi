using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class EarthSlamVisualTests
{
    [Test]
    public void EarthSkillPrefab_UsesTheApprovedVisualConfiguration()
    {
        var skill = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Project/Resources/Prefab/Base/Skill/Passive/earth.prefab").GetComponent<SkillEarth>();
        var settings = new SerializedObject(skill);
        var visual = (EarthSlamVisual)settings.FindProperty("visualPrefab").objectReferenceValue;
        Assert.That(visual, Is.Not.Null);
        var inputs = new SerializedObject(visual);
        Assert.That(inputs.FindProperty("boulderSize").floatValue, Is.EqualTo(12f));
        Assert.That(inputs.FindProperty("impactWidth").floatValue, Is.EqualTo(17f));
        Assert.That(inputs.FindProperty("smokeShader").objectReferenceValue, Is.Not.Null);
        var setup = typeof(EarthSlamVisual).GetProperty("IsConfigured", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(setup.GetValue(visual), Is.EqualTo(true));
        Assert.That(skill.impactRadius, Is.EqualTo(3f));
    }

    [Test]
    public void EarthVisual_HandsOffFromFallToImpact_AndResetsForReuse()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Project/Resources/Prefab/VFX/EarthSlamVisual.prefab").GetComponent<EarthSlamVisual>();
        var poolObject = new GameObject("Earth visual test pool");
        var pool = poolObject.AddComponent<ObjectPoolingManager>();
        var visual = pool.Spawn(prefab, new Vector3(3f, 4f, 985f), Quaternion.identity);
        try
        {
            Invoke(visual, "BeginFall", pool, 22f);
            var meteor = RenderPart(visual, "Falling meteor");
            var flash = RenderPart(visual, "Expanding fracture flash");
            Assert.That(meteor.enabled, Is.True);
            Assert.That(RenderPart(visual, "Boulder contact").enabled, Is.False);
            Assert.That(flash.enabled, Is.False);

            Invoke(visual, "MarkTarget", new Vector3(1f, 3f, 990f));
            Invoke(visual, "MarkTarget", new Vector3(7f, 5f, 990f));
            Invoke(visual, "BeginImpact");
            Invoke(visual, "AdvanceImpact", 0.42f);
            Assert.That(meteor.enabled, Is.False);
            Assert.That(flash.enabled, Is.True);
            var crack = visual.transform.Find("Fracture").GetComponent<LineRenderer>();
            Assert.That(crack.enabled, Is.True);
            Assert.That(crack.GetPosition(crack.positionCount - 1).z, Is.EqualTo(985f));

            Invoke(visual, "AdvanceImpact", 1f);
            Assert.That(visual.gameObject.activeSelf, Is.False);
            Assert.That(flash.enabled, Is.False);
            Assert.That(crack.enabled, Is.False);

            var reused = pool.Spawn(prefab, Vector3.zero, Quaternion.identity);
            Assert.That(reused, Is.SameAs(visual));
            Invoke(reused, "BeginFall", pool, 22f);
            Assert.That(meteor.enabled, Is.True);
            Assert.That(crack.enabled, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(visual.gameObject);
            Object.DestroyImmediate(poolObject);
        }
    }

    private static SpriteRenderer RenderPart(EarthSlamVisual visual, string child) =>
        visual.transform.Find(child).GetComponent<SpriteRenderer>();

    private static void Invoke(EarthSlamVisual visual, string method, params object[] arguments) =>
        typeof(EarthSlamVisual).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(visual, arguments);
}
