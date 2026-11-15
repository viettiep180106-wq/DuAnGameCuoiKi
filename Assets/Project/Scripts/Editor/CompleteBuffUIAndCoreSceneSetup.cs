using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class CompleteBuffUIAndCoreSceneSetup
{
    [MenuItem("Tools/Setup Buff UI and Energy Core Scene")]
    public static void SetupScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("[CompleteBuffUIAndCoreSceneSetup] Active scene is invalid!");
            return;
        }

        // --- 1. SETUP STATS PANEL (Replace COIN with Core Icon) ---
        PlayerStatsUI playerStats = Object.FindFirstObjectByType<PlayerStatsUI>(FindObjectsInactive.Include);
        if (playerStats != null)
        {
            Transform statCoinTrans = playerStats.transform.Find("Stat_Coin");
            if (statCoinTrans != null)
            {
                RectTransform coinRect = statCoinTrans.GetComponent<RectTransform>();
                // Allow width for icon + text
                coinRect.sizeDelta = new Vector2(100f, 30f);

                // Add or get CoreIcon child
                Transform coreIconTrans = statCoinTrans.Find("CoreIcon");
                GameObject coreIconGO;
                if (coreIconTrans == null)
                {
                    coreIconGO = new GameObject("CoreIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    coreIconGO.transform.SetParent(statCoinTrans, false);
                }
                else
                {
                    coreIconGO = coreIconTrans.gameObject;
                }

                RectTransform iconRect = coreIconGO.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0f, 0.5f);
                iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.pivot = new Vector2(0f, 0.5f);
                iconRect.anchoredPosition = new Vector2(0f, 0f);
                iconRect.sizeDelta = new Vector2(22f, 26f);

                Image iconImg = coreIconGO.GetComponent<Image>();
                Sprite coreSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Events/EnergyCore_Icon.png");
                if (coreSprite != null)
                {
                    iconImg.sprite = coreSprite;
                    iconImg.raycastTarget = false;
                }

                // Adjust text RectTransform so it sits to the right of the icon
                TextMeshProUGUI coinTMP = statCoinTrans.GetComponent<TextMeshProUGUI>();
                if (coinTMP != null)
                {
                    coinTMP.margin = new Vector4(26f, 0f, 0f, 0f);
                    coinTMP.alignment = TextAlignmentOptions.MidlineLeft;
                    coinTMP.text = "50";
                }

                // Wire into PlayerStatsUI
                SerializedObject soStats = new SerializedObject(playerStats);
                soStats.FindProperty("energyCoreIcon").objectReferenceValue = iconImg;
                soStats.ApplyModifiedProperties();
                EditorUtility.SetDirty(playerStats);
            }
        }

        // --- 2. SETUP ENERGY CORE FLY MANAGER IN CANVAS ---
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            Transform flyManagerTrans = canvas.transform.Find("EnergyCoreFlyManager");
            GameObject flyManagerGO;
            if (flyManagerTrans == null)
            {
                flyManagerGO = new GameObject("EnergyCoreFlyManager", typeof(RectTransform), typeof(EnergyCoreFlyManager));
                flyManagerGO.transform.SetParent(canvas.transform, false);
            }
            else
            {
                flyManagerGO = flyManagerTrans.gameObject;
                if (flyManagerGO.GetComponent<EnergyCoreFlyManager>() == null)
                {
                    flyManagerGO.AddComponent<EnergyCoreFlyManager>();
                }
            }

            RectTransform flyRect = flyManagerGO.GetComponent<RectTransform>();
            flyRect.anchorMin = Vector2.zero;
            flyRect.anchorMax = Vector2.one;
            flyRect.sizeDelta = Vector2.zero;
            flyRect.anchoredPosition = Vector2.zero;
            flyManagerGO.transform.SetAsLastSibling();

            EnergyCoreFlyManager flyMgr = flyManagerGO.GetComponent<EnergyCoreFlyManager>();
            SerializedObject soFly = new SerializedObject(flyMgr);
            soFly.FindProperty("targetCanvas").objectReferenceValue = canvas;
            soFly.FindProperty("containerRect").objectReferenceValue = flyRect;
            Sprite coreSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Events/EnergyCore_Icon.png");
            soFly.FindProperty("energyCoreSprite").objectReferenceValue = coreSprite;
            soFly.ApplyModifiedProperties();
            EditorUtility.SetDirty(flyMgr);
        }

        // --- 3. SETUP RUNE EVENT UI CONTROLLER (Deck Shuffle, Card Front, Envelopes, Spirits) ---
        RuneEventUIController runeController = Object.FindFirstObjectByType<RuneEventUIController>(FindObjectsInactive.Include);
        if (runeController != null)
        {
            Transform rollPanel = runeController.transform.Find("Phase2_RollPanel");
            if (rollPanel != null)
            {
                // Deck
                Transform deckTrans = rollPanel.Find("CardDeck");
                if (deckTrans != null)
                {
                    RectTransform deckRect = deckTrans.GetComponent<RectTransform>();
                    deckRect.sizeDelta = new Vector2(130f, 165f);

                    Image deckImg = deckTrans.GetComponent<Image>();
                    Sprite deckRollSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Events/Cards/Deck_Roll.png");
                    if (deckImg != null && deckRollSprite != null)
                    {
                        deckImg.sprite = deckRollSprite;
                    }
                }

                // 3 Cards setup
                Sprite cardFrontSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Events/Card_Front_Base.png");
                string[] cardNames = new string[] { "Card_1", "Card_2", "Card_3" };

                for (int i = 0; i < cardNames.Length; i++)
                {
                    Transform cardTrans = rollPanel.Find(cardNames[i]);
                    if (cardTrans == null) continue;

                    RectTransform cardRect = cardTrans.GetComponent<RectTransform>();
                    cardRect.sizeDelta = new Vector2(120f, 175f);

                    Image bgImg = cardTrans.GetComponent<Image>();
                    if (bgImg != null && cardFrontSprite != null)
                    {
                        bgImg.sprite = cardFrontSprite;
                    }

                    // Ensure Icon image child exists
                    Transform iconTrans = cardTrans.Find("Icon");
                    GameObject iconGO;
                    if (iconTrans == null)
                    {
                        iconGO = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                        iconGO.transform.SetParent(cardTrans, false);
                    }
                    else
                    {
                        iconGO = iconTrans.gameObject;
                    }

                    RectTransform iconRect = iconGO.GetComponent<RectTransform>();
                    iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                    iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                    iconRect.pivot = new Vector2(0.5f, 0.5f);
                    iconRect.anchoredPosition = new Vector2(0f, 30f);
                    iconRect.sizeDelta = new Vector2(50f, 50f);
                    Image iconImg = iconGO.GetComponent<Image>();
                    iconImg.raycastTarget = false;

                    // Ensure Desc text child exists and styled
                    Transform descTrans = cardTrans.Find("Desc");
                    if (descTrans != null)
                    {
                        RectTransform descRect = descTrans.GetComponent<RectTransform>();
                        descRect.anchorMin = new Vector2(0f, 0f);
                        descRect.anchorMax = new Vector2(1f, 0.45f);
                        descRect.pivot = new Vector2(0.5f, 0.5f);
                        descRect.offsetMin = new Vector2(10f, 8f);
                        descRect.offsetMax = new Vector2(-10f, -4f);

                        TextMeshProUGUI descTMP = descTrans.GetComponent<TextMeshProUGUI>();
                        if (descTMP != null)
                        {
                            descTMP.fontSize = 11f;
                            descTMP.alignment = TextAlignmentOptions.Top;
                            descTMP.raycastTarget = false;
                        }
                    }
                }
            }

            // Envelopes
            Transform chestPanel = runeController.transform.Find("Phase2_ChestPanel");
            if (chestPanel != null)
            {
                Transform envContainer = chestPanel.Find("EnvelopesContainer");
                if (envContainer != null)
                {
                    Transform envBuff = envContainer.Find("Envelope_Buff");
                    if (envBuff != null)
                    {
                        Image img = envBuff.GetComponent<Image>();
                        Sprite env1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Events/Envelopes/Envelope_1.png");
                        if (img != null && env1 != null)
                        {
                            img.sprite = env1;
                            envBuff.GetComponent<RectTransform>().sizeDelta = new Vector2(110f, 70f);
                        }
                    }

                    Transform envDebuff = envContainer.Find("Envelope_Debuff");
                    if (envDebuff != null)
                    {
                        Image img = envDebuff.GetComponent<Image>();
                        Sprite env2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Events/Envelopes/Envelope_2.png");
                        if (img != null && env2 != null)
                        {
                            img.sprite = env2;
                            envDebuff.GetComponent<RectTransform>().sizeDelta = new Vector2(110f, 70f);
                        }
                    }
                }
            }

            // Wire all references into RuneEventUIController
            SerializedObject soRune = new SerializedObject(runeController);
            if (rollPanel != null)
            {
                Transform deckTrans = rollPanel.Find("CardDeck");
                if (deckTrans != null)
                {
                    soRune.FindProperty("_deckImage").objectReferenceValue = deckTrans.GetComponent<Image>();
                }

                SerializedProperty cardImgsProp = soRune.FindProperty("_cardImages");
                SerializedProperty cardBgsProp = soRune.FindProperty("_cardBackgroundImages");
                cardImgsProp.arraySize = 3;
                cardBgsProp.arraySize = 3;

                string[] cardNames = new string[] { "Card_1", "Card_2", "Card_3" };
                for (int i = 0; i < 3; i++)
                {
                    Transform c = rollPanel.Find(cardNames[i]);
                    if (c != null)
                    {
                        cardBgsProp.GetArrayElementAtIndex(i).objectReferenceValue = c.GetComponent<Image>();
                        Transform iconTrans = c.Find("Icon");
                        if (iconTrans != null)
                        {
                            cardImgsProp.GetArrayElementAtIndex(i).objectReferenceValue = iconTrans.GetComponent<Image>();
                        }
                    }
                }
            }

            soRune.FindProperty("_spriteDeckRoll").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Events/Cards/Deck_Roll.png");
            soRune.FindProperty("_spriteDeckShuffle1").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Events/Cards/Deck_Shuffle_1.png");
            soRune.FindProperty("_spriteDeckShuffle2").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Events/Cards/Deck_Shuffle_2.png");
            soRune.FindProperty("_spriteCardFrontBase").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Events/Card_Front_Base.png");
            soRune.FindProperty("_spriteEnvelope1").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Events/Envelopes/Envelope_1.png");
            soRune.FindProperty("_spriteEnvelope2").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Events/Envelopes/Envelope_2.png");
            soRune.FindProperty("_spriteSpiritFairy").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Events/Spirits/Spirit_Fairy.png");
            soRune.FindProperty("_spriteSpiritDemon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Events/Spirits/Spirit_Demon.png");

            soRune.ApplyModifiedProperties();
            EditorUtility.SetDirty(runeController);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[CompleteBuffUIAndCoreSceneSetup] Scene setup successfully completed!");
    }
}
