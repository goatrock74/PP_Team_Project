using Assets.PJW.Script.SO_Script;
using System.Collections.Generic;
using UnityEngine;

public class Input_SO_Data : MonoBehaviour
{
    [Header("계절별 ItemListSO (순서 무관, SO의 Target Type 기준)")]
    [SerializeField] private ItemListSO[] itemListSO;

    [Header("UI 연결")]
    [SerializeField] private List<Plant_Shop_BT> shopButtons;
    [SerializeField] private ItemDetailPanel detailPanel;

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

    private void Start()
    {
        RefreshShopUI(currentSeasonIndex);
    }

    private void OnEnable()
    {
        // 외부에서 마지막으로 전달받은 계절을 유지한다.
        RefreshShopUI(currentSeasonIndex);
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
        if (itemListSO == null || itemListSO.Length == 0)
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

        ItemListSO seasonList = GetSeasonList(seasonIndex);
        if (seasonList == null)
        {
            Debug.LogWarning($"[Input_SO_Data] {currentSeasonIndex}번 계절의 SO가 없습니다. 목록을 비웁니다.");
        }

        if (shopButtons == null || shopButtons.Count == 0)
        {
            Debug.LogError("[Input_SO_Data] shopButtons 리스트가 연결되지 않았습니다!");
            return;
        }

        Item[] currentItems = seasonList != null ? seasonList.ItemList : null;

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
