using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PJH.Scripts
{
    public class PlayerFishing : MonoBehaviour
    {
        [Header("나중에 뺄것")]
        private Rigidbody2D rigid;
        private Vector3 movedir;
        [SerializeField] private float moveSpeed;
        [SerializeField] private Animator animator;
        private readonly int _hashFishing = Animator.StringToHash("Base Layer.Fishing");
        private readonly int _hashFishHook = Animator.StringToHash("Base Layer.FishingHook");
        private readonly int _hashIdle = Animator.StringToHash("Base Layer.Idle");
        private const int BaseLayer = 0;
        
        private enum FishingState
        {
            Idle,
            WaitingBite,
            Hooking
        }
    
        [Header("Fishing Settings")]
        [SerializeField] private FishingAreaCheck fishingAreaCheck;
        private bool isFishing;
        private FishingState state = FishingState.Idle;
        private bool canClick = true;
        
        
    

        private void Awake()
        {
            rigid = GetComponent<Rigidbody2D>();
            canClick = true;
        }

        public void CheckBobberLanding()
        {
            bool canFish = fishingAreaCheck.IsFishingLayer();
            
            if(canFish)
            {
                Debug.Log("낚시 가능 구역");
                canClick = true;
                //미니게임 코드
            }
            
            else
            {
                animator.Play(_hashIdle, BaseLayer, 0f);
                FinishFishing();
            }
        }


        private void Update()
        {
            if (Mouse.current.leftButton.wasPressedThisFrame && canClick)
            {
                if (!isFishing)
                {
                    Debug.Log("Fishing 애니메이션 실행");
                    canClick = false;
                    animator.Play(_hashFishing, BaseLayer, 0f);
                }

                else
                {
                    Debug.Log("FishHook 애니메이션 실행");
                    canClick = false;
                    animator.Play(_hashFishHook, BaseLayer, 0f);
                }
            
            }
        }

        public void StartFishing()
        {
            isFishing = true;
        }

        public void FinishFishing()
        {
            isFishing = false;
            canClick = true;
        }

        private void FixedUpdate()
        {
            if (isFishing)
            {
                rigid.linearVelocity = Vector2.zero;
                return;
            }
            rigid.linearVelocity = movedir * moveSpeed;
        }
    
        
        private void OnMove(InputValue value)
        {
            movedir = value.Get<Vector2>();
        }

        public void HandBobberLand()
        {
            throw new NotImplementedException();
        }
    }
}
