using UnityEngine;
using DG.Tweening;
using System.Collections;
public class KnockBackEffect : BaseEffect
{
    public override Effect EffectType => Effect.Knockback;
    public override void Active(EffectPayLoad payLoad)
    {
        RemoveEffect();
        Vector3 direction = (transform.position- character.currentTargetPos).normalized;
        Vector3 PosKnockback = transform.position + direction*payLoad.disKnockback; 
        transform.DOMove(PosKnockback, 0.2f).SetEase(Ease.OutBack).OnComplete(() =>
        {
            character.Trans(character.currentTargetPos, Ease.InQuad); 
        });
    }
    public override void RemoveEffect()
    {
        character.moveTwen?.Kill();
    }
}
