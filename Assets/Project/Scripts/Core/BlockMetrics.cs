using UnityEngine;

/// <summary>
/// Quản lý đơn vị tọa độ chuẩn Block trong Game theo công thức GDD:
/// 1 block = (khoảng cách từ mép màn hình đối diện đến vị trí player) / 20
/// </summary>
public static class BlockMetrics
{
    private static float _cachedBlockSize = -1f;
    private static float _lastCalculationTime = -1f;

    /// <summary>
    /// Tính toán kích thước 1 block trong đơn vị tọa độ Unity World Space
    /// </summary>
    public static float CalculateBlockSize()
    {
        // Cache trong 0.2s để tránh tính toán lại liên tục mỗi frame
        if (_cachedBlockSize > 0f && Time.time - _lastCalculationTime < 0.2f)
        {
            return _cachedBlockSize;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            return 0.2155f; // Giá trị fallback chuẩn với Orthographic Size = 5
        }

        // 1. Lấy vị trí Player trong World Space
        Vector3 playerPos = Vector3.zero;
        if (Player.Instance != null)
        {
            playerPos = Player.Instance.transform.position;
        }
        else
        {
            var pObj = GameObject.FindWithTag("Player");
            if (pObj != null)
            {
                playerPos = pObj.transform.position;
            }
            else
            {
                playerPos = new Vector3(2.0f, 2.8f, 0f);
            }
        }

        // 2. Xác định mép màn hình đối diện:
        // Nếu Player ở nửa bên phải màn hình (x >= cam.x) -> Mép đối diện là mép trái (viewport x = 0)
        // Nếu Player ở nửa bên trái màn hình (x < cam.x) -> Mép đối diện là mép phải (viewport x = 1)
        float oppositeViewportX = (playerPos.x >= cam.transform.position.x) ? 0f : 1f;
        Vector3 oppositeEdgeWorld = cam.ViewportToWorldPoint(new Vector3(oppositeViewportX, 0.5f, Mathf.Abs(cam.transform.position.z)));

        // 3. Công thức: khoảng cách từ mép màn hình đối diện đến vị trí player / 5
        float distanceToPlayer = Mathf.Abs(playerPos.x - oppositeEdgeWorld.x);
        _cachedBlockSize = Mathf.Max(0.01f, distanceToPlayer / 5.0f);
        _lastCalculationTime = Time.time;

        return _cachedBlockSize;
    }

    /// <summary>
    /// Chuyển đổi tốc độ từ block/s sang Unity units/s
    /// </summary>
    public static float ConvertSpeedToWorldUnits(float speedInBlocks)
    {
        return speedInBlocks * CalculateBlockSize();
    }

    /// <summary>
    /// Chuyển đổi cự ly từ block sang Unity units
    /// </summary>
    public static float ConvertDistanceToWorldUnits(float distanceInBlocks)
    {
        return distanceInBlocks * CalculateBlockSize();
    }
}
