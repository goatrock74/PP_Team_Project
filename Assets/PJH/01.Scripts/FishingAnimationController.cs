using UnityEngine;
using DG.Tweening;
 
namespace PJH.Scripts
{
    /// <summary>
    /// 낚을 때 캐릭터가 부르르 떨리는 연출. 순수 연출이라 없어도 게임은 돈다.
    ///
    /// ★ 원본에서 보강한 것 2가지
    ///   1) Awake 의 rendererTransform null 검사 — 안 채우면 시작하자마자
    ///      NullReferenceException 이 나서 컴포넌트가 통째로 죽는다.
    ///   2) Play("Landing") 전에 스테이트 존재 확인 — Animator 에 그 스테이트가 없으면
    ///      조용히 무시되는데, 그러면 "왜 안 떨리지" 를 한참 찾게 된다.
    ///      친구 프로젝트에는 Landing 이 있지만 이쪽 Animator 에는 없을 수 있다.
    /// </summary>
    public class FishingAnimationController : MonoBehaviour
    {
        #region 필드
 
        [Tooltip("플레이어의 Animator. 비우면 자신과 자식에서 찾는다")]
        [SerializeField] private Animator playerAnimator;
 
        [Tooltip("흔들 대상 — 스프라이트가 있는 자식 오브젝트.\n" +
                 "★ 콜라이더가 같이 붙은 오브젝트를 넣으면 판정까지 흔들린다")]
        [SerializeField] private Transform rendererTransform;
 
        [Header("애니메이션")]
        [Tooltip("떨기 시작할 때 재생할 스테이트 이름. 없으면 비워둬도 된다")]
        [SerializeField] private string shakeStateName = "Landing";
 
        [SerializeField, Min(0)] private int animatorLayer;
 
        [Header("떨림")]
        [SerializeField, Min(0.01f)] private float shakeDuration = 0.15f;
        [SerializeField] private Vector3 shakeStrength = new Vector3(0.06f, 0f, 0f);
        [SerializeField, Min(1)] private int vibrato = 6;
        [SerializeField, Min(0f)] private float randomness = 10f;
 
        private Tweener shakeTween;
        private Vector3 originalLocalPosition;
        private bool isShaking;
 
        #endregion
 
        #region 초기화
 
        private void Awake()
        {
            if (rendererTransform == null)
            {
                Debug.LogWarning(
                    "[낚시연출] Renderer Transform 이 비어 있어 자기 자신을 흔듭니다. " +
                    "스프라이트가 있는 자식 오브젝트를 넣어주세요.", this);
 
                rendererTransform = transform;
            }
 
            if (playerAnimator == null) playerAnimator = GetComponentInChildren<Animator>();
 
            originalLocalPosition = rendererTransform.localPosition;
        }
 
        private void OnDisable()
        {
            StopFishingShake();
        }
 
        #endregion
 
        #region 낚시 떨림 연출
 
        public void ShakePlayer()
        {
            shakeTween?.Kill();
 
            PlayShakeState();
 
            if (rendererTransform == null) return;
 
            originalLocalPosition = rendererTransform.localPosition;
            isShaking = true;
 
            shakeTween = rendererTransform
                .DOShakePosition(shakeDuration, shakeStrength, vibrato, randomness, false, false)
                .SetLoops(-1, LoopType.Restart)
                .SetLink(rendererTransform.gameObject, LinkBehaviour.KillOnDestroy);
        }
 
        public void StopFishingShake()
        {
            if (!isShaking) return;
 
            shakeTween?.Kill();
            shakeTween = null;
 
            if (rendererTransform != null)
                rendererTransform.localPosition = originalLocalPosition;
 
            isShaking = false;
        }
 
        #endregion
 
      
        private void PlayShakeState()
        {
            if (playerAnimator == null || string.IsNullOrWhiteSpace(shakeStateName)) return;
 
            int hash = Animator.StringToHash(shakeStateName);
 
            if (!playerAnimator.HasState(animatorLayer, hash))
            {
                Debug.LogWarning(
                    $"[낚시연출] Animator 에 '{shakeStateName}' 스테이트가 없습니다. " +
                    "이름을 고치거나 Shake State Name 을 비워주세요.", this);
 
                return;
            }
 
            playerAnimator.Play(hash, animatorLayer, 0f);
        }
    }
}
 