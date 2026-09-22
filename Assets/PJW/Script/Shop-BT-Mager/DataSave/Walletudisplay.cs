using TMPro;
using UnityEngine;

public class Walletudisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText;
    private PlayerWallet wallet;

    public void SetTextTarget(TextMeshProUGUI text)
    {
        moneyText = text;
        Bind();
        UpdateText(wallet.CurrentMoney);
    }

    private void OnEnable()
    {
        Bind();
    }

    private void Bind()
    {
        if (moneyText == null) moneyText = GetComponent<TextMeshProUGUI>();
        if (wallet != null) return;
        wallet = PlayerWallet.Instance;
        if (wallet != null)
        {
            wallet.OnMoneyChanged += UpdateText;
            UpdateText(wallet.CurrentMoney);
        }
    }

    private void OnDisable()
    {
        if (wallet != null)
        {
            wallet.OnMoneyChanged -= UpdateText;
        }
        wallet = null;
    }

    private void UpdateText(int amount)
    {
        if (moneyText != null)
            moneyText.text = $"{amount:#,##0} G";
    }
}
