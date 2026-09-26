using UnityEngine;

public class BackgroundScroll : MonoBehaviour
{
    [SerializeField] private float scrollSpeed = 2f;

    private SpriteRenderer spriteRenderer;
    private float width;

    private void Start()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            width = spriteRenderer.bounds.size.x;

        }
        else
        {
            Debug.LogError("SpriteRenderer를 찾을 수 없습니다!");
        }
    }

    private void Update()
    {
        transform.Translate(Vector3.left * scrollSpeed * Time.deltaTime);

        float screenLeft =
            Camera.main.transform.position.x -
            Camera.main.orthographicSize * Camera.main.aspect;

        if (spriteRenderer.bounds.max.x <= screenLeft)
        {
            transform.position += Vector3.right * width * 2f;
        }
    }
}
