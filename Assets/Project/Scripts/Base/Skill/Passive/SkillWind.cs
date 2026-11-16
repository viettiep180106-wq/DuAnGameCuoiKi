using UnityEngine;
using DG.Tweening;

/// <summary>
/// Kỹ năng Passive Hệ Kim: Khiên Kim Cang (Hợp Kim Phòng Ngự) theo chuẩn GDD.
/// - Đối tượng: Bản thân Player.
/// - Mốc 3 (Tier 1): Nhận Khiên = 15% Max HP.
/// - Mốc 6 (Tier 2): Nhận Khiên = 25% Max HP đầu trận. Làm mới khiên (15% Max HP) sau mỗi 3 Round.
/// - Mốc 9 (Tier 3): Nhận Khiên = 40% Max HP đầu trận. Làm mới khiên (25% Max HP) sau mỗi 2 Round + Miễn nhiễm 1 hiệu ứng khống chế đầu tiên.
/// </summary>
public class SkillWind : SkillPassive
{
    [Header("Metal / Shield Settings")]
    public float visualScale = 2.0f;

    public SkillWind()
    {
        stoneType = TypeStone.Wind;
    }

    public override void OnDespawn()
    {
        transform.DOKill();
        transform.SetParent(null);
        base.OnDespawn();
    }

    public override void Active()
    {
        // Hủy toàn bộ timer cũ để lá chắn không tự động biến mất
        transform.DOKill();
        DOTween.Kill(gameObject.GetInstanceID());

        Player player = Player.Instance != null ? Player.Instance : FindFirstObjectByType<Player>();
        if (player == null)
        {
            DespawnSelf();
            return;
        }

        int tier = Mathf.Max(1, (int)(skillValue / 3f));
        player.MetalShieldTier = Mathf.Max(player.MetalShieldTier, tier);

        float shieldPercent = tier switch
        {
            1 => 0.15f,
            2 => 0.25f,
            _ => 0.40f
        };

        float shieldAmount = player.MaxHealth * shieldPercent;

        if (tier >= 3)
        {
            player.HasImmunityCrowdControl = true;
        }

        // Sinh hạt hiệu ứng ma thuật Kim Cang
        EnemyBase.SpawnSkillVFX(player.transform.position + Vector3.up * 1.0f, ElementType.Metal);

        // NẾU KHIÊN ĐANG TỒN TẠI: Chỉ cộng dồn chỉ số khiên & cập nhật text, KHÔNG bật thêm visual GameObject!
        if (player.HasActiveShield())
        {
            player.AddShield(shieldAmount);
            player.PulseShieldVisual();

            if (DamageTextManager.Instance != null)
            {
                DamageTextManager.Instance.SpawnCustomText($"+{Mathf.RoundToInt(shieldAmount)} [KHIÊN]", new Color(1f, 0.92f, 0.35f), player.transform.position + Vector3.up * 2.0f, 1.4f);
            }

            DespawnSelf();
            return;
        }

        // KHIÊN CHƯA TỒN TẠI: Tạo mới và gắn làm con của Player để di chuyển theo người chơi
        transform.SetParent(player.transform, false);
        transform.localPosition = new Vector3(0f, 0.2f, -0.2f);
        float pScale = Mathf.Max(0.01f, player.transform.localScale.x);
        visualScale = 14.0f / pScale;
        transform.localScale = Vector3.one * (visualScale * 0.4f);
        transform.DOScale(visualScale, 0.45f).SetEase(Ease.OutBack);

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 30;
            sr.color = new Color(1f, 0.92f, 0.35f, 0.80f); // Khiên mờ hoàng kim bảo vệ
        }

        // Bàn giao Visual cho Player quản lý: duy trì bảo vệ và hiển thị text HP khiên, tự hủy khi về 0 hoặc hết wave
        player.AddShield(shieldAmount, gameObject);
    }

    private void DespawnSelf()
    {
        transform.DOKill();
        transform.SetParent(null);
        if (gameObject.activeInHierarchy)
        {
            if (ObjectPoolingManager.Instance != null)
                ObjectPoolingManager.Instance.Despawn(gameObject);
            else
            {
                if (Application.isPlaying)
                    Destroy(gameObject);
                else
                    DestroyImmediate(gameObject);
            }
        }
    }
}
