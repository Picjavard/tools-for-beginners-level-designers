using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Arlycad
{
    public static class MarkerListGUI
    {
        static string[] cached = new string[0];
        static bool ready;

        [DidReloadScripts]
        static void OnScriptsReloaded()
        {
            ready = false;
        }

        public static void Draw(SerializedProperty list)
        {
            if (list == null)
                return;

            string[] options = GetNames();
            int deleteIndex = -1;

            EditorGUILayout.LabelField("Метки-скрипты", EditorStyles.boldLabel);
            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginHorizontal();
                DrawPopup(element, options);
                if (GUILayout.Button("Убрать", GUILayout.Width(70)))
                    deleteIndex = i;
                EditorGUILayout.EndHorizontal();
            }

            if (deleteIndex >= 0)
                list.DeleteArrayElementAtIndex(deleteIndex);

            EditorGUI.BeginDisabledGroup(options.Length == 0);
            if (GUILayout.Button("Добавить метку"))
            {
                int index = list.arraySize;
                list.InsertArrayElementAtIndex(index);
                list.GetArrayElementAtIndex(index).stringValue = options.Length > 0 ? options[0] : string.Empty;
            }

            EditorGUI.EndDisabledGroup();

            if (options.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "В Assets/_Arlycad нет компонентов с именем Tag_. Папка Tags должна лежать внутри проекта.",
                    MessageType.Warning);
            }
        }

        static void DrawPopup(SerializedProperty element, string[] options)
        {
            string current = element.stringValue;
            int index = Array.IndexOf(options, current);
            if (index < 0)
            {
                string[] withMissing = new string[options.Length + 1];
                withMissing[0] = string.IsNullOrEmpty(current) ? "(пусто)" : current + " (скрипт не найден)";
                Array.Copy(options, 0, withMissing, 1, options.Length);
                int picked = EditorGUILayout.Popup("Метка", 0, withMissing);
                if (picked > 0)
                    element.stringValue = options[picked - 1];
                return;
            }

            int next = EditorGUILayout.Popup("Метка", index, options);
            if (next != index && next >= 0 && next < options.Length)
                element.stringValue = options[next];
        }

        static string[] GetNames()
        {
            if (ready)
                return cached;

            ready = true;
            List<string> found = new List<string>();
            string[] guids = AssetDatabase.FindAssets("t:MonoScript");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]).Replace('\\', '/');
                if (path.IndexOf("/_Arlycad/", StringComparison.Ordinal) < 0)
                    continue;

                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script == null)
                    continue;

                Type type = script.GetClass();
                if (type == null || type.IsAbstract)
                    continue;

                if (!typeof(MonoBehaviour).IsAssignableFrom(type))
                    continue;

                if (!type.Name.StartsWith("Tag_", StringComparison.Ordinal))
                    continue;

                if (!found.Contains(type.Name))
                    found.Add(type.Name);
            }

            found.Sort(StringComparer.Ordinal);
            cached = found.ToArray();
            return cached;
        }
    }
}
