using UnityEngine;
using UnityEngine.Tilemaps;
using KSM._00.Scripts.Crop;
 
/// <summary>
/// 물뿌리개. 두 가지 일을 한다.
///   1) 땅을 젖은 타일로 바꾼다 — 하루쯤 지나면 저절로 마른다
///   2) 그 자리 작물의 성장을 즉시 조금 앞당긴다 (Growth Days, 0이면 생략)
///
/// 젖어 있는 동안 그 칸의 작물은 CropManager 의 Wet Growth Multiplier 만큼 빨리 자란다.
/// 마르면 자동으로 원래 속도로 돌아간다.
///
/// 물통 용량은 넣지 않았다. 남은 물은 "지금 이 물뿌리개 하나"의 상태인데
/// SO 는 모든 물뿌리개가 공유하는 데이터라 거기에 담으면 안 되기 때문이다.
/// </summary>
[CreateAssetMenu(fileName = "WateringCanSO", menuName = "SO/Item/Tool/Watering Can")]
public class WateringCanSO : ToolSO
{
    [Header("젖은 땅")]
    [Tooltip("적시면 이 타일로 바뀐다. 마르면 원래 타일로 저절로 돌아간다")]
    public TileBase wetTile;
 
    [Tooltip("이 타일들만 적실 수 있다 (보통 밭 타일). 비우면 아무 타일이나")]
    public TileBase[] wettableTiles;
 
    [Tooltip("젖은 상태가 유지되는 인게임 일수")]
    [Min(0.05f)] public float wetDurationDays = 1f;
 
    [Header("즉시 성장")]
    [Tooltip("물을 줄 때 바로 앞당길 인게임 일수. 0이면 젖게만 하고 즉시 성장은 없음")]
    [Min(0f)] public float growthDays;
 
    [Tooltip("이미 다 자란 작물에도 물을 줄 수 있게 할지 (보통 끔)")]
    public bool waterMatureCrops;
 
    public override bool CanUse(in ToolUseContext ctx)
    {
        if (ctx.farm == null) return false;
 
        foreach (Vector3Int cell in GetCells(ctx.cell))
        {
            if (CanWetCell(ctx.farm, cell)) return true;
            if (CanWaterCrop(ctx.farm.GetOccupant(cell))) return true;
        }
 
        return false;
    }
 
    public override bool Use(in ToolUseContext ctx)
    {
        if (ctx.farm == null) return false;
 
        int wetCount = 0;
        int cropCount = 0;
 
        foreach (Vector3Int cell in GetCells(ctx.cell))
        {
            // 1) 땅 적시기
            if (CanWetCell(ctx.farm, cell) && ctx.farm.SetCellWet(cell, wetTile, wetDurationDays))
                wetCount++;
 
            // 2) 즉시 성장 (여러 칸 작물은 원점 칸에서만 처리해 중복을 막는다)
            if (growthDays <= 0f) continue;
 
            GrowCrop crop = ctx.farm.GetOccupant(cell);
            if (!CanWaterCrop(crop) || crop.OriginCell != cell) continue;
 
            crop.AddGrowth(growthDays);
            cropCount++;
        }
 
        if (wetCount > 0 || cropCount > 0)
            Log($"{wetCount}칸을 적셨습니다" + (cropCount > 0 ? $" (작물 {cropCount}개 +{growthDays}일)" : string.Empty));
 
        return wetCount > 0 || cropCount > 0;
    }
 
    // ════════════════════════════════════════════════════════════
 
    private bool CanWetCell(CropManager farm, Vector3Int cell)
    {
        if (wetTile == null) return false;
        if (farm.IsWet(cell)) return false;                 // 이미 젖어 있음
 
        TileBase current = farm.GetGroundTile(cell);
        if (current == null || current == wetTile) return false;
 
        if (wettableTiles == null || wettableTiles.Length == 0) return true;
 
        foreach (TileBase t in wettableTiles)
            if (t == current) return true;
 
        return false;
    }
 
    private bool CanWaterCrop(GrowCrop crop)
    {
        if (crop == null || growthDays <= 0f) return false;
        return waterMatureCrops || !crop.IsGrowFinished;
    }
}