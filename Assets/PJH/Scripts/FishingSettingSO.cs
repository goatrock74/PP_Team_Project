using UnityEngine;

namespace PJH.Scripts
{
    [CreateAssetMenu(fileName = "FishingSettingSO", menuName = "FishingSO", order = 0)]
    public class FishingSettingSO : ScriptableObject
    {
        [Header("Bite time")] 
        [field: SerializeField] public float MinBiteTime { get; private set; } = 4f;
        [field: SerializeField] public float MaxBiteTime { get; private set; } = 10f;
    }
}