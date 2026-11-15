using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

[TestFixture]
public class LevelProgressBarTests
{
    private GameObject _root;
    private LevelProgressBarUI _ui;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("LevelProgressBar", typeof(RectTransform));
        var rt = _root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(600f, 60f);

        _ui = _root.AddComponent<LevelProgressBarUI>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
        {
            Object.DestroyImmediate(_root);
        }
    }

    [Test]
    public void LevelProgressBar_StepSpacing_EnsuresThreeToFourNodesVisibleInViewport()
    {
        // Arrange
        _ui.EnsureScrollViewHierarchy();
        _ui.InitMilestones(10);

        // Act
        var sv = _root.transform.Find("ScrollView");
        var vp = sv.Find("Viewport") as RectTransform;
        var content = vp.Find("Content") as RectTransform;
        var milestones = content.Find("Milestones") as RectTransform;

        float vpWidth = vp.rect.width > 50f ? vp.rect.width : 600f;
        float halfVp = vpWidth * 0.5f;

        // Bắt đầu tại wave 2 (nút 2 ở giữa)
        _ui.ScrollToWave(2, 0f);

        // Đếm số lượng node nằm trong tầm nhìn của Viewport (-halfVp <= relativeX <= halfVp)
        int visibleCount = 0;
        for (int i = 0; i < 10; i++)
        {
            var node = milestones.GetChild(i) as RectTransform;
            float worldX = content.anchoredPosition.x + node.anchoredPosition.x;
            float relX = worldX - halfVp;

            // Kiểm tra node có nằm trong khung hiển thị của Viewport không
            if (relX >= -halfVp - 5f && relX <= halfVp + 5f)
            {
                visibleCount++;
            }
        }

        // Assert - Phải hiển thị ít nhất 3 đến 4 wave cùng lúc theo đúng yêu cầu người dùng
        Assert.GreaterOrEqual(visibleCount, 3, "Phải hiển thị ít nhất 3 điểm wave trong Viewport");
        Assert.LessOrEqual(visibleCount, 5, "Không được hiển thị quá nhiều điểm wave gây chật chội");
    }

    [Test]
    public void LevelProgressBar_ScrollToWave_CentersCurrentWaveNode()
    {
        // Arrange
        _ui.EnsureScrollViewHierarchy();
        _ui.InitMilestones(10);

        // Act - Cuộn đến wave 3
        _ui.ScrollToWave(3, 0f);

        var sv = _root.transform.Find("ScrollView");
        var vp = sv.Find("Viewport") as RectTransform;
        var content = vp.Find("Content") as RectTransform;
        var milestones = content.Find("Milestones") as RectTransform;

        float vpWidth = vp.rect.width > 50f ? vp.rect.width : 600f;
        float halfVp = vpWidth * 0.5f;

        var node3 = milestones.GetChild(2) as RectTransform; // Wave 3 (index 2)
        float worldX = content.anchoredPosition.x + node3.anchoredPosition.x;
        float relX = worldX - halfVp;

        // Assert - Node 3 phải nằm ngay chính giữa Viewport (relX ~ 0)
        Assert.AreEqual(0f, relX, 1.0f, "Wave hiện tại phải luôn nằm chính giữa Viewport");
    }

    [Test]
    public void LevelProgressBar_Milestones_TextCenteredAndZeroMargin()
    {
        // Arrange
        _ui.EnsureScrollViewHierarchy();
        _ui.TryLoadDefaultSprites();
        _ui.InitMilestones(10);

        // Act - Cập nhật wave 1
        _ui.UpdateProgress(1, 10);

        var sv = _root.transform.Find("ScrollView");
        var vp = sv.Find("Viewport") as RectTransform;
        var content = vp.Find("Content") as RectTransform;
        var milestones = content.Find("Milestones") as RectTransform;

        // Assert - Tất cả các text phải có margin = 0 và anchorMin/Max = (0.5, 0.5)
        for (int i = 0; i < 10; i++)
        {
            var node = milestones.GetChild(i);
            var txt = node.Find("WaveNum")?.GetComponent<TMPro.TMP_Text>();
            var txtRt = txt?.GetComponent<RectTransform>();

            Assert.IsNotNull(txt, $"Node {i + 1} phải có TextMeshPro WaveNum");
            Assert.AreEqual(Vector4.zero, txt.margin, $"Node {i + 1} margin phải bằng Vector4.zero (không được lệch)");
            Assert.AreEqual(new Vector2(0.5f, 0.5f), txtRt.anchorMin, $"Node {i + 1} anchorMin phải là (0.5, 0.5)");
            Assert.AreEqual(new Vector2(0.5f, 0.5f), txtRt.anchorMax, $"Node {i + 1} anchorMax phải là (0.5, 0.5)");
            Assert.AreEqual(Vector2.zero, txtRt.anchoredPosition, $"Node {i + 1} anchoredPosition phải là Vector2.zero");
        }
    }

    [Test]
    public void LevelProgressBar_TrackBg_StartsAtNode1_NeverExtendsPastLevel1()
    {
        // Arrange
        _ui.EnsureScrollViewHierarchy();
        _ui.InitMilestones(10);

        var sv = _root.transform.Find("ScrollView");
        var vp = sv.Find("Viewport") as RectTransform;
        var content = vp.Find("Content") as RectTransform;
        var trackBg = content.Find("ProgressBarTrack") as RectTransform;
        var milestones = content.Find("Milestones") as RectTransform;
        var node1 = milestones.GetChild(0) as RectTransform;

        // Assert - Track bg phải bắt đầu CHÍNH XÁC tại tâm của Node 1 (không kéo dài về bên trái)
        Assert.AreEqual(node1.anchoredPosition.x, trackBg.anchoredPosition.x, 0.1f, 
            "Track Bg phải bắt đầu chính xác tại x của Node 1, không được thừa ra trước Level 1");
    }

    [Test]
    public void LevelProgressBar_ElementColor_ReturnsValidColors()
    {
        // Assert - Kiểm tra cơ chế đổi màu theo ngũ hành
        Assert.AreNotEqual(Color.clear, _ui.GetElementColor(ElementType.Wood));
        Assert.AreNotEqual(Color.clear, _ui.GetElementColor(ElementType.Fire));
        Assert.AreNotEqual(Color.clear, _ui.GetElementColor(ElementType.Water));
        Assert.AreNotEqual(Color.clear, _ui.GetElementColor(ElementType.Earth));
        Assert.AreNotEqual(Color.clear, _ui.GetElementColor(ElementType.Metal));
    }
}
