using UnityEngine;

/// <summary>
/// Quản lý vòng đời và tự động hoàn trả Particle VFX về Pool sau khi hiệu ứng kết thúc.
/// Đảm bảo Zero-GC và không rò rỉ tài nguyên.
/// </summary>
public class VFXParticleAutoDespawn : MonoBehaviour, IPoolable
{
    [SerializeField] private float duration = 1.2f;
    private ParticleSystem _ps;

    private void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
    }

    public void OnSpawn()
    {
        if (_ps != null)
        {
            _ps.Clear();
            _ps.Play();
        }
        CancelInvoke();
        Invoke(nameof(DespawnSelf), duration);
    }

    public void OnDespawn()
    {
        CancelInvoke();
        if (_ps != null)
        {
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
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
}
