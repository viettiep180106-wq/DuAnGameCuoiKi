using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;

public enum CameraShakeTier
{
    None = 0,
    Light = 1,
    Heavy = 2
}

public enum EnemyZoomScope
{
    AllActiveSkills = 0,
    OffensiveActiveOnly = 1,
    BossAndFinalBossOnly = 2,
    FinalBossOnly = 3
}

[Serializable]
public struct CameraShakeProfile
{
    public float duration;
    public Vector3 strength;
    public int vibrato;
    public float randomness;
}

/// <summary>
/// Quản lý tập trung Pan, Focus Zoom 1s và Camera Shake theo tầng (Light / Heavy)
/// bằng cách tách lớp _basePos và _shakeOffset trong LateUpdate để không bị trôi camera
/// và không phá vỡ Canvas ScreenSpaceCamera.
/// </summary>
public class CombatCameraController : MonoBehaviour
{
    public static CombatCameraController Instance { get; private set; }

    [Header("Focus Zoom Settings")]
    [SerializeField, Range(0.25f, 0.95f)] private float zoomOrthoRatio = 0.58f;
    [SerializeField, Min(0.05f)] private float zoomInDuration = 0.22f;
    [SerializeField, Min(0f)] private float zoomHoldDuration = 0.56f;
    [SerializeField, Min(0.05f)] private float zoomOutDuration = 0.22f;
    [Tooltip("Đẩy tâm camera xuống thấp hơn nhân vật khi zoom để nhân vật nằm thoáng phía trên bàn cờ 6x6.")]
    [SerializeField] private float focusYOffset = -7.0f;
    [SerializeField] private bool clampToBackgroundBounds = true;

    [Header("Enemy & Player Trigger Policies")]
    [SerializeField] private EnemyZoomScope enemyZoomScope = EnemyZoomScope.AllActiveSkills;

    [Header("Camera Shake Profiles")]
    [SerializeField] private CameraShakeProfile lightShakeProfile = new CameraShakeProfile
    {
        duration = 0.18f,
        strength = new Vector3(0.45f, 0.45f, 0f),
        vibrato = 12,
        randomness = 90f
    };

    [SerializeField] private CameraShakeProfile heavyShakeProfile = new CameraShakeProfile
    {
        duration = 0.45f,
        strength = new Vector3(1.25f, 1.25f, 0f),
        vibrato = 20,
        randomness = 90f
    };

    private Camera _targetCamera;
    private Vector3 _preWavePos;
    private Vector3 _combatRestPos;
    private Vector3 _basePos;
    private Vector3 _shakeOffset;
    private float _defaultOrthoSize = 49.11628f;
    private float _currentOrthoSize = 49.11628f;
    private bool _initialized;

    private Tween _panTween;
    private Tween _shakeTween;
    private CameraShakeTier _activeShakeTier = CameraShakeTier.None;

    private CombatBackgroundScroller _cachedScroller;
    private SpriteRenderer _cachedBgRenderer;
    private SpriteRenderer _cachedBgCloneRenderer;

