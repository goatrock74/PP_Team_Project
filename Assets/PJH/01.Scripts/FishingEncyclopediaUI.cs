using UnityEngine;

namespace PJH.Scripts
{
    public class FishingEncyclopediaUI : MonoBehaviour
    {
        [SerializeField] private FishDataSO[] allFishData;
        [SerializeField] private FishSlotUI[] fishSlots;
        [SerializeField] private FishDetailPanelUI detailPanel;

        private void SetupFishSlot()
        {
            for (int i = 0; i < allFishData.Length; i++)
            {
                fishSlots[i].SetUp(allFishData[i], detailPanel.ShowFishData);
            }
        }
    }
}