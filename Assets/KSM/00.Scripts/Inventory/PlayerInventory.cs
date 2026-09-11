using System;
using UnityEngine;
using KSM._00.Scripts.Crop;
 
namespace KSM._00.Scripts.Items
{
   
    public class PlayerInventory : MonoBehaviour
    {
        private static PlayerInventory _instance;
 
        public static PlayerInventory Instance
        {
            get
            {
                if (_instance == null) _instance = FindFirstObjectByType<PlayerInventory>();
                return _instance;
            }
        }
 
        [Header("설정")]
        [Tooltip("가방 칸 수 (인벤토리 창)")]
        [SerializeField, Min(1)] private int capacity = 30;
 
        [Tooltip("핫바 칸 수. 가방과 별개로 잡히는 칸이다")]
        [SerializeField, Range(1, 9)] private int hotbarCapacity = 8;
 
        [Tooltip("켜면 주운 아이템이 핫바부터 채운다. 보통은 꺼두고 가방부터 채운다")]
        [SerializeField] private bool fillHotbarFirst;
 
        [Tooltip("획득 내역을 콘솔에 찍는다")]
        [SerializeField] private bool verboseLog = true;
 
        [Header("테스트")]
        [Tooltip("게임 시작 시 가방에 넣어둘 아이템")]
        [SerializeField] private StartingItem[] startingItems;
 
        [Tooltip("게임 시작 시 핫바에 넣어둘 아이템 (도구 등)")]
        [SerializeField] private StartingItem[] startingHotbarItems;
        public Inventory Bag { get; private set; }
        public Inventory Hotbar { get; private set; }
        public Inventory Inventory => Bag;
        public event Action<ItemSO, int> OnOverflow;
        public event Action<ItemSO, int, ItemQuality> OnItemGained;
        public ItemSO HeldItem { get; private set; }
        public ItemQuality HeldQuality { get; private set; }
        public SlotArea HeldArea { get; private set; } = SlotArea.Bag;
        public int HeldSlotIndex { get; private set; } = -1;
        public event Action OnHeldChanged;
 
        public bool HasHeldItem => HeldItem != null;
        public bool IsHeld(SlotArea area, int index)
            => HeldItem != null && HeldArea == area && HeldSlotIndex == index;
        public bool CanUseHeld => HeldItem != null && HeldArea == SlotArea.Hotbar;
 
 
        private CropManager _cropManager;
 
        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
 
            Bag = new Inventory(capacity);
            Hotbar = new Inventory(hotbarCapacity);
 
            Bag.OnChanged += RevalidateHeld;
            Hotbar.OnChanged += RevalidateHeld;
        }
 
        private void OnDestroy()
        {
            if (Bag != null) Bag.OnChanged -= RevalidateHeld;
            if (Hotbar != null) Hotbar.OnChanged -= RevalidateHeld;
 
            if (_instance == this) _instance = null;
        }
 
        private void OnEnable()
        {
            _cropManager = CropManager.Instance;
            if (_cropManager == null) return;
 
            _cropManager.OnHarvested += HandleHarvested;
            _cropManager.CanAcceptHarvest = CanAccept;  
        }
 
        private void OnDisable()
        {
            if (_cropManager == null) return;
 
            _cropManager.OnHarvested -= HandleHarvested;
            _cropManager.CanAcceptHarvest = null;
            _cropManager = null;
        }
 
        private void Start()
        {
            if (startingItems != null)
                foreach (StartingItem s in startingItems)
                    if (s.item != null) AddTo(SlotArea.Bag, s.item, s.count, s.quality);
 
            if (startingHotbarItems != null)
                foreach (StartingItem s in startingHotbarItems)
                    if (s.item != null) AddTo(SlotArea.Hotbar, s.item, s.count, s.quality);
        }
 
 
        public Inventory GetContainer(SlotArea area)
            => area == SlotArea.Hotbar ? Hotbar : Bag;
 
        public ItemStack GetSlot(SlotArea area, int index)
            => GetContainer(area).GetSlot(index);
 
        public bool CanAccept(ItemSO item, int amount, ItemQuality quality = ItemQuality.Normal)
        {
            if (item == null || amount <= 0) return true;
 
            return Bag.SpaceFor(item, quality) + Hotbar.SpaceFor(item, quality) >= amount;
        }
 
        public bool HasFreeSlot() => Bag.HasEmptySlot() || Hotbar.HasEmptySlot();
        public int Add(ItemSO item, int amount, ItemQuality quality = ItemQuality.Normal)
        {
            if (item == null || amount <= 0) return 0;
 
            Inventory first = fillHotbarFirst ? Hotbar : Bag;
            Inventory second = fillHotbarFirst ? Bag : Hotbar;
 
            int leftover = first.Add(item, amount, quality);
            if (leftover > 0) leftover = second.Add(item, leftover, quality);
 
            ReportGain(item, amount - leftover, leftover, quality);
            return leftover;
        }
        public int AddTo(SlotArea area, ItemSO item, int amount, ItemQuality quality = ItemQuality.Normal)
        {
            if (item == null || amount <= 0) return 0;
 
            int leftover = GetContainer(area).Add(item, amount, quality);
 
            ReportGain(item, amount - leftover, leftover, quality);
            return leftover;
        }
 
