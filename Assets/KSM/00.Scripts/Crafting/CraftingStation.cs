using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using KSM._00.Scripts.Items;
 
namespace KSM._00.Scripts.Crafting
{
    /// <summary>
    /// 월드에 놓는 제작대(모루, 요리대 등).
    /// 플레이어가 가까이 오면 "E" 같은 안내 버튼을 띄우고, 그 키를 누르면 제작창을 연다.
    ///
    /// 씬 구조:
    ///   ForgeObj              SpriteRenderer + CraftingStation
    ///    └ Prompt             ← Prompt Root 에 연결 (평소엔 꺼둔다)
    ///        ├ KeyBg          Image / SpriteRenderer
    ///        └ Text (TMP)     TextMeshPro   ← Prompt Text 에 연결
    ///
    /// 안 열릴 때는 인스펙터 우클릭 → <b>제작대 진단</b> 을 누르면
    /// 어디서 막혔는지 콘솔에 전부 찍어준다.
    /// </summary>
    public class CraftingStation : MonoBehaviour
    {
        [Header("제작")]
        [Tooltip("이 제작대에서 만들 수 있는 것들")]
        [SerializeField] private CraftingStationSO station;
 
        [Tooltip("비우면 씬에서 자동으로 찾는다")]
        [SerializeField] private CraftingUI craftingUI;
 
        [Header("상호작용")]
        [Tooltip("이 키를 누르면 제작창이 열린다")]
        [SerializeField] private Key interactKey = Key.E;
 
        [Tooltip("이 거리 안에 들어오면 안내가 뜬다 (월드 단위)")]
        [SerializeField, Min(0.1f)] private float interactRange = 2f;
 
        [Tooltip("거리를 잴 기준점. 비우면 이 오브젝트의 위치를 쓴다.\n" +
                 "스프라이트 중심과 피벗이 다를 때 자식 Empty 를 만들어 꽂으면 편하다")]
        [SerializeField] private Transform rangeOrigin;
 
        [Header("안내 버튼")]
        [Tooltip("가까이 왔을 때 켜질 오브젝트. 평소엔 꺼둔다")]
        [SerializeField] private GameObject promptRoot;
 
        [Tooltip("키 글자가 들어갈 텍스트. 비워도 동작한다")]
        [SerializeField] private TMP_Text promptText;
 
        [Tooltip("표시할 문구. {KEY} 자리에 키 이름이 들어간다")]
        [SerializeField] private string promptFormat = "{KEY}";
 
        [Header("디버그")]
        [Tooltip("범위에 들어오고 나갈 때, 키를 눌렀을 때 콘솔에 찍는다")]
        [SerializeField] private bool verboseLog = true;
 
        [Tooltip("선택하지 않아도 씬 뷰에 범위 원을 그린다")]
        [SerializeField] private bool alwaysDrawRange = true;
 
        private Transform _player;
        private bool _inRange;
        private float _nextPlayerSearch;
 
        /// <summary>거리를 재는 기준 위치</summary>
        private Vector3 Origin => rangeOrigin != null ? rangeOrigin.position : transform.position;
 
        // ════════════════════════════════════════════════════════════
        //  수명 주기
        // ════════════════════════════════════════════════════════════
 
        private void Awake()
        {
            if (craftingUI == null)
                craftingUI = FindFirstObjectByType<CraftingUI>(FindObjectsInactive.Include);
 
            SetPromptVisible(false);
        }
 
        private void Start()
        {
            FindPlayer();
            UpdatePromptText();
        }
 
        private void Update()
        {
            // 플레이어가 나중에 생기는 구조여도 계속 찾아본다 (0.5초에 한 번)
            if (_player == null)
            {
                if (Time.unscaledTime < _nextPlayerSearch) return;
 
                _nextPlayerSearch = Time.unscaledTime + 0.5f;
                FindPlayer();
 
                if (_player == null) return;
            }
 
            float dist = Vector2.Distance(Origin, _player.position);
            bool near = !CraftingUI.IsOpen && dist <= interactRange;
 
            if (near != _inRange)
            {
                _inRange = near;
                SetPromptVisible(near);
 
                if (verboseLog)
                    Debug.Log($"[제작대] {name} — 범위 {(near ? "진입" : "이탈")} (거리 {dist:0.00} / {interactRange:0.00})", this);
            }
 
            if (!near) return;
            if (Keyboard.current == null)
            {
                if (verboseLog) Debug.LogWarning("[제작대] Keyboard.current 가 null 입니다. Input System 설정을 확인하세요.", this);
                return;
            }
 
            if (!Keyboard.current[interactKey].wasPressedThisFrame) return;
 
            if (verboseLog) Debug.Log($"[제작대] {interactKey} 입력 감지 → 열기 시도", this);
 
            Open();
        }
 
        // ════════════════════════════════════════════════════════════
        //  열기
        // ════════════════════════════════════════════════════════════
 
