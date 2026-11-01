using UnityEngine;

public class SkillWood : SkillPassive
{
    [Header("Wood Settings")]
    public Player player;
    public float baseShieldHP = 20f;
    public float baseHeal = 15f;
    
    private float currentShieldHP;

    public SkillWood() { stoneType = TypeStone.Wood; }

    public override void Active()
    {
        base.Active();
        
        transform.localScale = new Vector3(2f, 2f, 2f);
        
        currentShieldHP = GetRateBuffSkill()*baseShieldHP;
        float healAmount = GetRateBuffSkill()*baseHeal;
        player.Health(healAmount);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TakeShieldDamage(5f); 
    }

    public void TakeShieldDamage(float damage)
    {
        currentShieldHP -= damage;
        if (currentShieldHP <= 0)
        {
            ObjectPoolingManager.Instance.Despawn(gameObject);
        }
    }
}