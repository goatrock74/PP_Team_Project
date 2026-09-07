using PJH.Scripts;
using UnityEngine;

namespace PJH._01.Scripts
{
    public class FishingController : MonoBehaviour
    {
        [SerializeField] private FishingAreaCheck fishingAreaCheck;
        [SerializeField] private FishSelector fishSelector;
        [SerializeField] private FishingMiniGame fishingMiniGame;
        [SerializeField] private FishingSettingSO fishingSettingSO;
        [SerializeField] private GameObject splashParticle;

    }
}