using UnityEngine;
using UnityEngine.InputSystem;

namespace PJH.Scripts
{
    public class PlayerFishing : MonoBehaviour
    {
        #region 필드 및 설정

        [Header("나중에 뺄것")]
        private Rigidbody2D rigid;
        private Vector3 movedir;
        [SerializeField] private float moveSpeed;
        [SerializeField] private Animator animator;
        private readonly int _hashFishing = Animator.StringToHash("Base Layer.Fishing");
        private readonly int _hashFishHook = Animator.StringToHash("Base Layer.FishingHook");
        private readonly int _hashIdle = Animator.StringToHash("Base Layer.Idle");
        private const int BaseLayer = 0;
        
        private enum FishingState
        {
            Idle,
            WaitingBite,
            Hooking
        }
    
        [Header("Fishing Settings")]
        [SerializeField] private FishingMiniGame fishingMiniGame;
        [SerializeField] private FishingAreaCheck fishingAreaCheck;
        [SerializeField] private FishSelector fishSelector;
        private bool isFishing;
        private FishingState state = FishingState.Idle;
        private bool canClick = true;
        private FishDataSO GetFishDataSO;

        #endregion

        #region 이벤트 연결 및 처리

        private void OnEnable()
        {
            fishingMiniGame.OnFishingSucceeded += HandleFishingSucceeded;
            fishingMiniGame.OnFishingFailed += HandleFishingFailed;
        }

        private void OnDisable()
        {
            fishingMiniGame.OnFishingSucceeded -= HandleFishingSucceeded;
            fishingMiniGame.OnFishingFailed -= HandleFishingFailed;
        }

        private void HandleFishingSucceeded(FishDataSO caughtFish)
        {
            animator.Play(_hashFishHook, BaseLayer, 0f);
            Debug.Log($"인벤토리에 {caughtFish.name}가 들어갈거임 ㅇㅇ");
            //caughtFish를 인벤토리에 추가
        }

        private void HandleFishingFailed()
        {
            animator.Play(_hashFishHook, BaseLayer, 0f);
        }

        #endregion

        #region 초기화

        private void Awake()
        {
            rigid = GetComponent<Rigidbody2D>();
            canClick = true;
        }

        #endregion

        #region 낚시 진행

        public void CheckBobberLanding()
        {
            bool canFish = fishingAreaCheck.IsFishingLayer();
            
            if(canFish)
            {
                Debug.Log("낚시 가능 구역");
                fishingMiniGame.BringFishData(fishSelector.RandomFish());
                fishingMiniGame.WaitBiteTime();
                canClick = true;
            }
            
            else
            {
                animator.Play(_hashIdle, BaseLayer, 0f);
                FinishFishing();
            }
        }


        private void Update()
        {
            if (fishingMiniGame.BlockFishingInput) return;
            
            if (Mouse.current.leftButton.wasPressedThisFrame && canClick)
            {
                if (!isFishing)
                {
                    Debug.Log("Fishing 애니메이션 실행");
                    canClick = false;
                    animator.Play(_hashFishing, BaseLayer, 0f);
                }

                else
                {
                    Debug.Log("FishHook 애니메이션 실행");
                    canClick = false;
                    animator.Play(_hashFishHook, BaseLayer, 0f);
                }
            
            }
        }

        public void StartFishing()
        {
            isFishing = true;
        }

        public void FinishFishing()
        {
            isFishing = false;
            fishingMiniGame.StopMiniGame();
            canClick = true;
        }

        #endregion

        #region 플레이어 이동

        private void FixedUpdate()
        {
            if (isFishing)
            {
                rigid.linearVelocity = Vector2.zero;
                return;
            }
            rigid.linearVelocity = movedir * moveSpeed;
        }
    
        
        private void OnMove(InputValue value)
        {
            movedir = value.Get<Vector2>();
        }

        #endregion

    }
}
