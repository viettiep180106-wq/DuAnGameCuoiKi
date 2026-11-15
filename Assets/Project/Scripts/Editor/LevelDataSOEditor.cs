using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LevelDataSO))]
public class LevelDataSOEditor : Editor
{
    private SerializedProperty levelDataProp;
    private SerializedProperty levelProp;
    private SerializedProperty sizeGridProp;
    private SerializedProperty listStepProp;

    private void OnEnable()
    {
        levelDataProp = serializedObject.FindProperty("levelData");
        if (levelDataProp != null)
        {
            levelProp = levelDataProp.FindPropertyRelative("level");
            sizeGridProp = levelDataProp.FindPropertyRelative("sizeGird");
            listStepProp = levelDataProp.FindPropertyRelative("listStep");
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Header
        EditorGUILayout.Space(8);
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("🎮 MA TRẬN 50 LEVEL - TIẾN TRÌNH GAMEPLAY", titleStyle);
        EditorGUILayout.HelpBox("Cấu trúc chuẩn hóa theo GDD Sheet '50 Level Game'. 10 Steps/Level gồm Wave Chiến Đấu (Combat) & Wave Khám Phá (Discovery). Đội hình 2 Hàng (Hàng trước 2 quái, Hàng sau 3 quái).", MessageType.Info);
        EditorGUILayout.Space(4);

        if (GUILayout.Button("⚡ Nạp Lại Level 1 Chuẩn 100% GDD", GUILayout.Height(28)))
        {
            LevelDataGDDImporter.GenerateLevel1();
            serializedObject.Update();
            EditorUtility.SetDirty(target);
        }

        EditorGUILayout.Space(6);

        if (levelDataProp != null)
        {
            EditorGUILayout.LabelField("Thông Tin Chung Level", EditorStyles.boldLabel);
            if (levelProp != null) EditorGUILayout.PropertyField(levelProp, new GUIContent("Level Number (Cấp Độ)"));
            if (sizeGridProp != null) EditorGUILayout.PropertyField(sizeGridProp, new GUIContent("Size Grid (Kích Thước Bàn 6x6)"));

            EditorGUILayout.Space(8);
            if (listStepProp != null)
            {
                EditorGUILayout.PropertyField(listStepProp, new GUIContent($"Danh Sách Wave ({listStepProp.arraySize} Steps)"), true);
            }
        }
        else
        {
            DrawDefaultInspector();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
