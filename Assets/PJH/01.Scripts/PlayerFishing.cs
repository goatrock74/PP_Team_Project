using UnityEngine;
using UnityEngine.InputSystem;
using KSM._00.Scripts;
 
namespace PJH.Scripts
{
    /// <summary>
    /// 낚시 진행 담당.
    ///
    /// ★ 원본에서 바뀐 것 3가지
    ///   1) 이동 코드를 전부 뺐다 — PlayerMovement 와 정면충돌했다.
    ///      PlayerInput 은 OnMove 를 같은 오브젝트의 모든 컴포넌트에 뿌리기 때문에
    ///      둘 다 입력을 받고 둘 다 FixedUpdate 에서 linearVelocity 를 써서
    ///      실행 순서에 따라 이동이 튀었다. 이제 PlayerMovement.HoldLocked 로 묶는다.
    ///   2) "던지기" 클릭을 여기서 안 읽는다 — 낚싯대(FishingRodSO)가 담당한다.
    ///      안 그러면 클릭 한 번에 낚시도 시작되고 씨앗도 심겼다.
    ///      여기서는 낚시 중의 "거두기" 클릭만 읽는다.
    ///   3) IsFishing static 추가 — 다른 시스템이 입력을 막을 수 있게.
    /// </summary>
    public class PlayerFishing : MonoBehaviour
    {
        #region 필드 및 설정
 
        [Header("애니메이션")]
        [SerializeField] private Animator animator;
 
        private readonly int _hashFishing = Animator.StringToHash("Base Layer.Fishing");
        private readonly int _hashFishHook = Animator.StringToHash("Base Layer.FishingHook");
        private readonly int _hashIdle = Animator.StringToHash("Base Layer.Idle");
        private const int BaseLayer = 0;
 
        [Header("Fishing Settings")]
        [SerializeField] private FishingController fishingController;
        [SerializeField] private FishingMiniGame fishingMiniGame;
        [SerializeField] private FishingAreaCheck fishingAreaCheck;
        [SerializeField] private FishSelector fishSelector;
 
        [SerializeField] private GameObject splashParticle;
 
        [Header("이동 잠금")]
        [Tooltip("비우면 이 오브젝트와 부모에서 찾는다")]
        [SerializeField] private PlayerMovement movement;
 
        private bool isFishing;
        private bool canClick = true;
 
        /// <summary>
        /// 지금 낚시 중인가. PlayerInteractor 같은 다른 입력 처리기가
        /// GachaUI.IsSpinning 처럼 이 값을 보고 비켜주면 된다.
        /// </summary>
        public static bool IsFishing { get; private set; }
 
        #endregion
 
        #region 이벤트 연결 및 처리
 
        private void OnEnable()
        {
            fishingMiniGame.OnFishingSucceeded += HandleFishingSucceeded;
            fishingMiniGame.OnFishingFailed += HandleFishingFailed;
            fishingMiniGame.OnFishSplash += StartSplash;
        }
 
        private void OnDisable()
        {
            fishingMiniGame.OnFishingSucceeded -= HandleFishingSucceeded;
            fishingMiniGame.OnFishingFailed -= HandleFishingFailed;
            fishingMiniGame.OnFishSplash -= StartSplash;
 
            // 낚시 도중에 꺼지면 플레이어가 영영 묶인 채로 남는다
            ReleaseLock();
        }
 
        private void HandleFishingSucceeded(FishDataSO caughtFish)
        {
            animator.Play(_hashFishHook, BaseLayer, 0f);
 
            // 인벤토리 투입은 FishingInventoryAdapter 가 같은 이벤트를 구독해서 처리한다.
            // 여기서 인벤토리를 직접 건드리지 않는 이유: 낚시 코드가 아이템 시스템을
            // 알게 되면 둘 중 하나만 바뀌어도 서로 깨지기 때문
        }
 
        private void HandleFishingFailed()
        {
            animator.Play(_hashFishHook, BaseLayer, 0f);
        }
 
        #endregion
 
        #region 초기화
 
        private void Awake()
        {
            canClick = true;
            IsFishing = false;
 
            if (movement == null) movement = GetComponentInParent<PlayerMovement>();
            if (fishingAreaCheck == null) fishingAreaCheck = GetComponentInChildren<FishingAreaCheck>();
        }
 
        #endregion
 
        #region 낚싯대가 부르는 진입점
 
        /// <summary>지금 던질 수 있는가. 낚싯대의 CanUse(미리보기 초록/빨강)가 이걸 본다</summary>
        public bool CanCast() => CanCast(out _);
 
