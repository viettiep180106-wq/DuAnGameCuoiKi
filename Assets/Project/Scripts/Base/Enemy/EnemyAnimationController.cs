using UnityEngine;
using System;
using UnityEngine.Events;

public class EnemyAnimationController : MonoBehaviour
{
    public Animator enemy;
    public UnityAction<TypeAnimationAtivity> OnChangedStatusEnemy;
    private void OnEnable()
    {
        OnChangedStatusEnemy+=Status;
    }
    public void Status(TypeAnimationAtivity name)
    {
        if(name != TypeAnimationAtivity.isFly)
        {
            enemy.SetTrigger(name.ToString());
        }
        else
        {
            enemy.SetBool(name.ToString(),true);
        }
    }
    private void OnDisable()
    {
        OnChangedStatusEnemy-=Status;
        
    }
    public void ActiveAnimation(TypeAnimationAtivity type)
    {
        OnChangedStatusEnemy?.Invoke(type);
    }
}
