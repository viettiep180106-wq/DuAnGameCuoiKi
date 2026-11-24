using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.TextCore.LowLevel;

public static class NunitoFontAssetGenerator
{
    private const string SourceFontPath = "Assets/Project/Fonts/Nunito-Black.ttf";
    private const string TargetAssetPath = "Assets/Project/Fonts/Nunito_Black_SDF.asset";

    [MenuItem("Tools/Typography/Generate Nunito-Black SDF Font Asset")]
    public static void GenerateNunitoSDF()
    {
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
        {
            Debug.LogError($"[NunitoFontAssetGenerator] Không tìm thấy font TTF tại {SourceFontPath}");
            return;
        }

        // Tạo TMP Font Asset dạng Dynamic SDF cao cấp: Tự động render mọi ký tự tiếng Việt runtime sắc nét
        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            90, // Sampling point size
            9,  // Padding
            GlyphRenderMode.SDFAA,
            1024,
            1024,
            AtlasPopulationMode.Dynamic,
            true
        );

        if (fontAsset == null)
        {
            Debug.LogError("[NunitoFontAssetGenerator] Tạo Font Asset thất bại!");
            return;
        }

        fontAsset.name = "Nunito_Black_SDF";

        // Tinh chỉnh Material mặc định của Nunito-Black: Nét dày tự nhiên, viền sắc nét, không vỡ
        Material mat = fontAsset.material;
        if (mat != null)
        {
            mat.name = "Nunito_Black_SDF Material";
            mat.SetFloat("_FaceDilate", 0.0f);        // Font đã dày tự nhiên, không cần nở giả tạo
            mat.SetFloat("_OutlineWidth", 0.16f);      // Viền đen thanh thoát, rõ ràng
            mat.SetFloat("_OutlineSoftness", 0.08f);   // Khử răng cưa mịn màng
            mat.SetColor("_OutlineColor", Color.black);
            mat.SetFloat("_UnderlayDilate", 0.08f);
            mat.SetFloat("_UnderlayOffsetX", 0.2f);
            mat.SetFloat("_UnderlayOffsetY", -0.3f);
            mat.SetFloat("_UnderlaySoftness", 0.2f);
            mat.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.5f));
            mat.EnableKeyword("OUTLINE_ON");
        }

        // Lưu Asset vào project
        AssetDatabase.CreateAsset(fontAsset, TargetAssetPath);
        if (fontAsset.material != null)
        {
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }
        if (fontAsset.atlasTexture != null)
        {
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=#00ff88>✦ [NunitoFontAssetGenerator] Đã tạo thành công {TargetAssetPath}!</color>");
    }

    [MenuItem("Tools/Typography/Apply Nunito-Black to PlayerStatsBar & Banner")]
    public static void ApplyNunitoToKeyUI()
    {
        TMP_FontAsset nunitoFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TargetAssetPath);
        if (nunitoFont == null)
        {
            Debug.LogError($"[NunitoFontAssetGenerator] Chưa tạo {TargetAssetPath}. Hãy chạy 'Generate Nunito-Black SDF Font Asset' trước!");
            return;
        }

        var playerStats = Object.FindFirstObjectByType<PlayerStatsUI>();
        if (playerStats != null)
        {
            Undo.RecordObject(playerStats, "Apply Nunito Font");
            var texts = playerStats.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in texts)
            {
                Undo.RecordObject(t, "Change Font");
                t.font = nunitoFont;
                t.fontSharedMaterial = nunitoFont.material;
                t.fontSize = Mathf.Max(t.fontSize, 26f);
                EditorUtility.SetDirty(t);
            }
        }

        // Tối ưu khung chữ HP không bị rớt dòng
        var slotHp = GameObject.Find("Slot_HP");
        if (slotHp != null)
        {
            var txt = slotHp.GetComponentInChildren<TMP_Text>();
            if (txt != null)
            {
                Undo.RecordObject(txt.rectTransform, "Widen HP Text");
                txt.rectTransform.sizeDelta = new Vector2(180f, 40f);
                txt.enableWordWrapping = false;
                EditorUtility.SetDirty(txt);
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("<color=#00ff88>✦ Đã áp dụng Nunito-Black lên PlayerStatsBar thành công!</color>");
    }
}
