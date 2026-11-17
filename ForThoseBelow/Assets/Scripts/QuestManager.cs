using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Serializable]
    public class Quest
    {
        public string questID;

        public List<GameObject> requiredObjects = new();

        public UnityEvent onAllItemsCollected;
        public UnityEvent onComplete;

        [NonSerialized]
        public HashSet<GameObject> collectedObjects = new();

        [NonSerialized] public bool isReadyForTurnIn;
        [NonSerialized] public bool isCompleted;
    }

    [SerializeField] private List<Quest> quests = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        foreach (Quest quest in quests)
        {
            quest.collectedObjects = new HashSet<GameObject>();
            quest.isReadyForTurnIn = false;
            quest.isCompleted = false;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private Quest FindQuest(string questID)
    {
        return quests.Find(q => q.questID == questID);
    }

    public bool CollectObject(string questID, GameObject obj)
    {
        Quest quest = FindQuest(questID);

        if (quest == null)
        {
            Debug.LogWarning(
                $"[QuestManager] Quest not found: {questID}",
                this
            );

            return false;
        }

        if (obj == null ||
            quest.isCompleted ||
            quest.isReadyForTurnIn)
        {
            return false;
        }

        if (!quest.requiredObjects.Contains(obj))
        {
            Debug.LogWarning(
                $"[QuestManager] {obj.name} is not registered in {questID}",
                this
            );

            return false;
        }

        if (!quest.collectedObjects.Add(obj))
            return false;

        Debug.Log(
            $"[QuestManager] {questID}: " +
            $"{quest.collectedObjects.Count}/{quest.requiredObjects.Count}",
            this
        );

        CheckQuestReadiness(quest);

        return true;
    }

    private void CheckQuestReadiness(Quest quest)
    {
        if (quest.isReadyForTurnIn ||
            quest.requiredObjects.Count == 0)
        {
            return;
        }

        foreach (GameObject obj in quest.requiredObjects)
        {
            if (obj == null ||
                !quest.collectedObjects.Contains(obj))
            {
                return;
            }
        }

        quest.isReadyForTurnIn = true;

        Debug.Log(
            $"[QuestManager] Return to the NPC: {quest.questID}",
            this
        );

        quest.onAllItemsCollected?.Invoke();
    }

    public bool IsQuestReadyForTurnIn(string questID)
    {
        Quest quest = FindQuest(questID);

        return quest != null &&
               quest.isReadyForTurnIn &&
               !quest.isCompleted;
    }

    public bool TryCompleteQuest(string questID)
    {
        Quest quest = FindQuest(questID);

        if (quest == null ||
            !quest.isReadyForTurnIn ||
            quest.isCompleted)
        {
            return false;
        }

        quest.isCompleted = true;

        Debug.Log(
            $"[QuestManager] Quest completed: {quest.questID}",
            this
        );

        quest.onComplete?.Invoke();

        return true;
    }

    public bool IsQuestCompleted(string questID)
    {
        Quest quest = FindQuest(questID);

        return quest != null && quest.isCompleted;
    }

    public int GetCollectedCount(string questID)
    {
        Quest quest = FindQuest(questID);

        return quest != null
            ? quest.collectedObjects.Count
            : 0;
    }

    // Debug shortcut only.
    // Marks the quest ready so you can test returning to the NPC.
    public void TestCompleteQuest(int questIndex)
    {
        if (questIndex < 0 || questIndex >= quests.Count)
            return;

        Quest quest = quests[questIndex];

        if (quest.isCompleted || quest.isReadyForTurnIn)
            return;

        quest.isReadyForTurnIn = true;

        quest.onAllItemsCollected?.Invoke();
    }
}