
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(QuestManager))]
public class QuestManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (!Application.isPlaying)
            return;

        QuestManager manager = (QuestManager)target;
        SerializedProperty quests =
            serializedObject.FindProperty("quests");

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Quest Debug", EditorStyles.boldLabel);

        for (int i = 0; i < quests.arraySize; i++)
        {
            SerializedProperty quest =
                quests.GetArrayElementAtIndex(i);

            string questID =
                quest.FindPropertyRelative("questID").stringValue;

            if (GUILayout.Button($"Test Complete: {questID}"))
                manager.TestCompleteQuest(i);
        }
    }
}
#endif
