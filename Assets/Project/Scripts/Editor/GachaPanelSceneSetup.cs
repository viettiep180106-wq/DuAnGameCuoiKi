using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class GachaPanelSceneSetup
{
    [MenuItem("Tools/Setup Gacha Panel In Scene")]
    public static void SetupGachaPanel()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("[GachaPanelSceneSetup] Active scene is invalid!");
            return;
        }

        RuneEventUIController runeController = Object.FindFirstObjectByType<RuneEventUIController>(FindObjectsInactive.Include);
        if (runeController == null)
        {
            Debug.LogError("[GachaPanelSceneSetup] RuneEventUIController not found in scene!");
            return;
        }

        // Find Phase2_GachaPanel
        Transform gachaPanelTrans = runeController.transform.Find("Phase2_GachaPanel");
        if (gachaPanelTrans == null)
        {
            Debug.LogError("[GachaPanelSceneSetup] Phase2_GachaPanel not found under RuneStoneEventPopup!");
            return;
        }

        // 1. Setup GachaWheel
        Transform wheelTrans = gachaPanelTrans.Find("GachaWheel");
        Image[] sectorIconImgs = new Image[8];
        if (wheelTrans != null)
        {
            RectTransform wheelRect = wheelTrans.GetComponent<RectTransform>();
            wheelRect.anchoredPosition = new Vector2(0f, 30f);
            wheelRect.sizeDelta = new Vector2(360f, 360f);

            Image wheelImg = wheelTrans.GetComponent<Image>();
            if (wheelImg != null)
            {
                Sprite wheelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Events/GachaWheel_Base.png");
                if (wheelSprite != null)
                {
                    wheelImg.sprite = wheelSprite;
                    wheelImg.preserveAspect = true;
                    EditorUtility.SetDirty(wheelImg);
                }
            }

            // Tạo 8 sector icons trên bàn xoay tại bán kính R = 120px
            float radius = 120f;
            for (int i = 0; i < 8; i++)
            {
                string iconName = $"SectorIcon_{i}";
                Transform iconT = wheelTrans.Find(iconName);
                GameObject sIconGO;
                if (iconT == null)
                {
                    sIconGO = new GameObject(iconName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    sIconGO.transform.SetParent(wheelTrans, false);
                }
                else
                {
                    sIconGO = iconT.gameObject;
                }

                RectTransform sRT = sIconGO.GetComponent<RectTransform>();
                sRT.anchorMin = new Vector2(0.5f, 0.5f);
                sRT.anchorMax = new Vector2(0.5f, 0.5f);
                sRT.pivot = new Vector2(0.5f, 0.5f);
                sRT.sizeDelta = new Vector2(44f, 44f);

                // Góc tính theo chiều kim đồng hồ từ vị trí 12h:
                float angleDeg = i * 45f;
                float angleRad = angleDeg * Mathf.Deg2Rad;
                float posX = radius * Mathf.Sin(angleRad);
                float posY = radius * Mathf.Cos(angleRad);
                sRT.anchoredPosition = new Vector2(posX, posY);
                sRT.localEulerAngles = Vector3.zero;

                Image sImg = sIconGO.GetComponent<Image>();
                sImg.preserveAspect = true;
                sImg.raycastTarget = false;
                EditorUtility.SetDirty(sImg);

                sectorIconImgs[i] = sImg;
            }
        }

        // 2. Setup Pointer (Direct child of Phase2_GachaPanel, rendered in front of GachaWheel)
        Transform pointerTrans = gachaPanelTrans.Find("Pointer");
        GameObject pointerGO;
        if (pointerTrans == null)
        {
            pointerGO = new GameObject("Pointer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            pointerGO.transform.SetParent(gachaPanelTrans, false);
        }
        else
        {
            pointerGO = pointerTrans.gameObject;
        }

        RectTransform pointerRect = pointerGO.GetComponent<RectTransform>();
        pointerRect.anchorMin = new Vector2(0.5f, 0.5f);
        pointerRect.anchorMax = new Vector2(0.5f, 0.5f);
        pointerRect.pivot = new Vector2(0.5f, 0.7f); // Pivot near top of pointer
        pointerRect.anchoredPosition = new Vector2(0f, 210f);
        pointerRect.sizeDelta = new Vector2(44f, 58f);
        pointerRect.localRotation = Quaternion.identity;
        pointerGO.transform.SetSiblingIndex(gachaPanelTrans.childCount - 1); // Draw on top

        Image pointerImg = pointerGO.GetComponent<Image>();
        if (pointerImg != null)
        {
            Sprite pointerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Events/GachaWheel_Pointer.png");
            if (pointerSprite != null)
            {
                pointerImg.sprite = pointerSprite;
                pointerImg.preserveAspect = true;
                pointerImg.raycastTarget = false;
                EditorUtility.SetDirty(pointerImg);
            }
        }

        // 3. Setup Result Container for Buff/Debuff dynamic display
        Transform resultContainerTrans = gachaPanelTrans.Find("ResultContainer");
        GameObject resultContainerGO;
        if (resultContainerTrans == null)
        {
            resultContainerGO = new GameObject("ResultContainer", typeof(RectTransform));
            resultContainerGO.transform.SetParent(gachaPanelTrans, false);
        }
        else
        {
            resultContainerGO = resultContainerTrans.gameObject;
        }

        RectTransform resultContainerRect = resultContainerGO.GetComponent<RectTransform>();
        resultContainerRect.anchorMin = new Vector2(0.5f, 0.5f);
        resultContainerRect.anchorMax = new Vector2(0.5f, 0.5f);
        resultContainerRect.pivot = new Vector2(0.5f, 0.5f);
        resultContainerRect.anchoredPosition = new Vector2(0f, -170f);
        resultContainerRect.sizeDelta = new Vector2(320f, 110f);

        // Result Icon
        Transform iconTrans = resultContainerGO.transform.Find("ResultIcon");
        GameObject iconGO;
        if (iconTrans == null)
        {
            iconGO = new GameObject("ResultIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGO.transform.SetParent(resultContainerGO.transform, false);
        }
        else
        {
            iconGO = iconTrans.gameObject;
        }
        RectTransform iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(35f, 0f);
        iconRect.sizeDelta = new Vector2(56f, 56f);
        Image iconImg = iconGO.GetComponent<Image>();
        iconImg.raycastTarget = false;

        // Result Title
        Transform titleTrans = resultContainerGO.transform.Find("ResultTitle");
        GameObject titleGO;
        if (titleTrans == null)
        {
            titleGO = new GameObject("ResultTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(resultContainerGO.transform, false);
        }
        else
        {
            titleGO = titleTrans.gameObject;
        }
        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.5f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 0.5f);
        titleRect.offsetMin = new Vector2(70f, 0f);
        titleRect.offsetMax = new Vector2(-10f, 0f);
        TextMeshProUGUI titleTMP = titleGO.GetComponent<TextMeshProUGUI>();
        titleTMP.fontSize = 17f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.MidlineLeft;
        titleTMP.raycastTarget = false;

        // Result Desc
        Transform descTrans = resultContainerGO.transform.Find("ResultDesc");
        GameObject descGO;
        if (descTrans == null)
        {
            descGO = new GameObject("ResultDesc", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            descGO.transform.SetParent(resultContainerGO.transform, false);
        }
        else
        {
            descGO = descTrans.gameObject;
        }
        RectTransform descRect = descGO.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0f, 0f);
        descRect.anchorMax = new Vector2(1f, 0.5f);
        descRect.pivot = new Vector2(0f, 0.5f);
        descRect.offsetMin = new Vector2(70f, 0f);
        descRect.offsetMax = new Vector2(-10f, 0f);
        TextMeshProUGUI descTMP = descGO.GetComponent<TextMeshProUGUI>();
        descTMP.fontSize = 13f;
        descTMP.alignment = TextAlignmentOptions.TopLeft;
        descTMP.textWrappingMode = TextWrappingModes.Normal;
        descTMP.raycastTarget = false;

        resultContainerGO.SetActive(false);

        // Adjust Btn_StartGacha position so it fits nicely
        Transform btnStartTrans = gachaPanelTrans.Find("Btn_StartGacha");
        if (btnStartTrans != null)
        {
            RectTransform btnRect = btnStartTrans.GetComponent<RectTransform>();
            btnRect.anchoredPosition = new Vector2(0f, -240f);
            btnRect.sizeDelta = new Vector2(200f, 65f);
        }

        // Adjust GachaResultText position
        Transform resultTextTrans = gachaPanelTrans.Find("GachaResultText");
        if (resultTextTrans != null)
        {
            RectTransform rtRect = resultTextTrans.GetComponent<RectTransform>();
            rtRect.anchoredPosition = new Vector2(0f, 245f);
            TextMeshProUGUI tmp = resultTextTrans.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.fontSize = 17f;
                tmp.alignment = TextAlignmentOptions.Center;
            }
        }

        // 4. Wire serialized properties into RuneEventUIController via SerializedObject
        SerializedObject so = new SerializedObject(runeController);
        so.FindProperty("_wheelTransform").objectReferenceValue = wheelTrans != null ? wheelTrans.GetComponent<RectTransform>() : null;
        so.FindProperty("_pointerTransform").objectReferenceValue = pointerRect;
        so.FindProperty("_gachaResultContainer").objectReferenceValue = resultContainerGO;
        so.FindProperty("_gachaResultIcon").objectReferenceValue = iconImg;
        so.FindProperty("_gachaResultTitle").objectReferenceValue = titleTMP;
        so.FindProperty("_gachaResultDesc").objectReferenceValue = descTMP;

        SerializedProperty sectorIconsProp = so.FindProperty("_wheelSectorIcons");
        if (sectorIconsProp != null)
        {
            sectorIconsProp.arraySize = 8;
            for (int i = 0; i < 8; i++)
            {
                sectorIconsProp.GetArrayElementAtIndex(i).objectReferenceValue = sectorIconImgs[i];
            }
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(runeController);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[GachaPanelSceneSetup] Successfully configured GachaWheel, Pointer, and ResultContainer in MainScene!");
    }
}
