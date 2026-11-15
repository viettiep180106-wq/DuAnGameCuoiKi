using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public enum ActiveSkillType
{
    MeteorStrike,    // Mưa Sao Băng: Sát thương AoE toàn bộ quái trên sân
    AbsoluteFreeze,  // Băng Hà Phong Ấn: Đóng băng toàn bộ quái trong 3s
    SacredShield,    // Khiên Thánh Vực: Hấp thụ sát thương bằng 40% Max HP
    BladeStorm       // Kiếm Khí Bão Táp: Phóng kiếm khí xuyên thủng toàn bộ kẻ địch
}

[RequireComponent(typeof(FillBarUI), typeof(TextUI))]
public class SkillActive : MonoBehaviour
{
    [Header("Skill Settings")]
    public string skillName = "Thiên Thạch Ma Pháp";
    public ActiveSkillType skillType = ActiveSkillType.MeteorStrike;
    public ElementType element = ElementType.Fire;
    public float cooldownTime = 15f;
    public float skillDamageMultiplier = 2.5f;

    [Header("Components")]
    [SerializeField] private Button skillButton;
    [SerializeField] private FillBarUI fillBar;
    [SerializeField] private TextUI cooldownText;
    [SerializeField] private Image iconImage;

    [Header("Unlock Status (Boss Drop Mechanic)")]
    [SerializeField] private bool isUnlocked = true;

    private float currentCooldown = 0f;
    private bool isReady = true;
    private bool isLocked = false; // Phục vụ cơ chế Debuff khóa skill

    public bool IsReady => isReady && !isLocked && isUnlocked;
    public bool IsUnlocked => isUnlocked;

    private void Awake()
    {
        if (fillBar == null) fillBar = GetComponent<FillBarUI>();
        if (cooldownText == null) cooldownText = GetComponent<TextUI>();
        if (skillButton == null) skillButton = GetComponent<Button>();
        if (iconImage == null) iconImage = GetComponent<Image>();

        if (skillButton != null)
        {
            skillButton.onClick.AddListener(OnSkillButtonClicked);
        }
    }

    private void Start()
    {
        ResetCooldownInstant();
        CheckInitialUnlockStatus();

        if (PlayerLoadoutManager.Instance != null)
        {
            PlayerLoadoutManager.Instance.OnActiveSkillUnlocked += HandleActiveSkillUnlocked;
        }
    }

    private void OnDestroy()
    {
        if (PlayerLoadoutManager.Instance != null)
        {
            PlayerLoadoutManager.Instance.OnActiveSkillUnlocked -= HandleActiveSkillUnlocked;
        }
    }

    private void CheckInitialUnlockStatus()
    {
        if (PlayerLoadoutManager.Instance != null)
        {
            isUnlocked = PlayerLoadoutManager.Instance.IsActiveSkillUnlocked(skillType);
        }
        UpdateUnlockVisuals(false);
    }

    private void HandleActiveSkillUnlocked(ActiveSkillDataSO skillData)
    {
        if (skillData != null && skillData.SkillType == skillType)
        {
            UnlockSkill();
        }
    }

    public void UnlockSkill()
    {
        isUnlocked = true;
        UpdateUnlockVisuals(true);
    }

    private void UpdateUnlockVisuals(bool withAnimation)
    {
        if (skillButton != null)
        {
            skillButton.interactable = isUnlocked;
        }

        if (iconImage != null)
        {
            Color targetColor = isUnlocked ? Color.white : new Color(0.35f, 0.35f, 0.35f, 0.45f);
            if (withAnimation)
            {
                transform.DOKill();
                transform.DOPunchScale(Vector3.one * 0.4f, 0.5f, 5, 0.5f);
                iconImage.DOColor(targetColor, 0.4f);
            }
            else
            {
                iconImage.color = targetColor;
            }
        }
    }

    private void Update()
    {
        if (!isUnlocked || isLocked) return;

        if (!isReady)
        {
            currentCooldown -= Time.deltaTime;
            if (currentCooldown <= 0f)
            {
                OnCooldownComplete();
            }
            else
            {
                UpdateCooldownUI();
            }
        }
    }

    private void OnSkillButtonClicked()
    {
        if (IsReady)
        {
            ActiveSkill();
        }
    }

    public virtual void ActiveSkill()
    {
        if (!IsReady) return;

        // Bắt đầu thời gian hồi chiêu
        StartCooldown(cooldownTime);

        // Hiệu ứng bấm nút nảy nhẹ
        transform.DOKill();
        transform.DOScale(Vector3.one * 0.9f, 0.1f).OnComplete(() =>
        {
            transform.DOScale(Vector3.one, 0.1f);
        });

        // Thực thi hiệu ứng kỹ năng theo loại
        ExecuteSkillEffect();
    }

