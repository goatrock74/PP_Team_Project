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
    ///   Anvil                 SpriteRenderer + CraftingStation
    ///    └ Prompt             ← Prompt Root 에 연결 (평소엔 꺼둔다)
    ///        ├ Background     Image
    ///        └ KeyText        TextMeshPro   ← Prompt Text 에 연결
    ///
    /// Prompt 는 월드 스페이스 Canvas 로 만들거나, 스프라이트 + TMP 조합 아무거나 상관없다.
    /// 이 스크립트는 켜고 끄고 글자만 바꾼다.
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
 
        [Header("안내 버튼")]
        [Tooltip("가까이 왔을 때 켜질 오브젝트. 평소엔 꺼둔다")]
        [SerializeField] private GameObject promptRoot;
 
        [Tooltip("키 글자가 들어갈 텍스트. 비워도 동작한다")]
        [SerializeField] private TMP_Text promptText;
 
        [Tooltip("표시할 문구. {KEY} 자리에 키 이름이 들어간다")]
        [SerializeField] private string promptFormat = "{KEY}";
 
        private Transform _player;
        private bool _inRange;
 
        private void Awake()
        {
            if (craftingUI == null) craftingUI = FindFirstObjectByType<CraftingUI>(FindObjectsInactive.Include);
 
            SetPromptVisible(false);
        }
 
        private void Start()
        {
            // 플레이어는 PlayerInventory 가 붙어 있는 오브젝트로 본다
            PlayerInventory player = PlayerInventory.Instance;
            if (player != null) _player = player.transform;
 
            if (_player == null)
                Debug.LogWarning("[제작대] 씬에서 플레이어(PlayerInventory)를 찾지 못했습니다.", this);
 
            UpdatePromptText();
        }
 
        private void Update()
        {
            if (_player == null) return;
 
            bool near = !CraftingUI.IsOpen
                     && Vector2.Distance(transform.position, _player.position) <= interactRange;
 
            if (near != _inRange)
            {
                _inRange = near;
                SetPromptVisible(near);
            }
 
            if (!near || Keyboard.current == null) return;
            if (!Keyboard.current[interactKey].wasPressedThisFrame) return;
 
            Open();
        }
 
        // ════════════════════════════════════════════════════════════
 
        public void Open()
        {
            if (craftingUI == null)
            {
                Debug.LogWarning("[제작대] 씬에 CraftingUI 가 없습니다.", this);
                return;
            }
 
            if (station == null || !station.IsUsable)
            {
                Debug.LogWarning("[제작대] Station SO 가 비어 있거나 레시피가 없습니다.", this);
                return;
            }
 
            SetPromptVisible(false);
            _inRange = false;
 
            craftingUI.Open(station);
        }
 
        private void SetPromptVisible(bool on)
        {
            if (promptRoot != null) promptRoot.SetActive(on);
        }
 
        private void UpdatePromptText()
        {
            if (promptText == null) return;
 
            string key = interactKey.ToString();
 
            // Digit1 → 1 처럼 보기 좋게 다듬는다
            if (key.StartsWith("Digit")) key = key.Substring(5);
            else if (key.StartsWith("Numpad")) key = key.Substring(6);
 
            promptText.text = promptFormat.Replace("{KEY}", key.ToUpperInvariant());
        }
 
        private void OnValidate()
        {
            if (Application.isPlaying) UpdatePromptText();
        }
 
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}