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
        if (textComponent == null) textComponent = GetComponentInChildren<TMP_Text>();
        if (textComponent != null) textComponent.alpha = 1f;
    }

    public void OnDespawn()
    {
        CancelInvoke(nameof(DespawnSelf));
        transform.DOKill();
        if (textComponent != null)
        {
            textComponent.DOKill();
            textComponent.alpha = 1f;
        }
        transform.localScale = Vector3.one;
    }

    public void Show(float damage, bool isCrit, ElementType element, Vector3 worldPos)
    {
        if (textComponent == null) textComponent = GetComponentInChildren<TMP_Text>();
        if (textComponent == null) return;

        transform.position = new Vector3(worldPos.x + Random.Range(-0.35f, 0.35f), worldPos.y + 0.8f, 985f);

        int dmgInt = Mathf.RoundToInt(damage);
        if (isCrit)
        {
            textComponent.SetText("CRIT! -{0}", dmgInt);
            textComponent.color = new Color(1f, 0.88f, 0.15f); // Vàng rực chí mạng
            transform.localScale = Vector3.one * 1.5f;
        }
        else
        {
            textComponent.SetText("-{0}", dmgInt);
            textComponent.color = ElementalHelper.GetColor(element);
            transform.localScale = Vector3.one * 1.2f;
        }

        // Animation: Nảy lên và mờ dần
        transform.DOKill();
        textComponent.DOKill();
        textComponent.alpha = 1f;
        transform.DOMoveY(transform.position.y + 1.8f, 0.85f).SetEase(Ease.OutCubic);
        textComponent.DOFade(0f, 0.85f).SetDelay(0.25f).SetEase(Ease.InQuad).OnComplete(DespawnSelf);

        CancelInvoke(nameof(DespawnSelf));
        Invoke(nameof(DespawnSelf), 1.25f);
    }

    public void ShowCustom(string message, Color color, Vector3 worldPos, float scaleMultiplier = 1.35f)
    {
        if (textComponent == null) textComponent = GetComponentInChildren<TMP_Text>();
        if (textComponent == null) return;

        transform.position = new Vector3(worldPos.x, worldPos.y + 1.6f, 985f);
        textComponent.text = message;
        textComponent.color = color;
        transform.localScale = Vector3.one * scaleMultiplier;

        transform.DOKill();
        textComponent.DOKill();
        textComponent.alpha = 1f;
        transform.DOMoveY(transform.position.y + 1.6f, 1.0f).SetEase(Ease.OutCubic);
        textComponent.DOFade(0f, 1.0f).SetDelay(0.35f).SetEase(Ease.InQuad).OnComplete(DespawnSelf);

        CancelInvoke(nameof(DespawnSelf));
        Invoke(nameof(DespawnSelf), 1.45f);
    }

    private void DespawnSelf()
    {
        CancelInvoke(nameof(DespawnSelf));
        transform.DOKill();
        if (textComponent != null) textComponent.DOKill();
        if (gameObject.activeInHierarchy)
        {
            if (ObjectPoolingManager.Instance != null)
                ObjectPoolingManager.Instance.Despawn(gameObject);
            else
                Destroy(gameObject);
        }
    }
}

