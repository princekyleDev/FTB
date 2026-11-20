using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Ttilescreen : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField, Min(0f)] private float delay = 1f;
    [SerializeField] private string sceneToLoad = "Main";

    [Header("Spam QTE")]
    [SerializeField] private bool useSpamQTE;

    [Header("Events - Animation or Sound Only")]
    public UnityEvent onPressAnyKey;
    public UnityEvent onAfterDelay;

    private bool isTriggered;
    private bool isLoading;
    private bool qteActive;
    private bool qteTimedOut;
    private double qteDeadline;
    private int requiredPresses;
    private int currentPresses;

    private void Update()
    {
        if (isTriggered || isLoading)
            return;

        Keyboard keyboard = Keyboard.current;

        if (qteActive)
        {
            if (Time.unscaledTimeAsDouble >= qteDeadline)
            {
                qteActive = false;
                qteTimedOut = true;
                return;
            }

            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                currentPresses++;

                if (currentPresses >= requiredPresses)
                {
                    qteActive = false;
                    PlayTransition();
                }
            }

            return;
        }

        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            BeginTransition();
    }

    // Can also be connected to a Start button.
    public void BeginTransition()
    {
        if (isTriggered || isLoading || qteActive)
            return;

        if (useSpamQTE)
        {
            StartQTE();
            return;
        }

        PlayTransition();
    }

    private void StartQTE()
    {
        // Give players 4–6 seconds and budget half a second to react.
        float duration = Random.Range(4, 7);
        int pressCap = Mathf.FloorToInt((duration - 0.5f) * 2.5f);

        requiredPresses = Random.Range(6, pressCap + 1);
        currentPresses = 0;
        qteDeadline = Time.unscaledTimeAsDouble + duration;
        qteTimedOut = false;
        qteActive = true;
    }

    private void OnGUI()
    {
        if (isTriggered || isLoading || (!qteActive && !qteTimedOut))
            return;

        string message = qteActive
            ? $"Tap SPACE  {currentPresses}/{requiredPresses}\n" +
              $"{System.Math.Max(0, qteDeadline - Time.unscaledTimeAsDouble):0.0}s left"
            : "Time's up!\nPress any key to try again.";

        float width = Mathf.Min(300f, Screen.width - 24f);
        Rect box = new Rect((Screen.width - width) / 2f, 20f, width, 64f);
        GUI.Box(box, message);
    }

    private void PlayTransition()
    {
        isTriggered = true;
        onPressAnyKey?.Invoke();
        StartCoroutine(TransitionAfterDelay());
    }

    private IEnumerator TransitionAfterDelay()
    {
        yield return new WaitForSecondsRealtime(delay);

        onAfterDelay?.Invoke();
        LoadScene();
    }

    public void LoadScene()
    {
        if (isLoading)
            return;

        if (string.IsNullOrWhiteSpace(sceneToLoad) ||
            !Application.CanStreamedLevelBeLoaded(sceneToLoad))
        {
            Debug.LogError(
                "Cannot load scene '" + sceneToLoad +
                "'. Check its name and the Build Profiles scene list.",
                this
            );

            isTriggered = false;
            return;
        }

        isLoading = true;
        isTriggered = true;
        Time.timeScale = 1f;

        SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Single);
    }
}