    public bool IsZooming { get; private set; }
    public EnemyZoomScope ZoomScope => enemyZoomScope;
    public float DefaultOrthoSize => _defaultOrthoSize;
    public float CurrentOrthoSize => _currentOrthoSize;
    public Vector3 CurrentBasePosition => _basePos;
    public Vector3 CurrentShakeOffset => _shakeOffset;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }

        EnsureInitialized();
    }

    private void OnDestroy()
    {
        KillAllTweens();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Initialize(Camera cam, Vector3 preWavePos, float combatCenterX)
    {
        _targetCamera = cam != null ? cam : ResolveCamera();
        _preWavePos = preWavePos;
        _combatRestPos = new Vector3(combatCenterX, preWavePos.y, preWavePos.z);

        if (!_initialized)
        {
            _basePos = _targetCamera != null ? _targetCamera.transform.position : preWavePos;
            if (_targetCamera != null && _targetCamera.orthographic)
            {
                _defaultOrthoSize = _targetCamera.orthographicSize;
            }
            _currentOrthoSize = _defaultOrthoSize;
            _initialized = true;
        }
    }

    public void SetCameraForTest(Camera cam)
    {
        _targetCamera = cam;
        _initialized = false;
        EnsureInitialized();
    }

    private Camera ResolveCamera()
    {
        if (_targetCamera != null) return _targetCamera;
        _targetCamera = GetComponent<Camera>();
        if (_targetCamera == null) _targetCamera = Camera.main;
        return _targetCamera;
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;

        Camera cam = ResolveCamera();
        if (cam != null)
        {
            _preWavePos = cam.transform.position;
            _combatRestPos = _preWavePos;
            _basePos = _preWavePos;
            if (cam.orthographic)
            {
                _defaultOrthoSize = cam.orthographicSize;
                _currentOrthoSize = _defaultOrthoSize;
            }
            _initialized = true;
        }
    }

    /// <summary>
    /// Di chuyển vị trí gốc của camera (thay thế DOMove trực tiếp từ CameraWavePreview).
    /// </summary>
    public void PanBasePosition(Vector3 targetPos, float duration, Ease ease, bool isCombatView, Action onComplete = null)
    {
        EnsureInitialized();
        if (isCombatView) _combatRestPos = targetPos;
        else _preWavePos = targetPos;

        _panTween?.Kill();
        if (_targetCamera == null || duration <= 0f)
        {
            _basePos = targetPos;
            ApplyCameraState();
            onComplete?.Invoke();
            return;
        }

        _panTween = DOTween.To(
            () => _basePos,
            v => _basePos = v,
            targetPos,
            duration
        ).SetEase(ease).SetUpdate(false).OnComplete(() =>
        {
            _panTween = null;
            _basePos = targetPos;
            ApplyCameraState();
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// Phục hồi ngay lập tức về góc nhìn Pre-Wave hoặc Combat (dùng khi thắng/thua/ngắt wave giữa zoom).
    /// </summary>
    public void ResetImmediate(bool toCombatRest = false)
    {
        EnsureInitialized();
        KillAllTweens();
        _shakeOffset = Vector3.zero;
        _activeShakeTier = CameraShakeTier.None;
        _currentOrthoSize = _defaultOrthoSize;
        _basePos = toCombatRest ? _combatRestPos : _preWavePos;
        IsZooming = false;
        ApplyCameraState();
    }

    /// <summary>
    /// Rung camera theo tầng Light / Heavy tách rời khỏi _basePos để không làm trôi camera.
    /// </summary>
    public void TriggerShake(CameraShakeTier tier)
    {
        if (tier == CameraShakeTier.None) return;
        CameraShakeProfile profile = tier == CameraShakeTier.Heavy ? heavyShakeProfile : lightShakeProfile;
        TriggerShakeInternal(profile.duration, profile.strength, profile.vibrato, profile.randomness, tier);
    }

    /// <summary>
    /// Cho phép các hiệu ứng kỹ năng Player dùng chung hệ rung tách lớp mà không tự chỉnh transform.
    /// </summary>
    public void TriggerCustomShake(float duration, Vector3 strength, int vibrato = 10, float randomness = 90f, CameraShakeTier priority = CameraShakeTier.Light)
    {
        TriggerShakeInternal(duration, strength, vibrato, randomness, priority);
    }

    private void TriggerShakeInternal(float duration, Vector3 strength, int vibrato, float randomness, CameraShakeTier tier)
    {
        EnsureInitialized();
        if (duration <= 0f || strength == Vector3.zero) return;

        // Không cho rung nhẹ ghi đè lên rung mạnh đang chạy (ví dụ Final Boss)
        if (_shakeTween != null && _shakeTween.IsActive() && _shakeTween.IsPlaying() && tier < _activeShakeTier)
        {
            return;
        }

        _shakeTween?.Kill();
        _shakeOffset = Vector3.zero;
        _activeShakeTier = tier;

        _shakeTween = DOTween.Shake(
            () => _shakeOffset,
            v => _shakeOffset = new Vector3(v.x, v.y, 0f),
            duration,
            strength,
            vibrato,
            randomness,
            true,
            ShakeRandomnessMode.Full
        ).SetUpdate(false).OnComplete(() =>
        {
            _shakeOffset = Vector3.zero;
            _activeShakeTier = CameraShakeTier.None;
            _shakeTween = null;
        });
    }

    /// <summary>
    /// Coroutine zoom mượt vào mục tiêu rồi trở lại góc nhìn chiến đấu trong tổng thời lượng mặc định 1s.
    /// </summary>
    public IEnumerator PlayFocusZoomRoutine(Transform focusTarget, float totalDuration = 1.0f)
    {
        EnsureInitialized();
        if (_targetCamera == null || focusTarget == null || !_targetCamera.orthographic)
        {
            yield break;
        }

        // Hủy pan cũ nếu đang chạy để không xung đột
        _panTween?.Kill();
        _panTween = null;

        IsZooming = true;
        Vector3 startPos = _basePos;
        float startSize = _currentOrthoSize;

        Vector3 returnPos = _combatRestPos;
        float returnSize = _defaultOrthoSize;

        float targetSize = Mathf.Max(5f, _defaultOrthoSize * zoomOrthoRatio);
        Vector3 focusPos = ComputeClampedFocusPosition(focusTarget.position, targetSize);

        float baseSum = Mathf.Max(0.1f, zoomInDuration + zoomHoldDuration + zoomOutDuration);
        float timeScaleFactor = Mathf.Max(0.1f, totalDuration / baseSum);
        float inTime = zoomInDuration * timeScaleFactor;
        float holdTime = zoomHoldDuration * timeScaleFactor;
        float outTime = zoomOutDuration * timeScaleFactor;

        try
        {
            // Pha 1: Zoom In vào mục tiêu
            float elapsed = 0f;
            while (elapsed < inTime)
            {
                elapsed += Time.deltaTime;
                float t = EaseOutCubic(Mathf.Clamp01(elapsed / inTime));
                _basePos = Vector3.LerpUnclamped(startPos, focusPos, t);
                _currentOrthoSize = Mathf.LerpUnclamped(startSize, targetSize, t);
                ApplyCameraState();
                yield return null;
            }
            _basePos = focusPos;
            _currentOrthoSize = targetSize;
            ApplyCameraState();

            // Pha 2: Giữ góc cận cảnh
            if (holdTime > 0f)
            {
                float holdElapsed = 0f;
                while (holdElapsed < holdTime)
                {
                    holdElapsed += Time.deltaTime;
                    yield return null;
                }
            }

            // Pha 3: Zoom Out trở về bình thường
            elapsed = 0f;
            while (elapsed < outTime)
            {
                elapsed += Time.deltaTime;
                float t = EaseInOutCubic(Mathf.Clamp01(elapsed / outTime));
                _basePos = Vector3.LerpUnclamped(focusPos, returnPos, t);
                _currentOrthoSize = Mathf.LerpUnclamped(targetSize, returnSize, t);
                ApplyCameraState();
                yield return null;
            }
        }
        finally
        {
            _basePos = returnPos;
            _currentOrthoSize = returnSize;
            ApplyCameraState();
            IsZooming = false;
        }
    }

    public Vector3 ComputeClampedFocusPosition(Vector3 targetWorldPos, float targetOrthoSize)
    {
        Vector3 desired = new Vector3(targetWorldPos.x, targetWorldPos.y + focusYOffset, _combatRestPos.z);

        if (!clampToBackgroundBounds || _targetCamera == null)
        {
            return desired;
        }

        if (TryGetBackgroundBounds(out Bounds bgBounds))
        {
            float halfHeight = targetOrthoSize;
            float halfWidth = targetOrthoSize * Mathf.Max(0.1f, _targetCamera.aspect);

            float minX = bgBounds.min.x + halfWidth;
            float maxX = bgBounds.max.x - halfWidth;
            desired.x = (minX <= maxX) ? Mathf.Clamp(desired.x, minX, maxX) : bgBounds.center.x;

            float minY = bgBounds.min.y + halfHeight;
            float maxY = bgBounds.max.y - halfHeight;
            desired.y = (minY <= maxY) ? Mathf.Clamp(desired.y, minY, maxY) : _combatRestPos.y;
        }

        return desired;
    }

    private bool TryGetBackgroundBounds(out Bounds combinedBounds)
    {
        if (_cachedScroller == null)
        {
            _cachedScroller = FindFirstObjectByType<CombatBackgroundScroller>();
        }

        if (_cachedScroller == null)
        {
            combinedBounds = default;
            return false;
        }

        if (_cachedBgRenderer == null)
        {
            _cachedBgRenderer = _cachedScroller.GetComponent<SpriteRenderer>();
        }

        if (_cachedBgRenderer == null)
        {
            combinedBounds = default;
            return false;
        }

        combinedBounds = _cachedBgRenderer.bounds;

        if (_cachedBgCloneRenderer == null && _cachedScroller.transform.parent != null)
        {
            Transform clone = _cachedScroller.transform.parent.Find("BackGround_Clone");
            if (clone != null)
            {
                _cachedBgCloneRenderer = clone.GetComponent<SpriteRenderer>();
            }
        }

        if (_cachedBgCloneRenderer != null && _cachedBgCloneRenderer.gameObject.activeInHierarchy)
        {
            combinedBounds.Encapsulate(_cachedBgCloneRenderer.bounds);
        }

        return true;
    }

    private void LateUpdate()
    {
        if (!_initialized) return;
        ApplyCameraState();
    }

    private void ApplyCameraState()
    {
        Camera cam = ResolveCamera();
        if (cam == null) return;

        cam.transform.position = _basePos + _shakeOffset;
        if (cam.orthographic)
        {
            cam.orthographicSize = _currentOrthoSize;
        }
    }

    private void KillAllTweens()
    {
        _panTween?.Kill();
        _panTween = null;
        _shakeTween?.Kill();
        _shakeTween = null;
    }

    private static float EaseOutCubic(float t)
    {
        float inv = 1f - t;
        return 1f - inv * inv * inv;
    }

    private static float EaseInOutCubic(float t)
    {
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
    }
}