        private void ReportGain(ItemSO item, int got, int leftover, ItemQuality quality)
        {
            if (got > 0)
            {
                OnItemGained?.Invoke(item, got, quality);
 
                if (verboseLog)
                {
                    string grade = quality == ItemQuality.Normal
                        ? string.Empty
                        : $"[{ItemQualityUtil.DisplayName(quality)}] ";
 
                    Debug.Log($"[인벤토리] {grade}{item.DisplayName} x{got} 획득");
                }
            }
 
            if (leftover <= 0) return;
 
            Debug.LogWarning($"[인벤토리] 자리가 없음 — {item.DisplayName} x{leftover} 못 받음");
            OnOverflow?.Invoke(item, leftover);
        }
 
        public int Remove(ItemSO item, int amount)
        {
            int removed = Bag.Remove(item, amount);
            if (removed < amount) removed += Hotbar.Remove(item, amount - removed);
 
            return removed;
        }
 
        public int Remove(ItemSO item, ItemQuality quality, int amount)
        {
            int removed = Bag.Remove(item, quality, amount);
            if (removed < amount) removed += Hotbar.Remove(item, quality, amount - removed);
 
            return removed;
        }
 
        public int CountOf(ItemSO item) => Bag.CountOf(item) + Hotbar.CountOf(item);
        public bool Has(ItemSO item, int amount = 1) => CountOf(item) >= amount;
        public void MoveOrSwap(SlotArea fromArea, int fromIndex, SlotArea toArea, int toIndex)
        {
            if (fromArea == toArea && fromIndex == toIndex) return;
 
            Inventory.MoveOrSwap(GetContainer(fromArea), fromIndex, GetContainer(toArea), toIndex);
 
            if (!IsHeld(fromArea, fromIndex)) return;
 
            ItemStack moved = GetSlot(toArea, toIndex);
            if (moved == null || !moved.Matches(HeldItem, HeldQuality)) return;
 
            HeldArea = toArea;
            HeldSlotIndex = toIndex;
            OnHeldChanged?.Invoke();
        }
        public void HoldSlot(int index) => HoldSlot(SlotArea.Bag, index);
 
        public void HoldSlot(SlotArea area, int index)
        {
            ItemStack stack = GetSlot(area, index);
 
            if (stack == null || stack.IsEmpty)
            {
                ClearHeld();
                return;
            }
 
            HeldItem = stack.item;
            HeldQuality = stack.quality;
            HeldArea = area;
            HeldSlotIndex = index;
 
            OnHeldChanged?.Invoke();
        }
 
        public void ClearHeld()
        {
            if (HeldItem == null && HeldSlotIndex < 0) return;
 
            HeldItem = null;
            HeldQuality = ItemQuality.Normal;
            HeldSlotIndex = -1;
 
            OnHeldChanged?.Invoke();
        }
 
        public bool ConsumeHeld(int amount = 1)
        {
            if (HeldItem == null || amount <= 0) return false;
 
            Inventory container = GetContainer(HeldArea);
            ItemStack stack = container.GetSlot(HeldSlotIndex);
 
            if (stack == null || stack.item != HeldItem || stack.count < amount) return false;
 
            container.RemoveFromSlot(HeldSlotIndex, amount);   // OnChanged → RevalidateHeld 가 정리
            return true;
        }
 
        private void RevalidateHeld()
        {
            if (HeldItem == null) return;
 
            ItemStack current = GetSlot(HeldArea, HeldSlotIndex);
            if (current != null && current.Matches(HeldItem, HeldQuality)) return; 
 
            if (TryFollow(HeldArea)) return;
            if (TryFollow(HeldArea == SlotArea.Bag ? SlotArea.Hotbar : SlotArea.Bag)) return;
 
            ClearHeld();  
        }
 
        private bool TryFollow(SlotArea area)
        {
            Inventory container = GetContainer(area);
 
            for (int i = 0; i < container.Capacity; i++)
            {
                ItemStack s = container.GetSlot(i);
                if (s == null || !s.Matches(HeldItem, HeldQuality)) continue;
 
                HeldArea = area;
                HeldSlotIndex = i;
                OnHeldChanged?.Invoke();
 
                return true;
            }
 
            return false;
        }
 
 
        private void HandleHarvested(ItemSO item, int amount, ItemQuality quality)
        {
            if (item == null)
            {
                Debug.LogWarning("[인벤토리] 수확물 ItemSO 가 비어있습니다. CropSO 의 Harvest Item 을 채워주세요.");
                return;
            }
 
            Add(item, amount, quality);
        }
 
        [Serializable]
        private struct StartingItem
        {
            public ItemSO item;
            [Min(1)] public int count;
            public ItemQuality quality;
        }
    }
}