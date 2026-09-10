using UnityEngine;
using UnityEngine.UI;
 
namespace KSM._00.Scripts.Items
{
    /// <summary>
    /// 드래그하는 동안 커서를 따라다니는 아이템 아이콘.
    ///
    /// 씬에 미리 만들어둘 필요가 없다. 처음 필요할 때 캔버스 맨 위에 자동으로 생긴다.
    ///
    /// ★ Raycast Target 을 반드시 꺼둔다. 켜져 있으면 이 아이콘이 커서 밑을 가려서
    ///   드롭 대상 슬롯이 마우스를 못 받고, 어디에 놓아도 아무 일도 안 일어난다.
    /// </summary>
    public class DragGhost : MonoBehaviour
    {
        private static DragGhost _instance;
 
        private Canvas _canvas;
        private RectTransform _rect;
        private Image _image;
 
        // ════════════════════════════════════════════════════════════
 
        /// <summary>드래그 시작 — 아이콘을 띄운다</summary>
        public static void Show(Sprite icon, Color tint, Vector2 size, Canvas canvas)
        {
            if (icon == null || canvas == null) return;
 
            DragGhost ghost = Ensure(canvas);
            if (ghost == null) return;
 
            ghost._image.sprite = icon;
            ghost._image.color = new Color(tint.r, tint.g, tint.b, 0.75f);
            ghost._rect.sizeDelta = size;
 
            ghost.gameObject.SetActive(true);
            ghost.transform.SetAsLastSibling();   // 항상 맨 위에
        }
 
        /// <summary>커서 위치로 옮긴다</summary>
        public static void Move(Vector2 screenPosition)
        {
            if (_instance == null || !_instance.gameObject.activeSelf) return;
 
            _instance.Follow(screenPosition);
        }
 
        public static void Hide()
        {
            if (_instance == null) return;
 
            _instance.gameObject.SetActive(false);
        }
 
        // ════════════════════════════════════════════════════════════
 
        private static DragGhost Ensure(Canvas canvas)
        {
            // 캔버스가 바뀌었으면 새로 만든다 (씬 전환 등)
            if (_instance != null && _instance._canvas == canvas) return _instance;
 
            if (_instance != null) Destroy(_instance.gameObject);
 
            var go = new GameObject("DragGhost", typeof(RectTransform), typeof(Image), typeof(DragGhost));
            go.transform.SetParent(canvas.transform, false);
 
            _instance = go.GetComponent<DragGhost>();
            _instance._canvas = canvas;
            _instance._rect = (RectTransform)go.transform;
            _instance._image = go.GetComponent<Image>();
 
            _instance._image.raycastTarget = false;   // ★ 드롭 대상을 가리면 안 된다
            _instance._image.preserveAspect = true;
 
            return _instance;
        }
 
        private void Follow(Vector2 screenPosition)
        {
            // Overlay 캔버스는 화면 좌표를 그대로 쓸 수 있다
            if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                _rect.position = screenPosition;
                return;
            }
 
            // Camera / World 캔버스는 변환이 필요하다
            bool ok = RectTransformUtility.ScreenPointToWorldPointInRectangle(
                (RectTransform)_canvas.transform,
                screenPosition,
                _canvas.worldCamera,
                out Vector3 world);
 
            if (ok) _rect.position = world;
        }
 
        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
 