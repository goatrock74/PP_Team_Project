using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using KSM._00.Scripts.Crafting;
using KSM._00.Scripts.Crop;
using KSM._00.Scripts.Items;

    public class ControlsGuideUI : MonoBehaviour
    {
        [Serializable]
        public struct Entry
        {
            [Tooltip("키 이름. 예: Tab, 좌클릭")]
            public string key;

            [Tooltip("하는 일. 예: 인벤토리")]
            public string action;

            public Entry(string key, string action)
            {
                this.key = key;
                this.action = action;
            }
        }

        public enum Corner { TopLeft, TopRight, BottomLeft, BottomRight }

        [Header("조작법 목록 (자유롭게 고치기)")]
        [SerializeField] private List<Entry> entries = new List<Entry>
        {
            new Entry("WASD", "이동"),
            new Entry("좌클릭", "도구 사용 · 심기 · 수확"),
            new Entry("우클릭", "들고 있는 것 내려놓기"),
            new Entry("1~9 / 휠", "핫바 선택"),
            new Entry("Tab", "인벤토리"),
            new Entry("E", "제작대 사용 (가까이서)"),
            new Entry("X", "작물 제거 모드 (괭이 들고)"),
            new Entry("마우스 올리기", "작물 정보 보기"),
        };

        [Header("열고 닫기")]
        [SerializeField] private Key toggleKey = Key.H;

        [Tooltip("켜면 게임 시작할 때 펼쳐진 상태로 시작한다")]
        [SerializeField] private bool startExpanded = true;

        [Tooltip("맨 아래에 '지금 들고 있는 것으로 할 수 있는 일' 을 띄운다")]
        [SerializeField] private bool showHeldHint = true;

        [Header("숨기기")]
        [Tooltip("이 중 하나라도 켜져 있으면 안내창을 숨긴다.\n" +
                 "상점 패널처럼 화면을 덮는 UI 를 끌어다 넣으면, 그게 열려 있는 동안 안 보인다")]
        [SerializeField] private List<GameObject> hideWhileActive = new List<GameObject>();

        [Header("위치")]
        [SerializeField] private Corner corner = Corner.TopLeft;

        [Tooltip("화면 가장자리에서 떨어진 거리")]
        [SerializeField] private Vector2 margin = new Vector2(16f, 16f);

        [Header("글자")]
        [Tooltip("한글 TMP 폰트. 비우면 씬의 다른 글자에서 한글 폰트를 찾아 쓴다")]
        [SerializeField] private TMP_FontAsset font;

        [SerializeField, Min(8f)] private float fontSize = 22f;

        [Tooltip("키 이름 칸의 너비. 설명이 이 위치에 줄 맞춰 시작한다")]
        [SerializeField, Min(20f)] private float keyColumnWidth = 150f;

        [Header("색")]
        [SerializeField] private Color background = new Color(0f, 0f, 0f, 0.55f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color keyColor = new Color(1f, 0.85f, 0.4f);
        [SerializeField] private Color hintColor = new Color(0.6f, 0.9f, 1f);
        [SerializeField] private Color dimColor = new Color(0.7f, 0.7f, 0.7f);

        [SerializeField] private Vector2 padding = new Vector2(14f, 10f);

        private RectTransform _panel;
        private CanvasGroup _group;
        private TextMeshProUGUI _text;
        private bool _expanded;
        private string _lastText;
        private float _pollTimer;

        private readonly StringBuilder _sb = new StringBuilder(512);
        private string _hexKey, _hexHint, _hexDim;

        /// <summary>
        /// 코드로 숨기기. 상점 같은 창을 열 때 true, 닫을 때 false 로 바꾼다.
        ///     ControlsGuideUI.Hidden = true;    // 숨기기
        ///     ControlsGuideUI.Hidden = false;   // 다시 보이기
        /// </summary>
        public static bool Hidden { get; set; }

        // ── 다른 스크립트가 끼워 넣은 줄 (예: 의뢰 게시판의 "J  의뢰 게시판 열기") ──
        private struct Extra
        {
            public string id;
            public string key;
            public string action;
        }

        private static readonly List<Extra> s_extras = new List<Extra>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_extras.Clear();
            Hidden = false;
        }

        /// <summary>조작법에 줄을 하나 끼워 넣는다. 같은 이름표로 다시 부르면 내용만 바뀐다</summary>
        public static void SetExtra(string id, string key, string action)
        {
            if (string.IsNullOrEmpty(id)) return;

            var extra = new Extra { id = id, key = key ?? string.Empty, action = action ?? string.Empty };

            for (int i = 0; i < s_extras.Count; i++)
            {
                if (s_extras[i].id != id) continue;

                s_extras[i] = extra;
                return;
            }

            s_extras.Add(extra);
        }

        /// <summary>끼워 넣었던 줄을 뺀다</summary>
        public static void RemoveExtra(string id)
        {
            for (int i = s_extras.Count - 1; i >= 0; i--)
                if (s_extras[i].id == id) s_extras.RemoveAt(i);
        }

        private void Awake()
        {
            // 씬을 새로 불러오면 보이는 상태로 시작한다 (전에 숨긴 채로 씬이 바뀌었어도)
            Hidden = false;

            if (GetComponentInParent<Canvas>() == null)
            {
                Debug.LogError("[조작 안내] Canvas 밑에 있어야 합니다. Canvas 우클릭 → Create Empty 로 만들어 주세요.", this);
                enabled = false;
                return;
            }

            _expanded = startExpanded;

            _hexKey = ColorUtility.ToHtmlStringRGB(keyColor);
            _hexHint = ColorUtility.ToHtmlStringRGB(hintColor);
            _hexDim = ColorUtility.ToHtmlStringRGB(dimColor);

            if (font == null) font = FindKoreanFont();
            if (font == null)
                Debug.LogWarning("[조작 안내] 한글 폰트를 못 찾았습니다. Font 칸에 한글 TMP 폰트를 넣어주세요.", this);

            Build();
            Refresh(true);
        }

        private void Update()
        {
            if (toggleKey != Key.None && Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                _expanded = !_expanded;
                Refresh(true);
            }

            // 제작창 · 뽑기 연출 · 상점 같은 창이 떠 있으면 가린다
            bool hide = Hidden || CraftingUI.IsOpen || GachaUI.IsSpinning || AnyActive(hideWhileActive);
            if (_group != null) _group.alpha = hide ? 0f : 1f;

            // 들고 있는 게 바뀌었을 수 있으니 가끔 다시 쓴다
            _pollTimer -= Time.unscaledDeltaTime;
            if (_pollTimer > 0f) return;

            _pollTimer = 0.15f;
            Refresh(false);
        }

        /// <summary>목록 중 하나라도 켜져 있는가</summary>
        private static bool AnyActive(List<GameObject> list)
        {
            if (list == null) return false;

            foreach (GameObject go in list)
                if (go != null && go.activeInHierarchy) return true;

            return false;
        }

        // ════════════════════════════════════════════════════════════
        //  내용
        // ════════════════════════════════════════════════════════════

        private void Refresh(bool force)
        {
            if (_text == null) return;

            _sb.Clear();

            string toggle = toggleKey == Key.None ? string.Empty : toggleKey.ToString();

            if (_expanded)
            {
                _sb.Append("<b>조작법</b>");
                if (toggle.Length > 0) _sb.Append("   <size=80%><color=#").Append(_hexDim).Append('>').Append(toggle).Append(" 접기</color></size>");

                bool extrasDone = false;

                foreach (Entry e in entries)
                {
                    if (string.IsNullOrEmpty(e.key) && string.IsNullOrEmpty(e.action)) continue;

                    AppendLine(e.key, e.action);

                    // 끼워 넣은 줄은 Tab(인벤토리) 바로 아래에
                    if (!extrasDone && SameKey(e.key, "Tab"))
                    {
                        AppendExtras();
                        extrasDone = true;
                    }
                }

                if (!extrasDone) AppendExtras();
            }
            else if (toggle.Length > 0)
            {
                _sb.Append("<color=#").Append(_hexKey).Append('>').Append(toggle).Append("</color>  조작법 보기");
            }

            if (showHeldHint)
            {
                string hint = HeldHint();

                if (!string.IsNullOrEmpty(hint))
                {
                    if (_sb.Length > 0) _sb.Append('\n');
                    _sb.Append("<color=#").Append(_hexHint).Append('>').Append(hint).Append("</color>");
                }
            }

            string text = _sb.ToString();
            if (!force && text == _lastText) return;

            _lastText = text;
            _text.text = text;

            // 쓸 글자가 하나도 없으면 배경도 숨긴다
            _panel.gameObject.SetActive(text.Length > 0);
        }

        private void AppendLine(string key, string action)
        {
            _sb.Append("\n<color=#").Append(_hexKey).Append('>').Append(key).Append("</color>")
               .Append("<pos=").Append(Mathf.RoundToInt(keyColumnWidth)).Append('>').Append(action);
        }

        /// <summary>다른 스크립트가 끼워 넣은 줄. 목록에 같은 키가 이미 있으면 (직접 적어 둔 경우) 건너뛴다</summary>
        private void AppendExtras()
        {
            foreach (Extra x in s_extras)
            {
                if (string.IsNullOrEmpty(x.key) && string.IsNullOrEmpty(x.action)) continue;
                if (HasEntryKey(x.key)) continue;

                AppendLine(x.key, x.action);
            }
        }

        private bool HasEntryKey(string key)
        {
            foreach (Entry e in entries)
                if (SameKey(e.key, key)) return true;

            return false;
        }

        private static bool SameKey(string a, string b)
            => !string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b) &&
               string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);

        /// <summary>지금 들고 있는 것으로 할 수 있는 일</summary>
        private static string HeldHint()
        {
            if (PlayerInteractor.IsRemoveMode) return "제거 모드 — 작물 클릭하면 뽑힘 · X 끄기";

            PlayerInventory inv = PlayerInventory.Instance;
            ItemSO held = inv != null ? inv.HeldItem : null;

            if (held == null) return "빈손 — 다 자란 작물 클릭하면 수확";

            string name = held.DisplayName;

            if (!inv.CanUseHeld) return $"{name} — 핫바에 올려야 쓸 수 있음";

            switch (held)
            {
                case HoeSO _:         return $"{name} — 좌클릭 땅 갈기 · X 작물 제거 모드";
                case WateringCanSO _: return $"{name} — 좌클릭 물 주기";
                case AxeSO _:         return $"{name} — 좌클릭 나무 베기";
                case ScytheSO _:      return $"{name} — 좌클릭 풀·채집물 베기";
                case SeedSO _:        return $"{name} — 갈아둔 밭에 좌클릭해서 심기";
                case ItemPackSO _:    return $"{name} — 좌클릭 뽑기 팩 열기";
            }

            // 친구 쪽 스크립트는 이름으로 확인한다 (이름이 바뀌어도 컴파일은 안 깨진다)
            switch (held.GetType().Name)
            {
                case "FishingRodSO":      return $"{name} — 물가에서 좌클릭 · 입질 오면 바로 클릭";
                case "FishingBaitDataSO": return $"{name} — 좌클릭 미끼 달기";
            }

            return $"{name} — 우클릭 내려놓기";
        }

        // ════════════════════════════════════════════════════════════
        //  화면에 만들기
        // ════════════════════════════════════════════════════════════

        private void Build()
        {
            // 이 오브젝트를 Canvas 전체 크기로 펴서, 그 구석에 안내창을 붙인다
            if (!(transform is RectTransform root))
            {
                Debug.LogError("[조작 안내] Canvas 우클릭 → Create Empty 로 만든 오브젝트에 붙여주세요.", this);
                return;
            }

            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            Vector2 anchor = corner switch
            {
                Corner.TopLeft => new Vector2(0f, 1f),
                Corner.TopRight => new Vector2(1f, 1f),
                Corner.BottomLeft => new Vector2(0f, 0f),
                _ => new Vector2(1f, 0f),
            };

            // ── 배경 ──
            var panelGo = new GameObject("GuidePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _panel = (RectTransform)panelGo.transform;
            _panel.SetParent(transform, false);
            _panel.anchorMin = anchor;
            _panel.anchorMax = anchor;
            _panel.pivot = anchor;
            _panel.anchoredPosition = new Vector2(
                anchor.x < 0.5f ? margin.x : -margin.x,
                anchor.y < 0.5f ? margin.y : -margin.y);

            var image = panelGo.GetComponent<Image>();
            image.color = background;
            image.raycastTarget = false;

            _group = panelGo.AddComponent<CanvasGroup>();
            _group.interactable = false;
            _group.blocksRaycasts = false;

            // 글자 양에 맞춰 배경 크기가 저절로 바뀌게
            var layout = panelGo.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(
                Mathf.RoundToInt(padding.x), Mathf.RoundToInt(padding.x),
                Mathf.RoundToInt(padding.y), Mathf.RoundToInt(padding.y));
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = panelGo.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ── 글자 ──
            var textGo = new GameObject("GuideText", typeof(RectTransform));
            textGo.transform.SetParent(_panel, false);

            _text = textGo.AddComponent<TextMeshProUGUI>();
            if (font != null) _text.font = font;
            _text.fontSize = fontSize;
            _text.color = textColor;
            _text.richText = true;
            _text.raycastTarget = false;
            _text.alignment = TextAlignmentOptions.TopLeft;
        }

        /// <summary>씬에 있는 글자들 중에서 한글이 들어 있는 폰트를 찾는다</summary>
        private static TMP_FontAsset FindKoreanFont()
        {
            TMP_Text[] all = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (TMP_Text t in all)
            {
                TMP_FontAsset f = t.font;
                if (f != null && f.HasCharacter('가', true, true)) return f;
            }

            return null;
        }
    }
