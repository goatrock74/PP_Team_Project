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


        public void SetUp(FishDataSO data, Action<FishDataSO> selectedCallback)
        {
            fishData = data;
            OnSelected = selectedCallback;

            fishIcon.sprite = data.icon;
            
            button.onClick.RemoveListener(SelectFish);
            button.onClick.AddListener(SelectFish);
            
        }

        private void SelectFish()
        {
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