using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
 
namespace KSM._00.Scripts.Items
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("켜고 끌 패널 루트")]
        [SerializeField] private GameObject panel;
 
        [Tooltip("Grid Layout Group 이 붙은 오브젝트. 슬롯들이 여기 자식으로 생성된다")]
        [SerializeField] private Transform slotParent;
 
        [SerializeField] private InventorySlotUI slotPrefab;
 
        [Tooltip("선택한 아이템 이름/설명 표시 (선택)")]
        [SerializeField] private TMP_Text infoText;
 
        [Header("설정")]
        [SerializeField] private Key toggleKey = Key.Tab;
        [SerializeField] private bool openOnStart;
 
        [Tooltip("씨앗을 손에 들면 창을 자동으로 닫는다. 꺼두면 Tab 으로 직접 닫아야 심을 수 있다")]
        [SerializeField] private bool closeOnHold;
 
        private readonly List<InventorySlotUI> _slotViews = new();
        private PlayerInventory _player;
        private Inventory _inventory;
        
        private CanvasGroup _canvasGroup;
        private bool _useCanvasGroup;
        private bool _isOpen;
 
        public bool IsOpen => _isOpen;
 
        private void Start()
        {
            _player = PlayerInventory.Instance;
            if (_player == null)
            {
                Debug.LogError("[InventoryUI] 씬에 PlayerInventory 가 없습니다. 플레이어에 붙여주세요.", this);
                enabled = false;
                return;
            }
 
            if (panel == null)
            {
                Debug.LogError("[InventoryUI] Panel 칸이 비어있습니다.", this);
                enabled = false;
                return;
            }
 
            SetupToggleMode();
 
            _inventory = _player.Inventory;
 
            BuildSlots();
            if (!enabled) return;         
 
            _inventory.OnChanged += Refresh;
            _player.OnHeldChanged += RefreshHighlight;
 
            Refresh();
            SetOpen(openOnStart);
        }
 
        private void OnDestroy()
        {
            if (_inventory != null) _inventory.OnChanged -= Refresh;
            if (_player != null) _player.OnHeldChanged -= RefreshHighlight;
 
            foreach (InventorySlotUI view in _slotViews)
                if (view != null) view.OnClicked -= HandleSlotClicked;
        }
 
        private void Update()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current[toggleKey].wasPressedThisFrame) Toggle();
        }
        
        
        private void SetupToggleMode()
        {
            _useCanvasGroup = transform.IsChildOf(panel.transform);
            if (!_useCanvasGroup) return;
 
            _canvasGroup = panel.GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = panel.AddComponent<CanvasGroup>();
 
            Debug.LogWarning(
                "[InventoryUI] 이 스크립트가 Panel 안에 있어서 CanvasGroup 방식으로 전환했습니다. " +
                "정상 동작하지만, 스크립트를 Canvas 로 옮기는 게 더 깔끔합니다.", this);
        }
 
        public void Toggle() => SetOpen(!_isOpen);
 
        public void SetOpen(bool open)
        {
            _isOpen = open;
 
            if (_useCanvasGroup)
            {
                _canvasGroup.alpha = open ? 1f : 0f;
                _canvasGroup.interactable = open;
                _canvasGroup.blocksRaycasts = open;
            }
            else
            {
                panel.SetActive(open);
            }
 
            if (open) Refresh();
        }
 
        private void BuildSlots()
        {
            if (slotParent == null || slotPrefab == null)
            {
                Debug.LogError("[InventoryUI] Slot Parent 또는 Slot Prefab 이 비어있습니다.", this);
                enabled = false;
                return;
            }
 
            for (int i = 0; i < _inventory.Capacity; i++)
            {
                InventorySlotUI view = Instantiate(slotPrefab, slotParent);
                view.name = $"Slot_{i:00}";
                view.Setup(i);
                view.OnClicked += HandleSlotClicked;
 
                _slotViews.Add(view);
            }
        }
 
        private void Refresh()
        {
            for (int i = 0; i < _slotViews.Count; i++)
                _slotViews[i].SetSlot(_inventory.GetSlot(i));
 
            RefreshHighlight();
        }
 
        private void RefreshHighlight()
        {
            int held = _player.HeldSlotIndex;
 
            for (int i = 0; i < _slotViews.Count; i++)
                _slotViews[i].SetSelected(i == held);
 
            UpdateInfoText();
        }
        
        private void HandleSlotClicked(int index, bool isLeftClick)
        {
            if (isLeftClick)
            {
                if (_player.HeldSlotIndex == index)
                {
                    _player.ClearHeld();
                    return;
                }
 
                _player.HoldSlot(index);
                if (closeOnHold && _player.HeldItem is SeedSO) SetOpen(false);
                return;
            }
            int held = _player.HeldSlotIndex;
            if (held < 0 || held == index) return;
 
            _inventory.SwapOrMerge(held, index);
            _player.HoldSlot(index);   // 옮긴 칸을 계속 들고 있는다
        }
 
        private void UpdateInfoText()
        {
            if (infoText == null) return;
 
            int held = _player.HeldSlotIndex;
            ItemStack stack = held >= 0 ? _inventory.GetSlot(held) : null;
 
            if (stack == null || stack.IsEmpty)
            {
                infoText.text = string.Empty;
                return;
            }
 
            var sb = new System.Text.StringBuilder();
            
            sb.Append($"<size=120%><b>{stack.item.DisplayName}</b></size>");
 
            if (stack.quality != ItemQuality.Normal)
            {
                string hex = ColorUtility.ToHtmlStringRGB(ItemQualityUtil.TintColor(stack.quality));
                sb.Append($"  <color=#{hex}>{ItemQualityUtil.Stars(stack.quality)} " +
                          $"{ItemQualityUtil.DisplayName(stack.quality)}</color>");
            }
 
            
            sb.Append($"\n<size=85%><color=#AAAAAA>{ItemTypeUtil.DisplayName(stack.item.itemType)}");
            if (stack.count > 1) sb.Append($"  ·  {stack.count}개");
            sb.Append("</color></size>");
 
            
            if (!string.IsNullOrWhiteSpace(stack.item.description))
                sb.Append($"\n\n<size=85%>{stack.item.description}</size>");
            
            if (stack.item.sellPrice > 0)
            {
                int unit = stack.item.GetSellPrice(stack.quality);
                sb.Append($"\n\n<size=85%>SellPrice {unit}G");
                if (stack.count > 1) sb.Append($"  (everyCount {stack.TotalSellPrice}G)");
                sb.Append("</size>");
            }
            
            if (stack.item is SeedSO seed && seed.IsPlantable)
                sb.Append($"\n\n<size=85%><color=#8FE08F>{toggleKey} Close Chang farm leftClick</color></size>");
 
            infoText.text = sb.ToString();
        }
    }
}