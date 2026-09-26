using System.Collections.Generic;
using PJH._01.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace PJH.Scripts
{
    public class FishCollectionUI : MonoBehaviour
    {
        [Header("도감 분류")]
        [SerializeField] private FishSlotUI[]  fishSlots;
        [SerializeField] private FishDataSO[] fishDataList;
        
        [Header("상세 정보")]
        [SerializeField] private FishDetailPanelUI fishDetailPanelUI;


        [Header("도감 관리")]
        [SerializeField] private FishCollectionManager collectionManager;
        
        [Header("분류 버튼")]
        [SerializeField] private Button seaButton;
        [SerializeField] private Button freshwaterButton;
        [SerializeField] private Button trashButton;

        private FishCollectionCategory currentCategory;

        private bool hasStartded;

        private void OnEnable()
        {
            collectionManager.OnCollectionChanged += RefreshSlots;
            if(hasStartded)
                ShowSeaFish();
            
        }

        private void Start()
        {
            hasStartded = true;
            ShowSeaFish();
        }


        private void ChangeCategory(FishCollectionCategory category)
        {
            currentCategory = category;
            
            FishSlotUI.ClearSelection();
            fishDetailPanelUI.ClearFishData();

            UpdateCategoryButtons();
            RefreshSlots();

        }

        private void UpdateCategoryButtons()
        {
            seaButton.interactable = currentCategory !=  FishCollectionCategory.Sea;
            freshwaterButton.interactable = currentCategory != FishCollectionCategory.FreshWater;
            trashButton.interactable = currentCategory != FishCollectionCategory.Trash;
        }
        public void ShowSeaFish()
        {
            ChangeCategory(FishCollectionCategory.Sea);
        }

        public void CloseCollection()
        {
            gameObject.SetActive(false);
        }

        public void ShowFreshwaterFish()
        {
            ChangeCategory(FishCollectionCategory.FreshWater);
        }

        public void ShowTrash()
        {
            ChangeCategory(FishCollectionCategory.Trash);
        }

        public void OnDisable()
        {
            collectionManager.OnCollectionChanged -= RefreshSlots;
        }

        private void RefreshSlots()
        {
            List<FishDataSO> filteredFishList = new();

            foreach (FishDataSO fishData in fishDataList)
            {
                if (fishData == null) continue;

                if (fishData.collectionCategory == currentCategory)
                {
                    filteredFishList.Add(fishData);
                }
            }

            for (int i = 0; i < fishSlots.Length; i++)
            {
                if (i >= filteredFishList.Count)
                {
                    fishSlots[i].gameObject.SetActive(false);
                    continue;
                }
                
                FishDataSO fishData =  filteredFishList[i];

                bool isDiscovered = collectionManager.IsDiscovered(fishData);
                
                fishSlots[i].gameObject.SetActive(true);
                fishSlots[i].SetUp(fishData, isDiscovered, fishDetailPanelUI.ShowFishData);
            }
        }
    }
}