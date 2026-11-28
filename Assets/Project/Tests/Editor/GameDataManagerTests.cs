using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

[TestFixture]
public class GameDataManagerTests
{
    [Test]
    public void BuildSettings_And_Scenes_AreConfiguredCleanly()
    {
        var scenes = EditorBuildSettings.scenes;
        Assert.GreaterOrEqual(scenes.Length, 3, "Build Settings phải có đủ 3 scene: PrepareScene, HomeScene, MainScene.");
        Assert.AreEqual("Assets/Project/Scene/PrepareScene.unity", scenes[0].path);
        Assert.IsTrue(scenes[0].enabled);
        Assert.AreEqual("Assets/Project/Scene/HomeScene.unity", scenes[1].path);
        Assert.IsTrue(scenes[1].enabled);
        Assert.AreEqual("Assets/Project/Scene/MainScene.unity", scenes[2].path);
        Assert.IsTrue(scenes[2].enabled);

        Scene previousActive = SceneManager.GetActiveScene();
        Scene prepScene = default;
        Scene homeScene = default;
        try
        {
            prepScene = EditorSceneManager.OpenScene("Assets/Project/Scene/PrepareScene.unity", OpenSceneMode.Additive);
            GameDataManager gdm = null;
            LoadAllLevelDataFromResource loader = null;
            PrepareSceneController prepCtrl = null;
            foreach (GameObject root in prepScene.GetRootGameObjects())
            {
                if (gdm == null) gdm = root.GetComponentInChildren<GameDataManager>(true);
                if (loader == null) loader = root.GetComponentInChildren<LoadAllLevelDataFromResource>(true);
                if (prepCtrl == null) prepCtrl = root.GetComponentInChildren<PrepareSceneController>(true);
            }

            Assert.IsNotNull(gdm, "PrepareScene phải có GameDataManager.");
            Assert.IsNotNull(loader, "PrepareScene phải có LoadAllLevelDataFromResource.");
            Assert.IsNotNull(prepCtrl, "PrepareScene phải có PrepareSceneController.");

            var soPrep = new SerializedObject(prepCtrl);
            Assert.IsNotNull(soPrep.FindProperty("gameDataManager").objectReferenceValue);
            Assert.IsNotNull(soPrep.FindProperty("levelLoader").objectReferenceValue);
            Assert.IsNotNull(soPrep.FindProperty("progressFillImage").objectReferenceValue);
            Assert.IsNotNull(soPrep.FindProperty("progressPercentText").objectReferenceValue);
            Assert.IsNotNull(soPrep.FindProperty("statusLabelText").objectReferenceValue);

            homeScene = EditorSceneManager.OpenScene("Assets/Project/Scene/HomeScene.unity", OpenSceneMode.Additive);
            SceneTransitionButton navButton = null;
            foreach (GameObject root in homeScene.GetRootGameObjects())
            {
                foreach (var btn in root.GetComponentsInChildren<SceneTransitionButton>(true))
                {
                    if (btn.name == "XuatPhat")
                    {
                        navButton = btn;
                        break;
                    }
                }
                if (navButton != null) break;
            }
            Assert.IsNotNull(navButton, "HomeScene nút XuatPhat phải gắn SceneTransitionButton.");
        }
        finally
        {
            if (previousActive.IsValid() && previousActive.isLoaded)
            {
                SceneManager.SetActiveScene(previousActive);
            }
            if (prepScene.IsValid() && prepScene.isLoaded)
            {
                EditorSceneManager.CloseScene(prepScene, true);
            }
            if (homeScene.IsValid() && homeScene.isLoaded)
            {
                EditorSceneManager.CloseScene(homeScene, true);
            }
        }
    }

    [Test]
    public void GameDataManagerPrefab_HasAllStaticAssetsWired_NoEditorDependencies()
    {
        GameObject prefab = Resources.Load<GameObject>(GameDataManager.ManagerPrefabResourcePath);
        Assert.IsNotNull(prefab, "GameDataManager.prefab phải nằm trong Resources/Prefab/Manager/");

        GameDataManager gdm = prefab.GetComponent<GameDataManager>();
        Assert.IsNotNull(gdm);
        Assert.IsNotNull(gdm.BuffDebuffDatabase, "BuffDebuffDatabase chưa được gắn trên prefab.");
        Assert.IsNotNull(gdm.DialogueDatabase, "DialogueDatabaseSO chưa được gắn trên prefab.");
        Assert.IsNotNull(gdm.EnemyDatabase, "EnemyDatabaseSO chưa được gắn trên prefab.");
        Assert.IsNotNull(gdm.DefaultPlayerData, "PlayerData_Default chưa được gắn trên prefab.");
        Assert.IsNotNull(gdm.BannerFont, "Nunito_Black_SDF bannerFont chưa được gắn trên prefab.");
        Assert.IsNotNull(gdm.SkillContainerPrefab, "SkillContainer prefab chưa được gắn.");
        Assert.IsNotNull(gdm.EnemyProjectilePrefab, "EnemyProjectile prefab chưa được gắn.");
        Assert.IsNotNull(gdm.DefaultEnemyPrefab, "DefaultEnemy prefab chưa được gắn.");
        Assert.IsNotNull(gdm.EnemyAttackVfxArt, "EnemyAttackVfxArt chưa được gắn.");
        Assert.IsNotNull(gdm.EnemyStatusAtlas, "EnemyStatusAtlas chưa được gắn.");
        Assert.IsNotNull(gdm.EnemyStatusBurnShader, "EnemyStatusBurn shader chưa được gắn.");

        var so = new SerializedObject(gdm);
        Assert.AreEqual(8, so.FindProperty("stonePrefabs").arraySize);
        Assert.AreEqual(6, so.FindProperty("enemySkillVfxPrefabs").arraySize);
        Assert.AreEqual(10, so.FindProperty("matchStoneVfxPrefabs").arraySize);
    }

