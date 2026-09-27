using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using KSM._00.Scripts.Effects;
using KSM._00.Scripts.Items;
using PJH._01.Scripts;

namespace KSM._00.Scripts.Crop
{
    /// <summary>
    /// 마우스 클릭으로 심기·수확·도구 사용·뽑기 팩 열기를 처리한다.
    ///
    /// ── 작물 제거 모드 ──
    ///   괭이를 핫바에 들고 X → 제거 모드 켜짐 (화면에 안내가 뜬다)
    ///   이 상태에서 작물을 좌클릭 → 그 작물 하나만 뽑힌다 (밭은 갈리지 않는다)
    ///   X 를 다시 누르거나, 괭이가 아닌 걸 들면 → 제거 모드 꺼짐
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        /// <summary>지금 작물 제거 모드인가. UI 가 이걸 보고 안내를 띄운다</summary>
        public static bool IsRemoveMode { get; private set; }

        /// <summary>제거 모드가 켜지거나 꺼질 때 (켜짐 = true)</summary>
        public static event System.Action<bool> RemoveModeChanged;

        // 도메인 리로드를 끈 설정에서도 이전 플레이의 값이 남지 않게
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsRemoveMode = false;
            RemoveModeChanged = null;
        }

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

        [Header("작물 제거 모드")]
        [Tooltip("괭이를 든 상태에서 이 키로 제거 모드를 켜고 끈다")]
        [FormerlySerializedAs("digKey")]
        [SerializeField] private Key removeModeKey = Key.X;

        [Tooltip("켜면 다 자란 작물은 제거 모드에서도 안 뽑힌다 (수확부터 해야 함).\n" +
                 "끄면 다 자란 작물도 뽑힌다")]
        [SerializeField] private bool protectMatureCrops = true;

        [Tooltip("뽑을 때 괭이 휘두르는 애니메이션을 재생하고, 괭이가 땅에 닿는 순간 뽑는다")]
        [SerializeField] private bool swingOnRemove = true;

        [Tooltip("괭이를 안 들고 X 를 눌렀을 때 화면에 띄울 문구")]
        [SerializeField] private string needHoeMessage = "괭이를 들어야 작물을 뽑을 수 있습니다";

        [Tooltip("다 자란 작물을 뽑으려 할 때 화면에 띄울 문구")]
        [SerializeField] private string protectedMessage = "다 자란 작물은 수확부터 하세요";

        [SerializeField] private bool verboseLog = true;

        [Header("낚시 미끼 UI")]
        [Tooltip("비워두면 씬에서 자동으로 찾는다")]
        [SerializeField] private BaitConfirmUI baitConfirmUI;

        private void Awake()
        {
            if (cam == null) cam = Camera.main;
            if (preview == null) preview = FindFirstObjectByType<PlacementPreview>();
            if (gacha == null) gacha = FindFirstObjectByType<GachaUI>(FindObjectsInactive.Include);
            if (toolAnimator == null) toolAnimator = GetComponentInChildren<ToolAnimator>();
            if (movement == null) movement = GetComponent<PlayerMovement>();

            if (baitConfirmUI == null)
                baitConfirmUI = FindFirstObjectByType<BaitConfirmUI>(FindObjectsInactive.Include);

            // ★ cam 이 없으면 마우스 좌표를 월드로 못 바꾼다.
            //   Camera.main 은 MainCamera 태그가 붙은 카메라만 찾으므로 여기서 미리 알려준다
            if (cam == null)
                Debug.LogError("[상호작용] 카메라를 찾지 못했습니다. " +
                               "Cam 칸에 카메라를 연결하거나, 카메라에 MainCamera 태그를 붙여주세요.", this);
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

            // ★ 모드 키는 마우스가 핫바 같은 UI 위에 있어도 받아야 한다.
            //   그래서 IsPointerOverUI 검사보다 먼저 본다
            if (removeModeKey != Key.None && Keyboard.current != null &&
                Keyboard.current[removeModeKey].wasPressedThisFrame)
                ToggleRemoveMode();

            // 괭이를 내려놓거나 다른 걸 들면 제거 모드도 끝난다.
            // 안 그러면 씨앗을 든 채로 클릭했는데 작물이 뽑히는 사고가 난다
            if (IsRemoveMode && !IsHoldingHoe()) SetRemoveMode(false);

            UpdatePreview();

            if (IsPointerOverUI()) return;

            if (Mouse.current.leftButton.wasPressedThisFrame)  HandleLeftClick();
            if (Mouse.current.rightButton.wasPressedThisFrame) HandleRightClick();
        }

        private void OnDisable()
        {
            if (preview != null) preview.Hide();

            // 플레이어가 꺼지면 모드와 안내 UI 도 같이 끈다
            SetRemoveMode(false);
        }

        private void UpdatePreview()
        {
            if (preview == null) return;

            if (IsPointerOverUI()) { preview.Hide(); return; }

            CropManager mgr = CropManager.Instance;
            if (mgr == null || cam == null) { preview.Hide(); return; }

            if (IsRemoveMode)
            {
                ShowRemovePreview(mgr);
                return;
            }

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
            // 제거 모드에서는 클릭이 "뽑기" 가 된다. 밭을 갈거나 수확하지 않는다
            if (IsRemoveMode)
            {
                HandleRemoveClick();
                return;
            }

            PlayerInventory player = PlayerInventory.Instance;

            if (player != null && player.CanUseHeld)
            {
                // 1. 손에 들고 있는 아이템이 미끼인지 먼저 검사
                if (player.HeldItem is FishingBaitDataSO bait)
                {
                    HandleUseBait(player, bait);
                    return;
                }

                // 2. 뽑기 팩 검사
                if (player.HeldItem is ItemPackSO pack)
                {
                    HandleOpenPack(player, pack);
                    return;
                }

                // 3. 도구 검사
                if (player.HeldItem is ToolSO tool)
                {
                    HandleUseTool(tool);
                    return;
                }

                // 4. 씨앗 검사
                SeedSO seed = GetHeldSeed();

                if (seed != null)
                {
                    HandlePlant(seed);
                    return;
                }
            }
            else if (player != null && IsUsableItem(player.HeldItem) && verboseLog)
            {
                Debug.Log($"[상호작용] {player.HeldItem.DisplayName}은(는) 핫바에 올려야 쓸 수 있습니다.");
            }

            HandleHarvest();
        }

        /// <summary>
        /// 미끼를 손에 들고 클릭했을 때. 실제 소모는 확인창에서 한다.
        ///
        /// ★ 예전엔 baitConfirmUI 가 null 이면 여기서 NullReferenceException 이 났다.
        ///   예외가 나면 그 뒤 프레임 처리가 통째로 끊기므로 반드시 막아야 한다
        /// </summary>
        private void HandleUseBait(PlayerInventory player, FishingBaitDataSO bait)
        {
            if (baitConfirmUI == null)
            {
                Debug.LogError("[낚시] 씬에 BaitConfirmUI 가 없습니다. " +
                               "PlayerInteractor 의 Bait Confirm UI 칸에 연결해주세요.", this);
                return;
            }

            if (verboseLog) Debug.Log($"[낚시] {bait.DisplayName} 사용 확인창을 엽니다.");

            baitConfirmUI.Open(player, bait);
        }

        private static bool IsUsableItem(ItemSO item)
            => item is ToolSO
            || item is ItemPackSO
            || item is FishingBaitDataSO
            || (item is SeedSO seed && seed.IsPlantable);

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
                UseTool(tool, ctx);
                return;
            }

            float delay = toolAnimator.PlayUse(tool);

            if (delay <= 0f) UseTool(tool, ctx);
            else StartCoroutine(UseAfterDelay(tool, ctx, delay));
        }

        private IEnumerator UseAfterDelay(ToolSO tool, ToolUseContext ctx, float delay)
        {
            yield return new WaitForSeconds(delay);
            UseTool(tool, ctx);
        }

        /// <summary>
        /// 도구를 실제로 쓰고, 결과에 맞는 파티클을 튀긴다 (흙·물방울·나뭇조각·풀잎).
        /// 쓰기 전 상태를 먼저 기억해 둬야 '어느 칸이 새로 갈렸는지', '어느 나무를 쳤는지' 를 알 수 있다
        /// </summary>
        private static void UseTool(ToolSO tool, ToolUseContext ctx)
        {
            ToolFX.Shot shot = ToolFX.BeforeUse(tool, in ctx);
            bool used = tool.Use(ctx);
            ToolFX.AfterUse(shot, used);
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

        // ════════════════════════════════════════════════════════════
        //  작물 제거 모드
        // ════════════════════════════════════════════════════════════

        private void ToggleRemoveMode()
        {
            if (IsRemoveMode)
            {
                SetRemoveMode(false);
                return;
            }

            if (!IsHoldingHoe())
            {
                ScreenMessageUI.Show(needHoeMessage);
                return;
            }

            SetRemoveMode(true);
        }

        private void SetRemoveMode(bool on)
        {
            if (IsRemoveMode == on) return;

            IsRemoveMode = on;
            RemoveModeChanged?.Invoke(on);

            if (verboseLog) Debug.Log($"[제거] 작물 제거 모드 {(on ? "켜짐" : "꺼짐")}");
        }

        /// <summary>괭이를 핫바에서 손에 들고 있는가</summary>
        private static bool IsHoldingHoe()
        {
            PlayerInventory player = PlayerInventory.Instance;
            return player != null && player.CanUseHeld && player.HeldItem is HoeSO;
        }

        /// <summary>제거 모드에서 클릭 — 클릭한 칸의 작물 하나를 뽑는다</summary>
        private void HandleRemoveClick()
        {
            CropManager mgr = CropManager.Instance;
            if (mgr == null) return;

            // 휘두르는 중에 또 누르면 무시 (연타로 여러 개 뽑히는 것 방지)
            if (toolAnimator != null && toolAnimator.IsBusy) return;

            if (!TryGetTargetCell(mgr, out Vector3Int cell)) return;

            GrowCrop crop = mgr.GetOccupant(cell);

            if (crop == null)
            {
                if (verboseLog) Debug.Log($"[제거] {cell} 에 작물이 없음");
                return;
            }

            if (protectMatureCrops && crop.CanHarvest)
            {
                ScreenMessageUI.Show(protectedMessage);
                return;
            }

            HoeSO hoe = PlayerInventory.Instance != null ? PlayerInventory.Instance.HeldItem as HoeSO : null;

            // 괭이를 휘두르고, 땅에 닿는 순간 뽑는다
            if (swingOnRemove && hoe != null && toolAnimator != null)
            {
                if (hoe.lockMovementWhileUsing && movement != null)
                    movement.LockFor(hoe.LockSeconds);

                float delay = toolAnimator.PlayUse(hoe);

                if (delay > 0f)
                {
                    StartCoroutine(RemoveAfterDelay(cell, delay));
                    return;
                }
            }

            RemoveCropNow(cell);
        }

        private IEnumerator RemoveAfterDelay(Vector3Int cell, float delay)
        {
            yield return new WaitForSeconds(delay);
            RemoveCropNow(cell);
        }

        private void RemoveCropNow(Vector3Int cell)
        {
            CropManager mgr = CropManager.Instance;
            if (mgr == null) return;

            // 파티클 위치는 뽑기 전에 기억해 둔다 (뽑으면 작물이 사라지니까)
            GrowCrop crop = mgr.GetOccupant(cell);
            Vector3 fxPos = crop != null ? crop.transform.position : mgr.CellToWorldCenter(cell);

            // 휘두르는 사이에 작물이 다 자랐거나 이미 없어졌으면 RemoveCropAt 이 false 를 준다
            if (mgr.RemoveCropAt(cell, protectMatureCrops))
            {
                ToolFX.CropRemoved(fxPos);
                if (verboseLog) Debug.Log($"[제거] {cell} 작물을 뽑았습니다");
                return;
            }

            if (verboseLog) Debug.Log($"[제거] {cell} 작물을 뽑지 못했습니다 (이미 없거나 다 자람)");
        }

        /// <summary>
        /// 제거 모드일 때 마우스 아래 작물에 상자를 띄운다.
        /// 초록 = 뽑을 수 있음 / 빨강 = 작물이 없거나, 너무 멀거나, 다 자라서 보호 중
        /// 2x2 같은 큰 작물은 작물 전체를 덮는다
        /// </summary>
        private void ShowRemovePreview(CropManager mgr)
        {
            Vector3Int cell = GetMouseCell(mgr);
            GrowCrop crop = mgr.GetOccupant(cell);

            if (crop == null)
            {
                preview.Show(cell, Vector2Int.one, false);
                return;
            }

            bool ok = IsInRange(mgr, cell) && !(protectMatureCrops && crop.CanHarvest);
            Vector2Int size = crop.Data != null ? crop.Data.size : Vector2Int.one;

            preview.Show(crop.OriginCell, size, ok);
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

            // 심은 자리에 흙이 살짝 튄다
            GrowCrop planted = mgr.GetOccupant(cell);
            ToolFX.Planted(planted != null ? planted.transform.position : mgr.CellToWorldCenter(cell));

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

        /// <summary>
        /// 마우스 위치를 월드 좌표로.
        /// ★ cam 이 없으면 예외가 나므로 자기 위치로 대신한다 (경고는 Awake 에서 이미 한 번 띄운다)
        /// </summary>
        private Vector3 GetMouseWorld()
        {
            if (cam == null) return transform.position;

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