using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    [Header("씬 전환 페이드 연출")]
    [SerializeField] private Image fadePanelImage; // 페이드에 사용할 검은색 UI Image
    [SerializeField] private float fadeDuration = 1.0f; // 암전되는 시간
    [SerializeField] private GameObject shoppanel;
    private DialogueData currentDialogue;
    private int currentIndex = 0;

    private Coroutine typingCoroutine;
    private bool isTyping = false;

    public bool isChat { get; set; } = false;

    public void StartDialogue(DialogueData data)
    {
        currentDialogue = data;
        currentIndex = 0;

        dialoguePanel.SetActive(true);
        nameText.text = currentDialogue.npcName + ":";

        DisplayNextSentence();
    }
    public void OpenShopPanel()
    {
        shoppanel.SetActive(true);
    }
    public void CloseShopPanel()
    {
        shoppanel.SetActive(false);
    }
    public void DisplayNextSentence()
    {
        if (currentDialogue == null) return;

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
        dialogueText.text = "";
        dialoguePanel.SetActive(false);
    }

    public void NextScene(int scenenumber)
    {
        StartCoroutine(FadeAndLoadSceneRoutine(scenenumber));
    }

    private IEnumerator FadeAndLoadSceneRoutine(int scenenumber)
    {
        EndDialogue();

        if (fadePanelImage != null)
        {
            fadePanelImage.gameObject.SetActive(true);
            fadePanelImage.DOKill();

            Color c = fadePanelImage.color;
            fadePanelImage.color = new Color(c.r, c.g, c.b, 0f);

            yield return fadePanelImage.DOFade(1f, fadeDuration).SetEase(Ease.Linear).WaitForCompletion();
        }

        SceneManager.LoadScene(scenenumber);
    }
    
    
}
