using UnityEngine;
using UnityEditor;

public static class ButtonSpritesAnalyzer
{
    [MenuItem("Tools/Analyze Button Sprites")]
    public static void Analyze()
    {
        string[] btnFiles = new string[] { "btn_1.png", "btn_2.png", "btn_3.png", "btn_4.png", "btn_5.png" };
        foreach (var file in btnFiles)
        {
            string path = $"Assets/Project/Sprite/Buttons/{file}";
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
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
            {
                Color c = tex.GetPixel(tex.width / 2, tex.height / 2);
                Debug.Log($"[ButtonSpritesAnalyzer] {file}: Size=({tex.width}x{tex.height}), CenterColor=({c.r:F2}, {c.g:F2}, {c.b:F2}, {c.a:F2})");
            }
        }
    }
}
