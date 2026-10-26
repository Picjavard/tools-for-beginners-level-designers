using UnityEngine;

namespace Arlycad
{
    /// <summary>
    /// Ставит объект в точку так, чтобы телепорт не ломал Character Controller и Rigidbody.
    /// </summary>
    public static class PhysicsPlacement
    {
        public static void Place(GameObject target, Vector3 position, Quaternion rotation, bool applyRotation)
        {
            if (target == null)
                return;

            CharacterController controller = target.GetComponentInParent<CharacterController>();
            Rigidbody body = target.GetComponent<Rigidbody>();
            if (body == null && controller == null)
                body = target.GetComponentInParent<Rigidbody>();

            Transform root = target.transform;
            if (controller != null)
                root = controller.transform;
            else if (body != null)
                root = body.transform;

            if (controller != null)
                controller.enabled = false;

            if (body != null && body.transform == root)
            {
                body.position = position;
                if (applyRotation)
                    body.rotation = rotation;

                if (!body.isKinematic)
                {
                    body.angularVelocity = Vector3.zero;
                    SetLinearVelocity(body, Vector3.zero);
                }
            }

            root.position = position;
            if (applyRotation)
                root.rotation = rotation;

            if (controller != null)
                controller.enabled = true;

            Physics.SyncTransforms();
        }

        static void SetLinearVelocity(Rigidbody body, Vector3 velocity)
        {
#if UNITY_6000_0_OR_NEWER
            body.linearVelocity = velocity;
#else
            body.velocity = velocity;
#endif
        }
    }
}
