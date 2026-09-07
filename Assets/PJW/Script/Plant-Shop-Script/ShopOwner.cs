using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopOwner : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
{
    [SerializeField] private Sprite image_fall;
    [SerializeField] private Sprite image_close;
    private Image imageComp;

    private void Awake()
    {
        imageComp = GetComponent<Image>();
    }

    private void Start()
    {
        imageComp.sprite = image_fall;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        imageComp.sprite = image_close;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        imageComp.sprite = image_fall;
    }
}
