#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class BuffDebuffScaleAndPositionUpgrade
{
    private const string SpriteDir = "Assets/Project/Sprite/Events/";

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    [MenuItem("Tools/Upgrade Buff Debuff Scales and Positions")]
    public static void UpgradeAll()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("[BuffDebuffScaleAndPositionUpgrade] Active scene is invalid!");
            return;
        }

        RuneEventUIController runeController = Object.FindFirstObjectByType<RuneEventUIController>(FindObjectsInactive.Include);
        if (runeController == null)
        {
            Debug.LogError("[BuffDebuffScaleAndPositionUpgrade] RuneEventUIController not found in scene!");
            return;
        }

        Transform popupTrans = runeController.transform;

        // --- 0. GLOW LIGHT ---
        Transform glowTrans = popupTrans.Find("GlowLight");
        if (glowTrans != null)
        {
            RectTransform glowRT = glowTrans.GetComponent<RectTransform>();
            glowRT.anchoredPosition = new Vector2(0f, 470f);
            glowRT.sizeDelta = new Vector2(650f, 650f);
        }

        // --- 1. PHASE 1: STONE PANEL ---
        Transform stonePanelTrans = popupTrans.Find("Phase1_StonePanel");
        if (stonePanelTrans != null)
        {
            RectTransform spRT = stonePanelTrans.GetComponent<RectTransform>();
            spRT.anchoredPosition = new Vector2(0f, 380f);
            spRT.sizeDelta = new Vector2(800f, 900f);

            Transform stoneImgT = stonePanelTrans.Find("StoneImage");
            if (stoneImgT != null)
            {
                RectTransform rt = stoneImgT.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0f, 90f);
                rt.sizeDelta = new Vector2(350f, 430f);
            }

            Transform noticeT = stonePanelTrans.Find("NoticeText");
            if (noticeT != null)
            {
                RectTransform rt = noticeT.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0f, -170f);
                rt.sizeDelta = new Vector2(720f, 90f);
                TextMeshProUGUI tmp = noticeT.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.fontSize = 22f;
            }

            Transform btnOpenT = stonePanelTrans.Find("Btn_Open25");
            if (btnOpenT != null)
            {
                RectTransform rt = btnOpenT.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(-130f, -270f);
                rt.sizeDelta = new Vector2(230f, 85f);
            }

            Transform btnSkipT = stonePanelTrans.Find("Btn_Skip");
            if (btnSkipT != null)
            {
                RectTransform rt = btnSkipT.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(130f, -270f);
                rt.sizeDelta = new Vector2(210f, 85f);
            }
        }

        // --- 2. PHASE 2: GACHA PANEL ---
        Transform gachaPanelTrans = popupTrans.Find("Phase2_GachaPanel");
        Image[] sectorIconImgs = new Image[8];
        RectTransform pointerRect = null;
        GameObject resultContainerGO = null;
        Image resIconImg = null;
        TextMeshProUGUI resTitleTMP = null;
        TextMeshProUGUI resDescTMP = null;

        if (gachaPanelTrans != null)
        {
            RectTransform gpRT = gachaPanelTrans.GetComponent<RectTransform>();
            gpRT.anchoredPosition = new Vector2(0f, 380f);
            gpRT.sizeDelta = new Vector2(800f, 900f);

            // GachaWheel
            Transform wheelTrans = gachaPanelTrans.Find("GachaWheel");
            if (wheelTrans != null)
            {
                RectTransform wRT = wheelTrans.GetComponent<RectTransform>();
                wRT.anchoredPosition = new Vector2(0f, 50f);
                wRT.sizeDelta = new Vector2(600f, 600f);

                Image wImg = wheelTrans.GetComponent<Image>();
                if (wImg != null)
                {
                    Sprite wheelSprite = LoadSprite(SpriteDir + "GachaWheel_Base.png");
                    if (wheelSprite != null) wImg.sprite = wheelSprite;
                    wImg.preserveAspect = true;
                }

                // 8 Sector Icons: R = 205f, Size = 80x80
                float radius = 205f;
                for (int i = 0; i < 8; i++)
                {
                    string iconName = $"SectorIcon_{i}";
                    Transform iconT = wheelTrans.Find(iconName);
                    GameObject iconGO;
                    if (iconT == null)
                    {
                        iconGO = new GameObject(iconName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                        iconGO.transform.SetParent(wheelTrans, false);
                    }
                    else
                    {
                        iconGO = iconT.gameObject;
                    }

                    RectTransform sRT = iconGO.GetComponent<RectTransform>();
                    sRT.anchorMin = new Vector2(0.5f, 0.5f);
                    sRT.anchorMax = new Vector2(0.5f, 0.5f);
                    sRT.pivot = new Vector2(0.5f, 0.5f);
                    sRT.sizeDelta = new Vector2(80f, 80f);

                    float angleDeg = i * 45f;
                    float angleRad = angleDeg * Mathf.Deg2Rad;
                    float posX = radius * Mathf.Sin(angleRad);
                    float posY = radius * Mathf.Cos(angleRad);
                    sRT.anchoredPosition = new Vector2(posX, posY);
                    sRT.localEulerAngles = Vector3.zero;

                    Image sImg = iconGO.GetComponent<Image>();
                    sImg.preserveAspect = true;
                    sImg.raycastTarget = false;
                    sectorIconImgs[i] = sImg;
                }
            }

            // Pointer
            Transform pointerT = gachaPanelTrans.Find("Pointer");
            if (pointerT != null)
            {
                pointerRect = pointerT.GetComponent<RectTransform>();
                pointerRect.anchorMin = new Vector2(0.5f, 0.5f);
                pointerRect.anchorMax = new Vector2(0.5f, 0.5f);
                pointerRect.pivot = new Vector2(0.5f, 0.7f);
                pointerRect.anchoredPosition = new Vector2(0f, 360f);
                pointerRect.sizeDelta = new Vector2(65f, 85f);
                pointerT.SetSiblingIndex(gachaPanelTrans.childCount - 1);
            }

            // Btn_StartGacha
            Transform btnStartT = gachaPanelTrans.Find("Btn_StartGacha");
            if (btnStartT != null)
            {
                RectTransform bRT = btnStartT.GetComponent<RectTransform>();
                bRT.anchoredPosition = new Vector2(0f, -310f);
                bRT.sizeDelta = new Vector2(240f, 75f);
            }

            // GachaResultText
            Transform resTextT = gachaPanelTrans.Find("GachaResultText");
            if (resTextT != null)
            {
                RectTransform rt = resTextT.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0f, 415f);
                rt.sizeDelta = new Vector2(650f, 45f);
                TextMeshProUGUI tmp = resTextT.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.fontSize = 22f;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.alignment = TextAlignmentOptions.Center;
                }
            }

            // ResultContainer
            Transform resContT = gachaPanelTrans.Find("ResultContainer");
            if (resContT != null)
            {
                resultContainerGO = resContT.gameObject;
                RectTransform rcRT = resContT.GetComponent<RectTransform>();
                rcRT.anchoredPosition = new Vector2(0f, -220f);
                rcRT.sizeDelta = new Vector2(460f, 130f);

                Transform icT = resContT.Find("ResultIcon");
                if (icT != null)
                {
                    RectTransform icRT = icT.GetComponent<RectTransform>();
                    icRT.anchoredPosition = new Vector2(45f, 0f);
                    icRT.sizeDelta = new Vector2(75f, 75f);
                    resIconImg = icT.GetComponent<Image>();
                }

                Transform ttlT = resContT.Find("ResultTitle");
                if (ttlT != null)
                {
                    RectTransform ttlRT = ttlT.GetComponent<RectTransform>();
                    ttlRT.offsetMin = new Vector2(95f, 0f);
                    ttlRT.offsetMax = new Vector2(-10f, 0f);
                    resTitleTMP = ttlT.GetComponent<TextMeshProUGUI>();
                    if (resTitleTMP != null) resTitleTMP.fontSize = 20f;
                }

                Transform dscT = resContT.Find("ResultDesc");
                if (dscT != null)
                {
                    RectTransform dscRT = dscT.GetComponent<RectTransform>();
                    dscRT.offsetMin = new Vector2(95f, 0f);
                    dscRT.offsetMax = new Vector2(-10f, 0f);
                    resDescTMP = dscT.GetComponent<TextMeshProUGUI>();
                    if (resDescTMP != null) resDescTMP.fontSize = 15f;
                }
            }

            // Btn_CloseX
            Transform closeXT = gachaPanelTrans.Find("Btn_CloseX");
            if (closeXT != null)
            {
                RectTransform cxRT = closeXT.GetComponent<RectTransform>();
                cxRT.anchoredPosition = new Vector2(340f, 400f);
                cxRT.sizeDelta = new Vector2(55f, 80f);
            }
        }

        // --- 3. PHASE 2: ROLL PANEL ---
        Transform rollPanelTrans = popupTrans.Find("Phase2_RollPanel");
        if (rollPanelTrans != null)
        {
            RectTransform rpRT = rollPanelTrans.GetComponent<RectTransform>();
            rpRT.anchoredPosition = new Vector2(0f, 380f);
            rpRT.sizeDelta = new Vector2(800f, 900f);

            // CardDeck
            Transform deckT = rollPanelTrans.Find("CardDeck");
            if (deckT != null)
            {
                RectTransform dRT = deckT.GetComponent<RectTransform>();
                dRT.anchoredPosition = new Vector2(0f, 90f);
                dRT.sizeDelta = new Vector2(220f, 300f);
            }

            // 3 Cards: Width 220x320 at -260, 0, +260
            float[] cardX = { -260f, 0f, 260f };
            for (int i = 0; i < 3; i++)
            {
                Transform cT = rollPanelTrans.Find($"Card_{i + 1}");
                if (cT != null)
                {
                    RectTransform cRT = cT.GetComponent<RectTransform>();
                    cRT.anchoredPosition = new Vector2(cardX[i], 90f);
                    cRT.sizeDelta = new Vector2(220f, 320f);

                    Transform icT = cT.Find("Icon");
                    if (icT != null)
                    {
                        RectTransform icRT = icT.GetComponent<RectTransform>();
                        icRT.anchoredPosition = new Vector2(0f, 45f);
                        icRT.sizeDelta = new Vector2(75f, 75f);
                    }

                    Transform dscT = cT.Find("Desc");
                    if (dscT != null)
                    {
                        RectTransform dRT = dscT.GetComponent<RectTransform>();
                        dRT.anchorMin = new Vector2(0f, 0f);
                        dRT.anchorMax = new Vector2(1f, 0.45f);
                        dRT.offsetMin = new Vector2(12f, 10f);
                        dRT.offsetMax = new Vector2(-12f, -10f);
                        TextMeshProUGUI tmp = dscT.GetComponent<TextMeshProUGUI>();
                        if (tmp != null) tmp.fontSize = 15f;
                    }
                }
            }

            // RollPromptText
            Transform rptT = rollPanelTrans.Find("RollPromptText");
            if (rptT != null)
            {
                RectTransform rt = rptT.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0f, -120f);
                rt.sizeDelta = new Vector2(720f, 45f);
                TextMeshProUGUI tmp = rptT.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.fontSize = 22f;
            }

            // Btn_Roll5
            Transform br5T = rollPanelTrans.Find("Btn_Roll5");
            if (br5T != null)
            {
                RectTransform rt = br5T.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0f, -220f);
                rt.sizeDelta = new Vector2(230f, 75f);
            }

            // RollCostText
            Transform rctT = rollPanelTrans.Find("RollCostText");
            if (rctT != null)
            {
                RectTransform rt = rctT.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0f, -280f);
                rt.sizeDelta = new Vector2(350f, 40f);
                TextMeshProUGUI tmp = rctT.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.fontSize = 18f;
            }

            // Btn_CloseX
            Transform closeXT = rollPanelTrans.Find("Btn_CloseX");
            if (closeXT != null)
            {
                RectTransform cxRT = closeXT.GetComponent<RectTransform>();
                cxRT.anchoredPosition = new Vector2(340f, 400f);
                cxRT.sizeDelta = new Vector2(55f, 80f);
            }
        }

        // --- 4. PHASE 2: CHEST PANEL ---
        Transform chestPanelTrans = popupTrans.Find("Phase2_ChestPanel");
        if (chestPanelTrans != null)
        {
            RectTransform cpRT = chestPanelTrans.GetComponent<RectTransform>();
            cpRT.anchoredPosition = new Vector2(0f, 380f);
            cpRT.sizeDelta = new Vector2(800f, 900f);

            Transform chestImgT = chestPanelTrans.Find("ChestImage");
            if (chestImgT != null)
            {
                RectTransform rt = chestImgT.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0f, 90f);
                rt.sizeDelta = new Vector2(360f, 320f);
            }

            Transform promptT = chestPanelTrans.Find("ChestPromptText");
            if (promptT != null)
            {
                RectTransform rt = promptT.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0f, -130f);
                rt.sizeDelta = new Vector2(720f, 50f);
                TextMeshProUGUI tmp = promptT.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.fontSize = 22f;
            }

            Transform envContT = chestPanelTrans.Find("EnvelopesContainer");
            if (envContT != null)
            {
                RectTransform ecRT = envContT.GetComponent<RectTransform>();
                ecRT.anchoredPosition = new Vector2(0f, 90f);
                ecRT.sizeDelta = new Vector2(700f, 300f);

                Transform envBuffT = envContT.Find("Envelope_Buff");
                if (envBuffT != null)
                {
                    RectTransform rt = envBuffT.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(-160f, 0f);
                    rt.sizeDelta = new Vector2(220f, 220f);
                }

                Transform envDebuffT = envContT.Find("Envelope_Debuff");
                if (envDebuffT != null)
                {
                    RectTransform rt = envDebuffT.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(160f, 0f);
                    rt.sizeDelta = new Vector2(220f, 220f);
                }
            }
        }

        // --- 5. PHASE 2: SPIRIT PANEL ---
        Transform spiritPanelTrans = popupTrans.Find("Phase2_SpiritPanel");
        if (spiritPanelTrans != null)
        {
            RectTransform spRT = spiritPanelTrans.GetComponent<RectTransform>();
            spRT.anchoredPosition = new Vector2(0f, 380f);
            spRT.sizeDelta = new Vector2(800f, 900f);

            Transform spiritImgT = spiritPanelTrans.Find("SpiritImage");
            if (spiritImgT != null)
            {
                RectTransform rt = spiritImgT.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0f, 90f);
                rt.sizeDelta = new Vector2(360f, 350f);
            }

            Transform titleT = spiritPanelTrans.Find("SpiritTitle");
            if (titleT != null)
            {
                RectTransform rt = titleT.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0f, -130f);
                rt.sizeDelta = new Vector2(720f, 50f);
                TextMeshProUGUI tmp = titleT.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.fontSize = 24f;
            }

            Transform effectT = spiritPanelTrans.Find("SpiritEffect");
            if (effectT != null)
            {
                RectTransform rt = effectT.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0f, -190f);
                rt.sizeDelta = new Vector2(720f, 50f);
                TextMeshProUGUI tmp = effectT.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.fontSize = 20f;
            }
        }

        // --- 5.1. BUFF REWARD POPUP (LEGACY / FALLBACK POPUP) ---
        BuffRewardPopup buffReward = Object.FindFirstObjectByType<BuffRewardPopup>(FindObjectsInactive.Include);
        if (buffReward != null)
        {
            Transform cc = buffReward.transform.Find("CardContainer");
            if (cc != null)
            {
                RectTransform ccRT = cc.GetComponent<RectTransform>();
                ccRT.anchoredPosition = new Vector2(0f, 380f);
                ccRT.sizeDelta = new Vector2(800f, 400f);
            }
        }

        // --- 6. SERIALIZED PROPERTIES WIRE ---
        SerializedObject so = new SerializedObject(runeController);
        if (gachaPanelTrans != null)
        {
            Transform wheelTrans = gachaPanelTrans.Find("GachaWheel");
            so.FindProperty("_wheelTransform").objectReferenceValue = wheelTrans != null ? wheelTrans.GetComponent<RectTransform>() : null;
            so.FindProperty("_pointerTransform").objectReferenceValue = pointerRect;
            so.FindProperty("_gachaResultContainer").objectReferenceValue = resultContainerGO;
            so.FindProperty("_gachaResultIcon").objectReferenceValue = resIconImg;
            so.FindProperty("_gachaResultTitle").objectReferenceValue = resTitleTMP;
            so.FindProperty("_gachaResultDesc").objectReferenceValue = resDescTMP;

            SerializedProperty sectorIconsProp = so.FindProperty("_wheelSectorIcons");
            if (sectorIconsProp != null)
            {
                sectorIconsProp.arraySize = 8;
                for (int i = 0; i < 8; i++)
                {
                    sectorIconsProp.GetArrayElementAtIndex(i).objectReferenceValue = sectorIconImgs[i];
                }
            }
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(runeController);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[BuffDebuffScaleAndPositionUpgrade] Successfully upgraded all 5 Buff/Debuff panels in MainScene!");
    }
}
#endif
