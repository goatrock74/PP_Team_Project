using KSM._00.Scripts.Items;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

namespace PJH._01.Scripts
{
    public class BaitConfirmUI : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private FishingBaitManager fishingBaitManager;
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI messageText;

        private FishingBaitDataSO selectedBait;
        private PlayerInventory selectedPlayer;

        public void Open(PlayerInventory player, FishingBaitDataSO bait)
        {
            if (player == null || bait == null) return;

            
            selectedPlayer = player;
            selectedBait = bait;
            
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
            }
                
        }

        private void Close()
        {
            throw new System.NotImplementedException();
        }
    }
}