using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace PJH.Scripts
{
    public class FishingMiniGame : MonoBehaviour
    {
        [SerializeField] private FishingMiniGameUI fishingMiniGameUI;
        [SerializeField] private FishingSettingSO fishingSettingSO;
        
        
        [Header("MiniGame Information")]
        private FishDataSO currentFishData;
        private bool isMiniGameRunning;
        private float currentFishHeight;
        private float targetFishHeight;
        private float targetChangeTimer;
        
        


        
        private void Update()
        {
            if (!isMiniGameRunning) return;
            
            Debug.Log("② 이동 구간 진입", this);
            
            targetChangeTimer -= Time.deltaTime;

            if (targetChangeTimer <= 0f)
            {
                ChooseNextTarget();
            }
            
            currentFishHeight = Mathf.MoveTowards(currentFishHeight,
                targetFishHeight, 
                currentFishData.MoveSpeed * Time.deltaTime);
            fishingMiniGameUI.SetFishHeight(currentFishHeight);
        }

        public void StopMiniGame()
        {
            isMiniGameRunning = false;
            currentFishData = null;
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
            isMiniGameRunning = true;
        }

        private void ChooseNextTarget()
        {
            float distance = currentFishData.MaxMoveDistance;
            
            float minHeight = Mathf.Max(0f, currentFishHeight - distance);
            float maxHeight = Mathf.Min(1f, currentFishHeight + distance);
            
            targetFishHeight = Random.Range(minHeight, maxHeight);

            float minTime = Mathf.Max(0.1f, currentFishData.MinTargetChangeTime);
            float maxTime = Mathf.Min(minTime, currentFishData.MaxTargetChangeTime);
            targetChangeTimer = Random.Range(minTime, maxTime);

        }

        public void BringFishData(FishDataSO fishDataSO)
        {
            isMiniGameRunning = false;
            currentFishData = fishDataSO;

            currentFishHeight = 0.5f;
            targetFishHeight = currentFishHeight;
            
            fishingMiniGameUI.SetFishHeight(currentFishHeight);
            Debug.Log($"{fishDataSO.name} 이새끼 잡힘!!");
        }


        private IEnumerator WaitBiteAndGame()
        {
            yield return new WaitForSeconds(GetRandomBiteTime());
            fishingMiniGameUI.OpenPanel();
        }
        private float GetRandomBiteTime()
        {
            float min = fishingSettingSO.MinBiteTime;
            float max = fishingSettingSO.MaxBiteTime;
            return (
                Random.Range(min, max) +
                Random.Range(min, max) * 0.5f);
        }

    }
}
