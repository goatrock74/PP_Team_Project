using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Plant_Shop_BT : MonoBehaviour
{
    private static Plant_Shop_BT currentlySelectedBT = null;

    [Header("선택 연출 테두리 Image (자식 이미지)")]
    [SerializeField] private Image clickimage;

    [Header("아이콘 표시용 Image (단일 이미지)")]
    [SerializeField] private Image itemIconImage;

    private Item currentItem;
    private ItemDetailPanel detailPanel;

    private void Awake()
    {
        if (clickimage == null)
        {
            Image[] images = GetComponentsInChildren<Image>();
            if (images.Length > 1)
            {
                clickimage = images[1];
            }
        }
    }

    private void OnEnable()
    {
        // 켜질 때 선택 테두리 초기화
        if (clickimage != null)
        {
            SetImageAlpha(clickimage, 0f);
        }
    }

    public void SetItem(Item item, ItemDetailPanel panel)
    {
        currentItem = item;
        detailPanel = panel;

        // 아이템 데이터가 없으면 비활성화
        if (currentItem == null)
        {
            gameObject.SetActive(false);
            return;
        }

        // 아이템 데이터가 있으면 확실하게 활성화 후 UI 업데이트
        gameObject.SetActive(true);
        UpdateItemUI();
    }

    public void UpdateItemUI()
    {
        if (currentItem != null && itemIconImage != null)
        {
            itemIconImage.sprite = currentItem.Item_icon;
        }
    }

    // [버튼 클릭 이벤트]
    public void ClcikItemIcon()
    {
        if (currentItem == null) return;

        // 1. 토글(해제)
        if (currentlySelectedBT == this)
        {
            DeselectSelf();
            currentlySelectedBT = null;

            if (detailPanel != null)
            {
                detailPanel.HideDetail();
            }
        }
        // 2. 새로운 버튼 선택
        else
        {
            if (currentlySelectedBT != null)
            {
                currentlySelectedBT.DeselectSelf();
            }

            currentlySelectedBT = this;
            SelectSelf();

            if (detailPanel != null)
            {
                detailPanel.ShowDetail(currentItem, UpdateItemUI);
            }
        }
    }

    private void SelectSelf()
    {
        if (clickimage != null) SetImageAlpha(clickimage, 1f);
    }

    private void DeselectSelf()
    {
        if (clickimage != null) SetImageAlpha(clickimage, 0f);
    }

    private void SetImageAlpha(Image img, float alpha)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }

    public void SettingSelectBT()
    {
        if (currentlySelectedBT == null) return;

        currentlySelectedBT.DeselectSelf();
        currentlySelectedBT = null;

        if (detailPanel != null)
        {
            detailPanel.HideDetail();
        }
    }
}