        /// <summary>
        /// 던질 수 있는지 + 안 되면 왜 안 되는지.
        /// "여기서는 낚시할 수 없습니다" 가 뜰 때 원인을 찾는 용도.
        /// </summary>
        public bool CanCast(out string reason)
        {
            if (isFishing)
            {
                reason = "이미 낚시 중입니다 (FinishFishing 애니메이션 이벤트가 안 걸렸을 수 있음)";
                return false;
            }
 
            if (!canClick)
            {
                reason = "canClick 이 false 입니다 " +
                         "(CheckBobberLanding / FinishFishing 애니메이션 이벤트가 안 불렸습니다)";
                return false;
            }
 
            if (fishingController == null)
            {
                reason = "Fishing Controller 가 연결되지 않았습니다";
                return false;
            }
 
            if (fishingController.BlockPlayerInput)
            {
                reason = "입질 대기 또는 미니게임 중입니다";
                return false;
            }
 
            if (fishingAreaCheck == null)
            {
                reason = "Fishing Area Check 가 연결되지 않았습니다";
                return false;
            }
 
            if (!fishingAreaCheck.IsFishingLayer())
            {
                reason = $"찌 지점 {fishingAreaCheck.FishingPointPosition} 이(가) 물이 아닙니다 " +
                         "(물 오브젝트의 Collider2D / FishingAreaCheck 의 Layer Mask 확인)";
                return false;
            }
 
            reason = string.Empty;
            return true;
        }
 
        /// <summary>플레이 중 컴포넌트 우클릭 → 지금 왜 낚시가 안 되는지 콘솔에 찍는다</summary>
        [ContextMenu("낚시 가능 여부 진단")]
        public void DebugCanCast()
        {
            bool ok = CanCast(out string reason);
 
            Debug.Log(ok
                ? "[낚시] 지금 던질 수 있습니다"
                : $"[낚시] 던질 수 없음 — {reason}", this);
        }
 
        /// <summary>
        /// 던지기 시작. 성공하면 true.
        /// 실제로 찌가 떨어지는 판정은 애니메이션 이벤트가 CheckBobberLanding 을 부를 때 일어난다.
        /// </summary>
        public bool TryStartCast()
        {
            if (!CanCast(out string reason))
            {
                Debug.Log($"[낚시] 던질 수 없음 — {reason}", this);
                return false;
            }
 
            // ★ 미니게임 패널이 물 반대쪽에 뜨지 않도록, 던지는 시점의 방향을 알려준다.
            //   패널이 열리기 전(입질 전)이라 자연스럽게 그 자리에서 펼쳐진다
            bool faceLeft = movement != null && movement.FacingX < 0f;
            fishingController.SetFishingDirection(faceLeft);
 
            canClick = false;
            animator.Play(_hashFishing, BaseLayer, 0f);
 
            return true;
        }
 
        #endregion
 
        #region 낚시 진행
 
        /// <summary>애니메이션 이벤트에서 호출 — 찌가 물에 닿는 프레임</summary>
        public void CheckBobberLanding()
        {
            bool started = fishingController.HandleBobberLanding();
 
            if (started)
            {
                canClick = true;
                return;
            }
 
            animator.Play(_hashIdle, BaseLayer, 0f);
            FinishFishing();
        }
 
        private void StartSplash()
        {
            if (splashParticle == null)
            {
                Debug.LogError("연결 안되었는디요?");
                return;
            }
 
            Instantiate(splashParticle, fishingAreaCheck.FishingPointPosition, Quaternion.identity);
        }
 
        private void Update()
        {
            // 던지기는 낚싯대(FishingRodSO)가 시작시킨다. 여기서는 거두기만 본다
            if (!isFishing || !canClick) return;
 
            // 입질 대기 / 미니게임 중에는 FishingController 와 FishingMiniGame 이
            // 클릭을 써야 하므로 비켜준다
            if (fishingController != null && fishingController.BlockPlayerInput) return;
 
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
 
            canClick = false;
            fishingController.CancelFishing();
            animator.Play(_hashFishHook, BaseLayer, 0f);
        }
 
        /// <summary>애니메이션 이벤트에서 호출 — 던지기 동작이 끝난 시점</summary>
        public void StartFishing()
        {
            isFishing = true;
            IsFishing = true;
 
            // 시간이 정해지지 않은 잠금. 낚시가 끝날 때까지 계속 묶인다
            if (movement != null) movement.HoldLocked = true;
        }
 
        /// <summary>애니메이션 이벤트에서 호출 — 거두기 동작이 끝난 시점</summary>
        public void FinishFishing()
        {
            isFishing = false;
            canClick = true;
 
            ReleaseLock();
 
            if (fishingController != null) fishingController.CancelFishing();
        }
 
        private void ReleaseLock()
        {
            IsFishing = false;
            if (movement != null) movement.HoldLocked = false;
        }
 
        #endregion
    }
}
 