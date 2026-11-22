using System.Collections;
using UnityEngine;

public sealed class EnemyFireBossPassiveVfx : MonoBehaviour
{
    private void OnEnable()
    {
        var player = Player.Instance;
        if (player && (player.DefDebuffRounds <= 0 || player.DefDebuffPercent < 0.5f))
            StartCoroutine(ShowWhenApplied(player));
    }

    private static IEnumerator ShowWhenApplied(Player player)
    {
        yield return null;
        if (player && player.DefDebuffRounds > 0 && player.DefDebuffPercent >= 0.5f)
            EnemyFireStatusVfx.ShowArmorBreak(player);
    }
}
