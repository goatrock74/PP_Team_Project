using System;
using System.Text;
using UnityEngine;
 
/// <summary>
/// 레벨마다 조금씩 쌓이는 수치 한 줄.
/// </summary>
/// <remarks>
/// 구조체라서 필드 초기화식(= 0.02f)을 못 쓴다. 기본값은 MasterySO.OnValidate 에서 넣는다.
/// </remarks>
[Serializable]
public struct MasteryStatGrowth
{
    public MasteryStat stat;
 
    [Tooltip("레벨 1당 오르는 양. 0.01 = 레벨당 +1%. 20렙이면 +20%")]
    public float perLevel;
}
 
/// <summary>
/// 특정 레벨에서 한 번 켜지는 특수 능력 한 줄.
/// </summary>
[Serializable]
public struct MasteryUnlock
{
    [Tooltip("이 레벨을 찍으면 켜진다")]
    [Min(1)] public int level;
 
    public MasteryPerk perk;
 
    [Tooltip("카드에 표시할 이름. 예: 빠른 재생")]
    public string title;
 
    [TextArea(1, 3)]
    [Tooltip("카드에 표시할 설명")]
    public string description;
 
    [Tooltip("배수가 필요한 퍽만 쓴다. 재생 1.5배 → 1.5, 채집량 2배 → 2")]
    public float value;
}
 
/// <summary>
/// 마스터리 하나의 <b>설계도</b>. 채집·농사·벌목·낚시마다 에셋을 하나씩 만든다.
///
/// ★ 여기에는 "레벨 몇이면 뭐가 좋아지는가" 라는 규칙만 들어간다.
///   지금 몇 레벨인지, 경험치가 얼마인지 같은 <b>진행 상황은 절대 넣지 않는다.</b>
///   SO 는 에디터에서만 값이 저장되고 빌드에서는 안 되기 때문에,
///   여기에 현재 레벨을 넣으면 빌드에서 저장이 안 되고 에디터에서는 리셋이 안 된다.
///   진행 상황은 MasteryProgress 가 들고 있다.
/// </summary>
[CreateAssetMenu(fileName = "MasterySO", menuName = "SO/Mastery")]
public class MasterySO : ScriptableObject
{
    [Header("정체")]
    public MasteryType type;
 
    [Tooltip("카드에 표시할 이름. 비우면 종류 이름을 쓴다")]
    public string displayName;
 
    public Sprite icon;
 
    [TextArea(2, 4)]
    public string description;
 
    [Header("레벨")]
    [Tooltip("만렙. 여기 도달하면 카드에 Level: Max 로 뜬다")]
    [Min(1)] public int maxLevel = 20;
 
    [Header("경험치 곡선")]
    [Tooltip("1 → 2 레벨에 필요한 경험치")]
    [Min(1)] public int baseExp = 100;
 
    [Tooltip("레벨마다 필요 경험치에 곱해지는 값. 1.25 면 레벨마다 25%씩 더 필요해진다")]
    [Min(1f)] public float expGrowth = 1.25f;
 
    [Header("레벨당 상승 수치")]
    [Tooltip("매 레벨 조금씩 쌓이는 것들. 여러 줄 넣어도 된다")]
    public MasteryStatGrowth[] statsPerLevel;
 
    [Header("특수 능력 해금")]
    [Tooltip("보통 10렙 / 15렙 두 줄")]
    public MasteryUnlock[] unlocks;
 
    public string Title => string.IsNullOrWhiteSpace(displayName)
        ? MasteryUtil.DisplayName(type)
        : displayName;
 
    // ════════════════════════════════════════════════════════════
    //  경험치
    // ════════════════════════════════════════════════════════════
 
    /// <summary>level → level+1 에 필요한 경험치. 만렙이면 0</summary>
    public int ExpToNext(int level)
    {
        if (level >= maxLevel) return 0;
 
        // 1렙에서 baseExp, 이후 레벨마다 expGrowth 배씩
        float need = baseExp * Mathf.Pow(expGrowth, Mathf.Max(0, level - 1));
        return Mathf.Max(1, Mathf.RoundToInt(need));
    }
 
    public bool IsMaxLevel(int level) => level >= maxLevel;
 
    // ════════════════════════════════════════════════════════════
    //  수치 조회
    // ════════════════════════════════════════════════════════════
 
