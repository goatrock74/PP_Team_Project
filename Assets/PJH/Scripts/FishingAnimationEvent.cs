using UnityEngine;
using UnityEngine.InputSystem;

namespace PJH.Scripts
{
    public class FishingAnimationEvent : MonoBehaviour
    {
        [SerializeField] private PlayerFishing playerFishing;
        
    
        public void OnBobberLand()
        {
            playerFishing.CheckBobberLanding();
        }

        public void OnHookingFinished()
        {
            playerFishing.FinishFishing();
        }
    }
}
