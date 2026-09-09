using UnityEngine;
using UnityEngine.InputSystem;
 
namespace KSM._00.Scripts
{
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private float speed = 4f;
 
        private Vector2 dir;
        private Rigidbody2D _rb;
        private float _lockUntil;
 
        /// <summary>지금 들어오고 있는 이동 입력</summary>
        public Vector2 MoveInput => dir;
 
        /// <summary>
        /// 마지막으로 향한 좌우 방향. +1 이면 오른쪽, -1 이면 왼쪽.
        /// 위아래로만 움직여도 값이 유지된다.
        /// </summary>
        public float FacingX { get; private set; } = 1f;
 
        /// <summary>정면 방향 벡터. 도구의 판정 상자가 놓일 쪽</summary>
        public Vector2 FacingDirection => new Vector2(FacingX, 0f);
 
        /// <summary>도구 사용 등으로 발이 묶여 있는가</summary>
        public bool IsLocked => Time.time < _lockUntil;
 
        /// <summary>실제로 움직이는 중인가 (입력이 있고 잠기지 않았을 때)</summary>
        public bool IsMoving => !IsLocked && dir.sqrMagnitude > 0.01f;
 
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }
 
        private void FixedUpdate()
        {
            // 잠긴 동안에도 입력은 계속 받아둔다. 풀리면 바로 이어서 움직인다
            _rb.linearVelocity = IsLocked ? Vector2.zero : dir * speed;
        }
 
        public void OnMove(InputValue value)
        {
            dir = value.Get<Vector2>();
 
            // 좌우 입력이 있을 때만 정면을 갱신한다
            if (Mathf.Abs(dir.x) > 0.01f) FacingX = Mathf.Sign(dir.x);
        }
 
        // ════════════════════════════════════════════════════════════
 
        /// <summary>
        /// 이 시간(초) 동안 제자리에 묶어둔다. 도구를 휘두를 때 쓴다.
        /// 이미 더 긴 잠금이 걸려 있으면 그쪽을 유지한다.
        /// </summary>
        public void LockFor(float seconds)
        {
            if (seconds <= 0f) return;
 
            float until = Time.time + seconds;
            if (until > _lockUntil) _lockUntil = until;
        }
 
        /// <summary>잠금을 즉시 해제 (연출 취소 등)</summary>
        public void Unlock() => _lockUntil = 0f;
    }
}