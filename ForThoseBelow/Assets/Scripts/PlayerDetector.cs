
using UnityEngine;
using UnityEngine.Events;

public class TagDetector : MonoBehaviour
{
    [SerializeField] private string targetTag = "Player";

    [Header("Events")]
    [SerializeField] private UnityEvent onDetected;
    [SerializeField] private UnityEvent onExited;

    [Header("Timer")]
    [SerializeField] private bool enableTimer = false;
    [SerializeField] private float timerDuration = 4f;
    [SerializeField] private bool triggerOnlyOnce = true;
    [SerializeField] private UnityEvent onTimerComplete;

    public bool IsPlayerInside { get; private set; }
    public bool HasTimerCompleted { get; private set; }

    private float timer;
    private bool timerCompleted;

    private void Update()
    {
        if (!enableTimer || !IsPlayerInside || timerCompleted)
            return;

        timer += Time.deltaTime;

        if (timer >= timerDuration)
        {
            timerCompleted = true;
            HasTimerCompleted = true;

            Debug.Log("Player timer completed!");

            onTimerComplete?.Invoke();

            if (!triggerOnlyOnce)
            {
                timer = 0f;
                timerCompleted = false;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(targetTag))
            return;

        IsPlayerInside = true;

        Debug.Log("Player Enter!");
        Debug.Log($"Trigger Enter: {other.name}, Tag: {other.tag}");

        onDetected?.Invoke();

        if (!timerCompleted || !triggerOnlyOnce)
        {
            timer = 0f;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(targetTag))
            return;

        IsPlayerInside = false;

        Debug.Log("Player Exited!");
        Debug.Log($"Trigger Exit: {other.name}, Tag: {other.tag}");

        onExited?.Invoke();

        // Reset timer when player exits.
        timer = 0f;

        if (!triggerOnlyOnce)
        {
            timerCompleted = false;
            HasTimerCompleted = false;
        }
    }
}