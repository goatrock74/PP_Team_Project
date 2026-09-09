using System.Collections.Generic;
using UnityEngine;
 
/// <summary>
/// 긴낫. <b>플레이어 정면 앞</b> 상자에 닿은 모든 채집물을 한 번에 벤다.
///
/// 도끼와 달리 여러 개를 쓸어담는 게 낫질의 맛이라, 범위 안 전부를 대상으로 한다.
/// 범위는 Hit Box Size 로 조절한다 — 강화 낫은 넓게 잡으면 된다.
///
/// 무엇이 나올지는 각 채집물(ForageNode)의 확률표가 정한다.
/// 낫은 "몇 번 굴릴지" 와 "어떤 등급까지 벨 수 있는지" 만 담당한다.
/// </summary>
[CreateAssetMenu(fileName = "ScytheSO", menuName = "SO/Item/Tool/Scythe")]
public class ScytheSO : ToolSO
{
    [Header("긴낫")]
    [Tooltip("채집물이 자체 확률표를 안 가졌을 때 대신 쓸 표")]
    public LootTableSO fallbackTable;
 
    [Tooltip("기본 1회에 더해 몇 번 더 굴릴지. 강화 낫은 1 이상")]
    [Min(0)] public int bonusRolls;
 
    public override bool UsesHitBox => true;
 
    public override bool CanUse(in ToolUseContext ctx)
    {
        var targets = new List<IForageable>();
        FindAll(in ctx, targets);
 
        foreach (IForageable t in targets)
            if (t.CanForage(this)) return true;
 
        return false;
    }
 
    public override bool Use(in ToolUseContext ctx)
    {
        // Forage 안에서 오브젝트가 파괴될 수 있으므로 대상을 먼저 다 모아둔다
        var targets = new List<IForageable>();
        FindAll(in ctx, targets);
 
        if (targets.Count == 0)
        {
            Log("채집할 것이 없습니다");
            return false;
        }
 
        int success = 0;
 
        foreach (IForageable target in targets)
        {
            if (target == null || !target.CanForage(this)) continue;
            if (target.Forage(this, in ctx)) success++;
        }
 
        if (success > 0) Log($"{success}개를 채집했습니다");
        return success > 0;
    }
}