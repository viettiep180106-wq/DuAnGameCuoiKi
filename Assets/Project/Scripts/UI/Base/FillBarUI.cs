using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using UnityEditor.ShaderGraph.Internal;

public class FillBarUI : MonoBehaviour
{
    [Header("Target Components (Gán 1 trong 2 hoặc cả 2)")]
    public Image uiImage;
    public SpriteRenderer worldSprite;

    [Header("Settings")]
    [Tooltip("Chiều rộng tối đa (Max Width) của SpriteRenderer khi máu đầy.")]
    [SerializeField] private float maxSpriteWidth;

    private void Awake()
    {
        if (uiImage == null) uiImage = GetComponent<Image>();
        if (worldSprite == null) worldSprite = GetComponent<SpriteRenderer>();

        if (worldSprite != null && worldSprite.drawMode != SpriteDrawMode.Sliced)
        {
            worldSprite.drawMode = SpriteDrawMode.Sliced;
        }
    }

    public void DoFill(float targetValue, float duration)
    {
        
        float normalizedValue = Mathf.Clamp01(targetValue);

        
        if (uiImage != null && uiImage.type == Image.Type.Filled)
        {
            uiImage.DOKill();
            uiImage.DOFillAmount(normalizedValue, duration).SetEase(Ease.OutQuad);
        }

        if (worldSprite != null)
        {
            worldSprite.DOKill();

            float targetWidth = normalizedValue * maxSpriteWidth;

            DOTween.To(
                () => worldSprite.size, 
                x => worldSprite.size = x, 
                new Vector2(targetWidth, worldSprite.size.y), 
                duration
            ).SetEase(Ease.OutQuad).SetTarget(worldSprite); 
        }
    }

    public void SetFillInstant(float value)
    {
        float normalizedValue = Mathf.Clamp01(value);

        if (uiImage != null)
        {
            uiImage.DOKill();
            uiImage.fillAmount = normalizedValue;
        }

        if (worldSprite != null)
        {
            worldSprite.DOKill();
            worldSprite.size = new Vector2(normalizedValue * maxSpriteWidth, worldSprite.size.y);
        }
    }
    public float GetData()
    {
        if(uiImage)return uiImage.fillAmount;
        if(worldSprite)return worldSprite.size.x;
        return 0;
    }

    private void OnDestroy()
    {
        if (uiImage != null) uiImage.DOKill();
        if (worldSprite != null) worldSprite.DOKill();
    }
}