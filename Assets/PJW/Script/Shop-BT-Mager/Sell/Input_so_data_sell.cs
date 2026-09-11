    using Assets.PJW.Script.SO_Script;
using System.Collections.Generic;
using UnityEngine;

public class Input_so_data_sell : MonoBehaviour
{
    [Header("계절별 ItemListSO 에셋 배열 (0:봄, 1:여름, 2:가을, 3:겨울) - Input_SO_Data와 동일한 에셋 4개 할당")]
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
        // 판매창이 열릴 때마다 최신 보유 수량 기준으로 다시 그림
        RefreshSellUI();
    }

    public void RefreshSellUI()
    {
        if (itemListSO == null || itemListSO.Length == 0)
        {
            Debug.LogError("[Input_SO_Data_Sell] itemListSO 배열이 할당되지 않았습니다!");
            return;
        }

        // 4계절 SO를 전부 훑어서 보유 중인 아이템만 모으기
        List<Item> ownedItems = new List<Item>();

        foreach (var seasonSO in itemListSO)
        {
            if (seasonSO == null || seasonSO.ItemList == null) continue;

            foreach (var item in seasonSO.ItemList)
            {
                if (item != null && item.Item_count > 0)
                {
                    ownedItems.Add(item);
                }
            }
        }

        if (sellButtons == null || sellButtons.Count == 0)
        {
            Debug.LogError("[Input_SO_Data_Sell] sellButtons 리스트가 연결되지 않았습니다!");
            return;
        }

        for (int i = 0; i < sellButtons.Count; i++)
        {
            if (sellButtons[i] == null) continue;

            if (i < ownedItems.Count)
            {
                // this(owner)를 같이 넘겨서, 판매 후 버튼이 전체 목록을 다시 갱신할 수 있게 함
                sellButtons[i].SetItem(ownedItems[i], detailPanel, this);
            }
            else
            {
                sellButtons[i].SetItem(null, detailPanel, this);
            }
        }
    }
}
