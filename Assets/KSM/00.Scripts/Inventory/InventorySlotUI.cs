using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
 
namespace KSM._00.Scripts.Items
{
    /// <summary>
    /// 인벤토리 한 칸의 화면 표시. 프리팹으로 만들어 InventoryUI 가 복제해서 쓴다.
    ///
    /// 프리팹 구조:
    ///   Slot            Image(칸 배경, Raycast Target 켜기) + InventorySlotUI
    ///    ├ Icon         Image           (Raycast Target 끄기)
    ///    ├ Count        TextMeshPro     (Raycast Target 끄기)
    ///    └ Quality      TextMeshPro     (Raycast Target 끄기) — 선택. ★ 표기
    ///
    /// 선택 테두리는 Auto Outline 이 켜져 있으면 런타임에 자동으로 만들어진다.
    /// 별도 오브젝트를 준비할 필요 없다.
    /// </summary>
    public class InventorySlotUI : MonoBehaviour,
        IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [Header("표시")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text countText;
 
        [Tooltip("품질 별표(★/★★) 표시. 없어도 됨")]
        [SerializeField] private TMP_Text qualityText;
 
        [Tooltip("켜면 품질에 따라 아이콘 색이 은/금빛으로 물든다")]
        [SerializeField] private bool tintIconByQuality;
 
        [Header("선택 테두리")]
        [Tooltip("켜면 선택 시 테두리를 자동으로 그린다 (프리팹에 오브젝트를 안 만들어도 됨)")]
        [SerializeField] private bool autoOutline = true;
 
        [SerializeField] private Color outlineColor = Color.red;
 
        [SerializeField, Min(1f)] private float outlineThickness = 3f;
 
        [Tooltip("직접 만든 선택 표시 오브젝트를 쓸 거면 여기에 연결 (Auto Outline 대신)")]
        [SerializeField] private GameObject selectedFrame;
 
        public int Index { get; private set; }
 
        /// <summary>이 칸이 속한 저장소 (가방 / 핫바)</summary>
        public SlotArea Area { get; private set; } = SlotArea.Bag;
 
        /// <summary>지금 내용물이 있는가. 드래그 시작 여부를 정한다</summary>
        public bool HasItem { get; private set; }
 
        /// <summary>InventoryUI / HotbarUI 가 연결해준다 (슬롯, 좌클릭 여부)</summary>
        public event Action<InventorySlotUI, bool> OnClicked;
 
        private GameObject _outlineRoot;
        private ItemSO _lastWarnedItem;
        private Canvas _canvas;
 
        /// <summary>예전 코드 호환용. 가방 칸으로 취급한다</summary>
        public void Setup(int index) => Setup(SlotArea.Bag, index);
 
        public void Setup(SlotArea area, int index)
        {
            Area = area;
            Index = index;
 
            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
 
            if (autoOutline && _outlineRoot == null) BuildOutline();
            SetSelected(false);
 
            // 프리팹 연결 누락은 0번 칸에서만 한 번 알린다 (30번 도배 방지)
            if (index != 0) return;
 
            if (iconImage == null)
                Debug.LogError("[슬롯] Icon Image 가 연결되지 않았습니다. " +
                               "슬롯 프리팹의 InventorySlotUI 에서 Icon 을 드래그해 주세요.", this);
 
            if (countText == null)
                Debug.LogWarning("[슬롯] Count Text 가 연결되지 않아 개수가 표시되지 않습니다.", this);
        }
 
        public void SetSlot(ItemStack stack)
        {
            bool hasItem = stack != null && !stack.IsEmpty;
            HasItem = hasItem;
 
            if (iconImage != null)
            {
                iconImage.enabled = hasItem;
                iconImage.sprite = hasItem ? stack.item.icon : null;
                iconImage.color = (hasItem && tintIconByQuality)
                    ? ItemQualityUtil.TintColor(stack.quality)
                    : Color.white;
 
                // 아이콘이 비어있으면 흰 사각형으로만 보여서 "아무것도 없는 것"처럼 착각하기 쉽다
                if (hasItem && stack.item.icon == null && _lastWarnedItem != stack.item)
                {
                    _lastWarnedItem = stack.item;
                    Debug.LogWarning($"[슬롯] '{stack.item.DisplayName}' 에 Icon 이 비어있습니다. " +
                                     "ItemSO 에셋의 Icon 칸을 채워주세요.", stack.item);
                }
            }
 
            if (countText != null)
            {
                // 1개짜리는 숫자를 안 띄우는 게 깔끔하다
                bool showCount = hasItem && stack.count > 1;
                countText.enabled = showCount;
                if (showCount) countText.text = stack.count.ToString();
            }
 
            if (qualityText != null)
            {
                bool showQuality = hasItem && stack.quality != ItemQuality.Normal;
                qualityText.enabled = showQuality;
 
                if (showQuality)
                {
                    qualityText.text = ItemQualityUtil.Stars(stack.quality);
                    qualityText.color = ItemQualityUtil.TintColor(stack.quality);
                }
            }
        }
 
        public void SetSelected(bool on)
        {
            if (_outlineRoot != null) _outlineRoot.SetActive(on);
            if (selectedFrame != null) selectedFrame.SetActive(on);
        }
 
        public void OnPointerClick(PointerEventData eventData)
        {
            bool isLeft = eventData.button == PointerEventData.InputButton.Left;
            OnClicked?.Invoke(this, isLeft);
        }
 
        // ════════════════════════════════════════════════════════════
        //  드래그 앤 드롭
        //
        //  EventSystem 이 "누르고 안 움직임 = 클릭", "누르고 움직임 = 드래그" 로
        //  알아서 갈라주기 때문에, 좌클릭으로 손에 들기와 드래그가 서로 안 싸운다.
        // ════════════════════════════════════════════════════════════
 
        /// <summary>지금 끌고 있는 칸. 드롭 받는 쪽이 출발지를 알아야 해서 static 으로 둔다</summary>
        private static InventorySlotUI _dragSource;
 
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (!HasItem) return;
 
            PlayerInventory player = PlayerInventory.Instance;
            ItemStack stack = player != null ? player.GetSlot(Area, Index) : null;
            if (stack == null || stack.IsEmpty) return;
 
            _dragSource = this;
 
            Vector2 size = iconImage != null
                ? ((RectTransform)iconImage.transform).rect.size
                : new Vector2(64f, 64f);
 
            Color tint = tintIconByQuality ? ItemQualityUtil.TintColor(stack.quality) : Color.white;
 
            DragGhost.Show(stack.item.icon, tint, size, _canvas);
            DragGhost.Move(eventData.position);
        }
 
