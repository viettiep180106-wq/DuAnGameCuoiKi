using UnityEngine;
using DG.Tweening;
using TMPro;
public class TextUI : MonoBehaviour
{
    [Header("----Setting Base----")]
    public TMP_Text textValueCurrent;
    [SerializeField]private float valueCurrent;
    private Tween text;
    public void SetValue(float value)
    {
        valueCurrent = value;
    }
    public void UpdateText(float target,float time)
    {
        text?.Kill();
        text = DOVirtual.Float(valueCurrent, target, time, (value) =>
        {
            int timeCurrent = (int)value;
            textValueCurrent.text = $"{timeCurrent}s";
        }).SetEase(Ease.OutCubic);
    }
    private void OnDestroy()
    {
        text?.Kill();
    }

}
