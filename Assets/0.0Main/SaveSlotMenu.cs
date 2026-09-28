using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Attached to the MenuManager in 0.0MainMenu_F. Uses the menu's own font.
public class SaveSlotMenu : MonoBehaviour
{
    private GameObject panel;
    private RectTransform content;
    private TMP_Text status;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Sprite shopPanelSprite;
    [SerializeField] private Sprite shopButtonSprite;
    private string pendingDelete;
    private readonly Color ink = new Color32(89, 53, 31, 255);
    private readonly Color paper = new Color32(255, 213, 139, 255);
    public void Open()
    {
        if (panel == null) Build();
        panel.SetActive(true);
        pendingDelete = null;
        Refresh();
    }
    private void Build()
    {
        var source = FindFirstObjectByType<TextMeshProUGUI>(FindObjectsInactive.Include);
        if (font == null) font = source != null ? source.font : TMP_Settings.defaultFontAsset;
        var root = new GameObject("Save Slots Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1000, 700); scaler.matchWidthOrHeight = .5f;
        panel = Box(root.transform, "Save Slots", new Color32(20, 24, 36, 205), Vector2.zero, Vector2.one).gameObject;
        var frame = Box(panel.transform, "Shop Wood Frame", Color.white, new Vector2(.055f, .06f), new Vector2(.945f, .94f));
        ApplyShopSprite(frame, shopPanelSprite);
        var body = Box(frame, "Layout", Color.clear, new Vector2(.035f, .045f), new Vector2(.965f, .955f));
        Label(body, "농장 기록", new Vector2(.1f, .88f), new Vector2(.9f, .98f), 32);
        Button(body, "X", new Vector2(.91f, .90f), new Vector2(.99f, .99f), () => panel.SetActive(false));
        var viewport = Box(body, "Viewport", new Color32(77, 49, 38, 65), new Vector2(.025f, .27f), new Vector2(.975f, .86f));
        viewport.gameObject.AddComponent<RectMask2D>();
        content = Box(viewport, "Slot Rows", Color.clear, Vector2.zero, Vector2.one);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1);
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 10; layout.padding = new RectOffset(8, 8, 8, 8); layout.childControlHeight = true; layout.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
        Button(body, "NEW", new Vector2(.04f, .10f), new Vector2(.45f, .23f), () => Run(() => { SaveSlotStore.Create(); Refresh(); }));
        Button(body, "BACK", new Vector2(.55f, .10f), new Vector2(.96f, .23f), () => panel.SetActive(false));
        status = Label(body, "", new Vector2(.04f, .015f), new Vector2(.96f, .09f), 18);
    }
    private void Refresh()
    {
        foreach (Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        Run(() =>
        {
            var slots = SaveSlotStore.List();
            status.text = slots.Count == 0 ? "NEW를 눌러 새 농장을 만드세요." : "저장 줄을 누르면 이어서 시작합니다.";
            string[] seasons = { "봄", "여름", "가을", "겨울" };
            foreach (var slot in slots)
            {
                var row = Box(content, "Slot " + slot.id, Color.clear, Vector2.zero, Vector2.one);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 104;
                int minutes = Mathf.FloorToInt(slot.dayFraction * 1440);
                var load = Button(row, slot.unavailable ? "읽을 수 없는 저장 파일\n오른쪽 버튼으로 삭제할 수 있습니다." : "", new Vector2(.01f, .02f), new Vector2(.87f, .98f), () => Run(() => SaveGameSession.Play(slot.id)));
                if (!slot.unavailable)
                {
                    // Match the in-game Day counter; the game does not track calendar years.
                    var date = Label(load.transform, $"{slot.day}일차 · {seasons[((slot.day - 1) / 5) % 4]}", new Vector2(.06f, .48f), new Vector2(.62f, .85f), 24);
                    date.alignment = TextAlignmentOptions.MidlineLeft;
                    var time = Label(load.transform, $"시간 {minutes / 60:00}:{minutes % 60:00}   ·   플레이 {(int)(slot.playSeconds / 3600):00}:{(int)(slot.playSeconds / 60) % 60:00}", new Vector2(.06f, .15f), new Vector2(.64f, .46f), 17);
                    time.alignment = TextAlignmentOptions.MidlineLeft;
                    var gold = Label(load.transform, $"{slot.money:N0}\n골드", new Vector2(.66f, .18f), new Vector2(.94f, .82f), 23);
                    gold.alignment = TextAlignmentOptions.MidlineRight;
                }
                load.interactable = !slot.unavailable;
                var delete = Button(row, pendingDelete == slot.id ? "삭제?" : "", new Vector2(.9f, .15f), new Vector2(.99f, .85f), () => Run(() =>
                {
                    if (pendingDelete == slot.id) { SaveSlotStore.Delete(slot.id); pendingDelete = null; Refresh(); }
                    else { pendingDelete = slot.id; Refresh(); status.text = "이 슬롯을 지우려면 삭제? 버튼을 한 번 더 누르세요."; }
                }));
                if (pendingDelete != slot.id)
                {
                    Box(delete.transform, "Bin", ink, new Vector2(.3f, .22f), new Vector2(.7f, .67f)).GetComponent<Image>().raycastTarget = false;
                    Box(delete.transform, "Lid", ink, new Vector2(.24f, .7f), new Vector2(.76f, .77f)).GetComponent<Image>().raycastTarget = false;
                    Box(delete.transform, "Handle", ink, new Vector2(.4f, .78f), new Vector2(.6f, .84f)).GetComponent<Image>().raycastTarget = false;
                }
            }
        });
    }
    private void Run(Action action) { try { action(); } catch (Exception e) { status.text = "처리 실패: " + e.Message; Debug.LogException(e); } }
    private RectTransform Box(Transform parent, string name, Color color, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform; rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<TextMeshProUGUI>().textWrappingMode = TextWrappingModes.NoWrap;
        go.GetComponent<Image>().color = color; return rt;
    }
    private TMP_Text Label(Transform parent, string text, Vector2 min, Vector2 max, int size)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform; rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var label = go.GetComponent<TextMeshProUGUI>(); label.font = font; label.text = text; label.fontSize = size; label.color = ink; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false; label.enableAutoSizing = true; label.fontSizeMin = 12; label.fontSizeMax = size; return label;
    }
    private Button Button(Transform parent, string text, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action)
    {
        var rt = Box(parent, string.IsNullOrEmpty(text) ? "Slot Button" : text, Color.white, min, max);
        ApplyShopSprite(rt, shopButtonSprite != null ? shopButtonSprite : shopPanelSprite);
        var button = rt.gameObject.AddComponent<Button>(); button.targetGraphic = rt.GetComponent<Image>(); button.onClick.AddListener(action);
        var colors = button.colors;
        colors.highlightedColor = new Color(1f, .94f, .8f);
        colors.pressedColor = new Color(.75f, .66f, .56f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        Label(rt, text, new Vector2(.03f, .04f), new Vector2(.97f, .96f), 24); return button;
    }
    private void ApplyShopSprite(RectTransform target, Sprite sprite)
    {
        var image = target.GetComponent<Image>();
        if (sprite == null) { image.color = paper; return; }
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 3;
    }
}
