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

        private void Awake()
        {
            if (SaveSlotStore.Active == null) return;
            var catalog = Resources.Load<InventoryItemCatalog>("InventoryItemCatalog");
            string id = SaveSlotStore.GetString("ActiveBait");
            if (catalog == null || catalog.items == null) return;
            foreach (var entry in catalog.items)
                if (entry != null && entry.id == id && entry.item is FishingBaitDataSO bait)
                {
                    activeBait = bait;
                    remainingCatchCount = Mathf.Clamp(SaveSlotStore.GetInt("BaitUses"), 0, bait.EffectiveCatchCount);
                    break;
                }
        }
        private void StageSave()
        {
            if (SaveSlotStore.Active == null) return;
            string id = "";
            var catalog = Resources.Load<InventoryItemCatalog>("InventoryItemCatalog");
            if (HasActiveBait && catalog != null && catalog.items != null)
                foreach (var entry in catalog.items)
                    if (entry != null && entry.item == activeBait) { id = entry.id; break; }
            SaveSlotStore.SetString("ActiveBait", id);
            SaveSlotStore.SetInt("BaitUses", remainingCatchCount);
        }

        public void ActivateBait(FishingBaitDataSO bait)
        {
            if (bait == null)
            {
                return;
            }
            
            activeBait = bait;
            remainingCatchCount = bait.EffectiveCatchCount;
            StageSave();

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
            StageSave();

            if (remainingCatchCount <= 0)
            {
                ClearBait();
            }
        }

        private void ClearBait()
        {
            activeBait = null;
            remainingCatchCount = 0;
            StageSave();

            Debug.Log("미끼 효과가 종료되었습니다.");
        }
    }
}
