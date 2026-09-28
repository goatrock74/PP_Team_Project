using UnityEngine;

public class PlantShopBGM : MonoBehaviour
{
    [SerializeField] private AudioClip bgm;
    void Start()
    {
        if (string.Equals(gameObject.scene.name, "0.1BaseScene", System.StringComparison.OrdinalIgnoreCase)) return;
        SoundManager.Instance.PlayBGM(bgm);
    }

}
