using System;
using UnityEngine;

namespace Arlycad
{
    /// <summary>
    /// Стабильный ключ объекта. В Unity 6.6 InstanceID заменён на EntityId.
    /// </summary>
    public readonly struct ActorId : IEquatable<ActorId>
    {
        readonly ulong value;

        ActorId(ulong value)
        {
            this.value = value;
        }

        public static ActorId Of(UnityEngine.Object target)
        {
            if (target == null)
                return default;

#if UNITY_6000_6_OR_NEWER
            return new ActorId(EntityId.ToULong(target.GetEntityId()));
#else
            return new ActorId(unchecked((uint)target.GetInstanceID()));
#endif
        }

        public bool Equals(ActorId other)
        {
            return value == other.value;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }
    }
}
