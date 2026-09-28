using KSM._00.Scripts.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
namespace PJH._01.Scripts
{
    public class BaitConfirmUI : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private FishingBaitManager fishingBaitManager;
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Image baitIcon; 
        
        
        [Header("등장 애니메이션")]
        [SerializeField] private float openDuration = 0.22f;
        [SerializeField, Range(0.1f, 1f)] private float startScaleRatio = 0.82f;

        private Vector3 originalPanelScale;
        private bool scaleCached;

        private void CachePanelScale()
        {
            if (scaleCached || panel == null)
                return;

            originalPanelScale = panel.transform.localScale;
            scaleCached = true;
        }

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
            
            CachePanelScale();

// 이전에 실행 중이던 애니메이션 제거
            panel.transform.DOKill();

// 활성화되기 전에 작게 만들어 순간적으로 크게 보이는 현상 방지
            panel.transform.localScale = originalPanelScale * startScaleRatio;
            panel.SetActive(true);

// 원래 크기로 부드럽게 확대
            panel.transform
                .DOScale(originalPanelScale, openDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
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