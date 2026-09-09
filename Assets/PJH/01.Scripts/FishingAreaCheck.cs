using UnityEngine;
using KSM._00.Scripts;
 
namespace PJH.Scripts
{
    /// <summary>
    /// 찌가 떨어질 자리가 물인지 판정한다.
    ///
    /// ★ 원본에서 추가된 것 2가지
    ///   1) 기즈모 — 씬 뷰에 찌 지점이 <b>초록(물) / 빨강(물 아님)</b> 구슬로 보인다.
    ///      "여기서는 낚시할 수 없습니다" 가 뜰 때 눈으로 바로 확인할 수 있다.
    ///   2) 좌우 반전 — bobberPoint 는 고정된 자식이라 플레이어가 왼쪽을 봐도
    ///      찌 지점이 오른쪽에 그대로 남는다 (스프라이트만 flipX 되고
    ///      자식 Transform 은 안 따라가기 때문). 그래서 왼쪽 물가에서 낚시가 안 됐다.
    /// </summary>
    public class FishingAreaCheck : MonoBehaviour
    {
        [Tooltip("물 오브젝트가 올라가 있는 레이어. 여러 개 선택 가능")]
        [SerializeField] private LayerMask fishingAreaLayer;
 
        [Tooltip("찌가 떨어질 지점. 플레이어 앞쪽에 빈 오브젝트를 두면 된다")]
        [SerializeField] private Transform bobberPoint;
 
        [Header("좌우 반전")]
        [Tooltip("켜면 플레이어가 왼쪽을 볼 때 찌 지점도 왼쪽으로 뒤집힌다.\n" +
                 "끄면 원본처럼 항상 같은 쪽만 본다")]
        [SerializeField] private bool mirrorByFacing = true;
 
        [Tooltip("비우면 부모에서 자동으로 찾는다")]
        [SerializeField] private PlayerMovement movement;
 
        [Tooltip("반전의 기준이 되는 축. 비우면 이 오브젝트")]
        [SerializeField] private Transform pivot;
 
        private void Awake()
        {
            if (movement == null) movement = GetComponentInParent<PlayerMovement>();
            if (pivot == null) pivot = transform;
        }
 
        /// <summary>찌가 실제로 떨어질 월드 좌표 (좌우 반전까지 반영된 값)</summary>
        public Vector3 FishingPointPosition
        {
            get
            {
                if (bobberPoint == null) return transform.position;
 
                Vector3 p = bobberPoint.position;
 
                if (!mirrorByFacing || movement == null || movement.FacingX >= 0f) return p;
 
                // 왼쪽을 보고 있으면 기준축을 중심으로 좌우를 뒤집는다
                float axis = pivot != null ? pivot.position.x : transform.position.x;
                p.x = axis - (p.x - axis);
 
                return p;
            }
        }
 
        public bool IsFishingLayer()
        {
            Collider2D result = Physics2D.OverlapPoint(FishingPointPosition, fishingAreaLayer);
 
            return result != null;
        }
 
        // ════════════════════════════════════════════════════════════
 
        /// <summary>
        /// 씬 뷰에 찌 지점을 그린다. 플레이 중에도 보이니까
        /// 물 위에 제대로 올라가는지 눈으로 확인하면 된다.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (bobberPoint == null) return;
 
            Vector3 p = FishingPointPosition;
 
            bool ok = Application.isPlaying && IsFishingLayer();
            Gizmos.color = ok ? Color.green : Color.red;
 
            Gizmos.DrawWireSphere(p, 0.15f);
            Gizmos.DrawLine(transform.position, p);
        }
    }
}