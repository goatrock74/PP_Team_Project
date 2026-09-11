using TMPro;
using UnityEngine;
using UnityEngine.UI;
 
namespace KSM._00.Scripts.Items
{
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