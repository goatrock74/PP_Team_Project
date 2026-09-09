using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
 
namespace KSM._00.Scripts.Items
{
    /// <summary>
    /// 획득 알림 한 줄. 오른쪽 밖에서 미끄러져 들어왔다가 잠시 후 사라진다.
    ///
    /// 프리팹 구조 — 바깥은 레이아웃이 잡고, <b>안쪽만 좌우로 움직인다</b>:
    ///   ToastRow        RectTransform + LayoutElement(Min Height 56) + PickupToast
    ///    └ Content      RectTransform + CanvasGroup      ← 이게 슬라이드·페이드 대상
    ///       ├ Background  Image
    ///       ├ Icon        Image
    ///       ├ Name        TextMeshPro
    ///       └ Count       TextMeshPro
    ///
    /// 바깥 ToastRow 를 직접 움직이면 Vertical Layout Group 이 매 프레임 되돌려 놓는다.
    /// 그래서 움직이는 대상을 한 겹 안쪽에 둔다.
    /// </summary>
    public class PickupToast : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("실제로 움직이고 사라질 안쪽 오브젝트")]
        [SerializeField] private RectTransform content;
 
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text countText;
 
        [Header("연출")]
        [Tooltip("이 거리만큼 오른쪽 밖에서 들어온다")]
        [SerializeField] private float slideDistance = 320f;
 
        [SerializeField, Min(0.05f)] private float slideInTime = 0.25f;
        [SerializeField, Min(0.1f)] private float holdTime = 2f;
        [SerializeField, Min(0.05f)] private float slideOutTime = 0.3f;
 
        /// <summary>지금 표시 중인 아이템. 같은 게 또 들어오면 합치는 데 쓴다</summary>
        public ItemSO Item { get; private set; }
 
        public ItemQuality Quality { get; private set; }
 
        /// <summary>화면에 떠 있는 중인가</summary>
        public bool IsActive { get; private set; }
 
        /// <summary>사라질 때 알린다. 매니저가 풀로 회수한다</summary>
        public event Action<PickupToast> OnFinished;
 
        private int _count;
        private Coroutine _routine;
 
        private void Awake()
        {
            if (content == null) content = (RectTransform)transform;
            if (group == null) group = content.GetComponent<CanvasGroup>();
        }
 
        // ════════════════════════════════════════════════════════════
 
        public void Show(ItemSO item, int count, ItemQuality quality)
        {
            Item = item;
            Quality = quality;
            _count = count;
 
            Refresh();
 
            gameObject.SetActive(true);
            IsActive = true;
 
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(Routine());
        }
 
        /// <summary>같은 아이템이 또 들어왔을 때. 숫자만 올리고 시간을 다시 잰다</summary>
        public void AddCount(int amount)
        {
            _count += amount;
            Refresh();
 
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(Routine(skipSlideIn: true));
        }
 
        // ════════════════════════════════════════════════════════════
 
        private void Refresh()
        {
            if (Item == null) return;
 
            if (iconImage != null)
            {
                iconImage.enabled = Item.icon != null;
                iconImage.sprite = Item.icon;
            }
 
            if (nameText != null)
            {
                nameText.text = Item.DisplayName;
                nameText.color = Quality != ItemQuality.Normal
                    ? ItemQualityUtil.TintColor(Quality)
                    : Color.white;
            }
 
            if (countText != null) countText.text = $"+{_count}";
        }
 
        private IEnumerator Routine(bool skipSlideIn = false)
        {
            if (!skipSlideIn)
            {
                yield return Slide(slideDistance, 0f, slideInTime, 0f, 1f, easeOut: true);
            }
            else
            {
                content.anchoredPosition = new Vector2(0f, content.anchoredPosition.y);
                if (group != null) group.alpha = 1f;
            }
 
            yield return new WaitForSeconds(holdTime);
 
            yield return Slide(0f, slideDistance, slideOutTime, 1f, 0f, easeOut: false);
 
            IsActive = false;
            gameObject.SetActive(false);
            _routine = null;
 
            OnFinished?.Invoke(this);
        }
 
        private IEnumerator Slide(float fromX, float toX, float time, float fromA, float toA, bool easeOut)
        {
            float t = 0f;
            float y = content.anchoredPosition.y;
 
            while (t < time)
            {
                t += Time.unscaledDeltaTime;   // 일시정지 중에도 알림은 흐른다
 
                float k = Mathf.Clamp01(t / time);
                float eased = easeOut ? 1f - Mathf.Pow(1f - k, 3f) : k * k;
 
                content.anchoredPosition = new Vector2(Mathf.Lerp(fromX, toX, eased), y);
                if (group != null) group.alpha = Mathf.Lerp(fromA, toA, eased);
 
                yield return null;
            }
 
            content.anchoredPosition = new Vector2(toX, y);
            if (group != null) group.alpha = toA;
        }
    }
}