using KSM._00.Scripts.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PJH._01.Scripts
{
    public class BaitConfirmUI : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private FishingBaitManager fishingBaitManager;
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Image baitIcon; 

        private FishingBaitDataSO selectedBait;
        private PlayerInventory selectedPlayer;

        public void Open(PlayerInventory player, FishingBaitDataSO bait)
        {
            if (player == null || bait == null) return;

            
            selectedPlayer = player;
            selectedBait = bait;
            baitIcon.sprite = bait.icon;
            
            messageText.text =  $"{bait.DisplayName}를 사용하시겠습니까?\n" +
                                $"다음 낚시 {bait.EffectiveCatchCount}회 · " +
                                $"희귀 확률 +{bait.FishLuckBonus * 100f:0}%";
            
            panel.SetActive(true);
        }

        public void Confirm()
        {
            if (selectedPlayer == null || selectedBait == null)
            {
                Close();
                return;
            }

            if (!selectedPlayer.CanUseHeld || selectedPlayer.HeldItem != selectedBait)
            {
                Debug.LogWarning("손에 들고있는 미끼가 변경되서 사용이 안됨");
                Close();
                return;
            }

            if (!selectedPlayer.ConsumeHeld(1))
            {
                Debug.LogWarning($"{selectedBait.DisplayName}제거 안됨");
                
                Close();
                return;
            }
            
            fishingBaitManager.ActivateBait(selectedBait);
            
            Close();
        }

        public void Cancel()
        {
            Close();
        }

        private void Close()
        {
            selectedPlayer = null;
            selectedBait = null;
            panel.SetActive(false);
        }
    }
}