using System;
using System.Collections.Generic;
using PJH._01.Scripts;
using UnityEngine;

namespace PJH.Scripts
{
    public class FishCollectionUI : MonoBehaviour
    {
        [SerializeField] private FishSlotUI[]  fishSlots;
        [SerializeField] private FishDataSO[] fishDataList;
        [SerializeField] private FishDetailPanelUI fishDetailPanelUI;



        [SerializeField] private FishCollectionManager collectionManager;

        private void OnEnable()
        {
            collectionManager.OnCollectionChanged += RefreshSlots;
            RefreshSlots();
        }

        private void OnDisable()
        {
            collectionManager.OnCollectionChanged -= RefreshSlots;
        }

        private void RefreshSlots()
        {
            int count = Mathf.Min(fishSlots.Length, fishDataList.Length);

            for (int i = 0; i < count; i++)
            {
                FishDataSO fishData = fishDataList[i];

                bool isDiscovered = collectionManager.IsDiscovered(fishData);
                Debug.Log(
                    $"{fishData.displayName} 발견 여부: {isDiscovered}");

                fishSlots[i].SetUp(fishData, isDiscovered, fishDetailPanelUI.ShowFishData);
            }
        }

        private void ShowFishDetail(FishDataSO fishData)
        {
            fishDetailPanelUI.ShowFishData(fishData);
        }
    }
}