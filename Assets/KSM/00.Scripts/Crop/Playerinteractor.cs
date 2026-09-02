using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;   // 신버전 Input System 패키지
 
namespace KSM._00.Scripts.Crop
{
    /// <summary>
    /// 플레이어에 붙인다. 마우스로 타일을 찍어서 수확/심기를 수행하되,
    /// 플레이어로부터 일정 거리 안쪽만 허용한다.
    ///
    /// 좌클릭 = 수확 / 우클릭 = 심기
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("비워두면 Camera.main 을 쓴다")]
        [SerializeField] private Camera cam;
 
        [Tooltip("그리드 밖 오브젝트(나무, 광석 등)를 찾을 레이어")]
        [SerializeField] private LayerMask interactableLayer;
 
        [Header("설정")]
        [Tooltip("플레이어로부터 이 거리 안쪽만 상호작용 가능 (월드 단위)")]
        [SerializeField] private float interactRange = 2.5f;
 
        [Header("테스트용")]
        [Tooltip("우클릭으로 심을 작물. 나중에 인벤토리의 선택 아이템으로 교체")]
        [SerializeField] private CropSO selectedCrop;
 
        [SerializeField] private bool verboseLog = true;
 
        private void Awake()
        {
            if (cam == null) cam = Camera.main;
        }
 
        private void Update()
        {
            if (Mouse.current == null) return;
            if (IsPointerOverUI()) return;          // UI 위를 클릭한 건 무시
 
            if (Mouse.current.leftButton.wasPressedThisFrame)  HandleHarvest();
            if (Mouse.current.rightButton.wasPressedThisFrame) HandlePlant();
        }
 
        // ════════════════════════════════════════════════════════════
 
        private void HandleHarvest()
        {
            if (!TryGetTargetCell(out Vector3Int cell)) return;
 
            IHarvestable target = FindHarvestable(cell);
 
            if (target == null)
            {
                if (verboseLog) Debug.Log($"[수확] {cell} 에 아무것도 없음");
                return;
            }
 
            if (!target.CanHarvest)
            {
                if (verboseLog) Debug.Log($"[수확] 아직 안 됨 — {target.HarvestPrompt}");
                return;
            }
 
            target.TryHarvest();
        }
 
        private void HandlePlant()
        {
            if (selectedCrop == null)
            {
                if (verboseLog) Debug.Log("[심기] 선택된 작물이 없음");
                return;
            }
 
            if (!TryGetTargetCell(out Vector3Int cell)) return;
 
            bool ok = CropManager.Instance.TryPlant(cell, selectedCrop);
 
            if (!ok && verboseLog)
                Debug.Log($"[심기] {cell} 에 심을 수 없음 (다른 작물이 있거나, 심을 수 없는 타일)");
        }
 
        // ════════════════════════════════════════════════════════════
 
        /// <summary>마우스가 가리키는 칸을 구하고, 사거리 안인지까지 확인한다</summary>
        private bool TryGetTargetCell(out Vector3Int cell)
        {
            cell = default;
 
            var mgr = CropManager.Instance;
            if (mgr == null || cam == null) return false;
 
            Vector2 screen = Mouse.current.position.ReadValue();
            Vector3 world = cam.ScreenToWorldPoint(screen);
            world.z = 0f;
 
            cell = mgr.WorldToCell(world);
 
            Vector3 cellCenter = mgr.CellToWorldCenter(cell);
            if (Vector2.Distance(transform.position, cellCenter) > interactRange)
            {
                if (verboseLog) Debug.Log("[상호작용] 너무 멀다");
                return false;
            }
 
            return true;
        }
 
        /// <summary>
        /// 1순위: 그리드에 등록된 작물 (3x3이면 9칸 어디를 찍어도 잡힌다)
        /// 2순위: 그리드 밖 오브젝트 — 나무, 광석 등. IHarvestable만 구현하면 여기서 잡힌다
        /// </summary>
        private IHarvestable FindHarvestable(Vector3Int cell)
        {
            var mgr = CropManager.Instance;
 
            GrowCrop occupant = mgr.GetOccupant(cell);
            if (occupant != null) return occupant;
 
            Vector3 world = mgr.CellToWorldCenter(cell);
            Collider2D hit = Physics2D.OverlapPoint(world, interactableLayer);
            if (hit == null) return null;
 
            return hit.GetComponentInParent<IHarvestable>();
        }
 
        private static bool IsPointerOverUI()
            => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
 
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}