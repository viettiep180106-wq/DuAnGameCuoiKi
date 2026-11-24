using UnityEngine;
using UnityEngine.SceneManagement;

internal sealed class EnemyFireStatusVfx : MonoBehaviour
{
    private Player player;
    private GameObject effectRoot;
    private EnemyFireAdvancedVisuals visuals;
    private ActiveRoundEffect burn;
    private Vector3 ground;
    private float scale;
    private bool blue;
    private int roundsRemaining;
    private float burnStartedAt;
    private float burnEndedAt;
    private float lastTickAt;
    private float armorStartedAt = float.NegativeInfinity;
    private float armorScale;

    internal static bool ShowBurn(Player target, EnemyAttackVfxArt art, bool blueFire, Vector3 groundPoint, float scale)
    {
        if (!target || target.CurrentHP <= 0f || BattleOver())
            return false;

        float value = blueFire ? 0.03f : 0.02f;
        var state = target.FindRoundEffect(Effect.DoT, ElementType.Fire);
        if (state == null || state.RoundsRemaining <= 0 || state.Value < value)
            return false;

        var presenter = GetOrCreate(target, art);
        bool sameBurn = presenter.burn == state;
        // The combat model merges burns. Keep the stronger blue presentation if it already owns this status.
        if (sameBurn && presenter.blue && !blueFire)
            return true;

        if (!sameBurn || presenter.blue != blueFire || !float.IsPositiveInfinity(presenter.burnEndedAt))
            presenter.burnStartedAt = Time.time;
        presenter.burn = state;
        presenter.blue = blueFire;
        presenter.ground = groundPoint;
        presenter.scale = scale;
        presenter.roundsRemaining = state.RoundsRemaining;
        presenter.lastTickAt = float.NegativeInfinity;
        presenter.burnEndedAt = float.PositiveInfinity;
        return true;
    }

    internal static void ShowArmorBreak(Player target)
    {
        var art = EnemyAttackVfxArt.Load();
        if (!target || target.CurrentHP <= 0f || !art || !art.HasAdvancedArt)
            return;

        var presenter = GetOrCreate(target, art);
        presenter.armorStartedAt = Time.time;
        var renderer = target.spriteRenderer;
        presenter.armorScale = renderer ? Mathf.Clamp(renderer.bounds.size.y / 5.6f, 0.85f, 2f) : 1f;
    }

    private static EnemyFireStatusVfx GetOrCreate(Player target, EnemyAttackVfxArt artwork)
    {
        var presenter = target.GetComponent<EnemyFireStatusVfx>();
        if (!presenter)
        {
            presenter = target.gameObject.AddComponent<EnemyFireStatusVfx>();
            presenter.player = target;
            presenter.effectRoot = new GameObject("Enemy Fire Status VFX") { hideFlags = HideFlags.DontSave };
            SceneManager.MoveGameObjectToScene(presenter.effectRoot, target.gameObject.scene);
            presenter.visuals = new EnemyFireAdvancedVisuals(presenter.effectRoot.transform, artwork);
            var renderer = target.spriteRenderer;
            int order = renderer ? renderer.sortingOrder : 15;
            presenter.visuals.SetSorting(renderer ? renderer.sortingLayerID : 0, order + 2, order - 1);
        }
        presenter.effectRoot.SetActive(true);
        presenter.enabled = true;
        return presenter;
    }

    private void LateUpdate()
    {
        if (!player || player.CurrentHP <= 0f || BattleOver())
        {
            enabled = false;
            return;
        }

        float time = Time.time;
        float armorAge = time - armorStartedAt;
        bool showArmor = armorAge < 0.30f;
        if (showArmor)
            visuals.RenderArmorCue(TargetCenter(), armorAge, armorScale);
        else
            visuals.Clear();

        if (burn != null)
        {
            bool present = player.ActiveRoundEffects.Contains(burn) && burn.RoundsRemaining > 0;
            if (present && burn.RoundsRemaining < roundsRemaining)
                lastTickAt = time;
            if (present)
                roundsRemaining = burn.RoundsRemaining;
            else if (float.IsPositiveInfinity(burnEndedAt))
            {
                burnEndedAt = time;
                if (roundsRemaining == 1)
                    lastTickAt = time;
            }

            float opacity = Smooth(0f, 0.12f, time - burnStartedAt)
                * (1f - Smooth(0.12f, 0.37f, time - burnEndedAt));
            if (opacity > 0f)
            {
                float tickAge = time - lastTickAt;
                float pulse = Smooth(0f, 0.04f, tickAge) * (1f - Smooth(0.08f, 0.23f, tickAge));
                visuals.DrawBurn(blue, time - burnStartedAt, scale, blue ? TargetCenter() : ground, pulse, opacity);
            }
            if (time >= burnEndedAt + 0.37f)
                burn = null;
        }

        if (!showArmor && burn == null)
            enabled = false;
    }

    private Vector3 TargetCenter() => player.spriteRenderer ? player.spriteRenderer.bounds.center : player.transform.position;
    private static float Smooth(float start, float end, float value) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, value));
    private static bool BattleOver() => CombatTurnManager.Instance
        && CombatTurnManager.Instance.CurrentState is CombatTurnState.BattleWon or CombatTurnState.BattleLost;

    private void OnDisable()
    {
        burn = null;
        armorStartedAt = float.NegativeInfinity;
        if (effectRoot)
            effectRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        visuals?.Dispose();
        if (effectRoot)
            Destroy(effectRoot);
    }
}
