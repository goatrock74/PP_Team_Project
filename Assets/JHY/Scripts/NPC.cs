using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class NPC : MonoBehaviour
{
    public DialogueData dialogue;

    [Header("사용할 DialogueManager")]
    [SerializeField] private DialogueManager dialogueManager;

    [SerializeField] private Vector2 boxSize = new Vector2(3f, 3f);
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private GameObject ui;
    private readonly List<Collider2D> detectionResults = new List<Collider2D>();
    [SerializeField] private AudioClip Esound;
    public void TriggerDialogue()
    {
        if (dialogueManager != null && dialogueManager.isChat == false)
        {
            dialogueManager.isChat = true;
            SoundManager.Instance.PlaySFX(Esound);
            dialogueManager.StartDialogue(dialogue);
        }
    }



    private void Update()
    {

        // Check every overlap so the NPC or terrain cannot hide the player.
        Physics2D.OverlapBox(transform.position, boxSize, 0f,
            new ContactFilter2D().NoFilter(), detectionResults);
        bool isDetected = false;
        foreach (Collider2D hit in detectionResults)
        {
            if (hit.CompareTag(targetTag))
            {
                isDetected = true;
                break;
            }
        }

        // UI 켜고 끄기
        if (ui != null) ui.SetActive(isDetected);

        // 감지 중 E키 입력 시 씬 이동
        if (isDetected && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            TriggerDialogue();
        }
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, boxSize);
    }
}
