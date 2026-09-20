using KSM._00.Scripts.Items;
using TMPro;
using UnityEngine;

namespace PJH._01.Scripts
{
    /// <summary>
    /// 미끼를 쓸지 물어보는 확인창.
    /// Confirm / Cancel 을 버튼의 OnClick 에 연결해서 쓴다.
    /// </summary>
    public class BaitConfirmUI : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private FishingBaitManager fishingBaitManager;
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI messageText;

        private FishingBaitDataSO selectedBait;
        private PlayerInventory selectedPlayer;

        /// <summary>창이 떠 있는가</summary>
        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            if (fishingBaitManager == null)
                fishingBaitManager = FindFirstObjectByType<FishingBaitManager>(FindObjectsInactive.Include);

            if (panel == null)
                Debug.LogError("[미끼확인] Panel 칸이 비어있습니다.", this);

            if (messageText == null)
                Debug.LogError("[미끼확인] Message Text 칸이 비어있습니다.", this);
        }

        public void Open(PlayerInventory player, FishingBaitDataSO bait)
        {
            if (player == null || bait == null) return;
            if (panel == null) return;                 // Awake 에서 이미 에러를 띄웠다

            selectedPlayer = player;
            selectedBait = bait;

            if (messageText != null)
            {
                messageText.text = $"{bait.DisplayName}를 사용하시겠습니까?\n" +
                                   $"다음 낚시 {bait.EffectiveCatchCount}회 · " +
                                   $"희귀 확률 +{bait.FishLuckBonus * 100f:0}%";
            }

            panel.SetActive(true);
        }

        public void Confirm()
        {
            if (selectedPlayer == null || selectedBait == null) { Close(); return; }

            if (!selectedPlayer.CanUseHeld || selectedPlayer.HeldItem != selectedBait)
            {
                Debug.LogWarning("[미끼확인] 손에 든 미끼가 바뀌어서 사용할 수 없습니다.");
                Close();
                return;
            }

            // ★ 미끼를 먼저 소모하고 나서 효과를 건다.
            //   순서를 바꾸면 소모에 실패했을 때 효과만 공짜로 걸린다
            if (!selectedPlayer.ConsumeHeld(1))
            {
                Debug.LogWarning($"[미끼확인] {selectedBait.DisplayName} 을(를) 인벤토리에서 빼지 못했습니다.");
                Close();
                return;
            }

            if (fishingBaitManager == null)
            {
                Debug.LogError("[미끼확인] 씬에 FishingBaitManager 가 없습니다. " +
                               "미끼만 없어지고 효과는 안 걸립니다.", this);
                Close();
                return;
            }

            fishingBaitManager.ActivateBait(selectedBait);
            Close();
        }

        public void Cancel() => Close();

        private void Close()
        {
            selectedPlayer = null;
            selectedBait = null;

            if (panel != null) panel.SetActive(false);
        }
    }
}