using UnityEngine;
 
namespace KSM._00.Scripts.Crop
{
    /// <summary>
    /// Y좌표로 그리기 순서를 정한다. 화면 아래에 있을수록 앞에 그려진다.
    /// 작물, 나무, 바위, 플레이어, NPC 등 "땅 위에 서 있는" 모든 것에 붙이면 된다.
    ///
    /// 전제 조건 두 가지:
    ///   1) 스프라이트 Pivot 이 Bottom 이어야 한다 (기준점 = 밑동)
    ///   2) 바닥 타일맵은 별도의 낮은 Sorting Layer 에 있어야 한다
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [DisallowMultipleComponent]
    public class YSorter : MonoBehaviour
    {
        [Tooltip("1유닛당 정렬 단계 수. 클수록 촘촘하지만 맵이 크면 넘칠 수 있다")]
        [SerializeField] private int precision = 100;
 
        [Tooltip("움직이는 오브젝트만 켠다. 작물처럼 고정된 건 끈 채로 둘 것")]
        [SerializeField] private bool continuous;
 
        [Tooltip("밑동이 스프라이트 원점과 다를 때 보정 (월드 단위)")]
        [SerializeField] private float yOffset;
 
        private SpriteRenderer _renderer;
 
        // sortingOrder 는 내부적으로 16비트다. 넘치면 정렬이 통째로 뒤집힌다
        private const int MinOrder = -32000;
        private const int MaxOrder = 32000;
 
        private void Awake() => _renderer = GetComponent<SpriteRenderer>();
 
        private void OnEnable() => Apply();
 
        private void LateUpdate()
        {
            if (continuous) Apply();
        }
 
        /// <summary>위치를 옮긴 직후 직접 불러도 된다</summary>
        public void Apply()
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
 
            float y = transform.position.y + yOffset;
            int order = Mathf.RoundToInt(-y * precision);
 
            _renderer.sortingOrder = Mathf.Clamp(order, MinOrder, MaxOrder);
        }
 
#if UNITY_EDITOR
        private void OnValidate()
        {
            precision = Mathf.Max(1, precision);
            if (Application.isPlaying) Apply();
        }
#endif
    }
}