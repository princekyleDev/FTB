using UnityEngine;
using UnityEngine.InputSystem;

public class FirstVisitPopup : MonoBehaviour
{
    [SerializeField] private GameObject popup;

    private static bool dismissedThisSession;
    private bool isShowing;
    private float previousTimeScale;
    private int shownFrame;

    // Reset when the game launches or when you enter Play Mode.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        dismissedThisSession = false;
    }

    private void Start()
    {
        if (popup == null)
            return;

        if (dismissedThisSession)
        {
            popup.SetActive(false);
            return;
        }

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        isShowing = true;
        shownFrame = Time.frameCount;
        popup.transform.SetAsLastSibling();
        popup.SetActive(true);
    }

    private void Update()
    {
        if (!isShowing || Time.frameCount <= shownFrame)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null &&
            (keyboard.enterKey.wasPressedThisFrame ||
             keyboard.numpadEnterKey.wasPressedThisFrame))
        {
            dismissedThisSession = true;
            ClosePopup();
        }
    }

    private void OnDisable()
    {
        if (isShowing)
            ClosePopup();
    }

    private void ClosePopup()
    {
        isShowing = false;
        Time.timeScale = previousTimeScale;

        if (popup != null)
            popup.SetActive(false);
    }
}
