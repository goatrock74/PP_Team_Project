using UnityEngine;
using UnityEngine.UI;
 
namespace KSM._00.Scripts.Items
{
    public class DragGhost : MonoBehaviour
    {
        private static DragGhost _instance;
 
        private Canvas _canvas;
        private RectTransform _rect;
        private Image _image;
 
        public static void Show(Sprite icon, Color tint, Vector2 size, Canvas canvas)
        {
            if (icon == null || canvas == null) return;
 
            DragGhost ghost = Ensure(canvas);
            if (ghost == null) return;
 
            ghost._image.sprite = icon;
            ghost._image.color = new Color(tint.r, tint.g, tint.b, 0.75f);
            ghost._rect.sizeDelta = size;
 
            ghost.gameObject.SetActive(true);
            ghost.transform.SetAsLastSibling();   
        }
 
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
 
 
        private static DragGhost Ensure(Canvas canvas)
        {
            if (_instance != null && _instance._canvas == canvas) return _instance;
 
            if (_instance != null) Destroy(_instance.gameObject);
 
            var go = new GameObject("DragGhost", typeof(RectTransform), typeof(Image), typeof(DragGhost));
            go.transform.SetParent(canvas.transform, false);
 
            _instance = go.GetComponent<DragGhost>();
            _instance._canvas = canvas;
            _instance._rect = (RectTransform)go.transform;
            _instance._image = go.GetComponent<Image>();
 
            _instance._image.raycastTarget = false;  
            _instance._image.preserveAspect = true;
 
            return _instance;
        }
 
        private void Follow(Vector2 screenPosition)
        {
            if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                _rect.position = screenPosition;
                return;
            }
 
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
 