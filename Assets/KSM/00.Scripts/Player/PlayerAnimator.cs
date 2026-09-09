using UnityEngine;
 
namespace KSM._00.Scripts
{
    /// <summary>
    /// 이동·대기 애니메이션과 좌우 반전을 담당한다.
    ///
    /// 방향은 <b>이동 입력의 좌우</b>만 본다 (PlayerMovement.FacingX).
    /// 마우스 위치는 방향에 관여하지 않는다.
    ///
    /// Animator 에 없는 파라미터는 알아서 건너뛰므로,
    /// 쓰지 않는 파라미터는 이름 칸을 비워두면 된다.
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        [Header("참조 (비우면 자동으로 찾음)")]
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerMovement movement;
 
        [Tooltip("좌우 반전용 스프라이트 렌더러")]
        [SerializeField] private SpriteRenderer bodyRenderer;
 
        [Header("Animator 파라미터 (비우면 건너뜀)")]
        [Tooltip("걷는 중이면 1, 멈춰 있으면 0 이 들어가는 Float")]
        [SerializeField] private string speedParam = "Speed";
 
        [Tooltip("바라보는 좌우 방향(+1 / -1)이 들어가는 Float")]
        [SerializeField] private string dirXParam = "DirX";
 
        [Header("설정")]
        [Tooltip("왼쪽을 볼 때 스프라이트를 뒤집는다. 좌우 그림이 따로 있으면 끄기")]
        [SerializeField] private bool flipSpriteForLeft = true;
 
        /// <summary>바라보는 방향 (좌우만)</summary>
        public Vector2 Facing => movement != null ? movement.FacingDirection : Vector2.right;
 
        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (movement == null) movement = GetComponent<PlayerMovement>();
            if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>();
        }
 
        private void Update()
        {
            if (animator == null || movement == null) return;
 
            // 도구를 쓰느라 발이 묶였으면 입력이 있어도 걷는 게 아니다
            bool moving = movement.IsMoving;
 
            SetFloatIfExists(speedParam, moving ? 1f : 0f);
            SetFloatIfExists(dirXParam, movement.FacingX);
 
            if (flipSpriteForLeft && bodyRenderer != null)
                bodyRenderer.flipX = movement.FacingX < 0f;
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