        public void Open()
        {
            if (craftingUI == null)
            {
                craftingUI = FindFirstObjectByType<CraftingUI>(FindObjectsInactive.Include);
 
                if (craftingUI == null)
                {
                    Debug.LogError("[제작대] 씬에 CraftingUI 가 없습니다. Canvas 에 CraftingUI 스크립트를 붙이세요.", this);
                    return;
                }
            }
 
            if (station == null)
            {
                Debug.LogError("[제작대] Station 칸이 비어 있습니다. CraftingStationSO 를 꽂으세요.", this);
                return;
            }
 
            if (!station.IsUsable)
            {
                Debug.LogError($"[제작대] '{station.name}' 의 Recipes 배열이 비어 있습니다. 레시피를 넣으세요.", station);
                return;
            }
 
            SetPromptVisible(false);
            _inRange = false;
 
            craftingUI.Open(station);
        }
 
        // ════════════════════════════════════════════════════════════
        //  내부
        // ════════════════════════════════════════════════════════════
 
        private void FindPlayer()
        {
            PlayerInventory inv = PlayerInventory.Instance;
            if (inv != null) { _player = inv.transform; return; }
 
            // 인벤토리를 못 찾으면 태그로 한 번 더
            GameObject tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged != null) _player = tagged.transform;
        }
 
        private void SetPromptVisible(bool on)
        {
            if (promptRoot != null) promptRoot.SetActive(on);
        }
 
        private void UpdatePromptText()
        {
            if (promptText == null) return;
 
            promptText.text = promptFormat.Replace("{KEY}", PrettyKey(interactKey));
        }
 
        private static string PrettyKey(Key key)
        {
            string s = key.ToString();
 
            // Digit1 → 1, Numpad1 → 1 처럼 보기 좋게 다듬는다
            if (s.StartsWith("Digit")) s = s.Substring(5);
            else if (s.StartsWith("Numpad")) s = s.Substring(6);
 
            return s.ToUpperInvariant();
        }
 
        private void OnValidate()
        {
            if (Application.isPlaying) UpdatePromptText();
        }
 
        // ════════════════════════════════════════════════════════════
        //  진단
        // ════════════════════════════════════════════════════════════
 
        [ContextMenu("제작대 진단")]
        private void Diagnose()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"═══ 제작대 진단 : {name} ═══");
 
            // 1. 이 오브젝트가 살아있는가
            sb.AppendLine($"1. 오브젝트 활성 : {(gameObject.activeInHierarchy ? "O" : "X ← 부모가 꺼져 있으면 Update 가 안 돕니다")}");
            sb.AppendLine($"   스크립트 활성 : {(enabled ? "O" : "X ← 컴포넌트 체크박스가 꺼져 있습니다")}");
 
            // 2. Station SO
            if (station == null)
                sb.AppendLine("2. Station : X ← 비어 있습니다");
            else
                sb.AppendLine($"2. Station : {station.name} / 레시피 {(station.recipes == null ? 0 : station.recipes.Length)}개" +
                              $" {(station.IsUsable ? "O" : "← X 레시피가 없어서 열려도 빈 창입니다")}");
 
            // 3. CraftingUI
            CraftingUI ui = craftingUI != null ? craftingUI : FindFirstObjectByType<CraftingUI>(FindObjectsInactive.Include);
            sb.AppendLine($"3. CraftingUI : {(ui == null ? "X ← 씬에 없습니다" : ui.name + " O")}");
            sb.AppendLine($"   CraftingUI.IsOpen : {CraftingUI.IsOpen}{(CraftingUI.IsOpen ? "  ← true 로 굳어 있으면 안내가 절대 안 뜹니다" : string.Empty)}");
 
            // 4. 플레이어
            if (!Application.isPlaying) FindPlayer();
 
            if (_player == null)
                sb.AppendLine("4. 플레이어 : X ← PlayerInventory 도, Player 태그도 못 찾았습니다");
            else
            {
                float d = Vector2.Distance(Origin, _player.position);
                sb.AppendLine($"4. 플레이어 : {_player.name} / 거리 {d:0.00} (범위 {interactRange:0.00}) {(d <= interactRange ? "→ 범위 안 O" : "→ 범위 밖 X")}");
            }
 
            // 5. 안내 버튼
            sb.AppendLine($"5. Prompt Root : {(promptRoot == null ? "X ← 비어 있어서 안내가 안 뜹니다 (열리기는 합니다)" : promptRoot.name + " O")}");
            sb.AppendLine($"   Prompt Text : {(promptText == null ? "(없음 — 선택사항)" : promptText.name + " O")}");
 
            // 6. 입력
            sb.AppendLine($"6. Interact Key : {interactKey} → 화면 표시 \"{PrettyKey(interactKey)}\"");
            sb.AppendLine($"   Keyboard.current : {(Keyboard.current == null ? "X ← Input System 미설정" : "O")}");
 
            Debug.Log(sb.ToString(), this);
        }
 
        [ContextMenu("지금 강제로 열어보기")]
        private void ForceOpen()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[제작대] 플레이 중에만 됩니다.", this);
                return;
            }
 
            Open();
        }
 
        // ════════════════════════════════════════════════════════════
 
        private void OnDrawGizmos()
        {
            if (!alwaysDrawRange) return;
 
            Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
            Gizmos.DrawWireSphere(Origin, interactRange);
        }
 
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(Origin, interactRange);
        }
    }
}
 