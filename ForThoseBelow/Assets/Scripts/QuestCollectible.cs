
using UnityEngine;
using UnityEngine.Events;

public class QuestCollectible : MonoBehaviour
{
    [Header("Quest Settings")]
    [SerializeField] private string questID = "Quest1";
    [SerializeField] private GameObject collectibleObject;
    [SerializeField] private bool disableOnCollect = false;

    [Header("Pickup VFX")]
    [SerializeField] private GameObject pickupVfxPrefab;
    [SerializeField, Min(0f)] private float pickupVfxLifetime = 1f;

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
        Vector3 pickupPosition = target.transform.position;

        if (!manager.CollectObject(questID, target))
            return;

        isCollected = true;

        if (pickupVfxPrefab != null)
        {
            GameObject effect = Instantiate(pickupVfxPrefab, pickupPosition, pickupVfxPrefab.transform.rotation);
            Destroy(effect, pickupVfxLifetime);
        }

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
