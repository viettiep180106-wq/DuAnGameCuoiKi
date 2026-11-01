using System.Collections;
using UnityEngine;
using DG.Tweening;
using System;
using System.Collections.Generic;
public class BaseCharacter : MonoBehaviour,IPoolable
{
    [Header("----Setting UI----")]
    public TextUI textHP;
    public FillBarUI fillBarUI;
    public SpriteRenderer spriteRenderer;
    [Header("Setting Data")]
    public BaseCharacterDataSO data;
    [SerializeField]private float HP; 
    [SerializeField]private float MaxHP;
    public Tween moveTwen;
    public Vector3 currentTargetPos;
    public Color colorOrigin;
    private Dictionary<Effect,BaseEffect> effectDictionary = new Dictionary<Effect, BaseEffect>();
    private void Awake()
    {
        colorOrigin = spriteRenderer.color;
        BaseEffect[]list = GetComponents<BaseEffect>();
        foreach(BaseEffect effect in list)
        {
            effectDictionary.Add(effect.EffectType,effect);
        }
    }
    public void OnSpawn()
    {
        HP = data.HP;
        MaxHP = HP;
        UpdateHPUI(HP/MaxHP);
    }
    public void OnDespawn()
    {
        CleanUpAllEffect();
    }
    public virtual void Trans(Vector3 posTarget,Ease easeType = Ease.Linear)
    {
        transform.GetComponent<EnemyAnimationController>().ActiveAnimation(TypeAnimationAtivity.isFly);
        currentTargetPos = posTarget;
        Vector2 currentPos2D = transform.position;
        Vector2 targetPos2D = posTarget;
        float distance = Vector2.Distance(targetPos2D, currentPos2D);
        Debug.Log(distance);
        if (distance > 0.1f)
        {
            float duration = distance / data.Speed_Trans;
            moveTwen?.Kill();
            moveTwen = transform.DOMove(posTarget, duration).SetEase(easeType).OnComplete(() =>
            {
                moveTwen = null;
            });
        }
    }
    public virtual void TakeDamage(float damage)
    {
        damage -= data.Dev;
        if (HP - damage > 0)
        {
            HP-=damage;
        }
        else
        {
            HP = 0;
            Deid();
        }
        UpdateHPUI(HP/MaxHP);
    }
    public void UpdateHPUI(float current)
    {
        if(fillBarUI!= null)fillBarUI.DoFill(current,0.4f);
        if(textHP!= null)textHP.UpdateText(HP,0.4f);
    }
    public void PlayEffect(EffectPayLoad effectPayLoad)
    {
        if(effectDictionary.TryGetValue(effectPayLoad.EffectType, out BaseEffect effect))
        {
            effect.Active(effectPayLoad);
        }
    }
    public void Health(float buffHp)
    {
        MaxHP+= buffHp;
        HP+=buffHp;
        UpdateHPUI(HP/MaxHP);
    }
    public virtual void Deid()
    {
        CleanUpAllEffect();
        StartCoroutine(Death());
    }
    public IEnumerator Death()
    {
        // hiệu ứng died xong mới tắt
        yield return new WaitForSeconds(1f);
        ObjectPoolingManager.Instance.Despawn(gameObject);
    }
    public void CleanUpAllEffect()
    {
        foreach(BaseEffect effect in effectDictionary.Values)
        {
            effect.RemoveEffect();
        }
        moveTwen?.Kill();
    }

}
public enum Effect{DoT,Stun,Bleed,Slow,Freeze,Knockback};
public struct EffectPayLoad
{
    public Effect EffectType;
    public float duration;
    public float value;
    public float tickRate;// cd mỗi lần nhảy sát thương
    public float slowRate; 
    public float disKnockback;
}