using UnityEngine;
using KSM._00.Scripts.Crop;
using KSM._00.Scripts.Items;
    /// <summary>
    /// 물을 안 준(마른 땅에 심긴) 작물 위에 물방울 아이콘을 띄운다.
    /// 물을 주면 쏙 들어가고, 땅이 다시 마르면 또 뜬다.
    /// 다 자란 작물과 시든 작물에는 안 뜬다.
    ///
    /// 붙일 곳: 작물 프리팹 (GrowCrop · CropFX 옆)
    /// ★ 아이콘 그림을 안 넣으면 코드로 만든 픽셀 물방울을 쓴다
    /// </summary>
    [RequireComponent(typeof(GrowCrop))]
    public class CropWaterIcon : MonoBehaviour
    {
        [Header("모양")]
        [Tooltip("비우면 코드로 만든 픽셀 물방울을 쓴다")]
        [SerializeField] private Sprite icon;

        [Tooltip("직접 넣은 그림에 색을 입힐 때만 바꿀 것. 흰색이면 원래 색 그대로")]
        [SerializeField] private Color tint = Color.white;

        [Tooltip("아이콘 크기 (월드 단위)")]
        [SerializeField, Min(0.05f)] private float size = 0.3f;

        [Tooltip("작물 그림 꼭대기에서 이만큼 위에 뜬다")]
        [SerializeField] private float height = 0.08f;

        [Header("움직임")]
        [Tooltip("위아래로 둥실거리는 폭")]
        [SerializeField, Min(0f)] private float bob = 0.05f;

        [SerializeField, Min(0f)] private float bobSpeed = 3.2f;

        [Tooltip("나타나고 사라지는 데 걸리는 시간(초)")]
        [SerializeField, Min(0.01f)] private float popTime = 0.2f;

        [Header("언제 보일지")]
        [Tooltip("켜면 물뿌리개를 들고 있을 때만 보인다. 밭이 넓어서 아이콘이 너무 많을 때 켤 것")]
        [SerializeField] private bool onlyWhileHoldingCan;

        private GrowCrop _crop;
        private SpriteRenderer _renderer;     // 작물 그림
        private SpriteRenderer _iconRenderer; // 물방울 (처음 필요할 때 만든다)

        private float _k;                     // 0 = 숨김, 1 = 다 나옴
        private float _phase;

        private static Sprite s_pixelDrop;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_pixelDrop = null;

        private void Awake()
        {
            _crop = GetComponent<GrowCrop>();
            _renderer = GetComponent<SpriteRenderer>();

            // 작물마다 둥실거리는 박자가 다르게
            _phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void OnDisable()
        {
            _k = 0f;
            if (_iconRenderer != null) _iconRenderer.enabled = false;
        }

        private void LateUpdate()
        {
            bool want = _renderer.isVisible && NeedsWater();

            // 숨겨진 채로 가만히 있으면 아무것도 안 한다
            if (!want && _k <= 0f) return;

            _k = Mathf.MoveTowards(_k, want ? 1f : 0f, Time.deltaTime / popTime);

            if (_iconRenderer == null) CreateIcon();

            _iconRenderer.enabled = _k > 0f;
            if (_k > 0f) Place();
        }

        private bool NeedsWater()
        {
            if (_crop.Data == null || _crop.IsGrowFinished || _crop.IsWilted) return false;

            CropManager mgr = CropManager.Instance;
            if (mgr == null || mgr.IsWet(_crop.OriginCell)) return false;

            if (onlyWhileHoldingCan)
            {
                PlayerInventory inv = PlayerInventory.Instance;
                if (inv == null || !(inv.HeldItem is WateringCanSO)) return false;
            }

            return true;
        }

        private void CreateIcon()
        {
            var go = new GameObject("WaterIcon");
            go.transform.SetParent(transform, false);

            _iconRenderer = go.AddComponent<SpriteRenderer>();
            _iconRenderer.sprite = icon != null ? icon : PixelDrop();
            _iconRenderer.color = tint;
            _iconRenderer.sortingLayerID = _renderer.sortingLayerID;
            _iconRenderer.sortingOrder = _renderer.sortingOrder + 2;   // 작물·반짝이보다 위
        }

        /// <summary>작물 그림 꼭대기 위에 띄운다. 작물이 흔들리거나 커져도 아이콘은 똑바로, 같은 크기로</summary>
        private void Place()
        {
            Sprite sprite = _iconRenderer.sprite;
            if (sprite == null) return;

            Transform t = _iconRenderer.transform;

            float s = EaseOutBack(_k);
            float world = size * s;

            Bounds b = _renderer.bounds;
            float y = b.max.y + height + world * 0.5f + Mathf.Sin(Time.time * bobSpeed + _phase) * bob;

            t.position = new Vector3(b.center.x, y, transform.position.z);
            t.rotation = Quaternion.identity;

            Vector3 native = sprite.bounds.size;
            float unit = world / Mathf.Max(0.0001f, Mathf.Max(native.x, native.y));

            // 부모(작물)의 크기 변화는 빼고 월드 기준 크기로 맞춘다
            Vector3 parent = transform.lossyScale;
            t.localScale = new Vector3(
                unit / Mathf.Max(0.0001f, Mathf.Abs(parent.x)),
                unit / Mathf.Max(0.0001f, Mathf.Abs(parent.y)),
                1f);
        }

        /// <summary>살짝 넘쳤다가 제자리로 — '뿅' 하고 나오는 느낌</summary>
        private static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;

            float p = x - 1f;
            return 1f + c3 * p * p * p + c1 * p * p;
        }

        /// <summary>9x11 픽셀 물방울을 코드로 만든다 (한 번만 만들어서 모두 같이 쓴다)</summary>
        private static Sprite PixelDrop()
        {
            if (s_pixelDrop != null) return s_pixelDrop;

            string[] rows =
            {
                "....o....",
                "...o#o...",
                "...o#o...",
                "..o###o..",
                ".o##w##o.",
                ".o#w###o.",
                "o##w####o",
                "o#######o",
                "o######do",
                ".o###ddo.",
                "..ooooo..",
            };

            int w = rows[0].Length;
            int h = rows.Length;

            var outline = new Color32(30, 70, 140, 255);
            var water = new Color32(95, 175, 255, 255);
            var shade = new Color32(60, 130, 215, 255);
            var shine = new Color32(235, 248, 255, 255);
            var clear = new Color32(0, 0, 0, 0);

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "PixelDrop",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                string row = rows[h - 1 - y];   // 텍스처는 아래 줄부터 채운다

                for (int x = 0; x < w; x++)
                {
                    pixels[y * w + x] = row[x] switch
                    {
                        'o' => outline,
                        '#' => water,
                        'd' => shade,
                        'w' => shine,
                        _ => clear,
                    };
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            s_pixelDrop = Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), Mathf.Max(w, h));
            s_pixelDrop.name = "PixelDrop";

            return s_pixelDrop;
        }
    }
