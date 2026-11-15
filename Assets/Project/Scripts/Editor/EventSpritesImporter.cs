using UnityEditor;
using UnityEngine;

public static class EventSpritesImporter
{
    [MenuItem("Tools/Configure Event Sprites")]
    public static void ConfigureSprites()
    {
        string[] spritePaths = new string[]
        {
            "Assets/Project/Sprite/Events/EnergyCore_Icon.png",
            "Assets/Project/Sprite/Events/Card_Front_Base.png",
            "Assets/Project/Sprite/Events/Cards/Deck_Roll.png",
            "Assets/Project/Sprite/Events/Cards/Deck_Shuffle_1.png",
            "Assets/Project/Sprite/Events/Cards/Deck_Shuffle_2.png",
            "Assets/Project/Sprite/Events/Spirits/Spirit_Fairy.png",
            "Assets/Project/Sprite/Events/Spirits/Spirit_Demon.png",
            "Assets/Project/Sprite/Events/Envelopes/Envelope_1.png",
            "Assets/Project/Sprite/Events/Envelopes/Envelope_2.png"
        };

        foreach (string path in spritePaths)
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
                importer.SaveAndReimport();
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[EventSpritesImporter] Configured {spritePaths.Length} new event sprites successfully!");
    }
}
