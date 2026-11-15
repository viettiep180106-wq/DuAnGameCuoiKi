using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class ButtonAssetSetup
{
    [MenuItem("Tools/Apply Button Assets To Main Scene")]
    public static void ApplyButtonAssets()
    {
        // 1. Configure 9-slice borders for button sprites
        ConfigureSpriteBorders();

        // 2. Load sprites
        Sprite btn1_Silver = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Buttons/btn_1.png");
        Sprite btn2_Gold = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Buttons/btn_2.png");
        Sprite btn3_Red = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Buttons/btn_3.png");
        Sprite btn4_Green = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Buttons/btn_4.png");
        Sprite btn5_Close = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Buttons/btn_5.png");

        if (btn1_Silver == null || btn2_Gold == null || btn3_Red == null || btn4_Green == null || btn5_Close == null)
        {
            Debug.LogError("[ButtonAssetSetup] Could not load all button sprites from Assets/Project/Sprite/Buttons/!");
            return;
        }

        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("[ButtonAssetSetup] Active scene is invalid!");
            return;
        }

        void StyleButton(Button btn, Sprite sprite, Color? textColor = null, bool isCloseSquare = false, Vector2? size = null)
        {
            if (btn == null) return;

            RectTransform rt = btn.GetComponent<RectTransform>();
            if (size.HasValue && rt != null)
            {
                rt.sizeDelta = size.Value;
            }

            Image img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = sprite;
                if (isCloseSquare)
                {
                    img.type = Image.Type.Simple;
                    img.preserveAspect = true;
                }
                else
                {
                    img.type = Image.Type.Sliced;
                }
                img.color = Color.white;
                EditorUtility.SetDirty(img);
            }

            TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
            {
                if (textColor.HasValue) tmp.color = textColor.Value;
                tmp.fontStyle = FontStyles.Bold;
                EditorUtility.SetDirty(tmp);
            }

            EditorUtility.SetDirty(btn);
        }

        // Find ALL buttons in the scene, including inactive GameObjects
        Button[] allButtons = Resources.FindObjectsOfTypeAll<Button>();
        int count = 0;

        foreach (Button btn in allButtons)
        {
            // Only affect objects belonging to the current active scene (not prefabs or internal assets)
            if (btn.gameObject.scene != scene) continue;

            string name = btn.gameObject.name;

            if (name == "StartStepBtn")
            {
                StyleButton(btn, btn2_Gold, new Color(0.25f, 0.15f, 0.05f), size: new Vector2(340f, 70f));
                TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null) tmp.fontSize = 26f;
                count++;
            }
            else if (name == "ExitBoardBtn")
            {
                StyleButton(btn, btn1_Silver, Color.white, size: new Vector2(140f, 48f));
                count++;
            }
            else if (name == "SettingsBtn")
            {
                StyleButton(btn, btn1_Silver, Color.white, size: new Vector2(90f, 44f));
                count++;
            }
            else if (name == "ExitBtn")
            {
                StyleButton(btn, btn1_Silver, Color.white, size: new Vector2(90f, 44f));
                count++;
            }
            else if (name == "Btn_Open25")
            {
                StyleButton(btn, btn4_Green, Color.white, size: new Vector2(200f, 65f));
                count++;
            }
            else if (name == "Btn_Skip")
            {
                StyleButton(btn, btn1_Silver, Color.white, size: new Vector2(160f, 55f));
                count++;
            }
            else if (name == "Btn_StartGacha")
            {
                StyleButton(btn, btn2_Gold, new Color(0.25f, 0.15f, 0.05f), size: new Vector2(200f, 65f));
                count++;
            }
            else if (name == "Btn_Roll5")
            {
                StyleButton(btn, btn2_Gold, new Color(0.25f, 0.15f, 0.05f), size: new Vector2(180f, 60f));
                count++;
            }
            else if (name == "Btn_CloseX")
            {
                StyleButton(btn, btn5_Close, Color.white, isCloseSquare: true, size: new Vector2(42f, 46f));
                count++;
            }
            else if (name == "CloseBtn")
            {
                StyleButton(btn, btn5_Close, Color.white, isCloseSquare: true, size: new Vector2(42f, 46f));
                count++;
            }
            else if (name == "Btn_Confirm" || name == "SurrenderBtn")
            {
                StyleButton(btn, btn3_Red, Color.white, size: new Vector2(160f, 55f));
                count++;
            }
            else if (name == "Btn_Cancel")
            {
                StyleButton(btn, btn1_Silver, Color.white, size: new Vector2(160f, 55f));
                count++;
            }
            else if (name == "ResumeBtn")
            {
                StyleButton(btn, btn4_Green, Color.white, size: new Vector2(180f, 60f));
                count++;
            }
            else if (name == "SkipDialogueBtn")
            {
                StyleButton(btn, btn1_Silver, new Color(1f, 0.88f, 0.35f, 1f), size: new Vector2(110f, 42f));
                count++;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[ButtonAssetSetup] Successfully styled {count} buttons in MainScene with new custom button sprites!");
    }

    private static void ConfigureSpriteBorders()
    {
        void SetBorder(string path, Vector4 border)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.spriteBorder = border; // Left, Bottom, Right, Top
                importer.SaveAndReimport();
            }
        }

        // Tinh chỉnh viền 9-slice phù hợp kích thước ảnh gốc và chống méo khi co lại
        Vector4 standardBorder = new Vector4(20f, 18f, 20f, 18f);
        SetBorder("Assets/Project/Sprite/Buttons/btn_1.png", standardBorder);
        SetBorder("Assets/Project/Sprite/Buttons/btn_2.png", standardBorder);
        SetBorder("Assets/Project/Sprite/Buttons/btn_3.png", standardBorder);
        SetBorder("Assets/Project/Sprite/Buttons/btn_4.png", standardBorder);
        SetBorder("Assets/Project/Sprite/Buttons/btn_5.png", new Vector4(14f, 14f, 14f, 14f));

        AssetDatabase.Refresh();
    }
}
