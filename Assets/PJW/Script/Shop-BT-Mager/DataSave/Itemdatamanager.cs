using Assets.PJW.Script.SO_Script;
using System.Collections.Generic;
using UnityEngine;

public class Itemdatamanager : MonoBehaviour
{
    private static Itemdatamanager _instance;

    // 처음 호출되는 순간, 씬에 없으면 Resources 프리팹에서 자동으로 만들어냄
    // → 어느 씬이 먼저 로드되든 상관없이 항상 사용 가능
    public static Itemdatamanager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<Itemdatamanager>();

                if (_instance == null)
                {
                    GameObject prefab = Resources.Load<GameObject>("Managers/ItemDataManager");
                    if (prefab != null)
                    {
                        GameObject obj = Instantiate(prefab);
                        obj.name = "ItemDataManager";
                        _instance = obj.GetComponent<Itemdatamanager>();
                    }
                    else
                    {
                        Debug.LogError("[Itemdatamanager] Resources/Managers/ItemDataManager 프리팹을 찾을 수 없습니다! Assets/Resources/Managers/ 폴더에 프리팹을 만들어주세요.");
                    }
                }
            }

            return _instance;
        }
    }

    [Header("전체 ItemListSO 에셋 목록 (계절별 4개 + 필요하면 추가로 더 넣어도 됨)")]
    [SerializeField] private List<ItemListSO> itemListSO = new List<ItemListSO>();

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 이 오브젝트는 파괴되지 않음
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    // 특정 계절의 아이템 배열 가져오기 (0:봄, 1:여름, 2:가을, 3:겨울)
    public Item[] GetItemsForSeason(int seasonIndex)
    {
        if (itemListSO == null || seasonIndex < 0 || seasonIndex >= itemListSO.Count)
        {
            Debug.LogError($"[Itemdatamanager] seasonIndex 범위 초과! (입력값: {seasonIndex})");
            return null;
        }

        var so = itemListSO[seasonIndex];
        if (so == null)
        {
            Debug.LogError($"[Itemdatamanager] {seasonIndex}번 인덱스의 ItemListSO가 Null입니다!");
            return null;
        }

        return so.ItemList;
    }

    // 등록된 모든 SO의 아이템을 하나의 리스트로 합쳐서 반환
    public List<Item> GetAllItems()
    {
        List<Item> all = new List<Item>();

        if (itemListSO == null) return all;

        foreach (var seasonSO in itemListSO)
        {
            if (seasonSO == null || seasonSO.ItemList == null) continue;

            foreach (var item in seasonSO.ItemList)
            {
                if (item != null) all.Add(item);
            }
        }

        return all;
    }

    // 전체 아이템 중 보유 수량(Item_count) > 0 인 것만 반환 (Panel_Sell용)
    public List<Item> GetOwnedItems()
    {
        List<Item> owned = new List<Item>();

        foreach (var item in GetAllItems())
        {
            if (item.Item_count > 0)
            {
                owned.Add(item);
            }
        }

        return owned;
    }

    // 새로운 ItemListSO를 하나 추가하고 싶을 때 (예: 시즌 이벤트, DLC 아이템 목록 등)
    public void AddItemListSO(ItemListSO newSO)
    {
        if (newSO == null) return;

        if (itemListSO == null) itemListSO = new List<ItemListSO>();

        if (!itemListSO.Contains(newSO))
        {
            itemListSO.Add(newSO);
            Debug.Log($"[Itemdatamanager] {newSO.name} 추가됨 (현재 {itemListSO.Count}개)");
        }
    }

    // 여러 개를 한 번에 추가하고 싶을 때
    public void AddItemListSORange(IEnumerable<ItemListSO> newSOList)
    {
        if (newSOList == null) return;

        foreach (var so in newSOList)
        {
            AddItemListSO(so);
        }
    }

    // 목록을 통째로 교체하고 싶을 때
    public void SetItemListSO(List<ItemListSO> newList)
    {
        itemListSO = newList ?? new List<ItemListSO>();
    }
}
