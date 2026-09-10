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
    [SerializeField] private TextMeshProUGUI buyButtonText; // [선택] 보유 수량을 같이 보여줄 텍스트

    private Item currentItem;
    private System.Action onPurchaseCallback;

    private void Start()
    {
        if (buyButton != null) buyButton.onClick.AddListener(OnClickBuy);
        gameObject.SetActive(false);
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
        if (priceText != null) priceText.text = $"{item.Item_price:#,##0} G";
        if (explanationText != null) explanationText.text = item.item_explanation;
        if (iconImage != null && item.Item_icon != null) iconImage.sprite = item.Item_icon;

        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        if (currentItem == null || buyButton == null) return;

        // 다회 구매 가능하므로 구매 버튼은 항상 클릭 가능 상태 유지
        buyButton.interactable = true;

        // [선택] 구매 버튼 텍스트에 보유 수량 표시 (예: "구매하기 (보유: 3개)")
        if (buyButtonText != null)
        {
            buyButtonText.text = $"구매하기 (보유: {currentItem.Item_count}개)";
        }
    }

    // [구매하기] 버튼 클릭 시 동작 (여러 번 클릭 가능)
    private void OnClickBuy()
    {
        if (currentItem == null) return;

        // 플레이어 소지금이 충분한지 확인 후 차감
        if (PlayerWallet.Instance != null && PlayerWallet.Instance.TrySpendMoney(currentItem.Item_price))
        {
            // 수량 1 증가
            currentItem.Item_count++;

            // 버튼 텍스트 및 UI 업데이트
            UpdateButtonState();
            onPurchaseCallback?.Invoke();

            Debug.Log($"{currentItem.Item_name} 구매 성공! (현재 보유량: {currentItem.Item_count}개)");
        }
        else
        {
            Debug.LogWarning("소지금이 부족하거나 Wallet이 없습니다!");
        }
    }

    public void HideDetail()
    {
        gameObject.SetActive(false);
    }
}
