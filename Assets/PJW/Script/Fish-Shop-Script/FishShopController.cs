using System.Collections.Generic;
using KSM._00.Scripts.Items;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class FishShopController : MonoBehaviour
{
    [SerializeField] private FishShopCatalog catalog;
    [SerializeField] private UIManger menu;
    [SerializeField] private FishShopSlot[] buySlots;
    [SerializeField] private FishShopSlot[] sellSlots;
    [SerializeField] private FishShopDetailPanel buyDetail;
    [SerializeField] private FishShopDetailPanel sellDetail;
    private readonly List<FishShopSlot> sellPool = new List<FishShopSlot>();
    private PlayerWallet wallet;
    private PlayerInventory subscribedInventory;
    private bool needsRefresh = true;
    private FishShopCatalog.Product selectedProduct;
    private FishDataSO selectedFish;
    private ItemQuality selectedQuality;
    private FishShopSlot selectedSlot;

    private bool Ready => PlayerInventory.Instance != null &&
        Inventorysavemanger.Instance != null && Inventorysavemanger.Instance.IsReady;

    private void Awake()
    {
        if (PlayerInventory.Instance == null) new GameObject("Shop Player Inventory").AddComponent<PlayerInventory>();
        wallet = PlayerWallet.Instance;
        wallet.OnMoneyChanged += OnMoneyChanged;
        buyDetail.Bind(PurchaseSelected);
        sellDetail.Bind(SellSelected);
        for (int i = 0; i < buySlots.Length; i++)
        {
            var slot = buySlots[i];
            if (i >= catalog.products.Length) { slot.gameObject.SetActive(false); continue; }
            var product = catalog.products[i];
            slot.Bind(product.icon, () => SelectProduct(product, slot));
        }
        sellPool.AddRange(sellSlots);
        foreach (var slot in sellPool) slot.gameObject.SetActive(false);
        BackToMenu();
    }

    private void Update()
    {
        if (Ready && subscribedInventory == null)
        {
            subscribedInventory = PlayerInventory.Instance;
            subscribedInventory.Bag.OnChanged += MarkDirty;
            subscribedInventory.Hotbar.OnChanged += MarkDirty;
            needsRefresh = true;
        }
        if (!needsRefresh) return;
        needsRefresh = false;
        RefreshSellSlots();
        RefreshDetail();
    }

    private void MarkDirty() => needsRefresh = true;
    private void OnMoneyChanged(int amount) => MarkDirty();
    private void OnDestroy()
    {
        if (wallet != null) wallet.OnMoneyChanged -= OnMoneyChanged;
        if (subscribedInventory == null) return;
        subscribedInventory.Bag.OnChanged -= MarkDirty;
        subscribedInventory.Hotbar.OnChanged -= MarkDirty;
    }

    public void OpenBuyPanel() { ClearSelection(); menu.OpenBuyPanel(); MarkDirty(); }
    public void OpenSellPanel() { ClearSelection(); menu.OpenSellPanel(); MarkDirty(); }
    public void BackToMenu() { ClearSelection(); menu.BackToMenu(); }

    private void ClearSelection()
    {
        if (selectedSlot != null) selectedSlot.SetSelected(false);
        selectedSlot = null;
        selectedProduct = null;
        selectedFish = null;
        buyDetail.HideDetail();
        sellDetail.HideDetail();
    }

    private void SelectProduct(FishShopCatalog.Product product, FishShopSlot slot)
    {
        bool toggleOff = selectedSlot == slot;
        ClearSelection();
        if (toggleOff) return;
        selectedProduct = product;
        selectedSlot = slot;
        slot.SetSelected(true);
        RefreshDetail();
    }

    private void SelectFish(FishDataSO fish, ItemQuality quality, FishShopSlot slot)
    {
        bool toggleOff = selectedSlot == slot;
        ClearSelection();
        if (toggleOff) return;
        selectedFish = fish;
        selectedQuality = quality;
        selectedSlot = slot;
        slot.SetSelected(true);
        RefreshDetail();
    }

    private int Count(FishDataSO fish, ItemQuality quality) => Ready ?
        PlayerInventory.Instance.Bag.CountOf(fish, quality) + PlayerInventory.Instance.Hotbar.CountOf(fish, quality) : 0;

    private void RefreshSellSlots()
    {
        int index = 0;
        foreach (var fish in catalog.fish)
        foreach (var quality in new[] { ItemQuality.Normal, ItemQuality.Good, ItemQuality.Best })
        {
            if (Count(fish, quality) == 0) continue;
            if (index >= sellPool.Count)
                sellPool.Add(Instantiate(sellSlots[0], sellSlots[0].transform.parent));
            var slot = sellPool[index++];
            slot.Bind(fish.icon, () => SelectFish(fish, quality, slot));
            bool selected = fish == selectedFish && quality == selectedQuality;
            slot.SetSelected(selected);
            if (selected) selectedSlot = slot;
        }
        for (; index < sellPool.Count; index++) sellPool[index].gameObject.SetActive(false);
        if (selectedFish != null && Count(selectedFish, selectedQuality) == 0) ClearSelection();
    }

    private void RefreshDetail()
    {
        if (selectedProduct != null)
        {
            var product = selectedProduct;
            bool owned = product.purchaseLimit == FishShopPurchaseLimit.Once &&
                (PlayerPrefs.GetInt(product.PurchaseKey, 0) != 0 || (Ready && PlayerInventory.Instance.CountOf(product.item) > 0));
            bool room = Ready && PlayerInventory.Instance.CanAccept(product.item, 1);
            bool affordable = wallet.CurrentMoney >= product.price;
            string description = string.IsNullOrWhiteSpace(product.description) ? product.item.description : product.description;
            buyDetail.Show(product.title, product.price, description, product.icon,
                Ready ? PlayerInventory.Instance.CountOf(product.item) : 0,
                "구매",
                Ready && !owned && room && affordable);
        }
        if (selectedFish != null)
        {
            int count = Count(selectedFish, selectedQuality);
            int price = catalog.SellPrice(selectedFish, selectedQuality);
            string description = string.IsNullOrWhiteSpace(selectedFish.description) ?
                (selectedFish.collectionCategory == FishCollectionCategory.Sea ? "바다에서 잡을 수 있는 물고기입니다." : "민물에서 잡을 수 있는 물고기입니다.") : selectedFish.description;
            sellDetail.Show(selectedFish.DisplayName + " (" + ItemQualityUtil.DisplayName(selectedQuality) + ")",
                price, description, selectedFish.icon, count, "판매",
                Ready && count > 0 && price > 0 && wallet.CurrentMoney <= int.MaxValue - price);
        }
    }

    private void PurchaseSelected()
    {
        Buy(selectedProduct);
        MarkDirty();
        RefreshDetail();
    }

    private void SellSelected()
    {
        Sell(selectedFish, selectedQuality);
        MarkDirty();
    }

    public bool Buy(FishShopCatalog.Product product)
    {
        if (!Ready || product == null || product.item == null || product.price < 0) return false;
        var inventory = PlayerInventory.Instance;
        if (product.purchaseLimit == FishShopPurchaseLimit.Once &&
            (PlayerPrefs.GetInt(product.PurchaseKey, 0) != 0 || inventory.CountOf(product.item) > 0))
            return false;
        if (!inventory.CanAccept(product.item, 1)) return false;
        if (!wallet.TrySpendMoney(product.price)) return false;
        if (inventory.Add(product.item, 1) != 0)
        {
            wallet.AddMoney(product.price);
            return false;
        }
        if (product.purchaseLimit == FishShopPurchaseLimit.Once) PlayerPrefs.SetInt(product.PurchaseKey, 1);
        Inventorysavemanger.Instance.SaveInventory();
        PlayerPrefs.Save();
        return true;
    }

    public bool Sell(FishDataSO fish, ItemQuality quality)
    {
        if (!Ready || fish == null || System.Array.IndexOf(catalog.fish, fish) < 0) return false;
        int price = catalog.SellPrice(fish, quality);
        if (price <= 0 || wallet.CurrentMoney > int.MaxValue - price) return false;
        if (PlayerInventory.Instance.Remove(fish, quality, 1) != 1) return false;
        wallet.AddMoney(price);
        Inventorysavemanger.Instance.SaveInventory();
        return true;
    }

}
