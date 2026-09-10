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
    /// 실제 제작은 이미 끝난 뒤에 재생된다. 순수 연출이라 없어도 게임은 돈다.
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
 
        private Coroutine _routine;
 
        private void Awake()
        {
            if (root == null) root = gameObject;
            if (group == null) group = root.GetComponent<CanvasGroup>();
 
            root.SetActive(false);
        }
 
        /// <summary>연출을 재생하고, 끝나면 onDone 을 부른다</summary>
        public void Play(Action onDone = null)
        {
            if (_routine != null) StopCoroutine(_routine);
 
            if (label != null) label.text = message;
 
            _routine = StartCoroutine(Routine(onDone));
        }
 
        private IEnumerator Routine(Action onDone)
        {
            IsPlaying = true;
 
            root.SetActive(true);
            transform.SetAsLastSibling();          // 다른 UI 위에 덮는다
 
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
 