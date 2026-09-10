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
            OnSelected?.Invoke(fishData);
        }
    }
}