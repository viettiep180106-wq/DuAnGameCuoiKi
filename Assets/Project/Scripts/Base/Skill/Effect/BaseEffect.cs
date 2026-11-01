using UnityEngine;

public abstract class BaseEffect : MonoBehaviour
{
    public abstract Effect EffectType{get;}
    protected BaseCharacter character;
    protected virtual void Awake()
    {
        character  = GetComponent<BaseCharacter>();
    }
    public abstract void Active(EffectPayLoad payLoad);
    public abstract void RemoveEffect();

}
