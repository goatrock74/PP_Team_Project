using UnityEngine;
using UnityEngine.InputSystem;

public class MenuCameraParallax : MonoBehaviour
{
    [Header("Camera Movement")]
    [SerializeField] private float moveAmountX = 0.3f;
    [SerializeField] private float moveAmountY = 0.2f;
    [SerializeField] private float smoothSpeed = 3f;

    private Vector3 initialLocalPos;

    private void Start()
    {
        initialLocalPos = transform.localPosition;
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        float mouseX = (mousePosition.x / Screen.width) * 2f - 1f;
        float mouseY = (mousePosition.y / Screen.height) * 2f - 1f;

        Vector3 targetLocalPos = new Vector3(
            initialLocalPos.x + mouseX * moveAmountX,
            initialLocalPos.y + mouseY * moveAmountY,
            initialLocalPos.z
        );

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetLocalPos,
            smoothSpeed * Time.deltaTime
        );
    }
}