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
 
            ApplyStage();
        }
 
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