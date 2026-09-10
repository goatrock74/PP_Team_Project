using UnityEngine;

public class BackgroundScroll : MonoBehaviour
{
    [SerializeField] private float speed = 2f;
    [SerializeField] private float width = 20f;

    void Update()
    {
        transform.position += Vector3.left * speed * Time.deltaTime;

        if (transform.position.x <= -width)
        {
            transform.position += Vector3.right * width * 2f;
        }
    }
}
