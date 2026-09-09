using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
 
/// <summary>
/// 탭 묶음. 버튼을 누르면 그 탭의 화면만 켜지고, <b>선택된 버튼이 조금 커진다.</b>
///
/// 인벤토리 ↔ 스탯 전환에 쓰지만 아무 데나 재사용할 수 있다.
///
/// 씬 구조 예시:
///   InventoryPanel
///    ├ TabBar                (Horizontal Layout Group)   ← TabGroupUI 를 여기에
///    │   ├ Btn_Inventory     (Button + TMP 자식)
///    │   └ Btn_Stat          (Button + TMP 자식)
///    └ Pages
///        ├ Page_Inventory    (기존 슬롯 그리드 + 정보 텍스트를 여기로 넣는다)
///        └ Page_Stat         (MasteryPanelUI)
///
/// Tabs 배열에 (버튼, 페이지) 짝을 순서대로 넣으면 끝이다.
///
/// ※ Layout Group 은 자식의 Scale 을 계산에 넣지 않는다. 그래서 버튼이 커져도
///   옆 버튼이 밀리지 않고 제자리에서 커진다. 밀리길 원하면 Scale 대신
///   LayoutElement 의 Preferred Width 를 바꾸는 방식으로 고쳐야 한다.
/// </summary>
public class TabGroupUI : MonoBehaviour
{
    [Serializable]
    public struct TabEntry
    {
        [Tooltip("인스펙터에서 알아보기 쉬우라고 두는 이름. 동작에는 안 쓴다")]
        public string name;
 
        public Button button;
 
        [Tooltip("크기가 바뀔 대상. 비우면 Button 자신")]
        public RectTransform scaleTarget;
 
        [Tooltip("이 탭을 고르면 켜질 화면")]
        public GameObject page;
    }
 
    [Header("탭")]
    [SerializeField] private TabEntry[] tabs;
 
    [Tooltip("시작할 때 열려 있을 탭")]
    [SerializeField, Min(0)] private int defaultIndex;
 
    [Header("선택 표시")]
    [Tooltip("선택된 탭의 크기 배수")]
    [SerializeField, Min(1f)] private float activeScale = 1.15f;
 
    [SerializeField, Min(0.1f)] private float inactiveScale = 1f;
 
    [Tooltip("크기가 변하는 속도. 0이면 즉시 바뀐다")]
    [SerializeField, Min(0f)] private float tweenSpeed = 14f;
 
    [Header("색 (선택)")]
    [Tooltip("끄면 크기만 바뀌고 색은 안 건드린다")]
    [SerializeField] private bool tintLabels = true;
 
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new(0.65f, 0.65f, 0.65f);
 
    /// <summary>지금 열려 있는 탭 번호</summary>
    public int Current { get; private set; } = -1;
 
    /// <summary>탭이 바뀌었을 때. 화면 쪽에서 갱신이 필요하면 구독하면 된다</summary>
    public event Action<int> OnTabChanged;
 
    private void Awake()
    {
        if (tabs == null) return;
 
        for (int i = 0; i < tabs.Length; i++)
        {
            // ★ for 의 i 를 그대로 람다에 넣으면 모든 버튼이 마지막 번호를 갖게 된다.
            //   반드시 이렇게 복사해서 넘겨야 한다
            int index = i;
 
            if (tabs[i].button != null)
                tabs[i].button.onClick.AddListener(() => Select(index));
        }
    }
 
    private void Start() => Select(defaultIndex, instant: true);
 
    private void OnDestroy()
    {
        if (tabs == null) return;
 
        foreach (TabEntry t in tabs)
            if (t.button != null) t.button.onClick.RemoveAllListeners();
    }
 
    // ════════════════════════════════════════════════════════════
 
    public void Select(int index) => Select(index, instant: false);
 
    public void Select(int index, bool instant)
    {
        if (tabs == null || tabs.Length == 0) return;
 
        index = Mathf.Clamp(index, 0, tabs.Length - 1);
        if (index == Current) return;
 
        Current = index;
 
        for (int i = 0; i < tabs.Length; i++)
        {
            bool on = i == index;
 
            if (tabs[i].page != null) tabs[i].page.SetActive(on);
 
            if (!tintLabels) continue;
 
            // 버튼 안의 글자 색을 바꾼다 (없으면 그냥 넘어간다)
            if (tabs[i].button == null) continue;
 
            TMP_Text label = tabs[i].button.GetComponentInChildren<TMP_Text>();
            if (label != null) label.color = on ? activeColor : inactiveColor;
        }
 
        if (instant || tweenSpeed <= 0f) ApplyScaleInstant();
 
        OnTabChanged?.Invoke(index);
    }
 
    private void Update()
    {
        if (tabs == null || tweenSpeed <= 0f) return;
 
        // unscaled: 인벤토리를 열면서 시간을 멈춰도 애니메이션은 돈다
        float t = 1f - Mathf.Exp(-tweenSpeed * Time.unscaledDeltaTime);
 
        for (int i = 0; i < tabs.Length; i++)
        {
            RectTransform target = Target(i);
            if (target == null) continue;
 
            float want = i == Current ? activeScale : inactiveScale;
            float now = Mathf.Lerp(target.localScale.x, want, t);
 
            target.localScale = new Vector3(now, now, 1f);
        }
    }
 
    private void ApplyScaleInstant()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            RectTransform target = Target(i);
            if (target == null) continue;
 
            float want = i == Current ? activeScale : inactiveScale;
            target.localScale = new Vector3(want, want, 1f);
        }
    }
 
    private RectTransform Target(int i)
    {
        if (tabs[i].scaleTarget != null) return tabs[i].scaleTarget;
        return tabs[i].button != null ? (RectTransform)tabs[i].button.transform : null;
    }
}
 