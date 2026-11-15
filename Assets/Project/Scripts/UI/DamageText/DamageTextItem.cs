using UnityEngine;
using TMPro;
using DG.Tweening;

public class DamageTextItem : MonoBehaviour, IPoolable
{
    [SerializeField] private TMP_Text textComponent;

    private void Awake()
    {
        if (textComponent == null) textComponent = GetComponentInChildren<TMP_Text>();
    }

    public void OnSpawn()
    {
        transform.localScale = Vector3.one;
    }

    public void OnDespawn()
    {
        transform.DOKill();
        if (textComponent != null) textComponent.DOKill();
    }

    public void Show(float damage, bool isCrit, ElementType element, Vector3 worldPos)
    {
        if (textComponent == null) textComponent = GetComponentInChildren<TMP_Text>();
        if (textComponent == null) return;

        transform.position = worldPos + new Vector3(Random.Range(-0.3f, 0.3f), 0.5f, 0);

        int dmgInt = Mathf.RoundToInt(damage);
        if (isCrit)
        {
            textComponent.text = $"CRIT! {dmgInt}";
            textComponent.color = new Color(1f, 0.85f, 0.1f); // Vàng rực
            transform.localScale = Vector3.one * 1.5f;
        }
        else
        {
            textComponent.text = $"{dmgInt}";
            textComponent.color = ElementalHelper.GetColor(element);
            transform.localScale = Vector3.one;
        }

        // Animation: Nảy lên và mờ dần
        transform.DOMoveY(transform.position.y + 1.2f, 0.7f).SetEase(Ease.OutCubic);
        textComponent.alpha = 1f;
        textComponent.DOFade(0f, 0.7f).SetEase(Ease.InQuad).OnComplete(() =>
        {
            ObjectPoolingManager.Instance.Despawn(gameObject);
        });
    }

    public void ShowCustom(string message, Color color, Vector3 worldPos, float scaleMultiplier = 1.2f)
    {
        if (textComponent == null) textComponent = GetComponentInChildren<TMP_Text>();
        if (textComponent == null) return;

        transform.position = worldPos + new Vector3(0f, 1.2f, 0f);
        textComponent.text = message;
        textComponent.color = color;
        transform.localScale = Vector3.one * scaleMultiplier;

        transform.DOMoveY(transform.position.y + 1.4f, 0.9f).SetEase(Ease.OutCubic);
        textComponent.alpha = 1f;
        textComponent.DOFade(0f, 0.9f).SetDelay(0.2f).SetEase(Ease.InQuad).OnComplete(() =>
        {
            ObjectPoolingManager.Instance.Despawn(gameObject);
        });
    }
}

