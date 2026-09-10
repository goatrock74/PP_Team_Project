using System;
using UnityEngine;
using KSM._00.Scripts.Crop;
 
namespace KSM._00.Scripts.Items
{
    /// <summary>
    /// 플레이어의 <b>가방 + 핫바 + 손에 든 아이템</b>.
    ///
    /// ★ 가방과 핫바는 서로 다른 Inventory 다. 핫바에 올려둔 도구는
    ///   가방 칸을 차지하지 않는다. 그래서 칸을 가리킬 때는 항상
    ///   (SlotArea, index) 짝으로 다닌다.
    ///
    /// 다른 시스템에서 아이템을 주려면 여전히 이거 한 줄이면 된다:
    ///     PlayerInventory.Instance.Add(fishItem, 1, ItemQuality.Best);
    ///
    /// 반환값은 "다 못 넣고 남은 개수" 다. 가방이 차면 핫바 빈 칸까지 쓴다.
    /// </summary>
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
 
        /// <summary>인벤토리 창에 보이는 칸들</summary>
        public Inventory Bag { get; private set; }
 
        /// <summary>화면 하단 핫바. 가방 칸을 차지하지 않는 별도 저장소</summary>
        public Inventory Hotbar { get; private set; }
 
        /// <summary>예전 코드 호환용 별칭. 가방을 가리킨다</summary>
        public Inventory Inventory => Bag;
 
        /// <summary>가방이 꽉 차서 못 받았을 때 (아이템, 남은 개수)</summary>
        public event Action<ItemSO, int> OnOverflow;
 
        /// <summary>
        /// 아이템이 실제로 들어갔을 때 (아이템, 들어간 개수, 품질).
        /// 획득 알림 UI 가 구독한다. 못 받은 몫은 여기 안 들어온다.
        /// </summary>
        public event Action<ItemSO, int, ItemQuality> OnItemGained;
 
        // ── 손에 든 아이템 ───────────────────────────────────────────
        public ItemSO HeldItem { get; private set; }
        public ItemQuality HeldQuality { get; private set; }
 
        /// <summary>손에 든 것이 어느 저장소에 있는가</summary>
        public SlotArea HeldArea { get; private set; } = SlotArea.Bag;
 
        /// <summary>손에 든 아이템이 있는 칸 번호. 없으면 -1</summary>
        public int HeldSlotIndex { get; private set; } = -1;
 
        /// <summary>손에 든 것이 바뀔 때. UI 하이라이트가 구독한다</summary>
        public event Action OnHeldChanged;
 
        public bool HasHeldItem => HeldItem != null;
 
        /// <summary>지정한 칸이 지금 손에 든 칸인가</summary>
        public bool IsHeld(SlotArea area, int index)
            => HeldItem != null && HeldArea == area && HeldSlotIndex == index;
 
        /// <summary>
        /// 손에 든 것을 <b>실제로 쓸 수 있는가</b>.
        /// 핫바에 올려둔 것만 쓸 수 있다 — 가방에 있는 건 정보만 볼 수 있다.
        /// </summary>
        public bool CanUseHeld => HeldItem != null && HeldArea == SlotArea.Hotbar;
        // ─────────────────────────────────────────────────────────────
 
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
            _cropManager.CanAcceptHarvest = CanAccept;   // 수확 전 자리 확인용
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
 
        /// <summary>
        /// 아이템을 넣는다. 1순위 저장소를 먼저 채우고 남으면 나머지로 넘긴다.
        /// 넣지 못하고 남은 개수를 반환 (0이면 전부 들어감)
        /// </summary>
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
 
        /// <summary>저장소를 지정해서 넣는다. 그 저장소가 차면 남은 건 반환한다</summary>
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
        //  옮기기 (드래그 앤 드롭)
        // ════════════════════════════════════════════════════════════
 
        /// <summary>
        /// 칸을 옮기거나 교환한다. 가방↔핫바를 가리지 않는다.
        /// 같은 아이템·품질이면 합쳐진다.
        /// </summary>
        public void MoveOrSwap(SlotArea fromArea, int fromIndex, SlotArea toArea, int toIndex)
        {
            if (fromArea == toArea && fromIndex == toIndex) return;
 
            Inventory.MoveOrSwap(GetContainer(fromArea), fromIndex, GetContainer(toArea), toIndex);
 
            // 손에 들고 있던 칸을 옮겼으면 손도 따라간다
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
 
        /// <summary>예전 코드 호환용. 가방 칸을 든다</summary>
        public void HoldSlot(int index) => HoldSlot(SlotArea.Bag, index);
 
        /// <summary>그 칸의 아이템을 손에 든다. 빈 칸이면 손을 비운다</summary>
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
 
        /// <summary>
        /// 손에 든 것을 소모한다. 들고 있는 그 칸에서만 뺀다.
        /// 다 쓰면 손이 자동으로 비워진다. 성공하면 true
        /// </summary>
        public bool ConsumeHeld(int amount = 1)
        {
            if (HeldItem == null || amount <= 0) return false;
 
            Inventory container = GetContainer(HeldArea);
            ItemStack stack = container.GetSlot(HeldSlotIndex);
 
            if (stack == null || stack.item != HeldItem || stack.count < amount) return false;
 
            container.RemoveFromSlot(HeldSlotIndex, amount);   // OnChanged → RevalidateHeld 가 정리
            return true;
        }
 
        /// <summary>
        /// 내용이 바뀌었을 때 손에 든 정보가 아직 유효한지 확인한다.
        /// 칸을 옮겼거나 다 써버렸을 때 엉뚱한 칸을 가리키는 걸 막는다.
        /// </summary>
        private void RevalidateHeld()
        {
            if (HeldItem == null) return;
 
            ItemStack current = GetSlot(HeldArea, HeldSlotIndex);
            if (current != null && current.Matches(HeldItem, HeldQuality)) return;   // 그대로면 할 일 없음
 
            // 같은 아이템·품질이 남아 있으면 그 칸으로 따라간다. 들고 있던 저장소부터 본다
            if (TryFollow(HeldArea)) return;
            if (TryFollow(HeldArea == SlotArea.Bag ? SlotArea.Hotbar : SlotArea.Bag)) return;
 
            ClearHeld();   // 다 떨어졌다
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
            [Min(1)] public int count;
            public ItemQuality quality;
        }
    }
}