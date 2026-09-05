using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
 
namespace KSM._00.Scripts.Items
{
    /// <summary>
    /// 인벤토리 화면.
    ///
    ///   좌클릭 : 그 칸을 손에 든다. 이미 들고 있는 칸을 다시 누르면 놓는다
    ///   우클릭 : 손에 든 칸과 위치를 바꾼다 (같은 아이템이면 합침)
    ///
    /// 권장 씬 구조 — 이 스크립트는 항상 켜져 있는 오브젝트(Canvas)에 붙인다:
    ///   Canvas                (InventoryUI 를 여기에)
    ///    └ InventoryPanel     (Image 배경)  ← Panel 칸에 이걸 연결
    ///       ├ SlotGrid        (Grid Layout Group)
    ///       └ InfoText        (TextMeshPro, 선택)
    /// </summary>
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
 
        // 패널 안에 자기 자신이 들어있는 경우의 대체 수단
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
            if (!enabled) return;          // BuildSlots 가 실패했으면 중단
 
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
 
        // ════════════════════════════════════════════════════════════
 
        /// <summary>
        /// 이 스크립트가 패널 안에 들어있으면 SetActive 로 껐을 때 자기도 같이 꺼진다.
        /// 그러면 Update 가 멈춰서 다시 열 방법이 없어지므로 CanvasGroup 으로 전환한다.
        /// </summary>
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
 
        /// <summary>손에 든 칸에만 테두리를 켠다</summary>
        private void RefreshHighlight()
        {
            int held = _player.HeldSlotIndex;
 
            for (int i = 0; i < _slotViews.Count; i++)
                _slotViews[i].SetSelected(i == held);
 
            UpdateInfoText();
        }
 
        // ════════════════════════════════════════════════════════════
 
        /// <summary>
        /// 좌클릭 : 손에 들기 / 같은 칸이면 놓기
        /// 우클릭 : 손에 든 칸과 교환·병합
        /// </summary>
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
 
                // 씨앗을 들었으면 바로 심으러 갈 수 있게 창을 닫아준다
                if (closeOnHold && _player.HeldItem is SeedSO) SetOpen(false);
                return;
            }
 
            // 우클릭 — 손에 든 칸이 있어야 옮길 수 있다
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
 
            // 1) 이름 (+ 품질 등급)
            sb.Append($"<size=120%><b>{stack.item.DisplayName}</b></size>");
 
            if (stack.quality != ItemQuality.Normal)
            {
                string hex = ColorUtility.ToHtmlStringRGB(ItemQualityUtil.TintColor(stack.quality));
                sb.Append($"  <color=#{hex}>{ItemQualityUtil.Stars(stack.quality)} " +
                          $"{ItemQualityUtil.DisplayName(stack.quality)}</color>");
            }
 
            // 2) 종류
            sb.Append($"\n<size=85%><color=#AAAAAA>{ItemTypeUtil.DisplayName(stack.item.itemType)}");
            if (stack.count > 1) sb.Append($"  ·  {stack.count}개");
            sb.Append("</color></size>");
 
            // 3) 설명
            if (!string.IsNullOrWhiteSpace(stack.item.description))
                sb.Append($"\n\n<size=85%>{stack.item.description}</size>");
 
            // 4) 판매가
            if (stack.item.sellPrice > 0)
            {
                int unit = stack.item.GetSellPrice(stack.quality);
                sb.Append($"\n\n<size=85%>판매가 {unit}G");
                if (stack.count > 1) sb.Append($"  (전부 {stack.TotalSellPrice}G)");
                sb.Append("</size>");
            }
 
            // 5) 씨앗 안내
            if (stack.item is SeedSO seed && seed.IsPlantable)
                sb.Append($"\n\n<size=85%><color=#8FE08F>{toggleKey} 로 창을 닫고 밭을 좌클릭</color></size>");
 
            // 6) 뽑기 팩이면 확률표
            if (stack.item is ItemPackSO pack)
            {
                sb.Append($"\n\n<size=85%><color=#8FE08F>{toggleKey} 로 창을 닫고 화면을 좌클릭</color></size>");
 
                if (pack.rollCount > 1)
                    sb.Append($"\n<size=85%>한 번에 {pack.rollCount}회 뽑기</size>");
 
                sb.Append("\n\n<size=85%><b>확률</b>");
                sb.Append(pack.lootTable != null
                    ? pack.lootTable.BuildChanceText()
                    : "\n<color=#FF7777>Loot Table 이 연결되지 않았습니다</color>");
                sb.Append("</size>");
            }
 
            infoText.text = sb.ToString();
        }
    }
}