using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace PJH.Scripts
{
    public class FishingMiniGameUI : MonoBehaviour
    {
        #region 필드 및 이벤트

        [Header("MiniGame Fish UI")]
        [SerializeField] private RectTransform fishMoveArea;
        [SerializeField] private RectTransform fishIconRoot;
        [SerializeField] private RectTransform fishVisual;
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
        private Tween shakeFish;
        private bool wasFishInside;
        #endregion

   

        #region 패널 열기 및 닫기

        public void Start()
        {
            SetPanelHeight(0f);
        }


        public void OpenPanel()
        {
            ResetFishShake();
            panelTween?.Kill();
            isOpend = true;
            catchBarImage.color = Color.red;
            SetPanelHeight(0f);

            panelTween = minigamePanel.DOSizeDelta(new Vector2(minigamePanel.sizeDelta.x, targetHeight), duration)
                .SetEase(Ease.OutCubic)
                .OnComplete(InvokeMiniGame);
        }

        public void ClosePanel()
        {
            ResetFishShake();
            panelTween?.Kill();

            panelTween = minigamePanel.DOSizeDelta(new Vector2(minigamePanel.sizeDelta.x, 0f), 0.5f
                ).SetEase(Ease.InCubic)
                .OnComplete(() =>  isOpend = false);
        }

        private void InvokeMiniGame()
        {
            OnShowComplete?.Invoke();
        }

        #endregion

        #region 진행도 게이지

        public void FillGuage(float progress)
        {
            gaugeImage.fillAmount = progress;
        }

        #endregion

        #region UI 위치 설정

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
            float iconHeight = fishIconRoot.rect.height;

            if (areaHeight <= iconHeight)
            {
                fishIconRoot.anchoredPosition = new Vector2(0f, areaHeight * 0.5f);
                return;
            }

            float minY = iconHeight * 0.5f;
            float maxY = areaHeight - iconHeight * 0.5f;
            float y = Mathf.Lerp(minY, maxY,  Mathf.Clamp01(normalizedHeight));
            fishIconRoot.anchoredPosition = new Vector2(fishIconRoot.anchoredPosition.x, y);
        }

        #endregion

        #region 물고기 포획 판정

        public bool CatchFishing()
        {
            catchBar.GetWorldCorners(catchBarCorners);
            fishIconRoot.GetWorldCorners(fishCorners);

            float catchBarBottom = catchBarCorners[0].y;
            float catchBarTop = catchBarCorners[1].y;
            
            float fishCenter = (fishCorners[0].y + fishCorners[1].y) * 0.5f;
            
            bool isFishInside = fishCenter >= catchBarBottom && fishCenter <= catchBarTop;
            
            if (isFishInside && !wasFishInside)
            {
                StartFishShake();
            }   
            else if(!isFishInside && wasFishInside)
            {
                StopFishShake();
            }
            catchBarImage.color = isFishInside ? Color.white : Color.red;
            wasFishInside = isFishInside;
            return isFishInside;
        }
        private void StartFishShake()
        {
            shakeFish?.Kill();
            
            shakeFish = fishVisual.DOShakeAnchorPos(0.15f,
                new Vector2(2f, 1f),
                10,
                15f,
                false,
                false)
                .SetLoops(-1, LoopType.Restart)
                .SetLink(fishVisual.gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void StopFishShake()
        {
            shakeFish?.Kill();
            shakeFish = null;
            fishVisual.anchoredPosition = Vector2.zero;
        }

        private void ResetFishShake()
        {
            shakeFish?.Kill();
            shakeFish = null;
            wasFishInside = false;
            fishVisual.anchoredPosition = Vector2.zero;
        }


        #endregion
    }

}
