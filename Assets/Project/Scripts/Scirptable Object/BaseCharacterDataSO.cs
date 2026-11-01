using UnityEngine;
[CreateAssetMenu(fileName = "BaseCharacterDataSO",menuName = "ScriptableObjects/BaseCharacterDataSO")]
public class BaseCharacterDataSO : ScriptableObject
{
    public float HP;
    public float Atk;
    public float Dev;
    public float Crit_Dmg;
    public float Crit_Rate;
    public float Speed_Trans;
}
public enum TypeAnimationAtivity{isFly,isAttack,isIdel,isAttack_1,isAttack_2,isDied,isHit};
