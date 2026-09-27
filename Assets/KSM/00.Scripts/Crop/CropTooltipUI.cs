using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using KSM._00.Scripts.Crop;
    /// <summary>
    /// 작물에 마우스를 올리면 정보창을 띄운다.
    ///
    ///   토마토
    ///   성장 60%  ·  수확까지 약 2일
    ///   물: 촉촉함
    ///
    /// 씬 구조 (Canvas 밑):
    ///   CropTooltip     Image(반투명 검정 배경) + CanvasGroup + CropTooltipUI
    ///    ├ Icon         Image (선택 — 작물 아이콘이 뜬다)
    ///    └ Text         TextMeshPro - Text (UI), 한글 폰트
    ///
    /// ★ 크기·위치는 코드가 알아서 맞춘다. 배경 크기를 신경 쓸 필요 없다
    /// ★ 이 오브젝트는 꺼두지 말 것. 숨기는 건 CanvasGroup 알파로 한다
    /// ★ 클릭은 통과시킨다. 안 그러면 정보창이 마우스 밑에 깔렸을 때 작물이 안 눌린다
    /// </summary>
    public class CropTooltipUI : MonoBehaviour
    {
        [Header("참조 (비우면 자동으로 찾음)")]
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text label;

        [Tooltip("작물 아이콘을 띄울 Image (선택)")]
        [SerializeField] private Image icon;

        [Tooltip("비우면 Main Camera")]
        [SerializeField] private Camera worldCamera;

        [Header("표시")]
        [Tooltip("마우스를 올리고 이 시간(초)이 지나야 뜬다. 밭을 훑고 지나갈 때 깜빡이지 않게")]
        [SerializeField, Min(0f)] private float showDelay = 0.15f;

        [SerializeField, Min(0.01f)] private float fadeTime = 0.1f;

        [Tooltip("마우스 커서에서 떨어진 거리 (캔버스 단위)")]
        [SerializeField] private Vector2 cursorOffset = new Vector2(22f, -22f);

        [Header("크기 자동 맞춤")]
        [Tooltip("끄면 배경 크기·글자 위치를 직접 잡은 그대로 쓴다")]
        [SerializeField] private bool autoSize = true;

        [SerializeField] private Vector2 padding = new Vector2(16f, 12f);

        [Tooltip("이보다 넓어지면 줄바꿈한다")]
        [SerializeField, Min(80f)] private float maxWidth = 400f;

        [SerializeField, Min(8f)] private float iconSize = 44f;

        [SerializeField, Min(0f)] private float iconGap = 12f;

        [Header("글자 색")]
        [SerializeField] private Color readyColor = new Color(1f, 0.86f, 0.35f);
        [SerializeField] private Color wetColor = new Color(0.45f, 0.78f, 1f);
        [SerializeField] private Color dryColor = new Color(1f, 0.62f, 0.38f);
        [SerializeField] private Color wiltedColor = new Color(0.8f, 0.62f, 0.45f);
        [SerializeField] private Color subColor = new Color(0.8f, 0.8f, 0.8f);

        private RectTransform _rect;
        private Canvas _canvas;

        private GrowCrop _hover;        // 지금 마우스 밑에 있는 작물
        private float _hoverTime;       // 그 작물 위에 머문 시간
        private GrowCrop _shown;        // 정보창에 표시 중인 작물
        private float _refreshTimer;
        private string _lastText;

        private readonly StringBuilder _sb = new StringBuilder(160);
        private string _hexReady, _hexWet, _hexDry, _hexWilted, _hexSub;

        private void Awake()
        {
            _rect = (RectTransform)transform;

            if (group == null) group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            if (label == null) label = GetComponentInChildren<TMP_Text>(true);

            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null) _canvas = _canvas.rootCanvas;

            // 정보창이 마우스를 가로채지 않게 한다
            group.interactable = false;
            group.blocksRaycasts = false;
            foreach (Graphic g in GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;

            // 부모의 가운데를 기준으로 위치를 잡는다
            _rect.anchorMin = _rect.anchorMax = new Vector2(0.5f, 0.5f);

            if (autoSize) SetupLayout();

            group.alpha = 0f;

            _hexReady = ColorUtility.ToHtmlStringRGB(readyColor);
            _hexWet = ColorUtility.ToHtmlStringRGB(wetColor);
            _hexDry = ColorUtility.ToHtmlStringRGB(dryColor);
            _hexWilted = ColorUtility.ToHtmlStringRGB(wiltedColor);
            _hexSub = ColorUtility.ToHtmlStringRGB(subColor);

            if (label == null)
                Debug.LogWarning("[작물 정보창] 자식에 TextMeshPro 글자가 없습니다.", this);
        }

        private void Update()
        {
            GrowCrop target = FindHoveredCrop();

            if (target != _hover)
            {
                _hover = target;
                _hoverTime = 0f;
            }
            else if (_hover != null)
            {
                _hoverTime += Time.unscaledDeltaTime;
            }

            // 이미 떠 있으면 옆 작물로 옮겨갈 때 기다리지 않고 바로 바꾼다
            bool show = _hover != null && (_hoverTime >= showDelay || group.alpha > 0.5f);

            if (show)
            {
                // 다른 작물로 옮겼거나, 숨어 있다가 다시 뜰 때는 내용을 바로 새로 쓴다
                if (_shown != _hover || group.alpha <= 0.001f)
                {
                    _shown = _hover;
                    _refreshTimer = 0f;
                }

                _refreshTimer -= Time.unscaledDeltaTime;
                if (_refreshTimer <= 0f)
                {
                    Refresh(_shown);
                    _refreshTimer = 0.25f;   // 자라는 중이면 숫자가 바뀌니 가끔 다시 쓴다
                }

                FollowMouse();
            }

            group.alpha = Mathf.MoveTowards(group.alpha, show ? 1f : 0f, Time.unscaledDeltaTime / fadeTime);
        }

        // ════════════════════════════════════════════════════════════
        //  어떤 작물 위에 있나
        // ════════════════════════════════════════════════════════════

        private GrowCrop FindHoveredCrop()
        {
            if (Mouse.current == null) return null;

            // 인벤토리 같은 UI 위에서는 안 띄운다
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return null;

            CropManager mgr = CropManager.Instance;
            if (mgr == null) return null;

            Camera cam = worldCamera != null ? worldCamera : Camera.main;
            if (cam == null) return null;

            Vector2 screen = Mouse.current.position.ReadValue();
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
            world.z = 0f;

            // 클릭과 똑같이 '칸' 으로 찾는다. 3x3 작물은 9칸 어디에 올려도 잡힌다
            return mgr.GetOccupant(mgr.WorldToCell(world));
        }

        // ════════════════════════════════════════════════════════════
        //  내용
        // ════════════════════════════════════════════════════════════

        private void Refresh(GrowCrop crop)
        {
            if (crop == null || crop.Data == null || label == null) return;

            CropSO data = crop.Data;
            CropManager mgr = CropManager.Instance;

            _sb.Clear();
            _sb.Append("<b>").Append(string.IsNullOrEmpty(data.cropName) ? data.name : data.cropName).Append("</b>\n");

            if (crop.IsWilted)
            {
                AppendColored(_hexWilted, "시들었다 — 제철이 지났다");
                _sb.Append('\n');
                AppendColored(_hexSub, "괭이 + X 로 뽑기");
            }
            else if (crop.CanHarvest)
            {
                AppendColored(_hexReady, "수확 가능!");

                if (data.harvestType == HarvestType.Multiple)
                    AppendColored(_hexSub, "  (다시 자람)");
            }
            else
            {
                _sb.Append("성장 ").Append(Mathf.FloorToInt(crop.GrowthProgress * 100f)).Append("%  ·  ");
                _sb.Append(RemainingText(crop, mgr));

                _sb.Append("\n물: ");
                if (mgr != null && mgr.IsWet(crop.OriginCell)) AppendColored(_hexWet, "촉촉함");
                else AppendColored(_hexDry, "말라 있음");
            }

            string text = _sb.ToString();
            if (text == _lastText) return;   // 바뀐 게 없으면 다시 그리지 않는다
            _lastText = text;

            label.text = text;

            if (icon != null)
            {
                Sprite s = data.icon;
                if (s == null && data.harvestItem != null) s = data.harvestItem.icon;

                icon.sprite = s;
                icon.enabled = s != null;
            }

            if (autoSize) Resize(text);
        }

        private static string RemainingText(GrowCrop crop, CropManager mgr)
        {
            // 계절·날씨·마스터리가 곱해진 '지금 속도' 기준. 물 보너스는 곧 마르므로 뺀다
            float speed = mgr != null ? mgr.GrowthSpeedMultiplier : 1f;
            if (speed <= 0.0001f) return "지금은 자라지 않는다";

            float days = crop.RemainingDays / speed;

            if (days < 1f) return $"수확까지 약 {Mathf.Max(1, Mathf.CeilToInt(days * 24f))}시간";
            return $"수확까지 약 {Mathf.Max(1, Mathf.RoundToInt(days))}일";
        }

        private void AppendColored(string hex, string text)
            => _sb.Append("<color=#").Append(hex).Append('>').Append(text).Append("</color>");

        // ════════════════════════════════════════════════════════════
        //  크기 · 위치
        // ════════════════════════════════════════════════════════════

        /// <summary>글자는 배경 안에 여백만큼 띄워서 꽉 채우고, 아이콘은 왼쪽 가운데에 둔다</summary>
        private void SetupLayout()
        {
            if (label != null)
            {
                RectTransform lr = label.rectTransform;
                lr.anchorMin = Vector2.zero;
                lr.anchorMax = Vector2.one;
                lr.pivot = new Vector2(0.5f, 0.5f);
                label.alignment = TextAlignmentOptions.Left;
                label.overflowMode = TextOverflowModes.Overflow;
            }

            if (icon != null)
            {
                RectTransform ir = icon.rectTransform;
                ir.anchorMin = ir.anchorMax = new Vector2(0f, 0.5f);
                ir.pivot = new Vector2(0f, 0.5f);
                ir.anchoredPosition = new Vector2(padding.x, 0f);
                ir.sizeDelta = new Vector2(iconSize, iconSize);
                icon.preserveAspect = true;
            }
        }

        /// <summary>글자 양에 맞춰 배경 크기를 바꾼다</summary>
        private void Resize(string text)
        {
            bool hasIcon = icon != null && icon.enabled;
            float left = padding.x + (hasIcon ? iconSize + iconGap : 0f);
            float textMax = Mathf.Max(20f, maxWidth - left - padding.x);

            Vector2 pref = label.GetPreferredValues(text, textMax, 0f);
            float w = Mathf.Min(pref.x + 2f, textMax);   // +2: 반올림 때문에 마지막 글자가 줄바꿈되는 것 방지
            float h = Mathf.Max(pref.y, hasIcon ? iconSize : 0f);

            RectTransform lr = label.rectTransform;
            lr.offsetMin = new Vector2(left, padding.y);
            lr.offsetMax = new Vector2(-padding.x, -padding.y);

            _rect.sizeDelta = new Vector2(left + w + padding.x, h + padding.y * 2f);
        }

        /// <summary>마우스를 따라다닌다. 화면 오른쪽·아래 가장자리 근처에서는 반대쪽으로 뒤집는다</summary>
        private void FollowMouse()
        {
            if (Mouse.current == null || _canvas == null) return;

            var parent = _rect.parent as RectTransform;
            if (parent == null) return;

            Vector2 screen = Mouse.current.position.ReadValue();
            Camera uiCam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, uiCam, out Vector2 local))
                return;

            bool flipX = screen.x > Screen.width * 0.65f;
            bool flipY = screen.y < Screen.height * 0.35f;

            _rect.pivot = new Vector2(flipX ? 1f : 0f, flipY ? 0f : 1f);

            var offset = new Vector2(
                flipX ? -cursorOffset.x : cursorOffset.x,
                flipY ? -cursorOffset.y : cursorOffset.y);

            // 앵커가 부모 가운데라서, 부모 기준 좌표에서 부모 가운데를 빼면 anchoredPosition 이 된다
            _rect.anchoredPosition = local - parent.rect.center + offset;
        }
    }
