using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

// Attach to the horizontal ScrollRect's Content.
[DefaultExecutionOrder(100)]
public class Swipe_menu : MonoBehaviour
{
    [SerializeField] private GameObject scrollbar;
    [SerializeField, Min(0.1f)] private float snapSpeed = 10f;
    [SerializeField, Range(0.5f, 1f)] private float sideScale = 0.8f;
    [SerializeField] private Image[] selectionImages = new Image[0];

    private readonly List<RectTransform> cards = new List<RectTransform>();
    private ScrollRect scrollRect;
    private RectTransform content;
    private GridLayoutGroup grid;
    private float viewportWidth = -1f;
    private int layoutSignature;
    private float inputUntil;

    private void Awake()
    {
        content = transform as RectTransform;
        scrollRect = GetComponentInParent<ScrollRect>();
        grid = GetComponent<GridLayoutGroup>();
        if (scrollRect != null && scrollbar != null)
            scrollRect.horizontalScrollbar = scrollbar.GetComponent<Scrollbar>();
    }

    private void OnEnable()
    {
        viewportWidth = -1f;
        inputUntil = 0f;
        SyncSelectionImages();
    }

    private void OnDisable()
    {
        foreach (var card in cards)
            if (card != null) card.localScale = Vector3.one;
        if (scrollRect != null) scrollRect.StopMovement();
        foreach (var selectionImage in selectionImages)
            if (selectionImage != null) selectionImage.gameObject.SetActive(false);
    }

    private void SyncSelectionImages()
    {
        // FishShopSlot owns selection and writes alpha, even while its Image is inactive.
        // Mirror that state without treating the centred carousel card as selected.
        foreach (var selectionImage in selectionImages)
        {
            if (selectionImage == null) continue;
            bool selected = selectionImage.color.a > 0f;
            if (selectionImage.gameObject.activeSelf != selected)
                selectionImage.gameObject.SetActive(selected);
        }
    }

    private void LateUpdate()
    {
        SyncSelectionImages();
        if (scrollRect == null || content == null || scrollRect.viewport == null) return;

        cards.Clear();
        int signature = 17;
        for (int i = 0; i < content.childCount; i++)
        {
            var card = content.GetChild(i) as RectTransform;
            if (card == null || !card.gameObject.activeInHierarchy || !card.TryGetComponent<Button>(out _)) continue;
            cards.Add(card);
            unchecked { signature = signature * 31 + card.GetInstanceID(); }
        }
        if (cards.Count == 0) return;

        float width = scrollRect.viewport.rect.width;
        if (width <= 0f) return;
        if (!Mathf.Approximately(viewportWidth, width) || layoutSignature != signature)
        {
            // End padding lets both the first and last card reach the centre.
            if (grid != null)
            {
                int padding = Mathf.Max(0, Mathf.RoundToInt((width - grid.cellSize.x) * 0.5f));
                grid.padding.left = padding;
                grid.padding.right = padding;
            }
            viewportWidth = width;
            layoutSignature = signature;
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            scrollRect.StopMovement();
        }

        float range = Mathf.Max(0f, content.rect.width - width);
        float current = Mathf.Clamp01(scrollRect.horizontalNormalizedPosition);
        int nearest = 0;
        float nearestDistance = float.MaxValue;
        for (int i = 0; i < cards.Count; i++)
        {
            float distance = Mathf.Abs(current - PositionFor(cards[i], width, range));
            if (distance < nearestDistance) { nearestDistance = distance; nearest = i; }
        }

        bool pressed = (Mouse.current != null && Mouse.current.leftButton.isPressed) ||
            (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed);
        bool wheel = Mouse.current != null && Mouse.current.scroll.ReadValue().sqrMagnitude > 0f;
        if (pressed || wheel) inputUntil = Time.unscaledTime + 0.15f;

        float blend = 1f - Mathf.Exp(-snapSpeed * Time.unscaledDeltaTime);
        if (!pressed && Time.unscaledTime >= inputUntil)
        {
            scrollRect.StopMovement();
            float target = PositionFor(cards[nearest], width, range);
            float next = Mathf.Lerp(current, target, blend);
            scrollRect.horizontalNormalizedPosition = Mathf.Abs(next - target) < 0.0001f ? target : next;
        }

        for (int i = 0; i < cards.Count; i++)
        {
            float scale = i == nearest ? 1f : sideScale;
            cards[i].localScale = Vector3.Lerp(cards[i].localScale, new Vector3(scale, scale, 1f), blend);
        }
    }

    private float PositionFor(RectTransform card, float width, float range)
    {
        if (range <= 0.01f) return 0f;
        float centre = card.anchoredPosition.x + (0.5f - card.pivot.x) * card.rect.width;
        return Mathf.Clamp01((centre - width * 0.5f) / range);
    }
}
