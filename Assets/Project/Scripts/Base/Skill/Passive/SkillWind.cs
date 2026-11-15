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

    public override void Active()
    {
        base.Active();

        Player player = Player.Instance != null ? Player.Instance : FindFirstObjectByType<Player>();
        if (player == null)
        {
            DespawnSelf();
            return;
        }

        // Vị trí hiệu ứng bao quanh Player
        transform.position = player.transform.position;
        transform.localScale = Vector3.one * 0.5f;

        int tier = Mathf.Max(1, (int)(skillValue / 3f));
        player.MetalShieldTier = Mathf.Max(player.MetalShieldTier, tier);

        float shieldPercent = tier switch
        {
            1 => 0.15f,
            2 => 0.25f,
            _ => 0.40f
        };

        float shieldAmount = player.MaxHealth * shieldPercent;
        player.AddShield(shieldAmount);

        if (tier >= 3)
        {
            player.HasImmunityCrowdControl = true;
        }

        // Hiển thị Pop-up số khiên hoàng kim rực rỡ trên đầu Player
        if (DamageTextManager.Instance != null)
        {
            DamageTextManager.Instance.SpawnCustomText($"+{Mathf.RoundToInt(shieldAmount)} SHIELD", new Color(1f, 0.88f, 0.15f), player.transform.position + Vector3.up * 1.8f, 1.4f);
        }

        // Sinh hạt hiệu ứng ma thuật Kim Cang
        EnemyBase.SpawnSkillVFX(player.transform.position, ElementType.Metal);

        // Hiệu ứng Visual Khiên Kim Cang lấp lánh quanh người chơi
        visualScale = 5.2f;
        transform.localScale = Vector3.one * 1.5f;
        transform.DOScale(visualScale, 0.45f).SetEase(Ease.OutBack);
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 30;
            sr.color = new Color(1f, 0.92f, 0.35f, 0.90f);
            sr.DOFade(0f, 0.6f).SetDelay(0.5f).OnComplete(DespawnSelf);
        }
        else
        {
            DOVirtual.DelayedCall(1.1f, DespawnSelf);
        }
    }

    private void DespawnSelf()
    {
        transform.DOKill();
        if (gameObject.activeInHierarchy)
        {
            ObjectPoolingManager.Instance.Despawn(gameObject);
        }
    }
}
