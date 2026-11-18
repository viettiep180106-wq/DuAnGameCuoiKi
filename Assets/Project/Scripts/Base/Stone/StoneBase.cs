using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using System;
using System.Collections;

public class StoneBase : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPoolable
{
    [Header("----Setting Base----")]
    public TypeStone nameStone;
    public RectTransform item;

    [Header("----Setting Data----")]
    [SerializeField] private Vector2 OriginPos;
    [SerializeField] private Vector2Int OriginIndex;
    [SerializeField] private Vector3 ScaleStone = new Vector3(0.9f, 0.9f, 0.9f);
    [SerializeField] private Vector2 startPosition;
    [SerializeField] private float Duration = 0.2f;

    [Header("----Setting VFX----")]
    private Tween trans;

    public static event Action<Vector3, TypeStone> OnStoneMatched;

    public ElementType GetElementType()
    {
        return nameStone switch
        {
            TypeStone.Fire => ElementType.Fire,
            TypeStone.Ice => ElementType.Water,
            TypeStone.Water => ElementType.Water,
            TypeStone.Earth => ElementType.Earth,
            TypeStone.Wood => ElementType.Wood,
            TypeStone.Light => ElementType.Metal,
            TypeStone.Wind => ElementType.Metal,
            TypeStone.Thunder => ElementType.Metal,
            _ => ElementType.Metal
        };
    }

    public void OnSpawn()
    {
        transform.localScale = Vector3.one;
    }

    public void OnDespawn()
    {
        if (item != null) item.DOKill();
    }

    public void FinishTask()
    {
        StartCoroutine(PalyMatch());
    }

    public bool isLocked { get; private set; } = false;
    public bool isFrozen { get; private set; } = false;
    private int lockDurationRounds = 0;

    public void LockStone(int rounds)
    {
        isLocked = true;
        lockDurationRounds = rounds;
        var img = GetComponent<UnityEngine.UI.Image>();
        if (img != null) img.color = new Color(0.35f, 0.35f, 0.35f, 0.85f);
        transform.DOShakePosition(0.25f, 8f);
    }

    public void FreezeStone(int rounds)
    {
        isFrozen = true;
        isLocked = true;
        lockDurationRounds = rounds;
        var img = GetComponent<UnityEngine.UI.Image>();
        if (img != null) img.color = new Color(0.4f, 0.8f, 1f, 0.95f);
    }

    public void TickLockRound()
    {
        if (lockDurationRounds > 0)
        {
            lockDurationRounds--;
            if (lockDurationRounds <= 0)
            {
                isLocked = false;
                isFrozen = false;
                var img = GetComponent<UnityEngine.UI.Image>();
                if (img != null) img.color = Color.white;
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isLocked)
        {
            transform.DOShakePosition(0.2f, 6f);
            return;
        }
        startPosition = eventData.position;
        if (item != null) item.DOScale(ScaleStone, 0.15f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isLocked) return;

        Vector2 endPosition = eventData.position;
        Vector2 delta = endPosition - startPosition;
        startPosition = Vector2.zero;
        if (item != null) item.DOScale(Vector3.one, 0.15f);

        Direction type = Direction.down;
        if (delta.magnitude < 20f)
        {
            return;
        }

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            type = delta.x > 0 ? Direction.right : Direction.left;
        }
        else
        {
            type = delta.y > 0 ? Direction.up : Direction.down;
        }

        if (BoardController.instance != null)
        {
            BoardController.instance.OnSwapStone?.Invoke(type, this);
        }
    }

    public void StoneTrans(Vector2 target, float duration)
    {
        if (item != null)
        {
            trans = item.DOAnchorPos(target, duration).SetEase(Ease.OutQuad);
        }
    }

    public IEnumerator PalyMatch()
    {
        // Kích hoạt hiệu ứng nổ ngọc đúng màu hệ
        OnStoneMatched?.Invoke(transform.position, nameStone);

        if (item != null)
        {
            item.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);
        }

        yield return new WaitForSeconds(0.2f);
        ObjectPoolingManager.Instance.Despawn(gameObject);
    }

    public void SetOriginIndex(int x, int y)
    {
        OriginIndex = new Vector2Int(x, y);
    }

    public Vector2Int GetOriginIndex()
    {
        return OriginIndex;
    }
}

public enum Direction { right, left, up, down };
public enum TypeStone { Fire, Ice, Earth, Wood, Light, Wind, Thunder, Water };
