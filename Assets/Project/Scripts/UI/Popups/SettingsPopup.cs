using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class SettingsPopup : MonoBehaviour
{
    [SerializeField] private GameObject popupContainer;
    [SerializeField] private RectTransform contentBox;

    [Header("Controls")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button homeButton;

    private void Awake()
    {
        if (popupContainer == null) popupContainer = gameObject;

        if (closeButton != null) closeButton.onClick.AddListener(ClosePopup);
        if (homeButton != null) homeButton.onClick.AddListener(ReturnToHome);

        if (bgmSlider != null)
        {
            bgmSlider.value = PlayerPrefs.GetFloat("BGM_Volume", 0.8f);
            bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = PlayerPrefs.GetFloat("SFX_Volume", 1.0f);
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        }

        popupContainer.SetActive(false);
    }

    public void OpenPopup()
    {
        popupContainer.SetActive(true);
        Time.timeScale = 0f;

        if (contentBox != null)
        {
            contentBox.localScale = Vector3.one * 0.7f;
            contentBox.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }

    public void ClosePopup()
    {
        Time.timeScale = 1f;

        if (contentBox != null)
        {
            contentBox.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() =>
            {
                popupContainer.SetActive(false);
            });
        }
        else
        {
            popupContainer.SetActive(false);
        }
    }

    private void OnBgmChanged(float value)
    {
        PlayerPrefs.SetFloat("BGM_Volume", value);
    }

    private void OnSfxChanged(float value)
    {
        PlayerPrefs.SetFloat("SFX_Volume", value);
    }

    private void ReturnToHome()
    {
        Time.timeScale = 1f;
        // Chuyển về HomeScene nếu có trong BuildSettings, hoặc tải lại scene 0
        if (Application.CanStreamedLevelBeLoaded("HomeScene"))
        {
            SceneManager.LoadScene("HomeScene");
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }
}

