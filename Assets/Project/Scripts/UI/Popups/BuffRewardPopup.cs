using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public enum BuffType
{
    AttackBoost,  // +10% ATK
    DefenseBoost, // +20% DEF
    HealthRestore,// +15% HP
    EnergyGain    // +20 Lõi năng lượng
}

public class BuffRewardPopup : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject popupContainer;
    [SerializeField] private RectTransform cardContainer;
    [SerializeField] private Button[] cardButtons;
    [SerializeField] private TMP_Text[] cardTexts;
    [SerializeField] private Button rerollButton;
    [SerializeField] private TMP_Text titleText;

    private Action onCompletedCallback;
    private BuffType[] currentOfferedBuffs = new BuffType[3];

    private void Awake()
    {
        if (popupContainer == null) popupContainer = gameObject;

        if (rerollButton != null)
        {
            rerollButton.onClick.AddListener(OnRerollClicked);
        }

        if (cardButtons != null)
        {
            for (int i = 0; i < cardButtons.Length; i++)
            {
                int idx = i;
                cardButtons[i].onClick.AddListener(() => OnCardSelected(idx));
            }
        }

        popupContainer.SetActive(false);
    }

    public void ShowRewardSelection(Action onComplete)
    {
        onCompletedCallback = onComplete;
        popupContainer.SetActive(true);

        if (titleText != null)
        {
            titleText.text = "CHÚC PHÚC THÁNH VẬT";
        }

        RollCards();

        // Animation mở popup nảy nhẹ
        if (cardContainer != null)
        {
            cardContainer.localScale = Vector3.one * 0.7f;
            cardContainer.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }
    }

    private void RollCards()
    {
        Array buffValues = Enum.GetValues(typeof(BuffType));
        for (int i = 0; i < 3; i++)
        {
            currentOfferedBuffs[i] = (BuffType)buffValues.GetValue(UnityEngine.Random.Range(0, buffValues.Length));
            if (cardTexts != null && i < cardTexts.Length && cardTexts[i] != null)
            {
                cardTexts[i].text = GetBuffDescription(currentOfferedBuffs[i]);
            }
        }
    }

    private string GetBuffDescription(BuffType buff)
    {
        return buff switch
        {
            BuffType.AttackBoost   => "[CƯỜNG HÓA]\nTăng +10% Sức Tấn Công (Toàn bộ Level)",
            BuffType.DefenseBoost  => "[KIM THẠCH]\nTăng +20% Phòng Thủ (Toàn bộ Level)",
            BuffType.HealthRestore => "[TÁI TẠO]\nHồi phục 20% HP cho Pháp Sư",
            BuffType.EnergyGain    => "[LÕI MA PHÁP]\nNhận ngay +20 Lõi Năng Lượng",
            _ => "Buff Thánh Vật"
        };
    }

    private void OnCardSelected(int index)
    {
        if (index < 0 || index >= currentOfferedBuffs.Length) return;

        ApplyBuff(currentOfferedBuffs[index]);

        // Đóng popup
        if (cardContainer != null)
        {
            cardContainer.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack).OnComplete(() =>
            {
                popupContainer.SetActive(false);
                onCompletedCallback?.Invoke();
            });
        }
        else
        {
            popupContainer.SetActive(false);
            onCompletedCallback?.Invoke();
        }
    }

    private void ApplyBuff(BuffType buff)
    {
        if (Player.Instance == null) return;

        switch (buff)
        {
            case BuffType.AttackBoost:
                // Tăng Atk
                if (Player.Instance.data != null) Player.Instance.data.Atk *= 1.1f;
                break;
            case BuffType.DefenseBoost:
                if (Player.Instance.data != null) Player.Instance.data.Dev += 5f;
                break;
            case BuffType.HealthRestore:
                Player.Instance.Health(Player.Instance.MaxHealth * 0.2f);
                break;
            case BuffType.EnergyGain:
                Player.Instance.AddEnergy(20);
                break;
        }
    }

    private void OnRerollClicked()
    {
        if (Player.Instance != null && Player.Instance.TrySpendEnergy(5))
        {
            RollCards();
        }
    }
}

