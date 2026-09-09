using UnityEngine;
using KSM._00.Scripts.Crop;
 
/// <summary>
/// 농사 마스터리를 작물 시스템에 잇는 <b>유일한</b> 다리.
///
/// SeasonGrowthAdapter 와 같은 자리에 나란히 꽂힌다. 둘 다 등록되면
/// CropManager 가 알아서 곱해준다 — 가을 1.5배 × 마스터리 1.2배 = 1.8배.
///
/// 작물 코드는 마스터리를 전혀 모르고, 마스터리 코드는 작물을 전혀 모른다.
/// 둘을 아는 건 이 파일 하나뿐이라, 어느 쪽을 갈아엎어도 여기만 고치면 된다.
///
/// 씬 배치: MasteryManager 와 같은 오브젝트에 붙이면 편하다.
/// </summary>
public class MasteryGrowthAdapter : MonoBehaviour, IGrowthModifier
{
    [Header("어떤 스탯을 볼 것인가")]
    [Tooltip("작물 성장 속도를 올려주는 스탯")]
    [SerializeField] private MasteryStat speedStat = MasteryStat.GrowthSpeed;
 
    [Tooltip("작물 품질 확률을 올려주는 스탯")]
    [SerializeField] private MasteryStat luckStat = MasteryStat.FarmLuck;
 
    [Header("어떤 퍽을 볼 것인가")]
    [Tooltip("이 퍽이 해금되기 전에는 최상 등급이 '좋음'으로 강등된다")]
    [SerializeField] private MasteryPerk bestQualityPerk = MasteryPerk.UnlockBestQuality;
 
    [Tooltip("이 퍽이 해금되면 수확량에 배수가 붙는다")]
    [SerializeField] private MasteryPerk yieldPerk = MasteryPerk.BonusHarvestYield;
 
    private void OnEnable()
    {
        CropManager mgr = CropManager.Instance;
 
        if (mgr == null)
        {
            Debug.LogWarning("[마스터리연동] 씬에서 CropManager 를 찾지 못했습니다.", this);
            return;
        }
 
        mgr.AddGrowthModifier(this);
    }
 
    private void OnDisable()
    {
        // Unity 의 == 는 파괴된 오브젝트도 null 로 판정한다.
        // ?. 는 그 판정을 안 거치므로 여기서는 쓰면 안 된다
        CropManager mgr = CropManager.Instance;
        if (mgr != null) mgr.RemoveGrowthModifier(this);
    }
 
    // ════════════════════════════════════════════════════════════
    //  IGrowthModifier
    // ════════════════════════════════════════════════════════════
 
    /// <summary>레벨당 쌓인 성장 속도. 스탯이 0.3 이면 1.3배</summary>
    public float GrowthSpeedMultiplier => 1f + MasteryManager.Stat(speedStat);
 
    /// <summary>15레벨 퍽이 켜지기 전에는 1배</summary>
    public float YieldMultiplier => MasteryManager.PerkVal(yieldPerk, 1f);
 
    /// <summary>레벨당 쌓인 농사 행운이 그대로 품질 확률에 더해진다</summary>
    public float QualityBonus => MasteryManager.Stat(luckStat);
 
    /// <summary>10레벨 퍽을 찍기 전에는 최상 등급이 안 나온다</summary>
    public bool AllowBestQuality => MasteryManager.Perk(bestQualityPerk);
 
    /// <summary>제철 판정은 계절 시스템 담당. 마스터리는 심기를 막지 않는다</summary>
    public bool CanPlantNow(CropSO crop) => true;
}