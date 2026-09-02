using System;
using UnityEngine;
 
namespace KSM._00.Scripts.Crop
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class GrowCrop : MonoBehaviour, IHarvestable
    {
        [SerializeField] private CropSO cropSO;
 
        private SpriteRenderer _renderer;
        private CropManager _manager;
        private bool _initialized;
 
        [field: SerializeField] public int NowGrowthStage { get; private set; }
        [field: SerializeField] public float CurrentTimeStage { get; private set; }
        [field: SerializeField] public bool IsGrowFinished { get; private set; }
 
        /// <summary>이 작물이 차지한 영역의 좌하단 칸</summary>
        public Vector3Int OriginCell { get; private set; }
 
        public CropSO Data => cropSO;
 
        /// <summary>단계가 바뀔 때 (파티클, 사운드 등이 구독)</summary>
        public event Action<int> OnStageChanged;
 
        // ── IHarvestable ────────────────────────────────────────────────
        public bool CanHarvest => _initialized && IsGrowFinished;
 
        public string HarvestPrompt
        {
            get
            {
                if (!_initialized) return string.Empty;
                return IsGrowFinished ? $"{cropSO.cropName} 수확" : $"{cropSO.cropName} (자라는 중)";
            }
        }
        // ────────────────────────────────────────────────────────────────
 
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
 
            ApplyStage();
        }
 
        public void Tick(float delta)
        {
            if (!_initialized || IsGrowFinished) return;
 
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
 
        public bool TryHarvest()
        {
            if (!CanHarvest) return false;
 
            // Unity의 == 는 파괴된 오브젝트도 null로 판정하므로 ??= 대신 이렇게 쓴다
            if (_manager == null) _manager = CropManager.Instance;
 
            int amount = cropSO.RollYield();
            if (_manager != null) _manager.NotifyHarvested(cropSO, amount);
 
            switch (cropSO.harvestType)
            {
                case HarvestType.Single:
                    // 파괴 전에 칸을 반납해야 그 자리에 다시 심을 수 있다
                    if (_manager != null) _manager.ReleaseCells(this);
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