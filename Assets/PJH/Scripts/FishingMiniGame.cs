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


        
        private void Update()
        {
            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                fishingMiniGameUI.OpenPanel();
            }
        }

        private void OnEnable()
        {
            fishingMiniGameUI.OnShowComplete += StartMiniGame;
        }

        public void StartMiniGame()
        {
            Debug.Log("낚시게임 시작");
            StartCoroutine(WaitBite());
        }

        private float GetRandomBiteTime()
        {
            float min = fishingSettingSO.MinBiteTime;
            float max = fishingSettingSO.MaxBiteTime;
            return (
                Random.Range(min, max) +
                Random.Range(min, max) * 0.5f);
        }

        private IEnumerator WaitBite()
        {
            yield return new WaitForSeconds(GetRandomBiteTime());
            fishingMiniGameUI.OpenPanel();
        }

    }
}
