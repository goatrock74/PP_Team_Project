using TMPro;
using UnityEngine;
using UnityEngine.UI;
 
/// <summary>
/// 스탯 화면의 카드 한 장. 사진의 네모 하나에 해당한다.
///
/// 프리팹 구조 (이름은 자유, 인스펙터에서 연결만 하면 된다):
///   MasteryCard         Image(배경) + LayoutElement + MasteryCardUI
///    ├ Icon             Image
///    ├ Name             TMP        "채집"
///    ├ Level            TMP        "Level: 12"  /  "Level: Max"
///    ├ ExpBar           Image (Image Type = Filled, Fill Method = Horizontal)
///    │   └ (배경은 ExpBar 의 부모에 따로 깔면 예쁘다)
///    ├ ExpText          TMP        "140 / 300"
///    └ Stats            TMP        "채집 행운 +12%  ◆ Lv10 빠른 재생 ..."
///
/// 안 쓸 칸은 비워둬도 된다. 전부 null 검사를 한다.
/// </summary>
public class MasteryCardUI : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
 
    [Tooltip("추가 능력치 목록. MasterySO 가 만들어준 문자열이 그대로 들어간다")]
    [SerializeField] private TMP_Text statText;
 
    [Header("경험치 바 (둘 중 편한 쪽만 연결)")]
    [Tooltip("Image Type 을 Filled 로 바꿔야 채워진다")]
    [SerializeField] private Image expFill;
 
    [SerializeField] private Slider expSlider;
 
    [SerializeField] private TMP_Text expText;
 
    [Header("색")]
    [Tooltip("마스터리 종류별 색을 칠할 곳 (테두리 등). 비워도 된다")]
    [SerializeField] private Image accent;
 
    [Tooltip("아이콘에도 종류별 색을 입힌다")]
    [SerializeField] private bool tintIcon;
 
    [Tooltip("만렙일 때 레벨 글자 색")]
    [SerializeField] private Color maxLevelColor = new(1f, 0.82f, 0.3f);
 
    private MasterySO _def;
    private MasteryManager _manager;
    private Color _normalLevelColor = Color.white;
 
    /// <summary>이 카드가 보여주는 마스터리</summary>
    public MasterySO Definition => _def;
 
    private void Awake()
    {
        if (levelText != null) _normalLevelColor = levelText.color;
    }
 
    /// <summary>패널이 카드를 만들면서 한 번 부른다</summary>
    public void Bind(MasterySO definition, MasteryManager manager)
    {
        _def = definition;
        _manager = manager;
 
        if (_def == null) return;
 
        name = $"Card_{_def.type}";
 
        if (iconImage != null)
        {
            iconImage.enabled = _def.icon != null;
            iconImage.sprite = _def.icon;
 
            if (tintIcon) iconImage.color = MasteryUtil.TypeColor(_def.type);
        }
 
        if (nameText != null) nameText.text = _def.Title;
        if (accent != null) accent.color = MasteryUtil.TypeColor(_def.type);
 
        Refresh();
    }
 
    /// <summary>레벨이나 경험치가 바뀔 때마다 부른다</summary>
    public void Refresh()
    {
        if (_def == null) return;
 
        int level = _manager != null ? _manager.GetLevel(_def.type) : 1;
        bool isMax = _def.IsMaxLevel(level);
 
        // ── 레벨 ──
        if (levelText != null)
        {
            levelText.text = isMax ? "Level: Max" : $"Level: {level}";
            levelText.color = isMax ? maxLevelColor : _normalLevelColor;
        }
 
        // ── 경험치 ──
        float ratio = _manager != null ? _manager.GetExpRatio(_def.type) : 0f;
 
        if (expFill != null) expFill.fillAmount = ratio;
        if (expSlider != null) expSlider.value = ratio;
 
        if (expText != null)
        {
            if (isMax)
            {
                expText.text = "MAX";
            }
            else
            {
                MasteryProgress p = _manager != null ? _manager.GetProgress(_def.type) : null;
                int have = p != null ? p.exp : 0;
 
                expText.text = $"{have} / {_def.ExpToNext(level)}";
            }
        }
 
        // ── 추가 능력치 ──
        if (statText != null) statText.text = _def.BuildStatText(level);
    }
}
 