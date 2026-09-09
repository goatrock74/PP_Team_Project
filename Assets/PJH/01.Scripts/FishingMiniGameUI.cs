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
 
        [Header("패널 위치 (좌우)")]
        [Tooltip("★ 펼쳐지는 MiniGamePanel 이 아니라 가장 바깥쪽 RevealMask 를 넣는다.\n" +
                 "안쪽을 움직이면 펼침 애니메이션과 충돌한다")]
        [SerializeField] private RectTransform revealMask;
 
        [Tooltip("오른쪽을 보고 낚시할 때 패널이 놓일 자리")]
        [SerializeField] private Vector2 rightPanelPosition = new Vector2(180f, 0f);
 
        [Tooltip("왼쪽을 보고 낚시할 때 패널이 놓일 자리. 좌우 대칭이면 X 만 반대로")]
        [SerializeField] private Vector2 leftPanelPosition = new Vector2(-180f, 0f);
 
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
 
        #region 패널 위치
 
        /// <summary>
        /// 낚시 방향에 따라 패널을 좌/우로 옮긴다.
        /// <b>패널이 열리기 전에</b> 불러야 이동한 자리에서 자연스럽게 펼쳐진다.
        /// </summary>
        public void SetPanelSide(bool faceLeft)
        {
            if (revealMask == null)
            {
                Debug.LogWarning("[낚시UI] Reveal Mask 가 연결되지 않아 패널 위치를 못 바꿉니다.", this);
                return;
            }
 
            revealMask.anchoredPosition = faceLeft ? leftPanelPosition : rightPanelPosition;
        }
 
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
 