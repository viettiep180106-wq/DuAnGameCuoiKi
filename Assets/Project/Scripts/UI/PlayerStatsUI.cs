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
    private float _prevAtk = -1f;
    private float _prevHp = -1f;
    private float _prevMaxHp = -1f;
    private float _prevDef = -1f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        BindPlayerEvents();
        UpdateStatsUI();
    }

    private void OnEnable()
    {
        BindPlayerEvents();
        UpdateStatsUI();
    }

    private void BindPlayerEvents()
    {
        if (Player.Instance != null)
        {
            Player.Instance.OnStatsUpdated -= UpdateStatsUI;
            Player.Instance.OnStatsUpdated += UpdateStatsUI;

            Player.Instance.OnEnergyCoresChanged -= UpdateEnergyUI;
            Player.Instance.OnEnergyCoresChanged += UpdateEnergyUI;

            displayedEnergy = Player.Instance.EnergyCores;
        }
        else
        {
            // Hiển thị giá trị mặc định chuẩn Level 1 từ GDD nếu chưa spawn Player
            SetDefaultStats();
        }
    }

    private void OnDestroy()
    {
        if (Player.Instance != null)
        {
            Player.Instance.OnStatsUpdated -= UpdateStatsUI;
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

        float curAtk = Player.Instance.PlayerATK;
        float curHp = Player.Instance.CurrentHP;
        float curMaxHp = Player.Instance.MaxHealth;
        float curDef = Player.Instance.CurrentDEF;
        int curEnergy = Player.Instance.EnergyCores;

        // 1. Cập nhật ATK kèm hiệu ứng nảy số (Punch Scale) và đổi màu thông minh
        if (atkText != null)
        {
            atkText.text = $"ATK: {Mathf.RoundToInt(curAtk)}";
            if (_prevAtk >= 0 && Mathf.Abs(curAtk - _prevAtk) > 0.01f)
            {
                atkText.DOKill();
                atkText.transform.DOKill();
                atkText.transform.localScale = Vector3.one;
                atkText.transform.DOPunchScale(Vector3.one * 0.18f, 0.25f, 5, 0.5f);
                Color flashColor = curAtk > _prevAtk ? new Color(0.2f, 1f, 0.4f) : new Color(1f, 0.35f, 0.35f);
                atkText.color = flashColor;
                atkText.DOColor(Color.white, 0.4f);
            }
            _prevAtk = curAtk;
        }

        // 2. Cập nhật HP kèm hiệu ứng nảy số
        if (hpText != null)
        {
            hpText.text = $"HP: {Mathf.RoundToInt(curHp)}/{Mathf.RoundToInt(curMaxHp)}";
            if (_prevHp >= 0 && (Mathf.Abs(curHp - _prevHp) > 0.01f || Mathf.Abs(curMaxHp - _prevMaxHp) > 0.01f))
            {
                hpText.DOKill();
                hpText.transform.DOKill();
                hpText.transform.localScale = Vector3.one;
                hpText.transform.DOPunchScale(Vector3.one * 0.18f, 0.25f, 5, 0.5f);
                Color flashColor = (curHp > _prevHp || curMaxHp > _prevMaxHp) ? new Color(0.2f, 1f, 0.4f) : new Color(1f, 0.35f, 0.35f);
                hpText.color = flashColor;
                hpText.DOColor(Color.white, 0.4f);
            }
            _prevHp = curHp;
            _prevMaxHp = curMaxHp;
        }

        // 3. Cập nhật DEF kèm hiệu ứng nảy số
        if (defText != null)
        {
            defText.text = $"DEF: {Mathf.RoundToInt(curDef)}";
            if (_prevDef >= 0 && Mathf.Abs(curDef - _prevDef) > 0.01f)
            {
                defText.DOKill();
                defText.transform.DOKill();
                defText.transform.localScale = Vector3.one;
                defText.transform.DOPunchScale(Vector3.one * 0.18f, 0.25f, 5, 0.5f);
                Color flashColor = curDef > _prevDef ? new Color(0.3f, 0.8f, 1f) : new Color(1f, 0.4f, 0.4f);
                defText.color = flashColor;
                defText.DOColor(Color.white, 0.4f);
            }
            _prevDef = curDef;
        }

        // 4. Cập nhật số lõi năng lượng
        if (energyCoresText != null && displayedEnergy != curEnergy)
        {
            UpdateEnergyUI(curEnergy);
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
            displayedEnergy = newAmount;
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
