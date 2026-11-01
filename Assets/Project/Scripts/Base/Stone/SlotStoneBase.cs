
using UnityEngine;
using UnityEngine.UI;


public class SlotStoneBase : MonoBehaviour
{
    [Header("----Setting Base----")]
    public Image Back;
     public RectTransform SlotItem;
    [Header("----Setting Data----")]
    [SerializeField]private StoneBase stone = null;
    public void SetStone(StoneBase st)
    {
        stone = st;
    }
    public StoneBase GetStone()
    {
        return stone;
    }
    public void SwapStoneToSlot(SlotStoneBase slotStone)
    { 
        StoneBase st = stone;
        Vector2Int Idx = stone.GetOriginIndex();
        Vector2Int Idx2 = slotStone.GetStone().GetOriginIndex();
        stone.SetOriginIndex(Idx2.x, Idx2.y);
        slotStone.GetStone().SetOriginIndex(Idx.x,Idx.y);
        SetStone(slotStone.GetStone());
        slotStone.SetStone(st);
    }
    public void SetSize(float x, float y)
    {
        SlotItem.sizeDelta = new Vector2(x,y);
    }
}
