using System;
using UnityEngine;

namespace PJH.Scripts
{
    public class FishCollectionUI : MonoBehaviour
    {
        [SerializeField] private FishSlotUI[]  fishSlots;
        [SerializeField] private FishDataSO[] fishDataList;
        [SerializeField] private FishDetailPanelUI fishDetailPanelUI;

        private void Start()
        {
            int count = Mathf.Min(fishSlots.Length, fishDataList.Length);

            for (int i = 0; i < count; i++)
            {
                fishSlots[i].SetUp(fishDataList[i], fishDetailPanelUI.ShowFishData);
            }
        }
    }
}