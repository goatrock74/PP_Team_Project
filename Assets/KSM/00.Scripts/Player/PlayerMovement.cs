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
 
        public Vector2 MoveInput => dir;
 
        public float FacingX { get; private set; } = 1f;
        public Vector2 FacingDirection => new Vector2(FacingX, 0f);
 
        
        public bool HoldLocked { get; set; }
 
        public bool IsLocked => HoldLocked || Time.time < _lockUntil;
 
        public bool IsMoving => !IsLocked && dir.sqrMagnitude > 0.01f;
 
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }
 
        private void Update()
        {
            UpdateFacing();
        }
 
        private void FixedUpdate()
        {
            _rb.linearVelocity = IsLocked ? Vector2.zero : dir * speed;
        }
 
        private void UpdateFacing()
        {
            if (freezeFacingWhileLocked && IsLocked) return;
            if (Mathf.Abs(dir.x) > 0.01f) FacingX = Mathf.Sign(dir.x);
        }
 
        public void OnMove(InputValue value)
        {
            dir = value.Get<Vector2>();
            UpdateFacing();
        }
        public void LockFor(float seconds)
        {
            if (seconds <= 0f) return;
 
            float until = Time.time + seconds;
            if (until > _lockUntil) _lockUntil = until;
        }
        public void Unlock()
        {
            _lockUntil = 0f;
            HoldLocked = false;
        }
    }
}