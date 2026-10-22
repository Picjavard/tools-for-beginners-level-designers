using UnityEditor;
using UnityEngine;

namespace Arlycad
{
    [CustomEditor(typeof(ZoneTrigger))]
    public class ZoneTriggerEditor : Editor
    {
        const string PhysicsHelp =
            "На зоне нужен Collider с включённым Is Trigger. " +
            "У того, кто входит, нужен Rigidbody. Персонажу на Character Controller поставьте Rigidbody и включите Is Kinematic. " +
            "Метка или роль может висеть на вошедшем объекте или на его родителе.";

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            ZoneTrigger zone = (ZoneTrigger)target;

            DrawScript();
            EditorGUILayout.HelpBox(PhysicsHelp, MessageType.Info);
            if (DrawColliderTools(zone))
                return;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("reactToAny"));
            bool any = serializedObject.FindProperty("reactToAny").boolValue;
            if (!any)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("roles"), true);
                MarkerListGUI.Draw(serializedObject.FindProperty("targetScriptNames"));
                if (!ActorQuery.HasRules(false, zone.targetScriptNames, zone.roles))
                {
                    EditorGUILayout.HelpBox(
                        "Сейчас зона никого не примет. Включите «Реагировать на всех», добавьте роль или метку.",
                        MessageType.Warning);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onlyOnce"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cooldown"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("repeatWhileInside"));
            if (serializedObject.FindProperty("repeatWhileInside").boolValue)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("repeatInterval"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("debugLog"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("whenEntered"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("whenLeft"));
            if (serializedObject.FindProperty("repeatWhileInside").boolValue)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("whileInside"));

            DrawObjectEvents();

            if (Application.isPlaying)
                EditorGUILayout.HelpBox("Сейчас внутри: " + zone.InsideCount, MessageType.None);

            serializedObject.ApplyModifiedProperties();
        }

        void DrawScript()
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            EditorGUI.EndDisabledGroup();
        }

        bool DrawColliderTools(ZoneTrigger zone)
        {
            GameObject owner = zone.gameObject;
            Collider[] colliders = owner.GetComponents<Collider>();
            if (colliders.Length == 0)
            {
                EditorGUILayout.HelpBox("На объекте нет Collider.", MessageType.Error);
                if (GUILayout.Button("Добавить Box Collider и включить Is Trigger"))
                    TriggerVolumeUtility.AddBoxTrigger(owner);
            }
            else if (!TriggerVolumeUtility.AnyColliderIsTrigger(owner))
            {
                EditorGUILayout.HelpBox("Is Trigger выключен. Зона не сработает.", MessageType.Warning);
                if (GUILayout.Button("Включить Is Trigger"))
                    TriggerVolumeUtility.EnableTriggers(owner);
            }

            if (TriggerVolumeUtility.HasConcaveTriggerMesh(owner))
            {
                EditorGUILayout.HelpBox(
                    "Вогнутый Mesh Collider не работает как триггер. У ProBuilder так устроен почти любой меш. Вынесите зону на отдельный куб.",
                    MessageType.Error);
            }
            else if (TriggerVolumeUtility.HasMeshCollider(owner))
            {
                EditorGUILayout.HelpBox(
                    "Триггер висит на Mesh Collider. Для уровня из ProBuilder лучше отдельный куб: его размер — это размер двери, а не всего меша.",
                    MessageType.Warning);
            }

            if (TriggerVolumeUtility.HasMeshCollider(owner) && GUILayout.Button("Вынести на дочерний куб-триггер"))
            {
                TriggerVolumeUtility.MoveToChildVolume(zone);
                return true;
            }

            return false;
        }

        void DrawObjectEvents()
        {
            bool hasCalls = HasPersistentCalls("OnEnter") || HasPersistentCalls("OnExit");
            string key = "Arlycad.ZoneTrigger.ObjectEvents";
            bool open = hasCalls || SessionState.GetBool(key, false);
            open = EditorGUILayout.Foldout(open, "События с объектом", true);
            if (!hasCalls)
                SessionState.SetBool(key, open);

            if (!open)
                return;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OnEnter"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OnExit"));
            EditorGUI.indentLevel--;
        }

        bool HasPersistentCalls(string propertyName)
        {
            SerializedProperty calls = serializedObject
                .FindProperty(propertyName)
                .FindPropertyRelative("m_PersistentCalls.m_Calls");
            return calls != null && calls.arraySize > 0;
        }
    }
}
