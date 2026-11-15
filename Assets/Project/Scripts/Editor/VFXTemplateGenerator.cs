using System.IO;
using UnityEngine;
using UnityEditor;

public static class VFXTemplateGenerator
{
    [MenuItem("Tools/Generate VFX Template Prefabs")]
    public static void GenerateAllVFXTemplates()
    {
        string folderPath = "Assets/Project/Resources/Prefab/VFX";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
        }

        // Material mặc định cho Particle System
        Material particleMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
        if (particleMat == null)
        {
            particleMat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        }

        CreateElementVFX("VFX_Template_Fire", new Color(1f, 0.35f, 0.05f, 1f), new Color(1f, 0.85f, 0.15f, 0.9f), ParticleSystemShapeType.Circle, particleMat, 1.4f, 2.4f, 2.2f);
        CreateElementVFX("VFX_Template_Water", new Color(0.1f, 0.65f, 1f, 1f), new Color(0.8f, 0.95f, 1f, 0.9f), ParticleSystemShapeType.Circle, particleMat, 1.4f, 2.5f, 2.2f);
        CreateElementVFX("VFX_Template_Wood", new Color(0.15f, 0.85f, 0.25f, 1f), new Color(0.7f, 1f, 0.5f, 0.9f), ParticleSystemShapeType.Circle, particleMat, 1.2f, 2.2f, 2.2f);
        CreateElementVFX("VFX_Template_Metal", new Color(1f, 0.88f, 0.25f, 1f), new Color(1f, 1f, 1f, 0.95f), ParticleSystemShapeType.Circle, particleMat, 1.3f, 2.3f, 2.2f);
        CreateElementVFX("VFX_Template_Earth", new Color(0.85f, 0.55f, 0.2f, 1f), new Color(0.95f, 0.75f, 0.45f, 0.9f), ParticleSystemShapeType.Sphere, particleMat, 1.5f, 2.5f, 2.4f);
        CreateActiveSkillVFX("VFX_Template_ActiveSkill", new Color(0.65f, 0.2f, 0.95f, 1f), new Color(1f, 0.85f, 0.1f, 0.95f), particleMat);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=#00ff88>✦ [VFX Generator] Đã khởi tạo hoàn tất 6 bộ khuôn VFX Prefabs kích thước chuẩn tại Assets/Project/Resources/Prefab/VFX/!</color>");
    }

    private static void CreateElementVFX(string prefabName, Color mainColor, Color secondaryColor, ParticleSystemShapeType shapeType, Material mat, float minSize, float maxSize, float radius)
    {
        GameObject go = new GameObject(prefabName);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();
        if (mat != null) psr.material = mat;
        psr.sortingLayerName = "Default";
        psr.sortingOrder = 32;

        var main = ps.main;
        main.duration = 1.0f;
        main.loop = false;
        main.startLifetime = 0.85f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 6.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = new ParticleSystem.MinMaxGradient(mainColor, secondaryColor);
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.rateOverTime = 40f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 30) });

        var shape = ps.shape;
        shape.shapeType = shapeType;
        shape.radius = radius;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(mainColor, 0.0f), new GradientColorKey(secondaryColor, 0.5f), new GradientColorKey(mainColor, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.85f, 0.6f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = grad;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0.4f);
        curve.AddKey(0.35f, 1.25f);
        curve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        go.AddComponent<VFXParticleAutoDespawn>();

        string path = $"Assets/Project/Resources/Prefab/VFX/{prefabName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    private static void CreateActiveSkillVFX(string prefabName, Color primaryColor, Color burstColor, Material mat)
    {
        GameObject go = new GameObject(prefabName);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();
        if (mat != null) psr.material = mat;
        psr.sortingLayerName = "Default";
        psr.sortingOrder = 32;

        var main = ps.main;
        main.duration = 1.2f;
        main.loop = false;
        main.startLifetime = 1.0f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(4.5f, 7.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.8f, 3.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(primaryColor, burstColor);
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.rateOverTime = 50f;
        emission.SetBursts(new ParticleSystem.Burst[] 
        { 
            new ParticleSystem.Burst(0.0f, 40),
            new ParticleSystem.Burst(0.18f, 25)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 3.5f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(burstColor, 0.0f), new GradientColorKey(primaryColor, 0.6f), new GradientColorKey(primaryColor, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.85f, 0.65f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = grad;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0.3f);
        curve.AddKey(0.35f, 1.4f);
        curve.AddKey(1f, 0.05f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        go.AddComponent<VFXParticleAutoDespawn>();

        string path = $"Assets/Project/Resources/Prefab/VFX/{prefabName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }
}
