using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace PJH.Scripts
{
    public class FishingMiniGame : MonoBehaviour
    {
        #region 필드 및 설정

        [Header("FishingMiniGame UI")]
        [SerializeField] private FishingMiniGameUI fishingMiniGameUI;
        [SerializeField] private FishingSettingSO fishingSettingSO;
        
        
        [Header("MiniGame fish Data")]
        private FishDataSO currentFishData;
        
        [Header("FishMovement")]
        private float currentFishHeight;
        private float targetFishHeight;
        private float targetChangeTimer;

        [Header("CatchBar")]
        private float currentCatchBarHeight;
        
        [Header("CatchBar Acceleration")]
        [SerializeField, Min(0f)]
        private float catchBarUpAcceleration = 1.4f;
        
        [SerializeField,  Min(0f)]
        private float catchBarGravity = 1.1f;
        
        [Header("CatchBar MaxSpeed")]
        [SerializeField, Min(0f)]
        private float catchBarMaxRiseSpeed = 0.8f;
        
        [SerializeField, Min(0f)]
        private float catchBarMaxFallSpeed = 0.95f;
        
        [Header("CatchBar Bounce")]
        [SerializeField, Range(0f, 1f)]
        private float catchBarBottomBounce = 0.6f;
        
        [SerializeField, Min(0f)]
        private float catchBarMinBounceSpeed = 0.05f;

        private float catchBarVelocity;
        
        [Header("MiniGame progress Information")]
        private float miniGameProgress;

        [SerializeField] private float progressIncreaseSpeed = 0.12f;
        [SerializeField] private float progressDecreaseSpeed = 0.18f;
        
        [Header("Animation")]
        [SerializeField] private FishingAnimationController animationController;
        
        [Header("MiniGame Bool")]
        public bool IsMiniGameRunning => isMiniGameRunning;
        public bool BlockFishingInput => isOpeningPanel || isMiniGameRunning;
        private bool isOpeningPanel;
        private bool isMiniGameRunning;

        public event Action<FishDataSO> OnFishingSucceeded;
        public event Action OnFishingFailed;
        
        private Coroutine biteCoroutine;

        #endregion

        #region 유니티 생명주기

        private void OnEnable()
        {
            fishingMiniGameUI.OnShowComplete += StartFishingMiniGame;
        }

        private void OnDisable()
        {
            fishingMiniGameUI.OnShowComplete -= StartFishingMiniGame;
            
            StopMiniGame();
        }

        private void Update()
        {
            if (!isMiniGameRunning) return;

            
            
            MoveCatchBar();
            targetChangeTimer -= Time.deltaTime;

            if (targetChangeTimer <= 0f)
            {
                ChooseNextTarget();
            }
            
            currentFishHeight = Mathf.MoveTowards(currentFishHeight,
                targetFishHeight, 
                currentFishData.MoveSpeed * Time.deltaTime);
            fishingMiniGameUI.SetFishHeight(currentFishHeight);

            if (fishingMiniGameUI.CatchFishing())
            {
                miniGameProgress += progressIncreaseSpeed * Time.deltaTime;
                miniGameProgress = Mathf.Clamp01(miniGameProgress);

                if (miniGameProgress >= 1f)
                {
                    SuccessMiniGame();
                }
            }
            else
            {
                miniGameProgress -= progressDecreaseSpeed * Time.deltaTime;
                miniGameProgress = Mathf.Clamp01(miniGameProgress);

                if (miniGameProgress <= 0f)
                {
                    FailedMiniGame();
                }
            }
            fishingMiniGameUI.FillGuage(miniGameProgress);
        }

        #endregion

        #region 미니게임 결과 및 종료

        private void SuccessMiniGame()
        {
            if (!isMiniGameRunning) return;
            
            FishDataSO caughtFish = currentFishData;
            
            StopMiniGame();

            OnFishingSucceeded?.Invoke(caughtFish);
        }

        private void FailedMiniGame()
        {
            if (!isMiniGameRunning) return;
            StopMiniGame();
            OnFishingFailed?.Invoke();
        }

        public void StopMiniGame()
        {

            if (biteCoroutine != null)
            {
                StopCoroutine(biteCoroutine);
                biteCoroutine = null;
            }
            
            
            isMiniGameRunning = false;
            isOpeningPanel = false;
            currentFishData = null;
            catchBarVelocity = 0f;
            if (animationController != null)
            {
                animationController.StopFishingShake();
            }
            
            fishingMiniGameUI.ClosePanel();
        }

        #endregion

        #region 미니게임 시작 및 입질 대기


        public void WaitBiteTime()
        {
            Debug.Log("찌 도착 후 물고기가 잡히길 기다림");

            if (biteCoroutine != null)
            {
                StopCoroutine(biteCoroutine);
            }
            biteCoroutine = StartCoroutine(WaitBiteAndGame());
            
        }

        public void StartFishingMiniGame()
        {
            if (currentFishData == null)
            {
                Debug.LogWarning(
                    "선택된 물고기 데이터가 없습니다. BringFishData 호출을 확인하세요."
                );
                return;
            }

            if (isMiniGameRunning) return;

            ChooseNextTarget();
            animationController.ShakePlayer();
            miniGameProgress = 0.3f;
            isOpeningPanel = false;
            isMiniGameRunning = true;
        }

        #endregion

        #region 캐치바 및 물고기 이동

        private void MoveCatchBar()
        {
            bool isHolding = Mouse.current != null && Mouse.current.leftButton.isPressed;
            float deltaTime = Time.deltaTime;
            
            float acceleration = isHolding ? catchBarUpAcceleration : -catchBarGravity;
            
            catchBarVelocity += acceleration * deltaTime;
            
            catchBarVelocity = Mathf.Clamp(catchBarVelocity, -catchBarMaxFallSpeed, catchBarMaxRiseSpeed);    
                
            float nextHeight = currentCatchBarHeight + catchBarVelocity * deltaTime;
            

            if (nextHeight <= 0f)
            {
                nextHeight = 0f;

                if (catchBarVelocity < 0f)
                {
                    float impactSpeed = -catchBarVelocity;
                    catchBarVelocity = impactSpeed >= catchBarMinBounceSpeed
                        ? impactSpeed * catchBarBottomBounce
                        : 0f;
                }
            }
            
            else if (nextHeight >= 1f)
            {
                nextHeight = 1f;

                if (catchBarVelocity > 0f)
                {
                    catchBarVelocity = 0f;
                }
            }
            
            currentCatchBarHeight = nextHeight;
            fishingMiniGameUI.SetCatchBarHeight(currentCatchBarHeight);
        }

        private void ChooseNextTarget()
        {
            float distance = currentFishData.MaxMoveDistance;
            
            float minHeight = Mathf.Max(0f, currentFishHeight - distance);
            float maxHeight = Mathf.Min(1f, currentFishHeight + distance);
            
            targetFishHeight = Random.Range(minHeight, maxHeight);

            float minTime = Mathf.Max(0.1f, currentFishData.MinTargetChangeTime);
            float maxTime = Mathf.Max(minTime, currentFishData.MaxTargetChangeTime);
            targetChangeTimer = Random.Range(minTime, maxTime);

        }

        public void BringFishData(FishDataSO fishDataSO)
        {
            currentCatchBarHeight = 0f;
            catchBarVelocity = 0f;
            
            isMiniGameRunning = false;
            currentFishData = fishDataSO;

            currentFishHeight = 0.5f;
            targetFishHeight = currentFishHeight;

            currentCatchBarHeight = 0f;
            fishingMiniGameUI.SetCatchBarHeight(currentCatchBarHeight);
            
            fishingMiniGameUI.SetFishHeight(currentFishHeight);
            Debug.Log($"{fishDataSO.name} 이새끼 잡힘!!");
        }

        #endregion

        #region 입질 시간 계산

        private IEnumerator WaitBiteAndGame()
        {
            yield return new WaitForSeconds(GetRandomBiteTime());
            biteCoroutine = null;
            isOpeningPanel = true;
            fishingMiniGameUI.OpenPanel();
        }
        
        
        private float GetRandomBiteTime()
        {
            float min = fishingSettingSO.MinBiteTime;
            float max = fishingSettingSO.MaxBiteTime;
            return 
            (Random.Range(min, max) +
             Random.Range(min, max)) * 0.5f;
        }

        #endregion

    }
}
