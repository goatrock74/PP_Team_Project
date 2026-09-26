using System;
using System.Collections.Generic;
using KSM._00.Scripts.Items;
using UnityEngine;

/// <summary>
/// Inventory/PlayerInventory 를 건드리지 않고 슬롯 변경을 구독해서 저장한다.
///
/// ★ 알아둘 것 — 저장본이 있으면 <b>Starting Items 는 무시된다</b>.
///   PlayerInventory.Start() 가 시작 아이템을 넣은 직후, 여기 LateUpdate 가
///   저장본으로 가방을 통째로 덮어쓰기 때문이다 (Inventory.Restore 는 빈 칸까지 null 로 만든다).
///   테스트하려고 시작 아이템을 새로 받고 싶으면 먼저 "저장 지우기" 를 실행할 것.
///
/// ★ 아이템은 반드시 Resources/InventoryItemCatalog 에 등록되어 있어야 저장된다.
/// </summary>
[DefaultExecutionOrder(1000)]
public class Inventorysavemanger : MonoBehaviour
{
    public const string SaveKey = "PlayerInventorySaveDataV2";

    private static Inventorysavemanger _instance;
    public static Inventorysavemanger Instance => _instance;

    public string Status { get; private set; } = "인벤토리 대기 중";
    public bool IsReady => bag != null && hotbar != null && canSave;
    public int SaveCount { get; private set; }

    [Header("설정")]
    [Tooltip("저장을 이 간격보다 자주 하지 않는다. PlayerPrefs.Save() 는 디스크에 바로 쓰기 때문에 " +
             "수확처럼 한꺼번에 여러 번 바뀔 때 매번 저장하면 렉이 걸린다")]
    [SerializeField, Min(0f)] private float saveInterval = 1f;

    [Tooltip("불러오기·저장 내역을 콘솔에 찍는다")]
    [SerializeField] private bool verboseLog = true;

    private PlayerInventory player;
    private Inventory bag;
    private Inventory hotbar;
    private bool dirty;
    private string storageKey = SaveKey;
    private bool canSave;
    private float nextSaveTime;

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

    // ════════════════════════════════════════════════════════════

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
            if (dirty) SaveInventory(true);

            Unsubscribe();

            player = current;
            bag = null;
            hotbar = null;
            canSave = false;

