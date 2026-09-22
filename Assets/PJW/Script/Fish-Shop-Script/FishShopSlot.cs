using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Uses the same icon and selection-frame Images as the PlantShop slot.
public class FishShopSlot : MonoBehaviour
{
    [SerializeField] private Image clickimage;
    [SerializeField] private Image itemIconImage;

    public void Bind(Sprite icon, UnityAction select)
    {
        var button = GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
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
