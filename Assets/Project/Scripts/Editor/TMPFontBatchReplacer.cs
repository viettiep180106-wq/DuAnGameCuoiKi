using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class TMPFontBatchReplacer
{
    private const string TargetFontGuid = "1e6d6829cf23ac44e9719dca652e20ae"; // Arial_Vietnamese_SDF
    private const string TargetFontPath = "Assets/Project/Fonts/Arial_Vietnamese_SDF.asset";
    private const string OldFontGuid = "8f586378b4e144a9851e7b34d9b748ee"; // LiberationSans SDF

    [MenuItem("Tools/Typography/Scan TMP Fonts in Project")]
    public static void ScanFonts()
    {
        TMP_FontAsset targetFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TargetFontPath);
        Debug.Log($"[TMPFontBatchReplacer] Target Font: {(targetFont != null ? targetFont.name : "NULL")}");

        // Scan active scene
        TMP_Text[] sceneTexts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int oldFontCount = 0;
        int targetFontCount = 0;
        int otherFontCount = 0;

        foreach (var t in sceneTexts)
        {
            if (t.font == null) continue;
            string assetPath = AssetDatabase.GetAssetPath(t.font);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            if (guid == OldFontGuid)
            {
                oldFontCount++;
                Debug.LogWarning($"[Scene Text] '{t.gameObject.name}' (Text: '{t.text}') is using LiberationSans SDF!", t.gameObject);
            }
            else if (guid == TargetFontGuid)
            {
                targetFontCount++;
            }
            else
            {
                otherFontCount++;
            }
        }

        Debug.Log($"[TMP Scan Scene] Total: {sceneTexts.Length} | LiberationSans: {oldFontCount} | Arial_Vietnamese: {targetFontCount} | Other: {otherFontCount}");
    }

    [MenuItem("Tools/Typography/Batch Replace All Fonts To Arial_Vietnamese_SDF")]
    public static void ReplaceAllFonts()
    {
        TMP_FontAsset targetFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TargetFontPath);
        if (targetFont == null)
        {
            Debug.LogError($"[TMPFontBatchReplacer] Không tìm thấy font tại: {TargetFontPath}");
            return;
        }

        int replacedInScene = 0;
        TMP_Text[] sceneTexts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var t in sceneTexts)
        {
            if (t.font == null) continue;
            string assetPath = AssetDatabase.GetAssetPath(t.font);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            if (guid == OldFontGuid)
            {
                Undo.RecordObject(t, "Replace TMP Font");
                t.font = targetFont;
                EditorUtility.SetDirty(t);
                replacedInScene++;
            }
        }

        if (replacedInScene > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[TMPFontBatchReplacer] Đã thay thế thành công {replacedInScene} text trong Scene sang Arial_Vietnamese_SDF!");
        }

        // Replace in Prefabs
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Project" });
        int replacedInPrefabs = 0;

        foreach (string pGuid in prefabGuids)
        {
            string pPath = AssetDatabase.GUIDToAssetPath(pGuid);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(pPath);
            if (prefabRoot == null) continue;

            TMP_Text[] prefabTexts = prefabRoot.GetComponentsInChildren<TMP_Text>(true);
            bool modified = false;

            foreach (var pt in prefabTexts)
            {
                if (pt.font == null) continue;
                string assetPath = AssetDatabase.GetAssetPath(pt.font);
                string guid = AssetDatabase.AssetPathToGUID(assetPath);

                if (guid == OldFontGuid)
                {
                    pt.font = targetFont;
                    modified = true;
                    replacedInPrefabs++;
                }
            }

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, pPath);
                Debug.Log($"[TMPFontBatchReplacer] Đã cập nhật font trong Prefab: {pPath}");
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[TMPFontBatchReplacer] HOÀN TẤT: Đã thay thế {replacedInScene} text trong Scene và {replacedInPrefabs} text trong Prefabs!");
    }
}
