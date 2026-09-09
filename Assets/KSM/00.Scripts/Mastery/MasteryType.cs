using UnityEngine;
 
/// <summary>
/// 마스터리 종류. 새 마스터리를 추가하려면 여기에 한 줄 넣고 MasterySO 에셋을 하나 만들면 된다.
/// </summary>
public enum MasteryType
{
    Foraging = 0,   // 채집
    Farming = 1,    // 농사
    Logging = 2,    // 벌목
    Fishing = 3,    // 낚시
}
 
/// <summary>
/// 레벨이 오를 때마다 <b>조금씩 계속 쌓이는</b> 수치들.
///
/// 퍽(MasteryPerk)과의 차이: 이건 1렙부터 20렙까지 매 레벨 조금씩 오르는 값이고,
/// 퍽은 특정 레벨에서 딱 한 번 켜지는 스위치다.
///
/// MasteryManager.GetStat() 은 <b>모든 마스터리를 합산</b>해서 돌려준다.
/// 그래서 두 마스터리가 같은 스탯을 준다면 자동으로 더해진다.
/// </summary>
public enum MasteryStat
{
    /// <summary>채집 행운. 확률표를 여러 번 굴려 더 희귀한 쪽을 고를 확률</summary>
    ForageLuck = 0,
 
    /// <summary>작물 성장 속도 증가율. 0.3 이면 +30%</summary>
    GrowthSpeed = 1,
 
    /// <summary>농사 행운. 좋음·최상 확률에 더해지는 값. 0.1 이면 +10%p</summary>
    FarmLuck = 2,
 
    /// <summary>벌목 데미지 증가율. 0.5 면 도끼 위력 +50%</summary>
    ChopDamage = 3,
 
    /// <summary>낚시 행운. 낚시 확률표에 쓰면 된다 (아직 미사용)</summary>
    FishLuck = 4,
}
 
/// <summary>
/// 특정 레벨에서 한 번 켜지는 특수 능력.
///
/// 값이 필요한 퍽(배수 등)은 MasteryUnlock.value 에 넣고
/// MasteryManager.GetPerkValue() 로 꺼내 쓴다. 코드에 1.5 같은 숫자를 박지 않기 위해서다.
///
/// 새 퍽을 추가하려면: 여기에 한 줄 → MasterySO 의 Unlocks 에 등록 →
/// 실제로 효과를 받을 코드에서 HasPerk / GetPerkValue 로 물어보기.
/// </summary>
public enum MasteryPerk
{
    None = 0,
 
    /// <summary>채집 10 — 채집물 재생 시간이 value 배 줄어든다 (1.5 면 2/3 시간)</summary>
    FastForageRespawn = 1,
 
    /// <summary>채집 15 — 채집량이 value 배 (2)</summary>
    BonusForageYield = 2,
 
    /// <summary>농사 10 — '최상' 등급 작물 수확 해금. 이전에는 '좋음'으로 강등된다</summary>
    UnlockBestQuality = 3,
 
    /// <summary>농사 15 — 수확량이 value 배 (1.5)</summary>
    BonusHarvestYield = 4,
 
    // 벌목 10 / 15, 낚시 10 / 15 는 아직 미정.
    // 정해지면 여기에 줄을 추가하고 해당 시스템에서 HasPerk 로 물어보면 된다.
}
 
public static class MasteryUtil
{
    public static string DisplayName(MasteryType t) => t switch
    {
        MasteryType.Foraging => "채집",
        MasteryType.Farming => "농사",
        MasteryType.Logging => "벌목",
        MasteryType.Fishing => "낚시",
        _ => t.ToString(),
    };
 
    /// <summary>카드에 한 줄로 적을 이름. 예: "채집 행운 +12%"</summary>
    public static string StatLabel(MasteryStat s) => s switch
    {
        MasteryStat.ForageLuck => "채집 행운",
        MasteryStat.GrowthSpeed => "성장 속도",
        MasteryStat.FarmLuck => "농사 행운",
        MasteryStat.ChopDamage => "벌목 데미지",
        MasteryStat.FishLuck => "낚시 행운",
        _ => s.ToString(),
    };
 
    /// <summary>
    /// 값을 사람이 읽는 형태로. 지금은 전부 비율이라 퍼센트로 찍지만,
    /// 나중에 "+3칸" 같은 절대값 스탯이 생기면 여기서 갈라주면 된다.
    /// </summary>
    public static string FormatStat(MasteryStat s, float value) => $"+{value * 100f:0.#}%";
 
    public static Color TypeColor(MasteryType t) => t switch
    {
        MasteryType.Foraging => new Color(0.55f, 0.85f, 0.45f),   // 풀색
        MasteryType.Farming => new Color(0.98f, 0.78f, 0.35f),    // 밀색
        MasteryType.Logging => new Color(0.75f, 0.55f, 0.35f),    // 나무색
        MasteryType.Fishing => new Color(0.45f, 0.72f, 0.95f),    // 물색
        _ => Color.white,
    };
}