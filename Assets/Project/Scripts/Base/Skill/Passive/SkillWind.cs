using UnityEngine;
using DG.Tweening;
public class SkillWind : SkillPassive
{
    [Header("Wind Settings")]
    public float baseDamage = 12f;
    public float windRadius = 2.5f;
    public float Knocback = 0.5f;

    private Vector2 moveDirection;

    public SkillWind() { stoneType = TypeStone.Wind; }

    public override void Active()
    {
        base.Active();
        
        transform.localScale = new Vector3(windRadius, windRadius, 1f);

        target = FindNearest(5f);
        
        if(target != null)
        {
            transform.DOMove(target.position,1f).SetEase(Ease.InQuad);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("OK_1");
        if (isDespawned) return;

        if ((1 << collision.gameObject.layer & targetLayer) != 0)
        {
            BaseCharacter enemy = collision.GetComponent<BaseCharacter>();
            if (enemy != null)
            {
                enemy.TakeDamage(GetRateBuffSkill() * baseDamage);
                
                EffectPayLoad knockback = new EffectPayLoad
                {
                    EffectType = Effect.Knockback,
                    disKnockback = GetRateBuffSkill() * Knocback,
                };
                enemy.PlayEffect(knockback);
            }
            transform.DOKill();
            ObjectPoolingManager.Instance.Despawn(gameObject);
        }
    }
}