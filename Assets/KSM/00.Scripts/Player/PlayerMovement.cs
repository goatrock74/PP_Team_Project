using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KSM._00.Scripts
{
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private float speed;
        private Vector2 dir;
        private Rigidbody2D _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            _rb.linearVelocity = dir * speed;
        }

        public void OnMove(InputValue value)
        {
            dir = value.Get<Vector2>();
        }
    }
}