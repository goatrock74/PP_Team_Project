using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
 
namespace KSM._00.Scripts.Crafting
{
    /// <summary>
    /// 제작 목록의 한 줄. 왼쪽에 결과물 아이콘, 오른쪽에 이름.
    ///
    /// 프리팹 구조:
    ///   RecipeEntry     Image(배경, Raycast Target 켜기) + CraftingRecipeEntryUI
    ///    ├ Icon         Image        (Raycast Target 끄기)
    ///    ├ Name         TextMeshPro  (Raycast Target 끄기)
    ///    └ (선택) Selected  Image 등 — Selected Frame 에 연결
    ///
    /// Layout Element 를 붙여 Min Height 를 주면 목록에서 높이가 일정해진다.
    /// </summary>
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
 
        /// <summary>목록이 연결해준다</summary>
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
 
        /// <summary>재료가 되는지에 따라 글자를 흐리게 / 선명하게</summary>
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
 