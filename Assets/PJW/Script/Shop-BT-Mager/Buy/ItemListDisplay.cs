using Assets.PJW.Script.SO_Script;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemListDisplay : MonoBehaviour
{
    [Header("UI 정보 컴포넌트")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI explanationText;
    [SerializeField] private Image iconImage;

    [Header("구매 버튼")]
    [SerializeField] private Button buyButton;

    private Item currentItem;
    private System.Action onPurchaseCallback;

    private void Start()
    {
        if (buyButton != null) buyButton.onClick.AddListener(OnClickBuy);
        //gameObject.SetActive(false);
    }

    public void ShowDetail(Item item, System.Action refreshCallback = null)
    {
        //if (item == null)
        //{
        //    HideDetail();
        //    return;
        //}

        currentItem = item;
        onPurchaseCallback = refreshCallback;
        gameObject.SetActive(true);

        if (nameText != null) nameText.text = item.Item_name;
        if (priceText != null) priceText.text = $"{item.Item_price:#,##0} G";
        if (explanationText != null) explanationText.text = item.item_explanation;
        if (iconImage != null && item.Item_icon != null) iconImage.sprite = item.Item_icon;
    }

    // [구매하기] 버튼 클릭 시 동작 (돈이 있는 한 계속 구매)
    private void OnClickBuy()
    {
        if (currentItem == null) return;

        PlayerWallet wallet = PlayerWallet.Instance;
        if (wallet == null || currentItem.Item_price < 0)
        {
            Debug.LogWarning("[상점] 지갑 또는 구매 가격을 확인하세요.");
            return;
        }

        if (!ShopInventoryBridge.CanReceive(currentItem, 1))
        {
            Debug.LogWarning("[상점] 아이템 매핑 또는 인벤토리 공간을 확인하세요.");
            return;
        }

        if (!wallet.TrySpendMoney(currentItem.Item_price))
        {
            Debug.LogWarning("[상점] 소지금이 부족합니다.");
            return;
        }

        int got = ShopInventoryBridge.Buy(currentItem, 1);
        if (got <= 0)
        {
            wallet.AddMoney(currentItem.Item_price);
            Debug.LogWarning("[상점] 아이템을 넣지 못해 구매 금액을 환불했습니다.");
            return;
        }

        onPurchaseCallback?.Invoke();
        Debug.Log($"{currentItem.Item_name} 구매 성공! (현재 보유량: {ShopInventoryBridge.CountOf(currentItem)}개)");
    }

    public void HideDetail()
    {
        gameObject.SetActive(false);
    }
}
