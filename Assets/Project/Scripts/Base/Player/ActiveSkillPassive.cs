using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ActiveSkillPassive : MonoBehaviour
{
    public static ActiveSkillPassive instance;
    public float atkRange = 4f;
    public LayerMask enemyLayer;
    public float cdUseSkill;

    private void Awake()
    {
        instance = this;
    }
    public bool CheckEnemyInAtkRange()
    {
        Collider2D[]hits = Physics2D.OverlapCircleAll(transform.position,atkRange);
        if(hits.Length>0)return true;
        else return false;
    }
    public void ActiveSkill()
    {
        if (CheckEnemyInAtkRange() == false) return;
        SkillPassive skill = SkillPassiveManager.instance.GetSkill();
        if(skill== null)return;
        skill.transform.position =transform.position;
        skill.transform.localScale = new Vector3(2f,2f,2f);
        skill.Active();
        transform.GetComponent<PlayerAnimationController>().ActiveAnimation(TypeAnimationAtivity.isAttack);
        DOVirtual.DelayedCall(cdUseSkill, () =>
        {
            ActiveSkill();
        }).SetId(gameObject.GetInstanceID() + "_use_" +skill.GetType().ToString());
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position,atkRange);
    }
}
