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
        private Action<FishDataSO> OnSelected;
        private static FishSlotUI selectedSlot;
        private bool isDiscovered;
        
        [Header("Icon color")]
        [SerializeField] private Color discoveredColor = Color.white;

        [SerializeField] private Color unDiscoveredColor = Color.black;


        public void SetUp(FishDataSO data, bool disCovered, Action<FishDataSO> selectedCallback)
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
            if (!isDiscovered)
            {
                Debug.Log("아직 잡지 못함");
                return;
            }
            
            if (selectedSlot != null && selectedSlot != this)
            {
                selectedSlot.SetSelected(false);
            }

            selectedSlot = this;
            SetSelected(true);
            
            
            
            OnSelected?.Invoke(fishData);
        }

        public void SetSelected(bool isSelected)
        {
            fishIcon.color = isSelected ? new Color(0.55f, 0.55f, 0.55f, 1f) : Color.white;
        }
    }
}