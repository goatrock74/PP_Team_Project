using System;
using UnityEngine;
using DG.Tweening;
namespace PJH.Scripts
{
    public class FishingAnimationController : MonoBehaviour
    {
        #region 필드

        [SerializeField] private Animator playerAnimator;
        [SerializeField] private Transform rendererTransform;
        
        private Tweener shakeTween;
        private Vector3 originalLocalPosition;
        private bool isShaking; 

        #endregion

        #region 초기화


        private void Awake()
        {
            originalLocalPosition = rendererTransform.localPosition;
        }

        #endregion

        #region 낚시 떨림 연출

        public void ShakePlayer()
        {
            shakeTween?.Kill();
            playerAnimator.Play("Landing");
            
            originalLocalPosition = rendererTransform.localPosition;
            isShaking = true;

            shakeTween = rendererTransform.DOShakePosition(0.15f, new Vector3(0.06f, 0, 0),
                6, 10,
                false, false
                ).SetLoops(-1, LoopType.Restart)
                .SetLink(rendererTransform.gameObject, LinkBehaviour.KillOnDestroy);
        }

        public void StopFishingShake()
        {
            if (!isShaking) return;
            shakeTween?.Kill();
            shakeTween = null;

            if (rendererTransform != null)
            {
                rendererTransform.localPosition = originalLocalPosition;
            }

            isShaking = false;
        }

        #endregion

    }
}
