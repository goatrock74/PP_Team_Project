using System;
using System.Reflection;
using UnityEngine;
using DG.Tweening;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace PJH.Scripts
{
    public class FishingMiniGame : MonoBehaviour
    {
        [SerializeField] private FishingMiniGameUI fishingMiniGameUI;
        [SerializeField] private FishingSettingSO fishingSettingSo;

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

        private void StartMiniGame()
        {
            Debug.Log("낚시게임 시작");
        }

        private float GetRandomBiteTime()
        {
            float min = fishingSettingSo.MinBiteTime;
            float max = fishingSettingSo.MaxBiteTime;
            return (
                Random.Range(min, max) +
                Random.Range(min, max) * 0.5f);
        }

    }
}
