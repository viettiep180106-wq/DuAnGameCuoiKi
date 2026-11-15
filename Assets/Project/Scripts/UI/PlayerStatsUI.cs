using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class PlayerStatsUI : MonoBehaviour
{
    public static PlayerStatsUI Instance { get; private set; }

    [Header("Stats UI Texts")]
    [SerializeField] private TMP_Text atkText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text defText;
    [SerializeField] private TMP_Text energyCoresText;
    [SerializeField] private Image energyCoreIcon;

    public Transform EnergyCoreTargetTransform => energyCoreIcon != null ? energyCoreIcon.transform : (energyCoresText != null ? energyCoresText.transform : transform);

    private int displayedEnergy = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (Player.Instance != null)
        {
            Player.Instance.OnEnergyCoresChanged += UpdateEnergyUI;
            displayedEnergy = Player.Instance.EnergyCores;
            UpdateStatsUI();
            UpdateEnergyUI(displayedEnergy);
        }
        else
        {
            // Hiển thị giá trị mặc định chuẩn Level 1 từ GDD nếu chưa spawn Player
            SetDefaultStats();
        }
    }

    private void Update()
    {
        // Cập nhật thông số liên tục nếu có thay đổi trong chiến đấu hoặc sau khi nhận Buff/Debuff
        if (Player.Instance != null)
        {
            if (hpText != null)
            {
                float curHp = Player.Instance.CurrentHP > 0 ? Player.Instance.CurrentHP : (Player.Instance.MaxHealth > 0 ? Player.Instance.MaxHealth : 500f);
                float maxHp = Player.Instance.MaxHealth > 0 ? Player.Instance.MaxHealth : 500f;
                hpText.text = $"HP: {Mathf.RoundToInt(curHp)}/{Mathf.RoundToInt(maxHp)}";
            }
            if (atkText != null)
            {
                float atk = Player.Instance.PlayerATK > 0 ? Player.Instance.PlayerATK : 200f;
                atkText.text = $"ATK: {Mathf.RoundToInt(atk)}";
            }
            if (defText != null)
            {
                float defVal = Player.Instance.data != null ? Player.Instance.data.Dev : 0f;
                defText.text = $"DEF: {Mathf.RoundToInt(defVal)}";
            }
            if (energyCoresText != null && displayedEnergy != Player.Instance.EnergyCores)
            {
                UpdateEnergyUI(Player.Instance.EnergyCores);
            }
        }
    }

    private void OnDestroy()
    {
        if (Player.Instance != null)
        {
            Player.Instance.OnEnergyCoresChanged -= UpdateEnergyUI;
        }
    }

    public void SetDefaultStats()
    {
        if (atkText != null) atkText.text = "ATK: 200";
        if (hpText != null) hpText.text = "HP: 500/500";
        if (defText != null) defText.text = "DEF: 0";
        if (energyCoresText != null) energyCoresText.text = energyCoreIcon != null ? "50" : "COIN: 50";
    }

    public void UpdateStatsUI()
    {
        if (Player.Instance == null)
        {
            SetDefaultStats();
            return;
        }

        if (atkText != null)
        {
            atkText.text = $"ATK: {Mathf.RoundToInt(Player.Instance.PlayerATK)}";
        }

        if (hpText != null)
        {
            hpText.text = $"HP: {Mathf.RoundToInt(Player.Instance.CurrentHP)}/{Mathf.RoundToInt(Player.Instance.MaxHealth)}";
        }

        if (defText != null)
        {
            float defVal = Player.Instance.data != null ? Player.Instance.data.Dev : 0f;
            defText.text = $"DEF: {Mathf.RoundToInt(defVal)}";
        }

        if (energyCoresText != null)
        {
            energyCoresText.text = energyCoreIcon != null ? $"{Player.Instance.EnergyCores}" : $"COIN: {Player.Instance.EnergyCores}";
        }
    }

    public void UpdateEnergyUI(int newAmount)
    {
        if (energyCoresText == null) return;

        energyCoresText.DOKill();
        energyCoresText.transform.DOKill();

        if (newAmount < displayedEnergy)
        {
            // TIÊU HAO NĂNG LƯỢNG: Trừ số tức thời, phản ánh sát thực tế, không bắt chờ đợi
            displayedEnergy = newAmount;
            energyCoresText.text = energyCoreIcon != null ? $"{displayedEnergy}" : $"COIN: {displayedEnergy}";
            energyCoresText.transform.localScale = Vector3.one;
            energyCoresText.transform.DOPunchScale(Vector3.one * 0.2f, 0.15f, 5, 0.5f);

            if (energyCoreIcon != null)
            {
                energyCoreIcon.DOKill();
                energyCoreIcon.color = new Color(1f, 0.35f, 0.35f, 1f); // Flash đỏ nhẹ khi tiêu hao
                energyCoreIcon.DOColor(Color.white, 0.18f);
            }
        }
        else
        {
            // NHẬN NĂNG LƯỢNG: Nảy số nhanh và mượt trong 0.2s
            int oldVal = displayedEnergy;
            displayedEnergy = newAmount; // Set đích ngay để Update() không bị loop lại
            energyCoresText.transform.DOScale(Vector3.one * 1.2f, 0.12f).OnComplete(() =>
            {
                energyCoresText.transform.DOScale(Vector3.one, 0.12f);
            });

            DOVirtual.Int(oldVal, newAmount, 0.2f, (val) =>
            {
                if (energyCoresText != null)
                {
                    energyCoresText.text = energyCoreIcon != null ? $"{val}" : $"COIN: {val}";
                }
            }).SetEase(Ease.OutQuad);
        }
    }

    /// <summary>
    /// Hiệu ứng juice khi lõi năng lượng bay chạm đích vào icon lõi trên Bar:
    /// Scale lên 1.1x rồi về lại 1.0x kèm hiệu ứng flash phát sáng vàng rực rỡ.
    /// </summary>
    public void PlayEnergyCoreCollectJuice()
    {
        if (energyCoreIcon == null) return;

        energyCoreIcon.transform.DOKill();
        energyCoreIcon.DOKill();

        energyCoreIcon.transform.localScale = Vector3.one;
        energyCoreIcon.transform.DOScale(Vector3.one * 1.1f, 0.12f).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            energyCoreIcon.transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.InQuad);
        });

        energyCoreIcon.color = new Color(1.35f, 1.25f, 0.5f, 1f);
        energyCoreIcon.DOColor(Color.white, 0.35f).SetEase(Ease.OutQuad);
    }
}

