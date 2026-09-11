using UnityEngine;
 
[CreateAssetMenu(fileName = "SeedSO", menuName = "SO/Item/Seed")]
public class SeedSO : ItemSO
{
    [Header("씨앗")]
    [Tooltip("이 씨앗을 심으면 자라날 작물")]
    public CropSO crop;
 
    public bool IsPlantable => crop != null
                               && crop.growthStages != null
                               && crop.growthStages.Length > 0;
 
    protected override void OnValidate()
    {
        base.OnValidate();
 
        itemType = ItemType.Seed;
 
        if (crop == null)
            return;
 
        if (string.IsNullOrWhiteSpace(displayName) && !string.IsNullOrWhiteSpace(crop.cropName))
            displayName = $"{crop.cropName} 씨앗";
    }
}