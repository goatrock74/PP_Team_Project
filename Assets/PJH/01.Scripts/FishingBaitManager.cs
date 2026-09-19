using UnityEngine;

namespace PJH._01.Scripts
{
    public class FishingBaitManager : MonoBehaviour
    {
        private FishingBaitDataSO activeBait;
        private int remainingCatchCount;
        
        public bool HasActiveBait => activeBait != null && remainingCatchCount > 0;

        public float ActiveBaitLuck => HasActiveBait ? activeBait.FishLuckBonus : 0f;
        
        public FishingBaitDataSO ActiveBait => activeBait;
        
        public int RemainingCatchCount => remainingCatchCount;

        public void ActivateBait(FishingBaitDataSO bait)
        {
            if (bait == null)
            {
                return;
            }
            
            activeBait = bait;
            remainingCatchCount = bait.EffectiveCatchCount;

            Debug.Log(
                $"{bait.name} 사용: " +
                $"행운 +{bait.FishLuckBonus}, " +
                $"{remainingCatchCount}회 적용"
            );
        }

        public void ConsumeOneUse()
        {
            if (!HasActiveBait)
            {
                return;
            }
            
            remainingCatchCount--;

            if (remainingCatchCount <= 0)
            {
                ClearBait();
            }
        }

        private void ClearBait()
        {
            activeBait = null;
            remainingCatchCount = 0;

            Debug.Log("미끼 효과가 종료되었습니다.");
        }
    }
}