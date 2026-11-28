using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Điều phối quá trình preload dữ liệu trong PrepareScene:
/// chạy LoadAllLevelDataFromResource bàn giao mảng level cho GameDataManager,
/// preload dữ liệu còn lại, cập nhật Loading UI và chuyển vào HomeScene.
/// </summary>
public class PrepareSceneController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private GameDataManager gameDataManager;
    [SerializeField] private LoadAllLevelDataFromResource levelLoader;

    [Header("Loading UI")]
    [SerializeField] private Image progressFillImage;
    [SerializeField] private TMP_Text progressPercentText;
    [SerializeField] private TMP_Text statusLabelText;

    [Header("Timing")]
    [SerializeField] private float minimumLoadingSeconds = 0.4f;

    private void Start()
    {
        StartCoroutine(RunPrepareSequence());
    }

    private IEnumerator RunPrepareSequence()
    {
        float startedAt = Time.unscaledTime;

        if (gameDataManager == null)
        {
            gameDataManager = GameDataManager.Instance;
        }
        if (levelLoader == null)
        {
            levelLoader = FindFirstObjectByType<LoadAllLevelDataFromResource>(FindObjectsInactive.Include);
        }

        SetProgressUI(0f, "Đang nạp dữ liệu màn chơi...");

        // Bước 1 (0% -> 40%): Load toàn bộ LevelDataSO và chuyển mảng cho GameDataManager.
        if (levelLoader != null)
        {
            yield return levelLoader.LoadAllAndTransferAsync(gameDataManager, p =>
            {
                SetProgressUI(Mathf.Lerp(0f, 0.4f, p), "Đang nạp dữ liệu màn chơi...");
            });
        }
        else if (gameDataManager != null && !gameDataManager.HasLevelData)
        {
            gameDataManager.SetLevelDataArray(LoadAllLevelDataFromResource.LoadAllLevelsSync());
            SetProgressUI(0.4f, "Đang nạp dữ liệu màn chơi...");
        }

        // Bước 2 (40% -> 95%): Preload EnemyDataSO, Prefab & Warmup VFX/Shader vào GameDataManager.
        if (gameDataManager != null)
        {
            yield return gameDataManager.PreloadRemainingDataAsync((p, status) =>
            {
                SetProgressUI(Mathf.Lerp(0.4f, 0.95f, p), status);
            });
        }

        // Bảo đảm thanh tiến trình hiển thị mượt nếu máy load quá nhanh.
        while (Time.unscaledTime - startedAt < minimumLoadingSeconds)
        {
            float elapsedRatio = Mathf.Clamp01((Time.unscaledTime - startedAt) / Mathf.Max(0.01f, minimumLoadingSeconds));
            SetProgressUI(Mathf.Lerp(0.95f, 1f, elapsedRatio), "Đang vào sảnh chính...");
            yield return null;
        }

        SetProgressUI(1f, "Hoàn tất!");

        if (gameDataManager != null)
        {
            gameDataManager.LoadHomeScene();
        }
    }

    private void SetProgressUI(float normalizedProgress, string statusMessage)
    {
        float clamped = Mathf.Clamp01(normalizedProgress);
        if (progressFillImage != null)
        {
            progressFillImage.fillAmount = clamped;
        }
        if (progressPercentText != null)
        {
            progressPercentText.text = $"{Mathf.RoundToInt(clamped * 100f)}%";
        }
        if (statusLabelText != null && !string.IsNullOrEmpty(statusMessage))
        {
            statusLabelText.text = statusMessage;
        }
    }
}
