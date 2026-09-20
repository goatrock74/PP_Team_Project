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
    private Button boundBuyButton;

    private void OnValidate() => ResolveBuyButton();

    private void ResolveBuyButton()
    {
        if (buyButton != null) return;
        // 상세창 내부에 버튼이 하나일 때만 자동 연결한다.
        Button[] buttons = GetComponentsInChildren<Button>(true);
        if (buttons.Length == 1) buyButton = buttons[0];
    }

    private void BindBuyButton()
    {
        ResolveBuyButton();
        if (boundBuyButton == buyButton) return;
        if (boundBuyButton != null) boundBuyButton.onClick.RemoveListener(OnClickBuy);
        boundBuyButton = buyButton;
        if (boundBuyButton != null) boundBuyButton.onClick.AddListener(OnClickBuy);
    }

    private void Awake()
    {
        BindBuyButton();
    }

    private void Start()
    {
        if (currentItem == null) gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (boundBuyButton != null) boundBuyButton.onClick.RemoveListener(OnClickBuy);
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
        BindBuyButton();
        if (buyButton == null)
            Debug.LogError("[상점] 구매 버튼이 연결되지 않았습니다. ItemDetailPanel의 Buy Button을 연결하세요.", this);

        if (nameText != null) nameText.text = item.Item_name;
        if (priceText != null) priceText.text = $"{(long)item.Item_price * buyAmount:#,##0} G";
        if (explanationText != null) explanationText.text = item.item_explanation;
        if (iconImage != null && item.Item_icon != null) iconImage.sprite = item.Item_icon;

        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        if (currentItem == null || buyButton == null) return;

        PlayerWallet wallet = PlayerWallet.Instance;

        long price = (long)currentItem.Item_price * buyAmount;
        bool canAfford = wallet != null && currentItem.Item_price >= 0 && buyAmount > 0
            && price <= int.MaxValue && wallet.CurrentMoney >= price;
        bool hasRoom = ShopInventoryBridge.CanReceive(currentItem, buyAmount);

        buyButton.interactable = canAfford && hasRoom;
    }

    // [구매하기] 버튼 클릭 시 동작
    public void OnClickBuy()
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

        long totalPrice = (long)currentItem.Item_price * buyAmount;
        if (currentItem.Item_price < 0 || buyAmount <= 0 || totalPrice > int.MaxValue)
        {
            Debug.LogWarning("[상점] 구매 가격 또는 수량이 올바르지 않습니다.");
            return;
        }
        int price = (int)totalPrice;

        if (!wallet.TrySpendMoney(price))
        {
            Debug.LogWarning("[상점] 소지금이 부족합니다.");
            UpdateButtonState();
            return;
        }

        // ★ Item_count++ 가 아니라 진짜 인벤토리에 넣는다
        int got = ShopInventoryBridge.Buy(currentItem, buyAmount);
        if (got < buyAmount)
        {
            // 실제로 받지 못한 수량만 환불한다.
            wallet.AddMoney(currentItem.Item_price * (buyAmount - got));
        }
        if (got <= 0)
        {
            Debug.LogWarning("[상점] 아이템을 넣지 못해 구매 금액을 환불했습니다. 매핑과 인벤토리 공간을 확인하세요.");
            UpdateButtonState();
            return;
        }

        Debug.Log($"[상점] {currentItem.Item_name} {got}개 구매 · {currentItem.Item_price * got}G " +
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
