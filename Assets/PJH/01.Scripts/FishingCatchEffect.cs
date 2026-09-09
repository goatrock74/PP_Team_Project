using DG.Tweening;
using UnityEngine;
 
namespace PJH.Scripts
{
    /// <summary>
    /// 잡은 물고기가 낚싯대 궤적을 따라 딸려 올라오는 연출.
    ///
    /// 움직임은 이 스크립트가 아니라 <b>애니메이션 클립</b>이 만든다.
    /// FishingHook 클립이 FishPullPoint 의 Position 에 키프레임을 찍어두었고,
    /// 여기서는 매 프레임 그 기준점 위치를 따라가기만 한다.
    ///
    /// ★ 원본에서 보강한 것 2가지
    ///   1) spriteRenderer null 검사 — 비어 있으면 예외가 나고 그 아래 연출이 통째로 안 돌았다.
    ///   2) 좌우 반전 — 이 오브젝트는 부모 없이 생성돼서 캐릭터의 뒤집힌 스케일을
    ///      물려받지 못한다. 그래서 기준점의 lossyScale 부호를 보고 스스로 뒤집는다.
    ///      (자식으로 붙이는 구조였다면 자동으로 됐겠지만, 여기선 월드 좌표를 따라가는 방식이라
    ///       스케일이 전달되지 않는다)
    /// </summary>
    public class FishingCatchEffect : MonoBehaviour
    {
        [Header("Componets")]
        [SerializeField] private SpriteRenderer spriteRenderer;
 
        [Header("Movement")]
        [Tooltip("기준점을 따라다니는 시간. 회수 애니메이션 길이에 맞추면 된다")]
        [SerializeField] private float moveDuration = 0.7f;
 
        [Header("Display")]
        [SerializeField] private float displayDuration = 0.5f;
        [SerializeField] private float disappearDuration = 0.2f;
 
        [Header("Flapping")]
        [SerializeField] private float rotaionAmount = 10f;
        [SerializeField] private float rotationDuration = 0.1f;
 
        [Header("좌우 반전")]
        [Tooltip("끄면 항상 오른쪽을 본다. 물고기 그림이 좌우 대칭이면 꺼도 무방")]
        [SerializeField] private bool mirrorByTargetScale = true;
 
        private Tween moveTween;
        private Tween rotationTween;
        private Sequence finishSequence;
 
        private Vector3 originalScale;
        private Transform followTarget;
        private bool isFollowing;
 
        private void Awake()
        {
            originalScale = transform.localScale;
 
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
 
        public void Play(Sprite fishIcon, Transform targetPoint)
        {
            if (fishIcon == null)
            {
                Debug.LogWarning("[낚시연출] 물고기 아이콘이 없습니다. FishDataSO 의 Icon 을 확인해주세요.", this);
                Destroy(gameObject);
                return;
            }
 
            if (targetPoint == null)
            {
                Debug.LogWarning("[낚시연출] 도착 지점(Caught Fish Point)이 없습니다.", this);
                Destroy(gameObject);
                return;
            }
 
            if (spriteRenderer == null)
            {
                Debug.LogError("[낚시연출] Sprite Renderer 가 연결되지 않았습니다.", this);
                Destroy(gameObject);
                return;
            }
 
            spriteRenderer.sprite = fishIcon;
            transform.localScale = ScaleFor(targetPoint);
            transform.rotation = Quaternion.Euler(0f, 0f, -rotaionAmount);
 
            followTarget = targetPoint;
            isFollowing = true;
            transform.position = followTarget.position;
 
            StartFlapping();
            FollowHookAnimation();
        }
 
        private void LateUpdate()
        {
            if (!isFollowing || followTarget == null) return;
 
            transform.position = followTarget.position;
        }
 
        // ════════════════════════════════════════════════════════════
 
        /// <summary>
        /// 기준점이 뒤집힌 루트(VisualRoot) 안에 있으면 lossyScale.x 가 음수가 된다.
        /// 그 부호를 그대로 빌려와서 물고기 그림도 같은 쪽을 보게 한다.
        ///
        /// ★ 이걸 쓰면 SpriteRenderer.flipX 는 건드리면 안 된다. 두 번 뒤집힌다
        /// </summary>
        private Vector3 ScaleFor(Transform targetPoint)
        {
            float x = Mathf.Abs(originalScale.x);
 
            if (mirrorByTargetScale && targetPoint.lossyScale.x < 0f) x = -x;
 
            return new Vector3(x, originalScale.y, originalScale.z);
        }
 
        private void FollowHookAnimation()
        {
            // 이동은 애니메이션이 만든다. 여기서는 "언제 도착으로 칠지" 만 잰다
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
 
            // 회전만 되돌린다. 스케일은 반전 상태를 유지해야 한다
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
            rotationTween = transform
                .DORotate(new Vector3(0f, 0f, rotaionAmount), rotationDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }
    }
}
 