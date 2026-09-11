using TMPro;
using UnityEngine;

public class Walletudisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText;

    private void OnEnable()
    {
        if (PlayerWallet.Instance != null)
        {
            PlayerWallet.Instance.OnMoneyChanged += UpdateText;
            UpdateText(PlayerWallet.Instance.CurrentMoney); // 켜지자마자 현재 값으로 바로 표시
        }
    }

    private void OnDisable()
    {
        if (PlayerWallet.Instance != null)
        {
            PlayerWallet.Instance.OnMoneyChanged -= UpdateText;
        }
    }

    private void UpdateText(int amount)
    {
        if (moneyText != null)
            moneyText.text = $"{amount:#,##0} G";
    }
}