    /// <summary>이 마스터리가 해당 레벨에서 주는 스탯 값. 없는 스탯이면 0</summary>
    public float StatAt(MasteryStat stat, int level)
    {
        if (statsPerLevel == null) return 0f;
 
        float sum = 0f;
 
        foreach (MasteryStatGrowth g in statsPerLevel)
            if (g.stat == stat) sum += g.perLevel * level;
 
        return sum;
    }
 
    /// <summary>해당 레벨에서 이 퍽이 켜졌는가</summary>
    public bool HasPerk(MasteryPerk perk, int level)
    {
        if (perk == MasteryPerk.None || unlocks == null) return false;
 
        foreach (MasteryUnlock u in unlocks)
            if (u.perk == perk && level >= u.level) return true;
 
        return false;
    }
 
    /// <summary>켜진 퍽의 배수. 안 켜졌으면 fallback</summary>
    public float PerkValue(MasteryPerk perk, int level, float fallback)
    {
        if (perk == MasteryPerk.None || unlocks == null) return fallback;
 
        foreach (MasteryUnlock u in unlocks)
            if (u.perk == perk && level >= u.level) return u.value;
 
        return fallback;
    }
 
    // ════════════════════════════════════════════════════════════
    //  카드 표시용 텍스트
    // ════════════════════════════════════════════════════════════
 
    /// <summary>
    /// 카드의 "추가 능력치들 나열" 칸에 그대로 넣을 문자열.
    /// 현재 수치 + 해금된 퍽(초록) + 아직 잠긴 퍽(회색)을 함께 보여준다.
    /// </summary>
    public string BuildStatText(int level)
    {
        var sb = new StringBuilder();
 
        // 1) 지금 쌓여 있는 수치들
        if (statsPerLevel != null)
        {
            foreach (MasteryStatGrowth g in statsPerLevel)
            {
                if (Mathf.Approximately(g.perLevel, 0f)) continue;
 
                float now = g.perLevel * level;
                sb.Append($"{MasteryUtil.StatLabel(g.stat)} <b>{MasteryUtil.FormatStat(g.stat, now)}</b>\n");
            }
        }
 
        if (unlocks == null || unlocks.Length == 0) return sb.ToString().TrimEnd();
 
        // 2) 특수 능력 — 레벨 순서대로
        var sorted = (MasteryUnlock[])unlocks.Clone();
        Array.Sort(sorted, (a, b) => a.level.CompareTo(b.level));
 
        foreach (MasteryUnlock u in sorted)
        {
            if (u.perk == MasteryPerk.None) continue;
 
            bool unlocked = level >= u.level;
            string color = unlocked ? "8FE08F" : "777777";
            string mark = unlocked ? "◆" : "◇";
            string body = string.IsNullOrWhiteSpace(u.description) ? u.title : u.description;
 
            sb.Append($"\n<color=#{color}>{mark} Lv{u.level} {u.title}</color>");
 
            if (!string.IsNullOrWhiteSpace(body) && body != u.title)
                sb.Append($"\n<size=85%><color=#{color}>   {body}</color></size>");
        }
 
        return sb.ToString().TrimEnd();
    }
 
    // ════════════════════════════════════════════════════════════
 
    private void OnValidate()
    {
        maxLevel = Mathf.Max(1, maxLevel);
        baseExp = Mathf.Max(1, baseExp);
        expGrowth = Mathf.Max(1f, expGrowth);
 
        // 구조체는 필드 초기화식을 못 쓰니 기본값을 여기서 채운다
        if (unlocks != null)
        {
            for (int i = 0; i < unlocks.Length; i++)
            {
                MasteryUnlock u = unlocks[i];
 
                if (u.level <= 0) u.level = 10;
                if (u.value <= 0f) u.value = 1f;      // 0 배수는 사고로 이어진다
                if (u.level > maxLevel) u.level = maxLevel;
 
                unlocks[i] = u;
            }
        }
 
        if (statsPerLevel == null) return;
 
        for (int i = 0; i < statsPerLevel.Length; i++)
        {
            MasteryStatGrowth g = statsPerLevel[i];
 
            if (Mathf.Approximately(g.perLevel, 0f)) g.perLevel = 0.01f;   // 레벨당 +1%
 
            statsPerLevel[i] = g;
        }
    }
}