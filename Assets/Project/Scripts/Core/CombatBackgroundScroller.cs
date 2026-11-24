using UnityEngine;

/// <summary>
/// Điều khiển cuộn phông nền trận đấu (SceneGameCombat/BackGround)
/// theo tốc độ di chuyển của người chơi và ngược hướng quái vật.
/// Tuân thủ Zero-GC theo unity-performance.md và chuẩn coding unity-core.md.
/// </summary>
public class CombatBackgroundScroller : MonoBehaviour
{
    [Header("Direction Settings")]
    [Tooltip("Hướng di chuyển của nền. Mặc định là sang trái (Vector2.left), ngược lại với hướng quái vật (+X).")]
    [SerializeField] private Vector2 _scrollDirection = Vector2.left;

    [Tooltip("Đảo ngược chiều di chuyển nếu muốn tinh chỉnh nhanh.")]
    [SerializeField] private bool _invertDirection = false;

    [Header("Speed Settings")]
    [Tooltip("Hệ số nhân tốc độ di chuyển.")]
    [SerializeField] private float _speedMultiplier = 1.0f;

    [Tooltip("Tốc độ mặc định nếu chưa khởi tạo Player hoặc trong test scene (đã nhân 2 theo yêu cầu).")]
    [SerializeField] private float _fallbackSpeed = 3.2f;

    [Tooltip("Tốc độ cuộn nền nhanh trong giai đoạn chuyển wave (Pre-Wave / Marching 3.0s).")]
    [SerializeField] private float _marchingSpeed = 10.0f;

    [Header("Looping & Boundary Settings")]
    [Tooltip("Chiều rộng của sprite nền để ghép nối vòng lặp vô hạn không kẽ hở.")]
    [SerializeField] private float _backgroundWidth = 20.0f;

    [Header("State Control")]
    [Tooltip("Chỉ cuộn trong giai đoạn chuyển wave (Pre-Wave / Marching). Đứng yên tuyệt đối khi vào Wave.")]
    [SerializeField] private bool _onlyScrollInMarching = true;

    // Cached References
    private Transform _transform;
    private Transform _clonePiece;
    private Vector3 _startLocalPos;
    private float _accumulatedDistance;
    private Player _cachedPlayer;
    private GameManager _cachedGameManager;
    private bool _manualScrollOverride;
    private bool _hasManualOverride;
    private bool _isMarchingActive;

    private Transform CachedTransform
    {
        get
        {
            if (_transform == null)
            {
                _transform = transform;
            }
            return _transform;
        }
    }

    // Public Properties
    public float CurrentSpeed => GetCurrentSpeed();
    public bool IsActive => ShouldScroll();
    public float AccumulatedDistance => _accumulatedDistance;
    public Vector2 EffectiveDirection => _invertDirection ? -_scrollDirection.normalized : _scrollDirection.normalized;
    public float BackgroundWidth => _backgroundWidth;

    private void Awake()
    {
        _transform = transform;
        _startLocalPos = _transform.localPosition;
        EnsureClonePiece();
    }

    private void Start()
    {
        CacheReferences();
        EnsureClonePiece();
    }

