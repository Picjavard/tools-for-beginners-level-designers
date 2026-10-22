using UnityEditor;
using UnityEngine;

namespace Arlycad
{
    [CustomEditor(typeof(ObjectSpawner))]
    public class ObjectSpawnerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.HelpBox(
                "Перетащите префаб из Project. Числа (сколько, как часто, когда удалять) задаются здесь. " +
                "Зона или кнопка только вызывает Spawn или StartRepeating.",
                MessageType.Info);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("prefab"));
            GameObject prefab = serializedObject.FindProperty("prefab").objectReferenceValue as GameObject;
            if (prefab != null && !EditorUtility.IsPersistent(prefab))
            {
                EditorGUILayout.HelpBox(
                    "Это объект со сцены. Перетащите его в Project, чтобы получился префаб, и назначьте уже префаб.",
                    MessageType.Warning);
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("spawnPoint"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("randomOffset"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useSpawnPointRotation"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("randomYaw"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("attachToSpawnPoint"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("spawnOnStart"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("repeat"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("interval"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("limitAlive"));
            if (serializedObject.FindProperty("limitAlive").boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxAlive"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("limitTotal"));
            if (serializedObject.FindProperty("limitTotal").boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("totalCount"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("destroyAfterDelay"));
            if (serializedObject.FindProperty("destroyAfterDelay").boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("lifetime"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onSpawned"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onReachedLimit"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("debugLog"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
