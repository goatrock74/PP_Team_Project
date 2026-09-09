using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
 
namespace KSM._00.Scripts.Crop
{
    /// <summary>
    /// 농장의 두 가지 책임을 진다.
    ///   1) 시간 배급  - 등록된 작물들에게 주기적으로 Tick(delta)을 나눠준다
    ///   2) 칸 관리    - 어느 칸에 무엇이 심겨 있는지, 심을 수 있는지를 판정한다
    /// </summary>
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
 
        /// <summary>수확이 일어났을 때 (수확 아이템, 수량, 품질). 인벤토리가 구독하면 된다</summary>
        public event Action<ItemSO, int, ItemQuality> OnHarvested;
 
        /// <summary>
        /// 수확물을 받을 자리가 있는지 묻는 함수. PlayerInventory 가 등록한다.
        /// 등록된 게 없으면 항상 받을 수 있는 것으로 본다.
        /// (이 델리게이트 덕분에 Crop 쪽이 인벤토리 클래스를 직접 알 필요가 없다)
        /// </summary>
        public Func<ItemSO, int, ItemQuality, bool> CanAcceptHarvest;
 
        /// <summary>수확 실패 사유를 알릴 때 (가방 가득 참 등). UI 토스트가 구독하면 된다</summary>
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
 
        // ════════════════════════════════════════════════════════════
        //  시간 배급
        // ════════════════════════════════════════════════════════════
 
        public void Register(GrowCrop crop)
        {
            if (crop != null && !_crops.Contains(crop)) _crops.Add(crop);   // 중복 등록 = 2배속 성장
        }
 
        public void Unregister(GrowCrop crop) => _crops.Remove(crop);
 
        private void Update()
        {
            // 훑는 "주기" 는 실제 시간 기준, 흐른 "양" 은 게임 시계에서 가져온다
            _timer += Time.deltaTime;
            if (_timer < tickInterval) return;
 
            float realElapsed = _timer;
            _timer = 0f;
 
            float gameDays = ConsumeGameDays(realElapsed);
            if (gameDays <= 0f) return;
 
            // 재생 타이머 등이 참고할 수 있게 누적 일수를 들고 있는다
            CurrentGameDays = GameClock != null ? GameClock.TotalGameDays : CurrentGameDays + gameDays;
 
            UpdateWetCells();   // 마른 칸을 먼저 되돌리고 나서 성장시킨다
            TickAll(gameDays * GrowthSpeedMultiplier);
        }
 
        /// <summary>
        /// ★ 시간의 출처는 여기 한 곳뿐.
        /// GameClock 이 꽂혀 있으면 그 시계가 흐른 만큼, 없으면 자체 계산으로 인게임 일수를 낸다.
        /// </summary>
        private float ConsumeGameDays(float realElapsed)
        {
            if (GameClock != null)
            {
                float now = GameClock.TotalGameDays;
 
                // 첫 호출은 기준점만 잡는다 (시작 시각이 0이 아닐 수 있으므로)
                if (!_clockPrimed)
                {
                    _lastClockDays = now;
                    _clockPrimed = true;
                    return 0f;
                }
 
                float delta = now - _lastClockDays;
                _lastClockDays = now;
 
                return Mathf.Max(0f, delta);   // 되감김은 무시
            }
 
            if (timeScale <= 0f) return 0f;   // 폴백 시계의 일시정지
 
            return realElapsed * timeScale / Mathf.Max(1f, fallbackSecondsPerDay);
        }
 
        // ════════════════════════════════════════════════════════════
        //  외부 영향 (계절 / 날씨 / 비료 ...)
        // ════════════════════════════════════════════════════════════
 
        /// <summary>
        /// 외부 시간 시스템이 여기에 꽂히면 작물이 그 시계를 따라간다.
        /// 안 꽂혀 있으면 fallbackSecondsPerDay 로 자체 계산한다.
        /// </summary>
        public IGameClock GameClock { get; set; }
 
