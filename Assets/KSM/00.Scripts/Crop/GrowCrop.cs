using System;
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
 
        [Tooltip("작물 크기 배수 범위. x=최소, y=최대")]
        [SerializeField] private Vector2 scaleRange = new Vector2(1f, 1.15f);
 
        [Tooltip("가끔 좌우를 뒤집는다. 좌우 대칭이 아닌 그림에서만 켤 것")]
        [SerializeField] private bool randomFlipX;
 
        private SpriteRenderer _renderer;
        private bool _lookApplied;
        private CropManager _manager;
        private bool _initialized;
        private bool _harvested;   
 
        [field: SerializeField] public int NowGrowthStage { get; private set; }
        [field: SerializeField] public float CurrentTimeStage { get; private set; }
        [field: SerializeField] public bool IsGrowFinished { get; private set; }
        public Vector3Int OriginCell { get; private set; }
 
        public CropSO Data => cropSO;
        public event Action<int> OnStageChanged;
        public bool CanHarvest => _initialized && IsGrowFinished && !_harvested;
 
        public string HarvestPrompt
        {
            get
            {
                if (!_initialized) return string.Empty;
                return IsGrowFinished ? $"{cropSO.cropName} 수확" : $"{cropSO.cropName} (자라는 중)";
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
                if (TryGetComponent<BoxCollider2D>(out var box))
                {
                    box.size /= scale;
                    box.offset /= scale;
                }
            }
            if (positionJitter <= 0f) return;
 
            CropManager mgr = _manager != null ? _manager : CropManager.Instance;
            Vector3 cell = mgr != null ? mgr.CellSize : Vector3.one;
 
            transform.position += new Vector3(
                Signed() * positionJitter * cell.x,
                Signed() * positionJitter * cell.y,
                0f);
        }
        private static int CellSeed(Vector3Int c)
            => unchecked(c.x * 73856093 ^ c.y * 19349663 ^ c.z * 83492791);
 
        public void Tick(float delta)
        {
            if (!_initialized || IsGrowFinished) return;
 
            CurrentTimeStage += delta;
 
            float needTime = cropSO.growthStages[NowGrowthStage].durationTime;
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
 
        public bool TryHarvest()
        {
            if (!CanHarvest) return false;
            if (_manager == null) _manager = CropManager.Instance;
            float yieldMul = _manager != null ? _manager.YieldMultiplier : 1f;
            float qualityBonus = _manager != null ? _manager.QualityBonus : 0f;
 
            int amount = Mathf.Max(1, Mathf.RoundToInt(cropSO.RollYield() * yieldMul));
 
            bool allowBest = _manager == null || _manager.AllowBestQuality;
 
            ItemQuality quality = cropSO.qualityChance.Roll(qualityBonus, allowBest);
 
            if (cropSO.harvestItem == null)
            {
                Debug.LogWarning($"[GrowCrop] {cropSO.name} 의 Harvest Item 이 비어있어 인벤토리에 아무것도 들어가지 않습니다.", cropSO);
            }
            else if (_manager != null)
            {
                if (!_manager.CheckCanAccept(cropSO.harvestItem, amount, quality))
                {
                    _manager.NotifyHarvestBlocked("가방이 가득 찼습니다");
                    return false;
                }
 
                _manager.NotifyHarvested(cropSO.harvestItem, amount, quality);
            }
            if (cropSO.harvestItem != null)
                MasteryManager.GainByValue(MasteryType.Farming, cropSO.harvestItem.GetSellPrice(quality) * amount);
 
            switch (cropSO.harvestType)
            {
                case HarvestType.Single:
                    if (_manager != null) _manager.ReleaseCells(this);
 
                    _harvested = true;
                    Destroy(gameObject);
                    break;
 
                case HarvestType.Multiple:
                    NowGrowthStage = Mathf.Clamp(cropSO.regrowStageIndex, 0, cropSO.harvestStageIndex);
                    CurrentTimeStage = 0f;
                    IsGrowFinished = NowGrowthStage >= cropSO.harvestStageIndex;
                    ApplyStage();
                    break;
            }
 
            return true;
        }
        public bool AddGrowth(float days)
        {
            if (!_initialized || IsGrowFinished || days <= 0f) return false;
 
            Tick(days);
            return true;
        }
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
 