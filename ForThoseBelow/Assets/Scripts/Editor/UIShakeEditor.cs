using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UIShake))]
public class UIShakeEditor : Editor
{
    private AnimationModeDriver previewDriver;
    private AnimationClip previewClip;
    private double previewStart;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var shake = (UIShake)target;
        bool playing = EditorApplication.isPlaying;
        bool prefabAsset = EditorUtility.IsPersistent(shake);
        float duration = serializedObject.FindProperty("duration").floatValue;
        bool otherPreview = previewDriver == null && AnimationMode.InAnimationMode();
        bool canTest = !prefabAsset && shake.isActiveAndEnabled && duration > 0f &&
            (playing || !otherPreview);

        using (new EditorGUI.DisabledScope(!canTest))
        {
            if (GUILayout.Button("Test Shake"))
            {
                if (playing)
                    shake.PlayShake();
                else
                    StartPreview(shake, duration);
            }
        }

        if (!playing && prefabAsset)
            EditorGUILayout.HelpBox("Open the prefab in Prefab Mode to test the shake.", MessageType.Info);
    }

    private void StartPreview(UIShake shake, float duration)
    {
        StopPreview();

        float delay = serializedObject.FindProperty("delay").floatValue;
        float frequency = serializedObject.FindProperty("frequency").floatValue;
        int samples = Mathf.Clamp(Mathf.CeilToInt(duration * Mathf.Max(60f, frequency * 8f)), 2, 4096);
        Vector2 restPosition = ((RectTransform)shake.transform).anchoredPosition;
        int firstShakeKey = delay > 0f ? 1 : 0;
        var xKeys = new Keyframe[samples + firstShakeKey + 1];
        var yKeys = new Keyframe[xKeys.Length];

        xKeys[0] = new Keyframe(0f, restPosition.x);
        yKeys[0] = new Keyframe(0f, restPosition.y);
        for (int i = 0; i <= samples; i++)
        {
            float time = duration * (i / (float)samples);
            Vector2 position = restPosition + shake.EvaluateOffset(time);
            xKeys[i + firstShakeKey] = new Keyframe(delay + time, position.x);
            yKeys[i + firstShakeKey] = new Keyframe(delay + time, position.y);
        }

        previewClip = new AnimationClip { hideFlags = HideFlags.HideAndDontSave };
        SetCurve("m_AnchoredPosition.x", xKeys);
        SetCurve("m_AnchoredPosition.y", yKeys);

        previewDriver = CreateInstance<AnimationModeDriver>();
        previewDriver.hideFlags = HideFlags.HideAndDontSave;
        AnimationMode.StartAnimationMode(previewDriver);
        previewStart = EditorApplication.timeSinceStartup;
        EditorApplication.update += UpdatePreview;
    }

    private void SetCurve(string property, Keyframe[] keys)
    {
        var binding = EditorCurveBinding.FloatCurve(string.Empty, typeof(RectTransform), property);
        AnimationUtility.SetEditorCurve(previewClip, binding, new AnimationCurve(keys));
    }

    private void UpdatePreview()
    {
        var shake = (UIShake)target;
        float time = (float)(EditorApplication.timeSinceStartup - previewStart);
        if (shake == null || !shake.isActiveAndEnabled || EditorApplication.isPlayingOrWillChangePlaymode ||
            !AnimationMode.InAnimationMode(previewDriver) || time >= previewClip.length)
        {
            StopPreview();
            return;
        }

        AnimationMode.BeginSampling();
        try
        {
            AnimationMode.SampleAnimationClip(shake.gameObject, previewClip, time);
        }
        finally
        {
            AnimationMode.EndSampling();
        }

        SceneView.RepaintAll();
        EditorApplication.QueuePlayerLoopUpdate();
    }

    private void OnDisable()
    {
        StopPreview();
    }

    private void StopPreview()
    {
        EditorApplication.update -= UpdatePreview;
        if (previewDriver != null)
        {
            AnimationMode.StopAnimationMode(previewDriver);
            DestroyImmediate(previewDriver);
            previewDriver = null;
        }

        if (previewClip != null)
        {
            DestroyImmediate(previewClip);
            previewClip = null;
            SceneView.RepaintAll();
            EditorApplication.QueuePlayerLoopUpdate();
        }
    }
}
