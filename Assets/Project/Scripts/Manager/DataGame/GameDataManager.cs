using UnityEngine;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager instance;
    public int levelCurrent = 0;
    private void Awake()
    {
        instance = this;
    }
}
