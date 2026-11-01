using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using System;
using System.Collections;
using UnityEngine.UI;
using JetBrains.Annotations;
public class StoneBase : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPoolable
{
    [Header("----Setting Base----")]
    public TypeStone nameStone;
    public RectTransform item;
    [Header("----Setting Data----")]
    [SerializeField]private Vector2 OriginPos;
    [SerializeField]private Vector2Int OriginIndex;
    [SerializeField]private Vector3 ScaleStone = new Vector3(0.9f,0.9f,0.9f);
    [SerializeField]private Vector2 startPosition;
    [SerializeField]private float Duration = 0.2f;
    [Header("----Setting VFX----")]
    Tween trans;
    public void OnSpawn()
    {
        transform.localScale = Vector3.one;
    }
    public void OnDespawn()
    {
        item.DOKill();
    }
    public void FinishTask()
    {
        StartCoroutine(PalyMatch());
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        startPosition = eventData.position;
        item.DOScale(ScaleStone,0.2f);
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        Vector2 endPosition = eventData.position;
        Vector2 delta = endPosition - startPosition;
        startPosition = new Vector2(0,0);
        item.DOScale(new Vector3(1,1,1),0.2f);
        Direction type = Direction.down;
        if(delta.magnitude < 20f)
        {
            return;
        }
        if(Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            // check trái phải
            if (delta.x > 0)
            {
                type = Direction.right;
            }
            if(delta.x < 0)
            {
                type = Direction.left;
            }
        }
        else
        {
            if (delta.y > 0)
            {
                type = Direction.up;
            }
            if(delta.y < 0)
            {
                type = Direction.down;
            }
           
        }
        BoardController.instance.OnSwapStone?.Invoke(type,this);
    }
    public void StoneTrans(Vector2 target,float duration)
    {
        trans = item.DOAnchorPos(target, duration).SetEase(Ease.OutQuad);
    }
    public IEnumerator PalyMatch()
    {
        yield return new WaitForSeconds(0.2f);
        ObjectPoolingManager.Instance.Despawn(gameObject);
        // vfx sau đó ....
    }
    public void SetOriginIndex(int x, int y)
    {
        OriginIndex = new Vector2Int(x,y);
    }
    public Vector2Int GetOriginIndex()
    {
        return OriginIndex;
    }
}
public enum Direction{right, left, up,down};
public enum TypeStone{Fire,Ice,Earth,Wood,Light,Wind,Thunder,Water};



