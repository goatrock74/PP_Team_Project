using UnityEngine;

public class GoldManger : MonoBehaviour
{
    private int moneys = 100;

    public void Buy(int value)
    {
        if (moneys - value < 0) return;

        moneys -= value;
        Debug.Log("상품샀당");
    }
}
