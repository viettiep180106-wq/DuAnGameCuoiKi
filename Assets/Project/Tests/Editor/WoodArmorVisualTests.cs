using System.Reflection;
using DG.Tweening;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class WoodArmorVisualTests
{
    private Player player;
    private Player previousPlayer;

    [SetUp]
    public void SetUp()
    {
        previousPlayer = Player.Instance;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Project/Resources/Prefab/Base/Player/Player.prefab");
        player = Object.Instantiate(prefab).GetComponent<Player>();
        Player.Instance = player;
        player.Element = ElementType.Wood;
        player.SetStatsForTest(500f, 200f);
        player.transform.localScale = Vector3.one * 5f;
    }

    [TearDown]
    public void TearDown()
    {
        if (player)
            Object.DestroyImmediate(player.gameObject);
        Player.Instance = previousPlayer;
    }

    [Test]
    public void Activation_AttachesOneVisual_AndKeepingParentScaleDoesNotEnlargeIt()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Project/Resources/Prefab/Base/Skill/Passive/wood.prefab");
        var skill = Object.Instantiate(prefab).GetComponent<SkillWood>();
        try
        {
            player.TakeDamage(100f);
            skill.Initialize(3f);
            skill.Active();
            Assert.That(player.CurrentHP, Is.EqualTo(420f).Within(0.01f));
            Assert.That(player.WoodArmorTier, Is.EqualTo(1));
            var visual = GetVisual();
            Assert.That(visual.transform.parent, Is.SameAs(player.transform));
            Assert.That(visual.transform.lossyScale.x, Is.EqualTo(1f).Within(0.001f));
            AssertMode(visual, WoodArmorVisual.Kind.Activate);
        }
        finally
        {
            DOTween.Kill(skill.gameObject.GetInstanceID());
            Object.DestroyImmediate(skill.gameObject);
        }
    }

    [Test]
    public void FullHealthAndShieldedHit_DoesNotStartAHealVisual()
    {
        player.WoodArmorTier = 1;
        player.AddShield(50f);
        player.TakeDamage(10f);
        Assert.That(player.CurrentHP, Is.EqualTo(500f));
        Assert.That(GetVisual(), Is.Null);
    }

    [Test]
    public void EmergencyHeal_UsesOneVisualAndDoesNotRepeatAfterTheFlagIsSet()
    {
        player.WoodArmorTier = 3;
        player.TakeDamage(400f);
        var visual = GetVisual();
        Assert.That(player.HasUsedEmergencyHeal, Is.True);
        Assert.That(player.CurrentHP, Is.EqualTo(261f).Within(0.01f));
        AssertMode(visual, WoodArmorVisual.Kind.EmergencyHeal);

        typeof(WoodArmorVisual).GetMethod("Advance", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(visual, new object[] { 1.6f });
        player.TakeDamage(200f);
        Assert.That(player.CurrentHP, Is.EqualTo(107f).Within(0.02f));
        Assert.That(GetVisual(), Is.SameAs(visual));
        AssertMode(visual, WoodArmorVisual.Kind.HealOnHit);
    }

    private WoodArmorVisual GetVisual() => player.GetComponentInChildren<WoodArmorVisual>();

    private static void AssertMode(WoodArmorVisual visual, WoodArmorVisual.Kind expected)
    {
        Assert.That(visual, Is.Not.Null);
        var field = typeof(WoodArmorVisual).GetField("kind", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field.GetValue(visual), Is.EqualTo(expected));
    }
}
