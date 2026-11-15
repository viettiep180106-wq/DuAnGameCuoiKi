#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class RuneEventHierarchyBuilder
{
    private const string SpriteDir = "Assets/Project/Sprite/Events/";

    private static Sprite LoadSprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + name + ".png");
    }

    [MenuItem("Tools/Clean Sprite Artifacts")]
    public static void CleanSpriteArtifacts()
    {
        CleanTexture("Btn_CloseX.png", (tex) =>
        {
            int w = tex.width;
            int h = tex.height;
            Color[] pixels = tex.GetPixels();

            // Flood-fill transparency from corners to remove outer grey arcs
            bool[] visited = new bool[w * h];
            System.Collections.Generic.Queue<Vector2Int> q = new System.Collections.Generic.Queue<Vector2Int>();
            
            void TryEnqueue(int x, int y)
            {
                if (x < 0 || x >= w || y < 0 || y >= h) return;
                int idx = y * w + x;
                if (visited[idx]) return;
                Color c = pixels[idx];
                // Stop when hitting the black border / red button
                bool isBorderOrButton = (c.r > 0.45f && c.g < 0.35f) || (c.r < 0.2f && c.g < 0.2f && c.b < 0.2f && c.a > 0.5f);
                if (!isBorderOrButton)
                {
                    visited[idx] = true;
                    q.Enqueue(new Vector2Int(x, y));
                }
            }

            // Start from 4 corners and borders
            for (int x = 0; x < w; x++) { TryEnqueue(x, 0); TryEnqueue(x, h - 1); }
            for (int y = 0; y < h; y++) { TryEnqueue(0, y); TryEnqueue(w - 1, y); }

            while (q.Count > 0)
            {
                Vector2Int p = q.Dequeue();
                pixels[p.y * w + p.x] = Color.clear;
                TryEnqueue(p.x + 1, p.y);
                TryEnqueue(p.x - 1, p.y);
                TryEnqueue(p.x, p.y + 1);
                TryEnqueue(p.x, p.y - 1);
            }

            tex.SetPixels(pixels);
            tex.Apply();
        });

        CleanTexture("Btn_Roll5.png", (tex) =>
        {
            int w = tex.width;
            int h = tex.height;
            Color[] pixels = tex.GetPixels();

            // Clear green artifacts on left border (columns 0 to 6)
            for (int x = 0; x < 7; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    int idx = y * w + x;
                    Color c = pixels[idx];
                    if (c.g > c.r + 0.05f || (c.g > 0.3f && c.r < 0.4f))
                    {
                        pixels[idx] = Color.clear;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
        });

        AssetDatabase.Refresh();
        Debug.Log("[RuneEventHierarchyBuilder] Sprite artifacts cleaned successfully!");
    }

    private static void CleanTexture(string fileName, System.Action<Texture2D> modifier)
    {
        string path = SpriteDir + fileName;
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        bool wasReadable = importer.isReadable;
        TextureImporterCompression origComp = importer.textureCompression;
        importer.isReadable = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex != null)
        {
            modifier(tex);
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
        }

        importer.isReadable = wasReadable;
        importer.textureCompression = origComp;
        importer.SaveAndReimport();
    }

    [MenuItem("Tools/Reimport Rune Event Sprites As Single")]
    public static void ReimportSprites()
    {
        string[] files = Directory.GetFiles(SpriteDir, "*.png");
        foreach (string file in files)
        {
            string unityPath = file.Replace('\\', '/');
            TextureImporter importer = AssetImporter.GetAtPath(unityPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }
        AssetDatabase.Refresh();
        Debug.Log("[RuneEventHierarchyBuilder] All event sprites reimported as Single Sprite!");
    }

    [MenuItem("Tools/Build Rune Event Hierarchy")]
    public static void BuildHierarchy()
    {
        EditorSceneManager.OpenScene("Assets/Project/Scene/MainScene.unity", OpenSceneMode.Single);
        CleanSpriteArtifacts();
        ReimportSprites();

        GameObject ui = GameObject.Find("UI");
        if (ui == null)
        {
            Debug.LogError("UI Canvas not found!");
            return;
        }

        // Remove old if exists
        Transform old = ui.transform.Find("RuneStoneEventPopup");
        if (old != null)
        {
            Object.DestroyImmediate(old.gameObject);
        }

        // 1. Root Popup with Override Sorting Canvas so it covers world space sprites (Player, SkillContainer)
        GameObject root = new GameObject("RuneStoneEventPopup", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(RuneEventUIController));
        root.transform.SetParent(ui.transform, false);
        RectTransform rootRT = root.GetComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        Canvas rootCanvas = root.GetComponent<Canvas>();
        rootCanvas.overrideSorting = true;
        rootCanvas.sortingOrder = 100;
        rootCanvas.worldCamera = Camera.main;

        CanvasGroup cg = root.GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        RuneEventUIController ctrl = root.GetComponent<RuneEventUIController>();

        // 2. Dark Overlay
        GameObject overlay = new GameObject("DarkOverlay", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(root.transform, false);
        RectTransform ovRT = overlay.GetComponent<RectTransform>();
        ovRT.anchorMin = Vector2.zero;
        ovRT.anchorMax = Vector2.one;
        ovRT.offsetMin = Vector2.zero;
        ovRT.offsetMax = Vector2.zero;
        Image ovImg = overlay.GetComponent<Image>();
        ovImg.color = new Color(0f, 0f, 0f, 0.88f);

        // 3. Glow Light (Radial magical light burst)
        GameObject glow = new GameObject("GlowLight", typeof(RectTransform), typeof(Image));
        glow.transform.SetParent(root.transform, false);
        RectTransform glowRT = glow.GetComponent<RectTransform>();
        glowRT.sizeDelta = new Vector2(400f, 400f);
        glowRT.anchoredPosition = new Vector2(0f, 100f);
        Image glowImg = glow.GetComponent<Image>();
        glowImg.sprite = LoadSprite("Fx_GlowLight");
        glowImg.color = new Color(0.4f, 0.9f, 1f, 0.85f);
        glow.SetActive(false);

        // 4. Stone Panel
        GameObject stonePanel = new GameObject("Phase1_StonePanel", typeof(RectTransform));
        stonePanel.transform.SetParent(root.transform, false);
        RectTransform spRT = stonePanel.GetComponent<RectTransform>();
        spRT.sizeDelta = new Vector2(500f, 600f);
        spRT.anchoredPosition = Vector2.zero;

        GameObject stoneImgGo = new GameObject("StoneImage", typeof(RectTransform), typeof(Image));
        stoneImgGo.transform.SetParent(stonePanel.transform, false);
        RectTransform sImgRT = stoneImgGo.GetComponent<RectTransform>();
        sImgRT.sizeDelta = new Vector2(210f, 260f);
        sImgRT.anchoredPosition = new Vector2(0f, 100f);
        Image sImg = stoneImgGo.GetComponent<Image>();
        sImg.sprite = LoadSprite("RuneStone_Intact");
        sImg.preserveAspect = true;

        GameObject noticeGo = new GameObject("NoticeText", typeof(RectTransform), typeof(TextMeshProUGUI));
        noticeGo.transform.SetParent(stonePanel.transform, false);
        RectTransform nRT = noticeGo.GetComponent<RectTransform>();
        nRT.sizeDelta = new Vector2(480f, 80f);
        nRT.anchoredPosition = new Vector2(0f, -70f);
        TextMeshProUGUI nText = noticeGo.GetComponent<TextMeshProUGUI>();
        nText.text = "Phát hiện một phiến đá có cổ ngữ ma pháp!\nSử dụng 25 Lõi Năng Lượng để khai mở?";
        nText.alignment = TextAlignmentOptions.Center;
        nText.fontSize = 19;
        nText.fontStyle = FontStyles.Bold;
        nText.color = Color.white;

        GameObject btnOpenGo = new GameObject("Btn_Open25", typeof(RectTransform), typeof(Image), typeof(Button));
        btnOpenGo.transform.SetParent(stonePanel.transform, false);
        RectTransform boRT = btnOpenGo.GetComponent<RectTransform>();
        boRT.sizeDelta = new Vector2(185f, 80f);
        boRT.anchoredPosition = new Vector2(-100f, -165f);
        Image boImg = btnOpenGo.GetComponent<Image>();
        boImg.sprite = LoadSprite("Btn_Open25");
        boImg.preserveAspect = true;

        GameObject btnSkipGo = new GameObject("Btn_Skip", typeof(RectTransform), typeof(Image), typeof(Button));
        btnSkipGo.transform.SetParent(stonePanel.transform, false);
        RectTransform bsRT = btnSkipGo.GetComponent<RectTransform>();
        bsRT.sizeDelta = new Vector2(165f, 80f);
        bsRT.anchoredPosition = new Vector2(100f, -165f);
        Image bsImg = btnSkipGo.GetComponent<Image>();
        bsImg.sprite = LoadSprite("Btn_Skip");
        bsImg.preserveAspect = true;

        // 5. Gacha Panel
        GameObject gachaPanel = new GameObject("Phase2_GachaPanel", typeof(RectTransform));
        gachaPanel.transform.SetParent(root.transform, false);
        RectTransform gRT = gachaPanel.GetComponent<RectTransform>();
        gRT.sizeDelta = new Vector2(500f, 600f);
        gRT.anchoredPosition = Vector2.zero;

        GameObject wheelGo = new GameObject("GachaWheel", typeof(RectTransform), typeof(Image));
        wheelGo.transform.SetParent(gachaPanel.transform, false);
        RectTransform wRT = wheelGo.GetComponent<RectTransform>();
        wRT.sizeDelta = new Vector2(310f, 315f);
        wRT.anchoredPosition = new Vector2(0f, 100f);
        Image wImg = wheelGo.GetComponent<Image>();
        wImg.sprite = LoadSprite("GachaWheel");
        wImg.preserveAspect = true;

        GameObject btnStartGacha = new GameObject("Btn_StartGacha", typeof(RectTransform), typeof(Image), typeof(Button));
        btnStartGacha.transform.SetParent(gachaPanel.transform, false);
        RectTransform bsgRT = btnStartGacha.GetComponent<RectTransform>();
        bsgRT.sizeDelta = new Vector2(170f, 80f);
        bsgRT.anchoredPosition = new Vector2(0f, -115f);
        Image bsgImg = btnStartGacha.GetComponent<Image>();
        bsgImg.sprite = LoadSprite("Btn_StartGacha");
        bsgImg.preserveAspect = true;

        GameObject btnCloseGacha = new GameObject("Btn_CloseX", typeof(RectTransform), typeof(Image), typeof(Button));
        btnCloseGacha.transform.SetParent(gachaPanel.transform, false);
        RectTransform bcgRT = btnCloseGacha.GetComponent<RectTransform>();
        bcgRT.sizeDelta = new Vector2(50f, 75f);
        bcgRT.anchoredPosition = new Vector2(200f, 240f);
        Image bcgImg = btnCloseGacha.GetComponent<Image>();
        bcgImg.sprite = LoadSprite("Btn_CloseX");
        bcgImg.preserveAspect = true;

        GameObject gResultGo = new GameObject("GachaResultText", typeof(RectTransform), typeof(TextMeshProUGUI));
        gResultGo.transform.SetParent(gachaPanel.transform, false);
        RectTransform grRT = gResultGo.GetComponent<RectTransform>();
        grRT.sizeDelta = new Vector2(450f, 40f);
        grRT.anchoredPosition = new Vector2(0f, -180f);
        TextMeshProUGUI grText = gResultGo.GetComponent<TextMeshProUGUI>();
        grText.text = "Nhấn [START GACHA] để bắt đầu quay!";
        grText.alignment = TextAlignmentOptions.Center;
        grText.fontSize = 18;
        grText.color = Color.yellow;
        gachaPanel.SetActive(false);

        // 6. Roll Panel
        GameObject rollPanel = new GameObject("Phase2_RollPanel", typeof(RectTransform));
        rollPanel.transform.SetParent(root.transform, false);
        RectTransform rpRT = rollPanel.GetComponent<RectTransform>();
        rpRT.sizeDelta = new Vector2(500f, 600f);
        rpRT.anchoredPosition = Vector2.zero;

        GameObject deckGo = new GameObject("CardDeck", typeof(RectTransform), typeof(Image));
        deckGo.transform.SetParent(rollPanel.transform, false);
        RectTransform dRT = deckGo.GetComponent<RectTransform>();
        dRT.sizeDelta = new Vector2(130f, 175f);
        dRT.anchoredPosition = new Vector2(0f, 100f);
        Image dImg = deckGo.GetComponent<Image>();
        dImg.sprite = LoadSprite("Card_Deck_Back");
        dImg.preserveAspect = true;

        GameObject btnRoll5 = new GameObject("Btn_Roll5", typeof(RectTransform), typeof(Image), typeof(Button));
        btnRoll5.transform.SetParent(rollPanel.transform, false);
        RectTransform br5RT = btnRoll5.GetComponent<RectTransform>();
        br5RT.sizeDelta = new Vector2(170f, 80f);
        br5RT.anchoredPosition = new Vector2(0f, -115f);
        Image br5Img = btnRoll5.GetComponent<Image>();
        br5Img.sprite = LoadSprite("Btn_Roll5");
        br5Img.preserveAspect = true;

        GameObject rollCostGo = new GameObject("RollCostText", typeof(RectTransform), typeof(TextMeshProUGUI));
        rollCostGo.transform.SetParent(rollPanel.transform, false);
        RectTransform rcRT = rollCostGo.GetComponent<RectTransform>();
        rcRT.sizeDelta = new Vector2(300f, 35f);
        rcRT.anchoredPosition = new Vector2(0f, -170f);
        TextMeshProUGUI rcText = rollCostGo.GetComponent<TextMeshProUGUI>();
        rcText.text = "Lần đầu: Miễn phí";
        rcText.alignment = TextAlignmentOptions.Center;
        rcText.fontSize = 16;
        rcText.color = Color.yellow;

        GameObject btnCloseRoll = new GameObject("Btn_CloseX", typeof(RectTransform), typeof(Image), typeof(Button));
        btnCloseRoll.transform.SetParent(rollPanel.transform, false);
        RectTransform bcrRT = btnCloseRoll.GetComponent<RectTransform>();
        bcrRT.sizeDelta = new Vector2(50f, 75f);
        bcrRT.anchoredPosition = new Vector2(200f, 240f);
        Image bcrImg = btnCloseRoll.GetComponent<Image>();
        bcrImg.sprite = LoadSprite("Btn_CloseX");
        bcrImg.preserveAspect = true;

        GameObject rollPromptGo = new GameObject("RollPromptText", typeof(RectTransform), typeof(TextMeshProUGUI));
        rollPromptGo.transform.SetParent(rollPanel.transform, false);
        RectTransform rpTextRT = rollPromptGo.GetComponent<RectTransform>();
        rpTextRT.sizeDelta = new Vector2(450f, 35f);
        rpTextRT.anchoredPosition = new Vector2(0f, -30f);
        TextMeshProUGUI rpPrompt = rollPromptGo.GetComponent<TextMeshProUGUI>();
        rpPrompt.text = "Chọn 1 trong 3 lá bài để nhận phước lành!";
        rpPrompt.alignment = TextAlignmentOptions.Center;
        rpPrompt.fontSize = 17;
        rpPrompt.fontStyle = FontStyles.Bold;
        rpPrompt.color = Color.yellow;

        RectTransform[] cardTransforms = new RectTransform[3];
        Image[] cardImages = new Image[3];
        TMP_Text[] cardTexts = new TMP_Text[3];
        Button[] cardButtons = new Button[3];

        string[] cardSpNames = { "Card_Atk", "Card_Speed", "Card_Hp" };
        float[] cardX = { -140f, 0f, 140f };

        for (int i = 0; i < 3; i++)
        {
            GameObject cGo = new GameObject($"Card_{i + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
            cGo.transform.SetParent(rollPanel.transform, false);
            RectTransform cRT = cGo.GetComponent<RectTransform>();
            cRT.sizeDelta = new Vector2(120f, 185f);
            cRT.anchoredPosition = new Vector2(cardX[i], 100f);
            Image cImg = cGo.GetComponent<Image>();
            cImg.sprite = LoadSprite(cardSpNames[i]);
            cImg.preserveAspect = true;

            GameObject txtGo = new GameObject("Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(cGo.transform, false);
            RectTransform tRT = txtGo.GetComponent<RectTransform>();
            tRT.sizeDelta = new Vector2(110f, 42f);
            tRT.anchoredPosition = new Vector2(0f, -52f);
            TextMeshProUGUI txt = txtGo.GetComponent<TextMeshProUGUI>();
            txt.alignment = TextAlignmentOptions.Center;
            txt.fontSize = 12;
            txt.fontStyle = FontStyles.Bold;
            txt.color = new Color(0.25f, 0.15f, 0.05f, 1f);

            cardTransforms[i] = cRT;
            cardImages[i] = cImg;
            cardTexts[i] = txt;
            cardButtons[i] = cGo.GetComponent<Button>();
            cGo.SetActive(false);
        }
        rollPanel.SetActive(false);

        // 7. Chest Panel
        GameObject chestPanel = new GameObject("Phase2_ChestPanel", typeof(RectTransform));
        chestPanel.transform.SetParent(root.transform, false);
        RectTransform cpRT = chestPanel.GetComponent<RectTransform>();
        cpRT.sizeDelta = new Vector2(500f, 600f);
        cpRT.anchoredPosition = Vector2.zero;

        GameObject chestGo = new GameObject("ChestImage", typeof(RectTransform), typeof(Image), typeof(Button));
        chestGo.transform.SetParent(chestPanel.transform, false);
        RectTransform chRT = chestGo.GetComponent<RectTransform>();
        chRT.sizeDelta = new Vector2(210f, 190f);
        chRT.anchoredPosition = new Vector2(0f, 100f);
        Image chImg = chestGo.GetComponent<Image>();
        chImg.sprite = LoadSprite("Chest_Closed");
        chImg.preserveAspect = true;

        GameObject chestPromptGo = new GameObject("ChestPromptText", typeof(RectTransform), typeof(TextMeshProUGUI));
        chestPromptGo.transform.SetParent(chestPanel.transform, false);
        RectTransform chpRT = chestPromptGo.GetComponent<RectTransform>();
        chpRT.sizeDelta = new Vector2(420f, 40f);
        chpRT.anchoredPosition = new Vector2(0f, -40f);
        TextMeshProUGUI chpText = chestPromptGo.GetComponent<TextMeshProUGUI>();
        chpText.text = "Chạm vào rương để mở!";
        chpText.alignment = TextAlignmentOptions.Center;
        chpText.fontSize = 20;
        chpText.fontStyle = FontStyles.Bold;
        chpText.color = Color.white;

        GameObject envContainer = new GameObject("EnvelopesContainer", typeof(RectTransform));
        envContainer.transform.SetParent(chestPanel.transform, false);
        RectTransform envRT = envContainer.GetComponent<RectTransform>();
        envRT.sizeDelta = new Vector2(450f, 200f);
        envRT.anchoredPosition = new Vector2(0f, 100f);

        // Buff Envelope (Angel)
        GameObject envBuff = new GameObject("Envelope_Buff", typeof(RectTransform), typeof(Image), typeof(Button));
        envBuff.transform.SetParent(envContainer.transform, false);
        RectTransform ebRT = envBuff.GetComponent<RectTransform>();
        ebRT.sizeDelta = new Vector2(150f, 100f);
        ebRT.anchoredPosition = new Vector2(-110f, 10f);
        Image ebImg = envBuff.GetComponent<Image>();
        ebImg.sprite = LoadSprite("Envelope_Buff");
        ebImg.preserveAspect = true;

        GameObject ebLabelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        ebLabelGo.transform.SetParent(envBuff.transform, false);
        RectTransform eblRT = ebLabelGo.GetComponent<RectTransform>();
        eblRT.sizeDelta = new Vector2(140f, 30f);
        eblRT.anchoredPosition = new Vector2(0f, -65f);
        TextMeshProUGUI eblText = ebLabelGo.GetComponent<TextMeshProUGUI>();
        eblText.text = "Thiên Thần (Buff)";
        eblText.alignment = TextAlignmentOptions.Center;
        eblText.fontSize = 15;
        eblText.fontStyle = FontStyles.Bold;
        eblText.color = new Color(0.5f, 1f, 0.5f, 1f);

        // Debuff Envelope (Demon)
        GameObject envDebuff = new GameObject("Envelope_Debuff", typeof(RectTransform), typeof(Image), typeof(Button));
        envDebuff.transform.SetParent(envContainer.transform, false);
        RectTransform edRT = envDebuff.GetComponent<RectTransform>();
        edRT.sizeDelta = new Vector2(125f, 90f);
        edRT.anchoredPosition = new Vector2(110f, 10f);
        Image edImg = envDebuff.GetComponent<Image>();
        edImg.sprite = LoadSprite("Envelope_Debuff");
        edImg.preserveAspect = true;

        GameObject edLabelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        edLabelGo.transform.SetParent(envDebuff.transform, false);
        RectTransform edlRT = edLabelGo.GetComponent<RectTransform>();
        edlRT.sizeDelta = new Vector2(140f, 30f);
        edlRT.anchoredPosition = new Vector2(0f, -65f);
        TextMeshProUGUI edlText = edLabelGo.GetComponent<TextMeshProUGUI>();
        edlText.text = "Ác Ma (Debuff)";
        edlText.alignment = TextAlignmentOptions.Center;
        edlText.fontSize = 15;
        edlText.fontStyle = FontStyles.Bold;
        edlText.color = new Color(1f, 0.45f, 0.45f, 1f);

        envContainer.SetActive(false);
        chestPanel.SetActive(false);

        // 8. Spirit Panel
        GameObject spiritPanel = new GameObject("Phase2_SpiritPanel", typeof(RectTransform));
        spiritPanel.transform.SetParent(root.transform, false);
        RectTransform sprtRT = spiritPanel.GetComponent<RectTransform>();
        sprtRT.sizeDelta = new Vector2(500f, 600f);
        sprtRT.anchoredPosition = Vector2.zero;

        GameObject spiritGo = new GameObject("SpiritImage", typeof(RectTransform), typeof(Image));
        spiritGo.transform.SetParent(spiritPanel.transform, false);
        RectTransform spiRT = spiritGo.GetComponent<RectTransform>();
        spiRT.sizeDelta = new Vector2(220f, 210f);
        spiRT.anchoredPosition = new Vector2(0f, 100f);
        Image spiImg = spiritGo.GetComponent<Image>();
        spiImg.sprite = LoadSprite("Spirit_Fairy");
        spiImg.preserveAspect = true;

        GameObject spTitleGo = new GameObject("SpiritTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        spTitleGo.transform.SetParent(spiritPanel.transform, false);
        RectTransform sptRT = spTitleGo.GetComponent<RectTransform>();
        sptRT.sizeDelta = new Vector2(450f, 40f);
        sptRT.anchoredPosition = new Vector2(0f, -50f);
        TextMeshProUGUI sptText = spTitleGo.GetComponent<TextMeshProUGUI>();
        sptText.text = "LỜI CHÚC PHÚC CỦA TINH LINH";
        sptText.alignment = TextAlignmentOptions.Center;
        sptText.fontSize = 22;
        sptText.fontStyle = FontStyles.Bold;
        sptText.color = Color.green;

        GameObject spEffectGo = new GameObject("SpiritEffect", typeof(RectTransform), typeof(TextMeshProUGUI));
        spEffectGo.transform.SetParent(spiritPanel.transform, false);
        RectTransform speRT = spEffectGo.GetComponent<RectTransform>();
        speRT.sizeDelta = new Vector2(450f, 40f);
        speRT.anchoredPosition = new Vector2(0f, -95f);
        TextMeshProUGUI speText = spEffectGo.GetComponent<TextMeshProUGUI>();
        speText.text = "Nhận phước lành ánh sáng!";
        speText.alignment = TextAlignmentOptions.Center;
        speText.fontSize = 18;
        speText.color = Color.white;
        spiritPanel.SetActive(false);

        // 9. Warning Popup
        GameObject warnPopup = new GameObject("WarningPopup", typeof(RectTransform), typeof(Image));
        warnPopup.transform.SetParent(root.transform, false);
        RectTransform wprt = warnPopup.GetComponent<RectTransform>();
        wprt.sizeDelta = new Vector2(440f, 260f);
        wprt.anchoredPosition = Vector2.zero;
        Image wpImg = warnPopup.GetComponent<Image>();
        wpImg.color = new Color(0.12f, 0.12f, 0.18f, 0.98f);

        GameObject wTitle = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        wTitle.transform.SetParent(warnPopup.transform, false);
        RectTransform wtRT = wTitle.GetComponent<RectTransform>();
        wtRT.sizeDelta = new Vector2(400f, 40f);
        wtRT.anchoredPosition = new Vector2(0f, 85f);
        TextMeshProUGUI wtText = wTitle.GetComponent<TextMeshProUGUI>();
        wtText.text = "CẢNH BÁO";
        wtText.alignment = TextAlignmentOptions.Center;
        wtText.fontSize = 24;
        wtText.fontStyle = FontStyles.Bold;
        wtText.color = Color.red;

        GameObject wMsg = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
        wMsg.transform.SetParent(warnPopup.transform, false);
        RectTransform wmRT = wMsg.GetComponent<RectTransform>();
        wmRT.sizeDelta = new Vector2(390f, 75f);
        wmRT.anchoredPosition = new Vector2(0f, 20f);
        TextMeshProUGUI wmText = wMsg.GetComponent<TextMeshProUGUI>();
        wmText.text = "Nếu thoát lúc này, 25 Lõi Năng Lượng sẽ không được hoàn lại! Bạn chắc chắn muốn thoát?";
        wmText.alignment = TextAlignmentOptions.Center;
        wmText.fontSize = 16;
        wmText.color = Color.white;

        GameObject btnConfirm = new GameObject("Btn_Confirm", typeof(RectTransform), typeof(Image), typeof(Button));
        btnConfirm.transform.SetParent(warnPopup.transform, false);
        RectTransform bcRT = btnConfirm.GetComponent<RectTransform>();
        bcRT.sizeDelta = new Vector2(145f, 48f);
        bcRT.anchoredPosition = new Vector2(-90f, -65f);
        btnConfirm.GetComponent<Image>().color = new Color(0.85f, 0.25f, 0.25f, 1f);
        GameObject ctGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        ctGo.transform.SetParent(btnConfirm.transform, false);
        TextMeshProUGUI ctt = ctGo.GetComponent<TextMeshProUGUI>();
        ctt.text = "Thoát";
        ctt.alignment = TextAlignmentOptions.Center;
        ctt.fontSize = 16;
        ctt.fontStyle = FontStyles.Bold;
        ctt.color = Color.white;

        GameObject btnCancel = new GameObject("Btn_Cancel", typeof(RectTransform), typeof(Image), typeof(Button));
        btnCancel.transform.SetParent(warnPopup.transform, false);
        RectTransform bcnRT = btnCancel.GetComponent<RectTransform>();
        bcnRT.sizeDelta = new Vector2(145f, 48f);
        bcnRT.anchoredPosition = new Vector2(90f, -65f);
        btnCancel.GetComponent<Image>().color = new Color(0.25f, 0.65f, 0.25f, 1f);
        GameObject cntGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        cntGo.transform.SetParent(btnCancel.transform, false);
        TextMeshProUGUI cntt = cntGo.GetComponent<TextMeshProUGUI>();
        cntt.text = "Ở Lại Tiếp Tục";
        cntt.alignment = TextAlignmentOptions.Center;
        cntt.fontSize = 16;
        cntt.fontStyle = FontStyles.Bold;
        cntt.color = Color.white;
        warnPopup.SetActive(false);

        // Reflection injection into private fields of RuneEventUIController
        System.Type t = typeof(RuneEventUIController);
        void SetField(string fName, object val)
        {
            var field = t.GetField(fName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(ctrl, val);
        }

        SetField("_eventRoot", root);
        SetField("_overlayCanvasGroup", cg);
        SetField("_glowLightImage", glowImg);
        SetField("_instructionText", nText);

        SetField("_stonePanel", stonePanel);
        SetField("_stoneImage", sImg);
        SetField("_btnOpen25", btnOpenGo.GetComponent<Button>());
        SetField("_btnSkip", btnSkipGo.GetComponent<Button>());
        SetField("_stoneNoticeText", nText);

        SetField("_gachaPanel", gachaPanel);
        SetField("_wheelTransform", wRT);
        SetField("_btnStartGacha", btnStartGacha.GetComponent<Button>());
        SetField("_btnCloseGacha", btnCloseGacha.GetComponent<Button>());
        SetField("_gachaResultText", grText);

        SetField("_rollPanel", rollPanel);
        SetField("_deckTransform", dRT);
        SetField("_btnRoll5", btnRoll5.GetComponent<Button>());
        SetField("_btnCloseRoll", btnCloseRoll.GetComponent<Button>());
        SetField("_cardTransforms", cardTransforms);
        SetField("_cardImages", cardImages);
        SetField("_cardTexts", cardTexts);
        SetField("_cardButtons", cardButtons);
        SetField("_rollCostText", rcText);

        SetField("_chestPanel", chestPanel);
        SetField("_chestImage", chImg);
        SetField("_btnChestOpen", chestGo.GetComponent<Button>());
        SetField("_chestTapPrompt", chpText);
        SetField("_envelopeContainer", envContainer);
        SetField("_btnEnvelopeBuff", envBuff.GetComponent<Button>());
        SetField("_btnEnvelopeDebuff", envDebuff.GetComponent<Button>());

        SetField("_spiritPanel", spiritPanel);
        SetField("_spiritImage", spiImg);
        SetField("_spiritTitleText", sptText);
        SetField("_spiritEffectText", speText);

        SetField("_warningPopup", warnPopup);
        SetField("_btnConfirmExit", btnConfirm.GetComponent<Button>());
        SetField("_btnCancelExit", btnCancel.GetComponent<Button>());

        root.SetActive(false);

        int uiLayer = LayerMask.NameToLayer("UI");
        void SetLayerRec(GameObject go)
        {
            go.layer = uiLayer;
            foreach (Transform c in go.transform) SetLayerRec(c.gameObject);
        }
        SetLayerRec(root);

        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(ui.scene);
        EditorSceneManager.SaveScene(ui.scene);
        Debug.Log("[RuneEventHierarchyBuilder] Refined Hierarchy built and saved successfully!");
    }
}
#endif
