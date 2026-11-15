using System;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class WaveIntroBannerUI : MonoBehaviour
{
    public static WaveIntroBannerUI Instance { get; private set; }

    [SerializeField] private RectTransform bannerRect;
    [SerializeField] private CanvasGroup bannerCanvasGroup;
    [SerializeField] private TMP_Text waveTitleText;
    [SerializeField] private TMP_Text waveDetailText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (bannerRect == null) bannerRect = GetComponent<RectTransform>();
        if (bannerCanvasGroup == null) bannerCanvasGroup = GetComponent<CanvasGroup>();

        if (bannerCanvasGroup != null)
        {
            bannerCanvasGroup.alpha = 0f;
        }
    }

    /// <summary>
    /// Hiển thị Banner giới thiệu độ khó và thông tin Wave (GDD Item B14 & B19)
    /// </summary>
    public void ShowBanner(int wave, int totalWaves, string difficulty, string elementStr, Action onComplete = null)
    {
        if (bannerRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (waveTitleText != null)
        {
            string diffStr = !string.IsNullOrEmpty(difficulty) ? $" – ĐỘ KHÓ: {difficulty.ToUpper()}" : "";
            waveTitleText.text = $"WAVE {wave}{diffStr}";
        }

        if (waveDetailText != null)
        {
            if (!string.IsNullOrEmpty(elementStr))
            {
                waveDetailText.text = $"Hệ Quái: {elementStr}";
                waveDetailText.gameObject.SetActive(true);
            }
            else
            {
                waveDetailText.gameObject.SetActive(false);
            }
        }

        bannerRect.DOKill();
        if (bannerCanvasGroup != null) bannerCanvasGroup.DOKill();

        // Slide-in từ cạnh trái vào giữa
        bannerRect.anchoredPosition = new Vector2(-Screen.width, bannerRect.anchoredPosition.y);
        if (bannerCanvasGroup != null) bannerCanvasGroup.alpha = 1f;

        bannerRect.DOAnchorPosX(0f, 0.45f).SetEase(Ease.OutCubic).OnComplete(() =>
        {
            // Dừng lại 1.2s rồi Fade-out biến mất
            DOVirtual.DelayedCall(1.2f, () =>
            {
                if (bannerCanvasGroup != null)
                {
                    bannerCanvasGroup.DOFade(0f, 0.4f).OnComplete(() =>
                    {
                        onComplete?.Invoke();
                    });
                }
                else
                {
                    onComplete?.Invoke();
                }
            });
        });
    }

    /// <summary>
    /// Hiển thị Banner "BẮT ĐẦU CHIẾN ĐẤU!" sau khi lời thoại narrative kết thúc (GDD Item B16 & B20)
    /// </summary>
    public void ShowBattleStartBanner(Action onComplete = null)
    {
        if (bannerRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (waveTitleText != null)
        {
            waveTitleText.text = "⚔️ BẮT ĐẦU CHIẾN ĐẤU! ⚔️";
        }

        if (waveDetailText != null)
        {
            waveDetailText.gameObject.SetActive(false);
        }

        bannerRect.DOKill();
        if (bannerCanvasGroup != null) bannerCanvasGroup.DOKill();

        bannerRect.anchoredPosition = new Vector2(0f, bannerRect.anchoredPosition.y);
        bannerRect.localScale = Vector3.zero;
        if (bannerCanvasGroup != null) bannerCanvasGroup.alpha = 1f;

        // Hiệu ứng Zoom-in nổ nhẹ (Ease.OutBack)
        bannerRect.DOScale(Vector3.one * 1.12f, 0.35f).SetEase(Ease.OutBack).OnComplete(() =>
        {
            bannerRect.DOScale(Vector3.one, 0.15f);
            DOVirtual.DelayedCall(0.45f, () =>
            {
                if (bannerCanvasGroup != null)
                {
                    bannerCanvasGroup.DOFade(0f, 0.25f).OnComplete(() =>
                    {
                        bannerRect.localScale = Vector3.one;
                        onComplete?.Invoke();
                    });
                }
                else
                {
                    bannerRect.localScale = Vector3.one;
                    onComplete?.Invoke();
                }
            });
        });
    }
}

