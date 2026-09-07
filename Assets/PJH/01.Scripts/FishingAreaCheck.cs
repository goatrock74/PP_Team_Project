using UnityEngine;

namespace PJH.Scripts
{
    public class FishingAreaCheck : MonoBehaviour
    {
        [SerializeField] private LayerMask fishingAreaLayer;
        [SerializeField] private Transform bobberPoint;
        
        public Vector3 FishingPointPosition => bobberPoint.position;
    
        public bool IsFishingLayer()
        {
            Collider2D result = Physics2D.OverlapPoint(bobberPoint.position, 
                fishingAreaLayer);
            
            return result != null;

        }
    }
}
