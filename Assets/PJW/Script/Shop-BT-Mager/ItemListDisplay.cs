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
    }

    // [구매하기] 버튼 클릭 시 동작 (돈이 있는 한 계속 구매)
    private void OnClickBuy()
    {
        if (currentItem == null) return;

        // 플레이어 소지금 차감 후 수량 증가
        if (PlayerWallet.Instance != null && PlayerWallet.Instance.TrySpendMoney(currentItem.Item_price))
        {
            currentItem.Item_count++; // 아이템 보유 수량 증가
            onPurchaseCallback?.Invoke(); // UI 갱신 필요한 곳에 알림

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
