using TMPro;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class MoneyManger : MonoBehaviour
{
    [SerializeField] private TMP_Text moneytext;
    private int totalMoney = 10000;

    private void Awake()
    {
        moneytext.text = $"{totalMoney}$";  
    }


    public void BuyItem(ShopBT shopBT)
    {
        int price = shopBT.Price;

        if (totalMoney - price < 0)
        {
            Debug.Log("돈없음");
            return;
        }


        totalMoney -= price;
        moneytext.text = $"{totalMoney}$";
    }

    public void SellPlant()
    {

    }
}
