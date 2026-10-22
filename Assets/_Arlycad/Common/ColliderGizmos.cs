using UnityEngine;

namespace Arlycad
{
    public static class ColliderGizmos
    {
        public static void DrawAll(GameObject owner, Color color)
        {
            if (owner == null)
                return;

            Collider[] colliders = owner.GetComponents<Collider>();
            for (int i = 0; i < colliders.Length; i++)
                Draw(colliders[i], color);
        }

        public static void Draw(Collider col, Color color)
        {
            if (col == null || !col.enabled)
                return;

            Gizmos.color = color;

            BoxCollider box = col as BoxCollider;
            if (box != null)
            {
                Transform t = box.transform;
                Vector3 scale = t.lossyScale;
                Vector3 size = new Vector3(
                    box.size.x * Mathf.Abs(scale.x),
                    box.size.y * Mathf.Abs(scale.y),
                    box.size.z * Mathf.Abs(scale.z));
                Gizmos.matrix = Matrix4x4.TRS(t.TransformPoint(box.center), t.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, size);
                Gizmos.matrix = Matrix4x4.identity;
                return;
            }

            SphereCollider sphere = col as SphereCollider;
            if (sphere != null)
            {
                Transform t = sphere.transform;
                float scale = MaxAbs(t.lossyScale);
                Gizmos.DrawWireSphere(t.TransformPoint(sphere.center), sphere.radius * scale);
                return;
            }

            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }

        public static void DrawAxes(Transform t, float size)
        {
            if (t == null)
                return;

            Vector3 origin = t.position;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origin, origin + t.right * size);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(origin, origin + t.up * size);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(origin, origin + t.forward * size);
        }

        static float MaxAbs(Vector3 scale)
        {
            return Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        }
    }
}
