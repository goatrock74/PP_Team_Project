using UnityEngine;
 
namespace KSM._00.Scripts
{
    /// <summary>
    /// 이동·대기 애니메이션과 좌우 반전을 담당한다.
    ///
    /// 방향은 <b>이동 입력의 좌우</b>만 본다 (PlayerMovement.FacingX).
    ///
    /// ★ 좌우 반전 방식이 두 가지다. 낚시가 있으면 반드시 Scale Root 를 써야 한다.
    ///
    ///   Sprite Flip X  — SpriteRenderer.flipX 로 <b>그림만</b> 뒤집는다.
    ///                    자식 Transform 은 그대로 있어서, 플레이어 앞에 둔 기준점
    ///                    (BobberPoint, FishPullPoint 등)이 왼쪽을 봐도 오른쪽에 남는다.
    ///                    → 왼쪽에서 낚시하면 찌와 물고기가 등 뒤에서 나온다.
    ///
    ///   Scale Root     — 루트의 localScale.x 를 음수로 만들어 <b>통째로</b> 뒤집는다.
    ///                    자식 전부가 같이 미러링되므로 기준점도 알아서 반대편으로 간다.
    ///                    애니메이션이 써넣는 localPosition 도 부모 스케일을 타고
    ///                    자동으로 뒤집히기 때문에, 오른쪽 기준으로 찍은 키프레임
    ///                    하나로 양쪽 다 처리된다.
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        public enum FlipMode
        {
            /// <summary>그림만 뒤집기. 자식 기준점은 안 따라온다</summary>
            SpriteFlipX = 0,
 
            /// <summary>루트를 통째로 뒤집기. 자식 기준점도 같이 미러링된다</summary>
            ScaleRoot = 1,
        }
 
        [Header("참조 (비우면 자동으로 찾음)")]
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerMovement movement;
 
        [Tooltip("Sprite Flip X 방식일 때 뒤집을 렌더러")]
        [SerializeField] private SpriteRenderer bodyRenderer;
 
        [Header("Animator 파라미터 (비우면 건너뜀)")]
        [Tooltip("걷는 중이면 1, 멈춰 있으면 0 이 들어가는 Float")]
        [SerializeField] private string speedParam = "Speed";
 
        [Tooltip("바라보는 좌우 방향(+1 / -1)이 들어가는 Float")]
        [SerializeField] private string dirXParam = "DirX";
 
        [Header("좌우 반전")]
        [Tooltip("낚시처럼 '플레이어 앞의 기준점'을 쓰는 기능이 있으면 Scale Root 를 써야 한다")]
        [SerializeField] private FlipMode flipMode = FlipMode.ScaleRoot;
 
        [Tooltip("Scale Root 방식일 때 뒤집을 오브젝트.\n" +
                 "★ 스프라이트와 기준점(FishPullPoint, BobberPoint)이 모두 이 밑에 있어야 한다.\n" +
                 "비우면 이 오브젝트 자신을 뒤집는다")]
        [SerializeField] private Transform flipRoot;
 
        /// <summary>바라보는 방향 (좌우만)</summary>
        public Vector2 Facing => movement != null ? movement.FacingDirection : Vector2.right;
 
        private float _baseScaleX = 1f;
 
        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (movement == null) movement = GetComponent<PlayerMovement>();
            if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>();
            if (flipRoot == null) flipRoot = transform;
 
            // 기준 크기를 기억해둔다. 매 프레임 현재값에서 읽으면 오차가 누적될 수 있다
            _baseScaleX = Mathf.Abs(flipRoot.localScale.x);
            if (Mathf.Approximately(_baseScaleX, 0f)) _baseScaleX = 1f;
        }
 
        private void Update()
        {
            if (animator == null || movement == null) return;
 
            // 도구를 쓰느라 발이 묶였으면 입력이 있어도 걷는 게 아니다
            bool moving = movement.IsMoving;
 
            SetFloatIfExists(speedParam, moving ? 1f : 0f);
            SetFloatIfExists(dirXParam, movement.FacingX);
 
            ApplyFacing(movement.FacingX < 0f);
        }
 
        // ════════════════════════════════════════════════════════════
 
        private void ApplyFacing(bool facingLeft)
        {
            if (flipMode == FlipMode.SpriteFlipX)
            {
                if (bodyRenderer != null) bodyRenderer.flipX = facingLeft;
                return;
            }
 
            if (flipRoot == null) return;
 
            // Scale Root — 자식 전부가 같이 뒤집힌다
            float want = facingLeft ? -_baseScaleX : _baseScaleX;
 
            Vector3 s = flipRoot.localScale;
            if (Mathf.Approximately(s.x, want)) return;   // 매 프레임 대입하지 않는다
 
            flipRoot.localScale = new Vector3(want, s.y, s.z);
 
            // 두 방식을 같이 쓰면 두 번 뒤집혀서 원래대로 돌아온다
            if (bodyRenderer != null && bodyRenderer.flipX) bodyRenderer.flipX = false;
        }
 
        /// <summary>없는 파라미터에 값을 넣으면 경고가 쏟아지므로 확인 후 설정</summary>
        private void SetFloatIfExists(string paramName, float value)
        {
            if (string.IsNullOrWhiteSpace(paramName) || animator == null) return;
 
            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.type != AnimatorControllerParameterType.Float || p.name != paramName) continue;
 
                animator.SetFloat(paramName, value);
                return;
            }
        }
    }
}