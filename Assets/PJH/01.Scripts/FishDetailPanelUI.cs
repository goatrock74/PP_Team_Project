using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PJH.Scripts
{
    public class FishDetailPanelUI : MonoBehaviour
    {
        [Header("fish detail information")]
        [SerializeField] private Image fishIcon;
        [SerializeField] private TextMeshProUGUI fishName;
        [SerializeField] private TextMeshProUGUI fishPrice;
        
        private FishDataSO currentFishData;

        private void Awake()
        {
            ClearFishData();
        }

        public void ShowFishData(FishDataSO fishData)
        {
            if (fishData == null)
            {
                ClearFishData();
                return;
            }
            
            currentFishData = fishData;
            fishIcon.sprite = fishData.icon;
            fishIcon.enabled = true;
            
            fishName.text = fishData.displayName;
            fishPrice.text = $"{fishData.basePrice} G";
            
            
        }

        private void ClearFishData()
        {
            currentFishData = null;
            fishIcon.sprite = null;
            fishIcon.enabled = false;

            fishName.text = "물고기를 선택하세요";
            fishPrice.text = string.Empty;
        }
    }
}
