
using UnityEngine;
using UnityEngine.Events;

public class TagDetector : MonoBehaviour
{
    [SerializeField] private string targetTag = "Player";

    [Header("Events")]
    [SerializeField] private UnityEvent onDetected;
    [SerializeField] private UnityEvent onExited;

    public bool IsPlayerInside { get; private set; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(targetTag))
            return;

        IsPlayerInside = true;
        Debug.Log("Player Enter!");
        Debug.Log($"Trigger Enter: {other.name}, Tag: {other.tag}");
        onDetected?.Invoke();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(targetTag))
            return;

        IsPlayerInside = false;
        Debug.Log("Player Exited!");
        Debug.Log($"Trigger Exit: {other.name}, Tag: {other.tag}");
        onExited?.Invoke();
    }
}