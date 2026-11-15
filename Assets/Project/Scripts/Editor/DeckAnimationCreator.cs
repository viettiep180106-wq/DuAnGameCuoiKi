using UnityEngine;
using UnityEditor;

public static class DeckAnimationCreator
{
    [MenuItem("Tools/Create Deck Animation")]
    public static void CreateAnimation()
    {
        string animFolder = "Assets/Project/Animation/AnimationDeck";
        if (!AssetDatabase.IsValidFolder(animFolder))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Project/Animation"))
            {
                AssetDatabase.CreateFolder("Assets/Project", "Animation");
            }
            AssetDatabase.CreateFolder("Assets/Project/Animation", "AnimationDeck");
        }

        Sprite roll = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Events/Cards/Deck_Roll.png");
        Sprite sh1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Events/Cards/Deck_Shuffle_1.png");
        Sprite sh2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Events/Cards/Deck_Shuffle_2.png");

        if (roll == null || sh1 == null || sh2 == null)
        {
            Debug.LogError("[DeckAnimationCreator] Missing required deck sprites!");
            return;
        }

        // 1. Create Idle clip
        AnimationClip idleClip = new AnimationClip();
        idleClip.frameRate = 30;
        EditorCurveBinding spriteBinding = new EditorCurveBinding
        {
            type = typeof(UnityEngine.UI.Image),
            path = "",
            propertyName = "m_Sprite"
        };
        ObjectReferenceKeyframe[] idleKeyframes = new ObjectReferenceKeyframe[]
        {
            new ObjectReferenceKeyframe { time = 0f, value = roll }
        };
        AnimationUtility.SetObjectReferenceCurve(idleClip, spriteBinding, idleKeyframes);
        AssetDatabase.CreateAsset(idleClip, $"{animFolder}/Deck_Idle.anim");

        // 2. Create Shuffle clip
        AnimationClip shuffleClip = new AnimationClip();
        shuffleClip.frameRate = 12;
        ObjectReferenceKeyframe[] shuffleKeyframes = new ObjectReferenceKeyframe[]
        {
            new ObjectReferenceKeyframe { time = 0f / 12f, value = sh1 },
            new ObjectReferenceKeyframe { time = 1f / 12f, value = sh2 },
            new ObjectReferenceKeyframe { time = 2f / 12f, value = sh1 },
            new ObjectReferenceKeyframe { time = 3f / 12f, value = sh2 },
            new ObjectReferenceKeyframe { time = 4f / 12f, value = sh1 },
            new ObjectReferenceKeyframe { time = 5f / 12f, value = roll }
        };
        AnimationUtility.SetObjectReferenceCurve(shuffleClip, spriteBinding, shuffleKeyframes);
        AssetDatabase.CreateAsset(shuffleClip, $"{animFolder}/Deck_Shuffle.anim");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[DeckAnimationCreator] Successfully created Deck_Idle and Deck_Shuffle animation clips!");
    }
}
