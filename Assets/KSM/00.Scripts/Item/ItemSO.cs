using UnityEngine;
 
/// <summary>
/// 인벤토리에 들어가는 모든 것의 공통 데이터.
///
/// 물고기·요리처럼 추가 정보가 필요하면 이걸 상속해서 필드만 더하면 된다.
/// 인벤토리는 ItemSO 로만 다루기 때문에 상속 클래스를 몰라도 그대로 담긴다.
///
///     public class FishSO : ItemSO { public float length; public Rarity rarity; }
///     public class DishSO : ItemSO { public int staminaHeal; public float buffDuration; }
/// </summary>
[CreateAssetMenu(fileName = "ItemSO", menuName = "SO/Item/Item")]
public class ItemSO : ScriptableObject
{
    [Header("식별")]
    [Tooltip("세이브 파일에 기록될 고정 ID. 비워두면 에셋 파일명을 쓴다. 한번 정하면 바꾸지 말 것")]
    [SerializeField] private string itemId;
 
    [Header("표시")]
    public string displayName;
    public Sprite icon;
    [TextArea(2, 4)] public string description;
 
    [Header("분류")]
    public ItemType itemType;
 
    [Header("스택")]
    [Tooltip("한 칸에 최대 몇 개까지 쌓이는가. 도구처럼 안 쌓이는 건 1")]
    [Min(1)] public int maxStack = 99;
 
    [Header("가격")]
    [Tooltip("일반 품질 기준 판매가. 좋음/최상은 여기에 배수가 곱해진다")]
    [Min(0)] public int sellPrice;
 
    /// <summary>
    /// 품질을 반영한 판매가. 상점은 작물이든 물고기든 이것만 부르면 된다.
    /// (가격이 CropSO 가 아닌 ItemSO 에 있는 이유 — 상점은 ItemSO 만 받는다)
    /// </summary>
    public int GetSellPrice(ItemQuality quality = ItemQuality.Normal)
        => Mathf.RoundToInt(sellPrice * ItemQualityUtil.PriceMultiplier(quality));
 
    public string ItemId => string.IsNullOrWhiteSpace(itemId) ? name : itemId;
    public bool IsStackable => maxStack > 1;
 
    /// <summary>UI에 띄울 이름. displayName 이 비어있으면 에셋 이름으로 대체</summary>
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
 
    // virtual: 상속 클래스(SeedSO 등)가 override 해서 base.OnValidate() 를 부를 수 있게.
    // private 으로 두면 자식이 같은 이름을 선언했을 때 부모 것이 아예 안 불린다
    protected virtual void OnValidate()
    {
        maxStack = Mathf.Max(1, maxStack);
    }
}
 
public enum ItemType
{
    Crop,       // 수확한 농작물
    Seed,       // 씨앗
    Fish,       // 물고기
    Dish,       // 요리
    Material,   // 재료
    Tool,       // 도구
    Etc,
}
 
public static class ItemTypeUtil
{
    /// <summary>정보창에 띄울 한글 분류명</summary>
    public static string DisplayName(ItemType t) => t switch
    {
        ItemType.Crop => "농작물",
        ItemType.Seed => "씨앗",
        ItemType.Fish => "물고기",
        ItemType.Dish => "요리",
        ItemType.Material => "재료",
        ItemType.Tool => "도구",
        _ => "기타",
    };
}
 