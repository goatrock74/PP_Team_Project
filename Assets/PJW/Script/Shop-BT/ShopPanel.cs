using Assets.PJW.Script.SO_Script;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShopPanel : MonoBehaviour
{
    [Header("계절별 ItemListSO")]
    [SerializeField] private ItemListSO[] itemLists;

    [Header("상점 버튼")]
    [SerializeField] private ShopBT[] itemButtons;

    private ItemListSO currentList;

    private void Start()
    {
        ChangeSeason(SeasonType.Autumn);
    }

    private void Update()
    {
        if(Keyboard.current.lKey.wasPressedThisFrame)
        {
            ChangeSeason(SeasonType.Winter);
        }
    }

    public void ChangeSeason(SeasonType season)
    {
        currentList = itemLists[(int)season];

        SetItemList();
    }

    private void SetItemList()
    {
        if (currentList == null)
            return;

        for (int i = 0; i < itemButtons.Length; i++)
        {
            if (i < currentList.itemList.Length)
            {
                itemButtons[i].SetItem(currentList.itemList[i]);
            }
            else
            {
                itemButtons[i].gameObject.SetActive(false);
            }
        }
    }
}
