using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
 
namespace KSM._00.Scripts.Items
{
    /// <summary>
    /// 뽑기 룰렛. 카드가 가로로 흐르다 화살표 아래에서 감속하며 멈춘다.
    ///
    /// 동작 순서:
    ///   1) 결과를 먼저 정한다 (확률표로)
    ///   2) 띠를 만들면서 "멈출 자리"에 그 결과를 심어둔다
    ///   3) 그 자리가 화살표 아래 오도록 감속 이동시킨다
    ///   눈속임이 아니라 결과가 먼저고 연출이 나중이라 확률이 정확하다.
    ///
    /// 권장 씬 구조 — 이 스크립트는 항상 켜져 있는 오브젝트(Canvas)에 붙인다:
    ///   Canvas                     (GachaUI 를 여기에)
    ///    └ GachaPanel              Image(반투명 검정)   ← Panel 칸에 이걸 연결
    ///       ├ Viewport             Image + RectMask2D   ← 룰렛 창의 크기는 "이것"이 정한다
    ///       │   └ Strip            RectTransform 만. 크기는 0이어도 되고 뭐든 상관없다
    ///       ├ Arrow                Image                ← Arrow 칸에 연결
    ///       ├ ResultText           TextMeshPro
    ///       └ CloseButton          Button
    ///
    /// Strip 은 카드를 담아 움직이는 "빈 손잡이"다. 눈에 보이는 창 크기는 Viewport 가 정한다.
    ///
    /// 이 스크립트를 GachaPanel 자신에게 붙여도 동작하도록 자동 보정하지만,
    /// Canvas 에 붙이는 쪽이 깔끔하다.
    /// </summary>
    public class GachaUI : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private GameObject panel;
        [SerializeField] private RectTransform strip;
        [SerializeField] private GachaCardUI cardPrefab;
        [Tooltip("정지 지점 표시. 연결하면 이 위치에 정확히 맞춰 멈춘다.\n" +
                 "비워두면 Strip 부모의 정중앙을 기준으로 삼는다")]
        [SerializeField] private RectTransform arrow;
 
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private Button closeButton;
 
        [Header("연출")]
        [Tooltip("카드 하나의 가로 폭 + 간격")]
        [SerializeField] private float cardSpacing = 170f;
 
        [Tooltip("띠에 만들 카드 수")]
        [SerializeField, Min(10)] private int stripLength = 45;
 
        [Tooltip("몇 번째 카드에서 멈출지. stripLength 보다 작아야 한다")]
        [SerializeField, Min(5)] private int stopIndex = 38;
 
        [SerializeField, Min(0.2f)] private float spinDuration = 3f;
 
        [Tooltip("살짝 어긋나게 멈춘다 (0이면 항상 정확히 화살표 아래).\n" +
                 "카드 사이에 틈이 있으면 이 값을 키울수록 틈에 멈출 수 있으니 주의")]
        [SerializeField, Range(0f, 0.4f)] private float landingJitter;
 
        /// <summary>지금 뽑는 중인가. 다른 입력을 막을 때 쓴다</summary>
        public static bool IsSpinning { get; private set; }
 
        public bool IsOpen => _isOpen;
 
        private readonly List<GachaCardUI> _cards = new();
        private Coroutine _spin;
 
        private CanvasGroup _canvasGroup;
        private bool _useCanvasGroup;
        private bool _toggleModeReady;
        private bool _isOpen;
        private bool _buttonHooked;
 
        private void Awake() => HookCloseButton();
 
