using UnityEngine;
 
namespace KSM._00.Scripts.Effects
{
    [RequireComponent(typeof(Collider2D))]
    public class ShakeOnContact : MonoBehaviour
    {
        [Tooltip("비우면 자신과 자식에서 찾는다")]
        [SerializeField] private Shaker shaker;
 
        [Tooltip("이 레이어에 있는 것이 닿았을 때만 흔들린다. 보통 Player 레이어")]
        [SerializeField] private LayerMask reactTo = ~0;
 
        [Tooltip("한 번 흔들린 뒤 이 시간(초)은 다시 안 흔들린다.\n" +
                 "작물 위에 서 있을 때 계속 떨리는 걸 막는다")]
        [SerializeField, Min(0f)] private float cooldown = 0.6f;
 
        [Tooltip("수확할 때보다 약하게. 1이면 Shaker 설정 그대로")]
        [SerializeField, Range(0.1f, 1f)] private float strengthScale = 0.5f;
 
        [Tooltip("들어올 때뿐 아니라 나갈 때도 흔들린다")]
        [SerializeField] private bool alsoOnExit = true;
 
        private float _nextShakeTime;
 
        private void Awake()
        {
            if (shaker == null) shaker = GetComponentInChildren<Shaker>();
        }
 
        private void OnTriggerEnter2D(Collider2D other) => TryShake(other);
 
        private void OnTriggerExit2D(Collider2D other)
        {
            if (alsoOnExit) TryShake(other);
        }
 
        private void TryShake(Collider2D other)
        {
            if (shaker == null || other == null) return;
            if ((reactTo.value & (1 << other.gameObject.layer)) == 0) return;
            if (Time.time < _nextShakeTime) return;
 
            _nextShakeTime = Time.time + cooldown;
            shaker.Shake(shaker.Duration, shaker.Strength * strengthScale);
        }
    }
}