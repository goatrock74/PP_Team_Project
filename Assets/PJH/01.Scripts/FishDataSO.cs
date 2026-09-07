using UnityEngine;

namespace PJH.Scripts
{
    [CreateAssetMenu(fileName = "Fish Data", menuName = "FishDataSO", order = 0)]
    public class FishDataSO : ScriptableObject
    {
        [Header("primary information")]
        public string fishID;
        public string displayName;
        public Sprite icon;
        
        [Header("Price")]
        public int basePrice;

        public float difficulty;

        [Header("MiniGame Movement")]
        [SerializeField, Min(0.01f)]
        private float moveSpeed = 0.25f;
        
        [SerializeField, Min(0.1f)]
        private float minTargetChangeTime = 1f;

        [SerializeField, Min(0.1f)]
        private float maxTargetChangeTime = 2f;
        
        [SerializeField, Range(0f, 1f)]
        private float maxMoveDistance = 0.3f;
        
        public float MoveSpeed => moveSpeed;
        public float MinTargetChangeTime => minTargetChangeTime;
        public float MaxTargetChangeTime => maxTargetChangeTime;
        public float MaxMoveDistance => maxMoveDistance;




    }
}