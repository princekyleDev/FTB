
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class DialogueTypewriter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text dialogueText;

    [Header("Settings")]
    [TextArea(3, 8)]
    [SerializeField] private string dialogue;

    [SerializeField, Min(0f)]
    private float characterDelay = 0.04f;

    [SerializeField] private bool playOnStart = false;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onTypeStart;
    [SerializeField] private UnityEvent onTypeComplete;

    private Coroutine typingCoroutine;
    private bool isTyping;

    public bool IsTyping => isTyping;

    private void Start()
    {
        if (playOnStart)
            Play();
    }

    public void Play()
    {
        Play(dialogue);
    }

    public void Play(string newDialogue)
    {
        if (dialogueText == null)
            return;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        dialogue = newDialogue;
        dialogueText.text = dialogue;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        isTyping = true;
        onTypeStart?.Invoke();

        typingCoroutine = StartCoroutine(TypeText());
    }

    private IEnumerator TypeText()
    {
        int totalCharacters = dialogueText.textInfo.characterCount;

        for (int i = 0; i < totalCharacters; i++)
        {
            dialogueText.maxVisibleCharacters = i + 1;

            if (useUnscaledTime)
                yield return new WaitForSecondsRealtime(characterDelay);
            else
                yield return new WaitForSeconds(characterDelay);
        }

        typingCoroutine = null;
        isTyping = false;

        onTypeComplete?.Invoke();
    }

    public void Skip()
    {
        if (!isTyping)
            return;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        dialogueText.maxVisibleCharacters = int.MaxValue;
        isTyping = false;

        onTypeComplete?.Invoke();
    }

    public void SetDialogue(string newDialogue)
    {
        dialogue = newDialogue;
    }

    private void OnDisable()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;
    }
}
