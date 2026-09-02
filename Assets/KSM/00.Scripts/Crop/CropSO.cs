using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;
 
[CreateAssetMenu(fileName = "CropSO", menuName = "SO/CropSO")]
public class CropSO : ScriptableObject
{
    [Header("기본 정보")]
    public string cropName;
    public Sprite icon;                     // 인벤토리 아이콘
    public HarvestType harvestType;
 
    [Header("성장 단계")]
    [Tooltip("각 단계의 스프라이트와 그 단계에 머무는 시간")]
    public GrowthStage[] growthStages;
 
    [Tooltip("이 단계에 도달하면 수확 가능 (인덱스, 0부터 시작). 보통 마지막 단계")]
    [Min(0)] public int harvestStageIndex;
 
    [Header("다회용 설정")]
    [FormerlySerializedAs("BackInDexStage")]
    [Tooltip("수확 후 되돌아갈 단계 (인덱스, 0부터 시작). Multiple일 때만 사용")]
    [Min(0)] public int regrowStageIndex;
 
    [Header("수확물")]
    [Min(1)] public int minYield = 1;
    [Min(1)] public int maxYield = 1;
 
    [Header("가격")]
    [Min(0)] public int minPrice = 10;
    [Min(0)] public int maxPrice = 100;
 
    [Header("설치 크기 (타일맵 칸 수)")]
    [Tooltip("1x1이면 한 칸, 3x3이면 9칸 차지")]
    public Vector2Int size = Vector2Int.one;
 
    [Header("설치 가능한 타일")]
    [Tooltip("이 목록에 있는 타일 위에만 심을 수 있음 (예: 밭 타일)")]
    public TileBase[] plantableTiles;
 
    // 런타임 전용 조회 캐시. 직렬화되지 않는다.
    private HashSet<TileBase> _plantableSet;
 
    /// <summary>이 타일 위에 심을 수 있는가</summary>
    public bool IsPlantableTile(TileBase tile)
    {
        if (tile == null) return false;
        if (plantableTiles == null || plantableTiles.Length == 0) return false;
 
        _plantableSet ??= new HashSet<TileBase>(plantableTiles);
        return _plantableSet.Contains(tile);
    }
 
    /// <summary>수확량을 굴린다 (min~max 포함)</summary>
    public int RollYield() => UnityEngine.Random.Range(minYield, maxYield + 1);
 
    private void OnValidate()
    {
        if (maxPrice < minPrice) maxPrice = minPrice;
        if (maxYield < minYield) maxYield = minYield;
 
        size.x = Mathf.Max(1, size.x);
        size.y = Mathf.Max(1, size.y);
 
        _plantableSet = null;   // 인스펙터에서 목록을 고쳤을 수 있으니 캐시 폐기
 
        if (growthStages == null || growthStages.Length == 0) return;
 
        harvestStageIndex = Mathf.Clamp(harvestStageIndex, 0, growthStages.Length - 1);
 
        if (harvestType == HarvestType.Multiple)
        {
            // 되돌아갈 단계는 반드시 수확 단계보다 앞이어야 한다. 아니면 즉시 재수확 버그
            regrowStageIndex = Mathf.Clamp(regrowStageIndex, 0, Mathf.Max(0, harvestStageIndex - 1));
        }
    }
}
 
[Serializable]
public struct GrowthStage
{
    [Tooltip("이 단계에서 보여줄 스프라이트")]
    public Sprite sprite;
 
    [FormerlySerializedAs("durationtime")]
    [Min(0.1f)]
    [Tooltip("이 단계에 머무는 시간. 마지막(수확) 단계의 값은 사용되지 않음")]
    public float durationTime;
}
 
public enum HarvestType
{
    Single,   // 1회용: 수확하면 사라짐
    Multiple, // 다회용: 수확하면 특정 단계로 돌아가서 다시 자람
}