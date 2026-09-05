    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public class ShopBT : MonoBehaviour
    {
    [SerializeField]private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemPriceText;
    private Image itemIcon;

    private Item currentItem;
    private int price;

    public int Price => price;

    private void Awake()
    {
        itemIcon = GetComponent<Image>();
    }

    public void SetItem(Item item)
    {
        currentItem = item;

        if (item == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        itemNameText.text = item.Item_name;
        itemIcon.sprite = item.Item_icon;
        price = item.Item_price;
        itemPriceText.text = (price == 0? "Free" : price.ToString());

    }

}
