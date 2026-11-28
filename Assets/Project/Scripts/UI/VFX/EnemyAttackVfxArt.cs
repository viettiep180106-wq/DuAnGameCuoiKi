using System;
using UnityEngine;

public sealed class EnemyAttackVfxArt : ScriptableObject
{
    [SerializeField] private Shader effectShader;
    [SerializeField] private Shader smokeShader;
    [SerializeField] private Shader advancedShader;
    [SerializeField] private Texture2D advancedAtlas;
    [SerializeField] private Sprite[] effects;

    private static EnemyAttackVfxArt cached;

    public Shader EffectShader => effectShader;
    public Shader SmokeShader => smokeShader;
    public Shader AdvancedShader => advancedShader;
    public Texture2D AdvancedAtlas => advancedAtlas;
    public bool HasAdvancedArt => advancedShader && advancedAtlas
        && advancedAtlas.width >= 1595 && advancedAtlas.height >= 938;
    public Sprite[] Effects => effects;
    public bool IsReady { get; private set; }

    public static EnemyAttackVfxArt Load()
    {
        if (!cached)
        {
            var mgr = GameDataManager.Instance;
            cached = (mgr != null && mgr.EnemyAttackVfxArt != null)
                ? mgr.EnemyAttackVfxArt
                : Resources.Load<EnemyAttackVfxArt>("ScriptObject/VFX/EnemyFireAttackArt");
            if (cached)
            {
                cached.IsReady = cached.effectShader && cached.smokeShader && cached.effects is { Length: 18 }
                    && Array.TrueForAll(cached.effects, sprite => sprite);
            }
        }
        return cached;
    }
}
