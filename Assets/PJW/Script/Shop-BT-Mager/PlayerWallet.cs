using TMPro;
using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet Instance { get; private set; }

    [Header("플레이어 소지금")]
    [SerializeField] private int currentMoney = 10000;

    [Header("UI 텍스트 (선택사항)")]
    [SerializeField] private TextMeshProUGUI moneyText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start() => UpdateMoneyUI();

    // 구매 시 돈 차감 (부족하면 false 리턴)
    public bool TrySpendMoney(int amount)
    {
        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            UpdateMoneyUI();
            return true;
        }
        return false;
    }

    private void UpdateMoneyUI()
    {
        if (moneyText != null)
            moneyText.text = $"{currentMoney:#,##0} G";
    }
}
