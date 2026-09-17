using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemDetailPanel : MonoBehaviour
{
    [Header("UI 정보 컴포넌트")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI explanationText;
    [SerializeField] private Image iconImage;

    [Header("구매 버튼")]
    [SerializeField] private Button buyButton;

    [Header("설정")]
    [Tooltip("한 번 클릭에 몇 개를 살지")]
    [SerializeField, Min(1)] private int buyAmount = 1;

    private Item currentItem;
    private System.Action onPurchaseCallback;

    private void Start()
    {
        if (buyButton != null) buyButton.onClick.AddListener(OnClickBuy);
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (buyButton != null) buyButton.onClick.RemoveListener(OnClickBuy);
    }

    public void ShowDetail(Item item, System.Action refreshCallback = null)
    {
        if (item == null)
        {
            HideDetail();
            return;
        }

        currentItem = item;
        onPurchaseCallback = refreshCallback;
        gameObject.SetActive(true);

        if (nameText != null) nameText.text = item.Item_name;
        if (priceText != null) priceText.text = $"{item.Item_price * buyAmount:#,##0} G";
        if (explanationText != null) explanationText.text = item.item_explanation;
        if (iconImage != null && item.Item_icon != null) iconImage.sprite = item.Item_icon;

        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        if (currentItem == null || buyButton == null) return;

        PlayerWallet wallet = PlayerWallet.Instance;

        bool canAfford = wallet != null && wallet.CurrentMoney >= currentItem.Item_price * buyAmount;
        bool hasRoom = ShopInventoryBridge.CanReceive(currentItem, buyAmount);

        buyButton.interactable = canAfford && hasRoom;
    }

    // [구매하기] 버튼 클릭 시 동작
    private void OnClickBuy()
    {
        if (currentItem == null) return;

        PlayerWallet wallet = PlayerWallet.Instance;

        if (wallet == null)
        {
            Debug.LogWarning("[상점] PlayerWallet 이 없습니다!");
            return;
        }

        // ★ 자리 확인을 돈 차감보다 먼저.
        //   순서가 바뀌면 가방이 꽉 찼을 때 돈만 사라진다
        if (!ShopInventoryBridge.CanReceive(currentItem, buyAmount))
        {
            Debug.LogWarning("[상점] 가방에 자리가 없습니다.");
            UpdateButtonState();
            return;
        }

        int price = currentItem.Item_price * buyAmount;

        if (!wallet.TrySpendMoney(price))
        {
            Debug.LogWarning("[상점] 소지금이 부족합니다.");
            UpdateButtonState();
            return;
        }

        // ★ Item_count++ 가 아니라 진짜 인벤토리에 넣는다
        int got = ShopInventoryBridge.Buy(currentItem, buyAmount);

        if (got <= 0)
        {
            // 못 넣었으면 돈을 돌려준다
            wallet.AddMoney(price);
            Debug.LogError("[상점] 아이템을 넣지 못해 구매를 취소했습니다. 매핑 표를 확인하세요.");
            return;
        }

        Debug.Log($"[상점] {currentItem.Item_name} {got}개 구매 · {price}G " +
                  $"(현재 보유 {ShopInventoryBridge.CountOf(currentItem)}개)");

        UpdateButtonState();
        onPurchaseCallback?.Invoke();
    }

    public void HideDetail()
    {
        currentItem = null;
        gameObject.SetActive(false);
    }
}
