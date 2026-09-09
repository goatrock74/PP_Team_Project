using UnityEngine;
 
/// <summary>
/// 도끼. <b>플레이어 정면 앞</b>의 상자에 닿은 것 중 가장 가까운 하나를 벤다.
///
/// 칸 단위가 아니라 상자로 판정하기 때문에, 나무가 격자에 딱 맞춰 놓여 있지 않아도
/// 그림에 닿기만 하면 맞는다. 여러 그루가 겹쳐 있어도 한 번에 한 그루만 친다.
/// </summary>
[CreateAssetMenu(fileName = "AxeSO", menuName = "SO/Item/Tool/Axe")]
public class AxeSO : ToolSO
{
    [Header("도끼")]
    [Tooltip("한 번 칠 때 깎는 체력")]
    [Min(1)] public int power = 1;
 
    [Tooltip("쓰러뜨렸을 때 전리품을 몇 번 더 굴릴지. 강화 도끼는 1 이상")]
    [Min(0)] public int extraDropRolls;
 
    public override bool UsesHitBox => true;
 
    public override bool CanUse(in ToolUseContext ctx)
    {
        IChoppable target = FindNearest<IChoppable>(in ctx);
        return target != null && target.CanChop(this);
    }
 
    public override bool Use(in ToolUseContext ctx)
    {
        IChoppable target = FindNearest<IChoppable>(in ctx);
 
        if (target == null)
        {
            Log("벨 것이 없습니다");
            return false;
        }
 
        return target.Chop(this, in ctx);
    }
}