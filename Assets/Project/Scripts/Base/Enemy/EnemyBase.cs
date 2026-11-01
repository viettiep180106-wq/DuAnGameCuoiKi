using System;
using UnityEngine;
using System.Collections;
[RequireComponent(typeof(EnemyAnimationController),typeof(BoxCollider2D))]
public class EnemyBase : BaseCharacter
{
    public EnemyType type;
    //
    private Coroutine attackRoutine; 
    //
    public virtual void Attack()
    {
        // Có thể gọi Animation tấn công ở đây
        transform.GetComponent<EnemyAnimationController>().ActiveAnimation(TypeAnimationAtivity.isAttack_1);
    }
    public override void TakeDamage(float damage)
    {
        transform.GetComponent<EnemyAnimationController>().ActiveAnimation(TypeAnimationAtivity.isHit);
        base.TakeDamage(damage);
    }
    public override void Deid()
    {
        transform.GetComponent<EnemyAnimationController>().ActiveAnimation(TypeAnimationAtivity.isDied);
        //
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
        //
        base.Deid();
        EnemyController.instance.numEnemy--;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Player targetPlayer = collision.GetComponent<Player>();
            if (targetPlayer != null && attackRoutine == null)
            {
                attackRoutine = StartCoroutine(DamageOverTime(targetPlayer));
            }
            //
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if(attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
            }
        }
    }
    //
    private IEnumerator DamageOverTime(Player targetPlayer)
    {
        // 
        while (targetPlayer != null)
        {
            
            Attack();
            
            float damageAmount = data.Atk; 
            
            targetPlayer.TakeDamage(damageAmount);

            yield return new WaitForSeconds(3f);
        }
        
        // 
        attackRoutine = null;
    }
    //
}
public enum EnemyType{Enemy_1,Enemy_2};
