using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Rendering;
public class StatusMatchUI : MonoBehaviour
{
    public static StatusMatchUI instance;
    [Header("Setting UI Element")]
    [SerializeField]private RectTransform ControllerBoard;
    [SerializeField]private RectTransform OptionSkillCharacter;
    [SerializeField]private Image Lock;
    [SerializeField]private Transform skillContainer;
    [SerializeField]private float time;
    [Header("Setting Position")]
    [SerializeField]private Vector3 PosTargetOfSkillContainer;
    [SerializeField]private Vector3 PosTargetOfBoard;
    [SerializeField]private Vector2 SizeTargetOfOptionSkill ;
    [SerializeField]private Vector2 SizeElementOfOptionSkill;
    [SerializeField]private Vector3 PosTargetOfOptionSkill;
    private void Awake()
    {
        instance = this;
    }
    public void TransAll(bool set)
    {
        Lock.gameObject.SetActive(set);
        SkillPassive();
        int hs = set? -1:1;
        SkillActive(hs);
        Controller();
    }
    public void SkillPassive()
    {
        Vector3 origin = skillContainer.localPosition;
        skillContainer.DOLocalMove(PosTargetOfSkillContainer,time).SetEase(Ease.InQuad).OnComplete(()=>{
            PosTargetOfSkillContainer = origin;
        });
    }
    public void SkillActive(float set)
    {
        Vector2 sizeOrigin = OptionSkillCharacter.sizeDelta;
        Vector3 posOrigin = OptionSkillCharacter.anchoredPosition;
        Button[]optionList = OptionSkillCharacter.GetComponentsInChildren<Button>();
        Vector2 sizeOriginElement = optionList[0].GetComponent<RectTransform>().sizeDelta;
        OptionSkillCharacter.DOSizeDelta(SizeTargetOfOptionSkill, time).SetEase(Ease.InQuad).OnComplete(() =>
        {
            SizeTargetOfOptionSkill = sizeOrigin;
        });
        Vector3 posOptinSkillOrigin = OptionSkillCharacter.anchoredPosition;
        OptionSkillCharacter.DOAnchorPos(PosTargetOfOptionSkill, time).SetEase(Ease.InQuad).OnComplete(() =>
        {
            SizeElementOfOptionSkill = sizeOriginElement;
            PosTargetOfOptionSkill = posOptinSkillOrigin;
        });
        foreach(Button btn in optionList)
        {
            btn.GetComponent<RectTransform>().DOSizeDelta(SizeElementOfOptionSkill,time).SetEase(Ease.InQuad);
        }
    }
    public void Controller()
    {
        Vector3 posOrigin = ControllerBoard.anchoredPosition;
        ControllerBoard.DOAnchorPos(PosTargetOfBoard, time).SetEase(Ease.InQuad).OnComplete(() =>
        {
            PosTargetOfBoard = posOrigin;
        });

    }
}
