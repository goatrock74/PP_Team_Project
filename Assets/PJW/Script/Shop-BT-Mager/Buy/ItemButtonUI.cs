using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemButtonUI : MonoBehaviour
{
        [Header("UI 연결 (인스펙터에서 드래그)")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText; // Unity 기본 Text면 Text 컴포넌트로 변경
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private Button button;

        public Button TargetButton => button;

        // 아이템 정보로 UI 갱신
        public void SetItem(Item item)
        {
            if (item == null)
            {
                gameObject.SetActive(false); // 아이템이 없으면 숨김
                return;
            }

            gameObject.SetActive(true);

            if (iconImage != null) iconImage.sprite = item.Item_icon;
            if (nameText != null) nameText.text = item.Item_name;
            if (priceText != null) priceText.text = $"{item.Item_price}G";
        }
}
