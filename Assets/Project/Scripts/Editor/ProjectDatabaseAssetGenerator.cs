using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ProjectDatabaseAssetGenerator
{
    [MenuItem("Tools/Generate All ScriptableObject Databases")]
    public static void GenerateAll()
    {
        GenerateEnemyDatabase();
        GenerateDialogueDatabase();
        GenerateBoardConfig();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=#00ff88><b>[DATABASE GENERATOR]</b> Successfully generated all ScriptableObject assets!</color>");
    }

    [MenuItem("Tools/Database/Generate Enemy Database Asset")]
    public static void GenerateEnemyDatabase()
    {
        EnsureFolderExists("Assets/Project/Resources");
        EnsureFolderExists("Assets/Project/Resources/Database");

        string assetPath = "Assets/Project/Resources/Database/EnemyDatabase.asset";
        EnemyDatabaseSO db = AssetDatabase.LoadAssetAtPath<EnemyDatabaseSO>(assetPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<EnemyDatabaseSO>();
            AssetDatabase.CreateAsset(db, assetPath);
        }

        List<EnemyConfigEntry> entries = new List<EnemyConfigEntry>();
        foreach (var kvp in EnemyStatsDatabase.HardcodedConfigs)
        {
            entries.Add(EnemyConfigEntry.FromEnemyConfig(kvp.Key, kvp.Value));
        }

        db.SetEnemiesForBuild(entries);
        EditorUtility.SetDirty(db);
        Debug.Log($"<color=#00e5ff>[EnemyDatabaseSO]</color> Exported {entries.Count} enemies into {assetPath}");
    }

    [MenuItem("Tools/Database/Generate Dialogue Database Asset")]
    public static void GenerateDialogueDatabase()
    {
        EnsureFolderExists("Assets/Project/Resources");
        EnsureFolderExists("Assets/Project/Resources/Database");

        string assetPath = "Assets/Project/Resources/Database/DialogueDatabase.asset";
        DialogueDatabaseSO db = AssetDatabase.LoadAssetAtPath<DialogueDatabaseSO>(assetPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<DialogueDatabaseSO>();
            AssetDatabase.CreateAsset(db, assetPath);
        }

        List<DialogueEntry> entries = new List<DialogueEntry>();
        foreach (var kvp in DialogueDatabase.HardcodedEntries)
        {
            entries.Add(kvp.Value);
        }

        db.SetEntriesForBuild(entries);
        EditorUtility.SetDirty(db);
        Debug.Log($"<color=#00e5ff>[DialogueDatabaseSO]</color> Exported {entries.Count} dialogue lines into {assetPath}");
    }

    [MenuItem("Tools/Database/Generate Board Config Asset")]
    public static void GenerateBoardConfig()
    {
        EnsureFolderExists("Assets/Project/Resources");
        EnsureFolderExists("Assets/Project/Resources/ScriptObject");

        string assetPath = "Assets/Project/Resources/ScriptObject/BoardConfig_Default.asset";
        BoardConfigSO cfg = AssetDatabase.LoadAssetAtPath<BoardConfigSO>(assetPath);
        if (cfg == null)
        {
            cfg = ScriptableObject.CreateInstance<BoardConfigSO>();
            AssetDatabase.CreateAsset(cfg, assetPath);
        }

        EditorUtility.SetDirty(cfg);
        Debug.Log($"<color=#00e5ff>[BoardConfigSO]</color> Generated default config into {assetPath}");
    }

    private static void EnsureFolderExists(string fullPath)
    {
        if (AssetDatabase.IsValidFolder(fullPath)) return;

        int lastSlash = fullPath.LastIndexOf('/');
        if (lastSlash > 0)
        {
            string parent = fullPath.Substring(0, lastSlash);
            string folderName = fullPath.Substring(lastSlash + 1);
            EnsureFolderExists(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
