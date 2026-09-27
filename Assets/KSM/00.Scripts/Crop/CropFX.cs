using System.Collections;
using UnityEngine;
using KSM._00.Scripts.Crop;

    /// <summary>
    /// 작물 연출 모음. 작물 프리팹(GrowCrop 이 붙은 것)에 붙이기만 하면 된다.
    ///
    ///   바람        — 밭 전체가 물결치듯 살랑살랑 흔들린다
    ///   자랄 때     — 단계가 바뀌면 '뽁' 하고 쫀득하게 튕긴다
    ///   다 자라면   — 반짝반짝 빛나서 수확할 게 한눈에 보인다
    ///   수확할 때   — 톡 튀어올라 플레이어한테 쏙 들어간다. 좋음은 은빛, 최상은 금빛으로 터진다
    ///
    /// ★ 스프라이트 피벗이 가운데든 아래든 상관없다. 항상 밑동을 축으로 흔들리고 튕긴다
    /// ★ Shaker 도 새 버전으로 바꿀 것. 옛 버전은 위치를 통째로 덮어써서 이 연출과 싸운다
    /// ★ 반짝이 그림을 안 넣으면 코드로 만든 픽셀 반짝이를 쓴다. 파티클·소리 칸은 비워도 된다
    /// </summary>
    [RequireComponent(typeof(GrowCrop))]
    public class CropFX : MonoBehaviour
    {
        // ───────────────────────── 바람 ─────────────────────────
        [Header("바람 — 살랑살랑")]
        [SerializeField] private bool wind = true;

        [Tooltip("최대로 기우는 각도. 도트가 지글거려 보이면 줄일 것")]
        [SerializeField, Range(0f, 10f)] private float windAngle = 2.5f;

        [Tooltip("흔들리는 빠르기")]
        [SerializeField, Range(0f, 6f)] private float windSpeed = 1.8f;

        [Tooltip("물결 간격. 클수록 옆 작물과 박자가 어긋나서 물결처럼 보인다. 0 이면 다 같이 흔들린다")]
        [SerializeField, Range(0f, 3f)] private float windWave = 0.8f;

        [Tooltip("이 단계부터 흔들린다. 0단계가 흙더미(씨앗) 그림이면 1 로 둘 것")]
        [SerializeField, Min(0)] private int windFromStage = 1;

        // ───────────────────────── 뽁 ─────────────────────────
        [Header("자랄 때 — 뽁")]
        [SerializeField] private bool stagePop = true;

        [Tooltip("심는 순간에도 튕긴다")]
        [SerializeField] private bool popOnPlant = true;

        [Tooltip("튕기는 세기. 0.25 면 키가 최대 약 18% 늘었다 돌아온다")]
        [SerializeField, Range(0f, 0.6f)] private float popStrength = 0.25f;

        [SerializeField, Min(0.05f)] private float popTime = 0.45f;

        [Tooltip("자랄 때 생성할 파티클 프리팹 (선택)")]
        [SerializeField] private GameObject growParticle;

        [Tooltip("자랄 때 소리 (선택)")]
        [SerializeField] private AudioClip growSound;

        // ───────────────────────── 반짝 ─────────────────────────
        [Header("다 자라면 — 반짝")]
        [SerializeField] private bool readySparkle = true;

        [Tooltip("비우면 코드로 만든 픽셀 반짝이를 쓴다")]
        [SerializeField] private Sprite sparkleSprite;

        [SerializeField] private Color sparkleColor = Color.white;

        [Tooltip("반짝이 크기 (월드 단위)")]
        [SerializeField, Min(0.02f)] private float sparkleSize = 0.22f;

        [Tooltip("다음 반짝임까지 기다리는 시간(초). x~y 사이 랜덤")]
        [SerializeField] private Vector2 sparkleInterval = new Vector2(0.8f, 2.2f);

        [SerializeField, Min(0.1f)] private float sparkleTime = 0.45f;

        [Tooltip("다 자란 작물이 숨 쉬듯 살짝 부풀었다 줄어든다. 0 이면 끔")]
        [SerializeField, Range(0f, 0.1f)] private float readyBreath = 0.03f;

        // ───────────────────────── 수확 ─────────────────────────
        [Header("수확할 때 — 쏙")]
        [SerializeField] private bool harvestFly = true;

        [Tooltip("날아가는 아이콘 크기 (월드 단위)")]
        [SerializeField, Min(0.05f)] private float flyIconSize = 0.55f;

        [Tooltip("여러 개를 수확하면 아이콘도 여러 개 튀어나온다. 그 최대 개수")]
        [SerializeField, Range(1, 8)] private int maxFlyIcons = 3;

        [Tooltip("튀어오르는 높이")]
        [SerializeField, Min(0f)] private float popUpHeight = 0.7f;

        [SerializeField, Min(0.05f)] private float popUpTime = 0.28f;

        [SerializeField, Min(0.05f)] private float flyTime = 0.3f;

        [Tooltip("플레이어 발밑에서 이만큼 위를 향해 날아간다")]
        [SerializeField] private Vector2 flyTargetOffset = new Vector2(0f, 0.7f);

        [Tooltip("수확할 때 생성할 파티클 프리팹 (선택)")]
        [SerializeField] private GameObject harvestParticle;

        [Tooltip("수확할 때 소리 (선택)")]
        [SerializeField] private AudioClip harvestSound;

        [SerializeField, Range(0f, 1f)] private float soundVolume = 0.8f;

        // ════════════════════════════════════════════════════════════

        private GrowCrop _crop;
        private SpriteRenderer _renderer;

        private bool _hasBase;
        private Vector3 _baseScale;
        private Quaternion _baseRotation;
        private Vector3 _basePosition;      // 물결 박자 계산용 (월드 좌표)
        private Vector3 _appliedOffset;     // 밑동 보정으로 위치에 더해 둔 값

        private bool _atRest = true;        // 기울기·크기가 원래대로인가 (그러면 매 프레임 건드리지 않는다)

        private int _stageEvents;
        private bool _justHarvested;
        private float _popTimer = -1f;      // 0 이상이면 튕기는 중
        private float _breathPhase;
        private float _windJitter = 1f;     // 작물마다 흔들리는 폭이 조금씩 다르다

        private SpriteRenderer _sparkle;
        private float _sparkleTimer = -1f;  // 0 이상이면 반짝이는 중
        private float _nextSparkleAt;

        private static Sprite s_pixelSparkle;
        private static Transform s_player;
        private static float s_lastSoundTime = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_pixelSparkle = null;
            s_player = null;
            s_lastSoundTime = -1f;
        }

        private void Awake()
        {
            _crop = GetComponent<GrowCrop>();
            _renderer = GetComponent<SpriteRenderer>();

            // Awake 에서 구독해야 심는 순간(Init)에 오는 첫 이벤트를 놓치지 않는다
            _crop.OnStageChanged += HandleStageChanged;
            _crop.Harvested += HandleHarvested;
        }

        private void OnDestroy()
        {
            if (_crop == null) return;

            _crop.OnStageChanged -= HandleStageChanged;
            _crop.Harvested -= HandleHarvested;
        }

        private void OnDisable()
        {
            // 연출 도중에 꺼져도 기울어지거나 찌그러진 채로 굳지 않게
            RestorePose();
            HideSparkle();
            _popTimer = -1f;
        }

        // ════════════════════════════════════════════════════════════
        //  이벤트
        // ════════════════════════════════════════════════════════════

        private void HandleStageChanged(int stage)
        {
            // 첫 이벤트는 GrowCrop.Init 안에서 '자연스러운 크기·위치가 정해진 직후' 에 온다.
            // 그래서 기준 자세를 여기서 잡는다. Start 에서 잡으면 순서에 따라 원래 크기를 덮어쓸 수 있다
            if (!_hasBase) CaptureBase();

            _stageEvents++;
            bool planting = _stageEvents == 1;
            bool afterHarvest = _justHarvested;
            _justHarvested = false;

            if (stagePop && (!planting || popOnPlant)) _popTimer = 0f;

            // 파티클·소리는 '자라서' 바뀔 때만. 심을 때, 수확 후 되돌아갈 때, 화면 밖일 때는 뺀다
            if (planting || afterHarvest || !_renderer.isVisible) return;

            SpawnOneShot(growParticle, _renderer.bounds.center);
            PlaySound(growSound);
        }

        private void HandleHarvested(int amount, ItemQuality quality)
        {
            // 다회용 작물은 곧바로 단계가 되돌아간다. 그때 '자랐다' 소리가 나지 않게 표시해 둔다
            _justHarvested = true;
            HideSparkle();

            Vector3 from = _renderer.bounds.center;

            SpawnOneShot(harvestParticle, from);
            PlaySound(harvestSound);

            if (!harvestFly) return;

            // 1회용 작물은 이 직후 파괴된다. 그래서 날아가는 연출은 CropManager 가 대신 돌린다
            CropManager runner = CropManager.Instance;
            if (runner == null || !runner.isActiveAndEnabled) return;

            Sprite icon = HarvestIcon();
            if (icon == null) return;

            var fly = new FlyParams
            {
                icon = icon,
                sortingLayerID = _renderer.sortingLayerID,
                sortingOrder = _renderer.sortingOrder + 100,     // 플레이어보다 위에 보이게
                from = from,
                size = flyIconSize,
                popUpHeight = popUpHeight,
                popUpTime = popUpTime,
                flyTime = flyTime,
                targetOffset = flyTargetOffset,
            };

            int count = Mathf.Clamp(amount, 1, maxFlyIcons);
            for (int i = 0; i < count; i++)
                runner.StartCoroutine(FlyRoutine(fly, i, count));

            if (quality == ItemQuality.Normal) return;

            // 좋음 = 은빛, 최상 = 금빛. 아이콘이 꼭대기에 닿는 순간 터진다
            bool best = quality == ItemQuality.Best;

            runner.StartCoroutine(BurstRoutine(
                sparkleSprite != null ? sparkleSprite : PixelSparkle(),
                from + Vector3.up * popUpHeight,
                ItemQualityUtil.TintColor(quality),
                best ? 10 : 6,
                sparkleSize * (best ? 1.4f : 1.1f),
                fly.sortingLayerID,
                fly.sortingOrder + 1,
                popUpTime));
        }

        private Sprite HarvestIcon()
        {
            CropSO data = _crop.Data;

            if (data != null && data.harvestItem != null && data.harvestItem.icon != null)
                return data.harvestItem.icon;

            return _renderer.sprite;   // 수확물 아이콘이 없으면 작물 그림 그대로
        }

        // ════════════════════════════════════════════════════════════
        //  매 프레임 — 바람 / 뽁 / 숨쉬기 / 반짝
        // ════════════════════════════════════════════════════════════

        private void LateUpdate()
        {
            if (!_hasBase) return;

            // 화면 밖이면 쉰다. 작물이 많아도 가볍게
            if (!_renderer.isVisible)
            {
                if (_sparkleTimer >= 0f) HideSparkle();
                return;
            }

            bool ready = _crop.IsGrowFinished;

            float angle = WindAngle();
            Vector2 scale = Vector2.Scale(TickPop(), BreathScale(ready));

            // 가만히 있어야 할 때는 트랜스폼을 건드리지 않는다 (콜라이더 갱신 비용 절약)
            bool idle = angle == 0f && scale.x == 1f && scale.y == 1f;

            if (!(idle && _atRest))
            {
                ApplyPose(angle, scale);
                _atRest = idle;
            }

            UpdateSparkle(ready);
        }

        private void CaptureBase()
        {
            _hasBase = true;
            _baseScale = transform.localScale;
            _baseRotation = transform.localRotation;
            _basePosition = transform.position;
            _appliedOffset = Vector3.zero;
            _atRest = true;

            // 작물마다 다른 숨쉬기 박자와 흔들림 폭. 위치로 정하므로 항상 같다
            float hash = Mathf.Repeat(_basePosition.x * 0.731f + _basePosition.y * 1.37f, 1f);
            _breathPhase = hash * Mathf.PI * 2f;
            _windJitter = Mathf.Lerp(0.8f, 1.2f, Mathf.Repeat(hash * 7.13f, 1f));
            _nextSparkleAt = Time.time + Random.Range(0f, Mathf.Max(0.1f, sparkleInterval.y));
        }

        private float WindAngle()
        {
            if (!wind || windAngle <= 0f || _crop.NowGrowthStage < windFromStage) return 0f;

            // 돌풍 — 밭 전체가 같이 세졌다 약해졌다 한다
            float gust = Mathf.Lerp(0.45f, 1f, Mathf.Clamp01(Mathf.PerlinNoise(Time.time * 0.23f, 0.37f)));

            // 물결 — 오른쪽 작물일수록 박자가 늦다 = 바람이 왼쪽에서 오른쪽으로 지나간다
            float wave = (_basePosition.x + _basePosition.y * 0.35f) * windWave;

            // 큰 작물(2x2, 3x3)은 덜 흔들린다. 커다란 호박이 휘청이면 이상하니까
            float big = 1f;
            if (_crop.Data != null) big = 1f / Mathf.Max(1, Mathf.Max(_crop.Data.size.x, _crop.Data.size.y));

            return windAngle * _windJitter * big * gust * Mathf.Sin(Time.time * windSpeed - wave);
        }

        /// <summary>뽁 — 쭉 늘어났다가, 살짝 눌렸다가, 잦아든다</summary>
        private Vector2 TickPop()
        {
            if (_popTimer < 0f) return Vector2.one;

            _popTimer += Time.deltaTime;
            float p = _popTimer / popTime;

            if (p >= 1f)
            {
                _popTimer = -1f;
                return Vector2.one;
            }

            float decay = (1f - p) * (1f - p);
            float s = Mathf.Sin(p * Mathf.PI * 3f) * decay * popStrength;

            return new Vector2(1f - s * 0.5f, 1f + s);   // 키가 크면 폭은 줄어든다 (쫀득한 느낌)
        }

        private Vector2 BreathScale(bool ready)
        {
            if (!ready || readyBreath <= 0f) return Vector2.one;

            float b = Mathf.Sin(Time.time * 2.4f + _breathPhase) * readyBreath;
            return new Vector2(1f - b * 0.5f, 1f + b);
        }

        /// <summary>
        /// 기울기·크기를 적용하고, 밑동이 제자리에 있도록 위치를 보정한다.
        /// 피벗이 아래면 보정값은 0 이다. 피벗이 가운데면 이 보정 덕분에 허리가 아니라 밑동을 축으로 움직인다
        /// </summary>
        private void ApplyPose(float angle, Vector2 scale)
        {
            transform.localRotation = _baseRotation * Quaternion.Euler(0f, 0f, angle);
            transform.localScale = new Vector3(_baseScale.x * scale.x, _baseScale.y * scale.y, _baseScale.z);

            Vector2 anchor = BottomAnchor();
            var rest = new Vector2(_baseScale.x * anchor.x, _baseScale.y * anchor.y);
            Vector2 now = Rotate(Vector2.Scale(rest, scale), angle);
            Vector3 offset = _baseRotation * (Vector3)(rest - now);

            // ★ 위치는 '바뀐 만큼만' 더한다. 그래서 Shaker 같은 다른 연출과 동시에 움직여도 서로 덮어쓰지 않는다
            transform.localPosition += offset - _appliedOffset;
            _appliedOffset = offset;
        }

        private void RestorePose()
        {
            if (!_hasBase) return;

            transform.localPosition -= _appliedOffset;
            _appliedOffset = Vector3.zero;

            transform.localRotation = _baseRotation;
            transform.localScale = _baseScale;
            _atRest = true;
        }

        /// <summary>그림의 밑동(아래 가운데) 위치. 피벗 기준, 크기 적용 전 단위</summary>
        private Vector2 BottomAnchor()
        {
            Sprite sprite = _renderer.sprite;
            if (sprite == null) return Vector2.zero;

            Bounds b = sprite.bounds;
            float x = _renderer.flipX ? -b.center.x : b.center.x;
            float y = _renderer.flipY ? -b.max.y : b.min.y;

            return new Vector2(x, y);
        }

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(r);
            float s = Mathf.Sin(r);

            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        // ════════════════════════════════════════════════════════════
        //  반짝
        // ════════════════════════════════════════════════════════════

        private void UpdateSparkle(bool ready)
        {
            if (!ready || !readySparkle)
            {
                if (_sparkleTimer >= 0f) HideSparkle();
                return;
            }

            if (_sparkleTimer < 0f)
            {
                if (Time.time < _nextSparkleAt) return;
                if (!ShowSparkle()) return;
            }

            _sparkleTimer += Time.deltaTime;
            float p = _sparkleTimer / sparkleTime;

            if (p >= 1f)
            {
                HideSparkle();
                return;
            }

            // 톡 커졌다가 스르르 작아진다
            float k = p < 0.3f ? p / 0.3f : 1f - (p - 0.3f) / 0.7f;
            SetSparkleSize(k * k * (3f - 2f * k));
        }

        private bool ShowSparkle()
        {
            Sprite sprite = sparkleSprite != null ? sparkleSprite : PixelSparkle();
            if (sprite == null) return false;

            if (_sparkle == null)
            {
                var go = new GameObject("Sparkle");
                go.transform.SetParent(transform, false);
                _sparkle = go.AddComponent<SpriteRenderer>();
            }

            _sparkle.sprite = sprite;
            _sparkle.color = sparkleColor;
            _sparkle.sortingLayerID = _renderer.sortingLayerID;
            _sparkle.sortingOrder = _renderer.sortingOrder + 1;

            // 작물 그림의 윗부분 아무 데나 (열매가 달리는 쪽)
            Bounds b = _renderer.bounds;
            _sparkle.transform.position = new Vector3(
                Mathf.Lerp(b.min.x, b.max.x, Random.Range(0.2f, 0.8f)),
                Mathf.Lerp(b.min.y, b.max.y, Random.Range(0.4f, 0.9f)),
                transform.position.z);

            _sparkle.enabled = true;
            _sparkleTimer = 0f;
            SetSparkleSize(0f);

            return true;
        }

        private void HideSparkle()
        {
            _sparkleTimer = -1f;
            if (_sparkle != null) _sparkle.enabled = false;

            float min = Mathf.Max(0.05f, Mathf.Min(sparkleInterval.x, sparkleInterval.y));
            float max = Mathf.Max(min, Mathf.Max(sparkleInterval.x, sparkleInterval.y));
            _nextSparkleAt = Time.time + Random.Range(min, max);
        }

        private void SetSparkleSize(float k)
        {
            if (_sparkle == null || _sparkle.sprite == null) return;

            Transform t = _sparkle.transform;
            t.rotation = Quaternion.identity;   // 작물이 기울어도 반짝이는 똑바로

            Vector3 size = _sparkle.sprite.bounds.size;
            float native = Mathf.Max(size.x, size.y);
            if (native <= 0f) return;

            // 부모(작물)의 크기 변화는 빼고 월드 기준 크기로 맞춘다
            Vector3 parent = transform.lossyScale;
            float world = sparkleSize * k / native;

            t.localScale = new Vector3(
                world / Mathf.Max(0.0001f, Mathf.Abs(parent.x)),
                world / Mathf.Max(0.0001f, Mathf.Abs(parent.y)),
                1f);
        }

        /// <summary>7x7 픽셀 반짝이를 코드로 만든다 (한 번만 만들어서 모두 같이 쓴다)</summary>
        private static Sprite PixelSparkle()
        {
            if (s_pixelSparkle != null) return s_pixelSparkle;

            string[] rows =
            {
                "...#...",
                "...#...",
                "..###..",
                "#######",
                "..###..",
                "...#...",
                "...#...",
            };

            const int n = 7;

            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                name = "PixelSparkle",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    bool on = rows[n - 1 - y][x] == '#';   // 텍스처는 아래 줄부터 채운다
                    pixels[y * n + x] = new Color32(255, 255, 255, on ? (byte)255 : (byte)0);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            s_pixelSparkle = Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), n);
            s_pixelSparkle.name = "PixelSparkle";

            return s_pixelSparkle;
        }

        // ════════════════════════════════════════════════════════════
        //  수확 — 날아가는 아이콘 / 품질 반짝이
        //  ★ 작물이 파괴된 뒤에도 돌아야 하므로 static 이고, 필요한 값은 전부 복사해서 넘긴다
        // ════════════════════════════════════════════════════════════

        private struct FlyParams
        {
            public Sprite icon;
            public int sortingLayerID;
            public int sortingOrder;
            public Vector3 from;
            public float size;
            public float popUpHeight;
            public float popUpTime;
            public float flyTime;
            public Vector2 targetOffset;
        }

        private static IEnumerator FlyRoutine(FlyParams f, int index, int count)
        {
            // 여러 개면 차례로, 부채꼴로 퍼지며 튀어나온다
            if (index > 0) yield return new WaitForSeconds(index * 0.08f);

            var go = new GameObject("HarvestIcon");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = f.icon;
            sr.sortingLayerID = f.sortingLayerID;
            sr.sortingOrder = f.sortingOrder;

            Transform tr = go.transform;

            Vector3 native = f.icon.bounds.size;
            float unit = f.size / Mathf.Max(0.0001f, Mathf.Max(native.x, native.y));
            Vector3 spriteCenter = f.icon.bounds.center;   // 피벗이 가운데가 아니어도 그림 중심이 경로를 따라가게

            float spread = count > 1 ? Mathf.Lerp(-1f, 1f, index / (float)(count - 1)) : 0f;
            Vector3 apex = f.from + new Vector3(spread * 0.4f + Random.Range(-0.06f, 0.06f), f.popUpHeight, 0f);

            // 1) 톡 튀어오른다 (점점 느려지게)
            float t = 0f;
            while (t < f.popUpTime)
            {
                if (go == null) yield break;

                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / f.popUpTime);
                float e = 1f - (1f - p) * (1f - p);

                Place(tr, Vector3.LerpUnclamped(f.from, apex, e), spriteCenter, unit * Mathf.Lerp(0.5f, 1.1f, e));
                yield return null;
            }

            // 2) 꼭대기에서 잠깐 멈칫
            yield return new WaitForSeconds(0.08f);

            // 3) 플레이어한테 빨려 들어간다 (점점 빠르게). 플레이어가 움직이면 따라간다
            Transform player = FindPlayer();
            Vector3 target = player != null ? player.position + (Vector3)f.targetOffset : apex + Vector3.up * 0.3f;

            t = 0f;
            while (t < f.flyTime)
            {
                if (go == null) yield break;

                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / f.flyTime);
                float e = p * p;

                if (player != null) target = player.position + (Vector3)f.targetOffset;

                Place(tr, Vector3.LerpUnclamped(apex, target, e), spriteCenter, unit * Mathf.Lerp(1.1f, 0.3f, e));

                Color c = sr.color;
                c.a = p < 0.6f ? 1f : 1f - (p - 0.6f) / 0.4f;
                sr.color = c;

                yield return null;
            }

            if (go != null) Destroy(go);
        }

        private static void Place(Transform tr, Vector3 visualCenter, Vector3 spriteCenter, float scale)
        {
            tr.localScale = new Vector3(scale, scale, 1f);
            tr.position = visualCenter - spriteCenter * scale;
        }

        private static IEnumerator BurstRoutine(Sprite sprite, Vector3 center, Color color, int count,
                                                float size, int sortingLayerID, int sortingOrder, float delay)
        {
            if (sprite == null) yield break;
            if (delay > 0f) yield return new WaitForSeconds(delay);

            var parts = new Transform[count];
            var dirs = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("QualitySparkle");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = color;
                sr.sortingLayerID = sortingLayerID;
                sr.sortingOrder = sortingOrder;

                float a = (i + Random.Range(-0.25f, 0.25f)) / count * Mathf.PI * 2f;
                dirs[i] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * Random.Range(0.35f, 0.6f);

                parts[i] = go.transform;
                parts[i].position = center;
                parts[i].localScale = Vector3.zero;
            }

            Vector3 native = sprite.bounds.size;
            float unit = size / Mathf.Max(0.0001f, Mathf.Max(native.x, native.y));

            const float duration = 0.5f;
            float t = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                float e = 1f - (1f - p) * (1f - p);                            // 퍼지는 건 점점 느리게
                float k = p < 0.25f ? p / 0.25f : 1f - (p - 0.25f) / 0.75f;   // 커졌다가 작아진다
                float s = unit * k;

                for (int i = 0; i < count; i++)
                {
                    if (parts[i] == null) continue;

                    parts[i].position = center + dirs[i] * e;
                    parts[i].localScale = new Vector3(s, s, 1f);
                }

                yield return null;
            }

            for (int i = 0; i < count; i++)
                if (parts[i] != null) Destroy(parts[i].gameObject);
        }

        private static Transform FindPlayer()
        {
            if (s_player == null)
            {
                PlayerInteractor interactor = FindFirstObjectByType<PlayerInteractor>();
                if (interactor != null) s_player = interactor.transform;
            }

            return s_player;
        }

        // ════════════════════════════════════════════════════════════
        //  파티클 / 소리
        // ════════════════════════════════════════════════════════════

        private static void SpawnOneShot(GameObject prefab, Vector3 position)
        {
            if (prefab == null) return;

            GameObject go = Instantiate(prefab, position, Quaternion.identity);
            Destroy(go, 3f);   // 파티클이 끝나면 치운다
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip == null || soundVolume <= 0f) return;

            // 하루가 넘어가서 작물 수십 개가 한꺼번에 자라도 소리가 한 번만 나게
            if (Time.unscaledTime - s_lastSoundTime < 0.08f) return;
            s_lastSoundTime = Time.unscaledTime;

            // 카메라 위치에서 틀면 거리와 상관없이 같은 크기로 들린다 (2D 게임용)
            Camera cam = Camera.main;
            Vector3 at = cam != null ? cam.transform.position : transform.position;

            AudioSource.PlayClipAtPoint(clip, at, soundVolume);
        }
    }
