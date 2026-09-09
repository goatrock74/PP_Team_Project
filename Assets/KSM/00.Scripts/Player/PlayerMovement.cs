using UnityEngine;
using UnityEngine.InputSystem;
 
namespace KSM._00.Scripts
{
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private float speed = 4f;
 
        [Tooltip("발이 묶여 있는 동안 바라보는 방향도 고정한다.\n" +
                 "끄면 제자리에서 좌우 입력만으로 몸이 돌아간다")]
        [SerializeField] private bool freezeFacingWhileLocked = true;
 
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
 
        /// <summary>
        /// 시간이 정해지지 않은 잠금. 켜 두는 동안 계속 멈춰 있는다.
        ///
        /// 낚시처럼 "언제 끝날지 모르는" 동작이 쓴다. 도구 휘두르기처럼
        /// 길이가 정해진 동작은 LockFor(초) 쪽을 쓰면 된다.
        ///
        /// ★ 켠 쪽이 반드시 다시 꺼야 한다. 안 끄면 영영 못 움직인다
        /// </summary>
        public bool HoldLocked { get; set; }
 
        /// <summary>도구 사용 등으로 발이 묶여 있는가</summary>
        public bool IsLocked => HoldLocked || Time.time < _lockUntil;
 
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
 
            // ★ 잠긴 동안에는 정면도 고정한다.
            //   안 그러면 제자리에 묶인 채로 A/D 를 눌러 몸만 빙글빙글 돈다.
            //   (낚싯대를 물에 던져놓고 반대쪽을 보는 그림이 나온다)
            //   도구를 휘두르는 중에도 마찬가지 — 판정 상자는 클릭 순간의 방향으로
            //   이미 정해졌는데 그림만 돌아가면 어긋나 보인다
            if (freezeFacingWhileLocked && IsLocked) return;
 
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
 
        /// <summary>잠금을 즉시 해제 (연출 취소 등). 시간 잠금과 홀드 잠금 둘 다 푼다</summary>
        public void Unlock()
        {
            _lockUntil = 0f;
            HoldLocked = false;
        }
    }
}