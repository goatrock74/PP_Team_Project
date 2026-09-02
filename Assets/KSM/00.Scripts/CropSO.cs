using System;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "CropSO", menuName = "SO/CropSO")]
public class CropSO : ScriptableObject
{
    [Header("기본 정보")]
    public string cropName;
    public Sprite icon; // 인벤토리 아이콘
    public HarvestType harvestType;
    [Header("성장 단계")]
    [SerializeField, Tooltip(" 자라는 스프라이트,자라는 시간")]
    GrowthStage[] growthStages;
    [Header("가격")]
    [Min(0)] public int minPrice = 10;
    [Min(0)] public int maxPrice = 100;

    [Header("설치 크기 (타일맵 칸 수)")]
    [Tooltip("1x1이면 한 칸, 3x3이면 9칸 차지")]
    public Vector2Int size = Vector2Int.one;
 
    [Header("설치 가능한 타일")]
    [Tooltip("이 목록에 있는 타일 위에만 심을 수 있음 (예: 밭 타일)")]
    public TileBase[] plantableTiles;
}
[Serializable]
public struct GrowthStage
{
    [Tooltip("이 단계에서 보여줄 스프라이트")]
    public Sprite sprite;

    [Min(1)]
    [Tooltip("이 단계에 머무는 인게임 일수")]
    public int durationDays;
}
public enum HarvestType
{
    Single,   // 1회용: 수확하면 사라짐
    Multiple, // 다회용: 수확하면 특정 단계로 돌아가서 다시 자람
}