        /// <summary>
        /// 게임 시작부터 흐른 인게임 일수. 시계가 없어도 항상 유효하다.
        /// 채집물 재생, 쿨다운 같은 "며칠 뒤" 계산에 쓰면 된다.
        /// </summary>
        public float CurrentGameDays { get; private set; }
 
        /// <summary>
        /// 성장에 영향을 주는 것들. 계절·날씨, 마스터리, 나중에 비료까지 <b>동시에</b> 꽂을 수 있다.
        /// 하나도 없으면 전부 기본값(배수 1, 보너스 0)으로 동작한다.
        /// </summary>
        private readonly List<IGrowthModifier> _modifiers = new();
 
        /// <summary>보정자를 꽂는다. 중복 등록은 무시한다 (두 번 곱해지는 사고 방지)</summary>
        public void AddGrowthModifier(IGrowthModifier modifier)
        {
            if (modifier == null || _modifiers.Contains(modifier)) return;
            _modifiers.Add(modifier);
        }
 
        public void RemoveGrowthModifier(IGrowthModifier modifier) => _modifiers.Remove(modifier);
 
        /// <summary>
        /// 예전처럼 하나만 꽂고 싶을 때 쓰는 호환용 프로퍼티.
        /// 새 코드는 AddGrowthModifier 를 쓰는 게 좋다 — 그래야 여러 개가 공존한다.
        /// </summary>
        public IGrowthModifier GrowthModifier
        {
            get => _modifiers.Count > 0 ? _modifiers[0] : null;
            set
            {
                if (_modifiers.Count > 0) _modifiers.RemoveAt(0);
                if (value != null) _modifiers.Insert(0, value);
            }
        }
 
        // ── 합산 규칙 ──
        //   속도·수확량 : 곱한다 (가을 1.5배 × 마스터리 1.2배 = 1.8배)
        //   품질 보너스 : 더한다 (확률에 더하는 값이라 곱하면 이상해진다)
        //   심기·최상   : 하나라도 반대하면 막힌다
 
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
 
