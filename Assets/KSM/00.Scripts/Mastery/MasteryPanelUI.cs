using System.Collections.Generic;
using UnityEngine;
 
/// <summary>
/// 스탯 화면. MasteryManager 에 등록된 마스터리마다 카드를 한 장씩 만든다.
///
/// 씬 구조:
///   Page_Stat            ← TabGroupUI 의 두 번째 Page 로 연결
///    └ CardArea          Horizontal Layout Group (또는 Grid Layout Group)
///                        + MasteryPanelUI          ← Card Parent 에 자기 자신
///
/// 카드는 MasterySO 를 인스펙터에 넣은 <b>순서대로</b> 만들어진다.
/// 순서를 바꾸고 싶으면 MasteryManager 의 Masteries 배열 순서를 바꾸면 된다.
/// </summary>
public class MasteryPanelUI : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("카드가 생성될 곳. Layout Group 이 붙어 있어야 줄이 맞는다")]
    [SerializeField] private Transform cardParent;
 
    [SerializeField] private MasteryCardUI cardPrefab;
 
    [Header("설정")]
    [Tooltip("켜면 화면이 열릴 때마다 카드를 다시 만든다. 보통 끈다")]
    [SerializeField] private bool rebuildOnEnable;
 
    private readonly List<MasteryCardUI> _cards = new();
    private MasteryManager _manager;
    private bool _built;
 
    private void Awake()
    {
        if (cardParent == null) cardParent = transform;
    }
 
    private void OnEnable()
    {
        _manager = MasteryManager.Instance;
 
        if (_manager == null)
        {
            Debug.LogWarning("[스탯화면] 씬에 MasteryManager 가 없습니다.", this);
            return;
        }
 
        if (rebuildOnEnable) Clear();
        if (!_built) Build();
 
        _manager.OnChanged += HandleChanged;
 
        RefreshAll();   // 창이 닫혀 있는 동안 오른 레벨을 반영한다
    }
 
    private void OnDisable()
    {
        if (_manager != null) _manager.OnChanged -= HandleChanged;
        _manager = null;
    }
 
    // ════════════════════════════════════════════════════════════
 
    private void Build()
    {
        if (cardPrefab == null)
        {
            Debug.LogError("[스탯화면] Card Prefab 이 비어있습니다.", this);
            return;
        }
 
        IReadOnlyList<MasterySO> defs = _manager.Definitions;
 
        if (defs == null || defs.Count == 0)
        {
            Debug.LogWarning("[스탯화면] MasteryManager 의 Masteries 가 비어 있어 카드를 만들 수 없습니다.", this);
            return;
        }
 
        foreach (MasterySO def in defs)
        {
            if (def == null) continue;
 
            MasteryCardUI card = Instantiate(cardPrefab, cardParent);
            card.Bind(def, _manager);
 
            _cards.Add(card);
        }
 
        _built = true;
    }
 
    private void Clear()
    {
        foreach (MasteryCardUI c in _cards)
            if (c != null) Destroy(c.gameObject);
 
        _cards.Clear();
        _built = false;
    }
 
    private void HandleChanged(MasteryType type)
    {
        // 바뀐 카드만 갱신한다
        foreach (MasteryCardUI c in _cards)
        {
            if (c == null || c.Definition == null || c.Definition.type != type) continue;
 
            c.Refresh();
            return;
        }
    }
 
    public void RefreshAll()
    {
        foreach (MasteryCardUI c in _cards)
            if (c != null) c.Refresh();
    }
}
 