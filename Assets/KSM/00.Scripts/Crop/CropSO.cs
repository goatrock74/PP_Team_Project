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
    public Sprite icon;                     
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
    [Tooltip("수확했을 때 인벤토리에 들어갈 아이템. 작물 자체(CropSO)와는 별개의 에셋이다")]
    public ItemSO harvestItem;
 
    [Min(1)] public int minYield = 1;
    [Min(1)] public int maxYield = 1;
 
    [Header("수확 품질")]
    [Tooltip("수확할 때 굴리는 품질 확률. 가격은 ItemSO 의 Sell Price 에 배수가 곱해진다")]
    public QualityChance qualityChance = QualityChance.Default;
 
    [Header("제철")]
    [Tooltip("이 계절에만 심을 수 있다. 계절 시스템이 안 붙어 있으면 무시된다")]
    public CropSeason plantableSeasons = CropSeason.All;
 
    [Header("설치 크기 (타일맵 칸 수)")]
    [Tooltip("1x1이면 한 칸, 3x3이면 9칸 차지")]
    public Vector2Int size = Vector2Int.one;
 
    [Header("설치 가능한 타일")]
    [Tooltip("이 목록에 있는 타일 위에만 심을 수 있음 (예: 밭 타일)")]
    public TileBase[] plantableTiles;
 
    private HashSet<TileBase> _plantableSet;
 
    public bool IsPlantableTile(TileBase tile)
    {
        if (tile == null) return false;
        if (plantableTiles == null || plantableTiles.Length == 0) return false;
 
        _plantableSet ??= new HashSet<TileBase>(plantableTiles);
        return _plantableSet.Contains(tile);
    }
 
    public int RollYield() => UnityEngine.Random.Range(minYield, maxYield + 1);
 
    private void OnValidate()
    {
        if (maxYield < minYield) maxYield = minYield;
 
        size.x = Mathf.Max(1, size.x);
        size.y = Mathf.Max(1, size.y);
 
        _plantableSet = null; 
 
        if (growthStages == null || growthStages.Length == 0) return;
        for (int i = 0; i < growthStages.Length; i++)
        {
            if (growthStages[i].durationTime > 0f) continue;
 
            GrowthStage stage = growthStages[i];
            stage.durationTime = 1f;         
            growthStages[i] = stage;
        }
 
        harvestStageIndex = Mathf.Clamp(harvestStageIndex, 0, growthStages.Length - 1);
 
        if (harvestType == HarvestType.Multiple)
        {
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
    [Min(0.02f)]
    [Tooltip("이 단계에 머무는 시간 — 단위는 '인게임 일수'.\n" +
             "1 = 하루, 0.5 = 반나절, 3 = 사흘.\n" +
             "마지막(수확) 단계의 값은 사용되지 않는다")]
    public float durationTime;   
}
 
public enum HarvestType
{
    Single,   
    Multiple, 
}
 