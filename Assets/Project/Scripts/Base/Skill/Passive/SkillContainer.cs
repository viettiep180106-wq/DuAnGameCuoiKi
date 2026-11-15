using UnityEngine;
using DG.Tweening;

public class SkillContainer : MonoBehaviour, IPoolable
{
    [Header("Visual Components")]
    [SerializeField] private SpriteRenderer _iconRenderer;
    [SerializeField] private TextMesh _tierTextMesh;

    [Header("Runtime State")]
    private SkillPassiveDataSO _equippedSkillData;
    private int _matchCount;
    private bool _isDespawned = false;

    public SkillPassiveDataSO EquippedSkillData => _equippedSkillData;
    public int MatchCount => _matchCount;
    public TypeStone StoneType => _equippedSkillData != null ? _equippedSkillData.StoneType : TypeStone.Fire;

    private void Awake()
    {
        EnsureVisualComponents();
    }

    private void EnsureVisualComponents()
    {
        // 1. Tắt Frame BoardStone cũ để không bao giờ bị các khối vuông to che màn hình
        Transform oldFrame = transform.Find("Frame");
        if (oldFrame != null)
        {
            oldFrame.gameObject.SetActive(false);
        }

        // 2. Icon biểu tượng kỹ năng nguyên tố (Fire, Ice, Wood, Earth, Wind)
        if (_iconRenderer == null)
        {
            Transform iconTrans = transform.Find("Icon");
            if (iconTrans == null)
            {
                GameObject iconObj = new GameObject("Icon", typeof(SpriteRenderer));
                iconObj.transform.SetParent(transform, false);
                _iconRenderer = iconObj.GetComponent<SpriteRenderer>();
            }
            else
            {
                _iconRenderer = iconTrans.GetComponent<SpriteRenderer>();
            }

            _iconRenderer.sortingOrder = 26;
            _iconRenderer.transform.localScale = Vector3.one * 0.9f;
        }

        // 3. Text số lượng match / tier (x3, x6, x9)
        if (_tierTextMesh == null)
        {
            Transform textTrans = transform.Find("TierText");
            if (textTrans == null)
            {
                GameObject textObj = new GameObject("TierText", typeof(TextMesh));
                textObj.transform.SetParent(transform, false);
                _tierTextMesh = textObj.GetComponent<TextMesh>();
                _tierTextMesh.characterSize = 0.45f;
                _tierTextMesh.fontSize = 32;
                _tierTextMesh.fontStyle = FontStyle.Bold;
                _tierTextMesh.alignment = TextAlignment.Center;
                _tierTextMesh.anchor = TextAnchor.LowerRight;
                _tierTextMesh.color = Color.yellow;
                textObj.transform.localPosition = new Vector3(1.2f, -1.2f, 0f);

                MeshRenderer mr = textObj.GetComponent<MeshRenderer>();
                if (mr != null) mr.sortingOrder = 27;
            }
            else
            {
                _tierTextMesh = textTrans.GetComponent<TextMesh>();
            }
        }
    }

    public void OnSpawn()
    {
        _isDespawned = false;
        transform.DOKill();
        // Hiệu ứng nảy vào hàng đợi (Juicy Bounce-in) với kích thước nhỏ gọn chuẩn xác (0.058f)
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one * 0.058f, 0.25f).SetEase(Ease.OutBack);
    }

    public void OnDespawn()
    {
        _isDespawned = true;
        _equippedSkillData = null;
        _matchCount = 0;
        transform.DOKill();
    }

    /// <summary>
    /// Khởi tạo Container với kỹ năng trang bị từ Loadout và số lượng match
    /// </summary>
    public void Initialize(SkillPassiveDataSO skillData, int matchCount)
    {
        EnsureVisualComponents();

        _equippedSkillData = skillData;
        _matchCount = matchCount;

        TypeStone stone = skillData != null ? skillData.StoneType : TypeStone.Fire;
        ElementType elem = skillData != null ? skillData.Element : ElementType.Fire;

        // Cập nhật Icon của kỹ năng trang bị (dùng trực tiếp sprite của đá nguyên tố)
        if (_iconRenderer != null)
        {
            Sprite icon = skillData != null ? skillData.Icon : null;
#if UNITY_EDITOR
            if (icon == null)
            {
                string iconName = elem switch
                {
                    ElementType.Fire => "Fire",
                    ElementType.Water => "Ice",
                    ElementType.Wood => "Wood",
                    ElementType.Earth => "Earth",
                    _ => "Wind"
                };
                icon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Project/Sprite/{iconName}.png");
            }
#endif
            _iconRenderer.sprite = icon;
            _iconRenderer.color = Color.white;
            _iconRenderer.transform.localScale = Vector3.one * 0.9f;
        }

        // Cập nhật Badge số lượng match (x3, x6, x9)
        if (_tierTextMesh != null)
        {
            _tierTextMesh.text = matchCount > 0 ? $"x{matchCount}" : "";
        }
    }

    /// <summary>
    /// Kích hoạt thi triển kỹ năng: Sinh ra đạn kỹ năng tương ứng từ Prefab đã trang bị
    /// </summary>
    public SkillPassive ExecuteSkill(Vector3 spawnPosition)
    {
        if (_equippedSkillData == null || _equippedSkillData.SkillPrefab == null)
        {
            Debug.LogWarning("[SkillContainer] Missing equipped skill prefab to execute!");
            return null;
        }

        // Sinh đạn kỹ năng từ Object Pool
        GameObject skillObj = ObjectPoolingManager.Instance.Spawn(
            _equippedSkillData.SkillPrefab,
            spawnPosition,
            Quaternion.identity
        );

        if (skillObj == null) return null;

        SkillPassive skill = skillObj.GetComponent<SkillPassive>();
        if (skill != null)
        {
            skill.transform.position = spawnPosition;
            skill.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            skill.Initialize(_matchCount);
            skill.Active();
        }

        // Hiệu ứng phóng ra và hoàn trả container về pool
        transform.DOKill();
        transform.DOScale(Vector3.zero, 0.15f).OnComplete(() =>
        {
            if (gameObject.activeSelf)
            {
                ObjectPoolingManager.Instance.Despawn(gameObject);
            }
        });

        return skill;
    }
}
