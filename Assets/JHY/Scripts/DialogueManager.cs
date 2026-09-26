using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

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
        if (data == null || dialoguePanel == null || nameText == null || dialogueText == null)
        {
            isChat = false;
            Debug.LogError("DialogueManager의 대사 데이터와 UI 연결을 확인하세요.", this);
            return;
        }

        EndDialogue();
        currentDialogue = data;
        currentIndex = 0;
        isChat = true;

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

        if (currentDialogue.sentences != null && currentIndex < currentDialogue.sentences.Length)
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

    public void EndDialogue()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;
        isChat = false;
        currentDialogue = null;
        currentIndex = 0;
        if (dialogueText != null) dialogueText.text = "";
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    // Button OnClick에서 Build Settings의 씬 번호를 입력합니다.
    public void NextScene(int scenenumber)
    {
        if (scenenumber < 0 || !Application.CanStreamedLevelBeLoaded(scenenumber))
        {
            Debug.LogError($"이동할 씬 번호 {scenenumber}를 Build Settings에서 확인하세요.", this);
            return;
        }

        EndDialogue();
        SceneManager.LoadScene(scenenumber);
    }
}