            if (current != null && current.Bag != null && current.Hotbar != null)
            {
                bag = current.Bag;
                hotbar = current.Hotbar;

                LoadInventory();                 // Start 의 초기 지급이 끝난 뒤 저장 슬롯으로 교체한다

                bag.OnChanged += MarkDirty;
                hotbar.OnChanged += MarkDirty;
            }
        }

        // ★ 매 프레임 저장하지 않는다. PlayerPrefs.Save() 는 즉시 디스크에 쓰는 동기 작업이라
        //   수확처럼 한 프레임에 여러 칸이 바뀌면 그대로 렉이 된다
        if (dirty && Time.unscaledTime >= nextSaveTime) SaveInventory();
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

        if (dirty) SaveInventory(true);

        Unsubscribe();
        _instance = null;
    }

    private void OnApplicationQuit() { if (dirty) SaveInventory(true); }

    private void OnApplicationPause(bool pause) { if (pause && dirty) SaveInventory(true); }

    // ════════════════════════════════════════════════════════════
    //  저장
    // ════════════════════════════════════════════════════════════

    public void SaveInventory() => SaveInventory(false);

    /// <param name="force">간격을 무시하고 지금 바로 저장한다 (종료·씬 전환용)</param>
    public void SaveInventory(bool force)
    {
        if (!IsReady) return;
        if (!force && Time.unscaledTime < nextSaveTime) return;

        try
        {
            var data = new SaveData { bag = Capture(bag), hotbar = Capture(hotbar) };

            PlayerPrefs.SetString(storageKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();

            dirty = false;
            nextSaveTime = Time.unscaledTime + saveInterval;
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

            if (slot == null || slot.IsEmpty) { slots[i] = new SlotData(); continue; }

            if (!itemKeys.TryGetValue(slot.item, out string key))
            {
                // ★ 예전엔 여기서 예외를 던져 저장 전체를 포기하고 canSave 를 꺼버렸다.
                //   등록 하나 빠졌다고 나머지 아이템까지 못 지키는 건 손해가 너무 크다.
                //   그 칸만 버리고 나머지는 저장한다
                Debug.LogWarning(
                    $"[인벤토리 저장] '{slot.item.name}' 이(가) InventoryItemCatalog 에 없어 " +
                    $"{i}번 칸을 저장하지 못했습니다. 카탈로그에 추가해주세요.", slot.item);

                slots[i] = new SlotData();
                continue;
            }

            slots[i] = new SlotData { itemId = key, count = slot.count, quality = slot.quality };
        }

        return slots;
    }

    // ════════════════════════════════════════════════════════════
    //  불러오기
    // ════════════════════════════════════════════════════════════

    public void LoadInventory()
    {
        if (bag == null || hotbar == null) return;

        canSave = false;
        dirty = false;

        try
        {
            if (!BuildCatalog()) return;         // 카탈로그가 깨졌으면 기존 저장을 건드리지 않는다

            if (PlayerPrefs.HasKey(storageKey))
            {
                SaveData data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(storageKey));

                if (data == null || data.version != 2)
                    throw new InvalidOperationException("지원하지 않는 저장 형식");

                ItemStack[] savedBag = Decode(data.bag, bag.Capacity, "가방");
                ItemStack[] savedHotbar = Decode(data.hotbar, hotbar.Capacity, "핫바");

                bag.Restore(savedBag);
                hotbar.Restore(savedHotbar);

                ShopInventoryBridge.SyncCounts();

                Status = "가방·핫바 복원 완료";

                // ★ 이 로그가 없어서 "시작 아이템이 안 들어온다" 를 한참 헤맸다.
                //   Restore 는 저장본에 없는 칸을 전부 비우므로 Starting Items 가 통째로 날아간다
                if (verboseLog)
                    Debug.Log("[인벤토리 저장] 저장본을 불러와 가방을 덮어썼습니다. " +
                              "Starting Items 는 무시됩니다 — 초기 지급을 다시 받으려면 " +
                              "이 컴포넌트 우클릭 ▸ 저장 지우기 를 실행하세요.", this);
            }
            else
            {
                Status = "새 인벤토리 저장 준비";

                if (verboseLog)
                    Debug.Log("[인벤토리 저장] 저장본이 없어 Starting Items 를 그대로 둡니다.", this);
            }

            canSave = true;
            dirty = true;
        }
        catch (Exception exception)
        {
            Status = "복원 실패 (기존 저장 보호): " + exception.Message;

            Debug.LogError("[인벤토리 저장] " + Status, this);
        }
    }

    /// <summary>아이템 카탈로그를 읽어 id ↔ ItemSO 표를 만든다. 실패하면 false</summary>
    private bool BuildCatalog()
    {
        items.Clear();
        itemKeys.Clear();

        InventoryItemCatalog catalog = Resources.Load<InventoryItemCatalog>("InventoryItemCatalog");

        if (catalog == null || catalog.items == null)
        {
            Status = "복원 중단 — Resources/InventoryItemCatalog 가 없습니다";
            Debug.LogError("[인벤토리 저장] " + Status, this);

            return false;
        }

        foreach (InventoryItemCatalog.Entry entry in catalog.items)
        {
            if (entry == null || entry.item == null || string.IsNullOrEmpty(entry.id))
            {
                Debug.LogWarning("[인벤토리 저장] InventoryItemCatalog 에 비어 있는 칸이 있습니다. 건너뜁니다.", catalog);
                continue;
            }

            if (items.ContainsKey(entry.id))
            {
                Debug.LogWarning($"[인벤토리 저장] id '{entry.id}' 가 중복입니다. 뒤엣것을 건너뜁니다.", catalog);
                continue;
            }

            if (itemKeys.ContainsKey(entry.item))
            {
                Debug.LogWarning($"[인벤토리 저장] '{entry.item.name}' 이(가) 두 번 등록돼 있습니다. 뒤엣것을 건너뜁니다.", catalog);
                continue;
            }

            items.Add(entry.id, entry.item);
            itemKeys.Add(entry.item, entry.id);
        }

        if (items.Count > 0) return true;

        Status = "복원 중단 — InventoryItemCatalog 가 비어 있습니다";
        Debug.LogError("[인벤토리 저장] " + Status, this);

        return false;
    }

    private ItemStack[] Decode(SlotData[] data, int capacity, string label)
    {
        if (data == null) throw new InvalidOperationException("슬롯 데이터 누락");

        var slots = new ItemStack[capacity];

        for (int i = 0; i < data.Length; i++)
        {
            SlotData slot = data[i];

            if (slot == null || string.IsNullOrEmpty(slot.itemId)) continue;
            if (i >= capacity) continue;

            // ★ 한 칸이 이상하다고 저장 전체를 버리면 플레이어가 가진 걸 다 잃는다.
            //   그 칸만 비우고 나머지는 살린다
            if (!items.TryGetValue(slot.itemId, out ItemSO item))
            {
                Debug.LogWarning($"[인벤토리 저장] {label} {i}번 칸의 '{slot.itemId}' 을(를) " +
                                 "카탈로그에서 찾지 못해 비웁니다.", this);
                continue;
            }

            if (slot.count <= 0 || slot.count > item.maxStack)
            {
                Debug.LogWarning($"[인벤토리 저장] {label} {i}번 칸 '{item.name}' 의 개수({slot.count})가 " +
                                 $"이상해서 비웁니다. (최대 {item.maxStack})", this);
                continue;
            }

            if (!Enum.IsDefined(typeof(ItemQuality), slot.quality))
            {
                Debug.LogWarning($"[인벤토리 저장] {label} {i}번 칸 '{item.name}' 의 품질 값이 이상해서 " +
                                 "일반으로 되돌립니다.", this);

                slots[i] = new ItemStack(item, slot.count, ItemQuality.Normal);
                continue;
            }

            slots[i] = new ItemStack(item, slot.count, slot.quality);
        }

        return slots;
    }

    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// 저장본을 지운다. 지운 뒤 플레이하면 Starting Items 가 다시 들어온다.
    /// </summary>
    [ContextMenu("저장 지우기")]
    public void ClearSave()
    {
        PlayerPrefs.DeleteKey(storageKey);
        PlayerPrefs.Save();

        dirty = false;
        Status = "저장 삭제 완료 (다음 수량 변경 시 다시 저장)";

        Debug.Log("[인벤토리 저장] " + Status + " — 플레이를 다시 시작하면 Starting Items 가 들어옵니다.", this);
    }

    /// <summary>지금 저장에 쓰이는 상태를 콘솔에 찍는다</summary>
    [ContextMenu("저장 상태 보기")]
    private void DumpStatus()
    {
        var sb = new System.Text.StringBuilder("[인벤토리 저장] 상태\n");

        sb.AppendLine($"  Status : {Status}");
        sb.AppendLine($"  저장 가능 : {canSave}   저장 횟수 : {SaveCount}");
        sb.AppendLine($"  카탈로그 등록 수 : {items.Count}");
        sb.AppendLine($"  저장본 있음 : {PlayerPrefs.HasKey(storageKey)}");
        sb.Append($"  PlayerInventory : {(player == null ? "못 찾음" : player.name)}");

        Debug.Log(sb.ToString(), this);
    }
}