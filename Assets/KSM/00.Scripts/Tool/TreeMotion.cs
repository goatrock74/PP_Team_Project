using System.Collections;
using UnityEngine;
    public class TreeMotion : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("나무 그림. 비우면 자신과 자식에서 찾는다")]
        [SerializeField] private SpriteRenderer body;

        [Header("칠 때 — 휘청임")]
        [Tooltip("최대로 기우는 각도")]
        [SerializeField, Range(0f, 30f)] private float swayAngle = 7f;

        [SerializeField, Min(0.05f)] private float swayDuration = 0.45f;

        [Tooltip("몇 번 왔다 갔다 하는지")]
        [SerializeField, Range(0.5f, 5f)] private float swayCount = 2f;

        [Header("쓰러질 때")]
        [Tooltip("넘어가는 각도. 90 이면 완전히 눕는다")]
        [SerializeField, Range(30f, 100f)] private float fallAngle = 88f;

        [Tooltip("넘어가는 데 걸리는 시간. 처음엔 천천히, 점점 빨라진다")]
        [SerializeField, Min(0.05f)] private float fallTime = 0.6f;

        [Tooltip("땅에 닿고 튕겨 오르는 각도. 0 이면 안 튕긴다")]
        [SerializeField, Range(0f, 20f)] private float bounceAngle = 6f;

        [SerializeField, Min(0f)] private float bounceTime = 0.18f;

        [Tooltip("누운 채로 머무는 시간")]
        [SerializeField, Min(0f)] private float lieTime = 0.25f;

        [Tooltip("서서히 사라지는 시간")]
        [SerializeField, Min(0.05f)] private float fadeTime = 0.6f;

        [Header("다시 자랄 때")]
        [SerializeField] private bool fadeInOnRegrow = true;

        [SerializeField, Min(0.05f)] private float regrowTime = 0.5f;

        [Tooltip("이 크기에서 시작해 원래 크기로 자란다. 1 이면 크기 변화 없이 나타나기만 한다")]
        [SerializeField, Range(0.3f, 1f)] private float regrowStartScale = 0.85f;

        private Transform _target;
        private Quaternion _baseRotation;
        private Vector3 _baseScale;

        private Coroutine _sway;
        private Coroutine _regrow;
        private float _regrowTargetAlpha = 1f;

        public float FallTotalTime => fallTime + bounceTime + lieTime + fadeTime;

        private void Awake()
        {
            if (body == null) body = GetComponentInChildren<SpriteRenderer>();

            _target = body != null ? body.transform : transform;
            _baseRotation = _target.localRotation;
            _baseScale = _target.localScale;
        }

        private void OnDisable()
        {
          
            ResetPose();
        }

      
        public void Sway(float hitFromX)
        {
            if (_target == null || !isActiveAndEnabled || swayAngle <= 0f) return;

            if (_sway != null) StopCoroutine(_sway);
            _sway = StartCoroutine(SwayRoutine(AwayFrom(hitFromX)));
        }

        private IEnumerator SwayRoutine(int dir)
        {
            float t = 0f;

            while (t < swayDuration)
            {
                t += Time.deltaTime;

                float p = Mathf.Clamp01(t / swayDuration);
                float angle = dir * swayAngle * Mathf.Sin(p * Mathf.PI * 2f * swayCount) * (1f - p);   // 갈수록 잦아든다

                _target.localRotation = _baseRotation * Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }

            _target.localRotation = _baseRotation;
            _sway = null;
        }

  
        public float Fall(float hitFromX)
        {
            if (body == null || body.sprite == null || !isActiveAndEnabled) return 0f;

            ResetPose();

            var go = new GameObject("FallingTree");
            Transform tr = go.transform;

            tr.SetParent(_target.parent, false);
            tr.localPosition = _target.localPosition;
            tr.localRotation = _baseRotation;
            tr.localScale = _baseScale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = body.sprite;
            sr.color = body.color;
            sr.flipX = body.flipX;
            sr.flipY = body.flipY;
            sr.sharedMaterial = body.sharedMaterial;
            sr.sortingLayerID = body.sortingLayerID;
            sr.sortingOrder = body.sortingOrder + 1;          
            sr.spriteSortPoint = body.spriteSortPoint;

            StartCoroutine(FallRoutine(tr, sr, AwayFrom(hitFromX)));
            return FallTotalTime;
        }

        private IEnumerator FallRoutine(Transform tr, SpriteRenderer sr, int dir)
        {
            Quaternion baseRot = tr.localRotation;
            float t;
            
            t = 0f;
            while (t < fallTime)
            {
                if (tr == null) yield break;

                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / fallTime);

                tr.localRotation = baseRot * Quaternion.Euler(0f, 0f, dir * fallAngle * p * p);
                yield return null;
            }
            
            t = 0f;
            while (t < bounceTime)
            {
                if (tr == null) yield break;

                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / bounceTime);
                float angle = fallAngle - bounceAngle * Mathf.Sin(p * Mathf.PI);

                tr.localRotation = baseRot * Quaternion.Euler(0f, 0f, dir * angle);
                yield return null;
            }

            if (tr == null) yield break;
            tr.localRotation = baseRot * Quaternion.Euler(0f, 0f, dir * fallAngle);
            if (lieTime > 0f) yield return new WaitForSeconds(lieTime);


            if (sr == null) yield break;

            Color c = sr.color;
            float startAlpha = c.a;

            t = 0f;
            while (t < fadeTime)
            {
                if (sr == null) yield break;

                t += Time.deltaTime;
                c.a = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(t / fadeTime));
                sr.color = c;

                yield return null;
            }

            if (tr != null) Destroy(tr.gameObject);
        }
        public void PlayRegrow()
        {
            if (!fadeInOnRegrow || body == null || !isActiveAndEnabled) return;

            if (_regrow != null) StopCoroutine(_regrow);
            _regrow = StartCoroutine(RegrowRoutine());
        }

        private IEnumerator RegrowRoutine()
        {
            Color c = body.color;
            _regrowTargetAlpha = c.a;

            float t = 0f;
            while (t < regrowTime)
            {
                t += Time.deltaTime;

                float p = Mathf.Clamp01(t / regrowTime);
                float e = 1f - (1f - p) * (1f - p);                  

                c.a = Mathf.Lerp(0f, _regrowTargetAlpha, e);
                body.color = c;
                _target.localScale = _baseScale * Mathf.Lerp(regrowStartScale, 1f, e);

                yield return null;
            }

            c.a = _regrowTargetAlpha;
            body.color = c;
            _target.localScale = _baseScale;
            _regrow = null;
        }
        private void ResetPose()
        {
            if (_sway != null) { StopCoroutine(_sway); _sway = null; }

            if (_regrow != null)
            {
                StopCoroutine(_regrow);
                _regrow = null;

                if (body != null)
                {
                    Color c = body.color;
                    c.a = _regrowTargetAlpha;
                    body.color = c;
                }
            }

            if (_target == null) return;

            _target.localRotation = _baseRotation;
            _target.localScale = _baseScale;
        }
        private int AwayFrom(float hitFromX)
        {
            float myX = _target != null ? _target.position.x : transform.position.x;

            if (Mathf.Approximately(hitFromX, myX)) return Random.value < 0.5f ? -1 : 1;

            return hitFromX < myX ? -1 : 1;
        }
    }
