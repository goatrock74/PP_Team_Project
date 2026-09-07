using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    private Vector2 moveDir;
    private Rigidbody2D rb;
    private PlayerAxe PlayerAxe;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        PlayerAxe = GetComponent<PlayerAxe>();
    }
    private void Update()
    {
        if(Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            PlayerAxe.UseAxe(moveDir);
        }
    }
    private void OnMove(InputValue value)
    {
        moveDir = value.Get<Vector2>();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveDir * speed;
    }
}
