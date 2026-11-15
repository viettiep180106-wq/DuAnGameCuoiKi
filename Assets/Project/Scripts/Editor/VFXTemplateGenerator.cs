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

        CreateElementVFX("VFX_Template_Fire", new Color(1f, 0.35f, 0.05f, 1f), new Color(1f, 0.8f, 0.1f, 0.8f), ParticleSystemShapeType.Cone, particleMat);
        CreateElementVFX("VFX_Template_Water", new Color(0.1f, 0.65f, 1f, 1f), new Color(0.8f, 0.95f, 1f, 0.8f), ParticleSystemShapeType.Cone, particleMat);
        CreateElementVFX("VFX_Template_Wood", new Color(0.15f, 0.85f, 0.25f, 1f), new Color(0.7f, 1f, 0.5f, 0.8f), ParticleSystemShapeType.Circle, particleMat);
        CreateElementVFX("VFX_Template_Metal", new Color(1f, 0.85f, 0.2f, 1f), new Color(1f, 1f, 1f, 0.9f), ParticleSystemShapeType.Box, particleMat);
        CreateElementVFX("VFX_Template_Earth", new Color(0.75f, 0.45f, 0.15f, 1f), new Color(0.9f, 0.7f, 0.4f, 0.8f), ParticleSystemShapeType.Sphere, particleMat);
        CreateActiveSkillVFX("VFX_Template_ActiveSkill", new Color(0.65f, 0.2f, 0.95f, 1f), new Color(1f, 0.85f, 0.1f, 0.9f), particleMat);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=#00ff88>✦ [VFX Generator] Đã khởi tạo hoàn tất 6 bộ khuôn VFX Prefabs tại Assets/Project/Resources/Prefab/VFX/!</color>");
    }

    private static void CreateElementVFX(string prefabName, Color mainColor, Color secondaryColor, ParticleSystemShapeType shapeType, Material mat)
    {
        GameObject go = new GameObject(prefabName);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();
        if (mat != null) psr.material = mat;

        var main = ps.main;
        main.duration = 1.0f;
        main.loop = false;
        main.startLifetime = 0.75f;
        main.startSpeed = 2.5f;
        main.startSize = 0.35f;
        main.startColor = new ParticleSystem.MinMaxGradient(mainColor, secondaryColor);
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.rateOverTime = 30f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 15) });

        var shape = ps.shape;
        shape.shapeType = shapeType;
        shape.radius = 0.35f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0.6f);
        curve.AddKey(0.4f, 1.2f);
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

        var main = ps.main;
        main.duration = 1.2f;
        main.loop = false;
        main.startLifetime = 1.0f;
        main.startSpeed = 3.5f;
        main.startSize = 0.5f;
        main.startColor = new ParticleSystem.MinMaxGradient(primaryColor, burstColor);
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.rateOverTime = 40f;
        emission.SetBursts(new ParticleSystem.Burst[] 
        { 
            new ParticleSystem.Burst(0.0f, 35),
            new ParticleSystem.Burst(0.2f, 20)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.5f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(burstColor, 0.0f), new GradientColorKey(primaryColor, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = grad;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0.2f);
        curve.AddKey(0.3f, 1.5f);
        curve.AddKey(1f, 0.1f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        go.AddComponent<VFXParticleAutoDespawn>();

        string path = $"Assets/Project/Resources/Prefab/VFX/{prefabName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }
}
