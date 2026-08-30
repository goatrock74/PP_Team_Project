using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestPlayer : MonoBehaviour
{
    private Rigidbody2D rigid;
    private Vector3 movedir;
    [SerializeField] private float moveSpeed;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        rigid.linearVelocity = movedir * moveSpeed;
    }

    private void OnMove(InputValue value)
    {
        movedir = value.Get<Vector2>();
    }
}
