using System.Collections;
using UnityEngine;
using UnityEngine.AdaptivePerformance;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace PJH.Scripts
{
    public class FishingController : MonoBehaviour
    {
        private enum FishingState
        {
            Idle,
            WaitingBite,
            BiteWindow,
            MiniGame
        }

        [Header("Fishing Components")]
        [SerializeField] private FishingAreaCheck fishingAreaCheck;
        [SerializeField] private FishSelector fishSelector;
        [SerializeField] private FishingMiniGame fishingMiniGame;
        [SerializeField] private FishingSettingSO fishingSettingSO;

        [Header("Bite Effect")]
        [SerializeField] private GameObject splashParticlePrefab;

        [SerializeField, Min(0.1f)]
        private float biteReactionTime = 1f;

        private FishingState currentState = FishingState.Idle;

        private FishDataSO currentFish;
        private Vector3 bobberPosition;

        private Coroutine fishingRoutine;
        private GameObject activeSplashParticle;
        
        [Header("Catch Effect")]
        [SerializeField] private FishingCatchEffect catchEffectPrefab;

        [SerializeField] private Transform caughtFishPoint;

        public bool BlockPlayerInput =>
            currentState == FishingState.BiteWindow ||
            currentState == FishingState.MiniGame;

        private void OnEnable()
        {
            fishingMiniGame.OnFishingSucceeded
                += HandleFishingSucceeded;

            fishingMiniGame.OnFishingFailed
                += HandleFishingFailed;
        }

        private void OnDisable()
        {
            fishingMiniGame.OnFishingSucceeded
                -= HandleFishingSucceeded;

            fishingMiniGame.OnFishingFailed
                -= HandleFishingFailed;

            StopFishingRoutine();
            StopSplashParticle();
        }

        public bool HandleBobberLanding()
        {
            bool canFish =
                fishingAreaCheck.IsFishingLayer();

            if (!canFish)
            {
                Debug.Log("낚시할 수 없는 위치입니다.");
                return false;
            }

            bobberPosition =
                fishingAreaCheck.FishingPointPosition;

            StartBiteSequence();

            return true;
        }

        private void PlayCaughtFishEffect(FishDataSO caughtFish)
        {
            if (catchEffectPrefab == null)
            {
                Debug.LogWarning("물고리 연결 안되어있음");
                return;
            }

            if (caughtFishPoint == null)
            {
                Debug.LogWarning("물고기 도착 지점 연결 X");
                return;
            }

            FishingCatchEffect effect = Instantiate(catchEffectPrefab, bobberPosition, Quaternion.identity);
            
            effect.Play(caughtFish.icon, caughtFishPoint);



        }

        private void StartBiteSequence()
        {
            StopFishingRoutine();
            StopSplashParticle();

            currentState = FishingState.WaitingBite;
            fishingRoutine = StartCoroutine(BiteSequence());
        }

        private IEnumerator BiteSequence()
        {
            while (true)
            {
                currentFish = fishSelector.RandomFish();

                currentState = FishingState.WaitingBite;

                float biteTime = GetRandomBiteTime();

                Debug.Log(
                    $"{biteTime:F1}초 동안 입질을 기다립니다."
                );

                yield return new WaitForSeconds(biteTime);

                // 입질시간이 끝났습니다.
                currentState = FishingState.BiteWindow;

                StartSplashParticle();

                Debug.Log(
                    "입질이 왔습니다. 1초 안에 클릭하세요."
                );

                float reactionTimer = 0f;

                while (reactionTimer < biteReactionTime)
                {
                    bool pressedMouse =
                        Mouse.current != null &&
                        Mouse.current.leftButton
                            .wasPressedThisFrame;

                    if (pressedMouse)
                    {
                        AcceptBite();
                        Debug.Log("클릭 잘함");
                        fishingRoutine = null;
                        yield break;
                    }

                    reactionTimer += Time.deltaTime;
                    yield return null;
                }

                MissBite();

                yield return null;
            }
        }

        private void AcceptBite()
        {
            StopSplashParticle();

            Debug.Log("입질 성공! 미니게임 패널을 엽니다.");

            currentState = FishingState.MiniGame;

            fishingMiniGame.OpenMiniGame(currentFish);
        }

        private void MissBite()
        {
            StopSplashParticle();

            Debug.Log(
                "입질을 놓쳤습니다. 다시 기다립니다."
            );

            currentFish = null;
            currentState = FishingState.WaitingBite;
        }

        private void StartSplashParticle()
        {
            if (splashParticlePrefab == null)
            {
                Debug.LogWarning(
                    "물장구 파티클 프리팹이 연결되지 않았습니다."
                );

                return;
            }

            activeSplashParticle = Instantiate(
                splashParticlePrefab,
                bobberPosition,
                Quaternion.identity
            );
        }

        private void StopSplashParticle()
        {
            if (activeSplashParticle == null)
                return;

            Destroy(activeSplashParticle);
            activeSplashParticle = null;
        }

        private float GetRandomBiteTime()
        {
            float min = fishingSettingSO.MinBiteTime;
            float max = fishingSettingSO.MaxBiteTime;

            return (
                Random.Range(min, max) +
                Random.Range(min, max)
            ) * 0.5f;
        }

        private void HandleFishingSucceeded(
            FishDataSO caughtFish)
        {
            Debug.Log(
                $"{caughtFish.name} 낚시 성공"
            );
            PlayCaughtFishEffect(caughtFish);
            FinishFishingProcess();
        }

        private void HandleFishingFailed()
        {
            Debug.Log("낚시 미니게임 실패");

            FinishFishingProcess();
        }

        private void FinishFishingProcess()
        {
            StopFishingRoutine();
            StopSplashParticle();

            currentFish = null;
            currentState = FishingState.Idle;
        }

        public void CancelFishing()
        {
            StopFishingRoutine();
            StopSplashParticle();

            currentFish = null;
            currentState = FishingState.Idle;

            fishingMiniGame.StopMiniGame();
        }

        private void StopFishingRoutine()
        {
            if (fishingRoutine == null)
                return;

            StopCoroutine(fishingRoutine);
            fishingRoutine = null;
        }
    }
}