using System;
using System.Collections.Generic;
using KSM._00.Scripts.Items;
using UnityEngine;

// Inventory/PlayerInventory를 변경하지 않고 슬롯 변경을 구독해 저장한다.
[DefaultExecutionOrder(1000)]
public class Inventorysavemanger : MonoBehaviour
{
    public const string SaveKey = "PlayerInventorySaveDataV2";
    private static Inventorysavemanger _instance;
    public static Inventorysavemanger Instance => _instance;
    public string Status { get; private set; } = "인벤토리 대기 중";
    public bool IsReady => bag != null && hotbar != null && canSave;
    public int SaveCount { get; private set; }

    private PlayerInventory player;
    private Inventory bag;
    private Inventory hotbar;
    private bool dirty;
    private string storageKey = SaveKey;
    private bool canSave;
    private readonly Dictionary<string, ItemSO> items = new Dictionary<string, ItemSO>();
    private readonly Dictionary<ItemSO, string> itemKeys = new Dictionary<ItemSO, string>();

    [Serializable]
    public class SlotData
    {
        public string itemId;
        public int count;
        public ItemQuality quality;
    }

    [Serializable]
    public class SaveData
    {
        public int version = 2;
        public SlotData[] bag;
        public SlotData[] hotbar;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() => _instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance == null)
            new GameObject("Inventory Auto Save").AddComponent<Inventorysavemanger>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(this); return; }
        _instance = this;
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void LateUpdate()
    {
        PlayerInventory current = PlayerInventory.Instance;
        if (current != player || (bag == null && current != null))
        {
            if (dirty) SaveInventory();
            Unsubscribe();
            player = current;
            bag = null;
            hotbar = null;
            canSave = false;
            if (current != null && current.Bag != null && current.Hotbar != null)
            {
                bag = current.Bag;
                hotbar = current.Hotbar;
                LoadInventory(); // Start의 초기 지급이 끝난 뒤 저장 슬롯으로 교체한다.
                bag.OnChanged += MarkDirty;
                hotbar.OnChanged += MarkDirty;
            }
        }
        if (dirty) SaveInventory();
    }

    private void MarkDirty() => dirty = true;
    private void Unsubscribe()
    {
        if (bag != null) bag.OnChanged -= MarkDirty;
        if (hotbar != null) hotbar.OnChanged -= MarkDirty;
    }

    private void OnDestroy()
    {
        if (_instance != this) return;
        if (dirty) SaveInventory();
        Unsubscribe();
        _instance = null;
    }
    private void OnApplicationQuit() { if (dirty) SaveInventory(); }
    private void OnApplicationPause(bool pause) { if (pause && dirty) SaveInventory(); }

    public void SaveInventory()
    {
        if (!IsReady) return;
        try
        {
            var data = new SaveData { bag = Capture(bag), hotbar = Capture(hotbar) };
            PlayerPrefs.SetString(storageKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
            dirty = false;
            SaveCount++;
            Status = $"자동 저장 완료 {DateTime.Now:HH:mm:ss} ({SaveCount}회)";
        }
        catch (Exception exception)
        {
            canSave = false;
            Status = "저장 실패: " + exception.Message;
            Debug.LogError("[인벤토리 저장] " + Status, this);
        }
    }

    private SlotData[] Capture(Inventory inventory)
    {
        var slots = new SlotData[inventory.Capacity];
        for (int i = 0; i < slots.Length; i++)
        {
            ItemStack slot = inventory.GetSlot(i);
            if (slot == null) { slots[i] = new SlotData(); continue; }
            if (!itemKeys.TryGetValue(slot.item, out string key))
                throw new InvalidOperationException($"카탈로그에 없는 아이템: {slot.item.name}");
            slots[i] = new SlotData { itemId = key, count = slot.count, quality = slot.quality };
        }
        return slots;
    }

    public void LoadInventory()
    {
        if (bag == null || hotbar == null) return;
        canSave = false;
        dirty = false;
        try
        {
            items.Clear();
            itemKeys.Clear();
            InventoryItemCatalog catalog = Resources.Load<InventoryItemCatalog>("InventoryItemCatalog");
            if (catalog == null || catalog.items == null)
                throw new InvalidOperationException("InventoryItemCatalog가 없습니다.");
            foreach (InventoryItemCatalog.Entry entry in catalog.items)
            {
                if (entry == null || entry.item == null || string.IsNullOrEmpty(entry.id))
                    throw new InvalidOperationException("아이템 카탈로그 참조 누락");
                items.Add(entry.id, entry.item);
                itemKeys.Add(entry.item, entry.id);
            }
            if (PlayerPrefs.HasKey(storageKey))
            {
                SaveData data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(storageKey));
                if (data == null || data.version != 2) throw new InvalidOperationException("지원하지 않는 저장 형식");
                ItemStack[] savedBag = Decode(data.bag, bag.Capacity);
                ItemStack[] savedHotbar = Decode(data.hotbar, hotbar.Capacity);
                bag.Restore(savedBag);
                hotbar.Restore(savedHotbar);
                ShopInventoryBridge.SyncCounts();
                Status = "가방·핫바 복원 완료";
            }
            else Status = "새 인벤토리 저장 준비";
            canSave = true;
            dirty = true;
        }
        catch (Exception exception)
        {
            Status = "복원 실패 (기존 저장 보호): " + exception.Message;
            Debug.LogError("[인벤토리 저장] " + Status, this);
        }
    }

    private ItemStack[] Decode(SlotData[] data, int capacity)
    {
        if (data == null) throw new InvalidOperationException("슬롯 데이터 누락");
        var slots = new ItemStack[capacity];
        for (int i = 0; i < data.Length; i++)
        {
            SlotData slot = data[i];
            if (slot == null || string.IsNullOrEmpty(slot.itemId)) continue;
            if (i >= capacity || !items.TryGetValue(slot.itemId, out ItemSO item)
                || slot.count <= 0 || slot.count > item.maxStack
                || !Enum.IsDefined(typeof(ItemQuality), slot.quality))
                throw new InvalidOperationException($"복원할 수 없는 슬롯 {i}: {slot.itemId}");
            slots[i] = new ItemStack(item, slot.count, slot.quality);
        }
        return slots;
    }

    public void ClearSave()
    {
        PlayerPrefs.DeleteKey(storageKey);
        PlayerPrefs.Save();
        dirty = false;
        Status = "저장 삭제 완료 (다음 수량 변경 시 다시 저장)";
    }
}

