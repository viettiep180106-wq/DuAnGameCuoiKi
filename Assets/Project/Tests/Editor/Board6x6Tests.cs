using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class Board6x6Tests
{
    private GameObject _controllerObj;
    private BoardController _boardController;
    private GameObject _boardCenterObj;

    [SetUp]
    public void SetUp()
    {
        _controllerObj = new GameObject("Controller", typeof(RectTransform));
        _boardController = _controllerObj.AddComponent<BoardController>();

        _boardCenterObj = new GameObject("Board", typeof(RectTransform));
        _boardCenterObj.transform.SetParent(_controllerObj.transform, false);
        _boardController.center = _boardCenterObj.GetComponent<RectTransform>();

        // Load prefabSlot từ Resources
        var slotPrefab = Resources.Load<SlotStoneBase>("Prefab/Base/Slot/SLot");
        if (slotPrefab == null)
        {
#if UNITY_EDITOR
            slotPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<SlotStoneBase>("Assets/Project/Resources/Prefab/Base/Slot/SLot.prefab");
#endif
        }
        _boardController.prefabSlot = slotPrefab;
    }

    [TearDown]
    public void TearDown()
    {
        if (_controllerObj != null)
        {
            Object.DestroyImmediate(_controllerObj);
        }
    }

    [Test]
    public void Test_Board_HasFixed6x6Dimensions_RegardlessOfInputSize()
    {
        // Arrange
        var elements = new List<TypeStone> { TypeStone.Fire, TypeStone.Ice, TypeStone.Wood };

        // Act: Thử truyền vào Vector2Int(10, 7) hoặc Vector2Int(4, 4)
        _boardController.InitDataBoard(elements, new Vector2Int(10, 7));

        // Assert: Cố định kích thước 6x6
        var so = new UnityEditor.SerializedObject(_boardController);
        int cols = so.FindProperty("cols").intValue;
        int rows = so.FindProperty("rows").intValue;
        int maxY = so.FindProperty("maxY").intValue;

        Assert.AreEqual(6, cols, "Bàn cờ phải có đúng 6 cột");
        Assert.AreEqual(6, rows, "Bàn cờ phải có đúng 6 hàng");
        Assert.AreEqual(6, maxY, "maxY phải bằng 6");
    }

    [Test]
    public void Test_Board_PlayAreaAndFrameSize_FullWidth_And_720x720()
    {
        // Arrange & Act
        _boardController.EnsureContainers();

        // Assert: Khung nền bàn cờ Full Width (880x740) phủ kín bề ngang màn hình
        Assert.AreEqual(new Vector2(880f, 740f), _boardController.center.sizeDelta, 
            "Image nền bàn cờ (center) phải có kích thước full width 880x740");

        var slotsCont = _boardController.center.Find("SlotsContainer") as RectTransform;
        Assert.IsNotNull(slotsCont, "SlotsContainer phải tồn tại");
        Assert.AreEqual(new Vector2(720f, 720f), slotsCont.sizeDelta, 
            "SlotsContainer (vùng chơi các slot) phải có kích thước chuẩn 720x720");

        var stonesCont = _boardController.center.Find("StonesContainer") as RectTransform;
        Assert.IsNotNull(stonesCont, "StonesContainer phải tồn tại");
        Assert.AreEqual(new Vector2(720f, 720f), stonesCont.sizeDelta, 
            "StonesContainer phải có kích thước chuẩn 720x720");
    }

    [Test]
    public void Test_SlotAndStoneSize_CalculatedDynamically_NotHardcoded()
    {
        // Arrange: Cấu hình kích thước bàn 720x720 với 6x6
        _boardController.EnsureContainers();
        _boardController.SetUpBoard();

        // Act & Assert 1: Với width=720, cols=6 -> slotSize = 720 / 6 = 120
        var slotsCont = _boardController.center.Find("SlotsContainer");
        Assert.IsNotNull(slotsCont);
        Assert.AreEqual(36, slotsCont.childCount, "Bàn cờ 6x6 phải sinh ra đúng 36 ô slot");

        var firstSlot = slotsCont.GetChild(0).GetComponent<SlotStoneBase>();
        Assert.IsNotNull(firstSlot);
        Assert.AreEqual(120f, firstSlot.SlotItem.sizeDelta.x, 0.01f, "Chiều rộng slot phải bằng width / cols = 720 / 6 = 120");
        Assert.AreEqual(120f, firstSlot.SlotItem.sizeDelta.y, 0.01f, "Chiều cao slot phải bằng height / rows = 720 / 6 = 120");

        // Act & Assert 2: Chứng minh tính động: Đổi width=480, height=480 -> slotSize = 480 / 6 = 80
        var so = new UnityEditor.SerializedObject(_boardController);
        so.FindProperty("width").floatValue = 480f;
        so.FindProperty("height").floatValue = 480f;
        so.ApplyModifiedProperties();

        _boardController.ClearOldBoard();
        _boardController.SetUpBoard();

        var dynamicSlot = slotsCont.GetChild(0).GetComponent<SlotStoneBase>();
        Assert.AreEqual(80f, dynamicSlot.SlotItem.sizeDelta.x, 0.01f, "Kích thước slot phải co giãn động khi đổi size bàn (480 / 6 = 80)");
        Assert.AreEqual(80f, dynamicSlot.SlotItem.sizeDelta.y, 0.01f);
    }

    [Test]
    public void Test_ZeroWidthHeight_AutoHeals_To_720x720()
    {
        // Arrange: Giả lập lỗi width=0, height=0 từ serialization
        var so = new UnityEditor.SerializedObject(_boardController);
        so.FindProperty("width").floatValue = 0f;
        so.FindProperty("height").floatValue = 0f;
        so.FindProperty("boardFrameWidth").floatValue = 0f;
        so.FindProperty("boardFrameHeight").floatValue = 0f;
        so.ApplyModifiedProperties();

        // Act
        _boardController.EnsureContainers();
        _boardController.SetUpBoard();

        // Assert: Tự động phục hồi kích thước an toàn không bị 0x0
        var slotsCont = _boardController.center.Find("SlotsContainer") as RectTransform;
        Assert.IsNotNull(slotsCont);
        Assert.AreEqual(new Vector2(720f, 720f), slotsCont.sizeDelta, "Phải tự phục hồi về 720x720 khi serialize bằng 0");

        var firstSlot = slotsCont.GetChild(0).GetComponent<SlotStoneBase>();
        Assert.IsNotNull(firstSlot);
        Assert.AreEqual(120f, firstSlot.SlotItem.sizeDelta.x, 0.01f, "Slot phải có kích thước 120px");
    }

    [Test]
    public void Test_ClearOldBoard_ClearsBothSlotsAndStonesContainers()
    {
        // Arrange
        _boardController.EnsureContainers();
        _boardController.SetUpBoard();

        var slotsCont = _boardController.center.Find("SlotsContainer");
        var stonesCont = _boardController.center.Find("StonesContainer");
        Assert.Greater(slotsCont.childCount, 0, "Slots phải có phần tử trước khi clear");
        Assert.Greater(stonesCont.childCount, 0, "Stones phải có phần tử trước khi clear");

        // Act
        _boardController.ClearOldBoard();

        // Assert: Cả 2 container đều phải sạch 100%
        Assert.AreEqual(0, slotsCont.childCount, "SlotsContainer phải sạch sau khi clear");
        Assert.AreEqual(0, stonesCont.childCount, "StonesContainer phải sạch sau khi clear");
    }

    [Test]
    public void Test_Board_SlideInPosition_StrictlyBelowPlayerStatsBar()
    {
        // Arrange: Tạo PlayerStatsBar giả lập nằm ở nửa trên của bottom section
        GameObject psBarObj = new GameObject("PlayerStatsBar", typeof(RectTransform));
        psBarObj.transform.SetParent(_controllerObj.transform, false);
        var psBarRt = psBarObj.GetComponent<RectTransform>();
        psBarRt.anchorMin = new Vector2(0.5f, 0.5f);
        psBarRt.anchorMax = new Vector2(0.5f, 0.5f);
        psBarRt.pivot = new Vector2(0.5f, 0.5f);
        psBarRt.sizeDelta = new Vector2(830f, 185f);
        psBarRt.anchoredPosition = new Vector2(0f, -100f);

        // Mép dưới của PlayerStatsBar = -100 - (185 / 2) = -192.5f

        // Act: Lấy vị trí trượt vào của bàn cờ
        _boardController.EnsureContainers();
        float targetY = _boardController.GetTargetSlideInY();
        float halfBoardH = _boardController.center.sizeDelta.y > 0 ? _boardController.center.sizeDelta.y * 0.5f : 370f;
        float boardTop = targetY + halfBoardH;
        float barBottom = psBarRt.anchoredPosition.y - (psBarRt.sizeDelta.y * 0.5f);

        // Assert: Mép trên của Board PHẢI NẰM DƯỚI mép dưới của PlayerStatsBar
        Assert.Less(boardTop, barBottom, 
            $"Mép trên của bàn cờ ({boardTop}) phải nằm hoàn toàn DƯỚI mép dưới của PlayerStatsBar ({barBottom})");

        float gap = barBottom - boardTop;
        Assert.GreaterOrEqual(gap, 10f, 
            $"Khoảng cách giữa PlayerStatsBar và Board phải >= 10px để không bị che đè (thực tế: {gap}px)");

        Object.DestroyImmediate(psBarObj);
    }

    [Test]
    public void Test_GenerateNoMatch3Matrix_StrictlyZeroMatch3Across100Runs()
    {
        // Chạy kiểm tra 100 lần sinh ngẫu nhiên liên tiếp theo đúng bảng verification của GDD
        int cols = 6;
        int rows = 6;
        var targetStones = new List<TypeStone> { TypeStone.Fire, TypeStone.Wood };

        for (int run = 0; run < 100; run++)
        {
            TypeStone[,] matrix = _boardController.GenerateNoMatch3Matrix(cols, rows, targetStones);

            int horizontalMatch3 = 0;
            int verticalMatch3 = 0;

            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    TypeStone current = matrix[x, y];

                    // Kiểm tra Match-3 ngang: (x, y), (x+1, y), (x+2, y)
                    if (x + 2 < cols)
                    {
                        if (matrix[x + 1, y] == current && matrix[x + 2, y] == current)
                        {
                            horizontalMatch3++;
                        }
                    }

                    // Kiểm tra Match-3 dọc: (x, y), (x, y+1), (x, y+2)
                    if (y + 2 < rows)
                    {
                        if (matrix[x, y + 1] == current && matrix[x, y + 2] == current)
                        {
                            verticalMatch3++;
                        }
                    }
                }
            }

            Assert.AreEqual(0, horizontalMatch3, $"Lượt {run}: Ma trận 6x6 không được phép có Match-3 ngang!");
            Assert.AreEqual(0, verticalMatch3, $"Lượt {run}: Ma trận 6x6 không được phép có Match-3 dọc!");
        }
    }
}
