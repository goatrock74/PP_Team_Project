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

        // ════════════════════════════════════════════════════════════

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
            GiveStartingItems(startingItems, SlotArea.Bag, "Starting Items");
            GiveStartingItems(startingHotbarItems, SlotArea.Hotbar, "Starting Hotbar Items");
        }

        /// <summary>
        /// 시작 아이템을 넣는다.
        ///
        /// ★ Count 가 0이면 예전엔 아무 말 없이 무시됐다.
        ///   StartingItem 은 구조체라 인스펙터에서 칸을 새로 만들면 Count 가 0으로 시작하는데,
        ///   AddTo 가 amount &lt;= 0 에서 조용히 return 해버려서 "아이템은 꽂았는데 안 들어온다" 가 됐다.
        ///   이제는 왜 안 들어갔는지 콘솔에 찍는다.
        /// </summary>
        private void GiveStartingItems(StartingItem[] list, SlotArea area, string label)
        {
            if (list == null) return;

            for (int i = 0; i < list.Length; i++)
            {
                StartingItem s = list[i];
                if (s.item == null) continue;

                if (s.count <= 0)
                {
                    Debug.LogWarning(
                        $"[인벤토리] {label} 의 {i}번째 칸 '{s.item.DisplayName}' 은 " +
                        $"Count 가 {s.count} 라서 넣지 않았습니다. 1 이상으로 바꿔주세요.", this);

                    continue;
                }

                AddTo(area, s.item, s.count, s.quality);
            }
        }

        /// <summary>
        /// 구조체는 필드 초기화식을 못 쓰고 [Min(1)] 도 값을 직접 건드릴 때만 작동한다.
        /// 그래서 인스펙터에서 칸이 새로 생기면 여기서 Count 를 1로 채워준다.
        /// </summary>
        private void OnValidate()
        {
            FixCounts(startingItems);
            FixCounts(startingHotbarItems);
        }

        private static void FixCounts(StartingItem[] list)
        {
            if (list == null) return;

            for (int i = 0; i < list.Length; i++)
            {
                if (list[i].count > 0) continue;

                StartingItem s = list[i];
                s.count = 1;
                list[i] = s;
            }
        }

        // ════════════════════════════════════════════════════════════
        //  저장소 접근
        // ════════════════════════════════════════════════════════════

        public Inventory GetContainer(SlotArea area)
            => area == SlotArea.Hotbar ? Hotbar : Bag;

        public ItemStack GetSlot(SlotArea area, int index)
            => GetContainer(area).GetSlot(index);

        /// <summary>가방·핫바를 합쳐서 이만큼 받을 자리가 있는가</summary>
        public bool CanAccept(ItemSO item, int amount, ItemQuality quality = ItemQuality.Normal)
        {
            if (item == null || amount <= 0) return true;

            return Bag.SpaceFor(item, quality) + Hotbar.SpaceFor(item, quality) >= amount;
        }

        /// <summary>완전히 빈 칸이 하나라도 있는가 (뽑기처럼 무엇이 나올지 모를 때)</summary>
        public bool HasFreeSlot() => Bag.HasEmptySlot() || Hotbar.HasEmptySlot();

        // ════════════════════════════════════════════════════════════
        //  넣기 / 빼기
        // ════════════════════════════════════════════════════════════

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

        /// <summary>가방에서 먼저 빼고, 모자라면 핫바에서 마저 뺀다</summary>
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

        // ════════════════════════════════════════════════════════════
        //  칸 옮기기
        // ════════════════════════════════════════════════════════════

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

        // ════════════════════════════════════════════════════════════
        //  손에 들기
        // ════════════════════════════════════════════════════════════

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

        // ════════════════════════════════════════════════════════════

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

            [Tooltip("몇 개를 넣을지. 구조체라 새 칸은 0으로 생기는데, OnValidate 가 1로 채워준다")]
            [Min(1)] public int count;

            public ItemQuality quality;
        }
    }
}