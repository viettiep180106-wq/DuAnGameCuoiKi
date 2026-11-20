using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Quản lý hiển thị các icon Buff và Debuff người chơi nhận được trên thanh Buff_Debuff_Container
/// </summary>
public class BuffDebuffContainerUI : MonoBehaviour
{
    public static BuffDebuffContainerUI Instance { get; private set; }

    [Header("Icon Settings")]
    [SerializeField] private float iconSize = 36f;
    [SerializeField] private float spacing = 8f;

    private readonly List<GameObject> _spawnedIcons = new List<GameObject>();
    private HorizontalLayoutGroup _layoutGroup;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        EnsureLayoutGroup();
    }

    private void OnEnable()
    {
        InterWaveEventManager.OnBuffDebuffApplied += AddBuffDebuffIcon;
        InterWaveEventManager.OnBuffDebuffReset += ClearAllIcons;

        // Tự động khôi phục icon nếu đã có buff/debuff trong level
        SyncExistingBuffs();
    }

    private void OnDisable()
    {
        InterWaveEventManager.OnBuffDebuffApplied -= AddBuffDebuffIcon;
        InterWaveEventManager.OnBuffDebuffReset -= ClearAllIcons;
    }

    private void EnsureLayoutGroup()
    {
        _layoutGroup = GetComponent<HorizontalLayoutGroup>();
        if (_layoutGroup == null)
        {
            _layoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();
        }

        _layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        _layoutGroup.spacing = spacing;
        _layoutGroup.childControlWidth = false;
        _layoutGroup.childControlHeight = false;
        _layoutGroup.childForceExpandWidth = false;
        _layoutGroup.childForceExpandHeight = false;
        _layoutGroup.padding = new RectOffset(6, 6, 2, 2);

        // Đảm bảo Image nền của container có độ trong suốt nhẹ hoặc không cản raycast
        Image containerImg = GetComponent<Image>();
        if (containerImg != null)
        {
            containerImg.raycastTarget = false;
            // Màu nền tối mờ tinh tế nếu chưa có sprite
            if (containerImg.sprite == null)
            {
                containerImg.color = new Color(0f, 0f, 0f, 0.35f);
            }
        }
    }

    public void SyncExistingBuffs()
    {
        if (InterWaveEventManager.Instance == null) return;

        ClearAllIcons();

        var buffs = InterWaveEventManager.Instance.ActiveBuffsInLevel;
        if (buffs != null)
        {
            for (int i = 0; i < buffs.Count; i++)
            {
                AddBuffDebuffIcon(buffs[i], true);
            }
        }

        var debuffs = InterWaveEventManager.Instance.ActiveDebuffsInLevel;
        if (debuffs != null)
        {
            for (int i = 0; i < debuffs.Count; i++)
            {
                AddBuffDebuffIcon(debuffs[i], false);
            }
        }
    }

    /// <summary>
    /// Thêm icon Buff (viền xanh lá) hoặc Debuff (viền đỏ) vào thanh container kèm animation pop-in
    /// </summary>
    public void AddBuffDebuffIcon(BuffDebuffType type, bool isBuff)
    {
        // Các buff tức thời 1 lần (Hồi máu, Hồi lõi, Reset CD) không thêm vào thanh icon container
        if (InterWaveEventManager.IsInstantBuff(type)) return;

        EnsureLayoutGroup();

        BuffDebuffInfo info = BuffDebuffDatabase.Instance != null ? BuffDebuffDatabase.Instance.Get(type) : null;
        if (info == null || info.icon == null) return;

        // 1. Tạo GameObject Frame cho Item
        GameObject item = new GameObject($"BuffIcon_{type}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        item.transform.SetParent(transform, false);

        RectTransform rt = item.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(iconSize, iconSize);

        Image frameImg = item.GetComponent<Image>();
        frameImg.raycastTarget = false;
        // Khung viền: Xanh lá neon cho Buff, Đỏ rực cho Debuff
        frameImg.color = isBuff ? new Color(0.18f, 0.85f, 0.35f, 0.9f) : new Color(0.95f, 0.22f, 0.22f, 0.9f);

        // 2. Tạo Image con bên trong hiển thị sprite icon
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObj.transform.SetParent(item.transform, false);

        RectTransform iconRT = iconObj.GetComponent<RectTransform>();
        iconRT.anchorMin = Vector2.zero;
        iconRT.anchorMax = Vector2.one;
        iconRT.offsetMin = new Vector2(2.5f, 2.5f);
        iconRT.offsetMax = new Vector2(-2.5f, -2.5f);

        Image iconImg = iconObj.GetComponent<Image>();
        iconImg.sprite = info.icon;
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        // 3. Hiệu ứng xuất hiện Pop-in nảy nhẹ đặc sắc
        item.transform.localScale = Vector3.zero;
        item.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        item.transform.DOPunchRotation(new Vector3(0, 0, isBuff ? 12f : -12f), 0.35f, 5);

        _spawnedIcons.Add(item);
    }

    /// <summary>
    /// Xóa toàn bộ icon trên thanh khi reset level
    /// </summary>
    public void ClearAllIcons()
    {
        for (int i = _spawnedIcons.Count - 1; i >= 0; i--)
        {
            if (_spawnedIcons[i] != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(_spawnedIcons[i]);
                }
                else
                {
                    Destroy(_spawnedIcons[i]);
                }
#else
                Destroy(_spawnedIcons[i]);
#endif
            }
        }
        _spawnedIcons.Clear();
    }
}
