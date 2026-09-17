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

    [Header("판매 버튼")]
    [SerializeField] private Button sellButton;

    [Header("지갑")]
    [SerializeField] private PlayerWallet playerWalletcomp;


    private ItemSO currentItem;
    private System.Action onSellCallback;

    //private void Start()
    //{
    //    if (sellButton != null) sellButton.onClick.AddListener(OnClickSell);
    //    gameObject.SetActive(false);
    //}

    public void ShowDetail(ItemSO item, System.Action refreshCallback = null)
    {
        if (item == null)
        {
            HideDetail();
            return;
        }

        currentItem = item;
        onSellCallback = refreshCallback;
        gameObject.SetActive(true);

        if (nameText != null) nameText.text = item.displayName;
        if (priceText != null) priceText.text = $"{item.sellPrice:#,##0} G";
        if (explanationText != null) explanationText.text = item.description;
        if (iconImage != null && item.icon != null) iconImage.sprite = item.icon;

        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        if (currentItem == null || sellButton == null) return;

        // 구매와 달리 판매는 보유 수량이 있을 때만 가능
    //    sellButton.interactable = currentItem.Item_count > 0;
    //}

    //// [판매하기] 버튼 클릭 시 동작
    //private void OnClickSell()
    //{
    //    if (currentItem == null || currentItem.Item_count <= 0) return;

    //    // 보유 수량 1 감소
    //    int sold = ShopInventoryBridge.Sell(currentItem, currentItem.Item_count, out int gold);

    //    if (sold <= 0) return;          // 실제로 없었음
    //    playerWalletcomp.AddMoney(gold);         // 친구 쪽 돈 처리
    //    onSellCallback?.Invoke();      // 목록 다시 그리기

    //    // 소지금에 판매가 지급
    //    if (PlayerWallet.Instance != null)
    //    {
    //        PlayerWallet.Instance.AddMoney(currentItem.Item_SellPrice);

    //        // 버튼 상태 및 UI 업데이트
    //        UpdateButtonState();
    //        onSellCallback?.Invoke();

    //        Debug.Log($"{currentItem.Item_name} 판매 성공! (현재 보유량: {currentItem.Item_count}개)");
    //    }
        else
        {
            Debug.LogWarning("Wallet이 없습니다!");
        }
    }

    public void HideDetail()
    {
        gameObject.SetActive(false);
    }
}
