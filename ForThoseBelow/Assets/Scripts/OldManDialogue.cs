using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
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
    [SerializeField] private string questID = "Quest1";
    [SerializeField] private UnityEvent onQuestAccepted;

    [Header("Ending UI")]
    [SerializeField] private GameObject endingPanel;
    [SerializeField] private TMP_Text endingHintText;

    [Header("Ending Page GameObjects")]
    [SerializeField] private GameObject weldingSkillCard;
    [SerializeField] private GameObject newspaperPage1;
    [SerializeField] private GameObject newspaperPage2;

    [Header("Return Scene")]
    [SerializeField] private string titleSceneName = "Titlescreen";

    [Header("Gameplay Scripts To Disable During Ending")]
    [Tooltip(
        "Assign movement, pickup, or other gameplay scripts. " +
        "Do not assign this script or QuestManager."
    )]
    [SerializeField] private MonoBehaviour[] disableDuringEnding;

    public bool IsOpen { get; private set; }
    public bool QuestAccepted { get; private set; }
    public bool IsEndingActive => endingActive;

    private enum Stage
    {
        Greeting,
        FirstChoices,
        FirstResponse,
        PlayerQuestion,
        ExplainProblem,
        SecondChoices,
        Offer,
        Reminder,
        Congratulations,
        WeldingLesson,
        SkillCard,
        Newspaper1,
        Newspaper2
    }

    private Stage stage;

    private Coroutine typingCoroutine;
    private bool isTyping;
    private bool waitingForChoice;
    private string currentLine;

    private bool endingActive;
    private bool loadingTitle;

    private int pageShownFrame;
    private float previousTimeScale;
    private bool[] previousEnabledStates;

    private void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        HideEndingPages();

        if (endingPanel != null)
            endingPanel.SetActive(false);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null || loadingTitle)
            return;

        // Handle ending input before normal interaction.
        if (endingActive)
        {
            HandleEndingInput(keyboard);
            return;
        }

        if (player == null ||
            dialoguePanel == null ||
            dialogueText == null)
        {
            return;
        }

        // Normal dialogue closes if the player walks away.
        if (IsOpen && !PlayerIsNear())
        {
            CloseDialogue();
            return;
        }

        // E opens or closes normal dialogue.
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

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            if (isTyping)
                RevealLine();
            else
                AdvanceDialogue();
        }
    }

    private void HandleEndingInput(Keyboard keyboard)
    {
        if (IsShowingEndingPage())
        {
            bool enterPressed =
                keyboard.enterKey.wasPressedThisFrame ||
                keyboard.numpadEnterKey.wasPressedThisFrame;

            // Require a new press after the page appears.
            if (Time.frameCount > pageShownFrame && enterPressed)
                AdvanceEndingPage();

            return;
        }

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
        QuestManager manager = QuestManager.Instance;

        if (manager != null && manager.IsQuestCompleted(questID))
            return;

        // All items found: begin the turn-in conversation.
        if (manager != null &&
            manager.IsQuestReadyForTurnIn(questID))
        {
            BeginEnding();
            return;
        }

        IsOpen = true;
        dialoguePanel.SetActive(true);

        if (QuestAccepted)
        {
            stage = Stage.Reminder;

            ShowLine(
                false,
                "Still waiting on those 10 pieces of Arcanium, " +
                "Fierren. Come back when you have them all."
            );
        }
        else
        {
            stage = Stage.Greeting;

            ShowLine(
                false,
                "Hey, you! Yes, you—the one in that unfortunate, " +
                "raggedy getup!"
            );
        }
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

        int count = dialogueText.textInfo.characterCount;

        if (characterDelay <= 0f || count == 0)
        {
            FinishLine();
            return;
        }

        isTyping = true;
        typingCoroutine = StartCoroutine(TypeLine(count));
    }

    private IEnumerator TypeLine(int count)
    {
        for (int i = 1; i <= count; i++)
        {
            dialogueText.maxVisibleCharacters = i;

            // Works even when gameplay is paused.
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

        string hint;

        if (endingActive)
        {
            hint = "[Space] Continue";
        }
        else if (stage == Stage.Offer || stage == Stage.Reminder)
        {
            hint = "[Space] Finish   [E] Close";
        }
        else
        {
            hint = "[Space] Continue   [E] Close";
        }

        dialogueText.text = currentLine +
            "\n\n<size=75%>" + hint + "</size>";

        dialogueText.maxVisibleCharacters = int.MaxValue;
    }

    private void ShowChoices(string first, string second)
    {
        StopTyping();

        SetSpeaker(true);
        waitingForChoice = true;

        dialogueText.text =
            "[1] " + first +
            "\n\n[2] " + second +
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
            case Stage.Reminder:
                CloseDialogue();
                break;

            case Stage.Congratulations:
                stage = Stage.WeldingLesson;

                ShowLine(
                    false,
                    "A promise is a promise. Come closer, Fierren. " +
                    "I’ll teach you my secret welding technique—the " +
                    "trick to making a repair that holds. " +
                    "Watch carefully!"
                );
                break;

            case Stage.WeldingLesson:
                FinishQuestAndShowCard();
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

    private void BeginEnding()
    {
        if (!ValidateEndingSetup())
            return;

        endingActive = true;

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        DisableGameplayScripts();

        IsOpen = true;
        dialoguePanel.SetActive(true);

        stage = Stage.Congratulations;

        ShowLine(
            false,
            "All 10 pieces of Arcanium! Well done, Fierren. " +
            "You’ve given this old man a chance to get back " +
            "on his feet. I knew there was a capable artificer " +
            "under that raggedy getup!"
        );
    }

    private bool ValidateEndingSetup()
    {
        if (endingPanel == null ||
            weldingSkillCard == null ||
            newspaperPage1 == null ||
            newspaperPage2 == null)
        {
            Debug.LogError(
                "Assign Ending Panel and all three ending page GameObjects.",
                this
            );

            return false;
        }

        if (weldingSkillCard == newspaperPage1 ||
            weldingSkillCard == newspaperPage2 ||
            newspaperPage1 == newspaperPage2)
        {
            Debug.LogError(
                "Assign a different GameObject to each ending page.",
                this
            );

            return false;
        }

        if (!IsValidEndingChild(weldingSkillCard) ||
            !IsValidEndingChild(newspaperPage1) ||
            !IsValidEndingChild(newspaperPage2))
        {
            Debug.LogError(
                "Each ending page must be a direct child of EndingPanel.",
                this
            );

            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(titleSceneName))
        {
            Debug.LogError(
                "Add the title scene to Build Profiles " +
                "and check Title Scene Name.",
                this
            );

            return false;
        }

        return true;
    }

    private bool IsValidEndingChild(GameObject page)
    {
        return page.transform.parent == endingPanel.transform;
    }

    private void DisableGameplayScripts()
    {
        if (disableDuringEnding == null)
            return;

        previousEnabledStates = new bool[disableDuringEnding.Length];

        for (int i = 0; i < disableDuringEnding.Length; i++)
        {
            MonoBehaviour script = disableDuringEnding[i];

            if (script == null ||
                script == this ||
                script is QuestManager)
            {
                continue;
            }

            previousEnabledStates[i] = script.enabled;
        }

        // Disable after recording every original state.
        for (int i = 0; i < disableDuringEnding.Length; i++)
        {
            MonoBehaviour script = disableDuringEnding[i];

            if (script == null ||
                script == this ||
                script is QuestManager)
            {
                continue;
            }

            script.enabled = false;
        }
    }

    private void RestoreGameplayScripts()
    {
        if (previousEnabledStates == null ||
            disableDuringEnding == null)
        {
            return;
        }

        for (int i = 0;
             i < previousEnabledStates.Length &&
             i < disableDuringEnding.Length;
             i++)
        {
            MonoBehaviour script = disableDuringEnding[i];

            if (script != null &&
                script != this &&
                !(script is QuestManager))
            {
                script.enabled = previousEnabledStates[i];
            }
        }

        previousEnabledStates = null;
    }

    private void FinishQuestAndShowCard()
    {
        QuestManager manager = QuestManager.Instance;

        if (manager == null ||
            !manager.TryCompleteQuest(questID))
        {
            Debug.LogError(
                "Cannot turn in quest. Check QuestManager.",
                this
            );

            return;
        }

        HideDialogue();

        ShowEndingPage(
            Stage.SkillCard,
            weldingSkillCard
        );
    }

    private bool IsShowingEndingPage()
    {
        return stage == Stage.SkillCard ||
               stage == Stage.Newspaper1 ||
               stage == Stage.Newspaper2;
    }

    private void HideEndingPages()
    {
        if (weldingSkillCard != null)
            weldingSkillCard.SetActive(false);

        if (newspaperPage1 != null)
            newspaperPage1.SetActive(false);

        if (newspaperPage2 != null)
            newspaperPage2.SetActive(false);
    }

    private void ShowEndingPage(Stage nextStage, GameObject page)
    {
        HideEndingPages();

        stage = nextStage;

        endingPanel.SetActive(true);
        endingPanel.transform.SetAsLastSibling();

        if (endingHintText != null)
        {
            endingHintText.text = stage == Stage.Newspaper2
                ? "[Enter] Return to title screen"
                : "[Enter] Continue";
        }

        pageShownFrame = Time.frameCount;

        // Each page can have its own Animator, Audio Source, and effects.
        page.SetActive(true);
    }

    private void AdvanceEndingPage()
    {
        switch (stage)
        {
            case Stage.SkillCard:
                ShowEndingPage(
                    Stage.Newspaper1,
                    newspaperPage1
                );
                break;

            case Stage.Newspaper1:
                ShowEndingPage(
                    Stage.Newspaper2,
                    newspaperPage2
                );
                break;

            case Stage.Newspaper2:
                ReturnToTitleScreen();
                break;
        }
    }

    private void ReturnToTitleScreen()
    {
        if (!Application.CanStreamedLevelBeLoaded(titleSceneName))
        {
            Debug.LogError(
                "Cannot load title scene: " + titleSceneName,
                this
            );

            return;
        }

        loadingTitle = true;

        Time.timeScale = 1f;

        SceneManager.LoadScene(
            titleSceneName,
            LoadSceneMode.Single
        );
    }

    public void CloseDialogue()
    {
        // E cannot cancel or skip the final ending sequence.
        if (endingActive)
            return;

        HideDialogue();
    }

    private void HideDialogue()
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
        HideDialogue();
        HideEndingPages();

        if (endingPanel != null)
            endingPanel.SetActive(false);

        if (!endingActive)
            return;

        endingActive = false;

        Time.timeScale = loadingTitle
            ? 1f
            : previousTimeScale;

        RestoreGameplayScripts();
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