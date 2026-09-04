using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using KSM._00.Scripts.Items;
 
namespace KSM._00.Scripts.Crop
{
    /// <summary>
    /// 플레이어에 붙인다. 마우스로 타일을 찍어서 심기/수확을 수행하되,
    /// 플레이어로부터 일정 거리 안쪽만 허용한다.
    ///
    ///   좌클릭 : 씨앗을 들고 있으면 심기, 아니면 수확
    ///   우클릭 : 손에 든 것 놓기
    ///
    /// 씨앗을 들고 있는 동안에는 마우스 자리에 심기 미리보기가 뜬다.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("비워두면 Camera.main 을 쓴다")]
        [SerializeField] private Camera cam;
 
        [Tooltip("씬의 PlacementPreview. 비워두면 자동으로 찾는다")]
        [SerializeField] private PlacementPreview preview;
 
        [Tooltip("그리드 밖 오브젝트(나무, 광석 등)를 찾을 레이어")]
        [SerializeField] private LayerMask interactableLayer;
 
        [Header("설정")]
        [Tooltip("플레이어로부터 이 거리 안쪽만 상호작용 가능 (월드 단위)")]
        [SerializeField] private float interactRange = 2.5f;
 
        [SerializeField] private bool verboseLog = true;
 
        private void Awake()
        {
            if (cam == null) cam = Camera.main;
            if (preview == null) preview = FindFirstObjectByType<PlacementPreview>();
        }
 
        private void Update()
        {
            if (Mouse.current == null) return;
 
            UpdatePreview();
 
            if (IsPointerOverUI()) return;          // UI 위 클릭은 무시
 
            if (Mouse.current.leftButton.wasPressedThisFrame)  HandleLeftClick();
            if (Mouse.current.rightButton.wasPressedThisFrame) HandleRightClick();
        }
 
        private void OnDisable()
        {
            if (preview != null) preview.Hide();
        }
 
        // ════════════════════════════════════════════════════════════
        //  미리보기
        // ════════════════════════════════════════════════════════════
 
        private void UpdatePreview()
        {
            if (preview == null) return;
 
            SeedSO seed = GetHeldSeed();
 
            if (seed == null || IsPointerOverUI())
            {
                preview.Hide();
                return;
            }
 
            CropManager mgr = CropManager.Instance;
            if (mgr == null || cam == null) { preview.Hide(); return; }
 
            Vector3Int cell = GetMouseCell(mgr);
            Vector3Int origin = CropManager.GetOrigin(cell, seed.crop.size);
 
            // 사거리 안이고, 9칸이 전부 비어있고 심을 수 있는 타일이어야 초록
            bool ok = IsInRange(mgr, cell) && mgr.CanPlace(origin, seed.crop);
 
            preview.Show(origin, seed.crop.size, ok);
        }
 
        // ════════════════════════════════════════════════════════════
        //  입력 처리
        // ════════════════════════════════════════════════════════════
 
        private void HandleLeftClick()
        {
            SeedSO seed = GetHeldSeed();
 
            if (seed != null) HandlePlant(seed);
            else HandleHarvest();
        }
 
        private void HandleRightClick()
        {
            PlayerInventory player = PlayerInventory.Instance;
            if (player != null && player.HasHeldItem) player.ClearHeld();
        }
 
        private void HandlePlant(SeedSO seed)
        {
            CropManager mgr = CropManager.Instance;
            if (mgr == null) return;
 
            if (!TryGetTargetCell(mgr, out Vector3Int cell)) return;
 
            if (!mgr.TryPlant(cell, seed.crop))
            {
                if (verboseLog) Debug.Log($"[심기] {cell} 에 심을 수 없음 (자리가 찼거나 심을 수 없는 타일)");
                return;
            }
 
            // 심었으니 씨앗 한 개 소모. 다 쓰면 손이 자동으로 비워진다
            PlayerInventory.Instance.ConsumeHeld(1);
 
            if (verboseLog) Debug.Log($"[심기] {seed.crop.cropName} 심음 @ {cell}");
        }
 
        private void HandleHarvest()
        {
            CropManager mgr = CropManager.Instance;
            if (mgr == null) return;
 
            if (!TryGetTargetCell(mgr, out Vector3Int cell)) return;
 
            IHarvestable target = FindHarvestable(mgr, cell);
 
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
 
        // ════════════════════════════════════════════════════════════
        //  도우미
        // ════════════════════════════════════════════════════════════
 
        /// <summary>손에 든 게 심을 수 있는 씨앗이면 반환, 아니면 null</summary>
        private static SeedSO GetHeldSeed()
        {
            PlayerInventory player = PlayerInventory.Instance;
            if (player == null) return null;
 
            return player.HeldItem is SeedSO seed && seed.IsPlantable ? seed : null;
        }
 
        private Vector3Int GetMouseCell(CropManager mgr)
        {
            Vector2 screen = Mouse.current.position.ReadValue();
            Vector3 world = cam.ScreenToWorldPoint(screen);
            world.z = 0f;
 
            return mgr.WorldToCell(world);
        }
 
        private bool IsInRange(CropManager mgr, Vector3Int cell)
        {
            Vector3 center = mgr.CellToWorldCenter(cell);
            return Vector2.Distance(transform.position, center) <= interactRange;
        }
 
        /// <summary>마우스가 가리키는 칸을 구하고, 사거리 안인지까지 확인한다</summary>
        private bool TryGetTargetCell(CropManager mgr, out Vector3Int cell)
        {
            cell = default;
            if (cam == null) return false;
 
            cell = GetMouseCell(mgr);
 
            if (!IsInRange(mgr, cell))
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
        private IHarvestable FindHarvestable(CropManager mgr, Vector3Int cell)
        {
            GrowCrop occupant = mgr.GetOccupant(cell);
            if (occupant != null) return occupant;
 
            Vector3 world = mgr.CellToWorldCenter(cell);
            Collider2D hit = Physics2D.OverlapPoint(world, interactableLayer);
 
            return hit != null ? hit.GetComponentInParent<IHarvestable>() : null;
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
 