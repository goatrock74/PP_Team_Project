    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public class ShopBT : MonoBehaviour
    {
        private TMP_Text itemNameText;
        private Image itemIcon;

        private Item currentItem;

        private void Awake()
        {
            itemNameText = GetComponentInChildren<TMP_Text>();
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
        }
    }
