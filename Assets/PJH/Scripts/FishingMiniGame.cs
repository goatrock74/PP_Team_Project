using System;
using UnityEngine;
using DG.Tweening;
using UnityEngine.InputSystem;

namespace PJH.Scripts
{
    public class FishingMiniGame : MonoBehaviour
    {
        [Header("MiniGame UI")]
        [SerializeField] private RectTransform minigamePanel;
        [SerializeField] private float targetHeight = 530f;
        [SerializeField] private float duration = 1.3f;
        private Tween panelTween;
        private bool isOpend;

        public void Start()
        {
            SetPanelHeight(0f);
        }


        private void Update()
        {
            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                OpenPanel();
            }
        }

        public void OpenPanel()
        {
            panelTween?.Kill();
            isOpend = true;
            SetPanelHeight(0f);

            panelTween = minigamePanel.DOSizeDelta(new Vector2(minigamePanel.sizeDelta.x, targetHeight), duration)
                .SetEase(Ease.OutCubic)
                .OnComplete(StartMiniGame);
        }

        private void StartMiniGame()
        {
            Debug.Log("미니게임 시작");
        }

        private void SetPanelHeight(float height)
        {
            minigamePanel.sizeDelta = new Vector2(minigamePanel.sizeDelta.x, height);
        }
    }
}
