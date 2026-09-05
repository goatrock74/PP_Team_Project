using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace PJH.Scripts
{
    public class FishingMiniGame : MonoBehaviour
    {
        [Header("Player Sprite")]
        
        
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
        [SerializeField] private float catchBarRiseSpeed = 0.6f;
        [SerializeField] private float catchBarFallSpeed = 0.4f;
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
        
        
        
        
        
        [Header("MiniGame Bool")]
        public bool IsMiniGameRunning => isMiniGameRunning;
        public bool BlockFishingInput => isOpeningPanel || isMiniGameRunning;
        private bool isOpeningPanel;
        private bool isMiniGameRunning;
        
        
        


        
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
                    StopMiniGame();
                }
            }
            fishingMiniGameUI.FillGuage(miniGameProgress);
        }

        private void SuccessMiniGame()
        {
            Debug.Log("미니게임 성공 후 물고기 획득!");
        }

        public void StopMiniGame()
        {
            isMiniGameRunning = false;
            isOpeningPanel = false;
            currentFishData = null;
            catchBarVelocity = 0f;
        }
        


        public void WaitBiteTime()
        {
            Debug.Log("찌 도착 후 물고기가 잡히길 기다림");
            StartCoroutine(WaitBiteAndGame());
            
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
            miniGameProgress = 0.3f;
            isOpeningPanel = false;
            isMiniGameRunning = true;
        }

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


        private IEnumerator WaitBiteAndGame()
        {
            yield return new WaitForSeconds(GetRandomBiteTime());
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
        private void OnEnable()
        {
            fishingMiniGameUI.OnShowComplete += StartFishingMiniGame;
        }

        private void OnDisable()
        {
            fishingMiniGameUI.OnShowComplete -= StartFishingMiniGame;
            
            StopMiniGame();
        }

    }
}
