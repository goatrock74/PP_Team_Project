using UnityEngine;
using PJH.Scripts;
 
/// <summary>
/// 낚싯대. 다른 도구들과 똑같이 <b>인벤토리에 들고 좌클릭</b>하면 던진다.
///
/// 이렇게 ToolSO 를 상속하면 공짜로 따라오는 것들:
///   · PlayerInteractor 가 이미 "손에 든 게 ToolSO 면 Use() 호출" 하고 있어서 클릭 배선 끝
///   · 인벤토리 · 핫바 등록, 판매가, 아이콘, 정보창
///   · 일반 / 강화 등급 (ToolTier)
///   · ToolAnimator 의 스프라이트 라이브러리 교체
///
/// 실제 낚시 로직은 하나도 여기 없다. 물 판정은 FishingAreaCheck 가,
/// 입질·미니게임은 FishingController 와 FishingMiniGame 이 한다.
/// 이 파일은 <b>도구 시스템과 낚시 시스템을 잇는 다리</b>일 뿐이다.
/// </summary>
[CreateAssetMenu(fileName = "FishingRodSO", menuName = "SO/Item/Tool/Fishing Rod")]
public class FishingRodSO : ToolSO
{
    [Header("낚싯대")]
    [Tooltip("강화 낚싯대에서 올려주면 좋을 값들. 지금은 표시용이고, " +
             "실제로 쓰려면 FishSelector / FishingSettingSO 쪽에서 읽어가면 된다")]
    [Min(0f)] public float biteTimeBonus;
 
    [Tooltip("이 낚싯대로 잡을 때 추가로 붙는 행운. FishSelector 가 읽는다")]
    [Min(0f)] public float luckBonus;
 
    /// <summary>
    /// 상자 판정으로 쳐서 PlayerInteractor 의 <b>칸 거리 검사를 건너뛴다.</b>
    /// 낚시는 마우스가 가리키는 칸이 아니라 찌가 떨어지는 지점(FishingAreaCheck)이
    /// 기준이라, 격자 거리로 막으면 물을 코앞에 두고도 안 던져진다.
    /// </summary>
    public override bool UsesHitBox => true;
 
    public override bool CanUse(in ToolUseContext ctx)
    {
        PlayerFishing fishing = FindFishing(in ctx);
        return fishing != null && fishing.CanCast();
    }
 
    public override bool Use(in ToolUseContext ctx)
    {
        PlayerFishing fishing = FindFishing(in ctx);
 
        if (fishing == null)
        {
            Log("플레이어에서 PlayerFishing 을 찾지 못했습니다");
            return false;
        }
 
        if (!fishing.TryStartCast())
        {
            Log("여기서는 낚시할 수 없습니다");
            return false;
        }
 
        return true;
    }
 
    /// <summary>낚싯대를 쓴 주체(플레이어)에게서 낚시 컴포넌트를 찾는다</summary>
    private static PlayerFishing FindFishing(in ToolUseContext ctx)
        => ctx.user != null ? ctx.user.GetComponentInChildren<PlayerFishing>() : null;
 
    protected override void OnValidate()
    {
        base.OnValidate();
 
        // 낚싯대는 휘두르고 끝나는 도구가 아니라 "던지고 기다리는" 도구다.
        // 발 묶기는 PlayerFishing 이 HoldLocked 로 직접 관리하므로 여기선 끈다
        lockMovementWhileUsing = false;
    }
}