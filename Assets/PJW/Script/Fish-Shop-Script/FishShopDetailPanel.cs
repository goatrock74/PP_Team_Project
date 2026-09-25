using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class FishShopDetailPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI explanationText;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Button actionButton;
    private TextMeshProUGUI actionText;
    private UnityAction boundAction;

    public void Bind(UnityAction action)
    {
        // Preserve Inspector events and avoid duplicate callbacks when rebound.
        if (boundAction != null) actionButton.onClick.RemoveListener(boundAction);
        boundAction = action;
        actionButton.onClick.AddListener(action);
        actionText = actionButton.GetComponentInChildren<TextMeshProUGUI>(true);
        HideDetail();
    }

    public void Show(string title, int price, string description, Sprite icon,
        int count, string action, bool canTrade)
    {
        nameText.text = title;
        priceText.text = $"{price:N0} G";
        explanationText.text = description;
        iconImage.sprite = icon;
        iconImage.preserveAspect = true;
        if (countText != null) countText.text = $"보유 {count}개";
        if (actionText != null) actionText.text = action;
        actionButton.interactable = canTrade;
        gameObject.SetActive(true);
    }

    public void HideDetail() => gameObject.SetActive(false);
}
