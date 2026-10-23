
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections;

public class Ttilescreen : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float delay = 1f;
    [SerializeField] private string sceneToLoad = "Main";

    [Header("Events")]
    public UnityEvent onPressAnyKey;
    public UnityEvent onAfterDelay;

    private bool isTriggered;

    private void Update()
    {
        if (isTriggered || Keyboard.current == null)
            return;

        if (Keyboard.current.anyKey.wasPressedThisFrame)
        {
            isTriggered = true;
            onPressAnyKey?.Invoke();
            StartCoroutine(InvokeAfterDelay());
        }
    }

    private IEnumerator InvokeAfterDelay()
    {
        yield return new WaitForSeconds(delay);
        onAfterDelay?.Invoke();
    }

    public void LoadScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoad);
    }


}
