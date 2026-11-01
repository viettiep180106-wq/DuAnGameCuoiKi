using System;
using UnityEngine;
[RequireComponent(typeof(PlayerAnimationController),typeof(BoxCollider2D))]
public class Player : BaseCharacter
{
    public override void TakeDamage(float damage)
    {
        transform.GetComponent<PlayerAnimationController>().ActiveAnimation(TypeAnimationAtivity.isHit);
        base.TakeDamage(damage);
    }
    public override void Deid()
    {
        GameManager.instance.OnLoseGame?.Invoke(true);
        transform.GetComponent<PlayerAnimationController>().ActiveAnimation(TypeAnimationAtivity.isDied);
        base.Deid();
    }
}
