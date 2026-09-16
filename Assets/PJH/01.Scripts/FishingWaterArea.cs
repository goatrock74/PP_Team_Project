using UnityEngine;

namespace PJH._01.Scripts
{
    public class FishingWaterArea : MonoBehaviour
    {
        [Header("Fish Water Area")]
        [SerializeField] private FishCollectionCategory category;
        
        public FishCollectionCategory Category => category;
    }
}