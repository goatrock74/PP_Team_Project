using TMPro;
using UnityEngine;
using KSM._00.Scripts.Crop;


    public class RemoveModeUI : MonoBehaviour
    {
        [Header("참조 (비우면 자동으로 찾음)")]
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text label;

        [Header("문구")]
        [SerializeField] private string message = "작물 제거 모드  ·  X 로 취소";

        [Header("깜빡임")]
        [Tooltip("켜져 있는 동안 은은하게 깜빡여서 모드가 켜진 걸 눈치채게 한다")]
        [SerializeField] private bool pulse = true;

        [SerializeField, Range(0f, 1f)] private float pulseMinAlpha = 0.55f;
        [SerializeField, Min(0.1f)] private float pulseSpeed = 3f;

        private bool _on;

        private void Awake()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            if (label == null) label = GetComponentInChildren<TMP_Text>(true);

            // 배너가 마우스를 가로채지 않게 한다
            group.interactable = false;
            group.blocksRaycasts = false;

            if (label != null)
            {
                label.raycastTarget = false;
                label.text = message;
            }

            // OnEnable 이 아니라 Awake 에서 구독한다.
            // 누가 이 컴포넌트를 잠깐 꺼도 구독이 풀리지 않게
            PlayerInteractor.RemoveModeChanged += Apply;
            Apply(PlayerInteractor.IsRemoveMode);
        }

        private void OnDestroy()
        {
            PlayerInteractor.RemoveModeChanged -= Apply;
        }

        private void Update()
        {
            if (!_on || !pulse || group == null) return;

            float t = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
            group.alpha = Mathf.Lerp(pulseMinAlpha, 1f, t);
        }

        private void Apply(bool on)
        {
            _on = on;
            if (group != null) group.alpha = on ? 1f : 0f;
        }
    }
