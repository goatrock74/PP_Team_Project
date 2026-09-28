using Assets.PJW.Script.SO_Script;
using System.Collections.Generic;
using UnityEngine;

public class Input_SO_Data : MonoBehaviour
{
    [Header("계절별 ItemListSO (순서 무관, SO의 Target Type 기준)")]
    [SerializeField] private ItemListSO[] itemListSO;
    [Header("계절과 관계없이 판매할 목록 (연결하면 계절 목록 대신 사용)")]
    [SerializeField] private ItemListSO allSeasonItems;
    [Header("암상인: 고정 상품 + 매일 무작위 상품")]
    [SerializeField] private Item guaranteedItem;
    [SerializeField] private int guaranteedPrice = 9999;
    [SerializeField, Min(0)] private int randomItemCount = 5;
    private Item guaranteedOffer;

    private Item[] GetDailyStock(Item[] pool)
    {
        if (guaranteedItem == null) return pool;
        if (guaranteedOffer == null) guaranteedOffer = guaranteedItem.CreateOffer(guaranteedPrice);
        var candidates = new List<Item>();
        if (pool != null)
            foreach (Item item in pool)
                if (item != null && item != guaranteedItem && !candidates.Contains(item)) candidates.Add(item);
        // Stable across reopening and loading the same save; do not alter Unity's global RNG.
        int seed = timeManager != null ? timeManager.CurrentDay : 1;
        foreach (char c in SaveSlotStore.Active?.id ?? "default") seed = unchecked(seed * 31 + c);
        var random = new System.Random(seed);
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            Item temp = candidates[i]; candidates[i] = candidates[j]; candidates[j] = temp;
        }
        var stock = new List<Item> { guaranteedOffer };
        stock.AddRange(candidates.GetRange(0, Mathf.Min(Mathf.Max(0, randomItemCount), candidates.Count)));
        return stock.ToArray();
    }

    private void OnDestroy()
    {
        if (guaranteedOffer != null) Destroy(guaranteedOffer);
    }

    [Header("UI 연결")]
    [SerializeField] private List<Plant_Shop_BT> shopButtons;
    [SerializeField] private ItemDetailPanel detailPanel;

    private TimeManager timeManager;
    private int currentSeasonIndex = 0;
    public int CurrentSeasonIndex => currentSeasonIndex;
    public ItemListSO GetSeasonList(int seasonIndex)
    {
        SeasonType season = seasonIndex switch
        {
            0 => SeasonType.Spring, 1 => SeasonType.Summer,
            2 => SeasonType.Autumn, _ => SeasonType.Winter,
        };
        if (seasonIndex < 0 || seasonIndex > 3 || itemListSO == null) return null;
        foreach (ItemListSO list in itemListSO)
            if (list != null && list.TargetSeason == season) return list;
        return null;
    }

    private void OnEnable()
    {
        timeManager = TimeManager.Instance;

        if (timeManager == null)
        {
            RefreshShopUI(currentSeasonIndex);
            return;
        }

        timeManager.OnSeasonChange += UpdateShopBySeason;

        // 상점에 들어오기 전에 바뀐 계절도 즉시 반영한다.
        UpdateShopBySeason(timeManager.CurrentSeason);
    }

    private void OnDisable()
    {
        if (timeManager != null)
        {
            timeManager.OnSeasonChange -= UpdateShopBySeason;
        }

        timeManager = null;
    }

    /// <summary>계절 변경 이벤트 또는 외부 코드에서 호출하는 상점 갱신 함수.</summary>
    public void UpdateShopBySeason(TimeManager.SeasonPeriod season)
    {
        int index = season switch
        {
            TimeManager.SeasonPeriod.Spring => 0,
            TimeManager.SeasonPeriod.Summer => 1,
            TimeManager.SeasonPeriod.Autumn => 2,
            TimeManager.SeasonPeriod.Winter => 3,
            _ => -1,
        };
        if (index >= 0) RefreshShopUI(index);
    }

    public void SetItemListSO(ItemListSO[] newSOArray, bool refreshImmediate = true)
    {
        if (newSOArray == null || newSOArray.Length == 0) return;

        itemListSO = newSOArray;

        if (refreshImmediate) RefreshShopUI(currentSeasonIndex);
    }

    public void SetSingleSeasonSO(int seasonIndex, ItemListSO newSO, bool refreshImmediate = true)
    {
        if (newSO == null || seasonIndex < 0 || seasonIndex > 3) return;
        var lists = new List<ItemListSO>(itemListSO ?? System.Array.Empty<ItemListSO>());
        int index = lists.IndexOf(GetSeasonList(seasonIndex));
        if (index >= 0) lists[index] = newSO;
        else lists.Add(newSO);
        itemListSO = lists.ToArray();

        if (refreshImmediate && currentSeasonIndex == seasonIndex) RefreshShopUI(currentSeasonIndex);
    }

    public void RefreshShopUI(int seasonIndex)
    {
        if (allSeasonItems == null && (itemListSO == null || itemListSO.Length == 0))
        {
            Debug.LogError("[Input_SO_Data] itemListSO 배열이 할당되지 않았습니다!");
            return;
        }

        if (seasonIndex < 0 || seasonIndex > 3)
        {
            Debug.LogError($"[Input_SO_Data] seasonIndex 범위 초과! (입력값: {seasonIndex})");
            return;
        }

        // ★ 이 줄이 주석 처리되어 있어서 계절 전환이 안 먹었다
        currentSeasonIndex = seasonIndex;

        ItemListSO seasonList = allSeasonItems != null ? allSeasonItems : GetSeasonList(seasonIndex);
        if (seasonList == null)
        {
            Debug.LogWarning($"[Input_SO_Data] {currentSeasonIndex}번 계절의 SO가 없습니다. 목록을 비웁니다.");
        }

        if (shopButtons == null || shopButtons.Count == 0)
        {
            Debug.LogError("[Input_SO_Data] shopButtons 리스트가 연결되지 않았습니다!");
            return;
        }

        Item[] currentItems = GetDailyStock(seasonList != null ? seasonList.ItemList : null);
        // Clone the configured slot so its icon, selection border and click event stay connected.
        if (allSeasonItems != null && currentItems != null)
        {
            Plant_Shop_BT template = shopButtons.Find(button => button != null);
            if (template != null)
                while (shopButtons.Count < currentItems.Length)
                {
                    Plant_Shop_BT slot = Instantiate(template, template.transform.parent);
                    slot.name = "Seed Slot " + (shopButtons.Count + 1);
                    shopButtons.Add(slot);
                }
        }

        if (currentItems == null || currentItems.Length == 0)
            Debug.LogWarning($"[Input_SO_Data] {currentSeasonIndex}번 계절의 ItemList가 비어있습니다.");

        // 이전 계절에서 선택한 상품을 상세창에 남겨두지 않는다.
        if (detailPanel != null) detailPanel.HideDetail();

        for (int i = 0; i < shopButtons.Count; i++)
        {
            if (shopButtons[i] == null) continue;

            shopButtons[i].SettingSelectBT();
            bool hasItem = currentItems != null && i < currentItems.Length;

            shopButtons[i].SetItem(hasItem ? currentItems[i] : null, detailPanel);
        }
    }
}
