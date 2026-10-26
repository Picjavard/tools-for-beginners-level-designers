using System.Collections.Generic;
using UnityEngine;

namespace Arlycad
{
    /// <summary>
    /// Считает коллайдеры одного объекта.
    /// Вход считается, когда пришёл первый коллайдер, выход — когда ушёл последний.
    /// </summary>
    public sealed class TriggerLatch
    {
        readonly Dictionary<ActorId, int> counts = new Dictionary<ActorId, int>();

        public int Count
        {
            get { return counts.Count; }
        }

        public bool Enter(GameObject actor)
        {
            if (actor == null)
                return false;

            ActorId id = ActorId.Of(actor);
            int count;
            counts.TryGetValue(id, out count);
            counts[id] = count + 1;
            return count == 0;
        }

        public bool Exit(GameObject actor)
        {
            if (actor == null)
                return false;

            ActorId id = ActorId.Of(actor);
            int count;
            if (!counts.TryGetValue(id, out count))
                return false;

            count--;
            if (count <= 0)
            {
                counts.Remove(id);
                return true;
            }

            counts[id] = count;
            return false;
        }

        public void Clear()
        {
            counts.Clear();
        }
    }
}
