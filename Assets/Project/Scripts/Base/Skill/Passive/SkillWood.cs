using UnityEngine;
using DG.Tweening;

/// <summary>
/// Kỹ năng Passive Hệ Mộc: Mộc Giáp Hồi Sinh (Hấp Thụ Sinh Lực) theo chuẩn GDD.
/// - Đối tượng: Bản thân Player khi chịu đòn.
/// - Mốc 3 (Tier 1): Hồi lại 3% Max HP mỗi khi nhận sát thương.
/// - Mốc 6 (Tier 2): Hồi lại 5% Max HP mỗi khi nhận sát thương + Tăng 15% lượng hồi máu nhận vào.
/// - Mốc 9 (Tier 3): Hồi lại 8% Max HP mỗi khi nhận sát thương + Nếu HP < 30%, lập tức hồi phục 20% Max HP (1 lần/trận).
/// </summary>
public class SkillWood : SkillPassive
{
    [Header("Wood Settings")]
    public float visualScale = 2.0f;

    public SkillWood()
    {
        stoneType = TypeStone.Wood;
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

        transform.position = player.transform.position;
        transform.localScale = Vector3.one * 0.5f;

        int tier = Mathf.Max(1, (int)(skillValue / 3f));
        player.WoodArmorTier = Mathf.Max(player.WoodArmorTier, tier);

        // Kích hoạt hồi máu tức thời tượng trưng cho sinh lực phục hồi (Player.Heal sẽ tự động hiển thị số máu hồi phục)
        float directHeal = player.MaxHealth * (0.04f * tier);
        player.Heal(directHeal);

        if (player.TryPlayWoodArmorVisual(WoodArmorVisual.Kind.Activate))
        {
            var legacyRenderer = GetComponent<SpriteRenderer>();
            if (legacyRenderer)
                legacyRenderer.color = Color.clear;
            DOVirtual.DelayedCall(1f, DespawnSelf, ignoreTimeScale: false).SetId(gameObject.GetInstanceID());
            return;
        }

        // Sinh hạt hiệu ứng ma thuật Mộc Diệp
        EnemyBase.SpawnSkillVFX(player.transform.position, ElementType.Wood);

        // Hiệu ứng Visual Mộc Quang xanh lá nở rộ quanh Player (bao trọn cơ thể Player)
        visualScale = 3.2f;
        transform.localScale = Vector3.one * 1.5f;
        transform.DOScale(visualScale, 0.45f).SetEase(Ease.OutBack);
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 30;
            sr.color = new Color(0.2f, 1f, 0.4f, 0.90f);
            sr.DOFade(0f, 0.55f).SetDelay(0.45f).OnComplete(DespawnSelf);
        }
        else
        {
            DOVirtual.DelayedCall(1.0f, DespawnSelf);
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
