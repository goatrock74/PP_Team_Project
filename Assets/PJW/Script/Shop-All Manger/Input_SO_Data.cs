    using Assets.PJW.Script.SO_Script;
using System.Collections.Generic;
using UnityEngine;

public class Input_SO_Data : MonoBehaviour
{
    [Header("계절별 ItemListSO 에셋 배열 (0:봄, 1:여름, 2:가을, 3:겨울)")]
    [SerializeField] private ItemListSO[] itemListSO;

    [Header("UI 연결")]
    [SerializeField] private List<Plant_Shop_BT> shopButtons;
    [SerializeField] private ItemDetailPanel detailPanel;

    private int currentSeasonIndex = 0;

    private void Start()
    {
        RefreshShopUI(0);
    }

    private void OnEnable()
    {
        RefreshShopUI(currentSeasonIndex);
    }

    public void SetItemListSO(ItemListSO[] newSOArray, bool refreshImmediate = true)
    {
        if (newSOArray == null || newSOArray.Length == 0) return;

        itemListSO = newSOArray;

        if (refreshImmediate) RefreshShopUI(currentSeasonIndex);
    }

    public void SetSingleSeasonSO(int seasonIndex, ItemListSO newSO, bool refreshImmediate = true)
    {
        if (itemListSO == null || seasonIndex < 0 || seasonIndex >= itemListSO.Length) return;

        itemListSO[seasonIndex] = newSO;

        if (refreshImmediate && currentSeasonIndex == seasonIndex) RefreshShopUI(currentSeasonIndex);
    }

    public void RefreshShopUI(int seasonIndex)
    {
        if (itemListSO == null || itemListSO.Length == 0)
        {
            Debug.LogError("[Input_SO_Data] itemListSO 배열이 할당되지 않았습니다!");
            return;
        }

        if (seasonIndex < 0 || seasonIndex >= itemListSO.Length)
        {
            Debug.LogError($"[Input_SO_Data] seasonIndex 범위 초과! (입력값: {seasonIndex})");
            return;
        }

        // ★ 이 줄이 주석 처리되어 있어서 계절 전환이 안 먹었다
        currentSeasonIndex = seasonIndex;

        if (itemListSO[currentSeasonIndex] == null)
        {
            Debug.LogError($"[Input_SO_Data] {currentSeasonIndex}번 인덱스의 ItemListSO 에셋이 Null입니다!");
            return;
        }

        if (shopButtons == null || shopButtons.Count == 0)
        {
            Debug.LogError("[Input_SO_Data] shopButtons 리스트가 연결되지 않았습니다!");
            return;
        }

        Item[] currentItems = itemListSO[currentSeasonIndex].ItemList;

        if (currentItems == null || currentItems.Length == 0)
            Debug.LogWarning($"[Input_SO_Data] {currentSeasonIndex}번 계절의 ItemList가 비어있습니다.");

        for (int i = 0; i < shopButtons.Count; i++)
        {
            if (shopButtons[i] == null) continue;

            bool hasItem = currentItems != null && i < currentItems.Length;

            shopButtons[i].SetItem(hasItem ? currentItems[i] : null, detailPanel);
        }
    }
}
