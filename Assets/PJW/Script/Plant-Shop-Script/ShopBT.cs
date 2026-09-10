using UnityEngine;
using UnityEngine.UI;

public class ShopBT : MonoBehaviour
{
    private static Image currentlySelectedImage = null;

    private Image clickimage;

    private void Awake()
    {
        // 0번: 본인(Button) Image, 1번: 자식 Image
        clickimage = GetComponentsInChildren<Image>()[1];
    }

    private void Start()
    {
        // 알파값은 0.0f ~ 1.0f 사이의 값이어야 합니다.
        clickimage.color = new Color(1f, 1f, 1f, 0f);
    }

    public void ClcikItemIcon()
    {
        // 1. 이미 켜져 있는 자기 자신을 다시 클릭한 경우 -> 끄기
        if (currentlySelectedImage == clickimage)
        {
            SetImageAlpha(clickimage, 0f);
            currentlySelectedImage = null; // 선택 해제
        }
        // 2. 다른 버튼을 클릭했거나 아무것도 안 켜져 있던 경우 -> 기존 것 끄고 새 것 켜기
        else
        {
            // 이전에 켜져 있던 다른 버튼이 있다면 끄기
            if (currentlySelectedImage != null)
            {
                SetImageAlpha(currentlySelectedImage, 0f);
            }

            // 현재 버튼 켜기
            SetImageAlpha(clickimage, 1f);
            currentlySelectedImage = clickimage; // 현재 켜진 버튼으로 갱신
        }
    }

    private void SetImageAlpha(Image img,float alpha)
    {
        Color color = img.color;
        color.a = alpha;
        img.color = color;
    }
}
