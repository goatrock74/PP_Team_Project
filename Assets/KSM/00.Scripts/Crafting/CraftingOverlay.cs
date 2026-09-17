using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KSM._00.Scripts.Crafting
{
    /// <summary>
    /// 제작할 때 화면을 덮는 "제작중" 연출.
    ///
    /// 씬 구조 (Canvas 밑, 다른 UI 보다 <b>아래쪽 형제</b>여야 위에 그려진다):
    ///   CraftingOverlay    Image(반투명 검정, 화면 전체 stretch) + CanvasGroup + 이 스크립트
    ///    ├ Icon            Image        ← 모루 그림
    ///    └ Label           TextMeshPro  ← "제작중"
    ///
    /// 연출이 끝나면 onDone 이 불린다. 제작 결과물은 그때 들어간다.
    /// </summary>
    public class CraftingOverlay : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("켜고 끌 루트. 비우면 이 오브젝트")]
        [SerializeField] private GameObject root;

        [Tooltip("페이드용. 없으면 그냥 켜고 꺼진다")]
        [SerializeField] private CanvasGroup group;

        [Tooltip("돌아가는 모루 그림 등. 없어도 된다")]
        [SerializeField] private RectTransform icon;

        [SerializeField] private TMP_Text label;

        [Header("연출")]
        [SerializeField] private string message = "제작중";

        [Tooltip("화면에 떠 있는 시간 (페이드 제외)")]
        [SerializeField, Min(0f)] private float holdTime = 0.8f;

        [SerializeField, Min(0f)] private float fadeInTime = 0.12f;
        [SerializeField, Min(0f)] private float fadeOutTime = 0.2f;

        [Tooltip("아이콘이 좌우로 흔들리는 각도. 0이면 가만히 있는다")]
        [SerializeField] private float iconSwingAngle = 18f;

        [SerializeField, Min(0.05f)] private float iconSwingPeriod = 0.35f;

        /// <summary>연출이 도는 중인가</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>
        /// 어느 것이든 연출이 도는 중인가.
        /// 인벤토리처럼 다른 창이 "지금은 열지 말자" 를 판단하는 데 쓴다.
        /// </summary>
        public static bool AnyPlaying { get; private set; }

        private Coroutine _routine;
        private bool _init;

        private void Awake()
        {
            EnsureInit();

            // 플레이를 다시 시작해도 static 이 true 로 굳지 않게 되돌린다
            AnyPlaying = false;

            // root 가 이 스크립트 자신이면 여기서 스스로 꺼진다.
            // 그래서 Play 는 반드시 먼저 켜고 나서 코루틴을 시작해야 한다
            root.SetActive(false);
        }

        /// <summary>
        /// Awake 가 못 돌았을 수도 있으므로(처음부터 꺼둔 오브젝트) 참조를 늦게라도 채운다
        /// </summary>
        private void EnsureInit()
        {
            if (_init) return;
            _init = true;

            if (root == null) root = gameObject;
            if (group == null) group = root.GetComponent<CanvasGroup>();
        }

        /// <summary>연출을 재생하고, 끝나면 onDone 을 부른다</summary>
        public void Play(Action onDone = null)
        {
            EnsureInit();

            // ★ 꺼진 오브젝트에서는 StartCoroutine 이 예외를 낸다.
            //   root 가 자기 자신이면 Awake 에서 이미 꺼둔 상태이므로 먼저 켠다
            root.SetActive(true);

            if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            {
                // 스크립트가 root 바깥의 꺼진 오브젝트에 있는 경우.
                // 연출은 장식이니 조용히 건너뛰고 제작 결과만 이어간다
                Debug.LogWarning("[제작연출] 이 오브젝트가 꺼져 있어 연출을 건너뜁니다. " +
                                 "CraftingOverlay 는 켜지는 오브젝트(root) 위에 두세요.", this);

                root.SetActive(false);
                onDone?.Invoke();
                return;
            }

            if (_routine != null) StopCoroutine(_routine);

            if (label != null) label.text = message;
            if (group != null) group.alpha = 0f;      // 이전 재생의 알파가 한 프레임 비치는 것 방지

            _routine = StartCoroutine(Routine(onDone));
        }

        private IEnumerator Routine(Action onDone)
        {
            IsPlaying = true;
            AnyPlaying = true;

            root.SetActive(true);                  // Play 에서 이미 켰지만 안전하게 한 번 더
            root.transform.SetAsLastSibling();     // 다른 UI 위에 덮는다

            yield return Fade(0f, 1f, fadeInTime);

            float t = 0f;
            while (t < holdTime)
            {
                t += Time.unscaledDeltaTime;
                SwingIcon(t);

                yield return null;
            }

            yield return Fade(1f, 0f, fadeOutTime);

            if (icon != null) icon.localRotation = Quaternion.identity;

            root.SetActive(false);
            IsPlaying = false;
            AnyPlaying = false;
            _routine = null;

            onDone?.Invoke();
        }

        private void SwingIcon(float time)
        {
            if (icon == null || iconSwingAngle <= 0f) return;

            float angle = Mathf.Sin(time / iconSwingPeriod * Mathf.PI * 2f) * iconSwingAngle;
            icon.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private IEnumerator Fade(float from, float to, float time)
        {
            if (group == null || time <= 0f)
            {
                if (group != null) group.alpha = to;
                yield break;
            }

            float t = 0f;
            while (t < time)
            {
                t += Time.unscaledDeltaTime;      // 시간을 멈춰도 연출은 흐른다
                group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / time));

                yield return null;
            }

            group.alpha = to;
        }
    }
}