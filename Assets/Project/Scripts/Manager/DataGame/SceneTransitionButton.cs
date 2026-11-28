using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn trực tiếp lên Button trong scene để chuyển màn thông qua GameDataManager.
/// </summary>
[RequireComponent(typeof(Button))]
public class SceneTransitionButton : MonoBehaviour
{
    public enum TargetScene
    {
        HomeScene,
        MainScene
    }

    [SerializeField] private TargetScene targetScene = TargetScene.MainScene;
    [SerializeField] private int levelNumber = 1;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        if (_button != null)
        {
            _button.onClick.AddListener(HandleClick);
        }
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(HandleClick);
        }
    }

    public void HandleClick()
    {
        var mgr = GameDataManager.Instance;
        if (mgr == null) return;

        if (targetScene == TargetScene.HomeScene)
        {
            mgr.LoadHomeScene();
        }
        else
        {
            mgr.LoadMainScene(levelNumber);
        }
    }
}
