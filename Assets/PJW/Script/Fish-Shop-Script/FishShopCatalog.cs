using System;
using UnityEngine;

public enum FishShopPurchaseLimit { Repeatable, Once }

[CreateAssetMenu(menuName = "Shop/Fish Shop Catalog")]
public class FishShopCatalog : ScriptableObject
{
    [Serializable]
    public class Product
    {
        public ItemSO item;
        public string title;
        [TextArea(2, 5)] public string description;
        public Sprite icon;
        [Min(0)] public int price;
        public FishShopPurchaseLimit purchaseLimit;
        public string PurchaseKey => "FishShop.Purchased." + item.ItemId;
    }

    public Product[] products;
    public FishDataSO[] fish;
    [Tooltip("FishDataSO의 판매가가 0일 때 사용할 일반 등급 판매가")]
    [Min(1)] public int defaultFishSellPrice = 100;

    public int SellPrice(FishDataSO item, ItemQuality quality)
    {
        return item.sellPrice > 0 ? item.GetSellPrice(quality) :
            Mathf.RoundToInt(defaultFishSellPrice * ItemQualityUtil.PriceMultiplier(quality));
    }
}
