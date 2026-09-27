using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace KSM._00.Scripts.Crop
{
    /// <summary>
    /// 밭과 작물, 나무와 풀숲을 저장하고, 다음에 켤 때 그대로 되살린다.
    ///
    /// 저장하는 것
    ///   · 괭이로 갈아서 바뀐 바닥 칸
    ///   · 물 준 칸 (마르기까지 남은 시간까지)
    ///   · 작물 — 종류, 자리, 몇 단계인지, 그 단계에서 얼마나 자랐는지, 시들었는지
    ///   · 나무 — 그루터기인지, 다시 자라기까지 남은 날짜, 깎인 체력, 베어서 없어졌는지
    ///   · 풀숲(채집물) — 캤는지, 다시 자라기까지 남은 날짜, 캐서 없어졌는지
    ///     나무·풀숲은 '자리' 로 구분한다. 저장한 뒤에 씬에서 옮기면 그것만 처음 상태로 돌아간다
    ///
    /// 저장 시점
    ///   · 몇 초마다 자동 (심거나 수확하거나 나무·풀숲을 베면 1초 안에 바로)
    ///   · 게임을 끌 때 (에디터에서 플레이를 멈출 때도)
    /// 저장 위치: PlayerPrefs (인벤토리 저장과 같은 방식). 씬마다 따로 저장된다
    ///
    /// 붙이는 법: CropManager 오브젝트에 이 컴포넌트를 추가. 끝.
    ///   아래 Crops / Tiles 목록은 에디터에서 자동으로 채워진다.
    ///
    /// ★ 처음 상태(빈 밭, 멀쩡한 나무·풀숲)로 다시 시작하려면: 컴포넌트 ⋮ → "저장 지우기"
    ///   (PlayerPrefs 를 통째로 초기화해도 같이 지워진다)
    /// ★ 씬이 바뀌는 순간(오브젝트가 부서지는 중)에는 저장하지 않는다.
    ///   부서지는 순서가 제멋대로라 반쯤 빈 밭이 저장될 수 있어서다. 대신 몇 초마다 저장해 둔다.
    ///   씬을 바꾸기 직전에 FarmSaveManager.SaveNow() 를 부르면 가장 확실하다 (안 불러도 된다)
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class FarmSaveManager : MonoBehaviour
    {
        private const string KeyPrefix = "FarmSaveV1_";

        [Header("저장")]
        [Tooltip("이 간격(초)마다 자동 저장한다")]
        [SerializeField, Min(2f)] private float autoSaveInterval = 5f;

        [Tooltip("끄면 저장본을 안 불러오고 빈 밭으로 시작한다 (테스트용)")]
        [SerializeField] private bool loadOnStart = true;

        [SerializeField] private bool verboseLog = true;

        [Header("불러올 때 이름으로 찾아볼 목록 — 에디터에서 자동으로 채워진다")]
        [Tooltip("모든 CropSO. 새 작물을 만들면 알아서 추가된다")]
        [SerializeField] private List<CropSO> crops = new();

        [Tooltip("갈아둔 땅·젖은 땅 타일 (괭이·물뿌리개·작물 설정에서 모은다)")]
        [SerializeField] private List<TileBase> tiles = new();

        // ════════════════════════════════════════════════════════════
        //  저장 형식
        // ════════════════════════════════════════════════════════════

        [Serializable]
        private class TileEntry
        {
            public int x, y, z;
            public string tile;       // 빈 문자열 = 타일을 지운 칸
        }

        [Serializable]
        private class WetEntry
        {
            public int x, y, z;
            public float daysLeft;    // 마르기까지 남은 인게임 일수
            public string tile;       // 젖은 땅 타일 이름
        }

        [Serializable]
        private class CropEntry
        {
            public string crop;       // CropSO 에셋 이름
            public int x, y, z;       // 차지한 영역의 좌하단 칸
            public int stage;
            public float time;
            public bool wilted;
        }

        [Serializable]
        private class NodeEntry
        {
            public string id;         // 종류 + 자리. 예) T1250,-300 = (12.5, -3) 에 있는 나무, F... = 풀숲
            public bool gone;         // 베어서/캐서 없어졌다 (다시 안 자라는 것)
            public bool waiting;      // 그루터기 · 캔 상태 — 다시 자라기를 기다리는 중
            public float daysLeft;    // 다시 자라기까지 남은 인게임 일수
            public int health;        // 나무의 남은 체력
        }

        [Serializable]
        private class FarmData
        {
            public int version = 2;
            public List<TileEntry> tiles = new();
            public List<WetEntry> wet = new();
            public List<CropEntry> crops = new();
            public List<NodeEntry> nodes = new();
        }

        // ════════════════════════════════════════════════════════════

        private CropManager _mgr;
        private bool _ready;             // 불러오기가 끝나야 저장한다 (빈 밭으로 저장본을 덮어쓰지 않게)
        private float _nextSave;
        private int _lastCropCount;
        private string _lastJson;

        private readonly Dictionary<Vector3Int, TileBase> _original = new();
        private readonly Dictionary<string, CropSO> _cropByName = new();
        private readonly Dictionary<string, TileBase> _tileByName = new();

        // 씬의 나무·풀숲 — 자리로 만든 이름표 → 오브젝트. 없어지면 값이 null 이 된다
        private readonly Dictionary<string, TreeNode> _trees = new();
        private readonly Dictionary<string, ForageNode> _forages = new();

        // 베어서/캐서 없어진 것의 이름표 (다시 안 자라는 것). 부서진 뒤엔 물어볼 수 없어서 따로 기억한다.
        // ★ 다른 스크립트가 없앤 건 여기 안 들어간다 — 그런 건 저장하지 않고, 다음에 켜면 원래대로 둔다
        private readonly HashSet<string> _goneIds = new();

        private string Key => KeyPrefix + gameObject.scene.name;

        /// <summary>
        /// CropManager.CurrentGameDays 가 실제 시계보다 뒤처진 만큼 (인게임 일수).
        /// 게임을 켠 직후 몇 초 동안은 0 이었다가 시계 값으로 뛰어오르기 때문에,
        /// 그 사이에 '며칠 남음' 을 넣고 빼면 이만큼 어긋난다. 평소에는 0 이다
        /// </summary>
        private float ClockLag
            => _mgr != null && _mgr.GameClock != null ? Mathf.Max(0f, _mgr.GameClock.TotalGameDays - _mgr.CurrentGameDays) : 0f;

        // ════════════════════════════════════════════════════════════
        //  시작 · 자동 저장
        // ════════════════════════════════════════════════════════════

        private IEnumerator Start()
        {
            _mgr = CropManager.Instance;

            if (_mgr == null || _mgr.GroundTilemap == null)
            {
                Debug.LogWarning("[밭 저장] CropManager 나 바닥 타일맵이 없어 저장을 끕니다.", this);
                enabled = false;
                yield break;
            }

            // 1) 씬에 원래 깔려 있던 바닥을 기억해 둔다. 저장할 때 '바뀐 칸' 만 골라내는 기준이 된다
            SnapshotOriginal();

            // 2) 씬의 나무·풀숲에 자리로 이름표를 붙인다. 뭔가 흔들리기 시작하기 전인 지금 해야 자리가 정확하다
            CollectNodes();

            TreeNode.AnyStateChanged += HandleNodeChanged;
            ForageNode.AnyStateChanged += HandleNodeChanged;

            // 3) 한 프레임 기다린다. 씬에 미리 놓아둔 작물이 자리를 잡은 뒤에 불러와야 서로 안 겹친다
            yield return null;

            if (loadOnStart) Load();

            _mgr.OnHarvested += HandleHarvested;

            _lastCropCount = _mgr.Crops.Count;
            _nextSave = Time.unscaledTime + autoSaveInterval;
            _ready = true;
        }

        private void OnDestroy()
        {
            if (_mgr != null) _mgr.OnHarvested -= HandleHarvested;

            TreeNode.AnyStateChanged -= HandleNodeChanged;
            ForageNode.AnyStateChanged -= HandleNodeChanged;
        }

        private void Update()
        {
            if (!_ready) return;

            // 심거나 뽑아서 작물 수가 바뀌면 1초 안에 저장한다.
            // 씨앗은 인벤토리에서 이미 빠졌는데 밭 저장이 늦으면, 그 사이에 꺼졌을 때 씨앗만 날아간다
            if (_mgr.Crops.Count != _lastCropCount)
            {
                _lastCropCount = _mgr.Crops.Count;
                SaveSoon();
            }

            if (Time.unscaledTime < _nextSave) return;

            _nextSave = Time.unscaledTime + autoSaveInterval;
            Save(false);
        }

        /// <summary>다회용 작물은 수확해도 작물 수가 안 바뀌어서 따로 듣는다</summary>
        private void HandleHarvested(ItemSO item, int amount, ItemQuality quality) => SaveSoon();

        /// <summary>1초 안에 저장한다 (심기 · 수확 · 나무 베기 · 풀숲 캐기)</summary>
        private void SaveSoon() => _nextSave = Mathf.Min(_nextSave, Time.unscaledTime + 1f);

        /// <summary>나무를 치거나 풀숲을 캤을 때. 없어지는 거면 부서지기 전인 지금 이름표를 기억해 둔다</summary>
        private void HandleNodeChanged(Component node)
        {
            bool gone = node is TreeNode t ? t.IsGone : node is ForageNode f && f.IsGone;

            if (gone)
            {
                foreach (KeyValuePair<string, TreeNode> kv in _trees)
                    if (ReferenceEquals(kv.Value, node)) _goneIds.Add(kv.Key);

                foreach (KeyValuePair<string, ForageNode> kv in _forages)
                    if (ReferenceEquals(kv.Value, node)) _goneIds.Add(kv.Key);
            }

            SaveSoon();
        }

        private void OnApplicationQuit()
        {
            if (_ready) Save(true);
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause && _ready) Save(true);
        }

        // ════════════════════════════════════════════════════════════
        //  저장
        // ════════════════════════════════════════════════════════════

        /// <summary>씬을 바꾸기 직전 등에 부르면 바로 저장된다. 안 불러도 몇 초마다 저장된다</summary>
        public static void SaveNow()
        {
            FarmSaveManager saver = FindFirstObjectByType<FarmSaveManager>();
            if (saver != null && saver._ready) saver.Save(true);
        }

        [ContextMenu("지금 저장")]
        private void SaveFromMenu()
        {
            if (!_ready)
            {
                Debug.Log("[밭 저장] 플레이 중에만 저장할 수 있습니다.", this);
                return;
            }

            Save(true);
        }

        private void Save(bool announce)
        {
            if (_mgr == null || _mgr.GroundTilemap == null) return;

            var data = new FarmData();

            // 1) 괭이로 바뀐 바닥. 젖은 칸은 '물 주기 전 타일' 로 본다 (젖음은 아래에서 따로 저장)
            foreach (Vector3Int cell in _mgr.GroundTilemap.cellBounds.allPositionsWithin)
            {
                TileBase now = _mgr.GetEffectiveGroundTile(cell);
                _original.TryGetValue(cell, out TileBase before);

                if (now == before) continue;

                data.tiles.Add(new TileEntry
                {
                    x = cell.x, y = cell.y, z = cell.z,
                    tile = now != null ? now.name : string.Empty,
                });
            }

            // 2) 젖은 칸
            float lag = ClockLag;

            foreach (Vector3Int cell in _mgr.WetCells)
            {
                TileBase wetTile = _mgr.GetGroundTile(cell);
                if (wetTile == null) continue;

                data.wet.Add(new WetEntry
                {
                    x = cell.x, y = cell.y, z = cell.z,
                    daysLeft = Mathf.Max(0f, _mgr.GetWetDaysLeft(cell) - lag),
                    tile = wetTile.name,
                });
            }

            // 3) 작물 — 칸을 실제로 차지한 것만 (막 수확돼서 사라지는 중인 작물은 빠진다)
            foreach (GrowCrop crop in _mgr.Crops)
            {
                if (crop == null || crop.Data == null || _mgr.GetOccupant(crop.OriginCell) != crop) continue;

                Vector3Int o = crop.OriginCell;

                data.crops.Add(new CropEntry
                {
                    crop = crop.Data.name,
                    x = o.x, y = o.y, z = o.z,
                    stage = crop.NowGrowthStage,
                    time = crop.CurrentTimeStage,
                    wilted = crop.IsWilted,
                });
            }

            // 4) 나무 · 풀숲 — 멀쩡한 건 안 적는다
            foreach (KeyValuePair<string, TreeNode> kv in _trees)
            {
                TreeNode tree = kv.Value;

                if (_goneIds.Contains(kv.Key) || (tree != null && tree.IsGone))
                {
                    data.nodes.Add(new NodeEntry { id = kv.Key, gone = true });
                    continue;
                }

                if (tree == null) continue;   // 다른 스크립트가 없앤 것 — 저장하지 않는다

                if (!tree.TryGetSaveState(out bool stump, out float daysLeft, out int health)) continue;

                data.nodes.Add(new NodeEntry { id = kv.Key, waiting = stump, daysLeft = daysLeft, health = health });
            }

            foreach (KeyValuePair<string, ForageNode> kv in _forages)
            {
                ForageNode forage = kv.Value;

                if (_goneIds.Contains(kv.Key) || (forage != null && forage.IsGone))
                {
                    data.nodes.Add(new NodeEntry { id = kv.Key, gone = true });
                    continue;
                }

                if (forage == null) continue;

                if (!forage.TryGetSaveState(out float daysLeft)) continue;

                data.nodes.Add(new NodeEntry { id = kv.Key, waiting = true, daysLeft = daysLeft });
            }

            string json = JsonUtility.ToJson(data);

            // 바뀐 게 없으면 디스크에 다시 쓰지 않는다
            if (json != _lastJson)
            {
                PlayerPrefs.SetString(Key, json);
                PlayerPrefs.Save();
                _lastJson = json;
            }

            if (announce && verboseLog)
                Debug.Log($"[밭 저장] 저장했습니다 — 바뀐 칸 {data.tiles.Count} · 젖은 칸 {data.wet.Count} · 작물 {data.crops.Count} · 나무·풀숲 {data.nodes.Count}", this);
        }

        // ════════════════════════════════════════════════════════════
        //  불러오기
        // ════════════════════════════════════════════════════════════

        private void Load()
        {
            string json = PlayerPrefs.GetString(Key, string.Empty);

            if (string.IsNullOrEmpty(json))
            {
                if (verboseLog) Debug.Log("[밭 저장] 저장본이 없어 빈 밭으로 시작합니다.", this);
                return;
            }

            FarmData data;
            try
            {
                data = JsonUtility.FromJson<FarmData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[밭 저장] 저장본을 읽지 못해 빈 밭으로 시작합니다: " + e.Message, this);
                return;
            }

            if (data == null) return;

            BuildLookups();

            int missing = 0;
            int restored = 0;

            // 1) 바닥
            if (data.tiles != null)
            {
                foreach (TileEntry t in data.tiles)
                {
                    TileBase tile = null;

                    if (!string.IsNullOrEmpty(t.tile) && !_tileByName.TryGetValue(t.tile, out tile))
                    {
                        missing++;
                        continue;
                    }

                    _mgr.SetGroundTile(new Vector3Int(t.x, t.y, t.z), tile);
                }
            }

            // 2) 젖은 칸 — 바닥을 먼저 되돌린 다음에 해야 '물 주기 전 타일' 이 제대로 기억된다
            if (data.wet != null)
            {
                float lag = ClockLag;   // 켠 직후라 CropManager 날짜가 아직 0 이면, 그만큼 더해 둬야 제때 마른다

                foreach (WetEntry w in data.wet)
                {
                    if (w.daysLeft <= 0f || string.IsNullOrEmpty(w.tile)) continue;

                    if (!_tileByName.TryGetValue(w.tile, out TileBase wetTile))
                    {
                        missing++;
                        continue;
                    }

                    _mgr.SetCellWet(new Vector3Int(w.x, w.y, w.z), wetTile, w.daysLeft + lag);
                }
            }

            // 3) 작물
            if (data.crops != null)
            {
                foreach (CropEntry c in data.crops)
                {
                    if (string.IsNullOrEmpty(c.crop) || !_cropByName.TryGetValue(c.crop, out CropSO so))
                    {
                        missing++;
                        continue;
                    }

                    GrowCrop grow = _mgr.RestoreCrop(so, new Vector3Int(c.x, c.y, c.z));
                    if (grow == null) continue;   // 그 자리가 이미 차 있다 (씬에 미리 놓아둔 작물 등)

                    grow.LoadState(c.stage, c.time);
                    if (c.wilted) grow.Wilt();

                    restored++;
                }
            }

            // 4) 나무 · 풀숲. 못 찾은 건(씬에서 옮겼거나 지운 것) 조용히 넘어간다 — 처음 상태로 시작
            int nodes = 0;

            if (data.nodes != null)
            {
                foreach (NodeEntry n in data.nodes)
                {
                    if (string.IsNullOrEmpty(n.id)) continue;

                    if (_trees.TryGetValue(n.id, out TreeNode tree) && tree != null)
                    {
                        tree.LoadSaveState(n.gone, n.waiting, n.daysLeft, n.health);
                    }
                    else if (_forages.TryGetValue(n.id, out ForageNode forage) && forage != null)
                    {
                        forage.LoadSaveState(n.gone, n.waiting, n.daysLeft);
                    }
                    else
                    {
                        continue;
                    }

                    if (n.gone) _goneIds.Add(n.id);
                    nodes++;
                }
            }

            _lastJson = json;

            if (verboseLog)
                Debug.Log($"[밭 저장] 불러왔습니다 — 바뀐 칸 {data.tiles?.Count ?? 0} · 젖은 칸 {data.wet?.Count ?? 0} · 작물 {restored} · 나무·풀숲 {nodes}", this);

            if (missing > 0)
                Debug.LogWarning($"[밭 저장] {missing}개를 되살리지 못했습니다 (작물이나 타일을 못 찾음). " +
                                 "FarmSaveManager ⋮ → '목록 새로고침' 을 누르고 씬을 저장해 주세요.", this);
        }

        /// <summary>씬에 원래 깔려 있던 바닥 타일을 기억한다</summary>
        private void SnapshotOriginal()
        {
            _original.Clear();

            Tilemap map = _mgr.GroundTilemap;

            foreach (Vector3Int cell in map.cellBounds.allPositionsWithin)
            {
                TileBase tile = map.GetTile(cell);
                if (tile != null) _original[cell] = tile;
            }
        }

        /// <summary>씬의 나무·풀숲을 모아서 자리로 이름표를 붙인다. 꺼져 있는 것도 포함한다</summary>
        private void CollectNodes()
        {
            _trees.Clear();
            _forages.Clear();
            _goneIds.Clear();

            AddNodes(_trees, "T", FindObjectsByType<TreeNode>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            AddNodes(_forages, "F", FindObjectsByType<ForageNode>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        }

        /// <summary>
        /// 이름표 = 종류 + 자리 (소수 둘째 자리까지). 나무는 안 움직이니 자리가 곧 이름이다.
        /// 같은 자리에 겹쳐 놓인 것(복제만 하고 안 옮긴 것 등)은 하이어라키 순서대로 #2, #3 을 붙인다
        /// </summary>
        private void AddNodes<T>(Dictionary<string, T> into, string prefix, T[] found) where T : Component
        {
            if (found == null) return;

            var list = new List<KeyValuePair<string, T>>();

            foreach (T node in found)
            {
                // 이 씬에 있는 것만 (다른 씬 · DontDestroyOnLoad 에 있는 건 이 씬 저장본에 안 넣는다)
                if (node != null && node.gameObject.scene == gameObject.scene)
                    list.Add(new KeyValuePair<string, T>(HierarchyKey(node.transform), node));
            }

            list.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));

            foreach (KeyValuePair<string, T> pair in list)
            {
                Vector3 p = pair.Value.transform.position;

                string baseId = prefix
                              + Mathf.RoundToInt(p.x * 100f).ToString(CultureInfo.InvariantCulture) + ","
                              + Mathf.RoundToInt(p.y * 100f).ToString(CultureInfo.InvariantCulture);

                string id = baseId;
                for (int n = 2; into.ContainsKey(id); n++) id = baseId + "#" + n;

                into.Add(id, pair.Value);
            }
        }

        /// <summary>하이어라키에서의 위치. 예) 00003/00012/ = 4번째 루트 오브젝트의 13번째 자식</summary>
        private static string HierarchyKey(Transform t)
        {
            var sb = new StringBuilder();

            for (; t != null; t = t.parent)
                sb.Insert(0, t.GetSiblingIndex().ToString("D5", CultureInfo.InvariantCulture) + "/");

            return sb.ToString();
        }

        /// <summary>저장본에 적힌 이름 → 실제 에셋. 목록에 있는 걸 먼저 쓰고, 없으면 메모리에 올라온 것에서 찾는다</summary>
        private void BuildLookups()
        {
            _cropByName.Clear();
            _tileByName.Clear();

            foreach (CropSO c in crops) AddCrop(c);
            foreach (TileBase t in tiles) AddTile(t);

            // 목록을 새로고침 안 했을 때의 보험 — 이미 메모리에 올라와 있는 에셋에서 찾는다
            foreach (CropSO c in Resources.FindObjectsOfTypeAll<CropSO>()) AddCrop(c);

            // 작물이 심길 수 있는 타일 = 갈아둔 땅
            foreach (CropSO c in _cropByName.Values)
            {
                if (c.plantableTiles == null) continue;
                foreach (TileBase t in c.plantableTiles) AddTile(t);
            }

            foreach (TileBase t in _original.Values) AddTile(t);
            foreach (TileBase t in Resources.FindObjectsOfTypeAll<TileBase>()) AddTile(t);
        }

        private void AddCrop(CropSO crop)
        {
            if (crop != null && !_cropByName.ContainsKey(crop.name)) _cropByName.Add(crop.name, crop);
        }

        private void AddTile(TileBase tile)
        {
            if (tile != null && !_tileByName.ContainsKey(tile.name)) _tileByName.Add(tile.name, tile);
        }

        // ════════════════════════════════════════════════════════════
        //  저장 지우기
        // ════════════════════════════════════════════════════════════

        [ContextMenu("저장 지우기 (밭·나무·풀숲 처음 상태로)")]
        private void DeleteSaveFromMenu()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();

            // 플레이 중이면 이번 판은 더 저장하지 않는다. 안 그러면 끌 때 다시 저장돼서 지운 게 되살아난다
            _ready = false;
            _lastJson = null;

            Debug.Log($"[밭 저장] '{gameObject.scene.name}' 의 밭·나무·풀숲 저장본을 지웠습니다.", this);
        }

        /// <summary>새 게임을 시작할 때 부른다. 씬 이름을 주면 그 씬의 밭 저장본을 지운다</summary>
        public static void DeleteSave(string sceneName)
        {
            PlayerPrefs.DeleteKey(KeyPrefix + sceneName);
            PlayerPrefs.Save();
        }

        // ════════════════════════════════════════════════════════════
        //  에디터 — 작물·타일 목록 자동 채우기
        // ════════════════════════════════════════════════════════════

        [ContextMenu("목록 새로고침 (작물·타일)")]
        private void RefreshLists()
        {
#if UNITY_EDITOR
            var newCrops = FindAssets<CropSO>();
            var newTiles = new List<TileBase>();

            foreach (CropSO c in newCrops)
            {
                if (c.plantableTiles == null) continue;
                foreach (TileBase t in c.plantableTiles) AddUnique(newTiles, t);
            }

            foreach (HoeSO hoe in FindAssets<HoeSO>()) AddUnique(newTiles, hoe.tilledTile);
            foreach (WateringCanSO can in FindAssets<WateringCanSO>()) AddUnique(newTiles, can.wetTile);

            if (SameList(crops, newCrops) && SameList(tiles, newTiles)) return;

            crops = newCrops;
            tiles = newTiles;

            EditorUtility.SetDirty(this);
            if (gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(gameObject.scene);

            Debug.Log($"[밭 저장] 목록을 새로고침했습니다 — 작물 {crops.Count}개 · 타일 {tiles.Count}개. 씬을 저장해 주세요.", this);
#endif
        }

#if UNITY_EDITOR
        private void Reset() => RefreshLists();

        private void OnValidate()
        {
            if (Application.isPlaying) return;

            // OnValidate 안에서 바로 에셋을 뒤지면 경고가 날 수 있어서 한 박자 늦춘다
            EditorApplication.delayCall -= DelayedRefresh;
            EditorApplication.delayCall += DelayedRefresh;
        }

        private void DelayedRefresh()
        {
            if (this != null && !Application.isPlaying) RefreshLists();
        }

        private static List<T> FindAssets<T>() where T : UnityEngine.Object
        {
            var list = new List<T>();

            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
            {
                T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null && !list.Contains(asset)) list.Add(asset);
            }

            return list;
        }

        private static void AddUnique(List<TileBase> list, TileBase tile)
        {
            if (tile != null && !list.Contains(tile)) list.Add(tile);
        }

        private static bool SameList<T>(List<T> a, List<T> b) where T : UnityEngine.Object
        {
            if (a == null || b == null || a.Count != b.Count) return false;

            for (int i = 0; i < a.Count; i++)
                if (a[i] != b[i]) return false;

            return true;
        }
#endif
    }
}