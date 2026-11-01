using System.Collections;
using DG.Tweening;
using UnityEngine;

public class StunEffect : BaseEffect
{
    public override Effect EffectType => Effect.Stun;
    public override void Active(EffectPayLoad payLoad)
    {
        StopAllCoroutines();
        StartCoroutine(StunRoutine(payLoad.duration));

    }
    IEnumerator StunRoutine(float duration)
    {
        if(character.moveTwen.IsActive() && character.moveTwen != null)
        {
            character.moveTwen.Pause();
        }
        yield return new WaitForSeconds(duration);
        if(character.moveTwen.IsActive() && character.moveTwen != null)
        {
            character.moveTwen.Play();
        }
    }
    public override void RemoveEffect()
    {
        StopAllCoroutines();
        if(character != null)
        {
            if(character.moveTwen != null)
            {
                if(character.moveTwen.active)
                {
                    character.moveTwen.Play();
                }
            }
        }
    }
}
