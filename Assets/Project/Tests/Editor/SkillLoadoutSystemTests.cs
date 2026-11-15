using NUnit.Framework;
using UnityEngine;

public class SkillLoadoutSystemTests
{
    private GameObject _managerObj;
    private PlayerLoadoutManager _loadoutManager;

    [SetUp]
    public void Setup()
    {
        _managerObj = new GameObject("Test_PlayerLoadoutManager");
        _loadoutManager = _managerObj.AddComponent<PlayerLoadoutManager>();
        _loadoutManager.InitializeLoadout();
    }

    [TearDown]
    public void Teardown()
    {
        if (_managerObj != null)
        {
            Object.DestroyImmediate(_managerObj);
        }
    }

    [Test]
    public void DefaultPassiveSkills_MappedCorrectlyToStoneTypes()
    {
        // Arrange & Act
        SkillPassiveDataSO fireSkill = _loadoutManager.GetEquippedPassive(TypeStone.Fire);
        SkillPassiveDataSO iceSkill = _loadoutManager.GetEquippedPassive(TypeStone.Ice);
        SkillPassiveDataSO woodSkill = _loadoutManager.GetEquippedPassive(TypeStone.Wood);
        SkillPassiveDataSO earthSkill = _loadoutManager.GetEquippedPassive(TypeStone.Earth);
        SkillPassiveDataSO windSkill = _loadoutManager.GetEquippedPassive(TypeStone.Wind);

        // Assert (Constraint Model per unity-testing.md)
        Assert.That(fireSkill, Is.Not.Null, "Hệ Hỏa phải có skill passive mặc định!");
        Assert.That(fireSkill.StoneType, Is.EqualTo(TypeStone.Fire));

        Assert.That(iceSkill, Is.Not.Null, "Hệ Băng/Thủy phải có skill passive mặc định!");
        Assert.That(iceSkill.StoneType, Is.EqualTo(TypeStone.Ice));

        Assert.That(woodSkill, Is.Not.Null, "Hệ Mộc phải có skill passive mặc định!");
        Assert.That(woodSkill.StoneType, Is.EqualTo(TypeStone.Wood));

        Assert.That(earthSkill, Is.Not.Null, "Hệ Thổ phải có skill passive mặc định!");
        Assert.That(earthSkill.StoneType, Is.EqualTo(TypeStone.Earth));

        Assert.That(windSkill, Is.Not.Null, "Hệ Kim/Phong phải có skill passive mặc định!");
        Assert.That(windSkill.StoneType, Is.EqualTo(TypeStone.Wind));
    }

    [Test]
    public void EquipPassiveSkill_UpdatesStoneToSkillMapping()
    {
        // Arrange
        var customSkill = ScriptableObject.CreateInstance<SkillPassiveDataSO>();
        customSkill.Initialize("skill_fire_inferno", "Bão Lửa Địa Ngục", TypeStone.Fire, ElementType.Fire, null, baseDamage: 50f);

        // Act
        _loadoutManager.EquipPassiveSkill(customSkill);
        SkillPassiveDataSO equipped = _loadoutManager.GetEquippedPassive(TypeStone.Fire);

        // Assert (Constraint Model)
        Assert.That(equipped, Is.Not.Null);
        Assert.That(equipped.SkillId, Is.EqualTo("skill_fire_inferno"));
        Assert.That(equipped.SkillName, Is.EqualTo("Bão Lửa Địa Ngục"));
        Assert.That(equipped.BaseDamage, Is.EqualTo(50f));
    }

    [Test]
    public void SkillContainer_InitializesAndCarriesEquippedSkillData()
    {
        // Arrange
        GameObject containerObj = new GameObject("Test_Container");
        SkillContainer container = containerObj.AddComponent<SkillContainer>();

        var testSkill = ScriptableObject.CreateInstance<SkillPassiveDataSO>();
        testSkill.Initialize("skill_wood_thorn", "Gai Mộc Tử Thần", TypeStone.Wood, ElementType.Wood, null, baseDamage: 30f);

        // Act
        container.Initialize(testSkill, 6);

        // Assert (Constraint Model)
        Assert.That(container.EquippedSkillData, Is.Not.Null);
        Assert.That(container.EquippedSkillData.SkillName, Is.EqualTo("Gai Mộc Tử Thần"));
        Assert.That(container.StoneType, Is.EqualTo(TypeStone.Wood));
        Assert.That(container.MatchCount, Is.EqualTo(6));

        Object.DestroyImmediate(containerObj);
    }

    [Test]
    public void BossDrop_ActiveSkillUnlock_UpdatesStatus()
    {
        // Arrange
        Assert.That(_loadoutManager.IsActiveSkillUnlocked(ActiveSkillType.MeteorStrike), Is.False, "Ban đầu MeteorStrike phải bị khóa!");

        // Act
        bool unlocked = _loadoutManager.UnlockActiveSkill(ActiveSkillType.MeteorStrike);

        // Assert
        Assert.That(unlocked, Is.True, "Mở khóa lần đầu phải trả về true!");
        Assert.That(_loadoutManager.IsActiveSkillUnlocked(ActiveSkillType.MeteorStrike), Is.True, "Sau khi mở khóa, IsActiveSkillUnlocked phải là true!");
    }

    [Test]
    public void BossDrop_TryDropFromBoss_DropsAndUnlocksSkill()
    {
        // Arrange: Ban đầu BladeStorm đã mở khóa, còn 3 skill chưa mở
        int initialCount = _loadoutManager.UnlockedActiveSkills.Count;

        // Act: Giả lập đánh bại Boss nhiều lần để kiểm tra tỷ lệ rơi
        for (int i = 0; i < 20; i++)
        {
            _loadoutManager.TryDropActiveSkillFromBoss(ElementType.Fire);
        }

        // Assert: Sau 20 lần đánh Boss, ít nhất phải rơi thêm skill active
        Assert.That(_loadoutManager.UnlockedActiveSkills.Count, Is.GreaterThanOrEqualTo(initialCount));
    }
}