        public void OnDrag(PointerEventData eventData)
        {
            if (_dragSource != this) return;
 
            DragGhost.Move(eventData.position);
        }
 
        public void OnEndDrag(PointerEventData eventData)
        {
            // 슬롯 밖에 놓았으면 OnDrop 이 안 불리므로 여기서 정리한다
            DragGhost.Hide();
 
            if (_dragSource == this) _dragSource = null;
        }
 
        /// <summary>다른 칸을 이 칸 위에 놓았을 때</summary>
        public void OnDrop(PointerEventData eventData)
        {
            if (_dragSource == null || _dragSource == this) return;
 
            PlayerInventory player = PlayerInventory.Instance;
            if (player == null) return;
 
            player.MoveOrSwap(_dragSource.Area, _dragSource.Index, Area, Index);
 
            DragGhost.Hide();
            _dragSource = null;
        }
 
        // ════════════════════════════════════════════════════════════
        //  테두리 자동 생성 — 얇은 Image 4개로 사각 테두리를 만든다
        // ════════════════════════════════════════════════════════════
 
        private void BuildOutline()
        {
            _outlineRoot = new GameObject("SelectOutline", typeof(RectTransform));
            var root = (RectTransform)_outlineRoot.transform;
 
            root.SetParent(transform, false);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
 
            float t = outlineThickness;
 
            //          anchorMin      anchorMax      pivot            sizeDelta
            CreateBar(root, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, t)); // 위
            CreateBar(root, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0f), new Vector2(0, t)); // 아래
            CreateBar(root, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0f, 0.5f), new Vector2(t, 0)); // 왼쪽
            CreateBar(root, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1f, 0.5f), new Vector2(t, 0)); // 오른쪽
 
            _outlineRoot.SetActive(false);
        }
 
        private void CreateBar(RectTransform parent, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 sizeDelta)
        {
            var go = new GameObject("Bar", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
 
            rt.SetParent(parent, false);
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = pivot;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = Vector2.zero;
 
            var img = go.GetComponent<Image>();
            img.color = outlineColor;
            img.raycastTarget = false;   // 테두리가 클릭을 가로채면 안 된다
        }
    }
}
 