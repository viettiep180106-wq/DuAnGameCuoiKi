using System.Collections;
using System.Collections.Generic;
using Unity.Android.Gradle.Manifest;
using UnityEngine;

public class LoadAllLevelDataFromResource : MonoBehaviour
{
    public static LoadAllLevelDataFromResource instance;
    private LevelDataSO dataSO = null;
    public LevelData GetLevelData()
    {
        LevelData data = dataSO.GetData();
        return data;
    } 
    public void LoadLevel(int lvCurrent)
    {
        StartCoroutine(LoadFromResource(lvCurrent));
    }
    IEnumerator LoadFromResource(int lv)
    {
        string path = $"LevelData/LevelDataSO_lv_{lv}";
        ResourceRequest request = Resources.LoadAsync<LevelDataSO>(path);
        yield return request;
        if(request.asset != null)
        {
            dataSO = request.asset as LevelDataSO;
            Debug.Log("Load Level Ok");
        }
        else
        {
            Debug.Log("Load Level Failed");
        }
    }
}
