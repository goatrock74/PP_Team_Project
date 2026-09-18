using System;
using UnityEngine;
using UnityEngine.UI;

namespace PJH.Scripts
{
    public class FishSlotUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image fishIcon;

        private FishDataSO fishData;
        private Action<FishDataSO, bool> OnSelected;
        private static FishSlotUI selectedSlot;
        private bool isDiscovered;
        
        [Header("Icon color")]
        [SerializeField] private Color discoveredColor = Color.white;

        [SerializeField] private Color unDiscoveredColor = Color.black;


        public void SetUp(FishDataSO data, bool disCovered, Action<FishDataSO, bool> selectedCallback)
        {
            fishData = data;
            OnSelected = selectedCallback;
            isDiscovered = disCovered;

            fishIcon.sprite = data.icon;
            
            fishIcon.color = isDiscovered ? discoveredColor : unDiscoveredColor;
            
            
            button.onClick.RemoveListener(SelectFish);
            button.onClick.AddListener(SelectFish);
            
        }

        private void SelectFish()
        {
            
            if (selectedSlot != null && selectedSlot != this)
            {
                selectedSlot.SetSelected(false);
            }
            
            OnSelected?.Invoke(fishData,  isDiscovered);
            selectedSlot = this;
            SetSelected(true);
            
            
        }

        public static void ClearSelection()
        {
            if (selectedSlot == null) return;
            
            selectedSlot.SetSelected(false);
            selectedSlot = null;
        }

        public void SetSelected(bool isSelected)
        {
            if (!isDiscovered)
            {
                fishIcon.color = unDiscoveredColor;
                return;
            }
            fishIcon.color = isSelected ? new Color(0.55f, 0.55f, 0.55f, 1f) : Color.white;
        }
    }
}