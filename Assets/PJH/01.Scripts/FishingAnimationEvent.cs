using PJH._01.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PJH.Scripts
{
    public class FishingAnimationEvent : MonoBehaviour
    {
        [SerializeField] private PlayerFishing playerFishing;
        [SerializeField] private FishingAudioPlayer fishingAudio;
    
        public void StartFishing()
        {
            playerFishing.StartFishing();
        }

        public void CheckBobberLanding()
        {
            playerFishing.CheckBobberLanding();
        }

        public void FinishFishing()
        {
            playerFishing.FinishFishing();
        }

        public void OnCastReleased()
        {
            fishingAudio?.PlayCast();
        }

    }
}
