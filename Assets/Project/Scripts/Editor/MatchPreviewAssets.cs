using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal static class MatchPreviewAssets
{
    internal const string Folder = "Assets/Project/Resources/" + MatchStoneVfxProfile.ResourceFolder;
    internal const string ShaderPath = Folder + "/MatchStonePreview.shader";

    internal static string AssetName(MatchPreviewElement element, bool link) =>
        MatchStoneVfxProfile.PrefabName((ElementType)element, link);

    internal static Material PrefabMaterial(MatchPreviewElement element, bool link)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/" + AssetName(element, link) + ".prefab");
        return prefab ? prefab.GetComponent<RawImage>()?.material : null;
    }

    public static void Build()
    {
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        if (!shader) throw new System.InvalidOperationException("Thiếu shader preview match đá.");

        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            foreach (MatchPreviewElement element in System.Enum.GetValues(typeof(MatchPreviewElement)))
            {
                Save(element, false, shader, scene);
                Save(element, true, shader, scene);
            }
            AssetDatabase.SaveAssets();
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    private static void Save(MatchPreviewElement element, bool link, Shader shader, Scene scene)
    {
        string id = AssetName(element, link);
        string path = Folder + "/" + id;
        var material = AssetDatabase.LoadAssetAtPath<Material>(path + ".mat");
        if (!material)
        {
            material = new Material(shader) { name = id };
            material.SetFloat("_Element",(int)element);
            material.SetFloat("_Kind",link ? 1 : 0);
            material.SetFloat("_Age",.19f);
            AssetDatabase.CreateAsset(material,path + ".mat");
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(path + ".prefab")) return;
        var root = new GameObject(id,typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));
        SceneManager.MoveGameObjectToScene(root,scene);
        var image = root.GetComponent<RawImage>();
        image.rectTransform.sizeDelta = link ? new Vector2(128,70) : new Vector2(224,224);
        image.material = material;
        image.raycastTarget = false;
        image.maskable = false;
        PrefabUtility.SaveAsPrefabAsset(root,path + ".prefab");
        Object.DestroyImmediate(root);
    }
}
