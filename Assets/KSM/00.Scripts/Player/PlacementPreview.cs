using System.Collections.Generic;
using UnityEngine;
 
namespace KSM._00.Scripts.Crop
{
    /// <summary>
    /// 심기 미리보기. 마우스가 가리키는 자리에 작물 크기만큼 칸을 그린다.
    /// 심을 수 있으면 초록, 없으면 빨강.
    ///
    /// 씬에 빈 GameObject 하나 만들어서 붙이면 끝. 스프라이트를 안 넣으면
    /// 흰 사각형을 런타임에 만들어 쓴다.
    /// </summary>
    public class PlacementPreview : MonoBehaviour
    {
        [Header("모양")]
        [Tooltip("비워두면 흰 사각형을 자동 생성한다")]
        [SerializeField] private Sprite cellSprite;
 
        [SerializeField] private Color okColor = new Color(0.35f, 1f, 0.4f, 0.45f);
        [SerializeField] private Color badColor = new Color(1f, 0.3f, 0.3f, 0.45f);
 
        [Header("그리기 순서")]
        [Tooltip("비워두면 Default 레이어를 쓴다. 작물용 레이어를 따로 만들었다면 그 이름을 적을 것")]
        [SerializeField] private string sortingLayerName = "";
 
        [Tooltip("작물보다 항상 위에 뜨도록 크게 잡는다")]
        [SerializeField] private int sortingOrder = 30000;
 
        private readonly List<SpriteRenderer> _cells = new();
        private Sprite _runtimeSprite;
        private int _visibleCount;
 
        /// <summary>
        /// origin(좌하단 칸)부터 size 만큼 칸을 그린다.
        /// </summary>
        public void Show(Vector3Int origin, Vector2Int size, bool ok)
        {
            CropManager mgr = CropManager.Instance;
            if (mgr == null) { Hide(); return; }
 
            int need = Mathf.Max(1, size.x) * Mathf.Max(1, size.y);
            EnsureCells(need);
 
            Color color = ok ? okColor : badColor;
            Vector3 cellScale = mgr.CellSize;
            int i = 0;
 
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    var cell = new Vector3Int(origin.x + x, origin.y + y, origin.z);
                    SpriteRenderer sr = _cells[i++];
 
                    sr.transform.position = mgr.CellToWorldCenter(cell);
                    sr.transform.localScale = new Vector3(cellScale.x, cellScale.y, 1f);
                    sr.color = color;
                    sr.enabled = true;
                }
            }
 
            // 이전에 더 큰 작물을 그렸다면 남는 칸은 끈다
            for (; i < _cells.Count; i++) _cells[i].enabled = false;
 
            _visibleCount = need;
        }
 
        public void Hide()
        {
            if (_visibleCount == 0) return;
 
            foreach (SpriteRenderer sr in _cells) sr.enabled = false;
            _visibleCount = 0;
        }
 
        // ════════════════════════════════════════════════════════════
 
        private void EnsureCells(int count)
        {
            while (_cells.Count < count)
            {
                var go = new GameObject($"PreviewCell_{_cells.Count:00}");
                go.transform.SetParent(transform, false);
 
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = GetSprite();
 
                // 존재하지 않는 레이어 이름을 넣으면 에러가 나므로 비어있으면 건드리지 않는다
                if (!string.IsNullOrWhiteSpace(sortingLayerName))
                    sr.sortingLayerName = sortingLayerName;
 
                sr.sortingOrder = sortingOrder;
                sr.enabled = false;
 
                _cells.Add(sr);
            }
        }
 
        private Sprite GetSprite()
        {
            if (cellSprite != null) return cellSprite;
            if (_runtimeSprite != null) return _runtimeSprite;
 
            // 4x4 흰 텍스처로 정확히 1x1 유닛짜리 스프라이트를 만든다
            const int Size = 4;
            var tex = new Texture2D(Size, Size) { filterMode = FilterMode.Point };
 
            var pixels = new Color[Size * Size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
 
            _runtimeSprite = Sprite.Create(
                tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
 
            return _runtimeSprite;
        }
 
        private void OnDisable() => Hide();
    }
}
 