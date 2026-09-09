using System;
using System.Collections.Generic;
using UnityEngine;
 
/// <summary>
/// 마스터리(채집·농사·벌목·낚시) 레벨을 관리하는 유일한 창구.
///
/// 다른 시스템은 이 클래스에게 <b>숫자만 물어본다.</b>
///     float luck = MasteryManager.Stat(MasteryStat.ForageLuck);
///     bool  best = MasteryManager.Perk(MasteryPerk.UnlockBestQuality);
///     float mul  = MasteryManager.PerkVal(MasteryPerk.BonusForageYield, 1f);
///
/// 위 세 개는 <b>static</b> 이라서 씬에 MasteryManager 가 없어도 안전하다.
/// 없으면 각각 0 / false / fallback 이 나와서 마스터리가 없던 때와 똑같이 동작한다.
/// 덕분에 친구들 씬에 이 매니저를 안 넣어도 아무것도 안 깨진다.
///
/// 씬 배치: 빈 오브젝트 하나 만들어서 붙이고, Masteries 에 SO 4개를 넣으면 끝.
/// </summary>
public class MasteryManager : MonoBehaviour
{
    private static MasteryManager _instance;
 
    public static MasteryManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<MasteryManager>(FindObjectsInactive.Include);
 
            return _instance;
        }
    }
 
    [Header("마스터리 목록")]
    [Tooltip("채집 / 농사 / 벌목 / 낚시 SO 를 넣는다. 순서가 곧 스탯 화면의 카드 순서")]
    [SerializeField] private MasterySO[] masteries;
 
    [Header("경험치 획득량")]
    [Tooltip("농사 — 수확물 판매가 1G 당 몇 경험치인가")]
    [SerializeField, Min(0f)] private float expPerGold = 1f;
 
    [Tooltip("벌목 — 나무 최대 체력 1당 몇 경험치인가")]
    [SerializeField, Min(0f)] private float expPerTreeHealth = 15f;
 
    [Tooltip("채집 · 낚시 — 등급별 경험치. 비워두면 기본값이 들어간다")]
    [SerializeField] private RarityExp[] rarityExp;
 
    [Header("디버그")]
    [Tooltip("경험치가 들어올 때마다 콘솔에 찍는다")]
    [SerializeField] private bool verboseLog;
 
    [Tooltip("플레이 중 진행 상황 확인용. 여기서 직접 고쳐도 반영된다")]
    [SerializeField] private List<MasteryProgress> progress = new();
 
    private readonly Dictionary<MasteryType, MasterySO> _defs = new();
    private readonly Dictionary<MasteryType, MasteryProgress> _byType = new();
 
    /// <summary>레벨이나 경험치가 바뀌었을 때. 스탯 화면이 구독한다</summary>
    public event Action<MasteryType> OnChanged;
 
    /// <summary>레벨업 했을 때 (설계도, 새 레벨). 연출·사운드가 구독하면 된다</summary>
    public event Action<MasterySO, int> OnLevelUp;
 
    /// <summary>인스펙터에 넣은 순서 그대로. 스탯 화면이 카드를 만들 때 쓴다</summary>
    public IReadOnlyList<MasterySO> Definitions => masteries;
 
    // ════════════════════════════════════════════════════════════
    //  수명 주기
    // ════════════════════════════════════════════════════════════
 
    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
 
        Rebuild();
    }
 
    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
 
    /// <summary>SO 목록과 진행 상황을 맞춘다. SO 를 추가해도 기존 진행은 유지된다</summary>
    private void Rebuild()
    {
        _defs.Clear();
        _byType.Clear();
 
        if (masteries == null) return;
 
        foreach (MasterySO def in masteries)
        {
            if (def == null) continue;
 
            if (_defs.ContainsKey(def.type))
            {
                Debug.LogWarning($"[마스터리] {def.type} SO 가 두 개 이상 들어있습니다. 하나만 남겨주세요.", def);
                continue;
            }
 
            _defs[def.type] = def;
        }
 
        // 이미 있는 진행 상황을 먼저 연결
        foreach (MasteryProgress p in progress)
        {
            if (p == null || !_defs.ContainsKey(p.type)) continue;
 
            p.level = Mathf.Clamp(p.level, 1, _defs[p.type].maxLevel);
            _byType[p.type] = p;
        }
 
        // 빠진 것만 새로 만든다
        foreach (KeyValuePair<MasteryType, MasterySO> kv in _defs)
        {
            if (_byType.ContainsKey(kv.Key)) continue;
 
            var fresh = new MasteryProgress(kv.Key);
            progress.Add(fresh);
            _byType[kv.Key] = fresh;
        }
    }
 
    // ════════════════════════════════════════════════════════════
    //  조회 — 다른 시스템이 쓰는 API
    // ════════════════════════════════════════════════════════════
 
    public MasterySO GetDefinition(MasteryType type)
        => _defs.TryGetValue(type, out MasterySO def) ? def : null;
 
    public MasteryProgress GetProgress(MasteryType type)
        => _byType.TryGetValue(type, out MasteryProgress p) ? p : null;
 
    public int GetLevel(MasteryType type) => GetProgress(type)?.level ?? 1;
 
    public bool IsMaxLevel(MasteryType type)
    {
        MasterySO def = GetDefinition(type);
        return def != null && def.IsMaxLevel(GetLevel(type));
    }
 
    /// <summary>0~1 사이의 다음 레벨까지 진행률. 만렙이면 1</summary>
    public float GetExpRatio(MasteryType type)
    {
        MasterySO def = GetDefinition(type);
        MasteryProgress p = GetProgress(type);
 
        if (def == null || p == null) return 0f;
        if (def.IsMaxLevel(p.level)) return 1f;
 
        int need = def.ExpToNext(p.level);
        return need <= 0 ? 1f : Mathf.Clamp01((float)p.exp / need);
    }
 
    /// <summary>
    /// ★ 모든 마스터리를 <b>합산한</b> 스탯 값.
    /// 두 마스터리가 같은 스탯을 준다면 자동으로 더해진다.
    /// </summary>
    public float GetStat(MasteryStat stat)
    {
        float sum = 0f;
 
        foreach (KeyValuePair<MasteryType, MasterySO> kv in _defs)
            sum += kv.Value.StatAt(stat, GetLevel(kv.Key));
 
        return sum;
    }
 
    /// <summary>어느 마스터리든 이 퍽을 해금했는가</summary>
    public bool HasPerk(MasteryPerk perk)
    {
        foreach (KeyValuePair<MasteryType, MasterySO> kv in _defs)
            if (kv.Value.HasPerk(perk, GetLevel(kv.Key))) return true;
 
        return false;
    }
 
    /// <summary>해금된 퍽의 배수. 안 켜졌으면 fallback</summary>
    public float GetPerkValue(MasteryPerk perk, float fallback)
    {
        foreach (KeyValuePair<MasteryType, MasterySO> kv in _defs)
            if (kv.Value.HasPerk(perk, GetLevel(kv.Key)))
                return kv.Value.PerkValue(perk, GetLevel(kv.Key), fallback);
 
        return fallback;
    }
 
    // ════════════════════════════════════════════════════════════
    //  static 편의 함수 — 매니저가 없어도 안전하다
    // ════════════════════════════════════════════════════════════
 
    public static float Stat(MasteryStat stat)
        => Instance != null ? Instance.GetStat(stat) : 0f;
 
    public static bool Perk(MasteryPerk perk)
        => Instance != null && Instance.HasPerk(perk);
 
    public static float PerkVal(MasteryPerk perk, float fallback)
        => Instance != null ? Instance.GetPerkValue(perk, fallback) : fallback;
 
    public static int Level(MasteryType type)
        => Instance != null ? Instance.GetLevel(type) : 1;
 
    // ════════════════════════════════════════════════════════════
    //  경험치 지급
    // ════════════════════════════════════════════════════════════
 
    /// <summary>경험치를 준다. 레벨업했으면 true</summary>
    public bool AddExp(MasteryType type, int amount)
    {
        if (amount <= 0) return false;
 
        MasterySO def = GetDefinition(type);
        MasteryProgress p = GetProgress(type);
 
        if (def == null || p == null) return false;
        if (def.IsMaxLevel(p.level)) return false;      // 만렙은 더 안 쌓는다
 
        p.exp += amount;
 
        bool leveled = false;
 
        // while + 빼기: 한 번에 큰 경험치가 들어와도 여러 레벨이 정확히 오른다
        // (작물 성장 Tick 과 같은 방식)
        while (!def.IsMaxLevel(p.level))
        {
            int need = def.ExpToNext(p.level);
            if (need <= 0 || p.exp < need) break;
 
            p.exp -= need;
            p.level++;
            leveled = true;
 
            OnLevelUp?.Invoke(def, p.level);
            Debug.Log($"[마스터리] {def.Title} 레벨 {p.level} 달성!");
        }
 
        if (def.IsMaxLevel(p.level)) p.exp = 0;         // 만렙은 경험치 표시 안 함
 
        if (verboseLog)
            Debug.Log($"[마스터리] {def.Title} +{amount} exp  →  Lv{p.level} ({p.exp}/{def.ExpToNext(p.level)})");
 
        OnChanged?.Invoke(type);
        return leveled;
    }
 
    /// <summary>등급 기반 경험치 — 채집물, 물고기</summary>
    public bool AddRarityExp(MasteryType type, ItemRarity rarity, int count = 1)
        => AddExp(type, ExpForRarity(rarity) * Mathf.Max(1, count));
 
    /// <summary>가격 기반 경험치 — 수확물</summary>
    public bool AddValueExp(MasteryType type, int totalGold)
        => AddExp(type, Mathf.RoundToInt(Mathf.Max(0, totalGold) * expPerGold));
 
    /// <summary>체력 기반 경험치 — 나무</summary>
    public bool AddHealthExp(MasteryType type, int health)
        => AddExp(type, Mathf.RoundToInt(Mathf.Max(0, health) * expPerTreeHealth));
 
    public int ExpForRarity(ItemRarity rarity)
    {
        if (rarityExp != null)
            foreach (RarityExp r in rarityExp)
                if (r.rarity == rarity) return Mathf.Max(0, r.exp);
 
        return DefaultRarityExp(rarity);
    }
 
    // ── static 편의 버전 (매니저가 없으면 조용히 무시) ──
 
    public static void Gain(MasteryType type, int amount)
        => Instance?.AddExp(type, amount);
 
    public static void GainByRarity(MasteryType type, ItemRarity rarity, int count = 1)
        => Instance?.AddRarityExp(type, rarity, count);
 
    public static void GainByValue(MasteryType type, int totalGold)
        => Instance?.AddValueExp(type, totalGold);
 
    public static void GainByHealth(MasteryType type, int health)
        => Instance?.AddHealthExp(type, health);
 
    // ════════════════════════════════════════════════════════════
    //  세이브 / 로드
    // ════════════════════════════════════════════════════════════
 
    public string ToJson() => JsonUtility.ToJson(new MasterySaveData { entries = progress.ToArray() });
 
    public void LoadJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;
 
        MasterySaveData data = JsonUtility.FromJson<MasterySaveData>(json);
        if (data?.entries == null) return;
 
        progress.Clear();
        progress.AddRange(data.entries);
 
        Rebuild();
 
        foreach (MasteryType t in _byType.Keys) OnChanged?.Invoke(t);
    }
 
    // ════════════════════════════════════════════════════════════
    //  테스트용 — 플레이 중 컴포넌트 우클릭
    // ════════════════════════════════════════════════════════════
 
    [ContextMenu("전부 +1 레벨")]
    private void DebugLevelUpAll()
    {
        foreach (KeyValuePair<MasteryType, MasterySO> kv in _defs)
            AddExp(kv.Key, kv.Value.ExpToNext(GetLevel(kv.Key)));
    }
 
    [ContextMenu("전부 만렙")]
    private void DebugMaxAll()
    {
        foreach (KeyValuePair<MasteryType, MasterySO> kv in _defs)
        {
            MasteryProgress p = GetProgress(kv.Key);
            if (p == null) continue;
 
            p.level = kv.Value.maxLevel;
            p.exp = 0;
 
            OnChanged?.Invoke(kv.Key);
        }
 
        Debug.Log("[마스터리] 전부 만렙으로 만들었습니다");
    }
 
    [ContextMenu("전부 초기화")]
    private void DebugResetAll()
    {
        foreach (MasteryProgress p in progress)
        {
            p.level = 1;
            p.exp = 0;
 
            OnChanged?.Invoke(p.type);
        }
 
        Debug.Log("[마스터리] 전부 1레벨로 되돌렸습니다");
    }
 
    [ContextMenu("현재 보정치 보기")]
    private void DebugDumpStats()
    {
        var sb = new System.Text.StringBuilder("───── 마스터리 현황 ─────\n");
 
        foreach (KeyValuePair<MasteryType, MasterySO> kv in _defs)
        {
            int lv = GetLevel(kv.Key);
            sb.Append($"{kv.Value.Title} Lv{lv}{(kv.Value.IsMaxLevel(lv) ? " (MAX)" : string.Empty)}\n");
        }
 
        sb.Append("\n[합산 스탯]\n");
        foreach (MasteryStat s in Enum.GetValues(typeof(MasteryStat)))
        {
            float v = GetStat(s);
            if (Mathf.Approximately(v, 0f)) continue;
 
            sb.Append($"  {MasteryUtil.StatLabel(s)} {MasteryUtil.FormatStat(s, v)}\n");
        }
 
        sb.Append("\n[해금된 퍽]\n");
        foreach (MasteryPerk p in Enum.GetValues(typeof(MasteryPerk)))
        {
            if (p == MasteryPerk.None || !HasPerk(p)) continue;
            sb.Append($"  {p}  (배수 {GetPerkValue(p, 1f)})\n");
        }
 
        Debug.Log(sb.Append("─────────────────────────").ToString(), this);
    }
 
    // ════════════════════════════════════════════════════════════
 
    private static int DefaultRarityExp(ItemRarity r) => r switch
    {
        ItemRarity.Uncommon => 25,
        ItemRarity.Rare => 60,
        ItemRarity.Epic => 150,
        ItemRarity.Legendary => 400,
        _ => 10,
    };
 
    private void OnValidate()
    {
        // 등급 표가 비어 있으면 기본값으로 채워준다
        if (rarityExp == null || rarityExp.Length == 0)
        {
            var all = (ItemRarity[])Enum.GetValues(typeof(ItemRarity));
            rarityExp = new RarityExp[all.Length];
 
            for (int i = 0; i < all.Length; i++)
                rarityExp[i] = new RarityExp { rarity = all[i], exp = DefaultRarityExp(all[i]) };
        }
 
        // 플레이 중 인스펙터에서 레벨을 직접 고쳤을 때 UI 를 갱신해준다
        if (!Application.isPlaying || _byType.Count == 0) return;
 
        foreach (MasteryProgress p in progress)
        {
            MasterySO def = GetDefinition(p.type);
            if (def == null) continue;
 
            p.level = Mathf.Clamp(p.level, 1, def.maxLevel);
            OnChanged?.Invoke(p.type);
        }
    }
}
 
/// <summary>등급별 경험치 한 줄</summary>
[Serializable]
public struct RarityExp
{
    public ItemRarity rarity;
 
    [Min(0)] public int exp;
}
 