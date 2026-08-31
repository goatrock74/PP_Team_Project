using UnityEngine;
using UnityEngine.InputSystem;

namespace PJH.Scripts
{
    public class PlayerFishing : MonoBehaviour
    {
        private Rigidbody2D rigid;
        private Vector3 movedir;
        [SerializeField] private float moveSpeed;
        [SerializeField] private Animator animator;
    
        private readonly int _hashFishing = Animator.StringToHash("Fishing");
        private readonly int _hashFishHook = Animator.StringToHash("FishHook");
        private bool isFishing;
    

        private void Awake()
        {
            rigid = GetComponent<Rigidbody2D>();
        }


        private void Update()
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (!isFishing)
                {
                    isFishing = true;
                    animator.Play(_hashFishing);
                }

                else
                {
                    isFishing = false;
                    animator.Play(_hashFishHook);
                }
            
            }
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
    }
}
