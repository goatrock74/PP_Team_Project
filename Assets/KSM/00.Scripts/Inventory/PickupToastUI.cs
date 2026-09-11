using System.Collections.Generic;
using UnityEngine;
 
namespace KSM._00.Scripts.Items
{
    public class PickupToastUI : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("알림 줄이 쌓일 곳. Vertical Layout Group 이 붙어 있어야 한다")]
        [SerializeField] private Transform container;
 
        [SerializeField] private PickupToast toastPrefab;
 
        [Header("설정")]
        [Tooltip("동시에 보여줄 최대 줄 수. 넘으면 가장 오래된 걸 재사용한다")]
        [SerializeField, Range(1, 10)] private int maxVisible = 5;
 
        [Tooltip("품질이 달라도 같은 아이템이면 한 줄로 합친다")]
        [SerializeField] private bool mergeAcrossQuality;
 
        [Tooltip("켜면 새 알림이 맨 아래에 붙고 기존 알림이 위로 밀린다 (아래→위로 쌓임).\n" +
                 "끄면 새 알림이 맨 위에 붙는다 (위→아래로 쌓임).\n" +
                 "이건 형제 순서만 바꾼다. 실제로 위로 밀리려면 ToastArea 의 Pivot 이 아래여야 한다")]
        [SerializeField] private bool newestAtBottom = true;
 
        private readonly List<PickupToast> _pool = new();
        private readonly List<PickupToast> _active = new();
 
        private PlayerInventory _player;
 
        private void Awake()
        {
            if (container == null) container = transform;
        }
 
        private void OnEnable()
        {
            _player = PlayerInventory.Instance;
 
            if (_player == null)
            {
                Debug.LogWarning("[획득알림] 씬에 PlayerInventory 가 없습니다.", this);
                return;
            }
 
            _player.OnItemGained += HandleItemGained;
        }
 
        private void OnDisable()
        {
            if (_player != null) _player.OnItemGained -= HandleItemGained;
            _player = null;
        }
 
        private void HandleItemGained(ItemSO item, int count, ItemQuality quality)
        {
            if (item == null || count <= 0) return;
            PickupToast existing = FindActive(item, quality);
            if (existing != null)
            {
                existing.AddCount(count);
                return;
            }
 
            PickupToast toast = Rent();
            if (toast == null) return;
            if (newestAtBottom) toast.transform.SetAsLastSibling();
            else toast.transform.SetAsFirstSibling();
 
            toast.Show(item, count, quality);
 
            _active.Add(toast);
        }
 
        private PickupToast FindActive(ItemSO item, ItemQuality quality)
        {
            foreach (PickupToast t in _active)
            {
                if (t == null || !t.IsActive || t.Item != item) continue;
                if (!mergeAcrossQuality && t.Quality != quality) continue;
 
                return t;
            }
 
            return null;
        }
 
 
        private PickupToast Rent()
        {
            if (toastPrefab == null)
            {
                Debug.LogError("[획득알림] Toast Prefab 이 비어있습니다.", this);
                return null;
            }
 
            foreach (PickupToast t in _pool)
                if (t != null && !t.IsActive) return t;
 
            if (_active.Count >= maxVisible && _active.Count > 0)
            {
                PickupToast oldest = _active[0];
                _active.RemoveAt(0);
                return oldest;
            }
 
            PickupToast created = Instantiate(toastPrefab, container);
            created.gameObject.SetActive(false);
            created.OnFinished += Release;
 
            _pool.Add(created);
            return created;
        }
 
        private void Release(PickupToast toast) => _active.Remove(toast);
    }
}
 