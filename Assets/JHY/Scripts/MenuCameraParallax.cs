using UnityEngine;
using UnityEngine.InputSystem;

public class MenuCameraParallax : MonoBehaviour
{
    [Header("Camera Movement")]
    [SerializeField] private float moveAmountX = 0.3f;
    [SerializeField] private float moveAmountY = 0.2f;
    [SerializeField] private float smoothSpeed = 3f;

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        float mouseX = mousePosition.x / Screen.width * 2f - 1f;
        float mouseY = mousePosition.y / Screen.height * 2f - 1f;

        float targetX = startPosition.x + mouseX * moveAmountX;
        float targetY = startPosition.y + mouseY * moveAmountY;

        Vector3 targetPosition = new Vector3(
            targetX,
            targetY,
            startPosition.z
        );

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            smoothSpeed * Time.deltaTime
        );
    }
}