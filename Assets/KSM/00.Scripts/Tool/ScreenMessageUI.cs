using System.Collections;
using TMPro;
using UnityEngine;


    /// <summary>
    /// 화면 아래쪽에 잠깐 떴다 사라지는 안내 문구. "채집불가" 같은 것.
    ///
    ///   ScreenMessageUI.Show("채집불가");                    // 기본 색(빨강)
    ///   ScreenMessageUI.Show("가방이 가득 찼습니다", Color.yellow);
    ///
    /// 씬 구조 (Canvas 밑):
    ///   ScreenMessage     RectTransform — Anchor 아래 가운데, Pos Y 160, Width 700, Height 60
    ///                     + CanvasGroup + ScreenMessageUI
    ///    └ Text (TMP)     Stretch, 가운데 정렬, 한글 폰트, ★ Raycast Target 끄기
    ///
    /// ★ 이 오브젝트는 꺼두지 말 것. 꺼지면 코루틴을 못 돌려서 글자가 안 뜬다.
    ///   안 보일 때는 CanvasGroup 알파로 숨긴다 (Awake 에서 알아서 0 으로 만든다).
    /// ★ 같은 문구를 연타하면 처음부터 다시 깜빡이지 않고 떠 있는 시간만 늘어난다.
    /// </summary>
    public class ScreenMessageUI : MonoBehaviour
    {
        private static ScreenMessageUI _instance;

        [Header("참조")]
        [Tooltip("비우면 자식에서 찾는다")]
        [SerializeField] private TMP_Text label;

        [Tooltip("비우면 자신에게서 찾고, 없으면 붙인다")]
        [SerializeField] private CanvasGroup group;

        [Header("모양")]
        [SerializeField] private Color defaultColor = new Color(1f, 0.35f, 0.35f);

        [Header("시간 (초)")]
        [SerializeField, Min(0f)] private float fadeInTime = 0.08f;
        [SerializeField, Min(0f)] private float holdTime = 1.2f;
        [SerializeField, Min(0f)] private float fadeOutTime = 0.35f;

        [Header("튀는 연출")]
        [Tooltip("뜰 때 이 배율로 커졌다가 원래 크기로 돌아온다. 1 이면 연출 없음")]
        [SerializeField, Min(1f)] private float punchScale = 1.15f;

        [SerializeField, Min(0.01f)] private float punchTime = 0.12f;

        private RectTransform _rect;
        private Coroutine _routine;
        private float _hideAt;
        private float _punchStart = -999f;

        // 도메인 리로드를 끈 설정에서도 static 이 이전 플레이 값으로 남지 않게
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() => _instance = null;

        // ════════════════════════════════════════════════════════════
        //  밖에서 부르는 곳
        // ════════════════════════════════════════════════════════════

        public static void Show(string message) => ShowInternal(message, null);

        public static void Show(string message, Color color) => ShowInternal(message, color);

        private static void ShowInternal(string message, Color? color)
        {
            if (string.IsNullOrEmpty(message)) return;

            if (_instance == null)
            {
                Debug.Log($"[화면메시지] {message}   (씬에 ScreenMessageUI 가 없어서 콘솔에만 찍습니다)");
                return;
            }

            _instance.Display(message, color ?? _instance.defaultColor);
        }

        // ════════════════════════════════════════════════════════════

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[화면메시지] ScreenMessageUI 가 씬에 두 개 있습니다. 하나만 남겨주세요.", this);
                Destroy(this);
                return;
            }

            _instance = this;
            _rect = transform as RectTransform;

            if (label == null) label = GetComponentInChildren<TMP_Text>(true);
            if (group == null) group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();

            // ★ 글자가 마우스를 가로채면 PlayerInteractor 가 "UI 위를 클릭했다" 고 보고
            //   밭 클릭·도구 사용을 막아버린다. 안 보일 때도, 보일 때도 클릭은 통과시킨다
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            if (label != null) label.raycastTarget = false;
            else Debug.LogError("[화면메시지] 자식에 Text (TMP) 가 없습니다.", this);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Display(string message, Color color)
        {
            if (label == null) return;

            // 꺼진 오브젝트에서는 StartCoroutine 이 예외를 낸다 (CraftingOverlay 에서 겪은 것)
            if (!isActiveAndEnabled)
            {
                Debug.LogWarning($"[화면메시지] 오브젝트가 꺼져 있어 '{message}' 를 못 띄웠습니다. " +
                                 "ScreenMessage 오브젝트를 켜두세요 (숨기는 건 알파로 합니다).", this);
                return;
            }

            label.text = message;
            label.color = color;

            _hideAt = Time.unscaledTime + fadeInTime + holdTime;
            _punchStart = Time.unscaledTime;

            transform.SetAsLastSibling();             // 다른 UI 위에 그려지게

            // 이미 떠 있으면 코루틴이 알아서 다시 올라온다. 새로 시작하지 않는다
            if (_routine == null) _routine = StartCoroutine(Routine());
        }

        private IEnumerator Routine()
        {
            while (true)
            {
                // 올라오기
                while (group.alpha < 1f)
                {
                    group.alpha = fadeInTime <= 0f
                        ? 1f
                        : Mathf.MoveTowards(group.alpha, 1f, Time.unscaledDeltaTime / fadeInTime);

                    ApplyPunch();
                    yield return null;
                }

                // 머무르기 — 연타하면 _hideAt 이 뒤로 밀린다
                while (Time.unscaledTime < _hideAt)
                {
                    ApplyPunch();
                    yield return null;
                }

                // 사라지기 — 도중에 다시 불리면 멈추고 위로 돌아간다
                while (group.alpha > 0f && Time.unscaledTime >= _hideAt)
                {
                    group.alpha = fadeOutTime <= 0f
                        ? 0f
                        : Mathf.MoveTowards(group.alpha, 0f, Time.unscaledDeltaTime / fadeOutTime);

                    ApplyPunch();
                    yield return null;
                }

                if (group.alpha <= 0f && Time.unscaledTime >= _hideAt) break;
            }

            if (_rect != null) _rect.localScale = Vector3.one;
            _routine = null;
        }

        private void ApplyPunch()
        {
            if (_rect == null || punchScale <= 1f) return;

            float k = Mathf.Clamp01((Time.unscaledTime - _punchStart) / punchTime);
            float s = Mathf.Lerp(punchScale, 1f, k);           // 크게 시작해서 원래 크기로

            _rect.localScale = new Vector3(s, s, 1f);
        }
    }
