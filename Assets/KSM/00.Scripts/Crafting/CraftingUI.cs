using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using KSM._00.Scripts.Items;
 
namespace KSM._00.Scripts.Crafting
{
    /// <summary>
    /// 제작창. 왼쪽에 목록(세로 스크롤), 오른쪽에 선택한 것의 상세.
    ///
    /// 씬 구조:
    ///   Canvas
    ///    └ CraftingPanel                ← Panel 에 연결 (평소엔 꺼둔다)
    ///       ├ RecipeScroll              ScrollRect
    ///       │   └ Viewport
    ///       │       └ Content           Vertical Layout Group + Content Size Fitter
    ///       │                           ← List Parent 에 연결
    ///       └ Detail
    ///           ├ ResultIcon  ResultName  MaterialsText  DescriptionText
    ///           ├ Amount      Btn_Minus   Btn_Plus
    ///           └ Btn_Craft (초록)  Btn_Cancel (빨강)
    ///
    /// Content 는 Anchor top-stretch, Pivot (0.5, 1), Content Size Fitter 의
    /// Vertical Fit 을 Preferred Size 로 두면 목록이 늘어날 때 스크롤이 생긴다.
    /// </summary>
    public class CraftingUI : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("켜고 끌 제작창 루트")]
        [SerializeField] private GameObject panel;
 
        [Tooltip("목록 줄이 생성될 곳 (ScrollRect 의 Content)")]
        [SerializeField] private Transform listParent;
 
        [SerializeField] private CraftingRecipeEntryUI entryPrefab;
 
        [Tooltip("스크롤을 맨 위로 되돌리는 데 쓴다. 없어도 된다")]
        [SerializeField] private ScrollRect scrollRect;
 
        [Header("상세 — 결과물")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Image resultIcon;
        [SerializeField] private TMP_Text resultNameText;
 
        [Tooltip("재료칸. 초록/빨강은 코드가 리치 텍스트로 칠한다")]
        [SerializeField] private TMP_Text materialsText;
 
        [Tooltip("능력치 설명창")]
        [SerializeField] private TMP_Text descriptionText;
 
        [Header("상세 — 개수")]
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private Button minusButton;
        [SerializeField] private Button plusButton;
 
        [Header("상세 — 버튼")]
        [SerializeField] private Button craftButton;
        [SerializeField] private Button cancelButton;
 
        [Tooltip("만들 수 없을 때 제작 버튼을 흐리게 만든다")]
        [SerializeField] private Image craftButtonImage;
 
        [SerializeField] private Color craftReadyColor = new Color(0.42f, 0.80f, 0.42f);
        [SerializeField] private Color craftBlockedColor = new Color(0.55f, 0.55f, 0.55f);
 
        [Header("연출")]
        [Tooltip("제작할 때 화면을 덮는 '제작중' 연출. 없어도 동작한다")]
        [SerializeField] private CraftingOverlay overlay;
 
        [Header("설정")]
        [Tooltip("이 키로도 창을 닫을 수 있다")]
        [SerializeField] private Key closeKey = Key.Escape;
 
        [Tooltip("창이 열려 있는 동안 캐릭터를 제자리에 묶는다")]
        [SerializeField] private bool lockMovementWhileOpen = true;
 
        /// <summary>제작창이 열려 있는가. 다른 입력 처리기가 비켜주는 데 쓴다</summary>
        public static bool IsOpen { get; private set; }
 
        // Enter Play Mode Options 에서 Reload Domain 을 꺼두면 static 이 남아 있는다.
        // true 로 굳어버리면 제작대 안내가 영영 안 뜨니 플레이 시작 때 되돌린다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => IsOpen = false;
 
        private readonly List<CraftingRecipeEntryUI> _entries = new();
 
        private CraftingStationSO _station;
        private CraftingRecipeSO _selected;
        private int _amount = 1;
 
        private PlayerInventory _player;
        private KSM._00.Scripts.PlayerMovement _movement;
 
        // ════════════════════════════════════════════════════════════
        //  수명 주기
        // ════════════════════════════════════════════════════════════
 
        private void Awake()
        {
            if (panel == null)
            {
                // 이 스크립트는 Canvas 에 붙는다. Panel 을 비워두면 Canvas 를 통째로 꺼버려서
                // 인벤토리·핫바까지 사라진다. 그래서 조용히 넘어가지 않고 크게 알린다
                panel = gameObject;
 
                Debug.LogError(
                    "[제작] Panel 칸이 비어 있습니다. CraftingPanel 을 연결하세요. " +
                    "이대로 두면 Canvas 전체가 꺼집니다.", this);
            }
 
            if (minusButton != null) minusButton.onClick.AddListener(() => ChangeAmount(-1));
            if (plusButton != null) plusButton.onClick.AddListener(() => ChangeAmount(+1));
            if (craftButton != null) craftButton.onClick.AddListener(Craft);
            if (cancelButton != null) cancelButton.onClick.AddListener(Close);
 
            panel.SetActive(false);
            IsOpen = false;
        }
 
        private void OnDestroy()
        {
            if (minusButton != null) minusButton.onClick.RemoveAllListeners();
            if (plusButton != null) plusButton.onClick.RemoveAllListeners();
            if (craftButton != null) craftButton.onClick.RemoveAllListeners();
            if (cancelButton != null) cancelButton.onClick.RemoveAllListeners();
 
            Unhook();
            IsOpen = false;
        }
 
        private void Update()
        {
            if (!IsOpen || Keyboard.current == null) return;
            if (overlay != null && overlay.IsPlaying) return;   // 연출 중엔 닫지 않는다
 
            if (Keyboard.current[closeKey].wasPressedThisFrame) Close();
        }
 
        // ════════════════════════════════════════════════════════════
        //  열고 닫기
        // ════════════════════════════════════════════════════════════
 
        public void Open(CraftingStationSO station)
        {
            if (station == null || !station.IsUsable)
            {
                Debug.LogWarning("[제작] 레시피가 없는 제작대입니다.", this);
                return;
            }
 
            _station = station;
            _player = PlayerInventory.Instance;
 
            if (_player == null)
            {
                Debug.LogError("[제작] 씬에 PlayerInventory 가 없습니다.", this);
                return;
            }
 
            panel.SetActive(true);
            IsOpen = true;
 
            if (titleText != null) titleText.text = station.Title;
 
            BuildList();
 
            // 재료가 바뀌면 색과 버튼 상태가 따라와야 한다
            _player.Bag.OnChanged += RefreshDetail;
            _player.Hotbar.OnChanged += RefreshDetail;
 
            LockMovement(true);
 
            // 첫 항목을 자동으로 선택해준다
            Select(_entries.Count > 0 ? _entries[0].Recipe : null);
 
            if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
        }
 
        public void Close()
        {
            if (!IsOpen) return;
 
            Unhook();
            LockMovement(false);
 
            panel.SetActive(false);
            IsOpen = false;
 
            _station = null;
            _selected = null;
        }
 
        private void Unhook()
        {
            if (_player == null) return;
 
            _player.Bag.OnChanged -= RefreshDetail;
            _player.Hotbar.OnChanged -= RefreshDetail;
        }
 
        private void LockMovement(bool on)
        {
            if (!lockMovementWhileOpen) return;
 
            if (_movement == null && _player != null)
                _movement = _player.GetComponentInParent<KSM._00.Scripts.PlayerMovement>();
 
            if (_movement != null) _movement.HoldLocked = on;
        }
 
        // ════════════════════════════════════════════════════════════
        //  목록
        // ════════════════════════════════════════════════════════════
 
        private void BuildList()
        {
            if (listParent == null || entryPrefab == null)
            {
                Debug.LogError("[제작] List Parent 또는 Entry Prefab 이 비어있습니다.", this);
                return;
            }
 
            ClearList();
 
            foreach (CraftingRecipeSO recipe in _station.recipes)
            {
                if (recipe == null || recipe.result == null) continue;
 
                CraftingRecipeEntryUI entry = Instantiate(entryPrefab, listParent);
                entry.Bind(recipe);
                entry.OnClicked += HandleEntryClicked;
 
                _entries.Add(entry);
            }
 
            if (_entries.Count == 0)
                Debug.LogWarning("[제작] 결과 아이템이 채워진 레시피가 하나도 없습니다.", this);
        }
 
        private void ClearList()
        {
            foreach (CraftingRecipeEntryUI e in _entries)
            {
                if (e == null) continue;
 
                e.OnClicked -= HandleEntryClicked;
                Destroy(e.gameObject);
            }
 
            _entries.Clear();
        }
 
        private void HandleEntryClicked(CraftingRecipeEntryUI entry) => Select(entry.Recipe);
 
        private void Select(CraftingRecipeSO recipe)
        {
            _selected = recipe;
 
            // 고른 것은 최대한 많이 만들려는 경우가 흔하니 1개부터 시작한다
            _amount = 1;
 
            foreach (CraftingRecipeEntryUI e in _entries)
                e.SetSelected(e.Recipe == recipe);
 
            RefreshDetail();
        }
 
        // ════════════════════════════════════════════════════════════
        //  상세
        // ════════════════════════════════════════════════════════════
 
        private void ChangeAmount(int delta)
        {
            if (_selected == null) return;
 
            _amount = Mathf.Clamp(_amount + delta, 1, Mathf.Max(1, _selected.maxBatch));
            RefreshDetail();
        }
 
        private void RefreshDetail()
        {
            bool has = _selected != null;
 
            if (resultIcon != null)
            {
                resultIcon.enabled = has && _selected.Icon != null;
                resultIcon.sprite = has ? _selected.Icon : null;
            }
 
            if (resultNameText != null)
            {
                int total = has ? _selected.resultCount * _amount : 0;
                resultNameText.text = has
                    ? _selected.DisplayName + (total > 1 ? $"  x{total}" : string.Empty)
                    : string.Empty;
            }
 
            if (descriptionText != null)
                descriptionText.text = has ? _selected.DescriptionText : string.Empty;
 
            if (materialsText != null)
                materialsText.text = has ? _selected.BuildMaterialText(_player, _amount) : string.Empty;
 
            if (amountText != null)
                amountText.text = has ? _amount.ToString() : "-";
 
            // 목록 쪽도 재료 상태에 맞춰 흐리게 / 선명하게
            foreach (CraftingRecipeEntryUI e in _entries)
                if (e != null && e.Recipe != null)
                    e.SetAffordable(e.Recipe.MaxAffordable(_player) > 0);
 
            RefreshButtons();
        }
 
        private void RefreshButtons()
        {
            bool has = _selected != null;
            bool canCraft = has && _selected.HasMaterials(_player, _amount);
 
            if (minusButton != null) minusButton.interactable = has && _amount > 1;
            if (plusButton != null) plusButton.interactable = has && _amount < _selected.maxBatch;
 
            if (craftButton != null) craftButton.interactable = canCraft;
 
            if (craftButtonImage != null)
                craftButtonImage.color = canCraft ? craftReadyColor : craftBlockedColor;
        }
 
        // ════════════════════════════════════════════════════════════
        //  제작
        // ════════════════════════════════════════════════════════════
 
        private void Craft()
        {
            if (_selected == null || _player == null) return;
            if (overlay != null && overlay.IsPlaying) return;   // 연타 방지
 
            // ★ 먼저 실제로 만들고, 연출은 그 뒤에 보여준다.
            //   연출이 끝나고 만들면 도중에 창을 닫았을 때 결과가 새어나간다
            if (!_selected.TryCraft(_player, _amount, out string reason))
            {
                Debug.Log($"[제작] 실패 — {reason}");
                RefreshDetail();
                return;
            }
 
            Debug.Log($"[제작] {_selected.DisplayName} x{_selected.resultCount * _amount} 완성");
 
            // 재료가 줄었으니 만들 수 있는 개수도 다시 잡아준다
            int max = _selected.MaxAffordable(_player);
            _amount = Mathf.Clamp(_amount, 1, Mathf.Max(1, Mathf.Min(_selected.maxBatch, Mathf.Max(1, max))));
 
            RefreshDetail();
 
            if (overlay != null) overlay.Play();
        }
    }
}
 