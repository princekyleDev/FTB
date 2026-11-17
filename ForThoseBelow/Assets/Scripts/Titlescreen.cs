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

    [Header("Events - Animation or Sound Only")]
    public UnityEvent onPressAnyKey;
    public UnityEvent onAfterDelay;

    private bool isTriggered;
    private bool isLoading;

    private void Update()
    {
        if (isTriggered || isLoading || Keyboard.current == null)
            return;

        if (Keyboard.current.anyKey.wasPressedThisFrame)
            BeginTransition();
    }

    // Can also be connected to a Start button.
    public void BeginTransition()
    {
        if (isTriggered || isLoading)
            return;

        isTriggered = true;

        onPressAnyKey?.Invoke();

        StartCoroutine(TransitionAfterDelay());
    }

    private IEnumerator TransitionAfterDelay()
    {
        // Works even if Time.timeScale is zero.
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

        SceneManager.LoadScene(
            sceneToLoad,
            LoadSceneMode.Single
        );
    }
}