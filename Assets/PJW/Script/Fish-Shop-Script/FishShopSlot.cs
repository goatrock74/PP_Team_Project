using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Uses the same icon and selection-frame Images as the PlantShop slot.
public class FishShopSlot : MonoBehaviour
{
    [SerializeField] private Image clickimage;
    [SerializeField] private Image itemIconImage;
    private UnityAction boundSelect;

    public void Bind(Sprite icon, UnityAction select)
    {
        var button = GetComponent<Button>();
        // Preserve Inspector events and replace only this slot's runtime callback.
        if (boundSelect != null) button.onClick.RemoveListener(boundSelect);
        boundSelect = select;
        button.onClick.AddListener(select);
        button.interactable = true;
        itemIconImage.sprite = icon;
        itemIconImage.preserveAspect = true;
        // PlantShop uses the slot's root Image as its item icon and hit target.
        itemIconImage.raycastTarget = true;
        SetSelected(false);
        gameObject.SetActive(true);
    }

    public void SetSelected(bool selected)
    {
        if (clickimage == null) return;
        var color = clickimage.color;
        color.a = selected ? 1f : 0f;
        clickimage.color = color;
        clickimage.raycastTarget = false;
    }
}
