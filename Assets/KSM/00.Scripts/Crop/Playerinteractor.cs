using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using KSM._00.Scripts.Items;
 
namespace KSM._00.Scripts.Crop
{
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("비워두면 Camera.main 을 쓴다")]
        [SerializeField] private Camera cam;
 
        [Tooltip("씬의 PlacementPreview. 비워두면 자동으로 찾는다")]
        [SerializeField] private PlacementPreview preview;
 
        [Tooltip("씬의 GachaUI. 비워두면 자동으로 찾는다")]
        [SerializeField] private GachaUI gacha;
 
        [Tooltip("도구 애니메이션 담당. 비워두면 이 오브젝트에서 찾는다. 없어도 도구는 동작한다")]
        [SerializeField] private ToolAnimator toolAnimator;
 
        [Tooltip("도구 사용 중 캐릭터를 멈추는 데 쓴다. 비워두면 이 오브젝트에서 찾는다")]
        [SerializeField] private PlayerMovement movement;
 
        [Tooltip("그리드 밖 오브젝트(나무, 광석 등)를 찾을 레이어")]
        [SerializeField] private LayerMask interactableLayer;
 
        [Header("설정")]
        [Tooltip("플레이어로부터 이 거리 안쪽만 상호작용 가능 (월드 단위)")]
        [SerializeField] private float interactRange = 2.5f;
 
        [Tooltip("작물을 파내는 키")]
        [SerializeField] private Key digKey = Key.X;
 
        [Tooltip("켜면 다 자란 작물은 파내지지 않는다 (실수로 날리는 것 방지)")]
        [SerializeField] private bool protectMatureCrops = true;
 
        [SerializeField] private bool verboseLog = true;
 
        private void Awake()
        {
            if (cam == null) cam = Camera.main;
            if (preview == null) preview = FindFirstObjectByType<PlacementPreview>();
            if (gacha == null) gacha = FindFirstObjectByType<GachaUI>(FindObjectsInactive.Include);
            if (toolAnimator == null) toolAnimator = GetComponentInChildren<ToolAnimator>();
            if (movement == null) movement = GetComponent<PlayerMovement>();
        }
 
        private void Update()
        {
            if (Mouse.current == null) return;
 
            if (GachaUI.IsSpinning) { if (preview != null) preview.Hide(); return; }
 
            if (KSM._00.Scripts.Crafting.CraftingUI.IsOpen)
            {
                if (preview != null) preview.Hide();
                return;
            }
 
            UpdatePreview();
 
            if (IsPointerOverUI()) return;          
 
            if (Mouse.current.leftButton.wasPressedThisFrame)  HandleLeftClick();
            if (Mouse.current.rightButton.wasPressedThisFrame) HandleRightClick();
 
            if (Keyboard.current != null && Keyboard.current[digKey].wasPressedThisFrame) HandleDig();
        }
 
        private void OnDisable()
        {
            if (preview != null) preview.Hide();
        }
 
 
        private void UpdatePreview()
        {
            if (preview == null) return;
 
            if (IsPointerOverUI()) { preview.Hide(); return; }
 
            CropManager mgr = CropManager.Instance;
            if (mgr == null || cam == null) { preview.Hide(); return; }
 
            PlayerInventory player = PlayerInventory.Instance;
 
            ItemSO held = player != null && player.CanUseHeld ? player.HeldItem : null;
 
            Vector3Int cell = GetMouseCell(mgr);
            bool inRange = IsInRange(mgr, cell);
 
            // 도구 — 작용 범위를 그대로 보여준다
            if (held is ToolSO tool)
            {
                if (!tool.showPreview) { preview.Hide(); return; }
 
                ToolUseContext ctx = BuildToolContext(mgr, cell);
 
                if (tool.UsesHitBox)
                {
                    preview.ShowBox(tool.GetHitBoxCenter(ctx), tool.hitBoxSize, tool.CanUse(ctx));
                    return;
                }
 
                bool usable = inRange && tool.CanUse(ctx);
                preview.Show(tool.GetOrigin(cell), tool.areaSize, usable);
                return;
            }
 
            if (held is SeedSO seed && seed.IsPlantable)
            {
                Vector3Int origin = CropManager.GetOrigin(cell, seed.crop.size);
                bool ok = inRange && mgr.CanPlace(origin, seed.crop);
 
                preview.Show(origin, seed.crop.size, ok);
                return;
            }
 
            preview.Hide();
        }
 
 
        private void HandleLeftClick()
        {
            PlayerInventory player = PlayerInventory.Instance;
 
            if (player != null && player.CanUseHeld)
            {
                if (player.HeldItem is ItemPackSO pack) { HandleOpenPack(player, pack); return; }
                if (player.HeldItem is ToolSO tool) { HandleUseTool(tool); return; }
                SeedSO seed = GetHeldSeed();
                if (seed != null) { HandlePlant(seed); return; }
            }
            else if (player != null && IsUsableItem(player.HeldItem) && verboseLog)
            {
                Debug.Log($"[상호작용] {player.HeldItem.DisplayName} 은(는) 핫바에 올려야 쓸 수 있습니다");
            }
 
            HandleHarvest();
        }
 
        private static bool IsUsableItem(ItemSO item)
            => item is ToolSO || item is ItemPackSO || (item is SeedSO seed && seed.IsPlantable);
 
        private void HandleUseTool(ToolSO tool)
        {
            CropManager mgr = CropManager.Instance;
            if (mgr == null) return;
            if (toolAnimator != null && toolAnimator.IsBusy) return;
 
            Vector3Int cell = GetMouseCell(mgr);
            ToolUseContext ctx = BuildToolContext(mgr, cell);
            bool inRange = tool.UsesHitBox || IsInRange(mgr, cell);
 
            if (!inRange)
            {
                if (verboseLog) Debug.Log("[상호작용] 너무 멀다");
                return;
            }
            if (tool.lockMovementWhileUsing && movement != null)
                movement.LockFor(tool.LockSeconds);
 
            if (toolAnimator == null)
            {
                tool.Use(ctx);
                return;
            }
            float delay = toolAnimator.PlayUse(tool);
 
            if (delay <= 0f) tool.Use(ctx);
            else StartCoroutine(UseAfterDelay(tool, ctx, delay));
        }
 
        private IEnumerator UseAfterDelay(ToolSO tool, ToolUseContext ctx, float delay)
        {
            yield return new WaitForSeconds(delay);
            tool.Use(ctx);
        }
 
        private ToolUseContext BuildToolContext(CropManager mgr, Vector3Int cell)
        {
            Vector3 aim = GetMouseWorld();
            Vector3 self = transform.position;
            Vector2 facing = movement != null ? movement.FacingDirection : Vector2.right;
 
            return new ToolUseContext
            {
                cell = cell,
                worldPoint = mgr.CellToWorldCenter(cell),
                aimPoint = aim,
                facing = facing,
 
                user = gameObject,
                userPosition = self,
                farm = mgr,
                targetLayer = interactableLayer,
            };
        }
 
        private void HandleOpenPack(PlayerInventory player, ItemPackSO pack)
        {
            if (gacha == null)
            {
                Debug.LogWarning("[뽑기] 씬에 GachaUI 가 없습니다.");
                return;
            }
 
            if (!pack.IsUsable)
            {
                if (verboseLog) Debug.Log($"[뽑기] {pack.DisplayName} 에 Loot Table 이 비어있음");
                return;
            }
 
            // 결과를 받을 자리가 없으면 열지 않는다 (열고 나서 증발하면 최악)
            if (player.Inventory.Capacity > 0 && !player.HasFreeSlot())
            {
                Debug.LogWarning("[뽑기] 가방에 빈 칸이 없어 열 수 없습니다");
                return;
            }
 
            // 먼저 소모하고 연출을 돌린다 (연출 중 중복 사용 방지)
            if (!player.ConsumeHeld(1)) return;
 
            bool started = gacha.Open(pack, entry =>
            {
                if (entry.item == null) return;
                player.Add(entry.item, entry.RollCount(), entry.quality);
            });
 
            if (!started) player.Add(pack, 1);   // 못 열었으면 팩을 돌려준다
        }
 
        private void HandleDig()
        {
            CropManager mgr = CropManager.Instance;
            if (mgr == null) return;
 
            if (!TryGetTargetCell(mgr, out Vector3Int cell)) return;
 
            if (mgr.RemoveCropAt(cell, protectMatureCrops))
            {
                if (verboseLog) Debug.Log($"[파내기] {cell} 작물 제거");
                return;
            }
 
            if (!verboseLog) return;
 
            GrowCrop crop = mgr.GetOccupant(cell);
            Debug.Log(crop == null
                ? $"[파내기] {cell} 에 작물이 없음"
                : "[파내기] 다 자란 작물은 파낼 수 없음 (수확부터 하세요)");
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
        private static SeedSO GetHeldSeed()
        {
            PlayerInventory player = PlayerInventory.Instance;
            if (player == null) return null;
 
            return player.HeldItem is SeedSO seed && seed.IsPlantable ? seed : null;
        }
 
        private Vector3 GetMouseWorld()
        {
            Vector2 screen = Mouse.current.position.ReadValue();
            Vector3 world = cam.ScreenToWorldPoint(screen);
            world.z = 0f;
 
            return world;
        }
 
        private Vector3Int GetMouseCell(CropManager mgr) => mgr.WorldToCell(GetMouseWorld());
 
        private bool IsInRange(CropManager mgr, Vector3Int cell)
            => IsInRange(mgr.CellToWorldCenter(cell));
 
        private bool IsInRange(Vector3 worldPoint)
            => Vector2.Distance(transform.position, worldPoint) <= interactRange;
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
 