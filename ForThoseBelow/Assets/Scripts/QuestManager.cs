
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
        public UnityEvent onComplete;

        [NonSerialized] public HashSet<GameObject> collectedObjects = new();
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
            quest.isCompleted = false;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool CollectObject(string questID, GameObject obj)
    {
        Quest quest = quests.Find(q => q.questID == questID);

        if (quest == null)
        {
            Debug.LogWarning($"[QuestManager] Quest not found: {questID}");
            return false;
        }

        if (quest.isCompleted)
            return false;

        if (!quest.requiredObjects.Contains(obj))
        {
            Debug.LogWarning(
                $"[QuestManager] {obj.name} is not registered in {questID}"
            );
            return false;
        }

        if (!quest.collectedObjects.Add(obj))
            return false;

        Debug.Log(
            $"[QuestManager] {questID}: " +
            $"{quest.collectedObjects.Count}/{quest.requiredObjects.Count}"
        );

        CheckQuestCompletion(quest);
        return true;
    }

    private void CheckQuestCompletion(Quest quest)
    {
        if (quest.requiredObjects.Count == 0)
            return;

        foreach (GameObject obj in quest.requiredObjects)
        {
            if (obj == null || !quest.collectedObjects.Contains(obj))
                return;
        }

        CompleteQuest(quest);
    }

    private void CompleteQuest(Quest quest)
    {
        if (quest.isCompleted)
            return;

        quest.isCompleted = true;

        Debug.Log($"[QuestManager] Quest Completed: {quest.questID}");

        quest.onComplete?.Invoke();
    }

    public void TestCompleteQuest(int questIndex)
    {
        if (questIndex < 0 || questIndex >= quests.Count)
            return;

        CompleteQuest(quests[questIndex]);
    }

    public bool IsQuestCompleted(string questID)
    {
        Quest quest = quests.Find(q => q.questID == questID);
        return quest != null && quest.isCompleted;
    }
}
