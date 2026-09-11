using UnityEngine;

namespace KSM._00.Scripts.Crop
{
    public interface IHarvestable
    {
        bool CanHarvest { get; }
 
        string HarvestPrompt { get; }
 
        bool TryHarvest();
    }
}