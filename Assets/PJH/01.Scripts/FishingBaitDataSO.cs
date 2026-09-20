using UnityEngine;

namespace PJH._01.Scripts
{
    [CreateAssetMenu(fileName = "FILENAME", menuName = "Fishing/Fishing Bait Data", order = 0)]
    public class FishingBaitDataSO : ItemSO
    {
        [Header("미끼 효과")]
        [SerializeField, Min(0f)] private float fishLuckBonus;

        [SerializeField, Min(1)] private int effectiveCatchCount = 1;
        
        public float FishLuckBonus => fishLuckBonus;
        public int EffectiveCatchCount => effectiveCatchCount;

        protected override void OnValidate()
        {
            base.OnValidate();
            itemType = ItemType.Bait;
            maxStack = 99;
        }
    }
}