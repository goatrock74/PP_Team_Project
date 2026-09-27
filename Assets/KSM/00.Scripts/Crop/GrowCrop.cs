using System;
using System.Collections;
using UnityEngine;

namespace KSM._00.Scripts.Crop
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class GrowCrop : MonoBehaviour, IHarvestable
    {
        [SerializeField] private CropSO cropSO;

        [Header("자연스러움")]
        [Tooltip("칸 중앙에서 상하좌우로 어긋나는 최대 거리. 칸 크기 대비 비율이다.\n" +
                 "0.06 이면 한 칸의 6% 안에서 흔들린다. 0 이면 정확히 중앙")]
        [SerializeField, Range(0f, 0.3f)] private float positionJitter = 0.06f;
        
        [SerializeField] private AudioClip sfx;


        [Tooltip("작물 크기 배수 범위. x=최소, y=최대")]
        [SerializeField] private Vector2 scaleRange = new Vector2(1f, 1.15f);

        [Tooltip("가끔 좌우를 뒤집는다. 좌우 대칭이 아닌 그림에서만 켤 것")]
        [SerializeField] private bool randomFlipX;

        [Header("제철이 지나면 — 시들기")]
        [Tooltip("CropSO 의 Plantable Seasons 에 없는 계절이 되면 시든다.\n" +
                 "시든 작물은 더 안 자라고 수확도 안 된다. 괭이 + X 로 뽑아야 한다.\n" +
                 "Plantable Seasons 가 All 이면 절대 안 시든다")]
        [SerializeField] private bool wiltOutOfSeason = true;

        [Tooltip("다 자란 작물은 제철이 지나도 안 시든다 (수확할 기회를 준다)")]
        [SerializeField] private bool matureSurvives = true;

        [Tooltip("시든 작물 그림 (선택). 비우면 지금 그림에 색만 입힌다")]
        [SerializeField] private Sprite wiltedSprite;

        [Tooltip("시든 작물 색. 원래 색에 곱해진다")]
        [SerializeField] private Color wiltedColor = new Color(0.62f, 0.5f, 0.36f, 1f);

        [Tooltip("색이 바뀌는 데 걸리는 시간(초)")]
        [SerializeField, Min(0f)] private float wiltFadeTime = 0.8f;

        [Tooltip("시들 때 화면 아래에 띄울 문구. 비우면 안 띄운다 (여러 개가 한꺼번에 시들어도 한 번만 뜬다)")]
        [SerializeField] private string wiltMessage = "제철이 지나 작물이 시들었습니다";

        private SpriteRenderer _renderer;
        private bool _lookApplied;
        private CropManager _manager;
        private bool _initialized;
        private bool _harvested;   // 1회용 작물이 흔들리는 동안 두 번 캐이는 걸 막는다

        private static float s_lastWiltMessageTime = -999f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_lastWiltMessageTime = -999f;

        [field: SerializeField] public int NowGrowthStage { get; private set; }
        [field: SerializeField] public float CurrentTimeStage { get; private set; }
        [field: SerializeField] public bool IsGrowFinished { get; private set; }

        /// <summary>제철이 지나 시들었는가. 시든 작물은 더 안 자라고 수확도 안 된다</summary>
        public bool IsWilted { get; private set; }

        /// <summary>이 작물이 차지한 영역의 좌하단 칸</summary>
        public Vector3Int OriginCell { get; private set; }

        public CropSO Data => cropSO;

        /// <summary>단계가 바뀔 때 (파티클, 사운드 등이 구독)</summary>
        public event Action<int> OnStageChanged;

        /// <summary>
        /// 수확됐을 때 (수량, 품질). 연출(CropFX)이 구독한다.
        /// 1회용 작물은 이 이벤트 직후 파괴된다
        /// </summary>
        public event Action<int, ItemQuality> Harvested;

        /// <summary>시들었을 때</summary>
        public event Action OnWilted;

        // ── IHarvestable ────────────────────────────────────────────────
        public bool CanHarvest => _initialized && IsGrowFinished && !_harvested && !IsWilted;

        public string HarvestPrompt
        {
            get
            {
                if (!_initialized) return string.Empty;
                if (IsWilted) return $"{cropSO.cropName} (시들었음)";
                return IsGrowFinished ? $"{cropSO.cropName} 수확" : $"{cropSO.cropName} (자라는 중)";
            }
        }
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// 수확까지 남은 성장량. 단위는 인게임 일수이고, 계절·물 보정은 곱하기 <b>전</b> 값이다.
        /// 다 자랐으면 0
        /// </summary>
        public float RemainingDays
        {
            get
            {
                if (!_initialized || IsGrowFinished) return 0f;

                float left = cropSO.growthStages[NowGrowthStage].durationTime - CurrentTimeStage;

                for (int i = NowGrowthStage + 1; i < cropSO.harvestStageIndex; i++)
                    left += cropSO.growthStages[i].durationTime;

                return Mathf.Max(0f, left);
            }
        }

        /// <summary>0 = 막 심음, 1 = 다 자람</summary>
        public float GrowthProgress
        {
            get
            {
                if (!_initialized) return 0f;
                if (IsGrowFinished) return 1f;

                float total = 0f;
                for (int i = 0; i < cropSO.harvestStageIndex; i++)
                    total += cropSO.growthStages[i].durationTime;

                return total > 0f ? Mathf.Clamp01(1f - RemainingDays / total) : 1f;
            }
        }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            _manager = CropManager.Instance;
            if (_manager != null) _manager.Register(this);
        }

        private void OnDisable()
        {
            if (_manager != null) _manager.Unregister(this);
            _manager = null;
        }

        /// <summary>
        /// 씬에 미리 배치해둔 테스트용 작물을 자동으로 등록한다.
        /// TryPlant로 심은 작물은 이미 _initialized라 여기서 건너뛴다.
        /// </summary>
        private void Start()
        {
            if (_initialized || cropSO == null) return;

            var mgr = CropManager.Instance;
            if (mgr == null) return;

            Vector3Int cell = mgr.WorldToCell(transform.position);
            Vector3Int origin = CropManager.GetOrigin(cell, cropSO.size);

            Init(cropSO, origin);
            mgr.OccupyCells(this);
        }

        /// <summary>심을 때 CropManager.TryPlant가 호출한다.</summary>
        public void Init(CropSO crop, Vector3Int originCell)
        {
            if (crop == null || crop.growthStages == null || crop.growthStages.Length == 0)
            {
                Debug.LogError($"[GrowCrop] 성장 단계가 비어있습니다: {name}", this);
                return;
            }

            cropSO = crop;
            OriginCell = originCell;
            NowGrowthStage = 0;
            CurrentTimeStage = 0f;
            IsGrowFinished = NowGrowthStage >= cropSO.harvestStageIndex;
            _initialized = true;

            ApplyNaturalLook();
            ApplyStage();
        }

        // ════════════════════════════════════════════════════════════
        //  자연스러움 — 칸마다 조금씩 다른 위치·크기
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// 전부 정확히 같은 자리에 같은 크기로 서 있으면 밭이 격자무늬처럼 보인다.
        /// 아주 살짝만 흔들어주면 훨씬 자연스러워진다.
        ///
        /// ★ 난수 씨앗을 <b>칸 좌표</b>로 만든다. 그래서 같은 칸이면 언제나 같은 값이 나오고,
        ///   씬을 다시 열든 세이브를 불러오든 작물이 제자리에 그대로 있는다.
        ///   (Random.Range 를 쓰면 불러올 때마다 위치가 튄다)
        /// </summary>
        private void ApplyNaturalLook()
        {
            if (_lookApplied) return;
            _lookApplied = true;

            var rng = new System.Random(CellSeed(OriginCell));

            float Next01() => (float)rng.NextDouble();
            float Signed() => Next01() * 2f - 1f;   // -1 ~ +1

            // ── 크기 ──
            float scale = Mathf.Lerp(
                Mathf.Min(scaleRange.x, scaleRange.y),
                Mathf.Max(scaleRange.x, scaleRange.y),
                Next01());

            if (randomFlipX && Next01() < 0.5f) _renderer.flipX = true;

            if (!Mathf.Approximately(scale, 1f))
            {
                Vector3 s = transform.localScale;
                transform.localScale = new Vector3(s.x * scale, s.y * scale, s.z);

                // 콜라이더는 커지면 안 된다. 옆 칸까지 삐져나와서 클릭이 헷갈린다.
                // 스케일이 곱해지는 만큼 크기를 미리 나눠둔다
                if (TryGetComponent<BoxCollider2D>(out var box))
                {
                    box.size /= scale;
                    box.offset /= scale;
                }
            }

            // ── 위치 ──
            if (positionJitter <= 0f) return;

            CropManager mgr = _manager != null ? _manager : CropManager.Instance;
            Vector3 cell = mgr != null ? mgr.CellSize : Vector3.one;

            transform.position += new Vector3(
                Signed() * positionJitter * cell.x,
                Signed() * positionJitter * cell.y,
                0f);
        }

        /// <summary>칸 좌표 → 난수 씨앗. 이웃한 칸끼리 값이 비슷해지지 않게 큰 소수를 섞는다</summary>
        private static int CellSeed(Vector3Int c)
            => unchecked(c.x * 73856093 ^ c.y * 19349663 ^ c.z * 83492791);

        public void Tick(float delta)
        {
            if (!_initialized || IsWilted) return;

            // 제철이 지났는지 먼저 본다. 계절 때문에 성장 속도가 0 이어도 이 검사는 매번 돈다
            if (ShouldWilt())
            {
                Wilt();
                return;
            }

            if (IsGrowFinished) return;

            CurrentTimeStage += delta;

            float needTime = cropSO.growthStages[NowGrowthStage].durationTime;

            // while + 빼기: 큰 delta 하나로 여러 단계를 건너뛰어도 시간이 새지 않는다
            while (!IsGrowFinished && CurrentTimeStage >= needTime)
            {
                CurrentTimeStage -= needTime;
                NowGrowthStage++;

                if (NowGrowthStage >= cropSO.harvestStageIndex)
                {
                    NowGrowthStage = cropSO.harvestStageIndex;
                    IsGrowFinished = true;
                }

                ApplyStage();
                needTime = cropSO.growthStages[NowGrowthStage].durationTime;
            }
        }

        private void ApplyStage()
        {
            _renderer.sprite = cropSO.growthStages[NowGrowthStage].sprite;
            OnStageChanged?.Invoke(NowGrowthStage);
        }

        // ════════════════════════════════════════════════════════════
        //  시들기
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// 지금 계절이 이 작물의 제철인가는 CropManager.CanPlantNow 로 묻는다.
        /// (심을 수 있는 계절 = 자랄 수 있는 계절. 판정은 SeasonGrowthAdapter 가 한다)
        /// 계절 시스템이 씬에 없으면 항상 제철로 나오므로 절대 안 시든다
        /// </summary>
        private bool ShouldWilt()
        {
            if (!wiltOutOfSeason) return false;
            if (matureSurvives && IsGrowFinished) return false;

            CropManager mgr = _manager != null ? _manager : CropManager.Instance;
            return mgr != null && !mgr.CanPlantNow(cropSO);
        }

        /// <summary>작물을 시들게 한다. 한 번 시들면 되돌아오지 않는다</summary>
        public void Wilt()
        {
            if (!_initialized || IsWilted) return;

            IsWilted = true;

            // 시든 작물은 '다 자란' 게 아니다. 그래서 반짝이·숨쉬기 같은 수확 연출도 같이 꺼진다
            IsGrowFinished = false;

            if (wiltedSprite != null) _renderer.sprite = wiltedSprite;

            Color from = _renderer.color;
            Color to = from * wiltedColor;

            if (wiltFadeTime > 0f && isActiveAndEnabled) StartCoroutine(FadeColor(from, to));
            else _renderer.color = to;

            if (!string.IsNullOrEmpty(wiltMessage) && Time.unscaledTime - s_lastWiltMessageTime > 3f)
            {
                s_lastWiltMessageTime = Time.unscaledTime;
                ScreenMessageUI.Show(wiltMessage, new Color(1f, 0.72f, 0.38f));
            }

            OnWilted?.Invoke();
        }

        private IEnumerator FadeColor(Color from, Color to)
        {
            float t = 0f;

            while (t < wiltFadeTime)
            {
                t += Time.deltaTime;
                _renderer.color = Color.Lerp(from, to, Mathf.Clamp01(t / wiltFadeTime));
                yield return null;
            }

            _renderer.color = to;
        }

        /// <summary>계절이 바뀔 때까지 기다리지 않고 바로 확인해 보는 용도</summary>
        [ContextMenu("테스트: 시들게 하기")]
        private void DebugWilt() => Wilt();

        // ════════════════════════════════════════════════════════════
        //  수확
        // ════════════════════════════════════════════════════════════

        public bool TryHarvest()
        {
            if (!CanHarvest) return false;

            // Unity의 == 는 파괴된 오브젝트도 null로 판정하므로 ??= 대신 이렇게 쓴다
            if (_manager == null) _manager = CropManager.Instance;

            // 계절·날씨 보정을 곱한다 (해당 시스템이 없으면 배수 1, 보너스 0)
            float yieldMul = _manager != null ? _manager.YieldMultiplier : 1f;
            float qualityBonus = _manager != null ? _manager.QualityBonus : 0f;

            int amount = Mathf.Max(1, Mathf.RoundToInt(cropSO.RollYield() * yieldMul));

            // 농사 마스터리 10레벨 전에는 최상 등급이 '좋음'으로 강등된다.
            // 보정자가 하나도 안 꽂혀 있으면 제한 없음
            bool allowBest = _manager == null || _manager.AllowBestQuality;

            // 한 번 수확에 품질을 한 번 굴린다.
            // 열매 하나하나 다르게 하고 싶으면 amount 만큼 반복해서 굴리고 품질별로 나눠 보내면 된다
            ItemQuality quality = cropSO.qualityChance.Roll(qualityBonus, allowBest);

            if (cropSO.harvestItem == null)
            {
                Debug.LogWarning($"[GrowCrop] {cropSO.name} 의 Harvest Item 이 비어있어 인벤토리에 아무것도 들어가지 않습니다.", cropSO);
            }
            else if (_manager != null)
            {
                // ★ 자리가 없으면 아예 수확하지 않는다. 안 그러면 수확물이 조용히 증발한다
                if (!_manager.CheckCanAccept(cropSO.harvestItem, amount, quality))
                {
                    _manager.NotifyHarvestBlocked("가방이 가득 찼습니다");
                    return false;
                }

                _manager.NotifyHarvested(cropSO.harvestItem, amount, quality);
            }

            // 농사 경험치는 수확물의 가치로 정해진다. 비싼 작물일수록, 등급이 좋을수록 많이 준다.
            // 씬에 MasteryManager 가 없으면 아무 일도 일어나지 않는다
            if (cropSO.harvestItem != null)
                MasteryManager.GainByValue(MasteryType.Farming, cropSO.harvestItem.GetSellPrice(quality) * amount);

            // 연출용 (튀어오르는 아이콘, 품질 반짝이 등). 파괴되기 '전에' 알려야 위치와 그림을 쓸 수 있다
            Harvested?.Invoke(amount, quality);
            SoundManager.Instance.PlaySFX(sfx);

            switch (cropSO.harvestType)
            {
                case HarvestType.Single:
                    // 한 번 캐면 끝. 칸을 반납하고 바로 치운다
                    if (_manager != null) _manager.ReleaseCells(this);

                    _harvested = true;
                    Destroy(gameObject);
                    break;

                case HarvestType.Multiple:
                    // 다시 자라는 작물. 지정된 단계로 되돌린다
                    NowGrowthStage = Mathf.Clamp(cropSO.regrowStageIndex, 0, cropSO.harvestStageIndex);
                    CurrentTimeStage = 0f;
                    IsGrowFinished = NowGrowthStage >= cropSO.harvestStageIndex;
                    ApplyStage();
                    break;
            }

            return true;
        }

        /// <summary>
        /// 성장을 즉시 앞당긴다 (물주기, 비료 등). 단위는 인게임 일수.
        /// 여러 단계를 한 번에 건너뛰어도 Tick 안의 while 이 알아서 처리한다.
        /// </summary>
        public bool AddGrowth(float days)
        {
            if (!_initialized || IsGrowFinished || IsWilted || days <= 0f) return false;

            Tick(days);
            return true;
        }

        /// <summary>세이브 로드용. Init 다음에 호출한다.</summary>
        public void LoadState(int stage, float stageTime)
        {
            if (!_initialized) return;

            NowGrowthStage = Mathf.Clamp(stage, 0, cropSO.harvestStageIndex);
            CurrentTimeStage = Mathf.Max(0f, stageTime);
            IsGrowFinished = NowGrowthStage >= cropSO.harvestStageIndex;

            ApplyStage();
        }
    }
}