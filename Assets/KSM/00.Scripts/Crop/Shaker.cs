using System.Collections;
using UnityEngine;
 
namespace KSM._00.Scripts.Effects
{
    public class Shaker : MonoBehaviour
    {
        [Tooltip("흔들 대상. 비우면 이 오브젝트")]
        [SerializeField] private Transform target;
 
        [SerializeField, Min(0.05f)] private float duration = 0.22f;
 
        [Tooltip("흔들리는 폭 (월드 단위)")]
        [SerializeField, Min(0.001f)] private float strength = 0.07f;
 
        [Tooltip("흔들리는 횟수. 클수록 잘게 떨린다")]
        [SerializeField, Min(1)] private int shakeCount = 4;
 
        [Tooltip("좌우로만 흔든다. 끄면 위아래로도 조금 움직인다")]
        [SerializeField] private bool horizontalOnly = true;
 
        public float Duration => duration;
 
        public float Strength => strength;
 
        private Vector3 _basePosition;
        private Coroutine _running;
 
        private void Awake()
        {
            if (target == null) target = transform;
            _basePosition = target.localPosition;
        }
 
        private void OnDisable()
        {
            if (target != null) target.localPosition = _basePosition;
            _running = null;
        }
 
        public void Shake() => Shake(duration, strength);
 
        public void Shake(float customDuration, float customStrength)
        {
            if (!isActiveAndEnabled || target == null) return;
 
            if (_running != null) StopCoroutine(_running);
 
            _running = StartCoroutine(ShakeRoutine(customDuration, customStrength));
        }
 
        private IEnumerator ShakeRoutine(float dur, float power)
        {
            float t = 0f;
 
            while (t < dur)
            {
                t += Time.deltaTime;
 
                float progress = t / dur;
                float decay = 1f - progress;                                 
                float wave = Mathf.Sin(progress * Mathf.PI * shakeCount * 2f);
                float offset = wave * power * decay;
 
                target.localPosition = _basePosition + (horizontalOnly
                    ? new Vector3(offset, 0f, 0f)
                    : new Vector3(offset, Mathf.Abs(offset) * 0.5f, 0f));
 
                yield return null;
            }
 
            target.localPosition = _basePosition;
            _running = null;
        }
    }
}
 