    private void EnsureClonePiece()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            float rawWidth = 20f;
            if (sr.drawMode != SpriteDrawMode.Simple && sr.size.x > 0f)
            {
                rawWidth = sr.size.x;
            }
            else if (sr.sprite != null)
            {
                rawWidth = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
            }
            float scaleX = Mathf.Abs(transform.localScale.x);
            _backgroundWidth = rawWidth * (scaleX > 0f ? scaleX : 1f);
        }

        if (_clonePiece == null && transform.parent != null)
        {
            Transform existing = transform.parent.Find("BackGround_Clone");
            if (existing != null)
            {
                _clonePiece = existing;
            }
            else
            {
                GameObject cloneObj = new GameObject("BackGround_Clone");
                cloneObj.transform.SetParent(transform.parent, false);
                _clonePiece = cloneObj.transform;

                if (sr != null)
                {
                    SpriteRenderer cloneSr = cloneObj.AddComponent<SpriteRenderer>();
                    cloneSr.sprite = sr.sprite;
                    cloneSr.drawMode = sr.drawMode;
                    cloneSr.size = sr.size;
                    cloneSr.color = sr.color;
                    cloneSr.sortingLayerID = sr.sortingLayerID;
                    cloneSr.sortingOrder = sr.sortingOrder;
                    cloneSr.sharedMaterial = sr.sharedMaterial;
                }
            }

            _clonePiece.localPosition = _startLocalPos + new Vector3(_backgroundWidth, 0f, 0f);
            _clonePiece.localScale = _transform.localScale;
            _clonePiece.localRotation = _transform.localRotation;
        }
    }

    private void Update()
    {
        if (!ShouldScroll()) return;

        float speed = GetCurrentSpeed();
        if (speed <= 0f) return;

        float moveDelta = EffectiveDirection.x * speed * Time.deltaTime;
        _accumulatedDistance += Mathf.Abs(moveDelta);

        // Cuộn cả 2 mảnh nền đồng thời (Zero-GC)
        CachedTransform.localPosition += new Vector3(moveDelta, 0f, 0f);
        if (_clonePiece != null)
        {
            _clonePiece.localPosition += new Vector3(moveDelta, 0f, 0f);

            // Khi di chuyển sang trái (moveDelta < 0)
            if (moveDelta < 0f)
            {
                if (CachedTransform.localPosition.x <= _startLocalPos.x - _backgroundWidth)
                {
                    CachedTransform.localPosition = new Vector3(_clonePiece.localPosition.x + _backgroundWidth, _startLocalPos.y, _startLocalPos.z);
                }
                if (_clonePiece.localPosition.x <= _startLocalPos.x - _backgroundWidth)
                {
                    _clonePiece.localPosition = new Vector3(CachedTransform.localPosition.x + _backgroundWidth, _startLocalPos.y, _startLocalPos.z);
                }
            }
            // Khi di chuyển sang phải (moveDelta > 0)
            else if (moveDelta > 0f)
            {
                if (CachedTransform.localPosition.x >= _startLocalPos.x + _backgroundWidth)
                {
                    CachedTransform.localPosition = new Vector3(_clonePiece.localPosition.x - _backgroundWidth, _startLocalPos.y, _startLocalPos.z);
                }
                if (_clonePiece.localPosition.x >= _startLocalPos.x + _backgroundWidth)
                {
                    _clonePiece.localPosition = new Vector3(CachedTransform.localPosition.x - _backgroundWidth, _startLocalPos.y, _startLocalPos.z);
                }
            }
        }
        else
        {
            // Fallback lặp đơn nếu không có clone (như trong môi trường test cô lập)
            float wrappedOffset = (_backgroundWidth > 0f) ? Mathf.Repeat(_accumulatedDistance, _backgroundWidth) : _accumulatedDistance;
            Vector2 dir = EffectiveDirection;
            CachedTransform.localPosition = new Vector3(
                _startLocalPos.x + dir.x * wrappedOffset,
                _startLocalPos.y + dir.y * wrappedOffset,
                _startLocalPos.z
            );
        }
    }

    private void CacheReferences()
    {
        if (_cachedPlayer == null)
        {
            _cachedPlayer = Player.Instance;
        }

        if (_cachedGameManager == null)
        {
            _cachedGameManager = GameManager.instance;
        }
    }

    public bool ShouldScroll()
    {
        if (_hasManualOverride)
        {
            return _manualScrollOverride;
        }

        if (_isMarchingActive)
        {
            return true;
        }

        // Khi đang ở trong Wave (Combat, Match-3, Khám phá): Background ĐỨNG YÊN HOÀN TOÀN
        if (_cachedGameManager == null)
        {
            _cachedGameManager = GameManager.instance;
        }

        if (_cachedGameManager != null)
        {
            if (_cachedGameManager.CurrentState == GamePlayState.Combat || 
                _cachedGameManager.CurrentState == GamePlayState.MatchBoard ||
                _cachedGameManager.IsWaveCombatActive)
            {
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Kích hoạt cuộn nền trong giai đoạn hành tiến chuyển Wave (Pre-Wave)
    /// </summary>
    public void StartMarching()
    {
        _isMarchingActive = true;
    }

    /// <summary>
    /// Dừng cuộn nền khi tiếp cận Wave mới
    /// </summary>
    public void StopMarching()
    {
        _isMarchingActive = false;
    }

    private float GetCurrentSpeed()
    {
        // Giai đoạn hành tiến chuyển Wave (Pre-Wave 3.0s): Dùng tốc độ lướt nhanh riêng biệt
        if (_isMarchingActive)
        {
            return _marchingSpeed * _speedMultiplier;
        }

        if (_cachedPlayer == null)
        {
            _cachedPlayer = Player.Instance;
        }

        // Tốc độ di chuyển cơ bản nhân 2 theo yêu cầu trải nghiệm mượt mà
        float baseSpeed = (_cachedPlayer != null) ? (_cachedPlayer.MoveSpeed * 2.0f) : _fallbackSpeed;
        return baseSpeed * _speedMultiplier;
    }

    /// <summary>
    /// Ghi đè trạng thái cuộn cho mục đích test hoặc cutscene
    /// </summary>
    public void SetManualOverride(bool enableOverride, bool isScrolling)
    {
        _hasManualOverride = enableOverride;
        _manualScrollOverride = isScrolling;
    }

    /// <summary>
    /// Đặt lại vị trí ban đầu của nền
    /// </summary>
    public void ResetPosition()
    {
        _accumulatedDistance = 0f;
        if (CachedTransform != null)
        {
            _transform.localPosition = _startLocalPos;
        }
        if (_clonePiece != null)
        {
            _clonePiece.localPosition = _startLocalPos + new Vector3(_backgroundWidth, 0f, 0f);
        }
    }

    /// <summary>
    /// Cho phép thiết lập vị trí gốc thủ công
    /// </summary>
    public void SetStartLocalPosition(Vector3 pos)
    {
        _startLocalPos = pos;
        if (_clonePiece != null)
        {
            _clonePiece.localPosition = _startLocalPos + new Vector3(_backgroundWidth, 0f, 0f);
        }
    }

    /// <summary>
    /// Cập nhật hệ số tốc độ linh hoạt
    /// </summary>
    public void SetSpeedMultiplier(float multiplier)
    {
        _speedMultiplier = Mathf.Max(0f, multiplier);
    }

    /// <summary>
    /// Đảo chiều cuộn linh hoạt từ mã nguồn hoặc UI
    /// </summary>
    public void SetInvertDirection(bool invert)
    {
        _invertDirection = invert;
    }
}
