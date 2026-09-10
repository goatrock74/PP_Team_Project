using System.Collections;
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
 
            // 뽑기 패널은 보통 꺼진 채로 시작하므로 비활성 오브젝트까지 뒤져야 찾는다
            if (gacha == null) gacha = FindFirstObjectByType<GachaUI>(FindObjectsInactive.Include);
            if (toolAnimator == null) toolAnimator = GetComponentInChildren<ToolAnimator>();
            if (movement == null) movement = GetComponent<PlayerMovement>();
        }
 
        private void Update()
        {
            if (Mouse.current == null) return;
 
            if (GachaUI.IsSpinning) { if (preview != null) preview.Hide(); return; }
 
            // 제작창이 열려 있으면 뒤쪽 세상은 클릭을 안 받는다
            if (KSM._00.Scripts.Crafting.CraftingUI.IsOpen)
            {
                if (preview != null) preview.Hide();
                return;
            }
 
            UpdatePreview();
 
            if (IsPointerOverUI()) return;          // UI 위 클릭은 무시
 
            if (Mouse.current.leftButton.wasPressedThisFrame)  HandleLeftClick();
            if (Mouse.current.rightButton.wasPressedThisFrame) HandleRightClick();
 
            if (Keyboard.current != null && Keyboard.current[digKey].wasPressedThisFrame) HandleDig();
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
 
            if (IsPointerOverUI()) { preview.Hide(); return; }
 
            CropManager mgr = CropManager.Instance;
            if (mgr == null || cam == null) { preview.Hide(); return; }
 
            PlayerInventory player = PlayerInventory.Instance;
 
            // 쓸 수 없는 것(가방에 있는 것)은 미리보기도 안 띄운다.
            // 초록 네모가 떴는데 클릭이 안 되면 더 헷갈린다
            ItemSO held = player != null && player.CanUseHeld ? player.HeldItem : null;
 
            Vector3Int cell = GetMouseCell(mgr);
            bool inRange = IsInRange(mgr, cell);
 
            // 도구 — 작용 범위를 그대로 보여준다
            if (held is ToolSO tool)
            {
                // 낚싯대처럼 범위 개념이 없는 도구는 아무것도 안 그린다
                if (!tool.showPreview) { preview.Hide(); return; }
 
                ToolUseContext ctx = BuildToolContext(mgr, cell);
 
                if (tool.UsesHitBox)
                {
                    // 상자형(도끼·낫) — 플레이어 정면 앞에 판정 상자를 그린다.
                    // 상자가 몸에 붙어 있으므로 사거리 제한은 필요 없다
                    preview.ShowBox(tool.GetHitBoxCenter(ctx), tool.hitBoxSize, tool.CanUse(ctx));
                    return;
                }
 
                bool usable = inRange && tool.CanUse(ctx);
                preview.Show(tool.GetOrigin(cell), tool.areaSize, usable);
                return;
            }
 
            // 씨앗 — 심을 자리를 보여준다
            if (held is SeedSO seed && seed.IsPlantable)
            {
                Vector3Int origin = CropManager.GetOrigin(cell, seed.crop.size);
                bool ok = inRange && mgr.CanPlace(origin, seed.crop);
 
                preview.Show(origin, seed.crop.size, ok);
                return;
            }
 
            preview.Hide();
        }
 
        // ════════════════════════════════════════════════════════════
        //  입력 처리
        // ════════════════════════════════════════════════════════════
 
        private void HandleLeftClick()
        {
            PlayerInventory player = PlayerInventory.Instance;
 
            // ★ 핫바에 올려둔 것만 실제로 쓸 수 있다.
            //   가방에 있는 건 정보 확인·정리용이라 세상에 영향을 주지 않는다
            if (player != null && player.CanUseHeld)
            {
                // 1) 뽑기 팩을 들고 있으면 룰렛
                if (player.HeldItem is ItemPackSO pack) { HandleOpenPack(player, pack); return; }
 
                // 2) 도구를 들고 있으면 도구 사용 (괭이·물뿌리개·도끼·낫·낚싯대)
                if (player.HeldItem is ToolSO tool) { HandleUseTool(tool); return; }
 
                // 3) 씨앗을 들고 있으면 심기
                SeedSO seed = GetHeldSeed();
                if (seed != null) { HandlePlant(seed); return; }
            }
            else if (player != null && IsUsableItem(player.HeldItem) && verboseLog)
            {
                Debug.Log($"[상호작용] {player.HeldItem.DisplayName} 은(는) 핫바에 올려야 쓸 수 있습니다");
            }
 
            // 4) 아니면 수확
            HandleHarvest();
        }
 
        /// <summary>핫바에 올렸을 때 실제로 동작하는 종류인가 (안내 메시지용)</summary>
        private static bool IsUsableItem(ItemSO item)
            => item is ToolSO || item is ItemPackSO || (item is SeedSO seed && seed.IsPlantable);
 
        private void HandleUseTool(ToolSO tool)
        {
            CropManager mgr = CropManager.Instance;
            if (mgr == null) return;
 
            // 아직 휘두르는 중이면 무시 (연타 방지)
            if (toolAnimator != null && toolAnimator.IsBusy) return;
 
            Vector3Int cell = GetMouseCell(mgr);
            ToolUseContext ctx = BuildToolContext(mgr, cell);
 
            // 상자형은 플레이어 몸에 붙어 있어서 사거리 제한이 없다
            bool inRange = tool.UsesHitBox || IsInRange(mgr, cell);
 
            if (!inRange)
            {
                if (verboseLog) Debug.Log("[상호작용] 너무 멀다");
                return;
            }
 
            // 발을 딛고 하는 동작이면 그동안 캐릭터를 묶어둔다
            if (tool.lockMovementWhileUsing && movement != null)
                movement.LockFor(tool.LockSeconds);
 
            if (toolAnimator == null)
            {
                tool.Use(ctx);
                return;
            }
 
            // 애니메이션을 먼저 재생하고, 도구가 대상에 닿는 타이밍에 효과를 낸다
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
 
            // 정면은 이동 입력의 좌우가 정한다. 마우스는 방향에 관여하지 않는다
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
 
        /// <summary>마우스가 가리키는 월드 좌표 (칸에 스냅되지 않은 원본)</summary>
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
 