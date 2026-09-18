using UnityEngine;
using TMPro;

[System.Serializable]
public class DialogueData
{
    public string npcName;
    [TextArea(2, 5)]
    public string[] sentences;
}

public class DialogueManager : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject dialoguePanel;
    public TMP_Text nameText;
    public TMP_Text dialogueText;

    private DialogueData currentDialogue;
    private int currentIndex = 0;

    public void StartDialogue(DialogueData data)
    {
        currentDialogue = data;
        currentIndex = 0;

        dialoguePanel.SetActive(true);
        nameText.text = currentDialogue.npcName + ":";
        DisplayNextSentence();
    }

    public void DisplayNextSentence()
    {
        if (currentDialogue == null) return;

        if (currentIndex < currentDialogue.sentences.Length)
        {
            dialogueText.text = currentDialogue.sentences[currentIndex];
            currentIndex++;
        }
        else
        {
            EndDialogue();
        }
    }

    void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        currentDialogue = null;
    }
}