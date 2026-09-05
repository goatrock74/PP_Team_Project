using UnityEngine;
 
/// <summary>
/// 뽑기 결과의 희귀도. 확률과는 별개로 "보여주기용" 등급이다.
/// 확률은 LootEntry 의 weight 가 정하고, 이건 색과 라벨만 담당한다.
/// </summary>
public enum ItemRarity
{
    Common = 0,      // 일반
    Uncommon = 1,    // 고급
    Rare = 2,        // 희귀
    Epic = 3,        // 영웅
    Legendary = 4,   // 전설
}
 
public static class ItemRarityUtil
{
    public static string DisplayName(ItemRarity r) => r switch
    {
        ItemRarity.Uncommon => "고급",
        ItemRarity.Rare => "희귀",
        ItemRarity.Epic => "영웅",
        ItemRarity.Legendary => "전설",
        _ => "일반",
    };
 
    public static Color Color(ItemRarity r) => r switch
    {
        ItemRarity.Uncommon => new Color(0.45f, 0.85f, 0.45f),   // 초록
        ItemRarity.Rare => new Color(0.35f, 0.65f, 1.00f),       // 파랑
        ItemRarity.Epic => new Color(0.75f, 0.45f, 1.00f),       // 보라
        ItemRarity.Legendary => new Color(1.00f, 0.80f, 0.20f),  // 금색
        _ => new Color(0.85f, 0.85f, 0.85f),                     // 회색
    };
 
    public static string ColorHex(ItemRarity r) => ColorUtility.ToHtmlStringRGB(Color(r));
}