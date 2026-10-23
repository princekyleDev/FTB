
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class DayNightTimer : MonoBehaviour
{
    public enum StartingTime
    {
        Day,
        Night
    }

    [Header("References")]
    [SerializeField] private DayNightController dayNightController;
    [SerializeField] private TMP_Text timeText;

    [Header("Clock Settings")]
    [Min(0.01f)]
    [SerializeField] private float secondsPerGameMinute = 1f;

    [SerializeField] private StartingTime startAt = StartingTime.Day;
    [SerializeField] private bool startAutomatically = true;

    [Header("Fast Forward Testing")]
    [SerializeField] private bool fastForwardEnabled = false;

    [Min(1f)]
    [SerializeField] private float fastForwardMinutesPerSecond = 10f;

    [Header("Transition Settings")]
    [SerializeField] private bool pauseDuringTransition = true;

    [Header("Time Events")]
    public UnityEvent on4PM;
    public UnityEvent on8PM;
    public UnityEvent on4AM;
    public UnityEvent on8AM;

    private float gameMinutes;
    private float minuteTimer;
    private float fastForwardTimer;

    private bool isDay;
    private bool isTransitionPaused;

    private void Start()
    {
        gameMinutes = startAt == StartingTime.Day ? 480f : 1200f;
        isDay = startAt == StartingTime.Day;

        ApplyCurrentTime();
        UpdateClockUI();
    }

    private void Update()
    {
        if (!startAutomatically || isTransitionPaused)
        {
            UpdateClockUI();
            return;
        }

        if (fastForwardEnabled)
        {
            fastForwardTimer += Time.deltaTime;

            while (fastForwardTimer >= 1f)
            {
                fastForwardTimer -= 1f;

                int minutesToAdvance =
                    Mathf.Max(1, Mathf.RoundToInt(fastForwardMinutesPerSecond));

                for (int i = 0; i < minutesToAdvance; i++)
                {
                    AdvanceOneMinute();

                    if (isTransitionPaused)
                    {
                        fastForwardTimer = 0f;
                        break;
                    }
                }

                if (isTransitionPaused)
                    break;
            }
        }
        else
        {
            minuteTimer += Time.deltaTime;

            while (minuteTimer >= secondsPerGameMinute)
            {
                minuteTimer -= secondsPerGameMinute;
                AdvanceOneMinute();

                if (isTransitionPaused)
                {
                    minuteTimer = 0f;
                    break;
                }
            }
        }

        UpdateClockUI();
    }

    private void AdvanceOneMinute()
    {
        gameMinutes += 1f;

        if (gameMinutes >= 1440f)
            gameMinutes = 0f;

        int totalMinutes = Mathf.FloorToInt(gameMinutes);
        int hour = totalMinutes / 60;
        int minute = totalMinutes % 60;

        if (minute == 0)
        {
            switch (hour)
            {
                case 16:
                    on4PM?.Invoke();
                    PauseForTransition();
                    break;

                case 20:
                    isDay = false;
                    ApplyCurrentTime();
                    on8PM?.Invoke();
                    break;

                case 4:
                    on4AM?.Invoke();
                    PauseForTransition();
                    break;

                case 8:
                    isDay = true;
                    ApplyCurrentTime();
                    on8AM?.Invoke();
                    break;
            }
        }
    }

    private void PauseForTransition()
    {
        if (!pauseDuringTransition)
            return;

        isTransitionPaused = true;
        minuteTimer = 0f;
        fastForwardTimer = 0f;
    }

    public void ResumeTime()
    {
        isTransitionPaused = false;
        minuteTimer = 0f;
        fastForwardTimer = 0f;
    }

    public void SetDay()
    {
        isDay = true;
        gameMinutes = 480f;
        isTransitionPaused = false;
        minuteTimer = 0f;
        fastForwardTimer = 0f;

        ApplyCurrentTime();
        UpdateClockUI();
    }

    public void SetNight()
    {
        isDay = false;
        gameMinutes = 1200f;
        isTransitionPaused = false;
        minuteTimer = 0f;
        fastForwardTimer = 0f;

        ApplyCurrentTime();
        UpdateClockUI();
    }

    public void ToggleFastForward()
    {
        fastForwardEnabled = !fastForwardEnabled;
        fastForwardTimer = 0f;
        minuteTimer = 0f;
    }

    public void EnableFastForward()
    {
        fastForwardEnabled = true;
        fastForwardTimer = 0f;
        minuteTimer = 0f;
    }

    public void DisableFastForward()
    {
        fastForwardEnabled = false;
        fastForwardTimer = 0f;
        minuteTimer = 0f;
    }

    private void ApplyCurrentTime()
    {
        if (dayNightController == null)
            return;

        if (isDay)
            dayNightController.SetDay();
        else
            dayNightController.SetNight();
    }

    private void UpdateClockUI()
    {
        if (timeText == null)
            return;

        int totalMinutes = Mathf.FloorToInt(gameMinutes) % 1440;
        int hour24 = totalMinutes / 60;
        int minute = totalMinutes % 60;

        string period = hour24 >= 12 ? "PM" : "AM";

        int hour12 = hour24 % 12;
        if (hour12 == 0)
            hour12 = 12;

        timeText.text = $"{hour12}:{minute:00} {period}";
    }
}
