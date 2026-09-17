using System.Collections.Generic;
using System.Text;
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

        [Tooltip("보조 닫기 키. 제작대의 상호작용 키(E)를 넣으면 E로 열고 E로 닫는 토글이 된다.\n" +
                 "안 쓰면 None 으로 두면 된다")]
        [SerializeField] private Key altCloseKey = Key.None;

        [Tooltip("창이 열려 있는 동안 캐릭터를 제자리에 묶는다")]
        [SerializeField] private bool lockMovementWhileOpen = true;

        [Tooltip("열릴 때마다 '왜 안 보이는지' 를 콘솔에 찍는다. 다 고치면 꺼둘 것")]
        [SerializeField] private bool diagnoseOnOpen;

        /// <summary>제작창이 열려 있는가. 다른 입력 처리기가 비켜주는 데 쓴다</summary>
        public static bool IsOpen { get; private set; }

        /// <summary>
        /// 창이 닫힌 마지막 프레임 번호.
        /// 열기 키와 닫기 키가 같을 때, 닫은 그 프레임에 제작대가 곧바로 다시 여는 걸 막는다.
        /// </summary>
        public static int LastCloseFrame { get; private set; } = -1;

        /// <summary>창을 연 프레임. 그 프레임엔 닫기 입력을 무시한다</summary>
        private int _openedFrame = -1;

        // Enter Play Mode Options 에서 Reload Domain 을 꺼두면 static 이 남아 있는다.
        // true 로 굳어버리면 제작대 안내가 영영 안 뜨니 플레이 시작 때 되돌린다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsOpen = false;
            LastCloseFrame = -1;
        }

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

            // ★ 창을 연 바로 그 프레임엔 닫지 않는다.
            //   열기 키와 닫기 키가 같으면 제작대가 연 것을 이 Update 가 같은 프레임에
            //   다시 닫아버려서, 화면에 한 프레임도 안 보이고 사라진다
            if (Time.frameCount == _openedFrame) return;

            if (WasPressed(closeKey) || WasPressed(altCloseKey)) Close();
        }

        /// <summary>Key.None 을 인덱싱하면 예외가 나므로 걸러준다</summary>
        private static bool WasPressed(Key key)
            => key != Key.None && Keyboard.current[key].wasPressedThisFrame;

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
            _openedFrame = Time.frameCount;

            if (titleText != null) titleText.text = station.Title;

            BuildList();

            // 재료가 바뀌면 색과 버튼 상태가 따라와야 한다
            _player.Bag.OnChanged += RefreshDetail;
            _player.Hotbar.OnChanged += RefreshDetail;

            LockMovement(true);
            CloseInventory();

            // 첫 항목을 자동으로 선택해준다
            Select(_entries.Count > 0 ? _entries[0].Recipe : null);

            if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;

            if (diagnoseOnOpen) DiagnoseVisibility();
        }

        public void Close()
        {
            if (!IsOpen) return;

            Unhook();
            LockMovement(false);

            panel.SetActive(false);
            IsOpen = false;

            // 제작대가 같은 프레임에 이 키 입력을 또 읽고 다시 열지 않도록 표시해둔다
            LastCloseFrame = Time.frameCount;

            _station = null;
            _selected = null;
        }

        private void Unhook()
        {
            if (_player == null) return;

            _player.Bag.OnChanged -= RefreshDetail;
            _player.Hotbar.OnChanged -= RefreshDetail;
        }

        /// <summary>제작창과 인벤토리가 겹치면 드래그가 꼬인다. 열려 있으면 닫아준다</summary>
        private void CloseInventory()
        {
            InventoryUI invUI = FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);

            if (invUI != null && invUI.IsOpen) invUI.SetOpen(false);
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

            // ★ 재료는 지금 빼고, 결과물은 연출이 끝난 뒤에 준다.
            //   재료를 미리 빼두면 연출 중에 또 눌러도 이중 제작이 안 되고,
            //   "제작중" 이 끝나는 순간 아이템이 들어와서 연출과 결과가 맞물린다
            if (!_selected.TryReserve(_player, _amount, out string reason))
            {
                Debug.Log($"[제작] 실패 — {reason}");
                RefreshDetail();
                return;
            }

            // 연출이 끝날 때쯤엔 _selected 나 _amount 가 바뀌어 있을 수 있다.
            // 지금 값을 복사해두면 도중에 창을 닫아도 결과물이 제대로 들어간다
            CraftingRecipeSO recipe = _selected;
            PlayerInventory player = _player;
            int batch = _amount;

            Debug.Log($"[제작] {recipe.DisplayName} x{recipe.resultCount * batch} 제작 시작");

            // 재료가 줄었으니 만들 수 있는 개수도 다시 잡아준다
            int max = recipe.MaxAffordable(player);
            _amount = Mathf.Clamp(_amount, 1, Mathf.Max(1, Mathf.Min(recipe.maxBatch, Mathf.Max(1, max))));

            RefreshDetail();

            if (overlay == null)
            {
                recipe.Deliver(player, batch);
                RefreshDetail();
                return;
            }

            overlay.Play(() =>
            {
                recipe.Deliver(player, batch);

                Debug.Log($"[제작] {recipe.DisplayName} x{recipe.resultCount * batch} 완성");

                if (IsOpen) RefreshDetail();   // 창이 아직 열려 있으면 목록 갱신
            });
        }

        // ════════════════════════════════════════════════════════════
        //  진단 — "켜졌는데 안 보인다" 를 잡는다
        // ════════════════════════════════════════════════════════════

        [ContextMenu("제작창 표시 진단")]
        private void DiagnoseVisibility()
        {
            if (panel == null) { Debug.LogError("[제작창진단] Panel 이 비어 있습니다.", this); return; }

            StringBuilder sb = new StringBuilder();
            int problems = 0;

            sb.AppendLine($"═══ 제작창 표시 진단 : {panel.name} ═══");
            sb.AppendLine($"· 활성 : {panel.activeSelf} / 계층 전체 {panel.activeInHierarchy}");

            if (!panel.activeInHierarchy)
            {
                sb.AppendLine("  ★ 부모 중 하나가 꺼져 있습니다");
                problems++;
            }

            // ── RectTransform ────────────────────────────────────
            RectTransform rt = panel.transform as RectTransform;

            if (rt == null)
            {
                sb.AppendLine("· RectTransform : 없음  ★ UI 오브젝트가 아닙니다. Canvas 밑에 UI→Panel 로 다시 만드세요");
                problems++;
            }
            else
            {
                Rect r = rt.rect;
                sb.AppendLine($"· anchoredPosition : {rt.anchoredPosition}");
                sb.AppendLine($"· rect 크기 : {r.width:0.#} x {r.height:0.#}");

                if (r.width < 1f || r.height < 1f)
                {
                    sb.AppendLine("  ★ 폭이나 높이가 0 입니다. Width/Height 를 넣거나 Anchor 를 stretch 로 두세요");
                    problems++;
                }

                sb.AppendLine($"· localScale : {rt.localScale} / 최종 {rt.lossyScale}");

                if (Mathf.Abs(rt.lossyScale.x) < 0.001f || Mathf.Abs(rt.lossyScale.y) < 0.001f)
                {
                    sb.AppendLine("  ★ Scale 이 0 입니다");
                    problems++;
                }

                // 화면 안에 걸쳐 있는지
                Canvas canvas = panel.GetComponentInParent<Canvas>();
                Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                           ? canvas.worldCamera
                           : null;

                Vector3[] corners = new Vector3[4];
                rt.GetWorldCorners(corners);

                Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
                Vector2 max = new Vector2(float.MinValue, float.MinValue);

                foreach (Vector3 c in corners)
                {
                    Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, c);
                    min = Vector2.Min(min, sp);
                    max = Vector2.Max(max, sp);
                }

                sb.AppendLine($"· 화면 좌표 : ({min.x:0}, {min.y:0}) ~ ({max.x:0}, {max.y:0})   [화면 {Screen.width}x{Screen.height}]");

                bool onScreen = max.x > 0f && min.x < Screen.width && max.y > 0f && min.y < Screen.height;

                if (!onScreen)
                {
                    sb.AppendLine("  ★ 화면 밖에 있습니다. Pos X / Pos Y 를 0 으로 되돌리세요");
                    problems++;
                }
            }

            // ── CanvasGroup 체인 ─────────────────────────────────
            Transform t = panel.transform;
            while (t != null)
            {
                CanvasGroup cg = t.GetComponent<CanvasGroup>();

                if (cg != null)
                {
                    sb.AppendLine($"· CanvasGroup [{t.name}] : alpha {cg.alpha:0.##}, interactable {cg.interactable}");

                    if (cg.alpha < 0.01f)
                    {
                        sb.AppendLine("  ★ alpha 가 0 이라 투명합니다");
                        problems++;
                    }
                }

                t = t.parent;
            }

            // ── Canvas ───────────────────────────────────────────
            Canvas root = panel.GetComponentInParent<Canvas>();

            if (root == null)
            {
                sb.AppendLine("· Canvas : 없음  ★ Canvas 밖에 있습니다");
                problems++;
            }
            else
            {
                sb.AppendLine($"· Canvas [{root.name}] : enabled {root.enabled}, {root.renderMode}, sortingOrder {root.sortingOrder}");

                if (!root.enabled)
                {
                    sb.AppendLine("  ★ Canvas 컴포넌트가 꺼져 있습니다");
                    problems++;
                }

                if (root.renderMode != RenderMode.ScreenSpaceOverlay && root.worldCamera == null)
                {
                    sb.AppendLine("  ★ Render Camera 가 비어 있습니다");
                    problems++;
                }
            }

            // ── 실제로 그릴 게 있는지 ────────────────────────────
            Graphic[] graphics = panel.GetComponentsInChildren<Graphic>(false);
            int visible = 0;

            foreach (Graphic g in graphics)
                if (g.enabled && g.color.a > 0.01f) visible++;

            sb.AppendLine($"· 자식 Graphic : 전체 {graphics.Length}개 중 보이는 것 {visible}개");

            if (visible == 0)
            {
                sb.AppendLine("  ★ 그려지는 게 하나도 없습니다. Image 색의 알파나 TMP 색을 확인하세요");
                problems++;
            }

            // ── 형제 순서 (뒤 형제가 위를 덮는다) ────────────────
            sb.AppendLine($"· 형제 순서 : {panel.transform.GetSiblingIndex()} / {panel.transform.parent.childCount - 1}  (숫자가 클수록 위에 그려짐)");

            sb.AppendLine(problems == 0
                ? "→ 표시 자체엔 문제가 없습니다. 화면을 다시 보세요"
                : $"→ ★ 표시된 {problems}곳이 원인입니다");

            if (problems == 0) Debug.Log(sb.ToString(), panel);
            else Debug.LogError(sb.ToString(), panel);
        }
    }
}