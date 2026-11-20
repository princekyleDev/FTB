using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UIShake : MonoBehaviour
{
    private enum Playback
    {
        Manual,
        OnAwake,
        OnEnable
    }

    [SerializeField] private Playback playback = Playback.OnAwake;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField, Min(0f)] private float delay = 0f;
    [SerializeField, Min(0f)] private float duration = 0.35f;
    [SerializeField, Min(0f)] private float frequency = 16f;
    [SerializeField] private Vector2 strength = new Vector2(8f, 5f);

    private RectTransform rectTransform;
    private Vector2 appliedOffset;
    private float elapsed;
    private bool isShaking;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
        if (playback == Playback.OnAwake)
            PlayShake();
    }

    private void OnEnable()
    {
        if (playback == Playback.OnEnable)
            PlayShake();
    }

    public void PlayShake()
    {
        if (!isActiveAndEnabled || duration <= 0f)
            return;

        StopShake();
        elapsed = -delay;
        isShaking = true;
    }

    public void StopShake()
    {
        if (!isShaking)
            return;

        SetOffset(Vector2.zero);
        isShaking = false;
    }

    private void Update()
    {
        if (!isShaking)
            return;

        elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (elapsed < 0f)
            return;

        if (elapsed >= duration)
        {
            StopShake();
            return;
        }

        SetOffset(EvaluateOffset(elapsed));
    }

    public Vector2 EvaluateOffset(float shakeTime)
    {
        if (shakeTime <= 0f || shakeTime >= duration)
            return Vector2.zero;

        float phase = shakeTime * frequency * (2f * Mathf.PI);
        float fade = 1f - shakeTime / duration;
        Vector2 wave = new Vector2(Mathf.Sin(phase), Mathf.Sin(phase * 1.37f));
        return Vector2.Scale(wave, strength) * fade;
    }

    private void OnDisable()
    {
        StopShake();
    }

    private void SetOffset(Vector2 offset)
    {
        rectTransform.anchoredPosition += offset - appliedOffset;
        appliedOffset = offset;
    }
}
