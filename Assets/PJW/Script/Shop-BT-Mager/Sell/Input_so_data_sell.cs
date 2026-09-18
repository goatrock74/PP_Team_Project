    using Assets.PJW.Script.SO_Script;
using System.Collections.Generic;
using UnityEngine;

public class Input_so_data_sell : MonoBehaviour
{
    [Header("계절별 ItemListSO 에셋 배열 (0:봄, 1:여름, 2:가을, 3:겨울)")]
    [SerializeField] private ItemListSO[] itemListSO;

    [Header("UI 연결")]
    [SerializeField] private List<Plant_sell_bt> sellButtons;
    [SerializeField] private Itemselldetailpanel detailPanel;


    private void Start()
    {
        RefreshSellUI();
    }

    private void OnEnable()
    {
        RefreshSellUI();
    }

    public void RefreshSellUI()
    {
        // ★ 인벤토리의 실제 수량을 Item_count 로 복사해온다.
        //   이게 있어야 아래 보유 판정이 진짜 소지품을 본다
        ShopInventoryBridge.SyncCounts();

        if (itemListSO == null   || itemListSO.Length == 0)
        {
            Debug.LogError("[Input_SO_Data_Sell] itemListSO 배열이 할당되지 않았습니다!");
            return;
        }

        if (sellButtons == null || sellButtons.Count == 0)
        {
            Debug.LogError("[Input_SO_Data_Sell] sellButtons 리스트가 연결되지 않았습니다!");
            return;
        }

        // 4계절 SO를 전부 훑어서 보유 중인 아이템만 모으기
        List<Item> ownedItems = new List<Item>();

        foreach (ItemListSO seasonSO in itemListSO)
        {
            if (seasonSO == null || seasonSO.ItemList == null) continue;

            foreach (Item item in seasonSO.ItemList)
            {
                if (item == null) continue;
                if (ownedItems.Contains(item)) continue;   // 여러 계절에 겹쳐 있어도 한 번만

                if (ShopInventoryBridge.CountOf(item) > 0) ownedItems.Add(item);
            }
        }

        for (int i = 0; i < sellButtons.Count; i++)
        {
            if (sellButtons[i] == null) continue;

            bool hasItem = i < ownedItems.Count;

            // this(owner)를 같이 넘겨서, 판매 후 버튼이 전체 목록을 다시 갱신할 수 있게 함
            sellButtons[i].SetItem(hasItem ? ownedItems[i] : null, detailPanel, this);
        }
    }
}
