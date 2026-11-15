using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ActiveSkillPassive : MonoBehaviour
{
    public static ActiveSkillPassive instance;

    [Header("Settings")]
    public float atkRange = 4.2f;
    public LayerMask enemyLayer;
    public float cdUseSkill = 0.8f;

    private void Awake()
    {
        instance = this;
    }

    public void StartCombat()
    {
        DOTween.Kill(gameObject.GetInstanceID() + "_retry_skill");
        DOTween.Kill(gameObject.GetInstanceID() + "_use_skill");
        // Cơ chế mới: Tấn công tức thì khi match stone trên bàn cờ (không dùng hàng đợi hay delay 0.8s nữa)
    }

    public void StopCombat()
    {
        DOTween.Kill(gameObject.GetInstanceID() + "_retry_skill");
        DOTween.Kill(gameObject.GetInstanceID() + "_use_skill");
    }

    public void ActiveSkill()
    {
        // Deprecated: Đòn tấn công hiện được kích hoạt trực tiếp từ BoardController & SkillPassiveManager
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, atkRange);
    }

    private void OnDestroy()
    {
        StopCombat();
    }
}
