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
        private Vector3 originalPosition;

        #endregion

        #region 초기화


        private void Awake()
        {
            originalPosition = rendererTransform.position;
        }

        #endregion

        #region 낚시 떨림 연출

        public void ShakePlayer()
        {
            shakeTween?.Kill();
            playerAnimator.Play("Landing");
            
            originalPosition = rendererTransform.position;

            shakeTween = rendererTransform.DOShakePosition(0.25f, new Vector3(0.06f, 0, 0),
                6, 10,
                false, false
                ).SetLoops(-1, LoopType.Restart)
                .SetLink(rendererTransform.gameObject, LinkBehaviour.KillOnDestroy);
        }

        public void StopFishingShake()
        {
            shakeTween?.Kill();
            shakeTween = null;

            if (rendererTransform != null)
            {
                rendererTransform.position = originalPosition;
            }
        }

        #endregion

    }
}
