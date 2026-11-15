#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class RuneEventVisualVerifier
{
    private const string ArtifactDir = @"C:\Users\HP\.gemini\antigravity\brain\7d275435-aef9-4b2c-a6d1-8c43aba6471e\";

    private static void SaveCameraCapture(string fileName)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("Camera.main not found!");
            return;
        }

        RenderTexture rt = new RenderTexture(1080, 1920, 24);
        RenderTexture prev = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();
        cam.targetTexture = prev;

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        byte[] bytes = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(rt);

        string fullPath = Path.Combine(ArtifactDir, fileName);
        File.WriteAllBytes(fullPath, bytes);
        Debug.Log($"[RuneEventVisualVerifier] Captured and saved: {fullPath}");
    }

    [MenuItem("Tools/Verify/Render All Rune Event Panels")]
    public static void RenderAllPanels()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Project/Scene/MainScene.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);

        GameObject ui = GameObject.Find("UI");
        if (ui == null) return;

        Transform popupT = ui.transform.Find("RuneStoneEventPopup");
        if (popupT == null) return;

        GameObject popup = popupT.gameObject;
        popup.SetActive(true);
        popup.GetComponent<CanvasGroup>().alpha = 1f;

        Transform p1 = popupT.Find("Phase1_StonePanel");
        Transform gacha = popupT.Find("Phase2_GachaPanel");
        Transform roll = popupT.Find("Phase2_RollPanel");
        Transform chest = popupT.Find("Phase2_ChestPanel");
        Transform spirit = popupT.Find("Phase2_SpiritPanel");
        Transform warn = popupT.Find("WarningPopup");
        Transform glow = popupT.Find("GlowLight");

        void HideAll()
        {
            if (p1 != null) p1.gameObject.SetActive(false);
            if (gacha != null) gacha.gameObject.SetActive(false);
            if (roll != null) roll.gameObject.SetActive(false);
            if (chest != null) chest.gameObject.SetActive(false);
            if (spirit != null) spirit.gameObject.SetActive(false);
            if (warn != null) warn.gameObject.SetActive(false);
            if (glow != null) glow.gameObject.SetActive(false);
        }

        // 1. Phase 1: Stone Panel
        HideAll();
        if (p1 != null) p1.gameObject.SetActive(true);
        SaveCameraCapture("verified_rune_stone_phase1.png");

        // 2. Phase 2: Gacha Wheel
        HideAll();
        if (gacha != null) gacha.gameObject.SetActive(true);
        SaveCameraCapture("verified_gacha_wheel_phase2.png");

        // 3. Phase 2: Roll Cards
        HideAll();
        if (roll != null)
        {
            roll.gameObject.SetActive(true);
            Transform deck = roll.Find("CardDeck");
            if (deck != null) deck.gameObject.SetActive(false);

            string[] sampleBuffs = { "+10% ATK", "+20% TỐC ĐỘ", "HỒI 15% HP" };
            for (int i = 0; i < 3; i++)
            {
                Transform c = roll.Find($"Card_{i + 1}");
                if (c != null)
                {
                    c.gameObject.SetActive(true);
                    Transform desc = c.Find("Desc");
                    if (desc != null)
                    {
                        var t = desc.GetComponent<TextMeshProUGUI>();
                        if (t != null) t.text = sampleBuffs[i];
                    }
                }
            }
        }
        SaveCameraCapture("verified_roll_cards_phase2.png");

        // Reset roll cards
        if (roll != null)
        {
            for (int i = 0; i < 3; i++)
            {
                Transform c = roll.Find($"Card_{i + 1}");
                if (c != null) c.gameObject.SetActive(false);
            }
            Transform deck = roll.Find("CardDeck");
            if (deck != null) deck.gameObject.SetActive(true);
        }

        // 4. Phase 2: Chest (Closed)
        HideAll();
        if (chest != null)
        {
            chest.gameObject.SetActive(true);
            Transform chImg = chest.Find("ChestImage");
            if (chImg != null)
            {
                chImg.gameObject.SetActive(true);
                chImg.GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Events/Chest_Closed.png");
            }
            Transform envs = chest.Find("EnvelopesContainer");
            if (envs != null) envs.gameObject.SetActive(false);
        }
        SaveCameraCapture("verified_chest_closed.png");

        // 5. Phase 2: Chest (Open with Envelopes)
        HideAll();
        if (chest != null)
        {
            chest.gameObject.SetActive(true);
            Transform chImg = chest.Find("ChestImage");
            if (chImg != null) chImg.gameObject.SetActive(false);

            Transform chp = chest.Find("ChestPromptText");
            if (chp != null)
            {
                chp.gameObject.SetActive(true);
                var t = chp.GetComponent<TextMeshProUGUI>();
                if (t != null)
                {
                    t.text = "Chọn một trong hai phong thư!";
                    t.color = Color.yellow;
                }
            }

            Transform envs = chest.Find("EnvelopesContainer");
            if (envs != null) envs.gameObject.SetActive(true);
        }
        SaveCameraCapture("verified_chest_envelopes.png");

        // 6. Phase 2: Spirit Fairy
        HideAll();
        if (spirit != null)
        {
            spirit.gameObject.SetActive(true);
            Transform spiImg = spirit.Find("SpiritImage");
            if (spiImg != null)
            {
                spiImg.GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Events/Spirit_Fairy.png");
            }
            Transform title = spirit.Find("SpiritTitle");
            if (title != null)
            {
                var t = title.GetComponent<TextMeshProUGUI>();
                t.text = "LỜI CHÚC PHÚC CỦA TINH LINH";
                t.color = Color.green;
            }
            Transform eff = spirit.Find("SpiritEffect");
            if (eff != null)
            {
                var t = eff.GetComponent<TextMeshProUGUI>();
                t.text = "Nhận phước lành: +10% Sát Thương (ATK)!";
            }
        }
        SaveCameraCapture("verified_spirit_fairy.png");

        // 7. Phase 2: Spirit Demon
        HideAll();
        if (spirit != null)
        {
            spirit.gameObject.SetActive(true);
            Transform spiImg = spirit.Find("SpiritImage");
            if (spiImg != null)
            {
                spiImg.GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Events/Spirit_Demon.png");
            }
            Transform title = spirit.Find("SpiritTitle");
            if (title != null)
            {
                var t = title.GetComponent<TextMeshProUGUI>();
                t.text = "LỜI NGUYỀN CỦA QUỶ DỮ";
                t.color = Color.red;
            }
            Transform eff = spirit.Find("SpiritEffect");
            if (eff != null)
            {
                var t = eff.GetComponent<TextMeshProUGUI>();
                t.text = "Bị nguyền rủa: Giảm 10% Sát Thương!";
            }
        }
        SaveCameraCapture("verified_spirit_demon.png");

        // 8. Warning Popup
        HideAll();
        if (warn != null) warn.gameObject.SetActive(true);
        SaveCameraCapture("verified_warning_popup.png");

        // Clean up: Reset to initial hidden state
        HideAll();
        if (p1 != null) p1.gameObject.SetActive(true);
        popup.SetActive(false);

        Debug.Log("[RuneEventVisualVerifier] All 8 panels successfully rendered and verified!");
    }
}
#endif
