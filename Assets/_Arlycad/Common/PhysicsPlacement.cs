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

            Rigidbody body = target.GetComponentInParent<Rigidbody>();
            CharacterController controller = target.GetComponentInParent<CharacterController>();

            Transform root = target.transform;
            if (body != null)
                root = body.transform;
            else if (controller != null)
                root = controller.transform;

            if (controller != null)
                controller.enabled = false;

            if (body != null)
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
