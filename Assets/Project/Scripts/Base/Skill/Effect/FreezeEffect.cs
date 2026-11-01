using UnityEngine;
using DG.Tweening;
public class FreezeEffect : BaseEffect
{
    public override Effect EffectType => Effect.Freeze;
    public override void Active(EffectPayLoad payLoad)
    {
        if(!character.moveTwen.IsActive() || character.moveTwen == null)return;
        if(character.spriteRenderer == null)return;
        RemoveEffect();
        character.moveTwen.timeScale = 1- payLoad.slowRate;
        character.spriteRenderer.DOColor(new Color(0.5f,0.8f,1f),0.2f);
        character.TakeDamage(payLoad.value);
        DOVirtual.DelayedCall(payLoad.duration,RemoveEffect,false).SetId(this.gameObject.GetInstanceID() + "Freeze");
        
    }
    public override void RemoveEffect()
    {
        DOTween.Kill(this.gameObject.GetInstanceID() + "Freeze");
        if(character!= null && character.moveTwen != null){
            character.moveTwen.timeScale = 1f;
            character.spriteRenderer.color = character.colorOrigin;
        }
    }
}
