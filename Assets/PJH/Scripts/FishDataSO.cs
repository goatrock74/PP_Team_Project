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

        
        



    }
}