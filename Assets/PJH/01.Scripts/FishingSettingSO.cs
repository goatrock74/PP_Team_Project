using UnityEngine;

namespace PJH.Scripts
{
    [CreateAssetMenu(fileName = "FishingSettingSO", menuName = "FishingSO", order = 0)]
    public class FishingSettingSO : ScriptableObject
    {
        [Header("Bite time")] 
        
        // 4 ~ 10
        [field: SerializeField] public float MinBiteTime { get; private set; } = 1f;
        [field: SerializeField] public float MaxBiteTime { get; private set; } = 2f;
    }
}