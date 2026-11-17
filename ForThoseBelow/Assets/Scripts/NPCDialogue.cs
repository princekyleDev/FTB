using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class NPCDialogue : MonoBehaviour
{
    [Serializable]
    public class Reply
    {
        public string replyText;

        [Tooltip("Dialogue node to show. -1 closes the dialogue.")]
        public int nextNode = -1;
    }

    [Serializable]
    public class DialogueNode
    {
        public string speakerName;

        [TextArea(3, 8)]
        public string text;

        [Tooltip("Used when this node has no replies. -1 ends dialogue.")]
        public int nextNode = -1;

        public Reply[] replies = new Reply[0];
    }

    [Header("Interaction")]
    [SerializeField] private Transform player;
    [SerializeField, Min(0.1f)] private float interactionDistance = 2f;

    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private DialogueTypewriter typewriter;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private Button[] replyButtons;

    [Header("Conversation")]
    [SerializeField] private DialogueNode[] nodes;

    private static NPCDialogue activeNPC;
    private static int lastInteractionFrame = -1;

    private int currentNode;
    private bool controlsShown;

    private void Update()
    {
        // Only the NPC with the open conversation handles dialogue input.
        if (activeNPC == this)
        {
            if (player == null || !IsPlayerNear())
            {
                CloseDialogue();
                return;
            }

            Keyboard keyboard = Keyboard.current;

            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                CloseDialogue();
                return;
            }

            if (!typewriter.IsTyping && !controlsShown)
                ShowReplies();

            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                if (typewriter.IsTyping)
                {
                    typewriter.Skip();
                }
                else if (!HasReplies())
                {
                    ShowNode(nodes[currentNode].nextNode);
                }
            }

            return;
        }

        if (activeNPC != null ||
            lastInteractionFrame == Time.frameCount ||
            player == null)
        {
            return;
        }

        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame &&
            IsPlayerNear())
        {
            OpenDialogue();
        }
    }

    private bool IsPlayerNear()
    {
        // XY distance for a 2D game.
        return Vector2.Distance(player.position, transform.position)
            <= interactionDistance;
    }

    private void OpenDialogue()
    {
        if (dialoguePanel == null || typewriter == null ||
            nodes == null || nodes.Length == 0)
        {
            Debug.LogWarning("Assign the dialogue UI and nodes.", this);
            return;
        }

        activeNPC = this;
        lastInteractionFrame = Time.frameCount;

        dialoguePanel.SetActive(true);
        typewriter.gameObject.SetActive(true);

        ShowNode(0);
    }

    private void ShowNode(int index)
    {
        if (index == -1)
        {
            CloseDialogue();
            return;
        }

        if (index < 0 || index >= nodes.Length)
        {
            Debug.LogWarning("Invalid dialogue node: " + index, this);
            CloseDialogue();
            return;
        }

        currentNode = index;
        controlsShown = false;

        HideReplies();

        DialogueNode node = nodes[index];

        if (speakerText != null)
            speakerText.text = node.speakerName;

        typewriter.Play(node.text ?? "");
    }

    private bool HasReplies()
    {
        Reply[] replies = nodes[currentNode].replies;
        return replies != null && replies.Length > 0;
    }

    private void ShowReplies()
    {
        controlsShown = true;

        if (!HasReplies())
            return;

        Reply[] replies = nodes[currentNode].replies;

        if (replyButtons == null || replyButtons.Length < replies.Length)
        {
            Debug.LogError(
                "Assign enough reply buttons for this node's replies.", this);
            CloseDialogue();
            return;
        }

        for (int i = 0; i < replies.Length; i++)
        {
            Button button = replyButtons[i];

            if (button == null)
            {
                Debug.LogError("A reply button is not assigned.", this);
                CloseDialogue();
                return;
            }

            int replyIndex = i;

            button.onClick.RemoveAllListeners();

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);

            if (label != null)
                label.text = replies[i].replyText;

            button.gameObject.SetActive(true);
            button.onClick.AddListener(() => ChooseReply(replyIndex));
        }
    }

    private void ChooseReply(int index)
    {
        if (activeNPC != this || typewriter.IsTyping)
            return;

        Reply[] replies = nodes[currentNode].replies;

        if (replies == null || index < 0 || index >= replies.Length)
            return;

        ShowNode(replies[index].nextNode);
    }

    private void HideReplies()
    {
        if (replyButtons == null)
            return;

        foreach (Button button in replyButtons)
        {
            if (button == null)
                continue;

            button.onClick.RemoveAllListeners();
            button.gameObject.SetActive(false);
        }
    }

    public void CloseDialogue()
    {
        if (activeNPC != this)
            return;

        activeNPC = null;
        lastInteractionFrame = Time.frameCount;

        HideReplies();

        if (typewriter != null)
            typewriter.Close();

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    private void OnDisable()
    {
        if (activeNPC == this)
            CloseDialogue();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}