        private void Start()
        {
            // Open 이 먼저 불려서 이미 열려 있다면 건드리지 않는다
            if (!_isOpen) ApplyVisible(false);
        }
 
        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            IsSpinning = false;
        }
 
        // ════════════════════════════════════════════════════════════
 
        /// <summary>
        /// 팩을 열어 룰렛을 돌린다. 결과는 onResult 로 전달된다 (여러 번 뽑으면 여러 번 호출).
        /// 이미 돌아가는 중이면 무시하고 false 를 반환한다.
        /// </summary>
        public bool Open(ItemPackSO pack, Action<LootEntry> onResult)
        {
            if (IsSpinning) return false;
 
            if (pack == null || !pack.IsUsable)
            {
                Debug.LogWarning("[뽑기] 팩에 Loot Table 이 없거나 비어있습니다.", pack);
                return false;
            }
 
            if (panel == null || strip == null || cardPrefab == null)
            {
                Debug.LogError("[뽑기] Panel / Strip / Card Prefab 연결을 확인하세요.", this);
                return false;
            }
 
            // 꺼져 있는 오브젝트에서는 코루틴이 안 돈다. 먼저 살려둔다
            if (!EnsureActive()) return false;
 
            HookCloseButton();
 
            _isOpen = true;
            ApplyVisible(true);
 
            if (resultText != null) resultText.text = string.Empty;
            SetCloseInteractable(false);
 
            _spin = StartCoroutine(SpinRoutine(pack, onResult));
            return true;
        }
 
        public void Close()
        {
            if (IsSpinning) return;   // 돌아가는 중엔 못 닫는다
 
            if (_spin != null) { StopCoroutine(_spin); _spin = null; }
 
            _isOpen = false;
            ApplyVisible(false);
        }
 
        // ════════════════════════════════════════════════════════════
        //  켜고 끄기 — 자기 자신을 꺼버리는 상황까지 감안한다
        // ════════════════════════════════════════════════════════════
 
        /// <summary>
        /// 이 스크립트가 꺼진 오브젝트에 붙어 있으면 StartCoroutine 이 예외를 던진다.
        /// 조상까지 거슬러 올라가며 켜서 살려낸다.
        /// </summary>
        private bool EnsureActive()
        {
            if (gameObject.activeInHierarchy) return true;
 
            for (Transform t = transform; t != null; t = t.parent)
                if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
 
            Debug.LogWarning(
                "[뽑기] GachaUI 가 꺼져 있는 오브젝트에 붙어 있어 강제로 켰습니다.\n" +
                "GachaUI 는 Canvas 처럼 항상 켜져 있는 오브젝트에 붙이고, " +
                "Panel 칸에 GachaPanel 을 연결하세요.", this);
 
            return gameObject.activeInHierarchy;
        }
 
        /// <summary>
        /// 패널 안에 자기 자신이 들어 있으면 SetActive 로 껐을 때 같이 꺼져서
        /// 다시 열 방법이 없어진다. 그 경우 CanvasGroup 으로 대신 숨긴다.
        /// </summary>
        private void SetupToggleMode()
        {
            if (_toggleModeReady || panel == null) return;
            _toggleModeReady = true;
 
            _useCanvasGroup = transform.IsChildOf(panel.transform);
            if (!_useCanvasGroup) return;
 
            _canvasGroup = panel.GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = panel.AddComponent<CanvasGroup>();
 
            Debug.LogWarning(
                "[뽑기] GachaUI 가 Panel 안에 있어서 CanvasGroup 방식으로 전환했습니다. " +
                "동작은 하지만 Canvas 로 옮기는 게 더 깔끔합니다.", this);
        }
 
        private void ApplyVisible(bool visible)
        {
            SetupToggleMode();
            if (panel == null) return;
 
            if (_useCanvasGroup)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.interactable = visible;
                _canvasGroup.blocksRaycasts = visible;
            }
            else
            {
                panel.SetActive(visible);
            }
        }
 
        private void HookCloseButton()
        {
            if (_buttonHooked || closeButton == null) return;
 
            closeButton.onClick.AddListener(Close);
            _buttonHooked = true;
        }
 
        // ════════════════════════════════════════════════════════════
        //  연출
        // ════════════════════════════════════════════════════════════
 
        private IEnumerator SpinRoutine(ItemPackSO pack, Action<LootEntry> onResult)
        {
            IsSpinning = true;
 
            int rolls = Mathf.Max(1, pack.rollCount);
 
            for (int n = 0; n < rolls; n++)
            {
                // 1) 결과를 먼저 확정한다
                LootEntry result = pack.lootTable.Roll();
                if (!result.IsValid) break;
 
                // 2) 띠를 만들고 멈출 자리에 결과를 심는다
                BuildStrip(pack.lootTable, result);
 
                // 3) 그 자리가 화살표 아래 오도록 감속 이동
                yield return SlideToStop();
 
                // 4) 결과 표시 + 지급
                ShowResult(result, n + 1, rolls);
                onResult?.Invoke(result);
 
                if (n < rolls - 1) yield return new WaitForSecondsRealtime(0.7f);
            }
 
            IsSpinning = false;
            SetCloseInteractable(true);
            _spin = null;
        }
 
        private void BuildStrip(LootTableSO table, in LootEntry result)
        {
            EnsureCards(stripLength);
 
            int stop = Mathf.Clamp(stopIndex, 1, stripLength - 2);
 
            for (int i = 0; i < stripLength; i++)
            {
                // 멈출 자리에만 확정된 결과, 나머지는 분위기용 랜덤
                LootEntry entry = (i == stop) ? result : table.Roll();
 
                _cards[i].SetEntry(entry);
                ((RectTransform)_cards[i].transform).anchoredPosition = new Vector2(i * cardSpacing, 0f);
                _cards[i].gameObject.SetActive(true);
            }
 
            // 0번 카드가 화살표 아래 오도록 시작 위치를 잡는다
            strip.anchoredPosition = new Vector2(GetStopLineX(), 0f);
        }
 
        /// <summary>
        /// 카드가 멈춰야 할 x 좌표. Strip 의 anchoredPosition 과 같은 좌표계로 환산한다.
        /// Arrow 를 연결해두면 Strip 이 정중앙에 있지 않아도 정확히 그 아래 멈춘다.
        /// </summary>
        private float GetStopLineX()
        {
            if (arrow == null) return 0f;
            if (strip.parent is not RectTransform parent) return 0f;
 
            Vector3 local = parent.InverseTransformPoint(arrow.position);
            return local.x;
        }
 
        private IEnumerator SlideToStop()
        {
            int stop = Mathf.Clamp(stopIndex, 1, stripLength - 2);
 
            float line = GetStopLineX();
            float jitter = UnityEngine.Random.Range(-landingJitter, landingJitter) * cardSpacing;
 
            float from = strip.anchoredPosition.x;
            float to = line - (stop * cardSpacing) + jitter;
            float t = 0f;
 
            while (t < spinDuration)
            {
                // Time.timeScale 이 0이어도 돌아가도록 unscaled 를 쓴다
                t += Time.unscaledDeltaTime;
 
                float k = Mathf.Clamp01(t / spinDuration);
                float eased = 1f - Mathf.Pow(1f - k, 4f);   // 빠르게 출발해 부드럽게 정지
 
                strip.anchoredPosition = new Vector2(Mathf.Lerp(from, to, eased), 0f);
                yield return null;
            }
 
            strip.anchoredPosition = new Vector2(to, 0f);
        }
 
        private void ShowResult(in LootEntry result, int index, int total)
        {
            if (resultText == null) return;
 
            int count = result.RollCount();
            string hex = ItemRarityUtil.ColorHex(result.rarity);
 
            string head = total > 1 ? $"<size=70%>{index} / {total}</size>\n" : string.Empty;
            string amount = count > 1 ? $" x{count}" : string.Empty;
 
            resultText.text = $"{head}<color=#{hex}>{ItemRarityUtil.DisplayName(result.rarity)}</color>  " +
                              $"<b>{result.item.DisplayName}</b>{amount}";
        }
 
        private void EnsureCards(int count)
        {
            while (_cards.Count < count)
            {
                GachaCardUI card = Instantiate(cardPrefab, strip);
                card.name = $"Card_{_cards.Count:00}";
 
                // Strip 의 "중앙" 을 기준으로 잡는다.
                // 왼쪽 끝(0, 0.5)을 기준으로 하면 Strip 의 가로 길이만큼 위치가 통째로 밀린다
                var rt = (RectTransform)card.transform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
 
                _cards.Add(card);
            }
 
            for (int i = count; i < _cards.Count; i++)
                _cards[i].gameObject.SetActive(false);
        }
 
        private void SetCloseInteractable(bool on)
        {
            if (closeButton != null) closeButton.interactable = on;
        }
    }
}
 