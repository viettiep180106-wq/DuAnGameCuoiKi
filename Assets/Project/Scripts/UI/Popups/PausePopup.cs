using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class PausePopup : MonoBehaviour
{
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private RectTransform contentBox;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button surrenderButton;

    private void Awake()
    {
        if (popupPanel == null) popupPanel = gameObject;

        if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
        if (surrenderButton != null) surrenderButton.onClick.AddListener(SurrenderGame);

        popupPanel.SetActive(false);
    }

    public void OpenPauseMenu()
    {
        popupPanel.SetActive(true);
        Time.timeScale = 0f;

        if (contentBox != null)
        {
            contentBox.localScale = Vector3.one * 0.7f;
            contentBox.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        if (contentBox != null)
        {
            contentBox.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() =>
            {
                popupPanel.SetActive(false);
            });
        }
        else
        {
            popupPanel.SetActive(false);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void SurrenderGame()
    {
        Time.timeScale = 1f;
        if (GameManager.instance != null)
        {
            GameManager.instance.OnLoseGame?.Invoke(true);
        }
        popupPanel.SetActive(false);
    }
}

