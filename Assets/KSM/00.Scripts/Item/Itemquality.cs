using System;
using UnityEngine;
 
/// <summary>
/// 아이템 품질. 농작물뿐 아니라 물고기·요리에도 그대로 쓸 수 있다.
/// 등급이 3개뿐이라 인벤토리에서 등급별로만 나뉘어 쌓인다 (개체마다 값이 다르면 스택이 깨진다).
/// </summary>
public enum ItemQuality
{
    Normal = 0,   // 일반
    Good = 1,     // 좋음
    Best = 2,     // 최상
}
 
public static class ItemQualityUtil
{
    /// <summary>판매가에 곱할 배수</summary>
    public static float PriceMultiplier(ItemQuality q) => q switch
    {
        ItemQuality.Good => 1.25f,
        ItemQuality.Best => 1.50f,
        _ => 1.00f,
    };
 
    public static string DisplayName(ItemQuality q) => q switch
    {
        ItemQuality.Good => "좋음",
        ItemQuality.Best => "최상",
        _ => "일반",
    };
 
    /// <summary>슬롯에 붙일 표기. 일반은 아무것도 안 붙인다</summary>
    public static string Stars(ItemQuality q) => q switch
    {
        ItemQuality.Good => "★",
        ItemQuality.Best => "★★",
        _ => string.Empty,
    };
 
    public static Color TintColor(ItemQuality q) => q switch
    {
        ItemQuality.Good => new Color(0.60f, 0.80f, 1.00f),   // 은빛
        ItemQuality.Best => new Color(1.00f, 0.82f, 0.30f),   // 금빛
        _ => Color.white,
    };
}
 
/// <summary>
/// 품질을 굴리는 확률표. CropSO 나 친구분의 FishSO 에 필드로 하나 넣어두고 Roll() 을 부르면 된다.
/// </summary>
[Serializable]
public struct QualityChance
{
    [Range(0f, 1f)]
    [Tooltip("'좋음'이 나올 확률")]
    public float goodChance;
 
    [Range(0f, 1f)]
    [Tooltip("'최상'이 나올 확률. 좋음보다 먼저 판정한다")]
    public float bestChance;
 
    /// <summary>비료·행운 같은 보너스를 더해서 굴릴 수 있다</summary>
    public ItemQuality Roll(float bonus = 0f)
    {
        float best = Mathf.Clamp01(bestChance + bonus);
        float good = Mathf.Clamp01(goodChance + bonus);
 
        float r = UnityEngine.Random.value;
 
        if (r < best) return ItemQuality.Best;
        if (r < best + good) return ItemQuality.Good;
 
        return ItemQuality.Normal;
    }
 
    /// <summary>인스펙터 기본값 대신 쓸 무난한 설정 (좋음 20%, 최상 5%)</summary>
    public static QualityChance Default => new QualityChance { goodChance = 0.20f, bestChance = 0.05f };
}
 