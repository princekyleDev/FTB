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
    [SerializeField] private bool disableOnFinish = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onTypeStart;
    [SerializeField] private UnityEvent onTypeComplete;
    [SerializeField] private UnityEvent onDialogueClosed;

    private Coroutine typingCoroutine;
    private bool isTyping;

    public bool IsTyping => isTyping;

    private void Start()
    {
        if (playOnStart)
            Play();
    }

    // Input is handled by NPCDialogue.cs.

    public void Play()
    {
        Play(dialogue);
    }

    public void Play(string newDialogue)
    {
        if (dialogueText == null)
        {
            Debug.LogWarning(
                "Assign Dialogue Text in the Inspector.",
                this
            );

            return;
        }

        StopTyping();

        dialogue = newDialogue ?? "";

        dialogueText.text = dialogue;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        isTyping = true;

        onTypeStart?.Invoke();

        if (dialogueText.textInfo.characterCount == 0)
        {
            FinishTyping();
            return;
        }

        typingCoroutine = StartCoroutine(TypeText());
    }

    private IEnumerator TypeText()
    {
        int totalCharacters = dialogueText.textInfo.characterCount;

        for (int i = 0; i < totalCharacters; i++)
        {
            dialogueText.maxVisibleCharacters = i + 1;

            if (characterDelay > 0f)
            {
                if (useUnscaledTime)
                {
                    yield return new WaitForSecondsRealtime(
                        characterDelay
                    );
                }
                else
                {
                    yield return new WaitForSeconds(
                        characterDelay
                    );
                }
            }
            else
            {
                yield return null;
            }
        }

        typingCoroutine = null;
        FinishTyping();
    }

    private void FinishTyping()
    {
        isTyping = false;
        onTypeComplete?.Invoke();
    }

    public void Skip()
    {
        if (!isTyping)
            return;

        StopTyping();

        if (dialogueText != null)
            dialogueText.maxVisibleCharacters = int.MaxValue;

        FinishTyping();
    }

    public void Close()
    {
        // Allow closing even while text is still typing.
        StopTyping();

        onDialogueClosed?.Invoke();

        if (disableOnFinish)
            gameObject.SetActive(false);
    }

    public void SetDialogue(string newDialogue)
    {
        dialogue = newDialogue ?? "";
    }

    private void StopTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;
    }

    private void OnDisable()
    {
        StopTyping();
    }
}