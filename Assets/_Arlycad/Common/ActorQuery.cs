using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arlycad
{
    /// <summary>
    /// Кто задел триггер и подходит ли он под фильтр зоны.
    /// Несколько коллайдеров одного Rigidbody считаются одним объектом.
    /// Метка ищется на задетом объекте и на всех его родителях.
    /// </summary>
    public static class ActorQuery
    {
        public struct Hit
        {
            public GameObject Identity;
            public GameObject SearchFrom;
        }

        static readonly Dictionary<string, Type> Types = new Dictionary<string, Type>();

        public static Hit Describe(Collider other)
        {
            Hit hit = new Hit();
            if (other == null)
                return hit;

            hit.SearchFrom = other.gameObject;
            hit.Identity = other.attachedRigidbody != null
                ? other.attachedRigidbody.gameObject
                : other.gameObject;
            return hit;
        }

        public static bool Matches(GameObject searchFrom, bool reactToAny, List<string> markerNames, List<Role> roles)
        {
            if (searchFrom == null)
                return false;

            if (reactToAny)
                return true;

            bool hasMarkers = markerNames != null && markerNames.Count > 0;
            bool hasRoles = HasAnyRoleAsset(roles);
            if (!hasMarkers && !hasRoles)
                return false;

            if (hasMarkers && HasMarker(searchFrom, markerNames))
                return true;

            if (hasRoles && HasRole(searchFrom, roles))
                return true;

            return false;
        }

        public static bool HasRules(bool reactToAny, List<string> markerNames, List<Role> roles)
        {
            if (reactToAny)
                return true;

            if (markerNames != null && markerNames.Count > 0)
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

        static bool HasMarker(GameObject searchFrom, List<string> markerNames)
        {
            Transform current = searchFrom.transform;
            while (current != null)
            {
                for (int i = 0; i < markerNames.Count; i++)
                {
                    Type type = FindType(markerNames[i]);
                    if (type != null && current.GetComponent(type) != null)
                        return true;
                }

                current = current.parent;
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

        static Type FindType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            Type cached;
            if (Types.TryGetValue(typeName, out cached))
                return cached;

            System.Reflection.Assembly assembly = typeof(ActorQuery).Assembly;
            Type found = assembly.GetType(typeName);
            if (found == null)
                found = assembly.GetType("Arlycad." + typeName);

            Types[typeName] = found;
            return found;
        }
    }
}
