using System.Collections.Generic;
using UnityEngine;
using KSM._00.Scripts.Crop;

    public class ToolFX : MonoBehaviour
    {
        [Header("소리 (SoundManager 로 재생. 비워두면 그 소리만 안 난다)")]
        [Tooltip("괭이로 땅을 갈았을 때")]
        [SerializeField] private AudioClip hoeSfx;

        [Tooltip("물을 줬을 때")]
        [SerializeField] private AudioClip waterSfx;

        [Tooltip("도끼로 나무를 찍었을 때")]
        [SerializeField] private AudioClip chopSfx;

        [Tooltip("나무가 쓰러질 때 (찍는 소리와 같이 난다)")]
        [SerializeField] private AudioClip fellSfx;

        [Tooltip("낫으로 풀·채집물을 벴을 때")]
        [SerializeField] private AudioClip scytheSfx;

        [Tooltip("씨앗을 심었을 때")]
        [SerializeField] private AudioClip plantSfx;

        [Tooltip("제거 모드로 작물을 뽑았을 때")]
        [SerializeField] private AudioClip removeSfx;

        [Header("크기·세기 (타일 한 칸 기준)")]
        [Tooltip("조각 하나의 크기. 0.06 = 한 칸의 6%")]
        [SerializeField, Range(0.02f, 0.3f)] private float pieceSize = 0.06f;

        [Tooltip("튀는 세기 배수. 크면 멀리 튄다")]
        [SerializeField, Range(0.2f, 3f)] private float power = 1f;

        [Header("개수")]
        [SerializeField, Range(0, 30)] private int dirtCount = 9;
        [SerializeField, Range(0, 30)] private int waterCount = 10;
        [SerializeField, Range(0, 30)] private int chipCount = 6;
        [SerializeField, Range(0, 40)] private int fellLeafCount = 16;
        [SerializeField, Range(0, 30)] private int grassCount = 7;

        [Header("색")]
        [SerializeField] private Color[] dirtColors =
        {
            new Color32(150, 95, 60, 255), new Color32(120, 75, 48, 255), new Color32(178, 118, 78, 255),
        };

        [SerializeField] private Color[] waterColors =
        {
            new Color32(140, 200, 255, 255), new Color32(215, 240, 255, 255), new Color32(90, 165, 240, 255),
        };

        [SerializeField] private Color[] woodColors =
        {
            new Color32(200, 150, 95, 255), new Color32(160, 110, 65, 255), new Color32(232, 196, 138, 255),
        };

        [SerializeField] private Color[] leafColors =
        {
            new Color32(95, 165, 70, 255), new Color32(132, 196, 86, 255), new Color32(70, 128, 55, 255),
        };

        [Header("그리는 순서")]
        [Tooltip("끄면 플레이어 그림을 따라간다 (추천). 켜면 아래 값을 쓴다")]
        [SerializeField] private bool customSorting;

        [SerializeField] private string sortingLayer = "Default";
        [SerializeField] private int sortingOrder = 200;

        [Tooltip("한꺼번에 떠 있을 수 있는 조각 수. 넘으면 가장 오래된 것부터 재사용한다")]
        [SerializeField, Range(32, 512)] private int maxPieces = 256;

        // ════════════════════════════════════════════════════════════

        private struct Piece
        {
            public Transform tr;
            public SpriteRenderer sr;
            public Vector2 ground;   // 땅 위의 위치
            public Vector2 vel;      // 땅 위에서 움직이는 속도
            public float height;     // 땅에서 뜬 높이 (위에서 내려다보는 게임이라 가짜 높이)
            public float vz;         // 위아래 속도
            public float gravity;
            public float life;
            public float maxLife;
            public float size;
            public float wobble;     // 나뭇잎처럼 좌우로 살랑이는 폭 (0 이면 없음)
            public float phase;
            public Color color;
            public bool alive;
        }

        private Piece[] _pieces;
        private int _next;

        private bool _sortingReady;
        private int _layerId;
        private int _order;

        private static ToolFX s_instance;
        private static Sprite s_pixel;
        private static readonly List<Collider2D> s_hits = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_instance = null;
            s_pixel = null;
        }

        /// <summary>씬에 있으면 그걸, 없으면 하나 만들어서 쓴다</summary>
        private static ToolFX Instance
        {
            get
            {
                if (s_instance != null) return s_instance;

                s_instance = FindFirstObjectByType<ToolFX>();
                if (s_instance == null) s_instance = new GameObject("[ToolFX]").AddComponent<ToolFX>();

                return s_instance;
            }
        }

        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(this);
                return;
            }

            s_instance = this;
        }

        private void OnDestroy()
        {
            if (s_instance == this) s_instance = null;
        }

        // ════════════════════════════════════════════════════════════
        //  PlayerInteractor 가 부르는 곳
        // ════════════════════════════════════════════════════════════

        /// <summary>도구를 쓰기 직전 상태. 어느 칸이 바뀌었는지, 무엇을 쳤는지 알아내는 데 쓴다</summary>
        public sealed class Shot
        {
            internal ToolSO tool;
            internal ToolUseContext ctx;
            internal readonly List<Vector3Int> cells = new();
            internal readonly List<Vector3> points = new();
            internal Collider2D hit;
            internal TreeNode tree;
            internal Vector3 hitPoint;
            internal Bounds canopy;
            internal bool hasCanopy;
        }

        /// <summary>도구를 쓰기 <b>직전</b>에 부른다</summary>
        public static Shot BeforeUse(ToolSO tool, in ToolUseContext ctx)
        {
            var shot = new Shot { tool = tool, ctx = ctx };
            CropManager farm = ctx.farm;

            switch (tool)
            {
                case HoeSO hoe when farm != null:
                    // 아직 안 갈린 칸만 기억해 둔다. 쓰고 나서 갈린 칸에만 흙을 튀긴다
                    foreach (Vector3Int c in hoe.GetCells(ctx.cell))
                        if (farm.GetGroundTile(c) != hoe.tilledTile) shot.cells.Add(c);
                    break;

                case WateringCanSO can when farm != null:
                    foreach (Vector3Int c in can.GetCells(ctx.cell))
                        if (farm.GetGroundTile(c) != null) shot.cells.Add(c);
                    break;

                case AxeSO axe:
                    CaptureAxeTarget(axe, in ctx, shot);
                    break;

                case ScytheSO scythe:
                    CaptureForageTargets(scythe, in ctx, shot);
                    break;
            }

            return shot;
        }

        /// <summary>도구를 쓴 <b>직후</b>에 부른다. used = 도구가 실제로 뭔가 했는가</summary>
        public static void AfterUse(Shot shot, bool used)
        {
            if (shot == null || shot.tool == null) return;

            ToolFX fx = Instance;
            CropManager farm = shot.ctx.farm;

            switch (shot.tool)
            {
                case HoeSO hoe:
                    if (!used || farm == null) return;

                    // ★ 소리는 반복문 밖에서 한 번만. 3x3 으로 갈아도 한 번만 난다
                    fx.Sfx(fx.hoeSfx);

                    foreach (Vector3Int c in shot.cells)
                        if (farm.GetGroundTile(c) == hoe.tilledTile) fx.PlayDirt(farm.CellToWorldCenter(c), 1f);
                    break;

                case WateringCanSO _:
                    fx.Sfx(fx.waterSfx);

                    // 물은 이미 젖은 땅이나 밭이 아닌 곳에 부어도 튄다 (부었다는 손맛)
                    if (farm == null || shot.cells.Count == 0)
                    {
                        fx.PlayWater(shot.ctx.worldPoint);
                        return;
                    }

                    foreach (Vector3Int c in shot.cells)
                        fx.PlayWater(farm.CellToWorldCenter(c));
                    break;

                case AxeSO _:
                    if (!used || shot.hit == null) return;

                    Vector2 dir = shot.hitPoint - shot.ctx.userPosition;
                    if (dir.sqrMagnitude < 0.0001f) dir = shot.ctx.facing;

                    // 쓰러졌는지: 그루터기가 됐거나, 안 다시 자라는 나무라 판정이 꺼졌거나
                    bool felled = shot.tree != null && (shot.tree.IsStump || !shot.hit.enabled);

                    fx.Sfx(fx.chopSfx);
                    if (felled) fx.Sfx(fx.fellSfx);

                    fx.PlayChips(shot.hitPoint, dir.normalized, felled);
                    if (felled && shot.hasCanopy) fx.PlayCanopyLeaves(shot.canopy);
                    break;

                case ScytheSO _:
                    if (!used) return;

                    fx.Sfx(fx.scytheSfx);

                    foreach (Vector3 p in shot.points)
                        fx.PlayGrass(p, 1f);
                    break;
            }
        }

        /// <summary>작물을 뽑았을 때</summary>
        public static void CropRemoved(Vector3 at)
        {
            ToolFX fx = Instance;

            fx.Sfx(fx.removeSfx);
            fx.PlayDirt(at, 1.2f);
            fx.PlayGrass(at, 0.6f);
        }

        /// <summary>씨앗을 심었을 때</summary>
        public static void Planted(Vector3 at)
        {
            ToolFX fx = Instance;

            fx.Sfx(fx.plantSfx);
            fx.PlayDirt(at, 0.45f);
        }

        /// <summary>친구의 SoundManager 로 효과음을 튼다. 클립이 비어 있거나 매니저가 없으면 조용히 넘어간다</summary>
        private void Sfx(AudioClip clip)
        {
            if (clip == null || SoundManager.Instance == null) return;

            SoundManager.Instance.PlaySFX(clip);
        }

        // ════════════════════════════════════════════════════════════
        //  대상 찾기 (도끼·긴낫과 같은 규칙)
        // ════════════════════════════════════════════════════════════

        private static void OverlapHitBox(ToolSO tool, in ToolUseContext ctx)
        {
            var filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = ctx.targetLayer,
                useTriggers = true,
            };

            s_hits.Clear();
            Physics2D.OverlapBox(tool.GetHitBoxCenter(in ctx), tool.hitBoxSize, 0f, filter, s_hits);
        }

        /// <summary>AxeSO 와 똑같이 고른다: 벨 수 있는 것 중 가장 가까운 것 → 없으면 아무거나 가장 가까운 것</summary>
        private static void CaptureAxeTarget(AxeSO axe, in ToolUseContext ctx, Shot shot)
        {
            OverlapHitBox(axe, in ctx);

            Vector2 center = axe.GetHitBoxCenter(in ctx);
            Collider2D best = PickNearest(axe, center, true) ?? PickNearest(axe, center, false);
            if (best == null) return;

            shot.hit = best;
            shot.tree = best.GetComponentInParent<TreeNode>();

            // 나무 콜라이더에서 플레이어 쪽 면 = 도끼날이 닿는 곳
            shot.hitPoint = best.bounds.ClosestPoint(ctx.userPosition);

            if (shot.tree == null) return;

            SpriteRenderer body = shot.tree.GetComponentInChildren<SpriteRenderer>();
            if (body == null) return;

            shot.canopy = body.bounds;
            shot.hasCanopy = true;
        }

        private static Collider2D PickNearest(AxeSO axe, Vector2 center, bool choppableOnly)
        {
            Collider2D best = null;
            float bestSqr = float.MaxValue;

            foreach (Collider2D c in s_hits)
            {
                if (c == null) continue;

                IChoppable target = c.GetComponentInParent<IChoppable>();
                if (target == null) continue;
                if (choppableOnly && !target.CanChop(axe)) continue;

                float sqr = ((Vector2)c.bounds.ClosestPoint(center) - center).sqrMagnitude;
                if (sqr >= bestSqr) continue;

                bestSqr = sqr;
                best = c;
            }

            return best;
        }

        private static void CaptureForageTargets(ScytheSO scythe, in ToolUseContext ctx, Shot shot)
        {
            OverlapHitBox(scythe, in ctx);

            var seen = new List<IForageable>();

            foreach (Collider2D c in s_hits)
            {
                if (c == null) continue;

                IForageable target = c.GetComponentInParent<IForageable>();
                if (target == null || seen.Contains(target) || !target.CanForage(scythe)) continue;

                seen.Add(target);

                // 그림의 가운데에서 튀게 한다. 그림이 없으면 콜라이더 가운데
                var comp = target as Component;
                SpriteRenderer sr = comp != null ? comp.GetComponentInChildren<SpriteRenderer>() : null;
                shot.points.Add(sr != null ? sr.bounds.center : c.bounds.center);
            }
        }

        // ════════════════════════════════════════════════════════════
        //  효과 종류
        // ════════════════════════════════════════════════════════════

        private void PlayDirt(Vector3 at, float amount)
        {
            int count = Mathf.RoundToInt(dirtCount * amount);
            Burst(at, dirtColors, count, speed: 1.3f * amount, up: 2.2f, gravity: 9f, life: 0.55f, size: pieceSize);
        }

        private void PlayWater(Vector3 at)
        {
            Burst(at, waterColors, waterCount, speed: 1.1f, up: 2.6f, gravity: 11f, life: 0.45f, size: pieceSize * 0.8f);
        }

        private void PlayChips(Vector3 at, Vector2 away, bool felled)
        {
            int count = felled ? chipCount * 2 : chipCount;

            // 친 반대쪽으로 부채꼴(120도)로 튄다. 도끼날이 닿는 허리 높이에서 시작
            Burst(at, woodColors, count, speed: felled ? 2.2f : 1.7f, up: 2.0f, gravity: 9f, life: 0.7f,
                  size: pieceSize * 1.15f, dir: away, spreadDeg: 120f, startHeight: 0.35f);
        }

        private void PlayCanopyLeaves(Bounds canopy)
        {
            if (leafColors == null || leafColors.Length == 0) return;

            float cell = CellSize();
            float gravity = 1.8f * cell;   // 천천히 팔랑팔랑
            float vz = 0.2f * cell;

            // 나무 그림의 윗부분(잎이 달린 곳) 여기저기서 나무 밑동 높이까지 떨어진다
            for (int i = 0; i < fellLeafCount; i++)
            {
                float x = Random.Range(canopy.min.x, canopy.max.x);
                float y = Mathf.Lerp(canopy.min.y, canopy.max.y, Random.Range(0.45f, 0.95f));

                var ground = new Vector2(x, canopy.min.y + Random.Range(-0.15f, 0.2f) * cell);
                float height = Mathf.Max(0f, y - ground.y);

                // 땅에 닿을 때까지 걸리는 시간 + 조금 더 머물다 사라진다
                float fall = (vz + Mathf.Sqrt(vz * vz + 2f * gravity * height)) / gravity;

                Spawn(ground,
                      new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(-0.1f, 0.1f)) * cell,
                      vz, gravity,
                      fall + Random.Range(0.2f, 0.45f),
                      pieceSize * Random.Range(0.9f, 1.3f) * cell,
                      leafColors[Random.Range(0, leafColors.Length)],
                      0.1f * cell,
                      height);
            }
        }

        private void PlayGrass(Vector3 at, float amount)
        {
            int count = Mathf.RoundToInt(grassCount * amount);
            Burst(at, leafColors, count, speed: 1.2f, up: 1.8f, gravity: 5f, life: 0.8f,
                  size: pieceSize, wobble: 0.05f);
        }

        // ════════════════════════════════════════════════════════════
        //  조각 뿌리기
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// 한 점에서 조각을 여러 개 튀긴다. 속도·높이·중력은 '타일 한 칸' 단위라서
        /// 타일이 커도 작아도 비슷하게 보인다.
        /// dir 을 주면 그쪽으로 spreadDeg 도 부채꼴 안에서만 튄다. 안 주면 사방으로
        /// </summary>
        private void Burst(Vector3 at, Color[] colors, int count, float speed, float up, float gravity, float life,
                           float size, Vector2 dir = default, float spreadDeg = 360f, float wobble = 0f,
                           float startHeight = 0f)
        {
            if (count <= 0 || colors == null || colors.Length == 0) return;

            float cell = CellSize();
            bool aimed = dir != Vector2.zero && spreadDeg < 360f;
            float baseAngle = aimed ? Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg : 0f;

            for (int i = 0; i < count; i++)
            {
                float angle = aimed
                    ? baseAngle + Random.Range(-spreadDeg, spreadDeg) * 0.5f
                    : Random.Range(0f, 360f);

                var d = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                d.y *= 0.55f;   // 위에서 내려다보는 시점이라 앞뒤로는 덜 퍼지게 (땅이 납작해 보인다)

                Spawn(
                    (Vector2)at + Random.insideUnitCircle * (0.1f * cell),
                    d * (speed * power * Random.Range(0.5f, 1.2f) * cell),
                    up * power * Random.Range(0.7f, 1.3f) * cell,
                    gravity * cell,
                    life * Random.Range(0.8f, 1.2f),
                    size * Random.Range(0.8f, 1.25f) * cell,
                    colors[Random.Range(0, colors.Length)],
                    wobble * cell,
                    startHeight * cell);
            }
        }

        private static float CellSize()
        {
            CropManager farm = CropManager.Instance;
            return farm != null ? Mathf.Max(0.1f, farm.CellSize.x) : 1f;
        }

        private void Spawn(Vector2 at, Vector2 vel, float vz, float gravity, float life, float size, Color color,
                           float wobble, float height = 0f)
        {
            if (_pieces == null) _pieces = new Piece[Mathf.Max(32, maxPieces)];
            if (!_sortingReady) SetupSorting();

            // 빈 자리를 찾는다. 꽉 찼으면 가장 오래된 자리를 재사용
            int index = _next;
            for (int n = 0; n < _pieces.Length; n++)
            {
                int i = (_next + n) % _pieces.Length;
                if (_pieces[i].alive) continue;

                index = i;
                break;
            }

            _next = (index + 1) % _pieces.Length;

            ref Piece p = ref _pieces[index];

            if (p.tr == null)
            {
                var go = new GameObject("Piece");
                go.transform.SetParent(transform, false);

                p.tr = go.transform;
                p.sr = go.AddComponent<SpriteRenderer>();
                p.sr.sprite = Pixel();
            }

            if (customSorting) p.sr.sortingLayerName = sortingLayer;
            else p.sr.sortingLayerID = _layerId;

            p.sr.sortingOrder = customSorting ? sortingOrder : _order;
            p.sr.enabled = true;

            p.ground = at;
            p.vel = vel;
            p.height = height;
            p.vz = vz;
            p.gravity = gravity;
            p.life = life;
            p.maxLife = life;
            p.size = size;
            p.wobble = wobble;
            p.phase = Random.Range(0f, Mathf.PI * 2f);
            p.color = color;
            p.alive = true;

            Draw(ref p);
        }

        /// <summary>
        /// 플레이어 그림 중 가장 위에 그려지는 것과 같은 Sorting Layer 에, 조금 더 위로 그린다.
        /// (그림자처럼 아래에 깔리는 그림을 기준으로 잡으면 땅에 묻힐 수 있어서)
        /// </summary>
        private void SetupSorting()
        {
            _sortingReady = true;
            _layerId = SortingLayer.NameToID("Default");
            _order = 200;

            PlayerInteractor player = FindFirstObjectByType<PlayerInteractor>();
            if (player == null) return;

            SpriteRenderer top = null;
            int topLayer = int.MinValue;

            foreach (SpriteRenderer r in player.GetComponentsInChildren<SpriteRenderer>(true))
            {
                int layer = SortingLayer.GetLayerValueFromID(r.sortingLayerID);

                if (top != null && (layer < topLayer || (layer == topLayer && r.sortingOrder <= top.sortingOrder)))
                    continue;

                top = r;
                topLayer = layer;
            }

            if (top == null) return;

            _layerId = top.sortingLayerID;
            _order = top.sortingOrder + 50;
        }

        private void Update()
        {
            if (_pieces == null) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            for (int i = 0; i < _pieces.Length; i++)
            {
                ref Piece p = ref _pieces[i];
                if (!p.alive) continue;

                p.life -= dt;
                if (p.life <= 0f || p.tr == null)
                {
                    p.alive = false;
                    if (p.sr != null) p.sr.enabled = false;
                    continue;
                }

                // 위로 떴다가 떨어진다
                p.vz -= p.gravity * dt;
                p.height += p.vz * dt;

                if (p.height <= 0f)
                {
                    // 땅에 닿으면 살짝 튀고, 미끄러지다 멈춘다
                    p.height = 0f;
                    p.vz = p.vz < -0.3f ? -p.vz * 0.35f : 0f;
                    p.vel *= 0.5f;
                }

                p.vel *= Mathf.Max(0f, 1f - 1.5f * dt);   // 공기 저항
                p.ground += p.vel * dt;

                Draw(ref p);
            }
        }

        private static void Draw(ref Piece p)
        {
            // 마지막 0.3초 동안만 스르르 사라지면서 조금 작아진다
            float fadeWindow = Mathf.Min(0.3f, p.maxLife * 0.4f);
            float fade = Mathf.Clamp01(p.life / fadeWindow);

            // 살랑임은 공중에 있을 때만. 땅에 가까워질수록 줄어들어서 내려앉으면 멈춘다
            float x = p.ground.x;
            if (p.wobble > 0f) x += Mathf.Sin((p.maxLife - p.life) * 6f + p.phase) * p.wobble * Mathf.Clamp01(p.height * 20f);

            p.tr.position = new Vector3(x, p.ground.y + p.height, 0f);

            float s = p.size * Mathf.Lerp(0.6f, 1f, fade);
            p.tr.localScale = new Vector3(s, s, 1f);

            Color c = p.color;
            c.a *= fade;
            p.sr.color = c;
        }

        /// <summary>1x1 흰 픽셀. 색은 SpriteRenderer 가 입힌다</summary>
        private static Sprite Pixel()
        {
            if (s_pixel != null) return s_pixel;

            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "ToolFXPixel",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            tex.SetPixel(0, 0, Color.white);
            tex.Apply(false, true);

            s_pixel = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            s_pixel.name = "ToolFXPixel";

            return s_pixel;
        }
    }
