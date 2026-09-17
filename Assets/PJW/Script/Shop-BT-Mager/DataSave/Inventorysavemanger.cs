using System.Collections.Generic;
using UnityEngine;

public class Inventorysavemanger : MonoBehaviour
{
    private static Inventorysavemanger _instance;

    // 처음 호출되는 순간, 씬에 없으면 Resources 프리팹에서 자동으로 만들어냄
    public static Inventorysavemanger Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<Inventorysavemanger>();

                if (_instance == null)
                {
                    GameObject prefab = Resources.Load<GameObject>("Managers/InventorySaveManager");
                    if (prefab != null)
                    {
                        GameObject obj = Instantiate(prefab);
                        obj.name = "InventorySaveManager";
                        _instance = obj.GetComponent<Inventorysavemanger>();
                    }
                    else
                    {
                        Debug.LogError("[InventorySaveManager] Resources/Managers/InventorySaveManager 프리팹을 찾을 수 없습니다!");
                    }
                }
            }

            return _instance;
        }
    }

    private const string SaveKey = "InventorySaveData";

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
            return;
        }

        LoadInventory();
    }

    private void OnApplicationQuit()
    {
        SaveInventory();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause) SaveInventory();
    }

    public void SaveInventory()
    {
        // ItemDataManager가 있어야 전체 아이템 목록을 받아올 수 있음
        if (Itemdatamanager.Instance == null)
        {
            Debug.LogError("[InventorySaveManager] ItemDataManager를 찾을 수 없습니다!");
            return;
        }

        // 저장용 데이터 컨테이너는 InventorySaveData 타입이어야 함 (자기 자신 타입 X)
        InventorySaveData data = new InventorySaveData();

        // 중앙 매니저에게서 전체 아이템 목록을 받아와서 저장
        List<Item> allItems = Itemdatamanager.Instance.GetAllItems();

        foreach (var item in allItems)
        {
            data.items.Add(new ItemSaveEntry
            {
                itemName = item.Item_name,
                count = item.Item_count
            });
        }

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();

        Debug.Log($"[InventorySaveManager] 저장 완료 (아이템 {data.items.Count}종)");
    }

    public void LoadInventory()
    {
        // ItemDataManager가 있어야 전체 아이템 목록을 받아올 수 있음
        if (Itemdatamanager.Instance == null)
        {
            Debug.LogError("[InventorySaveManager] ItemDataManager를 찾을 수 없습니다!");
            return;
        }

        if (!PlayerPrefs.HasKey(SaveKey))
        {
            Debug.Log("[InventorySaveManager] 저장된 데이터가 없습니다. (최초 실행)");
            return;
        }

        string json = PlayerPrefs.GetString(SaveKey);
        InventorySaveData data = JsonUtility.FromJson<InventorySaveData>(json);

        if (data == null || data.items == null) return;

        Dictionary<string, int> savedCounts = new Dictionary<string, int>();
        foreach (var entry in data.items)
        {
            savedCounts[entry.itemName] = entry.count;
        }

        // Itemdatamanager(소문자 오타) -> ItemDataManager로 수정
        List<Item> allItems = Itemdatamanager.Instance.GetAllItems();

        foreach (var item in allItems)
        {
            item.Item_count = savedCounts.TryGetValue(item.Item_name, out int savedCount)
                ? savedCount
                : 0;
        }

        Debug.Log($"[InventorySaveManager] 불러오기 완료 (아이템 {data.items.Count}종)");
    }

    public void ClearSave()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        Debug.Log("[InventorySaveManager] 저장 데이터 삭제됨");
    }
}
