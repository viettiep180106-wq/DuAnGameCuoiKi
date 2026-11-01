using UnityEngine;
using DG.Tweening;

public class SkillIce : SkillPassive
{
    [Header("Ice Settings")]
    public float baseDamage = 15f;
    public float baseFreezeDuration = 2f;
    public float activeRadius = 3f; 
    public SpriteRenderer visualRenderer;

    public SkillIce() { stoneType = TypeStone.Ice; }

    public override void Active()
    {
        base.Active();
        
        transform.localScale = Vector3.zero;
        transform.DOScale(activeRadius * 4f, 0.3f).SetEase(Ease.OutBack).OnComplete(() =>
        {
            ApplyIceExplosion();
            
            visualRenderer.DOFade(0, 0.5f).OnComplete(() => {
                ObjectPoolingManager.Instance.Despawn(gameObject);
            });
        }).SetId(this.gameObject.GetInstanceID());
    }

    private void ApplyIceExplosion()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, activeRadius, targetLayer);
        foreach (var hit in hits)
        {
            BaseCharacter enemy = hit.GetComponent<BaseCharacter>();
            if (enemy != null)
            {
                enemy.TakeDamage(GetRateBuffSkill()*baseDamage);

                EffectPayLoad freezeEffect = new EffectPayLoad
                {
                    EffectType = Effect.Freeze,
                    duration = GetRateBuffSkill()*baseFreezeDuration,
                    value = 0.5f 
                };
                enemy.PlayEffect(freezeEffect);
            }
        }
    }
}