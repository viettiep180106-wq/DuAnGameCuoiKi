using UnityEngine;
using UnityEngine.Events;

public class EnemyAnimationController : MonoBehaviour
{
    public Animator enemy;
    public UnityAction<TypeAnimationAtivity> OnChangedStatusEnemy;

    private void Awake()
    {
        if (enemy == null)
        {
            enemy = GetComponent<Animator>();
        }
    }

    private void OnEnable()
    {
        OnChangedStatusEnemy -= Status;
        OnChangedStatusEnemy += Status;
    }

    public void Status(TypeAnimationAtivity name)
    {
        if (enemy == null)
        {
            enemy = GetComponent<Animator>();
            if (enemy == null) return;
        }

        // Trạng thái Fly là trạng thái mặc định (Idle), Animator tự động hồi về thông qua HasExitTime
        if (name == TypeAnimationAtivity.isFly)
        {
            return;
        }

        enemy.SetTrigger(name.ToString());
    }

    private void OnDisable()
    {
        OnChangedStatusEnemy -= Status;
    }

    public void ActiveAnimation(TypeAnimationAtivity type)
    {
        Status(type);
    }
}
