using UnityEngine;

namespace PJH.Scripts
{
    public class FishingAreaCheck : MonoBehaviour
    {
        [SerializeField] private LayerMask fishingAreaLayer;
    
        public bool IsFishingLayer()
        {
            Collider2D result = Physics2D.OverlapPoint(transform.position + Vector3.one, fishingAreaLayer);
            
            return result != null;

        }
    }
}
