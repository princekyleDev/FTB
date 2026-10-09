
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TMPro;

public class SpacebarQTE : MonoBehaviour
{
    [Header("QTE Settings")]
    [SerializeField] private int requiredPresses = 20;
    [SerializeField] private float timeLimit = 5f;
    [SerializeField] private bool startOnAwake = true;

    [Header("UI")]
    [SerializeField] private TMP_Text countText;
    [SerializeField] private TMP_Text timerText;

    [Header("Events")]
    public UnityEvent onStart;
    public UnityEvent onSuccess;
    public UnityEvent onFail;

    private int currentPresses;
    private float timer;
    private bool isActive;

    public int CurrentPresses => currentPresses;
    public int RequiredPresses => requiredPresses;
    public float TimeRemaining => Mathf.Max(0f, timeLimit - timer);
    public bool IsActive => isActive;

    private void OnEnable()
    {
        if (startOnAwake)
            StartQTE();
        else
        {
            currentPresses = 0;
            timer = 0f;
            isActive = false;

            UpdateCountText();
            UpdateTimerText();
        }
    }

    private void Update()
    {
        if (!isActive)
            return;

        timer += Time.deltaTime;

        UpdateTimerText();

        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            currentPresses++;
            UpdateCountText();

            if (currentPresses >= requiredPresses)
            {
                CompleteQTE();
                return;
            }
        }

        if (timer >= timeLimit)
            FailQTE();
    }

    public void StartQTE()
    {
        currentPresses = 0;
        timer = 0f;
        isActive = true;

        UpdateCountText();
        UpdateTimerText();

        onStart?.Invoke();

        Debug.Log("QTE Started!");
    }

    private void UpdateCountText()
    {
        if (countText != null)
            countText.text = $"{currentPresses} / {requiredPresses}";
    }

    private void UpdateTimerText()
    {
        if (timerText != null)
        {
            float remaining = Mathf.Max(0f, timeLimit - timer);
            timerText.text = $"{remaining:F1}s";
        }
    }

    private void CompleteQTE()
    {
        isActive = false;

        Debug.Log("QTE Success!");
        onSuccess?.Invoke();
    }

    private void FailQTE()
    {
        isActive = false;
        timer = timeLimit;

        UpdateTimerText();

        Debug.Log("QTE Failed!");
        onFail?.Invoke();
    }

    private void OnDisable()
    {
        // Stop the QTE while this object is disabled.
        isActive = false;
    }
}