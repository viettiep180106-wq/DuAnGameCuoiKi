using UnityEngine;

public class MatchParticleManager : MonoBehaviour
{
    public static MatchParticleManager Instance { get; private set; }

    [SerializeField] private ParticleSystem matchParticlePrefab;
    private ParticleSystem runtimeParticleSystem;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        CreateRuntimeParticleSystemIfNeeded();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void CreateRuntimeParticleSystemIfNeeded()
    {
        if (matchParticlePrefab != null) return;

        GameObject pObj = new GameObject("RuntimeMatchParticles");
        pObj.transform.SetParent(transform);
        runtimeParticleSystem = pObj.AddComponent<ParticleSystem>();

        var main = runtimeParticleSystem.main;
        main.playOnAwake = false;
        main.startLifetime = 0.4f;
        main.startSpeed = 4f;
        main.startSize = 0.15f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = runtimeParticleSystem.emission;
        emission.rateOverTime = 0;

        var shape = runtimeParticleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.2f;

        var renderer = pObj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 30;
    }

    public void PlayMatchEffect(Vector3 worldPos, TypeStone stoneType)
    {
        if (runtimeParticleSystem == null && matchParticlePrefab == null)
        {
            CreateRuntimeParticleSystemIfNeeded();
        }

        ElementType elem = stoneType switch
        {
            TypeStone.Fire => ElementType.Fire,
            TypeStone.Ice => ElementType.Water,
            TypeStone.Water => ElementType.Water,
            TypeStone.Wood => ElementType.Wood,
            TypeStone.Earth => ElementType.Earth,
            _ => ElementType.Metal
        };

        Color elemColor = ElementalHelper.GetColor(elem);

        if (runtimeParticleSystem != null)
        {
            runtimeParticleSystem.transform.position = worldPos;
            var emitParams = new ParticleSystem.EmitParams
            {
                startColor = elemColor,
                position = worldPos
            };
            runtimeParticleSystem.Emit(emitParams, 12);
        }
        else if (matchParticlePrefab != null)
        {
            ParticleSystem p = Instantiate(matchParticlePrefab, worldPos, Quaternion.identity);
            var main = p.main;
            main.startColor = elemColor;
            p.Play();
            Destroy(p.gameObject, 0.8f);
        }
    }
}

