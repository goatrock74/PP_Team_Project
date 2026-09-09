using System.Collections;
using UnityEngine;
 
namespace KSM._00.Scripts.Effects
{
    /// <summary>
    /// 잠깐 흔들리는 연출. 작물을 캘 때, 나무를 칠 때 등에 쓴다.
    ///
    /// 흔들 대상을 따로 지정할 수 있다. 비워두면 자기 자신을 흔드는데,
    /// 콜라이더가 같이 붙어 있으면 판정까지 흔들리므로
    /// <b>스프라이트만 있는 자식 오브젝트를 지정하는 편이 안전하다.</b>
    /// </summary>
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
 
        /// <summary>인스펙터에 설정된 기본 지속 시간. 다른 스크립트가 비율로 줄여 쓸 때 참고한다</summary>
        public float Duration => duration;
 
        /// <summary>인스펙터에 설정된 기본 세기. ShakeOnContact 가 이 값을 비율로 줄여 쓴다</summary>
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
            // 흔들리는 도중에 꺼지면 어긋난 위치로 굳어버린다
            if (target != null) target.localPosition = _basePosition;
            _running = null;
        }
 
        public void Shake() => Shake(duration, strength);
 
        public void Shake(float customDuration, float customStrength)
        {
            if (!isActiveAndEnabled || target == null) return;
 
            // 이미 흔들리는 중이면 처음부터 다시. 기준 위치는 그대로 유지된다
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
                float decay = 1f - progress;                                   // 갈수록 약해진다
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
 