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
        [Tooltip("작물들을 훑는 주기(초). 매 프레임 돌 필요가 없다")]
        [SerializeField] private float tickInterval = 0.5f;
 
        [Tooltip("0이면 일시정지, 2면 2배속. 친구의 시간 시스템이 붙으면 제거될 예정")]
        [SerializeField] private float timeScale = 1f;
 
        private readonly List<GrowCrop> _crops = new();
        private readonly Dictionary<Vector3Int, GrowCrop> _occupied = new();
        private float _timer;
 
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
            if (timeScale <= 0f) { _timer = 0f; return; }   // 일시정지 중엔 헛돌지 않는다
 
            _timer += Time.deltaTime;
            if (_timer < tickInterval) return;
 
            // tickInterval이 아니라 _timer를 넘긴다. 프레임 드랍으로 초과된 시간이 증발하지 않도록
            float delta = ToGameDelta(_timer);
            _timer = 0f;
 
            TickAll(delta);
        }
 
        /// <summary>★ 시간의 출처는 여기 한 곳뿐. 외부 TimeManager가 붙으면 이 메서드만 갈아끼운다</summary>
        private float ToGameDelta(float realSeconds) => realSeconds * timeScale;
 
        private void TickAll(float delta)
        {
            // 역순: Tick 도중 작물이 리스트에서 빠져도 안전
            for (int i = _crops.Count - 1; i >= 0; i--)
                _crops[i].Tick(delta);
        }
 
        /// <summary>"자고 일어나면 8시간 경과" 같은 명시적 시간 점프</summary>
        public void SkipTime(float gameSeconds) => TickAll(gameSeconds);
 
        // ════════════════════════════════════════════════════════════
        //  좌표 변환
        // ════════════════════════════════════════════════════════════
 
        public Vector3Int WorldToCell(Vector3 world) => groundTilemap.WorldToCell(world);
 
        public Vector3 CellToWorldCenter(Vector3Int cell) => groundTilemap.GetCellCenterWorld(cell);
 
        /// <summary>타일 한 칸의 크기 (미리보기 스케일 등에 쓴다)</summary>
        public Vector3 CellSize => groundTilemap != null ? groundTilemap.cellSize : Vector3.one;
 
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
        /// 어느 쪽이든 Y정렬은 항상 "밑동" 기준으로 맞춰진다 (TryPlant 에서 YSorter 를 보정).
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
        /// 오브젝트 원점이 영역 중앙일 때, 정렬 기준을 아랫줄로 되돌리기 위한 보정값.
        /// 중앙 배치가 아니면 0.
        /// </summary>
        private float GetSortYOffset(Vector2Int size)
            => placeAtFootprintCenter ? -(size.y - 1) * groundTilemap.cellSize.y * 0.5f : 0f;
 
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
 
            for (int x = 0; x < crop.size.x; x++)
            {
                for (int y = 0; y < crop.size.y; y++)
                {
                    var cell = new Vector3Int(origin.x + x, origin.y + y, origin.z);
 
                    if (_occupied.ContainsKey(cell)) return false;                    // 이미 뭔가 있음
                    if (!crop.IsPlantableTile(groundTilemap.GetTile(cell))) return false; // 심을 수 없는 타일
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
 
            // 스프라이트를 가운데 놓더라도 앞뒤 정렬은 밑동 기준이어야 자연스럽다
            if (go.TryGetComponent<YSorter>(out var sorter))
                sorter.SetYOffset(GetSortYOffset(crop.size));
 
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