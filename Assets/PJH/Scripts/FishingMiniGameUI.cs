using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PJH.Scripts
{
    public class FishingMiniGameUI : MonoBehaviour
    {
        [Header("MiniGame UI")]
        [SerializeField] private RectTransform minigamePanel;
        [SerializeField] private float targetHeight = 530f;
        [SerializeField] private float duration = 1.3f;
        private Tween panelTween;
        private bool isOpend;

        public event Action OnShowComplete; 

        public void Start()
        {
            SetPanelHeight(0f);
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
            OnShowComplete?.Invoke();
        }

        private void SetPanelHeight(float height)
        {
            minigamePanel.sizeDelta = new Vector2(minigamePanel.sizeDelta.x, height);
        }
    }

}