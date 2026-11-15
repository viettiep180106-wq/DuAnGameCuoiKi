using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Đạn chưởng ma pháp của quái đánh xa (Ranged Enemy), tự động đổi màu theo hệ nguyên tố của quái
/// </summary>
[RequireComponent(typeof(CircleCollider2D), typeof(Rigidbody2D))]
public class EnemyProjectile : MonoBehaviour, IPoolable
{
    [SerializeField] private SpriteRenderer _visualRenderer;
    [SerializeField] private CircleCollider2D _collider;
    [SerializeField] private Rigidbody2D _rigidbody;
    [SerializeField] private TrailRenderer _trailRenderer;

    private float _damage = 10f;
    private ElementType _element = ElementType.Wood;
    private bool _hasHit = false;
    private Tween _moveTween;

    private void Awake()
    {
        if (_visualRenderer == null) _visualRenderer = GetComponent<SpriteRenderer>();
        if (_collider == null) _collider = GetComponent<CircleCollider2D>();
        if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody2D>();
        if (_trailRenderer == null) _trailRenderer = GetComponent<TrailRenderer>();

        if (_collider != null) _collider.isTrigger = true;
        if (_rigidbody != null)
        {
            _rigidbody.gravityScale = 0f;
            _rigidbody.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    public void OnSpawn()
    {
        _hasHit = false;
        transform.localScale = Vector3.one * 7.5f;
        if (_visualRenderer != null)
        {
            _visualRenderer.sortingLayerName = "Default";
            _visualRenderer.sortingOrder = 32;
        }
        if (_trailRenderer != null)
        {
            _trailRenderer.Clear();
            _trailRenderer.enabled = true;
            _trailRenderer.widthMultiplier = 1.5f;
            _trailRenderer.sortingLayerName = "Default";
            _trailRenderer.sortingOrder = 31;
        }
    }

    public void OnDespawn()
    {
        _moveTween?.Kill();
        _moveTween = null;
        if (_trailRenderer != null)
        {
            _trailRenderer.enabled = false;
            _trailRenderer.Clear();
        }
    }

    /// <summary>
    /// Bắn đạn hướng về phía mục tiêu với màu sắc theo hệ nguyên tố của quái
    /// </summary>
    public void Launch(float damage, ElementType element, Vector3 spawnPos, Vector3 targetPos, float speed = 5.0f)
    {
        _damage = damage;
        _element = element;
        _hasHit = false;

        transform.position = spawnPos;

        // Tự động nhuộm màu ma pháp theo hệ nguyên tố
        Color elemColor = GetProjectileColor(element);
        if (_visualRenderer != null)
        {
            _visualRenderer.color = elemColor;
        }
        if (_trailRenderer != null)
        {
            _trailRenderer.startColor = elemColor;
            _trailRenderer.endColor = new Color(elemColor.r, elemColor.g, elemColor.b, 0f);
        }

        // Tính toán hướng bay và thời gian
        Vector2 start2D = spawnPos;
        Vector2 target2D = targetPos;
        float distance = Vector2.Distance(start2D, target2D);
        float duration = Mathf.Max(0.1f, distance / Mathf.Max(1f, speed));

        // Xoay đầu đạn về hướng mục tiêu
        Vector2 dir = (target2D - start2D).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Hiệu ứng bay mượt mà
        _moveTween?.Kill();
        _moveTween = transform.DOMove(targetPos, duration)
            .SetEase(Ease.Linear)
            .OnComplete(OnReachTarget);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_hasHit) return;

        if (collision.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();
            if (player != null)
            {
                ApplyDamage(player);
            }
        }
    }

    private void OnReachTarget()
    {
        if (_hasHit) return;

        // Nếu chạm điểm đích mà chưa trigger (do góc chạm), kiểm tra Player gần đích
        Player player = Player.Instance != null ? Player.Instance : FindFirstObjectByType<Player>();
        if (player != null)
        {
            float dist = Vector2.Distance(transform.position, player.transform.position);
            if (dist <= 0.9f)
            {
                ApplyDamage(player);
                return;
            }
        }

        DespawnSelf();
    }

    private void ApplyDamage(Player player)
    {
        _hasHit = true;
        _moveTween?.Kill();

        if (player != null && player.CurrentHP > 0)
        {
            player.TakeDamage(_damage, false, _element);
            EnemyBase.SpawnSkillVFX(transform.position, _element);
            Camera.main?.transform.DOShakePosition(0.15f, 0.2f, 10, 90f, false, true);
        }

        // Hiệu ứng nổ nhỏ khi trúng mục tiêu rồi biến mất
        transform.DOScale(transform.localScale * 1.4f, 0.10f).OnComplete(() =>
        {
            DespawnSelf();
        });
    }

    private void DespawnSelf()
    {
        if (ObjectPoolingManager.Instance != null)
        {
            ObjectPoolingManager.Instance.Despawn(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Lấy màu sắc ma pháp rực rỡ đại diện cho 5 hệ nguyên tố
    /// </summary>
    public static Color GetProjectileColor(ElementType element)
    {
        return element switch
        {
            ElementType.Wood => new Color(0.2f, 0.95f, 0.35f, 0.95f),  // Xanh lá cây ma thuật
            ElementType.Fire => new Color(1.0f, 0.30f, 0.12f, 0.95f),  // Đỏ cam rực lửa
            ElementType.Earth => new Color(0.92f, 0.68f, 0.22f, 0.95f), // Nâu vàng đất đá
            ElementType.Water => new Color(0.22f, 0.68f, 1.0f, 0.95f),  // Xanh lam băng giá
            ElementType.Metal => new Color(1.0f, 0.88f, 0.35f, 0.95f),  // Vàng kim sáng chói
            _ => Color.white
        };
    }
}
