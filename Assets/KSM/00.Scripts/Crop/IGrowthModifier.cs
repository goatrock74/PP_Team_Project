namespace KSM._00.Scripts.Crop
{
  
    public interface IGameClock
    {
        float TotalGameDays { get; }
    }
 
    public interface IGrowthModifier
    {
        float GrowthSpeedMultiplier { get; }
 
        float YieldMultiplier { get; }
 
        float QualityBonus { get; }
 

        bool AllowBestQuality { get; }
 
        bool CanPlantNow(CropSO crop);
    }
}