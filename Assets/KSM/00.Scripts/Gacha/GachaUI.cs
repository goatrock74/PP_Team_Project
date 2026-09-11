using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
 
namespace KSM._00.Scripts.Items
{
   
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
            if (!_isOpen) ApplyVisible(false);
        }
 
        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            IsSpinning = false;
        }
 
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
            if (IsSpinning) return;  
 
            if (_spin != null) { StopCoroutine(_spin); _spin = null; }
 
            _isOpen = false;
            ApplyVisible(false);
        }
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
 
        private IEnumerator SpinRoutine(ItemPackSO pack, Action<LootEntry> onResult)
        {
            IsSpinning = true;
 
            int rolls = Mathf.Max(1, pack.rollCount);
 
            for (int n = 0; n < rolls; n++)
            {
                LootEntry result = pack.lootTable.Roll();
                if (!result.IsValid) break;
 
                BuildStrip(pack.lootTable, result);
                yield return SlideToStop();
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
                LootEntry entry = (i == stop) ? result : table.Roll();
 
                _cards[i].SetEntry(entry);
                ((RectTransform)_cards[i].transform).anchoredPosition = new Vector2(i * cardSpacing, 0f);
                _cards[i].gameObject.SetActive(true);
            }
            strip.anchoredPosition = new Vector2(GetStopLineX(), 0f);
        }
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
                t += Time.unscaledDeltaTime;
 
                float k = Mathf.Clamp01(t / spinDuration);
                float eased = 1f - Mathf.Pow(1f - k, 4f); 
 
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
 