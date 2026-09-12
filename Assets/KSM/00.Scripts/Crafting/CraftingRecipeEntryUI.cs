using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
 
namespace KSM._00.Scripts.Crafting
{
    public class CraftingRecipeEntryUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("표시")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
 
        [Tooltip("이 줄이 선택됐을 때 켜질 표시. 없어도 된다")]
        [SerializeField] private GameObject selectedFrame;
 
        [Tooltip("재료가 모자라면 이름을 이 색으로 흐리게 만든다")]
        [SerializeField] private Color unaffordableColor = new Color(0.6f, 0.6f, 0.6f);
 
        public CraftingRecipeSO Recipe { get; private set; }
        public event Action<CraftingRecipeEntryUI> OnClicked;
 
        private Color _normalNameColor = Color.white;
 
        private void Awake()
        {
            if (nameText != null) _normalNameColor = nameText.color;
        }
 
        public void Bind(CraftingRecipeSO recipe)
        {
            Recipe = recipe;
            if (recipe == null) return;
 
            name = $"Recipe_{recipe.name}";
 
            if (iconImage != null)
            {
                iconImage.enabled = recipe.Icon != null;
                iconImage.sprite = recipe.Icon;
            }
 
            if (nameText != null)
            {
                string count = recipe.resultCount > 1 ? $" x{recipe.resultCount}" : string.Empty;
                nameText.text = recipe.DisplayName + count;
            }
 
            SetSelected(false);
        }
        public void SetAffordable(bool affordable)
        {
            if (nameText != null)
                nameText.color = affordable ? _normalNameColor : unaffordableColor;
        }
 
        public void SetSelected(bool on)
        {
            if (selectedFrame != null) selectedFrame.SetActive(on);
        }
 
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
 
            OnClicked?.Invoke(this);
        }
    }
}
 