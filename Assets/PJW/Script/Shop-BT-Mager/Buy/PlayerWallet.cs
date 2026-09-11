using TMPro;
using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet Instance { get; private set; }

    [Header("플레이어 소지금")]
    [SerializeField] private int currentMoney = 10000;
    public int CurrentMoney => currentMoney;

    [Header("UI 텍스트 (선택사항 - 이 오브젝트와 같은 씬에서만 쓸 거면 연결)")]
    [SerializeField] private TextMeshProUGUI moneyText;

    // 다른 씬에서도 각자의 UI Text가 소지금을 표시하고 싶을 때 구독해서 쓰는 이벤트
    // (필요 없으면 그냥 무시해도 됨 - moneyText 하나만 써도 충분함)
    public event System.Action<int> OnMoneyChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 이 오브젝트는 파괴되지 않음
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
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

    // 판매 시 돈 지급
    public void AddMoney(int amount)
    {
        if (amount <= 0) return;

        currentMoney += amount;
        UpdateMoneyUI();
    }

    private void UpdateMoneyUI()
    {
        if (moneyText != null)
            moneyText.text = $"{currentMoney:#,##0} G";

        OnMoneyChanged?.Invoke(currentMoney);
    }
}
