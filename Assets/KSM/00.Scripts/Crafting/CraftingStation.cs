using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using KSM._00.Scripts.Items;
 
namespace KSM._00.Scripts.Crafting
{
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
        private Vector3 Origin => rangeOrigin != null ? rangeOrigin.position : transform.position;
        
 
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
 
            // ★ 이 프레임에 창이 방금 닫혔다면, 그 키 입력은 이미 "닫기" 로 쓰인 것이다.
            //   막지 않으면 스크립트 실행 순서에 따라 닫자마자 다시 열려버린다
            if (Time.frameCount == CraftingUI.LastCloseFrame) return;
 
            if (!Keyboard.current[interactKey].wasPressedThisFrame) return;
 
            if (verboseLog) Debug.Log($"[제작대] {interactKey} 입력 감지 → 열기 시도", this);
 
            Open();
        }
        
 
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
                return;
            }
 
            if (!station.IsUsable)
            {
                return;
            }
 
            SetPromptVisible(false);
            _inRange = false;
 
            craftingUI.Open(station);
        }
 
        private void FindPlayer()
        {
            PlayerInventory inv = PlayerInventory.Instance;
            if (inv != null) { _player = inv.transform; return; }
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
            if (s.StartsWith("Digit")) s = s.Substring(5);
            else if (s.StartsWith("Numpad")) s = s.Substring(6);
 
            return s.ToUpperInvariant();
        }
 
        private void OnValidate()
        {
            if (Application.isPlaying) UpdatePromptText();
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
