using UnityEditor;
using UnityEngine;

namespace Arlycad
{
    [CustomEditor(typeof(Mover))]
    public class MoverEditor : Editor
    {
        const string Help =
            "Поставьте объект в позу «закрыто». Сдвиг и поворот задают позу «открыто»: " +
            "дверь вверх — сдвиг Y, дверь на петлях — поворот Y. " +
            "Либо перетащите пустой объект в «Точку назначения». " +
            "Зона вызывает ToEnd, ToStart или Toggle. Длительность 0 ставит объект в позу сразу.";

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.HelpBox(Help, MessageType.Info);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("duration"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("offset"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("angle"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("destination"));

            Mover mover = (Mover)target;
            if (mover.destination != null && mover.destination.IsChildOf(mover.transform))
            {
                EditorGUILayout.HelpBox(
                    "Точка назначения стоит внутри этого объекта и уедет вместе с ним. Поставьте её рядом, отдельным объектом.",
                    MessageType.Warning);
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("smooth"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("playOnStart"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pingPong"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("whenStarted"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("whenFinished"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
