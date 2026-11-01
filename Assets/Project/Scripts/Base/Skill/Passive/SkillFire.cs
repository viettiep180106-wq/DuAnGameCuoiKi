using UnityEngine;
using DG.Tweening;

public class SkillFire : SkillPassive
{
    [Header("Data Setting")]
    public float damage = 10f;
    public float durationDot = 4f;
    public float damageDot = 4f;
    public float cdDot = 3f;

    public override void Active()
    {
        base.Active();
        
        target = FindNearest(5f); 
        
        float valueBuff = GetRateBuffSkill();
        damage *= valueBuff;
        durationDot *= valueBuff;
        damageDot *= valueBuff;
        if(target != null)
        {
            transform.DOMove(target.position,1f).SetEase(Ease.InQuad).SetUpdate(UpdateType.Fixed);;
        }
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("OK_1");
        if(isDespawned) return;
        if((1 << collision.gameObject.layer & targetLayer) != 0)
        {
            BaseCharacter enemyTarget = collision.transform.GetComponent<BaseCharacter>();
            if(enemyTarget != null)
            {
                enemyTarget.TakeDamage(damage);
                EffectPayLoad effect = new EffectPayLoad {
                    EffectType = Effect.DoT,
                    duration = durationDot,
                    value = damageDot,
                    tickRate = cdDot
                };
                enemyTarget.PlayEffect(effect);
            }
            Debug.Log("OK");
            transform.DOKill();
            ObjectPoolingManager.Instance.Despawn(gameObject);
        }
    }
}