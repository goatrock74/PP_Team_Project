using UnityEngine;

public class PlantShopBGM : MonoBehaviour
{
    [SerializeField] private AudioClip bgm;
    void Start()
    {
        SoundManager.Instance.PlayBGM(bgm);
    }

}
