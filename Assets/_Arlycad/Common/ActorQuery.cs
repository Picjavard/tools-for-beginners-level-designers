using System.Collections.Generic;
using UnityEngine;

namespace Arlycad
{
    /// <summary>
    /// Кто задел триггер и есть ли у него нужная роль.
    /// Несколько коллайдеров одного Rigidbody считаются одним объектом.
    /// Role Holder ищется на задетом объекте и на всех его родителях.
    /// </summary>
    public static class ActorQuery
    {
        public struct Hit
        {
            public GameObject Identity;
            public GameObject SearchFrom;
        }

        public static Hit Describe(Collider other)
        {
            Hit hit = new Hit();
            if (other == null)
                return hit;

            hit.SearchFrom = other.gameObject;
            Rigidbody body = other.attachedRigidbody;
            GameObject identity = body != null ? body.gameObject : other.gameObject;
            if (body != null)
            {
                CharacterController controller = body.GetComponentInParent<CharacterController>();
                if (controller != null)
                    identity = controller.gameObject;
            }

            hit.Identity = identity;
            return hit;
        }

        public static bool Matches(GameObject searchFrom, bool reactToAny, List<Role> roles)
        {
            if (searchFrom == null)
                return false;

            if (reactToAny)
                return true;

            if (!HasAnyRoleAsset(roles))
                return false;

            return HasRole(searchFrom, roles);
        }

        public static bool HasRules(bool reactToAny, List<Role> roles)
        {
            if (reactToAny)
                return true;

            return HasAnyRoleAsset(roles);
        }

        static bool HasAnyRoleAsset(List<Role> roles)
        {
            if (roles == null)
                return false;

            for (int i = 0; i < roles.Count; i++)
            {
                if (roles[i] != null)
                    return true;
            }

            return false;
        }

        static bool HasRole(GameObject searchFrom, List<Role> roles)
        {
            Transform current = searchFrom.transform;
            while (current != null)
            {
                RoleHolder holder = current.GetComponent<RoleHolder>();
                if (holder != null && holder.ContainsAny(roles))
                    return true;

                current = current.parent;
            }

            return false;
        }
    }
}
