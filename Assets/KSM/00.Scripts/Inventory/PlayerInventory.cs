using System;
using UnityEngine;
using KSM._00.Scripts.Crop;
 
namespace KSM._00.Scripts.Items
{
    /// <summary>
    /// 플레이어의 가방 + 손에 든 아이템.
    ///
    /// 다른 시스템(낚시, 요리, 채광...)에서 아이템을 주려면 이거 한 줄이면 된다:
    ///     PlayerInventory.Instance.Add(fishItem, 1);
    ///     PlayerInventory.Instance.Add(fishItem, 1, ItemQuality.Best);   // 품질까지
    ///
    /// 반환값은 "가방이 꽉 차서 못 넣은 개수"다. 0이 아니면 바닥에 떨어뜨리거나
    /// "가방이 가득 찼습니다" 를 띄우면 된다.
    ///
    /// ScriptableObject 에는 아무것도 저장하지 않는다. 개수·품질은 전부 Inventory 가 들고 있다.
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
        [Tooltip("가방 칸 수")]
        [SerializeField, Min(1)] private int capacity = 30;
 
        [Tooltip("획득 내역을 콘솔에 찍는다")]
        [SerializeField] private bool verboseLog = true;
 
        [Header("테스트")]
        [Tooltip("게임 시작 시 넣어둘 아이템")]
        [SerializeField] private StartingItem[] startingItems;
 
        public Inventory Inventory { get; private set; }
 
        /// <summary>가방이 꽉 차서 못 받았을 때 (아이템, 남은 개수)</summary>
        public event Action<ItemSO, int> OnOverflow;
 
        // ── 손에 든 아이템 ───────────────────────────────────────────
        public ItemSO HeldItem { get; private set; }
        public ItemQuality HeldQuality { get; private set; }
 
        /// <summary>손에 든 아이템이 있는 칸 번호. 없으면 -1</summary>
        public int HeldSlotIndex { get; private set; } = -1;
 
        /// <summary>손에 든 것이 바뀔 때. UI 하이라이트가 구독한다</summary>
        public event Action OnHeldChanged;
 
        public bool HasHeldItem => HeldItem != null;
        // ─────────────────────────────────────────────────────────────
 
        private CropManager _cropManager;
 
        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
 
            Inventory = new Inventory(capacity);
            Inventory.OnChanged += RevalidateHeld;
        }
 
        private void OnDestroy()
        {
            if (Inventory != null) Inventory.OnChanged -= RevalidateHeld;
            if (_instance == this) _instance = null;
        }
 
        private void OnEnable()
        {
            _cropManager = CropManager.Instance;
            if (_cropManager != null) _cropManager.OnHarvested += HandleHarvested;
        }
 
        private void OnDisable()
        {
            if (_cropManager != null) _cropManager.OnHarvested -= HandleHarvested;
            _cropManager = null;
        }
 
        private void Start()
        {
            if (startingItems == null) return;
 
            foreach (StartingItem s in startingItems)
                if (s.item != null) Add(s.item, s.count, s.quality);
        }
 
        // ════════════════════════════════════════════════════════════
        //  넣기 / 빼기
        // ════════════════════════════════════════════════════════════
 
        /// <summary>아이템을 넣는다. 넣지 못하고 남은 개수를 반환 (0이면 전부 들어감)</summary>
        public int Add(ItemSO item, int amount, ItemQuality quality = ItemQuality.Normal)
        {
            if (item == null || amount <= 0) return 0;
 
            int leftover = Inventory.Add(item, amount, quality);
 
            if (verboseLog)
            {
                int got = amount - leftover;
                if (got > 0)
                {
                    string grade = quality == ItemQuality.Normal
                        ? string.Empty
                        : $"[{ItemQualityUtil.DisplayName(quality)}] ";
 
                    Debug.Log($"[인벤토리] {grade}{item.DisplayName} x{got} 획득");
                }
            }
 
            if (leftover > 0)
            {
                Debug.LogWarning($"[인벤토리] 가방이 가득 참 — {item.DisplayName} x{leftover} 못 받음");
                OnOverflow?.Invoke(item, leftover);
            }
 
            return leftover;
        }
 
        public int Remove(ItemSO item, int amount) => Inventory.Remove(item, amount);
 
        public int Remove(ItemSO item, ItemQuality quality, int amount)
            => Inventory.Remove(item, quality, amount);
 
        public int CountOf(ItemSO item) => Inventory.CountOf(item);
        public bool Has(ItemSO item, int amount = 1) => Inventory.Has(item, amount);
 
        // ════════════════════════════════════════════════════════════
        //  손에 들기
        // ════════════════════════════════════════════════════════════
 
        /// <summary>그 칸의 아이템을 손에 든다. 빈 칸이면 손을 비운다</summary>
        public void HoldSlot(int index)
        {
            ItemStack stack = Inventory.GetSlot(index);
 
            if (stack == null || stack.IsEmpty)
            {
                ClearHeld();
                return;
            }
 
            HeldItem = stack.item;
            HeldQuality = stack.quality;
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
 
            ItemStack stack = Inventory.GetSlot(HeldSlotIndex);
            if (stack == null || stack.item != HeldItem || stack.count < amount) return false;
 
            Inventory.RemoveFromSlot(HeldSlotIndex, amount);   // OnChanged → RevalidateHeld 가 정리
            return true;
        }
 
        /// <summary>
        /// 인벤토리가 바뀌었을 때 손에 든 정보가 아직 유효한지 확인한다.
        /// 칸을 옮겼거나 다 써버렸을 때 엉뚱한 칸을 가리키는 걸 막는다.
        /// </summary>
        private void RevalidateHeld()
        {
            if (HeldItem == null) return;
 
            ItemStack current = Inventory.GetSlot(HeldSlotIndex);
            if (current != null && current.Matches(HeldItem, HeldQuality)) return;   // 그대로면 할 일 없음
 
            // 같은 아이템·품질이 다른 칸에 남아있으면 그 칸으로 따라간다
            for (int i = 0; i < Inventory.Capacity; i++)
            {
                ItemStack s = Inventory.GetSlot(i);
                if (s == null || !s.Matches(HeldItem, HeldQuality)) continue;
 
                HeldSlotIndex = i;
                OnHeldChanged?.Invoke();
                return;
            }
 
            ClearHeld();   // 다 떨어졌다
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