using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
 
namespace KSM._00.Scripts.Crop
{
    public class CropManager : MonoBehaviour
    {
        private static CropManager _instance;
 
        public static CropManager Instance
        {
            get
            {
                if (_instance == null) _instance = FindFirstObjectByType<CropManager>();
                return _instance;
            }
        }
 
        [Header("타일맵")]
        [Tooltip("작물을 심을 바닥 타일맵 (밭 타일이 그려진 것)")]
        [SerializeField] private Tilemap groundTilemap;
 
        [Tooltip("GrowCrop + SpriteRenderer + BoxCollider2D 가 붙은 프리팹")]
        [SerializeField] private GameObject cropPrefab;
 
        [Tooltip("켜면 여러 칸 작물의 스프라이트가 영역 정중앙에 놓인다.\n" +
                 "끄면 아랫줄에 놓인다 (스프라이트가 영역 전체를 채우도록 그렸을 때).\n" +
                 "어느 쪽이든 앞뒤 정렬은 밑동 기준으로 유지된다.")]
        [SerializeField] private bool placeAtFootprintCenter = true;
 
        [Header("시간")]
        [Tooltip("작물들을 훑는 주기(실제 초). 자주 훑을 필요가 없다")]
        [SerializeField] private float tickInterval = 0.5f;
 
        [Tooltip("Game Clock 이 안 꽂혀 있을 때만 쓰는 폴백 — 인게임 하루 = 실제 몇 초인가")]
        [SerializeField] private float fallbackSecondsPerDay = 720f;
 
        [Tooltip("폴백 시계의 배속. 0이면 일시정지. Game Clock 을 쓰면 무시된다")]
        [SerializeField] private float timeScale = 1f;
 
        [Header("젖은 땅")]
        [Tooltip("젖은 칸에 심긴 작물의 성장 속도 배수")]
        [Min(1f)] public float wetGrowthMultiplier = 1.2f;
 
        private readonly List<GrowCrop> _crops = new();
        private readonly Dictionary<Vector3Int, GrowCrop> _occupied = new();
 
        private float _timer;
        private float _lastClockDays;
        private bool _clockPrimed;
 
        public event Action<ItemSO, int, ItemQuality> OnHarvested;
 
        public Func<ItemSO, int, ItemQuality, bool> CanAcceptHarvest;
 
        public event Action<string> OnHarvestBlocked;
 
        public bool CheckCanAccept(ItemSO item, int amount, ItemQuality quality)
            => CanAcceptHarvest == null || CanAcceptHarvest(item, amount, quality);
 
        public void NotifyHarvestBlocked(string reason) => OnHarvestBlocked?.Invoke(reason);
 
        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
        }
 
        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
 
   
 
        public void Register(GrowCrop crop)
        {
            if (crop != null && !_crops.Contains(crop)) _crops.Add(crop);   
        }
 