        /// <summary>보정자가 하나도 없으면 제한 없음(true). 하나라도 막으면 false</summary>
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
            // 역순: Tick 도중 작물이 리스트에서 빠져도 안전
            for (int i = _crops.Count - 1; i >= 0; i--)
            {
                GrowCrop crop = _crops[i];
                if (crop == null) continue;
 
                // 젖은 땅에 있는 작물만 추가 배수를 받는다
                float mul = IsWet(crop.OriginCell) ? wetGrowthMultiplier : 1f;
                crop.Tick(delta * mul);
            }
        }
 
        // ════════════════════════════════════════════════════════════
        //  젖은 땅
        // ════════════════════════════════════════════════════════════
 
        private struct WetData
        {
            public TileBase originalTile;   // 마르면 되돌릴 타일
            public float dryAtDay;          // 이 시각이 지나면 마른다
        }
 
        private readonly Dictionary<Vector3Int, WetData> _wet = new();
        private readonly List<Vector3Int> _dryBuffer = new();
 
        public bool IsWet(Vector3Int cell) => _wet.ContainsKey(cell);
 
        /// <summary>
        /// 그 칸을 적신다. days 가 지나면 원래 타일로 저절로 돌아간다.
        /// 이미 젖어 있으면 기한만 갱신한다 (원래 타일을 젖은 타일로 덮어쓰지 않도록).
        /// </summary>
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
 
        /// <summary>기한이 지난 칸을 원래 타일로 되돌린다</summary>
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
 
        /// <summary>
        /// 인게임 일수만큼 즉시 성장시킨다. 0.5 = 반나절.
        /// GameClock 을 쓰는 경우 시계가 점프하면 자동으로 반영되므로 보통은 부를 일이 없다.
        /// </summary>
        public void SkipGameDays(float days) => TickAll(Mathf.Max(0f, days));
 
        // ════════════════════════════════════════════════════════════
        //  진단
        // ════════════════════════════════════════════════════════════
 
        /// <summary>
        /// 작물이 안 자랄 때 원인을 찾는 용도.
        /// 플레이 중 인스펙터의 CropManager 컴포넌트 우클릭 → "성장 상태 진단"
        /// </summary>
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
 
        // ════════════════════════════════════════════════════════════
        //  좌표 변환
        // ════════════════════════════════════════════════════════════
 
        public Vector3Int WorldToCell(Vector3 world) => groundTilemap.WorldToCell(world);
 
        public Vector3 CellToWorldCenter(Vector3Int cell) => groundTilemap.GetCellCenterWorld(cell);
 
        /// <summary>타일 한 칸의 크기 (미리보기 스케일 등에 쓴다)</summary>
        public Vector3 CellSize => groundTilemap != null ? groundTilemap.cellSize : Vector3.one;
 
        /// <summary>그 칸의 바닥 타일. 없으면 null</summary>
        public TileBase GetGroundTile(Vector3Int cell)
            => groundTilemap != null ? groundTilemap.GetTile(cell) : null;
 
        /// <summary>
        /// ★ "판정용" 바닥 타일.
        /// 젖은 칸이면 물 주기 전의 <b>원래 타일</b>을, 아니면 지금 깔려 있는 타일을 준다.
        ///
        /// 젖음은 땅의 종류가 바뀐 게 아니라 상태가 얹힌 것뿐이므로,
        /// 심을 수 있는지 같은 판정은 젖은 타일이 아니라 원래 타일로 해야 한다.
        /// 덕분에 CropSO 의 Plantable Tiles 에 젖은 타일을 따로 넣을 필요가 없다.
        /// </summary>
        public TileBase GetEffectiveGroundTile(Vector3Int cell)
            => _wet.TryGetValue(cell, out WetData data) ? data.originalTile : GetGroundTile(cell);
 
        /// <summary>바닥 타일을 바꾼다 (괭이로 밭 갈기 등)</summary>
        public void SetGroundTile(Vector3Int cell, TileBase tile)
        {
            if (groundTilemap != null) groundTilemap.SetTile(cell, tile);
        }
 
        /// <summary>
        /// 클릭한 칸을 중앙으로 보고 좌하단 원점을 구한다.
        /// 3x3 → 클릭 칸이 정확히 가운데. 2x2처럼 짝수는 중앙이 없으므로 좌하단으로 치우친다.
        /// </summary>
        public static Vector3Int GetOrigin(Vector3Int clickedCell, Vector2Int size)
        {
            return new Vector3Int(
                clickedCell.x - (size.x - 1) / 2,
                clickedCell.y - (size.y - 1) / 2,
                clickedCell.z);
        }
 
        /// <summary>
        /// 스프라이트를 놓을 위치.
        ///
        /// placeAtFootprintCenter = true  → 차지한 영역의 정중앙 (스프라이트가 가운데 보임)
        ///                        = false → 아랫줄 가운데 (스프라이트가 영역 전체를 채울 때)
        ///
        /// 앞뒤 겹침 정렬을 Y좌표로 하는 경우, 중앙 배치는 여러 칸 작물의 기준점을
        /// 가운뎃줄로 올려버린다. 정렬이 어긋나 보이면 이 옵션을 꺼서 아랫줄에 놓거나,
        /// 정렬 쪽에서 SortYOffset 만큼 보정해주면 된다.
        /// </summary>
        public Vector3 GetPlantWorldPos(Vector3Int origin, Vector2Int size)
        {
            Vector3 left = groundTilemap.GetCellCenterWorld(origin);
 
            int topRow = placeAtFootprintCenter ? origin.y + size.y - 1 : origin.y;
            Vector3 opposite = groundTilemap.GetCellCenterWorld(
                new Vector3Int(origin.x + size.x - 1, topRow, origin.z));
 
            return (left + opposite) * 0.5f;
        }
 
        /// <summary>
        /// 오브젝트 원점이 영역 중앙일 때, 정렬 기준을 아랫줄(밑동)로 되돌리기 위한 보정값.
        /// 중앙 배치가 아니면 0.
        ///
        /// Y정렬을 쓰는 쪽에서 이 값만큼 기준점을 내려주면 여러 칸 작물의 앞뒤가 맞는다.
        /// </summary>
        public float GetSortYOffset(Vector2Int size)
            => placeAtFootprintCenter ? -(size.y - 1) * CellSize.y * 0.5f : 0f;
 
        // ════════════════════════════════════════════════════════════
        //  심기
        // ════════════════════════════════════════════════════════════
 
        /// <summary>클릭한 칸을 중앙으로 해서 심을 수 있는지 판정</summary>
        public bool CanPlantAt(Vector3Int clickedCell, CropSO crop)
        {
            if (crop == null || groundTilemap == null) return false;
            return CanPlace(GetOrigin(clickedCell, crop.size), crop);
        }
 
        /// <summary>좌하단 원점 기준으로 심을 수 있는지 판정</summary>
        public bool CanPlace(Vector3Int origin, CropSO crop)
        {
            if (crop == null || groundTilemap == null) return false;
 
            // 제철이 아니면 심을 수 없다 (계절 시스템이 없으면 항상 통과)
            if (!CanPlantNow(crop)) return false;
 
            for (int x = 0; x < crop.size.x; x++)
            {
                for (int y = 0; y < crop.size.y; y++)
                {
                    var cell = new Vector3Int(origin.x + x, origin.y + y, origin.z);
 
                    if (_occupied.ContainsKey(cell)) return false;   // 이미 뭔가 있음
 
                    // 젖은 칸은 원래 타일로 판정한다. 젖었다고 못 심으면 이상하니까
                    if (!crop.IsPlantableTile(GetEffectiveGroundTile(cell))) return false;
                }
            }
 
            return true;
        }
 
        /// <summary>클릭한 칸을 중앙으로 심는다. 성공하면 true</summary>
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
 
            // 여러 칸 작물이면 클릭 판정 영역도 그만큼 넓혀준다.
            // 아랫줄 배치일 때는 콜라이더를 위로 밀어야 영역과 맞는다
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
 
        /// <summary>작물이 차지한 모든 칸을 점유 표시</summary>
        public void OccupyCells(GrowCrop crop)
        {
            if (crop == null || crop.Data == null) return;
 
            Vector3Int origin = crop.OriginCell;
            Vector2Int size = crop.Data.size;
 
            for (int x = 0; x < size.x; x++)
                for (int y = 0; y < size.y; y++)
                    _occupied[new Vector3Int(origin.x + x, origin.y + y, origin.z)] = crop;
        }
 
        /// <summary>작물이 차지했던 칸을 반납. 파괴 전에 반드시 호출해야 그 자리에 다시 심을 수 있다</summary>
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
 
        // ════════════════════════════════════════════════════════════
        //  수확 / 조회
        // ════════════════════════════════════════════════════════════
 
        /// <summary>이 칸을 차지한 작물 (없으면 null). 3x3이면 9칸 어디를 물어도 같은 작물이 나온다</summary>
        public GrowCrop GetOccupant(Vector3Int cell)
            => _occupied.TryGetValue(cell, out var crop) ? crop : null;
 
        public bool TryHarvestAt(Vector3Int cell)
        {
            var crop = GetOccupant(cell);
            return crop != null && crop.CanHarvest && crop.TryHarvest();
        }
 
        /// <summary>
        /// 그 칸의 작물을 파낸다. 칸 반납까지 처리하므로 그 자리에 바로 다시 심을 수 있다.
        /// 괭이 같은 도구에서도 이 메서드를 부르면 된다.
        /// </summary>
        /// <param name="protectMature">true 면 다 자란(수확 가능한) 작물은 파내지 않는다</param>
        public bool RemoveCropAt(Vector3Int cell, bool protectMature = true)
        {
            GrowCrop crop = GetOccupant(cell);
            if (crop == null) return false;
 
            if (protectMature && crop.CanHarvest) return false;
 
            ReleaseCells(crop);
            Destroy(crop.gameObject);
 
            return true;
        }
 
        /// <summary>GrowCrop이 수확 시 호출. 인벤토리는 OnHarvested만 구독하면 된다</summary>
        public void NotifyHarvested(ItemSO item, int amount, ItemQuality quality)
            => OnHarvested?.Invoke(item, amount, quality);
    }
}
 