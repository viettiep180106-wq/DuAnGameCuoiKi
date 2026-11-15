using System;
using UnityEngine;
using DG.Tweening;

public class CameraWavePreview : MonoBehaviour
{
    public static CameraWavePreview Instance { get; private set; }

    [Header("Camera Pan Settings")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float panSpeed = 0.8f;
    [SerializeField] private float combatCenterX = -5.693f;

    private Vector3 originalCameraPos;
    private Tween cameraTween;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera != null)
        {
            originalCameraPos = targetCamera.transform.position;
        }
    }

    /// <summary>
    /// Lia camera sang phải căn giữa Player (rìa trái) và Enemy hàng sau (rìa phải) theo chuẩn GDD
    /// </summary>
    public void PanToCombatView(Action onComplete)
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null)
        {
            onComplete?.Invoke();
            return;
        }

        cameraTween?.Kill();
        Vector3 targetPos = new Vector3(combatCenterX, originalCameraPos.y, originalCameraPos.z);
        cameraTween = targetCamera.transform.DOMove(targetPos, panSpeed)
            .SetEase(Ease.InOutCubic)
            .OnComplete(() =>
            {
                cameraTween = null;
                onComplete?.Invoke();
            });
    }

    /// <summary>
    /// Lia camera quay trở về tâm ban đầu (X = 0) cho giai đoạn Pre-Wave và Khám phá
    /// </summary>
    public void PanToPreWaveView(Action onComplete = null)
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null)
        {
            onComplete?.Invoke();
            return;
        }

        cameraTween?.Kill();
        Vector3 targetPos = new Vector3(originalCameraPos.x, originalCameraPos.y, originalCameraPos.z);
        cameraTween = targetCamera.transform.DOMove(targetPos, panSpeed)
            .SetEase(Ease.InOutCubic)
            .OnComplete(() =>
            {
                cameraTween = null;
                onComplete?.Invoke();
            });
    }

    /// <summary>
    /// Backward compatibility
    /// </summary>
    public void PlayWavePreview(float enemyDistance, Action onComplete)
    {
        PanToCombatView(onComplete);
    }

    public float GetLeadEnemyX(float fallbackDistance)
    {
        return combatCenterX;
    }

    public void ResetCameraPosition()
    {
        cameraTween?.Kill();
        if (targetCamera != null)
        {
            targetCamera.transform.position = originalCameraPos;
        }
    }

    private void OnDestroy()
    {
        cameraTween?.Kill();
    }
}
