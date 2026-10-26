using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Arlycad
{
    public static class PlayerPrefabBuilder
    {
        const string Folder = "Assets/_Arlycad/Player";
        const string PrefabPath = Folder + "/Player.prefab";
        const string MaterialPath = Folder + "/PlayerBody.mat";
        const string ActionsPath = Folder + "/PlayerControls.inputactions";
        const string RolePath = "Assets/_Arlycad/Roles/Player.asset";

        [InitializeOnLoadMethod]
        static void Schedule()
        {
            EditorApplication.delayCall += CreateIfMissing;
        }

        [MenuItem("Arlycad/Пересобрать префаб игрока")]
        public static void Rebuild()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                AssetDatabase.DeleteAsset(PrefabPath);

            CreateIfMissing();
        }

        static int tries;

        static void CreateIfMissing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (EditorApplication.isCompiling)
            {
                Retry();
                return;
            }

            if (!AssetDatabase.IsValidFolder(Folder)
                || AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath) == null)
            {
                Retry();
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                return;

            ArlycadMenus.CreateDefaultRoles();

            Material material = EnsureMaterial();
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            Role role = AssetDatabase.LoadAssetAtPath<Role>(RolePath);

            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
            GameObject root = new GameObject("Player");
            EditorSceneManager.MoveGameObjectToScene(root, preview);
            CharacterController controller = root.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.3f;
            controller.slopeLimit = 45f;

            root.AddComponent<PlayerControls>();
            root.AddComponent<PlayerMotor>();
            root.AddComponent<PlayerCamera>();
            PlayerControls controls = root.GetComponent<PlayerControls>();
            PlayerMotor motor = root.GetComponent<PlayerMotor>();
            PlayerCamera view = root.GetComponent<PlayerCamera>();
            RoleHolder holder = root.AddComponent<RoleHolder>();
            if (role != null)
                holder.roles = new List<Role> { role };

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            Renderer renderer = body.GetComponent<Renderer>();
            if (material != null)
                renderer.sharedMaterial = material;

            GameObject cameraObject = new GameObject("Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.fieldOfView = 70f;
            cameraObject.AddComponent<AudioListener>();
            System.Type extra = System.Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (extra != null)
                cameraObject.AddComponent(extra);

            controls.actions = actions;
            motor.controls = controls;
            motor.view = view;
            motor.bodyVisual = body.transform;
            view.controls = controls;
            view.motor = motor;
            view.aim = cameraObject.transform;
            view.bodyRenderer = renderer;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }

            AssetDatabase.SaveAssets();
        }

        static void Retry()
        {
            if (tries > 120)
            {
                Debug.LogWarning("Arlycad: префаб игрока не собран. Меню Arlycad / Пересобрать префаб игрока.");
                return;
            }

            tries++;
            EditorApplication.delayCall += CreateIfMissing;
        }

        static Material EnsureMaterial()
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing != null)
                return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            if (shader == null)
                return null;

            Material material = new Material(shader);
            Color grey = new Color(0.75f, 0.78f, 0.82f);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", grey);
            else
                material.color = grey;
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }
    }
}
