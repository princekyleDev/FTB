
using UnityEngine;
using UnityEngine.Events;

public class QuestCollectible : MonoBehaviour
{
    [Header("Quest Settings")]
    [SerializeField] private string questID = "Quest1";
    [SerializeField] private GameObject collectibleObject;
    [SerializeField] private bool disableOnCollect = false;

    [Header("Events")]
    [SerializeField] private UnityEvent onCollected;

    private bool isCollected;

    public void Collect()
    {
        if (isCollected)
            return;

        QuestManager manager = QuestManager.Instance;

        if (manager == null)
        {
            Debug.LogWarning("[QuestCollectible] QuestManager not found.", this);
            return;
        }

        GameObject target = collectibleObject != null
            ? collectibleObject
            : gameObject;

        if (!manager.CollectObject(questID, target))
            return;

        isCollected = true;

        Debug.Log($"[QuestCollectible] Collected: {target.name}", this);

        onCollected?.Invoke();

        if (disableOnCollect)
            target.SetActive(false);
    }

    [ContextMenu("Test Collect")]
    private void TestCollect()
    {
        if (Application.isPlaying)
            Collect();
    }
}