        public void Unregister(GrowCrop crop) => _crops.Remove(crop);
 
        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < tickInterval) return;
 
            float realElapsed = _timer;
            _timer = 0f;
 
            float gameDays = ConsumeGameDays(realElapsed);
            if (gameDays <= 0f) return;
            CurrentGameDays = GameClock != null ? GameClock.TotalGameDays : CurrentGameDays + gameDays;
 
            UpdateWetCells();   
            TickAll(gameDays * GrowthSpeedMultiplier);
        }
        private float ConsumeGameDays(float realElapsed)
        {
            if (GameClock != null)
            {
                float now = GameClock.TotalGameDays;
                if (!_clockPrimed)
                {
                    _lastClockDays = now;
                    _clockPrimed = true;
                    return 0f;
                }
 
                float delta = now - _lastClockDays;
                _lastClockDays = now;
 
                return Mathf.Max(0f, delta);  
            }
 
            if (timeScale <= 0f) return 0f;  
 
            return realElapsed * timeScale / Mathf.Max(1f, fallbackSecondsPerDay);
        }
        public IGameClock GameClock { get; set; }
        public float CurrentGameDays { get; private set; }
        private readonly List<IGrowthModifier> _modifiers = new();
        public void AddGrowthModifier(IGrowthModifier modifier)
        {
            if (modifier == null || _modifiers.Contains(modifier)) return;
            _modifiers.Add(modifier);
        }
 
        public void RemoveGrowthModifier(IGrowthModifier modifier) => _modifiers.Remove(modifier);
        public IGrowthModifier GrowthModifier
        {
            get => _modifiers.Count > 0 ? _modifiers[0] : null;
            set
            {
                if (_modifiers.Count > 0) _modifiers.RemoveAt(0);
                if (value != null) _modifiers.Insert(0, value);
            }
        }
 
        public float GrowthSpeedMultiplier
        {
            get
            {
                float v = 1f;
                foreach (IGrowthModifier m in _modifiers) v *= m.GrowthSpeedMultiplier;
                return v;
            }
        }
 
        public float YieldMultiplier
        {
            get
            {
                float v = 1f;
                foreach (IGrowthModifier m in _modifiers) v *= m.YieldMultiplier;
                return v;
            }
        }
 
        public float QualityBonus
        {
            get
            {
                float v = 0f;
                foreach (IGrowthModifier m in _modifiers) v += m.QualityBonus;
                return v;
            }
        }
        public bool AllowBestQuality
        {
            get
            {
                foreach (IGrowthModifier m in _modifiers)
                    if (!m.AllowBestQuality) return false;
 
                return true;
            }
        }
 
        public bool CanPlantNow(CropSO crop)
        {
            foreach (IGrowthModifier m in _modifiers)
                if (!m.CanPlantNow(crop)) return false;
 
            return true;
        }
 
        private void TickAll(float delta)
        {
            for (int i = _crops.Count - 1; i >= 0; i--)
            {
                GrowCrop crop = _crops[i];
                if (crop == null) continue;
                float mul = IsWet(crop.OriginCell) ? wetGrowthMultiplier : 1f;
                crop.Tick(delta * mul);
            }
        }
 
        private struct WetData
        {
            public TileBase originalTile;  
            public float dryAtDay;          
        }
 
        private readonly Dictionary<Vector3Int, WetData> _wet = new();
        private readonly List<Vector3Int> _dryBuffer = new();
 
        public bool IsWet(Vector3Int cell) => _wet.ContainsKey(cell);
 
        public bool SetCellWet(Vector3Int cell, TileBase wetTile, float days)
        {
            if (wetTile == null || days <= 0f) return false;
 
            if (_wet.TryGetValue(cell, out WetData existing))
            {
                existing.dryAtDay = CurrentGameDays + days;
                _wet[cell] = existing;
                return true;
            }
 
            TileBase original = GetGroundTile(cell);
            if (original == null || original == wetTile) return false;
 
            _wet[cell] = new WetData { originalTile = original, dryAtDay = CurrentGameDays + days };
            SetGroundTile(cell, wetTile);
 
            return true;
        }
 
        private void UpdateWetCells()
        {
            if (_wet.Count == 0) return;
 
            _dryBuffer.Clear();
 
            foreach (KeyValuePair<Vector3Int, WetData> kv in _wet)
                if (CurrentGameDays >= kv.Value.dryAtDay) _dryBuffer.Add(kv.Key);
 
            foreach (Vector3Int cell in _dryBuffer)
            {
                if (_wet.TryGetValue(cell, out WetData data))
                    SetGroundTile(cell, data.originalTile);
 
                _wet.Remove(cell);
            }
        }
 
        public void SkipGameDays(float days) => TickAll(Mathf.Max(0f, days));
 
        [ContextMenu("성장 상태 진단")]
        public void DebugGrowthStatus()
        {
            bool hasClock = GameClock != null;
 
            float daysPerRealSecond = hasClock
                ? -1f                                            // 외부 시계라 여기서 알 수 없음
                : timeScale / Mathf.Max(1f, fallbackSecondsPerDay);
 
            string clockLine = hasClock
                ? $"외부 시계 ({GameClock.GetType().Name}) — 현재 {GameClock.TotalGameDays:0.0000} 일차"
                : $"자체 시계 — 실제 {fallbackSecondsPerDay}초 = 인게임 1일, 배속 {timeScale}";
 
            string rateLine = hasClock
                ? "진행 속도는 외부 시계가 결정합니다"
                : $"실제 1초당 {daysPerRealSecond * GrowthSpeedMultiplier:0.00000} 인게임일 진행";
 
            Debug.Log(
                "───── CropManager 진단 ─────\n" +
                $"등록된 작물 : {_crops.Count}개\n" +
                $"점유 중인 칸 : {_occupied.Count}칸\n" +
                $"시계 : {clockLine}\n" +
                $"성장 배수 : x{GrowthSpeedMultiplier}  (0이면 절대 안 자랍니다)\n" +
                $"수확량 배수 : x{YieldMultiplier}\n" +
                $"{rateLine}\n" +
                "───────────────────────────", this);
 
            // 각 작물이 지금 어디까지 왔는지
            for (int i = 0; i < _crops.Count && i < 5; i++)
            {
                GrowCrop c = _crops[i];
                if (c == null || c.Data == null) continue;
 
                float need = c.Data.growthStages[c.NowGrowthStage].durationTime;
 
                Debug.Log($"  · {c.Data.cropName}  {c.NowGrowthStage}단계  " +
                          $"{c.CurrentTimeStage:0.000} / {need} 일  " +
                          $"{(c.IsGrowFinished ? "(수확 가능)" : string.Empty)}", c);
            }
        }
 
        public Vector3Int WorldToCell(Vector3 world) => groundTilemap.WorldToCell(world);
 
        public Vector3 CellToWorldCenter(Vector3Int cell) => groundTilemap.GetCellCenterWorld(cell);
        public Vector3 CellSize => groundTilemap != null ? groundTilemap.cellSize : Vector3.one;
        public TileBase GetGroundTile(Vector3Int cell)
            => groundTilemap != null ? groundTilemap.GetTile(cell) : null;
        public TileBase GetEffectiveGroundTile(Vector3Int cell)
            => _wet.TryGetValue(cell, out WetData data) ? data.originalTile : GetGroundTile(cell);
        public void SetGroundTile(Vector3Int cell, TileBase tile)
        {
            if (groundTilemap != null) groundTilemap.SetTile(cell, tile);
        }
        
        public static Vector3Int GetOrigin(Vector3Int clickedCell, Vector2Int size)
        {
            return new Vector3Int(
                clickedCell.x - (size.x - 1) / 2,
                clickedCell.y - (size.y - 1) / 2,
                clickedCell.z);
        }
 
        public Vector3 GetPlantWorldPos(Vector3Int origin, Vector2Int size)
        {
            Vector3 left = groundTilemap.GetCellCenterWorld(origin);
 
            int topRow = placeAtFootprintCenter ? origin.y + size.y - 1 : origin.y;
            Vector3 opposite = groundTilemap.GetCellCenterWorld(
                new Vector3Int(origin.x + size.x - 1, topRow, origin.z));
 
            return (left + opposite) * 0.5f;
        }
        public float GetSortYOffset(Vector2Int size)
            => placeAtFootprintCenter ? -(size.y - 1) * CellSize.y * 0.5f : 0f;
        public bool CanPlantAt(Vector3Int clickedCell, CropSO crop)
        {
            if (crop == null || groundTilemap == null) return false;
            return CanPlace(GetOrigin(clickedCell, crop.size), crop);
        }
 
        public bool CanPlace(Vector3Int origin, CropSO crop)
        {
            if (crop == null || groundTilemap == null) return false;
 
            if (!CanPlantNow(crop)) return false;
 
            for (int x = 0; x < crop.size.x; x++)
            {
                for (int y = 0; y < crop.size.y; y++)
                {
                    var cell = new Vector3Int(origin.x + x, origin.y + y, origin.z);
 
                    if (_occupied.ContainsKey(cell)) return false;  
 
                    if (!crop.IsPlantableTile(GetEffectiveGroundTile(cell))) return false;
                }
            }
 
            return true;
        }
        public bool TryPlant(Vector3Int clickedCell, CropSO crop)
        {
            if (crop == null || cropPrefab == null || groundTilemap == null) return false;
 
            Vector3Int origin = GetOrigin(clickedCell, crop.size);
            if (!CanPlace(origin, crop)) return false;
 
            Vector3 pos = GetPlantWorldPos(origin, crop.size);
            GameObject go = Instantiate(cropPrefab, pos, Quaternion.identity, transform);
            go.name = $"{crop.cropName}_{origin.x}_{origin.y}";
 
            if (!go.TryGetComponent<GrowCrop>(out var grow))
            {
                Debug.LogError("[CropManager] cropPrefab에 GrowCrop이 없습니다.", cropPrefab);
                Destroy(go);
                return false;
            }
            if (go.TryGetComponent<BoxCollider2D>(out var box))
            {
                Vector3 cs = groundTilemap.cellSize;
                box.size = new Vector2(crop.size.x * cs.x, crop.size.y * cs.y);
                box.offset = placeAtFootprintCenter
                    ? Vector2.zero
                    : new Vector2(0f, (crop.size.y - 1) * cs.y * 0.5f);
            }
 
            grow.Init(crop, origin);
            OccupyCells(grow);
 
            return true;
        }
        public void OccupyCells(GrowCrop crop)
        {
            if (crop == null || crop.Data == null) return;
 
            Vector3Int origin = crop.OriginCell;
            Vector2Int size = crop.Data.size;
 
            for (int x = 0; x < size.x; x++)
                for (int y = 0; y < size.y; y++)
                    _occupied[new Vector3Int(origin.x + x, origin.y + y, origin.z)] = crop;
        }
        public void ReleaseCells(GrowCrop crop)
        {
            if (crop == null || crop.Data == null) return;
 
            Vector3Int origin = crop.OriginCell;
            Vector2Int size = crop.Data.size;
 
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    var cell = new Vector3Int(origin.x + x, origin.y + y, origin.z);
 
                    // 남의 칸을 지우지 않도록 주인 확인
                    if (_occupied.TryGetValue(cell, out var owner) && owner == crop)
                        _occupied.Remove(cell);
                }
            }
        }
        public GrowCrop GetOccupant(Vector3Int cell)
            => _occupied.TryGetValue(cell, out var crop) ? crop : null;
 
        public bool TryHarvestAt(Vector3Int cell)
        {
            var crop = GetOccupant(cell);
            return crop != null && crop.CanHarvest && crop.TryHarvest();
        }
        public bool RemoveCropAt(Vector3Int cell, bool protectMature = true)
        {
            GrowCrop crop = GetOccupant(cell);
            if (crop == null) return false;
 
            if (protectMature && crop.CanHarvest) return false;
 
            ReleaseCells(crop);
            Destroy(crop.gameObject);
 
            return true;
        }
        public void NotifyHarvested(ItemSO item, int amount, ItemQuality quality)
            => OnHarvested?.Invoke(item, amount, quality);
    }
}
 