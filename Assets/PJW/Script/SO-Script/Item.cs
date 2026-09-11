using System;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "new ItemSO", menuName = "PJW/Item/ItemSO")]
public class Item : ScriptableObject
{
    [field: SerializeField] public SeasonType Season { get; private set; }

    [field: SerializeField] public Sprite Item_icon { get; private set; }

    [field: SerializeField] public int Item_price { get; private set; }

    [Header("판매 가격 (0으로 두면 구매가의 절반으로 자동 계산)")]
    [SerializeField] private int item_sellPrice = 0;

    // 실제 판매 시 사용하는 가격. 인스펙터에 값을 안 넣으면 구매가의 절반으로 자동 계산됨
    public int Item_SellPrice => item_sellPrice > 0 ? item_sellPrice : Item_price / 2;

    [field: SerializeField] public string Item_name { get; private set; }

    [TextArea][field: SerializeField] public string item_explanation { get; private set; }

    public int Item_count = 0;
}

public enum SeasonType
{
    Autumn,
    Summer,
    Spring,
    Winter
}