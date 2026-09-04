using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
 
namespace KSM._00.Scripts.Items
{
   
    public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
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
        public event Action<int, bool> OnClicked;
 
        private GameObject _outlineRoot;
        private ItemSO _lastWarnedItem;
 
        public void Setup(int index)
        {
            Index = index;
 
            if (autoOutline && _outlineRoot == null) BuildOutline();
            SetSelected(false);
            
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
 
            if (iconImage != null)
            {
                iconImage.enabled = hasItem;
                iconImage.sprite = hasItem ? stack.item.icon : null;
                iconImage.color = (hasItem && tintIconByQuality)
                    ? ItemQualityUtil.TintColor(stack.quality)
                    : Color.white;
 
                if (hasItem && stack.item.icon == null && _lastWarnedItem != stack.item)
                {
                    _lastWarnedItem = stack.item;
                    Debug.LogWarning($"[슬롯] '{stack.item.DisplayName}' 에 Icon 이 비어있습니다. " +
                                     "ItemSO 에셋의 Icon 칸을 채워주세요.", stack.item);
                }
            }
 
            if (countText != null)
            {
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
            OnClicked?.Invoke(Index, isLeft);
        }
 
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
            img.raycastTarget = false;  
        }
    }
}