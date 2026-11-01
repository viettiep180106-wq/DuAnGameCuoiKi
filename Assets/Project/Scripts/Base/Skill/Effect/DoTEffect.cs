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
    IEnumerator DoTRoutine(EffectPayLoad payLoad)
    {
        if(character.spriteRenderer == null) yield break;
        float timer = 0;
        float tick = payLoad.tickRate;
        while(timer > payLoad.duration)
        {
            character.spriteRenderer.DOColor(Color.red, 0.15f).OnComplete(() => {
                character.spriteRenderer.DOColor(character.colorOrigin, 0.15f);
            });
            character.TakeDamage(payLoad.value);
            yield return new WaitForSeconds(timer);
            timer += tick;
        }
    }
    public override void RemoveEffect()
    {
        StopAllCoroutines();
        character.spriteRenderer.color = character.colorOrigin;
    }
}
