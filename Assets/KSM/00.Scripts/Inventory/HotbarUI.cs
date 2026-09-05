using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
 
namespace KSM._00.Scripts.Items
{
    /// <summary>
    /// 화면 하단 핫바. 인벤토리의 앞 N칸을 그대로 비춘다.
    /// 별도 저장소가 아니라 같은 Inventory 를 보므로, 인벤토리 창에서 옮기면 핫바도 바뀐다.
    ///
    ///   숫자키 1~9 : 그 칸을 손에 든다 (같은 칸 다시 = 놓기)
    ///   마우스 휠   : 좌우로 이동
    ///   슬롯 클릭   : 손에 들기
    ///
    /// 씬 구조:
    ///   Canvas
    ///    └ HotbarPanel      Image(배경) + HotbarUI
    ///       └ SlotRow       Horizontal Layout Group   ← Slot Parent 로 연결
    /// </summary>
    public class HotbarUI : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("Horizontal Layout Group 이 붙은 오브젝트")]
        [SerializeField] private Transform slotParent;
 
        [Tooltip("인벤토리와 같은 슬롯 프리팹을 써도 된다")]
        [SerializeField] private InventorySlotUI slotPrefab;
 
        [Header("설정")]
        [Tooltip("핫바 칸 수. 인벤토리 0번부터 이만큼을 비춘다")]
        [SerializeField, Range(1, 9)] private int slotCount = 8;
 
        [Tooltip("마우스 휠로 칸을 옮길 수 있게 한다")]
        [SerializeField] private bool wheelSelect = true;
 
        // Key 열거형은 Digit1~Digit9 순서가 보장되지 않을 수 있어 명시적으로 나열한다
        private static readonly Key[] NumberKeys =
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
            Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9,
        };
 
        private readonly List<InventorySlotUI> _views = new();
        private PlayerInventory _player;
        private Inventory _inventory;
 
        private void Start()
        {
            _player = PlayerInventory.Instance;
            if (_player == null)
            {
                Debug.LogError("[핫바] 씬에 PlayerInventory 가 없습니다.", this);
                enabled = false;
                return;
            }
 
            if (slotParent == null || slotPrefab == null)
            {
                Debug.LogError("[핫바] Slot Parent 또는 Slot Prefab 이 비어있습니다.", this);
                enabled = false;
                return;
            }
 
            _inventory = _player.Inventory;
            slotCount = Mathf.Min(slotCount, _inventory.Capacity);
 
            BuildSlots();
 
            _inventory.OnChanged += Refresh;
            _player.OnHeldChanged += RefreshHighlight;
 
            Refresh();
        }
 
        private void OnDestroy()
        {
            if (_inventory != null) _inventory.OnChanged -= Refresh;
            if (_player != null) _player.OnHeldChanged -= RefreshHighlight;
 
            foreach (InventorySlotUI v in _views)
                if (v != null) v.OnClicked -= HandleSlotClicked;
        }
 
        private void Update()
        {
            if (GachaUI.IsSpinning) return;   // 뽑는 중엔 조작 막기
 
            HandleNumberKeys();
            if (wheelSelect) HandleWheel();
        }
 
        // ════════════════════════════════════════════════════════════
 
        private void BuildSlots()
        {
            for (int i = 0; i < slotCount; i++)
            {
                InventorySlotUI view = Instantiate(slotPrefab, slotParent);
                view.name = $"Hotbar_{i}";
                view.Setup(i);                       // 인벤토리 칸 번호와 그대로 일치시킨다
                view.OnClicked += HandleSlotClicked;
 
                _views.Add(view);
            }
        }
 
        private void Refresh()
        {
            for (int i = 0; i < _views.Count; i++)
                _views[i].SetSlot(_inventory.GetSlot(i));
 
            RefreshHighlight();
        }
 
        private void RefreshHighlight()
        {
            int held = _player.HeldSlotIndex;
 
            for (int i = 0; i < _views.Count; i++)
                _views[i].SetSelected(i == held);
        }
 
        // ════════════════════════════════════════════════════════════
 
        private void HandleNumberKeys()
        {
            if (Keyboard.current == null) return;
 
            int max = Mathf.Min(slotCount, NumberKeys.Length);
 
            for (int i = 0; i < max; i++)
            {
                if (!Keyboard.current[NumberKeys[i]].wasPressedThisFrame) continue;
 
                Select(i);
                return;
            }
        }
 
        private void HandleWheel()
        {
            if (Mouse.current == null) return;
 
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) < 0.01f) return;
 
            int current = _player.HeldSlotIndex;
 
            // 핫바 밖(인벤토리 뒷칸)을 들고 있었으면 0번부터 시작
            if (current < 0 || current >= slotCount) current = 0;
            else current += scroll > 0 ? -1 : 1;
 
            // 양끝에서 반대편으로 돌아간다
            if (current < 0) current = slotCount - 1;
            if (current >= slotCount) current = 0;
 
            Select(current, toggleOff: false);
        }
 
        /// <summary>같은 칸을 다시 고르면 손을 비운다 (toggleOff 가 true 일 때)</summary>
        private void Select(int index, bool toggleOff = true)
        {
            if (toggleOff && _player.HeldSlotIndex == index)
            {
                _player.ClearHeld();
                return;
            }
 
            _player.HoldSlot(index);
        }
 
        private void HandleSlotClicked(int index, bool isLeftClick)
        {
            if (isLeftClick) Select(index);
            else _player.ClearHeld();
        }
    }
}