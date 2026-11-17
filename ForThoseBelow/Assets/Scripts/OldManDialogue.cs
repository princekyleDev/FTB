using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class OldManDialogue : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform player;

    [SerializeField, Min(0.1f)]
    private float interactionDistance = 2f;

    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private Image portraitImage;

    [Header("Character Names")]
    [SerializeField] private string oldManName = "Old Man";
    [SerializeField] private string playerName = "Fierren";

    [Header("Character Portraits")]
    [SerializeField] private Sprite oldManPortrait;
    [SerializeField] private Sprite fierrenPortrait;

    [Header("Typewriter")]
    [SerializeField, Min(0f)]
    private float characterDelay = 0.04f;

    [Header("Quest")]
    [SerializeField] private UnityEvent onQuestAccepted;

    public bool IsOpen { get; private set; }
    public bool QuestAccepted { get; private set; }

    private enum Stage
    {
        Greeting,
        FirstChoices,
        FirstResponse,
        PlayerQuestion,
        ExplainProblem,
        SecondChoices,
        Offer
    }

    private Stage stage;

    private Coroutine typingCoroutine;
    private bool isTyping;
    private bool waitingForChoice;
    private string currentLine;

    private void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    private void Update()
    {
        if (player == null ||
            dialoguePanel == null ||
            dialogueText == null)
        {
            return;
        }

        if (IsOpen && !PlayerIsNear())
        {
            CloseDialogue();
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        // E opens or closes the conversation.
        if (keyboard.eKey.wasPressedThisFrame)
        {
            if (IsOpen)
                CloseDialogue();
            else if (PlayerIsNear())
                OpenDialogue();

            return;
        }

        if (!IsOpen)
            return;

        // Number keys only work on a choice screen.
        if (waitingForChoice)
        {
            if (keyboard.digit1Key.wasPressedThisFrame ||
                keyboard.numpad1Key.wasPressedThisFrame)
            {
                ChooseReply(1);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame ||
                     keyboard.numpad2Key.wasPressedThisFrame)
            {
                ChooseReply(2);
            }

            return;
        }

        // Space reveals text or advances the conversation.
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            if (isTyping)
                RevealLine();
            else
                AdvanceDialogue();
        }
    }

    private bool PlayerIsNear()
    {
        return Vector2.Distance(
            player.position,
            transform.position
        ) <= interactionDistance;
    }

    private void OpenDialogue()
    {
        IsOpen = true;
        dialoguePanel.SetActive(true);

        stage = Stage.Greeting;

        ShowLine(
            false,
            "Hey, you! Yes, you—the one in that unfortunate, " +
            "raggedy getup!"
        );
    }

    private void SetSpeaker(bool isFierren)
    {
        if (speakerNameText != null)
        {
            speakerNameText.text = isFierren
                ? playerName
                : oldManName;
        }

        if (portraitImage != null)
        {
            Sprite portrait = isFierren
                ? fierrenPortrait
                : oldManPortrait;

            portraitImage.sprite = portrait;
            portraitImage.enabled = portrait != null;
            portraitImage.preserveAspect = true;
        }
    }

    private void ShowLine(bool isFierren, string line)
    {
        StopTyping();

        waitingForChoice = false;
        SetSpeaker(isFierren);

        currentLine = line;
        dialogueText.text = currentLine;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        int characterCount = dialogueText.textInfo.characterCount;

        if (characterDelay <= 0f || characterCount == 0)
        {
            FinishLine();
            return;
        }

        isTyping = true;
        typingCoroutine = StartCoroutine(TypeLine(characterCount));
    }

    private IEnumerator TypeLine(int characterCount)
    {
        for (int i = 1; i <= characterCount; i++)
        {
            dialogueText.maxVisibleCharacters = i;

            yield return new WaitForSecondsRealtime(characterDelay);
        }

        typingCoroutine = null;
        FinishLine();
    }

    private void RevealLine()
    {
        StopTyping();
        FinishLine();
    }

    private void FinishLine()
    {
        isTyping = false;

        string hint = stage == Stage.Offer
            ? "[Space] Finish   [E] Close"
            : "[Space] Continue   [E] Close";

        dialogueText.text = currentLine +
            "\n\n<size=75%>" + hint + "</size>";

        dialogueText.maxVisibleCharacters = int.MaxValue;
    }

    private void ShowChoices(string firstReply, string secondReply)
    {
        StopTyping();

        // Replace the NPC's dialogue with Fierren's choices.
        SetSpeaker(true);
        waitingForChoice = true;

        dialogueText.text =
            "[1] " + firstReply +
            "\n\n[2] " + secondReply +
            "\n\n<size=75%>Press 1 or 2 to reply   [E] Close</size>";

        dialogueText.maxVisibleCharacters = int.MaxValue;
    }

    private void AdvanceDialogue()
    {
        switch (stage)
        {
            case Stage.Greeting:
                stage = Stage.FirstChoices;

                ShowChoices(
                    "Well, that’s rude…",
                    "That’s funny coming from you."
                );
                break;

            case Stage.FirstResponse:
                stage = Stage.PlayerQuestion;

                ShowLine(
                    true,
                    "What’s wrong?"
                );
                break;

            case Stage.PlayerQuestion:
                stage = Stage.ExplainProblem;

                ShowLine(
                    false,
                    "My mechanical leg’s given out. Been " +
                    "rattling for weeks, and this morning it " +
                    "finally decided to retire. Think you " +
                    "could lend an old man a hand?"
                );
                break;

            case Stage.ExplainProblem:
                stage = Stage.SecondChoices;

                ShowChoices(
                    "What’s in it for me?",
                    "Eh, sure. Why not, old man."
                );
                break;

            case Stage.Offer:
                CloseDialogue();
                break;
        }
    }

    private void ChooseReply(int choice)
    {
        if (!waitingForChoice)
            return;

        waitingForChoice = false;

        if (stage == Stage.FirstChoices)
        {
            stage = Stage.FirstResponse;

            if (choice == 1)
            {
                ShowLine(
                    false,
                    "Aye, that came out sharper than I meant. " +
                    "I’m in a bit of a fix. Forgive the temper."
                );
            }
            else
            {
                ShowLine(
                    false,
                    "Hah! Fair enough. Suppose I’m hardly a " +
                    "picture of elegance myself. Forgive the " +
                    "temper I’m in a bit of a fix."
                );
            }
        }
        else if (stage == Stage.SecondChoices)
        {
            stage = Stage.Offer;

            if (choice == 1)
            {
                ShowLine(
                    false,
                    "Straight to business, eh? Bring me " +
                    "10 pieces of Arcanium, and I’ll teach you " +
                    "my secret welding technique. A useful " +
                    "trick for an artificer provided you like " +
                    "your repairs to stay repaired?"
                );
            }
            else
            {
                ShowLine(
                    false,
                    "“Old man,” is it? Help me get this leg " +
                    "working, and we’ll see if you can keep up! " +
                    "Bring me 10 pieces of Arcanium, and I’ll " +
                    "teach you my secret welding technique. " +
                    "Consider it a thank you!"
                );
            }

            if (!QuestAccepted)
            {
                QuestAccepted = true;
                onQuestAccepted?.Invoke();
            }
        }
    }

    public void CloseDialogue()
    {
        StopTyping();

        IsOpen = false;
        waitingForChoice = false;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
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
        CloseDialogue();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            interactionDistance
        );
    }
}