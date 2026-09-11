using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
 
namespace KSM._00.Scripts.Items
{
    public class HotbarUI : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("Horizontal Layout Group 이 붙은 오브젝트")]
        [SerializeField] private Transform slotParent;
 
        [Tooltip("인벤토리와 같은 슬롯 프리팹을 써도 된다")]
        [SerializeField] private InventorySlotUI slotPrefab;
 
        [Header("설정")]
        [Tooltip("마우스 휠로 칸을 옮길 수 있게 한다")]
        [SerializeField] private bool wheelSelect = true;
 
        private static readonly Key[] NumberKeys =
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
            Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9,
        };
 
        private readonly List<InventorySlotUI> _views = new();
        private PlayerInventory _player;
        private Inventory _inventory;
 
        private int slotCount;
 
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
 
            _inventory = _player.Hotbar;      
            slotCount = _inventory.Capacity;
 
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
            if (GachaUI.IsSpinning) return;   
 
            HandleNumberKeys();
            if (wheelSelect) HandleWheel();
        }
 
 
        private void BuildSlots()
        {
            for (int i = 0; i < slotCount; i++)
            {
                InventorySlotUI view = Instantiate(slotPrefab, slotParent);
                view.name = $"Hotbar_{i}";
                view.Setup(SlotArea.Hotbar, i);
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
            for (int i = 0; i < _views.Count; i++)
                _views[i].SetSelected(_player.IsHeld(SlotArea.Hotbar, i));
        }
 
 
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
 
            if (_player.HeldArea != SlotArea.Hotbar || current < 0 || current >= slotCount) current = 0;
            else current += scroll > 0 ? -1 : 1;
 
            if (current < 0) current = slotCount - 1;
            if (current >= slotCount) current = 0;
 
            Select(current, toggleOff: false);
        }
 
        private void Select(int index, bool toggleOff = true)
        {
            if (toggleOff && _player.IsHeld(SlotArea.Hotbar, index))
            {
                _player.ClearHeld();
                return;
            }
 
            _player.HoldSlot(SlotArea.Hotbar, index);
        }
 
        private void HandleSlotClicked(InventorySlotUI slot, bool isLeftClick)
        {
            if (isLeftClick) Select(slot.Index);
            else _player.ClearHeld();
        }
    }
}
 