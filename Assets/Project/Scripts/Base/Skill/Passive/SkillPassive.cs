using DG.Tweening;
using UnityEngine;
using System.Collections;
[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public class SkillPassive : MonoBehaviour, IPoolable
{
    [Header("Skill Settings")]
    public float moveSpeed = 7f;
    public float timeToLive = 5f;
    public LayerMask targetLayer;
    protected TypeStone stoneType;
    protected float skillValue;
    public Transform target;
    protected bool isDespawned = false;
    protected Rigidbody2D rb;
    protected Collider2D col;
    protected virtual void Awake()
    {
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
    }
    public virtual void OnSpawn()
    {
        target = null; 
        isDespawned = false;
        col.isTrigger = true;
    }

    public virtual void OnDespawn()
    {
        target = null;
        isDespawned = true;
        rb.linearVelocity = Vector2.zero;
        DOTween.Kill(this.gameObject.GetInstanceID());
    }

    public void Initialize(float value)
    {
        skillValue = value;
    }
    public TypeStone GetTypeStone() => stoneType;
    public Transform FindNearest(float radius)
    {
        Collider2D []hits = Physics2D.OverlapCircleAll(transform.position,radius,targetLayer);
        float minDis= Mathf.Infinity;
        Transform nearest = null;
        foreach(var hit in hits)
        {
            float dis =  Vector2.Distance(hit.transform.position,transform.position);
            if(dis < minDis)
            {
                minDis = dis;
                nearest = hit.transform;
            }
        }
        return nearest;
    }
    public float GetRateBuffSkill()
    {
        return skillValue/3;
    }
    public virtual void Active()
    {
        DOVirtual.DelayedCall(timeToLive, () =>
        {
           if(gameObject.activeSelf!= false)ObjectPoolingManager.Instance.Despawn(gameObject); 
        }).SetId(gameObject.GetInstanceID());
    }
}
//