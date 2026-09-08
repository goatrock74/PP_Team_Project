using System;
using DG.Tweening;
using UnityEngine;

namespace PJH.Scripts
{
    public class FishingCatchEffect : MonoBehaviour
    {
        [Header("Componets")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        
        [Header("Movement")]
        [SerializeField] private float moveDuration = 0.7f;
        [SerializeField] private float jumpPower = 0.8f;
        
        [Header("Display")]
        [SerializeField] private float displayDuration = 0.5f;
        [SerializeField] private float disappearDuration = 0.2f;
        
        [Header("Flapping")]
        [SerializeField] private float rotaionAmount = 10f;
        [SerializeField] private float rotationDuration = 0.1f;


        private Tween moveTween;
        private Tween rotationTween;
        private Sequence finishSequence;

        private Vector3 originalScale;
        private Transform followTarget;
        private bool isFollowing;

        private void Awake()
        {
            originalScale = transform.localScale;
        }

        public void Play(Sprite fishIcon, Transform targetPoint)
        {
            if (fishIcon == null)
            {
                Debug.LogWarning("FishIcon is null");
                Destroy(gameObject);
                return;
            }

            if (targetPoint == null)
            {
                return;
            }
            
            spriteRenderer.sprite = fishIcon;
            transform.localScale = originalScale;
            transform.rotation = Quaternion.Euler(0f, 0f, -rotaionAmount);

            followTarget = targetPoint;
            isFollowing = true;
            transform.position = followTarget.position;

            StartFlapping();
            FollowHookAnimation();
        }

        private void LateUpdate()
        {
            if (!isFollowing || followTarget == null)
                return;

            transform.position = followTarget.position;
        }

        private void FollowHookAnimation()
        {
            moveTween = DOVirtual.DelayedCall(moveDuration, OnArrived)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .SetTarget(gameObject);
        }

        private void OnArrived()
        {
            isFollowing = false;
            followTarget = null;

            rotationTween?.Kill();
            rotationTween = null;

            transform.rotation = Quaternion.identity;

            finishSequence = DOTween.Sequence()
                .AppendInterval(displayDuration)
                .Append(transform.DOScale(Vector3.zero, disappearDuration))
                .SetEase(Ease.InBack)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .OnComplete(() => Destroy(gameObject));
            
        }

        private void OnDestroy()
        {
            moveTween?.Kill();
            rotationTween?.Kill();
            finishSequence?.Kill();
        }

        private void StartFlapping()
        {
            rotationTween = transform.DORotate(new Vector3(0f, 0f, rotaionAmount), rotationDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }
    }
}
