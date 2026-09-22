using UnityEngine;
using UnityEngine.InputSystem;

public class NPC : MonoBehaviour
{
    public DialogueData dialogue;

    public void TriggerDialogue()
    {
        DialogueManager manager = FindFirstObjectByType<DialogueManager>();
        if (manager != null && manager.isChat == false)
        {
            manager.isChat = true;
            manager.StartDialogue(dialogue);
        }
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos3D = mainCam.ScreenToWorldPoint(mouseScreenPos);
            Vector2 mouseWorldPos2D = new Vector2(mouseWorldPos3D.x, mouseWorldPos3D.y);

            Collider2D hitCollider = Physics2D.OverlapPoint(mouseWorldPos2D);

            if (hitCollider != null && hitCollider.gameObject == gameObject)
            {
                Debug.Log("NPC 클릭 성공!");
                TriggerDialogue();
            }
        }
    }
}
