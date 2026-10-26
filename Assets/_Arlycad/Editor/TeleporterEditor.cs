using UnityEditor;
using UnityEngine;

namespace Arlycad
{
    [CustomEditor(typeof(Teleporter))]
    public class TeleporterEditor : Editor
    {
        const string Help =
            "Поставьте пустой объект туда, куда нужно попасть, и перетащите его в «Точка назначения». " +
            "Синяя стрелка этой точки — направление взгляда после переноса. " +
            "Точка должна стоять снаружи коллайдера плиты. " +
            "Галочка «Телепортировать вошедшего» переносит того, кто вошёл. Метод Teleport() из события переносит объект из поля «Объект».";

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            Teleporter teleporter = (Teleporter)target;

            DrawScript();
            EditorGUILayout.HelpBox(Help, MessageType.Info);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("destination"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("applyDestinationRotation"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("extraOffset"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("teleportOnEnter"));
            if (serializedObject.FindProperty("teleportOnEnter").boolValue)
            {
                if (DrawTriggerTools(teleporter))
                    return;
                WarnIfDestinationInside(teleporter);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("reactToAny"));
                if (!serializedObject.FindProperty("reactToAny").boolValue)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("roles"), true);
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("cooldown"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onlyOnce"));
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("assignedObject"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onTeleported"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("debugLog"));

            serializedObject.ApplyModifiedProperties();
        }

        void DrawScript()
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            EditorGUI.EndDisabledGroup();
        }

        bool DrawTriggerTools(Teleporter teleporter)
        {
            GameObject owner = teleporter.gameObject;
            Collider[] colliders = owner.GetComponents<Collider>();
            if (colliders.Length == 0)
            {
                EditorGUILayout.HelpBox("Для входа в плиту нужен Collider с Is Trigger.", MessageType.Warning);
                if (GUILayout.Button("Добавить Box Collider и включить Is Trigger"))
                    TriggerVolumeUtility.AddBoxTrigger(owner);
                return false;
            }

            if (!TriggerVolumeUtility.AnyColliderIsTrigger(owner))
            {
                EditorGUILayout.HelpBox("Is Trigger выключен.", MessageType.Warning);
                if (GUILayout.Button("Включить Is Trigger"))
                    TriggerVolumeUtility.EnableTriggers(owner);
            }

            if (TriggerVolumeUtility.HasMeshCollider(owner))
            {
                EditorGUILayout.HelpBox(
                    "Плита на Mesh Collider ProBuilder ненадёжна. Вынесите телепорт на отдельный куб.",
                    MessageType.Warning);
                if (GUILayout.Button("Вынести на дочерний куб-триггер"))
                {
                    TriggerVolumeUtility.MoveToChildVolume(teleporter);
                    return true;
                }
            }

            return false;
        }

        void WarnIfDestinationInside(Teleporter teleporter)
        {
            if (teleporter.destination == null)
                return;

            Vector3 point = teleporter.destination.position;
            Collider[] colliders = teleporter.GetComponents<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null && colliders[i].enabled && colliders[i].bounds.Contains(point))
                {
                    EditorGUILayout.HelpBox(
                        "Точка назначения внутри плиты. Объект будет прыгать туда-сюда. Отодвиньте точку.",
                        MessageType.Error);
                    return;
                }
            }
        }
    }
}
