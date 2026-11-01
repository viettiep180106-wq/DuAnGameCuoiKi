using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(FillBarUI),typeof(TextUI))]
public class SkillActive : MonoBehaviour
{
    public virtual void ActiveSkill()
    {
        
    }
    public void SetUpTimeUI()
    {
        FillBarUI fill = transform.GetComponent<FillBarUI>();
        TextUI text = transform.GetComponent<TextUI>();
        if(fill.uiImage.gameObject.activeInHierarchy == false)
        {
            fill.uiImage.gameObject.SetActive(true);
            fill.DoFill(0,60);
        }
        if(text.textValueCurrent.gameObject.activeInHierarchy == false)
        {
            text.textValueCurrent.gameObject.SetActive(true);
            text.UpdateText(0,60);
        }
    }
}
