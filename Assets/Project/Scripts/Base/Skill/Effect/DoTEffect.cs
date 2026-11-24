using System.Collections;
using DG.Tweening;
using UnityEngine;

public class DoTEffect : BaseEffect
{
    public override Effect EffectType => Effect.DoT;

    public override void Active(EffectPayLoad payLoad)
    {
        StopAllCoroutines();
        StartCoroutine(DoTRoutine(payLoad));
    }

    private IEnumerator DoTRoutine(EffectPayLoad payLoad)
    {
        if (character == null || character.spriteRenderer == null) yield break;

        EnemyStatusVfx.Show(character, EnemyStatusAnimation.Kind.Burn);
        float timer = 0;
        float tick = payLoad.tickRate > 0 ? payLoad.tickRate : 1f;

        while (timer < payLoad.duration)
        {
            yield return new WaitForSeconds(tick);
            timer += tick;

            if (character == null || !character.gameObject.activeInHierarchy) yield break;

            if (character.spriteRenderer != null)
            {
                character.spriteRenderer.DOKill();
                character.spriteRenderer.DOColor(Color.red, 0.15f).OnComplete(() =>
                {
                    if (character != null && character.spriteRenderer != null)
                    {
                        character.spriteRenderer.DOColor(character.colorOrigin, 0.15f);
                    }
                });
            }

            float hpBefore = character.CurrentHP;
            character.TakeDamage(payLoad.value);
            if (character.CurrentHP < hpBefore) EnemyStatusVfx.BurnTick(character);
        }

        EnemyStatusVfx.Finish(character, EnemyStatusAnimation.Kind.Burn);
    }

    public override void RemoveEffect()
    {
        StopAllCoroutines();
        EnemyStatusVfx.Finish(character, EnemyStatusAnimation.Kind.Burn);
        if (character != null && character.spriteRenderer != null)
        {
            character.spriteRenderer.DOKill();
            character.spriteRenderer.color = character.colorOrigin;
        }
    }
}
