using UnityEngine;
using System;
using UnityEngine.Events;

public class PlayerAnimationController : MonoBehaviour
{
     public Animator player;
    public UnityAction<TypeAnimationAtivity> OnChangedStatusPlayer;
    private void OnEnable()
    {
        OnChangedStatusPlayer+=Status;
        
    }
    public void Status(TypeAnimationAtivity name)
    {
        if(name != TypeAnimationAtivity.isIdel)
        {
            player.SetTrigger(name.ToString());
            player.SetBool("isIdel",false);
        }
        else
        {
            player.SetBool(name.ToString(),true);
        }
    }
    private void OnDisable()
    {
        OnChangedStatusPlayer-=Status;
    }
    public void ActiveAnimation(TypeAnimationAtivity type)
    {
        OnChangedStatusPlayer?.Invoke(type);
    }

}