    [Test]
    public void LoadAllLevelDataFromResource_LoadsAndSortsAllLevels()
    {
        LevelDataSO[] levels = LoadAllLevelDataFromResource.LoadAllLevelsSync();
        Assert.IsNotNull(levels);
        Assert.GreaterOrEqual(levels.Length, 2, "Phải nạp được tối thiểu 2 level (lv_001, lv_002).");
        Assert.AreEqual(1, levels[0].GetData().GetLevel());
        Assert.AreEqual("LevelDataSO_lv_002", levels[1].name);
    }

    [UnityTest]
    public IEnumerator PrepareScene_PreloadsDataIntoDontDestroyOnLoad_AndTransitionsToHomeAndMain()
    {
        yield return new EnterPlayMode();

        // Đảm bảo test khởi chạy sạch từ PrepareScene (xóa instance auto-bootstrap nếu EnterPlayMode vừa mở từ scene khác).
        if (GameDataManager.HasInstance)
        {
            Object.Destroy(GameDataManager.instance.gameObject);
            GameDataManager.instance = null;
            yield return null;
        }

        SceneManager.LoadScene("PrepareScene");

        float timeout = Time.unscaledTime + 10f;
        while (SceneManager.GetActiveScene().name != "HomeScene" && Time.unscaledTime < timeout)
        {
            yield return null;
        }

        Assert.AreEqual("HomeScene", SceneManager.GetActiveScene().name, "PrepareScene phải tự chuyển sang HomeScene sau khi load xong.");
        Assert.IsTrue(GameDataManager.HasInstance, "GameDataManager phải tồn tại qua DontDestroyOnLoad ở HomeScene.");

        GameDataManager gdm = GameDataManager.Instance;
        Assert.IsTrue(gdm.IsPreloaded);
        Assert.IsTrue(gdm.HasLevelData);
        Assert.GreaterOrEqual(gdm.LevelDataArray.Count, 2, "GameDataManager phải giữ mảng LevelDataSO do LoadAllLevelDataFromResource truyền sang.");
        Assert.AreSame(gdm.BuffDebuffDatabase, BuffDebuffDatabase.Instance);
        Assert.AreSame(gdm.DialogueDatabase, DialogueDatabaseSO.Instance);
        Assert.AreSame(gdm.EnemyDatabase, EnemyDatabaseSO.Instance);
        Assert.IsNotNull(EnemyDataSO.Get(EnemyType.BossDatNung));
        Assert.IsNotNull(gdm.BannerFont);
        Assert.IsTrue(gdm.TryGetMatchStoneVfxPrefab(ElementType.Fire, false, out _));
        Assert.IsTrue(gdm.TryGetMatchStoneVfxPrefab(ElementType.Fire, true, out _));
        Assert.IsTrue(gdm.TryGetEnemySkillVfxPrefab(ElementType.Earth, out _));

        SceneTransitionButton battleBtn = Object.FindFirstObjectByType<SceneTransitionButton>(FindObjectsInactive.Include);
        Assert.IsNotNull(battleBtn, "HomeScene phải tìm thấy SceneTransitionButton trên nút XuatPhat.");
        battleBtn.HandleClick();

        timeout = Time.unscaledTime + 10f;
        while (SceneManager.GetActiveScene().name != "MainScene" && Time.unscaledTime < timeout)
        {
            yield return null;
        }

        Assert.AreEqual("MainScene", SceneManager.GetActiveScene().name, "Bấm XuatPhat phải chuyển sang MainScene.");
        Assert.AreEqual(1, Object.FindObjectsByType<GameDataManager>(FindObjectsSortMode.None).Length, "Chỉ được có 1 instance GameDataManager.");
        Assert.IsNotNull(LevelManager.instance);
        Assert.IsNotNull(LevelManager.instance.levelData);
        Assert.AreEqual(1, LevelManager.instance.CurrentLevelNumber);
        Assert.IsNotNull(EnemyController.Instance);
        Assert.IsNotNull(BoardController.instance);
        Assert.IsNotNull(Player.Instance);
        Assert.IsNotNull(Player.Instance.PlayerData);
        Assert.IsNotNull(RoundNotificationBannerUI.Instance);

        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator DirectMainScene_AutoBootstrapsGameDataManager_WithoutPrepareScene()
    {
        yield return new EnterPlayMode();

        SceneManager.LoadScene("MainScene");
        float timeout = Time.unscaledTime + 10f;
        while (SceneManager.GetActiveScene().name != "MainScene" && Time.unscaledTime < timeout)
        {
            yield return null;
        }

        Assert.AreEqual("MainScene", SceneManager.GetActiveScene().name);
        Assert.IsTrue(GameDataManager.HasInstance, "Vào thẳng MainScene vẫn phải tự bootstrap GameDataManager.");
        Assert.IsTrue(GameDataManager.Instance.IsPreloaded);
        Assert.IsTrue(GameDataManager.Instance.HasLevelData);
        Assert.IsNotNull(GameDataManager.Instance.BannerFont);
        Assert.IsNotNull(LevelManager.instance);
        Assert.IsNotNull(EnemyController.Instance);
        Assert.IsNotNull(BoardController.instance);

        yield return new ExitPlayMode();
    }
}
