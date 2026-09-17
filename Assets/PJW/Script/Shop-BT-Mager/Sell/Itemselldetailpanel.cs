using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Itemselldetailpanel : MonoBehaviour
{
    [Header("UI 정보 컴포넌트")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI explanationText;
    [SerializeField] private Image iconImage;

    [Tooltip("보유 수량 표시 (선택)")]
    [SerializeField] private TextMeshProUGUI countText;

    [Header("판매 버튼")]
    [SerializeField] private Button sellButton;

    [Header("지갑")]
    [Tooltip("비우면 PlayerWallet.Instance 를 쓴다")]
    [SerializeField] private PlayerWallet playerWalletcomp;

    [Header("설정")]
    [Tooltip("한 번 클릭에 몇 개를 팔지")]
    [SerializeField, Min(1)] private int sellAmount = 1;

    [Tooltip("켜면 버튼 한 번에 보유량 전부를 판다")]
    [SerializeField] private bool sellAll;

    // ★ 친구 쪽 Item 이다. 내 ItemSO 가 아니다
    private Item currentItem;
    private System.Action onSellCallback;

    private void Start()
    {
        if (sellButton != null) sellButton.onClick.AddListener(OnClickSell);
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (sellButton != null) sellButton.onClick.RemoveListener(OnClickSell);
    }

    public void ShowDetail(Item item, System.Action refreshCallback = null)
    {
        if (item == null)
        {
            HideDetail();
            return;
        }

        currentItem = item;
        onSellCallback = refreshCallback;
        gameObject.SetActive(true);

        if (nameText != null) nameText.text = item.Item_name;
        if (explanationText != null) explanationText.text = item.item_explanation;
        if (iconImage != null && item.Item_icon != null) iconImage.sprite = item.Item_icon;

        Refresh();
    }

    /// <summary>수량·가격·버튼 상태를 인벤토리 기준으로 다시 그린다</summary>
    private void Refresh()
    {
        if (currentItem == null) return;

        // ★ Item_count 가 아니라 다리에게 직접 물어본다.
        //   Item_count 는 SyncCounts 가 돌아야만 최신이라 타이밍을 탄다
        int have = ShopInventoryBridge.CountOf(currentItem);

        if (countText != null) countText.text = $"보유 {have}개";

        // 지금 팔면 얼마 받는지. 품질이 섞여 있으면 그것까지 반영된다
        int gold = have > 0 ? ShopInventoryBridge.PreviewSellValue(currentItem, AmountToSell(have)) : 0;

        if (priceText != null) priceText.text = $"{gold:#,##0} G";
    }

    private int AmountToSell(int have)
    {
        if (have <= 0) return 0;

        return sellAll ? have : Mathf.Min(sellAmount, have);
    }

    // [판매하기] 버튼 클릭 시 동작
    private void OnClickSell()
    {
        if (currentItem == null) return;

        int have = ShopInventoryBridge.CountOf(currentItem);
        int want = AmountToSell(have);

        if (want <= 0) return;

        // 인벤토리에서 빼고 금액을 받아온다.
        // sold 가 요청보다 적을 수 있으니 반드시 이 값으로 정산한다
        int sold = ShopInventoryBridge.Sell(currentItem, want, out int gold);

        if (sold <= 0)
        {
            Debug.LogWarning($"[상점] {currentItem.Item_name} 을(를) 팔지 못했습니다. 인벤토리에 없습니다.");
            return;
        }

        // ★ 돈은 한 번만 준다
        PlayerWallet wallet = playerWalletcomp != null ? playerWalletcomp : PlayerWallet.Instance;

        if (wallet != null) wallet.AddMoney(gold);
        else Debug.LogError("[상점] PlayerWallet 이 없습니다. 판매는 됐는데 돈이 안 들어갔습니다.");

        Debug.Log($"[상점] {currentItem.Item_name} {sold}개 판매 · {gold}G " +
                  $"(남은 수량 {ShopInventoryBridge.CountOf(currentItem)}개)");

        Refresh();

        // ★ 목록 갱신도 한 번만
        onSellCallback?.Invoke();

        // 다 팔아서 목록에서 빠질 아이템이면 상세창을 닫는다
        if (ShopInventoryBridge.CountOf(currentItem) <= 0) HideDetail();
    }

    public void HideDetail()
    {
        currentItem = null;
        gameObject.SetActive(false);
    }
}
