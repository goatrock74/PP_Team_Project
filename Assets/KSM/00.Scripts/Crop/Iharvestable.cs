using UnityEngine;

namespace KSM._00.Scripts.Crop
{
    public interface IHarvestable
    {
        /// <summary>지금 수확할 수 있는 상태인가</summary>
        bool CanHarvest { get; }
 
        /// <summary>UI에 띄울 문구 (예: "감자 수확", "자라는 중")</summary>
        string HarvestPrompt { get; }
 
        /// <summary>수확 시도. 성공하면 true</summary>
        bool TryHarvest();
    }
}