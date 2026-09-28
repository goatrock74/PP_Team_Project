using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using KSM._00.Scripts.Crafting;
using KSM._00.Scripts.Crop;
using KSM._00.Scripts.Items;

    /// <summary>
    /// 의뢰 게시판. J 로 열고 닫는다 (Esc 로도 닫힌다).
    ///
    ///   납품 — 아이템 N개 가져다주기 (보상 받을 때 가방에서 빠진다)
    ///   수확 — 작물 N개 수확하기 (아이템을 비우면 아무 작물이나)
    ///   심기 — 씨앗 N개 심기
    ///   벌목 — 나무 N그루 베기
    ///   채집 — 풀숲 N곳 채집하기
    ///
    /// 게시판은 두 칸이다.
    ///   의뢰        — Quests 목록 위에서부터 몇 개씩(Max Active) 올라오고, 보상을 받으면 다음 것이 올라온다. 한 번 깨면 끝
    ///   오늘의 의뢰 — 하루가 바뀔 때마다 Daily Pool 에서 랜덤으로 몇 개(Daily Count) 뽑힌다. 매일 새로 나온다
    ///
    /// 수확·심기·벌목·채집은 게시판에 올라와 있는 동안 한 것만 센다.
    /// 다 채우면 화면 위에 알림이 뜨고, 게시판에서 [보상 받기] 를 눌러야 끝난다.
    /// 다 채우고 보상을 안 받은 오늘의 의뢰는 다음 날로 넘어가도 남아 있다.
    ///
    /// 붙이는 법: Canvas_KSM 우클릭 → Create Empty → 이 컴포넌트 추가. 끝.
    ///   창은 플레이하면 코드가 만든다. 의뢰 내용은 Inspector 의 Quests / Daily Pool 에서 고친다.
    ///   조작법 안내창(ControlsGuideUI)에 "J  의뢰 게시판 열기" 줄이 알아서 들어간다.
    ///
    /// ★ 골드 보상 — 골드는 상점(팀원) 쪽에 있어서 여기서 직접 못 준다.
    ///   On Gold Reward 칸의 + 를 누르고, 골드 스크립트가 붙은 오브젝트를 끌어다 넣은 뒤
    ///   골드를 더하는 함수(int 하나 받는 것)를 고른다. 연결 전에는 골드만 안 들어간다.
    ///   코드로 받을 거면:  QuestBoard.GoldRewarded += amount => { /* 골드 += amount */ };
    /// ★ 진행 상황은 선택한 저장 슬롯에 저장된다. 처음부터 다시: 컴포넌트 ⋮ → "진행 초기화"
    /// ★ 나무·풀숲을 세려면 나무·풀숲 저장 때 보낸 TreeNode / ForageNode 가 들어가 있어야 한다
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class QuestBoard : MonoBehaviour
    {
        public enum QuestType
        {
            [InspectorName("납품 (가져다주기)")] Deliver,
            [InspectorName("수확")] Harvest,
            [InspectorName("심기")] Plant,
            [InspectorName("벌목")] ChopTree,
            [InspectorName("채집")] Forage,
        }

        [Serializable]
        public class Reward
        {
            public ItemSO item;
            [Min(1)] public int count = 1;
            public ItemQuality quality = ItemQuality.Normal;
        }

        [Serializable]
        public class Quest
        {
            [Tooltip("제목. 저장할 때 이름표로도 쓰니 다른 의뢰와 겹치면 안 된다")]
            public string title;

            [Tooltip("설명. 비우면 종류에 맞게 알아서 만든다")]
            [TextArea(1, 3)] public string description;

            public QuestType type;

            [Tooltip("납품: 가져올 아이템 (꼭 넣을 것)\n수확: 거둔 작물 아이템 — 씨앗 말고 수확물 (비우면 아무 작물)\n심기·벌목·채집: 안 씀")]
            public ItemSO item;

            [Tooltip("몇 개 / 몇 번")]
            [Min(1)] public int count = 1;

            [Tooltip("보상 골드. On Gold Reward 를 연결해야 들어간다")]
            [Min(0)] public int gold;

            [Tooltip("보상 아이템 (씨앗, 뽑기 팩, 도구 등)")]
            public List<Reward> rewards = new List<Reward>();

            public Quest() { }

            public Quest(string title, QuestType type, int count, int gold)
            {
                this.title = title;
                this.type = type;
                this.count = count;
                this.gold = gold;
            }
        }

        /// <summary>오늘의 의뢰 후보. 매일 여기서 뽑아서, 개수는 최소~최대 사이에서 랜덤으로 정한다</summary>
        [Serializable]
        public class DailyTemplate
        {
            [Tooltip("제목. 비우면 알아서 만든다 (예: 주문 · 순무)")]
            public string title;

            public QuestType type;

            [Tooltip("납품: 가져올 아이템 (꼭 넣을 것)\n수확: 거둔 작물 아이템 (비우면 아무 작물)\n심기·벌목·채집: 안 씀")]
            public ItemSO item;

            [Min(1)] public int minCount = 3;
            [Min(1)] public int maxCount = 6;

            [Tooltip("개수 하나당 골드. 보상 골드 = 이 값 × 뽑힌 개수")]
            [Min(0)] public int goldPerUnit = 30;

            [Tooltip("(선택) 골드 말고 같이 주는 아이템")]
            public List<Reward> rewards = new List<Reward>();

            [Tooltip("뽑힐 확률 가중치. 클수록 자주 나온다")]
            [Min(0.01f)] public float weight = 1f;

            public DailyTemplate() { }

            public DailyTemplate(string title, QuestType type, int minCount, int maxCount, int goldPerUnit)
            {
                this.title = title;
                this.type = type;
                this.minCount = minCount;
                this.maxCount = maxCount;
                this.goldPerUnit = goldPerUnit;
            }
        }

        [Serializable] public class IntEvent : UnityEvent<int> { }

        // ════════════════════════════════════════════════════════════
        //  Inspector
        // ════════════════════════════════════════════════════════════

        [Header("의뢰 — 위에서부터 순서대로 올라온다. 한 번 깨면 끝")]
        [SerializeField] private List<Quest> quests = new List<Quest>
        {
            new Quest("첫 농사", QuestType.Plant, 3, 100),
            new Quest("첫 수확", QuestType.Harvest, 3, 150),
            new Quest("나무꾼 입문", QuestType.ChopTree, 2, 150),
            new Quest("풀숲 뒤지기", QuestType.Forage, 3, 150),
            new Quest("부지런한 농부", QuestType.Harvest, 20, 500),
            new Quest("숲 정리", QuestType.ChopTree, 8, 400),
        };

        [Tooltip("게시판에 한 번에 올라와 있는 의뢰 수")]
        [SerializeField, Range(1, 4)] private int maxActive = 3;

        [Header("오늘의 의뢰 — 하루마다 랜덤으로 새로 뽑힌다")]
        [SerializeField] private bool dailyEnabled = true;

        [Tooltip("하루에 뽑는 개수")]
        [SerializeField, Range(1, 4)] private int dailyCount = 2;

        [Tooltip("오늘의 의뢰 후보. 납품 후보를 넣으려면 + 를 누르고 종류를 납품으로, Item 에 아이템을 넣는다")]
        [SerializeField] private List<DailyTemplate> dailyPool = new List<DailyTemplate>
        {
            new DailyTemplate("오늘의 수확", QuestType.Harvest, 4, 8, 30),
            new DailyTemplate("씨 뿌리기", QuestType.Plant, 3, 6, 20),
            new DailyTemplate("땔감 구하기", QuestType.ChopTree, 1, 3, 60),
            new DailyTemplate("숲 채집", QuestType.Forage, 2, 4, 40),
        };

        [Header("열고 닫기")]
        [Tooltip("어디서든 이 키로 연다. None 이면 안 쓴다")]
        [SerializeField] private Key toggleKey = Key.J;

        [Tooltip("(선택) 맵에 둔 게시판 오브젝트. 넣으면 가까이 가서 E 로도 열린다")]
        [SerializeField] private Transform worldBoard;

        [SerializeField] private Key interactKey = Key.E;

        [SerializeField, Min(0.1f)] private float interactRange = 1.5f;

        [Tooltip("(선택) 게시판 가까이 가면 켜질 안내 (E 말풍선 등). 평소엔 꺼둔다")]
        [SerializeField] private GameObject promptRoot;

        [Header("보상 연결")]
        [Tooltip("골드 보상을 줄 곳. 골드 스크립트의 '골드 더하기(int)' 함수를 연결한다")]
        [SerializeField] private IntEvent onGoldReward = new IntEvent();

        [Tooltip("의뢰(한 번짜리)를 전부 끝냈을 때 한 번 (엔딩 씬 불러오기 등)")]
        [SerializeField] private UnityEvent onAllCompleted = new UnityEvent();

        [Header("소리 (SoundManager 로 재생. 비워두면 안 난다)")]
        [Tooltip("의뢰를 다 채웠을 때 · 납품할 게 다 모였을 때")]
        [SerializeField] private AudioClip completeSfx;

        [Tooltip("보상을 받을 때")]
        [SerializeField] private AudioClip claimSfx;

        [Header("화면")]
        [Tooltip("한글 TMP 폰트. 비우면 씬의 다른 글자에서 한글 폰트를 찾아 쓴다")]
        [SerializeField] private TMP_FontAsset font;

        [Tooltip("다른 UI 보다 위에 그려지게 하는 순서")]
        [SerializeField] private int sortingOrder = 300;

        [Tooltip("알림이 떠 있는 시간(초)")]
        [SerializeField, Min(0.5f)] private float toastTime = 2.5f;

        [Tooltip("알림이 화면 위에서 떨어진 거리")]
        [SerializeField] private float toastY = 110f;

        [Header("저장")]
        [SerializeField] private bool saveProgress = true;

        // ════════════════════════════════════════════════════════════
        //  색 · 크기
        // ════════════════════════════════════════════════════════════

        private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color FrameColor = new Color(0.25f, 0.17f, 0.11f);
        private static readonly Color PaperColor = new Color(0.95f, 0.89f, 0.74f);
        private static readonly Color CardColor = new Color(1f, 0.97f, 0.88f);
        private static readonly Color IconBgColor = new Color(0.87f, 0.79f, 0.62f);
        private static readonly Color InkColor = new Color(0.25f, 0.17f, 0.11f);
        private static readonly Color SubInkColor = new Color(0.45f, 0.35f, 0.26f);
        private static readonly Color RewardInkColor = new Color(0.62f, 0.4f, 0.08f);
        private static readonly Color BarBgColor = new Color(0.82f, 0.74f, 0.58f);
        private static readonly Color BarColor = new Color(0.45f, 0.7f, 0.3f);
        private static readonly Color BarDoneColor = new Color(0.95f, 0.72f, 0.2f);
        private static readonly Color ButtonOnColor = new Color(0.4f, 0.66f, 0.28f);
        private static readonly Color ButtonOffColor = new Color(0.66f, 0.61f, 0.52f);
        private static readonly Color ButtonDoneColor = new Color(0.78f, 0.62f, 0.3f);
        private static readonly Color CloseColor = new Color(0.72f, 0.36f, 0.3f);

        private static readonly Color ToastProgressColor = new Color(0.92f, 0.92f, 0.92f);
        private static readonly Color ToastReadyColor = new Color(1f, 0.84f, 0.35f);
        private static readonly Color ToastWarnColor = new Color(1f, 0.55f, 0.5f);

        private const string SubInkHex = "735943";

        private const float PanelWidth = 860f;
        private const float TitleArea = 64f;      // 창 위쪽 제목 자리
        private const float FooterArea = 40f;     // 창 아래쪽 안내 자리
        private const float CardHeight = 100f;
        private const float SectionHeight = 30f;
        private const float NoteHeight = 36f;
        private const float RowSpacing = 8f;
        private const float Border = 4f;

        private const string DailyPrefix = "daily#";

        // ════════════════════════════════════════════════════════════
        //  상태
        // ════════════════════════════════════════════════════════════

        /// <summary>게시판 창이 열려 있는가</summary>
        public static bool IsOpen { get; private set; }

        /// <summary>골드 보상이 나올 때. 코드로 골드를 연결할 때 쓴다</summary>
        public static event Action<int> GoldRewarded;

        private static int s_lastCloseFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsOpen = false;
            GoldRewarded = null;
            s_lastCloseFrame = -1;
        }

        private readonly Dictionary<Quest, string> _ids = new Dictionary<Quest, string>();
        private readonly Dictionary<string, int> _progress = new Dictionary<string, int>();
        private readonly HashSet<string> _claimed = new HashSet<string>();
        private readonly HashSet<string> _notified = new HashSet<string>();   // 완료 알림을 이미 띄운 것
        private readonly List<Quest> _active = new List<Quest>();             // 게시판에 올라온 한 번짜리 의뢰

        // 오늘의 의뢰
        private readonly List<Quest> _daily = new List<Quest>();
        private readonly List<int> _dailyTemplate = new List<int>();          // _daily 와 같은 순서로, 몇 번 후보에서 뽑았는지
        private int _dailyDay = -1;                                           // 몇 번째 날 것인지. -1 = 아직 안 뽑음

        private CropManager _mgr;
        private int _lastCropCount = -1;     // -1 = 아직 기준을 안 잡음 (밭 불러오기가 끝난 뒤에 잡는다)
        private bool _dirty;
        private bool _saveLoaded;
        private string _saveSlotId;
        private float _saveAt;
        private float _pollAt;
        private bool _allDoneFired;
        private bool _warnedGold;
        private bool _subscribed;

        // 화면
        private RectTransform _window;
        private RectTransform _frame;
        private TextMeshProUGUI _storyHeader;
        private TextMeshProUGUI _storyNote;
        private TextMeshProUGUI _dailyHeader;
        private readonly List<CardView> _storyCards = new List<CardView>();
        private readonly List<CardView> _dailyCards = new List<CardView>();
        private readonly List<LayoutElement> _rows = new List<LayoutElement>();
        private CanvasGroup _toastGroup;
        private TextMeshProUGUI _toastText;
        private float _toastUntil;

        private class CardView
        {
            public GameObject root;
            public Image icon;
            public TextMeshProUGUI iconLabel;
            public TextMeshProUGUI title;
            public TextMeshProUGUI desc;
            public RectTransform barFill;
            public Image barFillImage;
            public TextMeshProUGUI progress;
            public TextMeshProUGUI reward;
            public Button button;
            public Image buttonImage;
            public TextMeshProUGUI buttonText;
            public Quest quest;
        }

        // ════════════════════════════════════════════════════════════
        //  수명 주기
        // ════════════════════════════════════════════════════════════

        private void Awake()
        {
            if (GetComponentInParent<Canvas>() == null || !(transform is RectTransform))
            {
                Debug.LogError("[의뢰] Canvas 밑에 있어야 합니다. Canvas_KSM 우클릭 → Create Empty 로 만들고 붙여주세요.", this);
                enabled = false;
                return;
            }

            if (font == null) font = FindKoreanFont();
            if (font == null)
                Debug.LogWarning("[의뢰] 한글 폰트를 못 찾았습니다. Font 칸에 한글 TMP 폰트를 넣어주세요.", this);

            BuildIds();
            Load();
            RefreshActive();

            _allDoneFired = AllDone();   // 이미 다 끝낸 저장본이면 엔딩 이벤트를 또 부르지 않는다

            BuildUI();
            _window.gameObject.SetActive(false);
        }

        private IEnumerator Start()
        {
            TreeNode.AnyStateChanged += HandleTree;
            ForageNode.AnyStateChanged += HandleForage;

            _mgr = CropManager.Instance;
            if (_mgr != null) _mgr.OnHarvested += HandleHarvested;

            _subscribed = true;
            _pollAt = Time.unscaledTime + 1f;   // 인벤토리 저장본을 불러오는 동안은 알림을 참는다

            // 조작법 안내창에 "J  의뢰 게시판 열기" 를 넣는다
            if (toggleKey != Key.None) ControlsGuideUI.SetExtra("questboard", KeyName(toggleKey), "의뢰 게시판 열기");
            else if (worldBoard != null) ControlsGuideUI.SetExtra("questboard", KeyName(interactKey), "의뢰 게시판 (게시판 앞에서)");

            // 밭 저장본이 작물을 되살린 뒤에 '심은 수' 의 기준을 잡는다. 안 그러면 불러온 작물이 심은 걸로 세진다
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(0.3f);

            if (_mgr != null) _lastCropCount = _mgr.Crops.Count;
        }

        private void OnDestroy()
        {
            if (_subscribed)
            {
                TreeNode.AnyStateChanged -= HandleTree;
                ForageNode.AnyStateChanged -= HandleForage;
                if (_mgr != null) _mgr.OnHarvested -= HandleHarvested;

                ControlsGuideUI.RemoveExtra("questboard");
            }

            if (_dirty) Save();
            if (IsOpen) IsOpen = false;
        }

        private void OnApplicationQuit()
        {
            if (_dirty) Save();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause && _dirty) Save();
        }

        private void Update()
        {
            HandleInput();
            CountPlanting();

            if (Time.unscaledTime >= _pollAt)
            {
                _pollAt = Time.unscaledTime + 0.25f;

                EnsureDaily();
                PollDeliver();
                if (IsOpen) RefreshView();
            }

            if (_dirty && Time.unscaledTime >= _saveAt) Save();

            if (_toastGroup != null)
            {
                float target = Time.unscaledTime < _toastUntil ? 1f : 0f;
                _toastGroup.alpha = Mathf.MoveTowards(_toastGroup.alpha, target, Time.unscaledDeltaTime * 5f);
            }
        }

        // ════════════════════════════════════════════════════════════
        //  열고 닫기
        // ════════════════════════════════════════════════════════════

        public void Open()
        {
            if (IsOpen || _window == null) return;
            if (CraftingUI.IsOpen || GachaUI.IsSpinning) return;

            EnsureDaily();
            RefreshActive();
            _window.gameObject.SetActive(true);
            IsOpen = true;

            if (promptRoot != null) promptRoot.SetActive(false);

            RefreshView();
        }

        public void Close()
        {
            if (!IsOpen) return;

            _window.gameObject.SetActive(false);
            IsOpen = false;
            s_lastCloseFrame = Time.frameCount;

            Deselect();
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        private void HandleInput()
        {
            // 제작창이 열리거나 뽑기 연출이 시작되면 게시판은 닫는다
            if (IsOpen && (CraftingUI.IsOpen || GachaUI.IsSpinning)) Close();

            Keyboard kb = Keyboard.current;

            if (kb != null)
            {
                if (IsOpen)
                {
                    if (Pressed(kb, Key.Escape) || Pressed(kb, toggleKey)) Close();
                }
                else if (Pressed(kb, toggleKey) && Time.frameCount != s_lastCloseFrame)
                {
                    Open();
                }
            }

            // 맵에 둔 게시판 — 가까이 가면 안내를 켜고, E 로 연다
            if (worldBoard == null) return;

            PlayerInventory inv = PlayerInventory.Instance;
            bool near = !IsOpen && inv != null && !CraftingUI.IsOpen &&
                        Vector2.Distance(worldBoard.position, inv.transform.position) <= interactRange;

            if (promptRoot != null && promptRoot.activeSelf != near) promptRoot.SetActive(near);

            if (near && kb != null && Pressed(kb, interactKey) && Time.frameCount != s_lastCloseFrame) Open();
        }

        private static bool Pressed(Keyboard kb, Key key) => key != Key.None && kb[key].wasPressedThisFrame;

        // ════════════════════════════════════════════════════════════
        //  진행 세기
        // ════════════════════════════════════════════════════════════

        private void HandleHarvested(ItemSO item, int amount, ItemQuality quality)
            => AddProgress(QuestType.Harvest, item, amount);

        private void HandleTree(TreeNode tree)
        {
            // 맞기만 한 것 · 다시 자란 것은 빼고, 쓰러진 순간만 센다
            if (tree != null && (tree.IsStump || tree.IsGone)) AddProgress(QuestType.ChopTree, null, 1);
        }

        private void HandleForage(ForageNode node)
        {
            // TryGetSaveState 는 '지금 캔 상태인가' 를 알려준다. 다시 자란 순간은 false 라서 빠진다
            if (node != null && (node.IsGone || node.TryGetSaveState(out _))) AddProgress(QuestType.Forage, null, 1);
        }

        /// <summary>작물 수가 늘면 그만큼 심은 것이다 (작물은 심을 때만 늘어난다)</summary>
        private void CountPlanting()
        {
            if (_mgr == null || _lastCropCount < 0) return;

            int now = _mgr.Crops.Count;
            if (now > _lastCropCount) AddProgress(QuestType.Plant, null, now - _lastCropCount);

            _lastCropCount = now;
        }

        /// <summary>지금 세고 있는 의뢰 — 올라와 있는 한 번짜리 의뢰 + 보상을 아직 안 받은 오늘의 의뢰</summary>
        private IEnumerable<Quest> Trackable()
        {
            foreach (Quest q in _active) yield return q;

            foreach (Quest q in _daily)
                if (!_claimed.Contains(_ids[q])) yield return q;
        }

        private void AddProgress(QuestType type, ItemSO item, int amount)
        {
            if (amount <= 0) return;

            bool changed = false;

            foreach (Quest q in Trackable())
            {
                if (q.type != type) continue;
                if (type == QuestType.Harvest && q.item != null && q.item != item) continue;

                string id = _ids[q];
                int before = GetCount(id);
                if (before >= q.count) continue;

                int now = Mathf.Min(q.count, before + amount);
                _progress[id] = now;
                changed = true;

                if (now >= q.count)
                {
                    _notified.Add(id);
                    Toast($"의뢰 완료!  {OpenHint()} · {q.title}", ToastReadyColor);
                    Sfx(completeSfx);
                }
                else
                {
                    Toast($"의뢰 진행 · {q.title}  {now}/{q.count}", ToastProgressColor);
                }
            }

            if (!changed) return;

            MarkDirty();
            if (IsOpen) RefreshView();
        }

        /// <summary>납품 의뢰는 가방을 보고 판단한다. 다 모이면 한 번 알려준다</summary>
        private void PollDeliver()
        {
            foreach (Quest q in Trackable())
            {
                if (q.type != QuestType.Deliver) continue;

                string id = _ids[q];
                bool ready = IsComplete(q);

                if (ready && _notified.Add(id))
                {
                    Toast($"납품 가능!  {OpenHint()} · {q.title}", ToastReadyColor);
                    Sfx(completeSfx);
                }
                else if (!ready)
                {
                    _notified.Remove(id);
                }
            }
        }

        // ════════════════════════════════════════════════════════════
        //  오늘의 의뢰
        // ════════════════════════════════════════════════════════════

        /// <summary>지금 몇 번째 날인가 (0부터). 게임 시계가 없으면 -1</summary>
        private static int CurrentDay()
        {
            CropManager farm = CropManager.Instance;
            if (farm == null) return -1;

            // CurrentGameDays 는 켠 직후 잠깐 0 이라서, 시계가 꽂혀 있으면 시계를 직접 본다
            float days = farm.GameClock != null ? farm.GameClock.TotalGameDays : farm.CurrentGameDays;
            return Mathf.Max(0, Mathf.FloorToInt(days));
        }

        /// <summary>날이 바뀌었으면 오늘의 의뢰를 새로 뽑는다</summary>
        private void EnsureDaily()
        {
            if (!dailyEnabled || !HasDailyPool()) return;

            int day = CurrentDay();
            if (day < 0 || day == _dailyDay) return;

            RerollDaily(day);
            Toast($"새로운 하루! 오늘의 의뢰가 올라왔어요  ({OpenHintShort()})", ToastReadyColor);

            if (IsOpen) RefreshView();
        }

        private void RerollDaily(int day)
        {
            // 다 채웠는데 보상을 안 받은 건 남겨 둔다 (날이 바뀌어서 보상이 날아가면 억울하니까)
            var keep = new List<(int template, int count, int progress)>();

            for (int i = 0; i < _daily.Count; i++)
            {
                Quest q = _daily[i];
                string id = _ids[q];
                int done = GetCount(id);

                if (!_claimed.Contains(id) && q.type != QuestType.Deliver && done >= q.count)
                    keep.Add((_dailyTemplate[i], q.count, done));
            }

            ClearDaily();
            _dailyDay = day;

            foreach (var k in keep)
            {
                if (_daily.Count >= dailyCount) break;

                Quest q = AddDaily(k.template, k.count);
                _progress[_ids[q]] = k.progress;
            }

            // 남은 자리를 새로 뽑는다. 후보가 충분하면 같은 날 같은 후보가 겹치지 않게 한다 (남겨 둔 것과도)
            var bag = new List<int>();
            for (int i = 0; i < dailyPool.Count; i++)
                if (IsValidTemplate(dailyPool[i]) && !_dailyTemplate.Contains(i)) bag.Add(i);

            if (bag.Count == 0)   // 후보가 모자라면 겹쳐도 된다
            {
                for (int i = 0; i < dailyPool.Count; i++)
                    if (IsValidTemplate(dailyPool[i])) bag.Add(i);
            }

            while (_daily.Count < dailyCount && bag.Count > 0)
            {
                int pick = WeightedPick(bag);
                DailyTemplate t = dailyPool[pick];

                int min = Mathf.Max(1, t.minCount);
                int max = Mathf.Max(min, t.maxCount);
                AddDaily(pick, UnityEngine.Random.Range(min, max + 1));

                if (bag.Count > 1) bag.Remove(pick);
            }

            MarkDirty();
        }

        private Quest AddDaily(int templateIndex, int count)
        {
            DailyTemplate t = dailyPool[templateIndex];

            var q = new Quest(DailyTitle(t), t.type, count, t.goldPerUnit * count)
            {
                item = t.item,
                rewards = t.rewards ?? new List<Reward>(),
            };

            _ids[q] = DailyPrefix + _daily.Count;
            _daily.Add(q);
            _dailyTemplate.Add(templateIndex);

            return q;
        }

        /// <summary>오늘의 의뢰와 그 진행 기록을 전부 지운다</summary>
        private void ClearDaily()
        {
            foreach (Quest q in _daily) _ids.Remove(q);

            _daily.Clear();
            _dailyTemplate.Clear();

            RemoveDailyKeys(_progress.Keys);
            RemoveDailyKeys(_claimed);
            RemoveDailyKeys(_notified);
        }

        private void RemoveDailyKeys(IEnumerable<string> keys)
        {
            var remove = new List<string>();
            foreach (string k in keys)
                if (k.StartsWith(DailyPrefix, StringComparison.Ordinal)) remove.Add(k);

            foreach (string k in remove)
            {
                _progress.Remove(k);
                _claimed.Remove(k);
                _notified.Remove(k);
            }
        }

        private int WeightedPick(List<int> candidates)
        {
            float total = 0f;
            foreach (int i in candidates) total += Mathf.Max(0.01f, dailyPool[i].weight);

            float r = UnityEngine.Random.value * total;

            foreach (int i in candidates)
            {
                r -= Mathf.Max(0.01f, dailyPool[i].weight);
                if (r <= 0f) return i;
            }

            return candidates[candidates.Count - 1];
        }

        private bool HasDailyPool()
        {
            foreach (DailyTemplate t in dailyPool)
                if (IsValidTemplate(t)) return true;

            return false;
        }

        private static bool IsValidTemplate(DailyTemplate t)
            => t != null && (t.type != QuestType.Deliver || t.item != null);

        private static string DailyTitle(DailyTemplate t)
        {
            if (!string.IsNullOrWhiteSpace(t.title)) return t.title.Trim();

            return t.type switch
            {
                QuestType.Deliver => $"주문 · {ItemName(t.item)}",
                QuestType.Harvest => t.item != null ? $"수확 · {ItemName(t.item)}" : "오늘의 수확",
                QuestType.Plant => "씨 뿌리기",
                QuestType.ChopTree => "땔감 구하기",
                _ => "숲 채집",
            };
        }

        private int DailyClaimedCount()
        {
            int n = 0;
            foreach (Quest q in _daily)
                if (_claimed.Contains(_ids[q])) n++;

            return n;
        }

        // ════════════════════════════════════════════════════════════
        //  보상 받기
        // ════════════════════════════════════════════════════════════

        private void Claim(Quest q)
        {
            if (q == null || !_ids.TryGetValue(q, out string id) || _claimed.Contains(id)) return;

            if (!IsComplete(q))
            {
                Toast("아직 다 못 채웠어요", ToastWarnColor);
                return;
            }

            PlayerInventory inv = PlayerInventory.Instance;

            // 보상 아이템이 들어갈 자리가 있는지 먼저 본다
            if (inv != null)
            {
                foreach (Reward r in q.rewards)
                {
                    if (r == null || r.item == null) continue;

                    if (!inv.CanAccept(r.item, r.count, r.quality))
                    {
                        Toast("가방에 자리가 없어요", ToastWarnColor);
                        return;
                    }
                }
            }

            // 납품 — 가방에서 뺀다
            if (q.type == QuestType.Deliver)
            {
                if (inv == null) return;

                int removed = inv.Remove(q.item, q.count);

                if (removed < q.count)
                {
                    if (removed > 0) inv.Add(q.item, removed);   // 모자라면 되돌린다 (거의 안 일어난다)
                    Toast("재료가 모자라요", ToastWarnColor);
                    return;
                }
            }

            if (q.gold > 0) GiveGold(q.gold);

            if (inv != null)
            {
                foreach (Reward r in q.rewards)
                    if (r != null && r.item != null) inv.Add(r.item, r.count, r.quality);
            }

            bool isDaily = _daily.Contains(q);

            _claimed.Add(id);
            _notified.Remove(id);
            if (!isDaily) _progress.Remove(id);   // 오늘의 의뢰는 '완료됨' 으로 계속 보여줘야 해서 남겨 둔다
            Save();

            Sfx(claimSfx);
            Toast($"보상 획득!  {RewardText(q)}", ToastReadyColor);

            RefreshActive();
            RefreshView();

            if (isDaily)
            {
                if (DailyClaimedCount() == _daily.Count) Toast("오늘의 의뢰 끝! 내일 새 의뢰가 올라와요", ToastReadyColor);
                return;
            }

            if (!_allDoneFired && AllDone())
            {
                _allDoneFired = true;
                Toast("의뢰를 모두 끝냈어요!", ToastReadyColor);
                onAllCompleted.Invoke();
            }
        }

        private void GiveGold(int amount)
        {
            bool hooked = onGoldReward.GetPersistentEventCount() > 0 || GoldRewarded != null;

            onGoldReward.Invoke(amount);
            GoldRewarded?.Invoke(amount);

            if (hooked || _warnedGold) return;

            _warnedGold = true;
            Debug.LogWarning("[의뢰] 골드 보상을 받을 곳이 연결 안 돼 있어서 골드가 안 들어갑니다. " +
                             "QuestBoard 의 On Gold Reward 칸에 골드 스크립트의 '골드 더하기(int)' 함수를 연결하세요.", this);
        }

        // ════════════════════════════════════════════════════════════
        //  의뢰 정보
        // ════════════════════════════════════════════════════════════

        /// <summary>저장용 이름표. 제목을 쓰고, 비었거나 겹치면 순서 번호를 붙인다</summary>
        private void BuildIds()
        {
            _ids.Clear();
            var used = new HashSet<string>();

            for (int i = 0; i < quests.Count; i++)
            {
                Quest q = quests[i];
                if (q == null) continue;

                string id = string.IsNullOrWhiteSpace(q.title) ? $"#{i}" : q.title.Trim();

                if (!used.Add(id))
                {
                    Debug.LogWarning($"[의뢰] 제목 '{id}' 이 겹칩니다. 저장이 섞이지 않게 제목을 다르게 바꿔주세요.", this);
                    id = $"{id}#{i}";
                    used.Add(id);
                }

                _ids[q] = id;

                if (q.type == QuestType.Deliver && q.item == null)
                    Debug.LogWarning($"[의뢰] '{id}' 는 납품 의뢰인데 아이템이 비어 있어서 건너뜁니다.", this);
            }

            for (int i = 0; i < dailyPool.Count; i++)
            {
                DailyTemplate t = dailyPool[i];
                if (t != null && t.type == QuestType.Deliver && t.item == null)
                    Debug.LogWarning($"[의뢰] 오늘의 의뢰 후보 {i}번은 납품인데 아이템이 비어 있어서 안 뽑힙니다.", this);
            }
        }

        private static bool IsValid(Quest q)
            => q != null && q.count > 0 && (q.type != QuestType.Deliver || q.item != null);

        /// <summary>목록 위에서부터, 보상을 안 받은 것을 Max Active 개까지 게시판에 올린다</summary>
        private void RefreshActive()
        {
            _active.Clear();

            foreach (Quest q in quests)
            {
                if (_active.Count >= maxActive) break;
                if (!IsValid(q) || !_ids.TryGetValue(q, out string id) || _claimed.Contains(id)) continue;

                _active.Add(q);
            }
        }

        private bool AllDone()
        {
            int valid = 0;

            foreach (Quest q in quests)
            {
                if (!IsValid(q) || !_ids.TryGetValue(q, out string id)) continue;

                valid++;
                if (!_claimed.Contains(id)) return false;
            }

            return valid > 0;
        }

        private int GetCount(string id) => _progress.TryGetValue(id, out int n) ? n : 0;

        /// <summary>지금까지 한 양. 납품은 가방에 있는 개수</summary>
        private int CurrentOf(Quest q)
        {
            if (q.type != QuestType.Deliver) return GetCount(_ids[q]);

            PlayerInventory inv = PlayerInventory.Instance;
            return inv != null && q.item != null ? inv.CountOf(q.item) : 0;
        }

        private bool IsComplete(Quest q) => CurrentOf(q) >= q.count;

        private static string TypeName(QuestType t) => t switch
        {
            QuestType.Deliver => "납품",
            QuestType.Harvest => "수확",
            QuestType.Plant => "심기",
            QuestType.ChopTree => "벌목",
            _ => "채집",
        };

        private static string TypeHex(QuestType t) => t switch
        {
            QuestType.Deliver => "B5651D",
            QuestType.Harvest => "4E8A2F",
            QuestType.Plant => "7A8F22",
            QuestType.ChopTree => "8A5A2B",
            _ => "2F7F6A",
        };

        private static string ItemName(ItemSO item) => item != null ? item.DisplayName : "?";

        private static string Describe(Quest q)
        {
            if (!string.IsNullOrWhiteSpace(q.description)) return q.description;

            return q.type switch
            {
                QuestType.Deliver => $"{ItemName(q.item)} {q.count}개를 가져다주세요",
                QuestType.Harvest => q.item != null
                    ? $"{ItemName(q.item)} {q.count}개를 수확하세요"
                    : $"아무 작물이나 {q.count}개 수확하세요",
                QuestType.Plant => $"갈아둔 밭에 씨앗 {q.count}개를 심으세요",
                QuestType.ChopTree => $"도끼로 나무 {q.count}그루를 베세요",
                _ => $"낫으로 풀숲 {q.count}곳을 채집하세요",
            };
        }

        private static readonly StringBuilder s_sb = new StringBuilder(128);

        private static string RewardText(Quest q)
        {
            s_sb.Clear();

            if (q.gold > 0) s_sb.Append(q.gold).Append('G');

            foreach (Reward r in q.rewards)
            {
                if (r == null || r.item == null) continue;

                if (s_sb.Length > 0) s_sb.Append(" · ");
                if (r.quality != ItemQuality.Normal) s_sb.Append(ItemQualityUtil.DisplayName(r.quality)).Append(' ');
                s_sb.Append(r.item.DisplayName).Append(" ×").Append(r.count);
            }

            return s_sb.Length > 0 ? s_sb.ToString() : "없음";
        }

        private string OpenHint()
            => toggleKey != Key.None ? $"{KeyName(toggleKey)} 를 눌러 보상 받기" : "게시판에서 보상 받기";

        private string OpenHintShort()
            => toggleKey != Key.None ? $"{KeyName(toggleKey)} 게시판" : "게시판 확인";

        private static string KeyName(Key key)
        {
            string s = key.ToString();

            if (s.StartsWith("Digit")) s = s.Substring(5);
            else if (s.StartsWith("Numpad")) s = s.Substring(6);

            return s.ToUpperInvariant();
        }

        // ════════════════════════════════════════════════════════════
        //  저장
        // ════════════════════════════════════════════════════════════

        [Serializable]
        private class SaveEntry
        {
            public string id;
            public int progress;
            public bool claimed;
        }

        [Serializable]
        private class DailySave
        {
            public int template;   // 오늘의 의뢰 후보 번호
            public int count;
        }

        [Serializable]
        private class SaveData
        {
            public List<SaveEntry> quests = new List<SaveEntry>();
            public int dailyDay = -1;
            public List<DailySave> daily = new List<DailySave>();
        }

        /// <summary>★ 친구의 저장 슬롯에 맞출 때는 이 이름만 바꾸면 된다</summary>
        private static string SaveKey => "QuestBoardV1";

        private void MarkDirty()
        {
            if (!_dirty) _saveAt = Time.unscaledTime + 1f;
            _dirty = true;
        }

        private void Load()
        {
            _saveSlotId = SaveSlotStore.Active?.id;
            _saveLoaded = true;
            _dirty = false;
            _progress.Clear();
            _claimed.Clear();
            _notified.Clear();
            ClearDaily();
            _dailyDay = -1;

            if (!saveProgress) return;

            string json = SaveSlotStore.GetString(SaveKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return;

            SaveData data;
            try
            {
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[의뢰] 저장본을 읽지 못해 처음부터 시작합니다: " + e.Message, this);
                return;
            }

            if (data == null) return;

            if (data.quests != null)
            {
                foreach (SaveEntry e in data.quests)
                {
                    if (e == null || string.IsNullOrEmpty(e.id)) continue;

                    if (e.claimed) _claimed.Add(e.id);
                    if (e.progress > 0) _progress[e.id] = e.progress;
                }
            }

            // 오늘의 의뢰 — 저장한 날의 것을 그대로 되살린다. 후보 목록이 바뀌어서 안 맞으면 새로 뽑게 둔다
            bool ok = dailyEnabled && data.dailyDay >= 0 && data.daily != null && data.daily.Count > 0;

            if (ok)
            {
                foreach (DailySave d in data.daily)
                {
                    if (_daily.Count >= dailyCount) break;

                    if (d == null || d.template < 0 || d.template >= dailyPool.Count || !IsValidTemplate(dailyPool[d.template]))
                    {
                        ok = false;
                        break;
                    }

                    AddDaily(d.template, Mathf.Max(1, d.count));
                }
            }

            if (ok && _daily.Count > 0)
            {
                _dailyDay = data.dailyDay;
            }
            else
            {
                ClearDaily();
                _dailyDay = -1;
            }
        }

        // Called before the session writes its complete snapshot, including pending progress.
        public void CaptureSave() => Save();

        private void Save()
        {
            // A board being destroyed must never write into the next slot or legacy data.
            if (!_saveLoaded || !saveProgress || _saveSlotId != SaveSlotStore.Active?.id) return;

            var data = new SaveData();
            var written = new HashSet<string>();

            foreach (string id in _claimed)
            {
                data.quests.Add(new SaveEntry { id = id, claimed = true, progress = GetCount(id) });
                written.Add(id);
            }

            foreach (KeyValuePair<string, int> kv in _progress)
                if (!written.Contains(kv.Key)) data.quests.Add(new SaveEntry { id = kv.Key, progress = kv.Value });

            data.dailyDay = _dailyDay;

            for (int i = 0; i < _daily.Count; i++)
                data.daily.Add(new DailySave { template = _dailyTemplate[i], count = _daily[i].count });

            SaveSlotStore.SetString(SaveKey, JsonUtility.ToJson(data));
            SaveSlotStore.Save();
            _dirty = false;
        }

        [ContextMenu("진행 초기화 (의뢰 처음부터)")]
        private void ResetProgress()
        {
            if (Application.isPlaying && (!_saveLoaded || _saveSlotId != SaveSlotStore.Active?.id)) return;
            SaveSlotStore.DeleteKey(SaveKey);
            SaveSlotStore.Save();

            _progress.Clear();
            _claimed.Clear();
            _notified.Clear();
            ClearDaily();
            _dailyDay = -1;
            _dirty = false;
            _allDoneFired = false;

            if (!Application.isPlaying) return;

            _lastCropCount = _mgr != null ? _mgr.Crops.Count : -1;
            _toastUntil = 0f;
            RefreshActive();
            EnsureDaily();
            if (IsOpen) RefreshView();
            Save();
            SaveGameSession.SaveNow();

            Debug.Log("[의뢰] 진행 상황을 지웠습니다. 첫 의뢰부터 다시 시작합니다.", this);
        }

        [ContextMenu("테스트: 올라와 있는 의뢰 다 채우기 (납품 제외)")]
        private void DebugCompleteActive()
        {
            if (!Application.isPlaying) return;

            foreach (Quest q in Trackable())
                if (q.type != QuestType.Deliver) _progress[_ids[q]] = q.count;

            MarkDirty();
            if (IsOpen) RefreshView();
        }

        [ContextMenu("테스트: 오늘의 의뢰 새로 뽑기")]
        private void DebugRerollDaily()
        {
            if (!Application.isPlaying || !dailyEnabled) return;

            RerollDaily(Mathf.Max(0, CurrentDay()));
            if (IsOpen) RefreshView();
        }

        // ════════════════════════════════════════════════════════════
        //  화면 — 그리기
        // ════════════════════════════════════════════════════════════

        private void RefreshView()
        {
            if (_window == null) return;

            // ── 한 번짜리 의뢰 ──
            int total = 0, done = 0;

            foreach (Quest q in quests)
            {
                if (!IsValid(q) || !_ids.TryGetValue(q, out string id)) continue;

                total++;
                if (_claimed.Contains(id)) done++;
            }

            bool hasStory = total > 0;
            _storyHeader.gameObject.SetActive(hasStory);
            _storyHeader.text = $"의뢰   <size=80%><color=#{SubInkHex}>완료 {done} / {total}</color></size>";

            for (int i = 0; i < _storyCards.Count; i++)
                Bind(_storyCards[i], hasStory && i < _active.Count ? _active[i] : null);

            // ── 오늘의 의뢰 ──
            bool hasDaily = dailyEnabled && _daily.Count > 0;
            int dailyDone = DailyClaimedCount();

            _dailyHeader.gameObject.SetActive(hasDaily);
            _dailyHeader.text = dailyDone >= _daily.Count
                ? $"오늘의 의뢰 · {_dailyDay + 1}일차   <size=80%><color=#{SubInkHex}>다 했어요! 내일 또 올라와요</color></size>"
                : $"오늘의 의뢰 · {_dailyDay + 1}일차   <size=80%><color=#{SubInkHex}>완료 {dailyDone} / {_daily.Count}</color></size>";

            for (int i = 0; i < _dailyCards.Count; i++)
                Bind(_dailyCards[i], hasDaily && i < _daily.Count ? _daily[i] : null);

            // ── 안내 한 줄 (올라온 게 없을 때) ──
            bool storyDone = hasStory && _active.Count == 0;
            bool nothing = !hasStory && !hasDaily;

            _storyNote.gameObject.SetActive(storyDone || nothing);
            _storyNote.text = nothing ? "올라온 의뢰가 없어요" : "의뢰를 모두 끝냈어요!";

            ResizePanel();
        }

        private void Bind(CardView v, Quest q)
        {
            v.quest = q;
            v.root.SetActive(q != null);
            if (q == null) return;

            bool claimed = _claimed.Contains(_ids[q]);
            int have = claimed ? q.count : CurrentOf(q);
            bool complete = have >= q.count;
            float p = Mathf.Clamp01(have / (float)q.count);

            v.title.text = $"<size=75%><color=#{TypeHex(q.type)}>{TypeName(q.type)}</color></size>  {q.title}";
            v.desc.text = Describe(q);
            v.progress.text = $"{Mathf.Min(have, q.count)} / {q.count}";
            v.reward.text = "보상  " + RewardText(q);

            v.barFill.anchorMax = new Vector2(p, 1f);
            v.barFillImage.color = complete ? BarDoneColor : BarColor;

            Sprite icon = q.item != null ? q.item.icon : null;
            v.icon.sprite = icon;
            v.icon.enabled = icon != null;
            v.iconLabel.text = icon != null ? string.Empty : TypeName(q.type);

            v.button.interactable = complete && !claimed;
            v.buttonImage.color = claimed ? ButtonDoneColor : complete ? ButtonOnColor : ButtonOffColor;
            v.buttonText.text = claimed ? "완료됨"
                : q.type == QuestType.Deliver ? (complete ? "납품하기" : "재료 부족")
                : (complete ? "보상 받기" : "진행 중");
        }

        /// <summary>보이는 줄 수에 맞춰 창 높이를 바꾸고, 화면보다 크면 줄인다</summary>
        private void ResizePanel()
        {
            float content = 0f;
            int rows = 0;

            foreach (LayoutElement row in _rows)
            {
                if (!row.gameObject.activeSelf) continue;

                content += row.preferredHeight;
                rows++;
            }

            if (rows > 1) content += (rows - 1) * RowSpacing;

            float width = PanelWidth + Border * 2f;
            float height = TitleArea + content + FooterArea + Border * 2f;
            _frame.sizeDelta = new Vector2(width, height);

            Rect screen = ((RectTransform)transform).rect;
            float scale = 1f;
            if (screen.height > 1f) scale = Mathf.Min(scale, (screen.height - 40f) / height);
            if (screen.width > 1f) scale = Mathf.Min(scale, (screen.width - 40f) / width);

            _frame.localScale = new Vector3(scale, scale, 1f);
        }

        private void Toast(string message, Color color)
        {
            if (_toastText == null) return;

            _toastText.text = message;
            _toastText.color = color;
            _toastUntil = Time.unscaledTime + toastTime;
        }

        private static void Sfx(AudioClip clip)
        {
            if (clip == null || SoundManager.Instance == null) return;
            SoundManager.Instance.PlaySFX(clip);
        }

        private static void Deselect()
        {
            // 버튼이 선택된 채로 남으면 스페이스·엔터로 다시 눌린다
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        // ════════════════════════════════════════════════════════════
        //  화면 — 만들기
        // ════════════════════════════════════════════════════════════

        private void BuildUI()
        {
            var root = (RectTransform)transform;
            Stretch(root, 0f);

            // 다른 UI 위에 그려지게 자체 Canvas 로 순서를 올린다. 버튼이 눌리려면 Raycaster 도 필요하다
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

            // ── 창 (열 때만 켜진다) ──
            _window = NewRect("Board", root);
            Stretch(_window, 0f);

            // 뒤를 어둡게. 여기를 누르면 닫힌다. 밭 클릭도 막아준다
            Image dim = NewImage("Dim", _window, DimColor, true);
            Stretch(dim.rectTransform, 0f);
            var dimButton = dim.gameObject.AddComponent<Button>();
            dimButton.transition = Selectable.Transition.None;
            dimButton.navigation = new Navigation { mode = Navigation.Mode.None };
            dimButton.onClick.AddListener(Close);

            Image frame = NewImage("Frame", _window, FrameColor, true);
            _frame = frame.rectTransform;
            PlaceBox(_frame, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(PanelWidth + Border * 2f, 400f));

            Image paper = NewImage("Paper", frame.transform, PaperColor, true);
            Stretch(paper.rectTransform, Border);

            TextMeshProUGUI title = NewText("Title", paper.transform, 30f, InkColor, TextAlignmentOptions.Left);
            title.fontStyle = FontStyles.Bold;
            title.text = "의뢰 게시판";
            PlaceTop(title.rectTransform, 24f, 120f, 12f, 42f);

            Button close = NewButton("Close", paper.transform, "X", CloseColor, out _);
            PlaceBox((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(-14f, -12f), new Vector2(44f, 44f));
            close.onClick.AddListener(Close);

            // ── 목록 (위에서부터 차례로 쌓인다) ──
            RectTransform list = NewRect("List", paper.transform);
            PlaceStretch(list, 20f, 20f, TitleArea, FooterArea);

            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = RowSpacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            _storyHeader = NewRowText("StoryHeader", list, 21f, InkColor, TextAlignmentOptions.BottomLeft, SectionHeight);
            _storyHeader.fontStyle = FontStyles.Bold;

            for (int i = 0; i < maxActive; i++) _storyCards.Add(BuildCard(list));

            _storyNote = NewRowText("StoryNote", list, 20f, SubInkColor, TextAlignmentOptions.Center, NoteHeight);

            _dailyHeader = NewRowText("DailyHeader", list, 21f, InkColor, TextAlignmentOptions.BottomLeft, SectionHeight);
            _dailyHeader.fontStyle = FontStyles.Bold;

            for (int i = 0; i < dailyCount; i++) _dailyCards.Add(BuildCard(list));

            TextMeshProUGUI footer = NewText("Footer", paper.transform, 18f, SubInkColor, TextAlignmentOptions.Center);
            PlaceBottom(footer.rectTransform, 20f, 20f, 8f, 26f);
            footer.text = toggleKey != Key.None ? $"{KeyName(toggleKey)} 또는 Esc 로 닫기" : "Esc 로 닫기";

            // ── 알림 (창이 닫혀 있어도 뜬다) ──
            BuildToast(root);
        }

        /// <summary>목록에 들어가는 글자 한 줄 (칸 제목, 안내 문구)</summary>
        private TextMeshProUGUI NewRowText(string name, Transform parent, float size, Color color, TextAlignmentOptions align, float height)
        {
            TextMeshProUGUI t = NewText(name, parent, size, color, align);
            t.overflowMode = TextOverflowModes.Ellipsis;

            var le = t.gameObject.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            _rows.Add(le);

            return t;
        }

        private CardView BuildCard(Transform parent)
        {
            var v = new CardView();

            Image bg = NewImage("Card", parent, CardColor, false);
            var le = bg.gameObject.AddComponent<LayoutElement>();
            le.minHeight = CardHeight;
            le.preferredHeight = CardHeight;
            _rows.Add(le);
            v.root = bg.gameObject;

            // 왼쪽 아이콘 칸. 아이템 그림이 없으면 종류 글자를 쓴다
            Image iconBg = NewImage("IconBg", bg.transform, IconBgColor, false);
            PlaceBox(iconBg.rectTransform, new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(70f, 70f), new Vector2(0f, 0.5f));

            v.icon = NewImage("Icon", iconBg.transform, Color.white, false);
            v.icon.preserveAspect = true;
            Stretch(v.icon.rectTransform, 7f);

            v.iconLabel = NewText("IconLabel", iconBg.transform, 19f, InkColor, TextAlignmentOptions.Center);
            v.iconLabel.fontStyle = FontStyles.Bold;
            Stretch(v.iconLabel.rectTransform, 2f);

            // 가운데 글자
            v.title = NewText("Title", bg.transform, 21f, InkColor, TextAlignmentOptions.Left);
            v.title.fontStyle = FontStyles.Bold;
            v.title.overflowMode = TextOverflowModes.Ellipsis;
            PlaceTop(v.title.rectTransform, 98f, 200f, 8f, 28f);

            v.desc = NewText("Desc", bg.transform, 16f, SubInkColor, TextAlignmentOptions.Left);
            v.desc.overflowMode = TextOverflowModes.Ellipsis;
            PlaceTop(v.desc.rectTransform, 98f, 200f, 38f, 22f);

            // 진행 막대 + 숫자
            Image barBg = NewImage("BarBg", bg.transform, BarBgColor, false);
            PlaceBottom(barBg.rectTransform, 98f, 300f, 16f, 12f);

            v.barFillImage = NewImage("Fill", barBg.transform, BarColor, false);
            v.barFill = v.barFillImage.rectTransform;
            v.barFill.anchorMin = Vector2.zero;
            v.barFill.anchorMax = new Vector2(0f, 1f);
            v.barFill.offsetMin = Vector2.zero;
            v.barFill.offsetMax = Vector2.zero;

            v.progress = NewText("Progress", bg.transform, 16f, SubInkColor, TextAlignmentOptions.Left);
            PlaceBox(v.progress.rectTransform, new Vector2(1f, 0f), new Vector2(-200f, 9f), new Vector2(92f, 26f), new Vector2(1f, 0f));

            // 오른쪽 보상 + 버튼
            v.reward = NewText("Reward", bg.transform, 15f, RewardInkColor, TextAlignmentOptions.TopRight);
            v.reward.overflowMode = TextOverflowModes.Ellipsis;
            PlaceBox(v.reward.rectTransform, new Vector2(1f, 1f), new Vector2(-16f, -8f), new Vector2(176f, 40f));

            v.button = NewButton("Button", bg.transform, "보상 받기", ButtonOnColor, out v.buttonText);
            v.buttonImage = (Image)v.button.targetGraphic;
            PlaceBox((RectTransform)v.button.transform, new Vector2(1f, 0f), new Vector2(-16f, 10f), new Vector2(166f, 38f), new Vector2(1f, 0f));

            CardView captured = v;
            v.button.onClick.AddListener(() =>
            {
                if (captured.quest != null) Claim(captured.quest);
                Deselect();
            });

            return v;
        }

        private void BuildToast(RectTransform root)
        {
            Image bg = NewImage("Toast", root, new Color(0.12f, 0.09f, 0.07f, 0.88f), false);
            RectTransform rt = bg.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -toastY);

            // 글자 길이에 맞춰 배경이 늘어나게
            var layout = bg.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 10, 10);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = bg.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _toastText = NewText("Text", bg.transform, 22f, Color.white, TextAlignmentOptions.Center);

            _toastGroup = bg.gameObject.AddComponent<CanvasGroup>();
            _toastGroup.alpha = 0f;
            _toastGroup.interactable = false;
            _toastGroup.blocksRaycasts = false;
        }

        // ── 작은 도우미들 ──

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private static Image NewImage(string name, Transform parent, Color color, bool raycast)
        {
            RectTransform rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        private TextMeshProUGUI NewText(string name, Transform parent, float size, Color color, TextAlignmentOptions align)
        {
            RectTransform rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.textWrappingMode = TextWrappingModes.NoWrap;
            if (font != null) t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.richText = true;
            t.raycastTarget = false;
            return t;
        }

        private Button NewButton(string name, Transform parent, string label, Color color, out TextMeshProUGUI text)
        {
            Image img = NewImage(name, parent, color, true);

            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            ColorBlock cb = button.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            cb.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.85f, 0.85f, 0.85f, 0.7f);
            button.colors = cb;

            text = NewText("Label", img.transform, 19f, Color.white, TextAlignmentOptions.Center);
            text.fontStyle = FontStyles.Bold;
            text.text = label;
            Stretch(text.rectTransform, 0f);

            return button;
        }

        /// <summary>부모를 꽉 채운다 (inset 만큼 안쪽으로)</summary>
        private static void Stretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        /// <summary>가로로 늘어나고 위에 붙는다. 좌·우·위 여백과 높이</summary>
        private static void PlaceTop(RectTransform rt, float left, float right, float top, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(left, -top - height);
            rt.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>가로로 늘어나고 아래에 붙는다. 좌·우·아래 여백과 높이</summary>
        private static void PlaceBottom(RectTransform rt, float left, float right, float bottom, float height)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, bottom + height);
        }

        /// <summary>네 변의 여백으로 채운다</summary>
        private static void PlaceStretch(RectTransform rt, float left, float right, float top, float bottom)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>한 점에 붙인 고정 크기 상자. pivot 을 안 주면 anchor 와 같게</summary>
        private static void PlaceBox(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot ?? anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
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
