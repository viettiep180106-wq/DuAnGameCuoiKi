using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class CombatBackgroundScrollerTests
{
    private GameObject _testObject;
    private CombatBackgroundScroller _scroller;

    [SetUp]
    public void SetUp()
    {
        _testObject = new GameObject("TestBackground");
        _testObject.transform.localPosition = new Vector3(0f, 1f, 0f);
        _scroller = _testObject.AddComponent<CombatBackgroundScroller>();
        _scroller.SetStartLocalPosition(_testObject.transform.localPosition);
    }

    [TearDown]
    public void TearDown()
    {
        if (_testObject != null)
        {
            Object.DestroyImmediate(_testObject);
        }
    }

    [Test]
    public void CombatBackgroundScroller_InitializesWithCorrectDefaults_DirectionIsOppositeToMonster()
    {
        // Arrange & Act
        Vector2 dir = _scroller.EffectiveDirection;

        // Assert - Hướng mặc định phải là sang trái (Vector2.left = (-1, 0)), ngược chiều quái (+X)
        Assert.AreEqual(-1f, dir.x, 0.001f);
        Assert.AreEqual(0f, dir.y, 0.001f);
    }

    [Test]
    public void CombatBackgroundScroller_WhenInvertDirectionSet_InvertsDirection()
    {
        // Arrange
        _scroller.SetInvertDirection(true);

        // Act
        Vector2 dir = _scroller.EffectiveDirection;

        // Assert - Sau khi đảo chiều thì hướng phải sang phải (Vector2.right = (1, 0))
        Assert.AreEqual(1f, dir.x, 0.001f);
        Assert.AreEqual(0f, dir.y, 0.001f);
    }

    [Test]
    public void CombatBackgroundScroller_SpeedMultiplier_ScalesEffectiveSpeed()
    {
        // Arrange
        float initialSpeed = _scroller.CurrentSpeed;

        // Act
        _scroller.SetSpeedMultiplier(2.5f);

        // Assert
        Assert.AreEqual(initialSpeed * 2.5f, _scroller.CurrentSpeed, 0.001f);
    }

    [Test]
    public void CombatBackgroundScroller_ManualOverride_ControlsScrollingState()
    {
        // Arrange & Act
        _scroller.SetManualOverride(true, false);

        // Assert
        Assert.IsFalse(_scroller.IsActive);

        // Act
        _scroller.SetManualOverride(true, true);

        // Assert
        Assert.IsTrue(_scroller.IsActive);
    }

    [Test]
    public void CombatBackgroundScroller_ResetPosition_RestoresStartPosition()
    {
        // Arrange
        Vector3 initialPos = _testObject.transform.localPosition;
        _testObject.transform.localPosition = new Vector3(99f, 99f, 99f);

        // Act
        _scroller.ResetPosition();

        // Assert
        Assert.AreEqual(initialPos.x, _testObject.transform.localPosition.x, 0.001f);
        Assert.AreEqual(initialPos.y, _testObject.transform.localPosition.y, 0.001f);
        Assert.AreEqual(initialPos.z, _testObject.transform.localPosition.z, 0.001f);
        Assert.AreEqual(0f, _scroller.AccumulatedDistance, 0.001f);
    }
}
