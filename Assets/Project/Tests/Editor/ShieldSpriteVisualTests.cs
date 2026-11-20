using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class ShieldSpriteVisualTests
{
    private GameObject root;
    private GameObject windPrefab;
    private Player oldPlayer;
    private Player player;
    private ShieldSpriteVisual visual;
    private SpriteRenderer shieldRenderer;

    [SetUp]
    public void SetUp()
    {
        oldPlayer = Player.Instance;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Project/Resources/Prefab/Base/Player/Player.prefab");
        windPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Project/Resources/Prefab/Base/Skill/Passive/wind.prefab");
        root = Object.Instantiate(prefab);
        player = root.GetComponent<Player>();
        Player.Instance = player;
        player.SetStatsForTest(500f, 200f);
        visual = root.GetComponentInChildren<ShieldSpriteVisual>();
        shieldRenderer = visual.GetComponent<SpriteRenderer>();
        InvokeVisual("Awake");
        InvokeVisual("OnEnable");
    }

    [TearDown]
    public void TearDown()
    {
        if (root)
            Object.DestroyImmediate(root);
        Player.Instance = oldPlayer;
    }

    [Test]
    public void MetalShield_UsesOneVisual_AndPlaysImpactBeforeShatter()
    {
        var legacyRenderer = ActivateMetalShield().GetComponent<SpriteRenderer>();
        Assert.That(legacyRenderer.enabled, Is.False);
        AssertSprite("Shield_01_Cast");
        Tick(0.48f);
        AssertSprite("Shield_03_Charge");
        Tick(0.37f);
        AssertSprite("Shield_02_Idle");

        ActivateMetalShield();
        Assert.That(player.CurrentShield, Is.EqualTo(150f));
        AssertSprite("Shield_03_Charge");
        player.TakeDamage(1f, false, ElementType.Wood, 0f, true);
        Assert.That(player.CurrentShield, Is.EqualTo(150f));
        AssertSprite("Shield_03_Charge");

        player.TakeDamage(1f);
        Assert.That(player.CurrentShield, Is.LessThan(150f));
        AssertSprite("Shield_04_Impact");
        Tick(0.36f);
        AssertSprite("Shield_02_Idle");

        player.TakeDamage(225f);
        Assert.That(player.CurrentShield, Is.Zero);
        Assert.That(player.HasActiveShield(), Is.False);
        AssertSprite("Shield_04_Impact");
        Tick(0.21f);
        AssertSprite("Shield_05_Shatter");
        Tick(0.68f);
        Assert.That(shieldRenderer.enabled, Is.False);
    }

    [Test]
    public void RoundRefill_FromZero_StartsCast_AndClearHidesImmediately()
    {
        player.AddShield(25f);
        Assert.That(shieldRenderer.enabled, Is.False);
        player.ClearShield();

        ActivateMetalShield(6);
        player.TakeDamage(150f);
        Assert.That(player.CurrentShield, Is.Zero);
        Tick(0.21f);
        AssertSprite("Shield_05_Shatter");

        player.TickRoundMetalShield();
        player.TickRoundMetalShield();
        player.TickRoundMetalShield();
        Assert.That(player.CurrentShield, Is.EqualTo(75f));
        AssertSprite("Shield_01_Cast");
        player.ClearShield();
        Tick(1f);
        Assert.That(shieldRenderer.enabled, Is.False);
    }

    private SkillWind ActivateMetalShield(int matchCount = 3)
    {
        var skill = Object.Instantiate(windPrefab, root.transform).GetComponent<SkillWind>();
        skill.Initialize(matchCount);
        skill.Active();
        return skill;
    }

    private void Tick(float elapsed) => InvokeVisual("AdvanceTime", elapsed);

    private void InvokeVisual(string methodName, params object[] arguments)
    {
        // EditMode allows the visual clock to be stepped deterministically.
        typeof(ShieldSpriteVisual).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(visual, arguments);
    }

    private void AssertSprite(string name)
    {
        Assert.That(shieldRenderer.enabled, Is.True);
        Assert.That(shieldRenderer.sprite.name, Is.EqualTo(name));
    }
}
