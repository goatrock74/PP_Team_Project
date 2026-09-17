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
        [SerializeField] private TextMeshProUGUI fishDescription;
        
        [Header("계절 출현 정보")]
        [SerializeField] private Image springTag;
        [SerializeField] private Image summerTag;
        [SerializeField] private Image autumnTag;
        [SerializeField] private Image winterTag;
        
        [Header("시간 출현 정보")]
        [SerializeField] private Image morningTag;
        [SerializeField] private Image afternoonTag;
        [SerializeField] private Image nightTag;
        private FishDataSO currentFishData;

        private void Awake()
        {
            ClearFishData();
        }

        public void ShowFishData(FishDataSO fishData, bool isDiscovered)
        {
            UpdateSpawnTags(fishData);
            
            if (!isDiscovered)
            {
                ShowUndiscoveredData(fishData);
                return;
            }
            
            ShowDiscoveredData(fishData);

            currentFishData = fishData;
                

            
    }

        private void ShowDiscoveredData(FishDataSO fishData)
        {
            
            if (fishData == null)
            {
                ClearFishData();
                return;
            }
            fishIcon.sprite = fishData.icon;
            fishIcon.enabled = true;

            fishName.text = fishData.displayName;
            fishPrice.text = $"가격 : {fishData.sellPrice.ToString()}";
            fishDescription.text = fishData.description;
        }

        private void ShowUndiscoveredData(FishDataSO fishData)
        {
            currentFishData = fishData;
            fishIcon.sprite = fishData.icon;
            fishIcon.enabled = false;

            fishName.text = "???";
            fishDescription.text = "???";
            fishPrice.text = $"가격 : ???";
        }

        private void UpdateSpawnTags(FishDataSO fishData)
        {
            SetTagColor(springTag, fishData.SpringMultiplier);
            SetTagColor(summerTag, fishData.SummerMultiplier);
            SetTagColor(autumnTag, fishData.AutumnMultiplier);
            SetTagColor(winterTag, fishData.WinterMultiplier);

            SetTagColor(morningTag, fishData.MorningMultiplier);
            SetTagColor(afternoonTag, fishData.AfternoonMultiplier);
            SetTagColor(nightTag, fishData.NightMultiplier);
        }

        private void SetTagColor(Image tag, float multiplier)
        {
            if (multiplier > 0f)
            {
                tag.color = Color.white;
            }
            else
            {
                tag.color = Color.gray;
            }
        }

        public void ClearFishData()
        {
            fishIcon.enabled = false;

            fishName.text = null;
            fishDescription.text = null;
            fishPrice.text = $"가격 : {null}" ;
        }

       
    }
}
