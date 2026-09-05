using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PJH.Scripts
{
    public class FishingMiniGameUI : MonoBehaviour
    {
        [Header("MiniGame Fish UI")]
        [SerializeField] private RectTransform fishMoveArea;
        [SerializeField] private RectTransform fishIcon;
        [SerializeField] private RectTransform minigamePanel;
        [SerializeField] private float targetHeight = 530f;
        [SerializeField] private float duration = 1.3f;
        private Tween panelTween;
        private bool isOpend;
        
        [Header("MiniGame CatchBar UI")]
        [SerializeField] private RectTransform catchBar;
        [SerializeField] private Image catchBarImage; 
        
        [SerializeField] private Image gaugeImage;
        
        private readonly Vector3[] catchBarCorners = new Vector3[4];
        private readonly Vector3[] fishCorners = new Vector3[4];

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
                .OnComplete(InvokeMiniGame);
        }

        private void InvokeMiniGame()
        {
            OnShowComplete?.Invoke();
        }

        public void FillGuage(float progress)
        {
            gaugeImage.fillAmount = progress;
        }

        private void SetPanelHeight(float height)
        {
            
            minigamePanel.sizeDelta = new Vector2(minigamePanel.sizeDelta.x, height);
        }
        
        public void SetCatchBarHeight(float nomarizedHeight)
        {
            float areaHeight = fishMoveArea.rect.height;
            float barHeight = catchBar.rect.height;

            if (areaHeight <= barHeight)
            {
                catchBar.anchoredPosition = new Vector2(0f, areaHeight * 0.5f);
                return;
            }
            
            float minY = barHeight * 0.5f;
            float maxY = areaHeight - barHeight * 0.5f;
            
            float y = Mathf.Lerp(minY, maxY, Mathf.Clamp01(nomarizedHeight));
            catchBar.anchoredPosition = new Vector2(0f, y);
            
        }

        public void SetFishHeight(float normalizedHeight)
        {
            float areaHeight = fishMoveArea.rect.height;
            float iconHeight = fishIcon.rect.height;

            if (areaHeight <= iconHeight)
            {
                fishIcon.anchoredPosition = new Vector2(0f, areaHeight * 0.5f);
                return;
            }

            float minY = iconHeight * 0.5f;
            float maxY = areaHeight - iconHeight * 0.5f;
            float y = Mathf.Lerp(minY, maxY,  Mathf.Clamp01(normalizedHeight));
            fishIcon.anchoredPosition = new Vector2(0f, y);
        }

        public bool CatchFishing()
        {
            catchBar.GetWorldCorners(catchBarCorners);
            fishIcon.GetWorldCorners(fishCorners);

            float catchBarBottom = catchBarCorners[0].y;
            float catchBarTop = catchBarCorners[1].y;
            
            float fishCenter = (fishCorners[0].y + fishCorners[1].y) * 0.5f;
            
            bool isFishInside = fishCenter >= catchBarBottom && fishCenter <= catchBarTop;
            
            if (isFishInside) catchBarImage.color = Color.white;
            else catchBarImage.color = Color.red;
            
            return isFishInside;
        }
    }

}