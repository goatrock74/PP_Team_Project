using UnityEngine;
using TMPro;
using System.Collections;

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

    [Header("대화 설정")]
    public float textSpeed = 0.05f;

    private DialogueData currentDialogue;
    private int currentIndex = 0;

    private Coroutine typingCoroutine;
    private bool isTyping = false;

    public bool isChat { get; set; } =false;

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

        // 현재 글자가 출력 중이면
        // 바로 문장 전체를 보여줌
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);

            dialogueText.text = currentDialogue.sentences[currentIndex - 1];

            isTyping = false;
            return;
        }

        if (currentIndex < currentDialogue.sentences.Length)
        {
            string sentence = currentDialogue.sentences[currentIndex];
            currentIndex++;

            typingCoroutine = StartCoroutine(TypeText(sentence));
        }
        else
        {
            EndDialogue();
        }
    }

    IEnumerator TypeText(string sentence)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in sentence)
        {
            dialogueText.text += letter;

            yield return new WaitForSeconds(textSpeed);
        }

        isTyping = false;
    }

    void EndDialogue()
    {
        isChat = false;
        dialoguePanel.SetActive(false);
        currentDialogue = null;
        dialogueText.text = "";
    }
}