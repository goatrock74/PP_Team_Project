using UnityEngine;
 
/// <summary>
/// 심을 수 있는 씨앗. 어떤 작물로 자랄지를 들고 있다.
///
/// 수확물(오이 열매)과 씨앗(오이 씨앗)은 서로 다른 에셋이다.
///   - 오이 열매 : ItemSO       (인벤토리에만 들어감)
///   - 오이 씨앗 : SeedSO       (인벤토리 + 심기 가능)
///   - 오이 작물 : CropSO       (밭에서 자라는 것)
/// </summary>
[CreateAssetMenu(fileName = "SeedSO", menuName = "SO/Item/Seed")]
public class SeedSO : ItemSO
{
    [Header("씨앗")]
    [Tooltip("이 씨앗을 심으면 자라날 작물")]
    public CropSO crop;
 
    /// <summary>심을 수 있는 상태인가 (작물 연결이 되어 있는가)</summary>
    public bool IsPlantable => crop != null
                               && crop.growthStages != null
                               && crop.growthStages.Length > 0;
 
    protected override void OnValidate()
    {
        base.OnValidate();
 
        // 씨앗은 항상 Seed 타입이어야 한다. 실수로 Crop 으로 두면 심기 판정에서 걸러진다
        itemType = ItemType.Seed;
 
        if (crop == null)
            return;
 
        // 이름을 안 정했으면 작물 이름으로 채워준다
        if (string.IsNullOrWhiteSpace(displayName) && !string.IsNullOrWhiteSpace(crop.cropName))
            displayName = $"{crop.cropName} 씨앗";
    }
}