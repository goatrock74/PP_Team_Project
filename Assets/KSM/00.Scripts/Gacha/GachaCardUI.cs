using TMPro;
using UnityEngine;
using UnityEngine.UI;
 
namespace KSM._00.Scripts.Items
{
    /// <summary>
    /// 룰렛 띠에 늘어서는 카드 한 장.
    ///
    /// 프리팹 구조:
    ///   Card          Image(배경) + GachaCardUI
    ///    ├ Frame      Image        ← 등급 색이 칠해지는 테두리 (선택)
    ///    ├ Icon       Image
    ///    ├ Name       TextMeshPro
    ///    └ Rarity     TextMeshPro
    ///
    /// 모든 자식의 Raycast Target 은 꺼두는 게 좋다 (클릭할 일이 없다).
    /// </summary>
    public class GachaCardUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text rarityText;
 
        [Tooltip("등급 색으로 물들일 테두리/배경. 없어도 됨")]
        [SerializeField] private Image frameImage;
 
        public void SetEntry(in LootEntry entry)
        {
            if (entry.item == null) return;
 
            Color color = ItemRarityUtil.Color(entry.rarity);
 
            if (iconImage != null)
            {
                iconImage.enabled = entry.item.icon != null;
                iconImage.sprite = entry.item.icon;
            }
 
            if (nameText != null)
                nameText.text = entry.item.DisplayName;
 
            if (rarityText != null)
            {
                rarityText.text = ItemRarityUtil.DisplayName(entry.rarity);
                rarityText.color = color;
            }
 
            if (frameImage != null)
                frameImage.color = color;
        }
    }
}