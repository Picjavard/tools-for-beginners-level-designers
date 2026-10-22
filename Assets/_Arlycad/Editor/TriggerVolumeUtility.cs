using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Arlycad
{
    public static class TriggerVolumeUtility
    {
        public static bool HasMeshCollider(GameObject owner)
        {
            return owner != null && owner.GetComponent<MeshCollider>() != null;
        }

        public static bool HasConcaveTriggerMesh(GameObject owner)
        {
            if (owner == null)
                return false;

            MeshCollider[] meshes = owner.GetComponents<MeshCollider>();
            for (int i = 0; i < meshes.Length; i++)
            {
                if (meshes[i].isTrigger && !meshes[i].convex)
                    return true;
            }

            return false;
        }

        public static bool AnyColliderIsTrigger(GameObject owner)
        {
            if (owner == null)
                return false;

            Collider[] colliders = owner.GetComponents<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].isTrigger)
                    return true;
            }

            return false;
        }

        public static void EnableTriggers(GameObject owner)
        {
            Collider[] colliders = owner.GetComponents<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                Undo.RecordObject(colliders[i], "Enable Is Trigger");
                colliders[i].isTrigger = true;
            }
        }

        public static void AddBoxTrigger(GameObject owner)
        {
            BoxCollider box = Undo.AddComponent<BoxCollider>(owner);
            box.isTrigger = true;
            box.size = new Vector3(2f, 2f, 2f);
        }

        public static void MoveToChildVolume(Component source)
        {
            if (source == null)
                return;

            GameObject host = source.gameObject;
            GameObject child = new GameObject("Trigger Volume");
            Undo.RegisterCreatedObjectUndo(child, "Create trigger volume");
            child.transform.SetParent(host.transform, false);
            child.layer = host.layer;

            BoxCollider box = Undo.AddComponent<BoxCollider>(child);
            box.isTrigger = true;
            Fit(host, box);

            if (!ComponentUtility.CopyComponent(source))
                return;

            if (!ComponentUtility.PasteComponentAsNew(child))
                return;

            Undo.DestroyObjectImmediate(source);
            Selection.activeGameObject = child;
        }

        static void Fit(GameObject host, BoxCollider box)
        {
            MeshFilter filter = host.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
            {
                box.center = filter.sharedMesh.bounds.center;
                box.size = filter.sharedMesh.bounds.size;
                return;
            }

            Renderer renderer = host.GetComponent<Renderer>();
            if (renderer == null)
            {
                box.size = Vector3.one;
                return;
            }

            Bounds world = renderer.bounds;
            Transform transform = host.transform;
            Vector3 scale = transform.lossyScale;
            box.center = transform.InverseTransformPoint(world.center);
            box.size = new Vector3(
                Divide(world.size.x, scale.x),
                Divide(world.size.y, scale.y),
                Divide(world.size.z, scale.z));
        }

        static float Divide(float size, float scale)
        {
            if (Mathf.Abs(scale) < 0.0001f)
                return size;

            return size / scale;
        }
    }
}