    private void ExecuteSkillEffect()
    {
        Player player = Player.Instance != null ? Player.Instance : FindFirstObjectByType<Player>();
        float playerAtk = player != null ? player.PlayerATK : 200f;

        switch (skillType)
        {
            case ActiveSkillType.MeteorStrike:
                // Sát thương hỏa toàn sân
                DealAoEDamageToAllEnemies(playerAtk * skillDamageMultiplier, ElementType.Fire);
                break;

            case ActiveSkillType.AbsoluteFreeze:
                // Đóng băng toàn bộ quái trong 3.5s
                FreezeAllEnemies(3.5f);
                break;

            case ActiveSkillType.SacredShield:
                // Tăng máu và khiên
                if (player != null)
                {
                    player.Health(player.MaxHealth * 0.35f);
                }
                break;

            case ActiveSkillType.BladeStorm:
                // Kiếm khí xuyên thấu
                DealAoEDamageToAllEnemies(playerAtk * skillDamageMultiplier * 1.2f, ElementType.Metal);
                break;
        }

        // Báo cho Player tung animation tấn công
        if (player != null)
        {
            player.PlayAttackAnimation();
        }
    }

    /// <summary>
    /// Sát thương AoE Zero-GC lấy trực tiếp từ EnemyController.LivingEnemies
    /// </summary>
    private void DealAoEDamageToAllEnemies(float rawDamage, ElementType elem)
    {
        if (EnemyController.instance != null)
        {
            var living = EnemyController.instance.LivingEnemies;
            for (int i = living.Count - 1; i >= 0; i--)
            {
                EnemyBase enemy = living[i];
                if (enemy != null && enemy.gameObject.activeInHierarchy && enemy.CurrentHP > 0)
                {
                    enemy.TakeDamage(rawDamage, true, elem);
                }
            }
        }
    }

    /// <summary>
    /// Đóng băng Zero-GC lấy trực tiếp từ EnemyController.LivingEnemies
    /// </summary>
    private void FreezeAllEnemies(float duration)
    {
        if (EnemyController.instance != null)
        {
            var living = EnemyController.instance.LivingEnemies;
            for (int i = 0; i < living.Count; i++)
            {
                EnemyBase enemy = living[i];
                if (enemy != null && enemy.gameObject.activeInHierarchy && enemy.CurrentHP > 0)
                {
                    EffectPayLoad freeze = new EffectPayLoad
                    {
                        EffectType = Effect.Freeze,
                        duration = duration,
                        slowRate = 0.8f,
                        value = 10f
                    };
                    enemy.PlayEffect(freeze);
                }
            }
        }
    }

    public void StartCooldown(float cd)
    {
        currentCooldown = cd;
        isReady = false;

        if (fillBar != null && fillBar.uiImage != null)
        {
            if (fillBar.uiImage.gameObject != this.gameObject)
            {
                fillBar.uiImage.gameObject.SetActive(true);
            }
            fillBar.SetFillInstant(1f);
            fillBar.DoFill(0f, cd);
        }

        if (cooldownText != null && cooldownText.textValueCurrent != null)
        {
            if (cooldownText.textValueCurrent.gameObject != this.gameObject)
            {
                cooldownText.textValueCurrent.gameObject.SetActive(true);
            }
            cooldownText.UpdateText(0, cd);
        }
    }

    private void UpdateCooldownUI()
    {
        if (cooldownText != null && cooldownText.textValueCurrent != null)
        {
            cooldownText.textValueCurrent.text = $"{Mathf.CeilToInt(currentCooldown)}s";
        }
    }

    private void OnCooldownComplete()
    {
        isReady = true;
        currentCooldown = 0f;

        if (fillBar != null && fillBar.uiImage != null)
        {
            if (fillBar.uiImage.gameObject != this.gameObject)
            {
                fillBar.uiImage.gameObject.SetActive(false);
            }
            else
            {
                fillBar.SetFillInstant(0f);
            }
        }

        if (cooldownText != null && cooldownText.textValueCurrent != null)
        {
            cooldownText.textValueCurrent.text = "";
            if (cooldownText.textValueCurrent.gameObject != this.gameObject)
            {
                cooldownText.textValueCurrent.gameObject.SetActive(false);
            }
        }

        // Hiệu ứng Pulse nảy sáng khi skill sẵn sàng (GDD Item B12)
        transform.DOKill();
        transform.DOScale(Vector3.one * 1.15f, 0.2f).SetLoops(2, LoopType.Yoyo);
    }

    public void ResetCooldownInstant()
    {
        currentCooldown = 0f;
        OnCooldownComplete();
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;
        if (iconImage != null)
        {
            iconImage.color = locked ? Color.gray : (isUnlocked ? Color.white : new Color(0.35f, 0.35f, 0.35f, 0.45f));
        }
    }

    public void SetUpTimeUI()
    {
        StartCooldown(cooldownTime);
    }
}
