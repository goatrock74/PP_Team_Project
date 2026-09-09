using UnityEngine;
using UnityEngine.Tilemaps;
 
/// <summary>
/// 괭이. 맨땅 타일을 밭 타일로 바꾼다.
///
/// ★ 여기서 지정한 Tilled Tile 을 CropSO 의 Plantable Tiles 에도 넣어야
///   갈아놓은 땅에 씨앗이 심긴다. 안 그러면 갈아도 못 심는다.
/// </summary>
[CreateAssetMenu(fileName = "HoeSO", menuName = "SO/Item/Tool/Hoe")]
public class HoeSO : ToolSO
{
    [Header("괭이")]
    [Tooltip("이 타일들만 갈 수 있다. 비워두면 아무 타일이나 갈 수 있다")]
    public TileBase[] tillableTiles;
 
    [Tooltip("갈고 나면 이 타일이 된다")]
    public TileBase tilledTile;
 
    public override bool CanUse(in ToolUseContext ctx)
    {
        if (ctx.farm == null || tilledTile == null) return false;
 
        // 범위 안에 갈 수 있는 칸이 하나라도 있으면 사용 가능
        foreach (Vector3Int cell in GetCells(ctx.cell))
            if (CanTill(ctx.farm, cell)) return true;
 
        return false;
    }
 
    public override bool Use(in ToolUseContext ctx)
    {
        if (ctx.farm == null) return false;
 
        if (tilledTile == null)
        {
            Debug.LogWarning($"[{DisplayName}] Tilled Tile 이 비어있습니다.", this);
            return false;
        }
 
        int count = 0;
 
        foreach (Vector3Int cell in GetCells(ctx.cell))
        {
            if (!CanTill(ctx.farm, cell)) continue;
 
            ctx.farm.SetGroundTile(cell, tilledTile);
            count++;
        }
 
        if (count > 0) Log($"{count}칸을 갈았습니다");
        return count > 0;
    }
 
    private bool CanTill(KSM._00.Scripts.Crop.CropManager farm, Vector3Int cell)
    {
        // 작물이 심겨 있으면 갈지 않는다 (실수로 밭을 뒤엎는 사고 방지)
        if (farm.GetOccupant(cell) != null) return false;
 
        // 젖은 땅은 이미 밭이다. 갈아버리면 물기가 날아간다
        if (farm.IsWet(cell)) return false;
 
        TileBase current = farm.GetGroundTile(cell);
        if (current == null) return false;              // 아무것도 없는 허공
        if (current == tilledTile) return false;        // 이미 갈아둔 곳
 
        if (tillableTiles == null || tillableTiles.Length == 0) return true;
 
        foreach (TileBase t in tillableTiles)
            if (t == current) return true;
 
        return false;
    }
}