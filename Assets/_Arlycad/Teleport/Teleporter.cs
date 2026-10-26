using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Arlycad
{
    /// <summary>
    /// Переносит объект в точку назначения.
    /// Галочка «телепортировать вошедшего» делает плиту самостоятельной.
    /// Метод Teleport() двигает объект из поля «кого двигает Teleport» — его вызывает UnityEvent.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Arlycad/Teleporter", 2)]
    public class Teleporter : MonoBehaviour
    {
        [Header("Куда")]
        [InspectorName("Точка назначения")]
        [Tooltip("Пустой объект в уровне. Синяя стрелка этой точки — куда объект будет смотреть, если включён поворот.")]
        public Transform destination;

        [InspectorName("Повернуть как точку")]
        [Tooltip("После переноса повернуть объект так же, как повёрнута точка назначения.")]
        public bool applyDestinationRotation = true;

        [InspectorName("Сдвиг")]
        [Tooltip("Сдвиг относительно точки назначения. Если персонаж проваливается в пол, поставьте Y = 0.1.")]
        public Vector3 extraOffset;

        [Header("Кто входит в эту плиту")]
        [InspectorName("Телепортировать вошедшего")]
        [Tooltip("Переносить того, кто вошёл в коллайдер этого объекта. Точка назначения должна стоять снаружи коллайдера.")]
        public bool teleportOnEnter = true;

        [InspectorName("Реагировать на всех")]
        [Tooltip("Принимать любой физический объект. Выключите, если плита должна реагировать только на игрока или на ящик.")]
        public bool reactToAny = true;

        [InspectorName("Роли")]
        [Tooltip("Роли, которым разрешён вход. Проверяются, только если «Реагировать на всех» выключено.")]
        public List<Role> roles = new List<Role>();

        [InspectorName("Пауза, секунды")]
        [Tooltip("Тот же объект не телепортируется повторно, пока не пройдёт столько секунд. Защита от плиты, точка которой стоит внутри неё самой.")]
        [Min(0f)]
        public float cooldown = 0.5f;

        [InspectorName("Только один раз")]
        [Tooltip("Сработать один раз за запуск сцены.")]
        public bool onlyOnce;

        [Header("Кого двигает метод Teleport")]
        [InspectorName("Объект")]
        [Tooltip("Объект для вызова Teleport() из UnityEvent. Если пусто, двигается объект, на котором висит этот компонент.")]
        public GameObject assignedObject;

        [Header("Событие")]
        [InspectorName("После телепорта")]
        [Tooltip("Вызывается после переноса. Сюда вешают звук, выключение света, вспышку.")]
        public UnityEvent onTeleported = new UnityEvent();

        [InspectorName("Писать в консоль")]
        [Tooltip("Писать в Console причины отказа.")]
        public bool debugLog;

        readonly TriggerLatch latch = new TriggerLatch();
        readonly List<GameObject> pendingEntrants = new List<GameObject>();
        readonly List<GameObject> pendingMoves = new List<GameObject>();
        readonly Dictionary<ActorId, float> nextEnterTime = new Dictionary<ActorId, float>();

        bool spent;
        bool warnedMissingDestination;

        void OnTriggerEnter(Collider other)
        {
            if (!teleportOnEnter)
                return;

            ActorQuery.Hit hit = ActorQuery.Describe(other);
            if (hit.Identity == null || hit.Identity == gameObject)
                return;

            if (!ActorQuery.Matches(hit.SearchFrom, reactToAny, roles))
            {
                Reject(hit.Identity, "нет подходящей роли");
                return;
            }

            bool first = latch.Enter(hit.Identity);
            if (!first)
                return;

            if (destination == null)
            {
                latch.Exit(hit.Identity);
                WarnMissingDestination();
                return;
            }

            if (spent)
            {
                latch.Exit(hit.Identity);
                Reject(hit.Identity, "телепорт уже использован");
                return;
            }

            if (IsCoolingDown(hit.Identity))
            {
                latch.Exit(hit.Identity);
                Reject(hit.Identity, "ещё не прошла пауза");
                return;
            }

            if (IsInsideOwnTrigger(DestinationPosition()))
            {
                latch.Exit(hit.Identity);
                Reject(hit.Identity, "точка назначения стоит внутри этой же плиты");
                return;
            }

            Stamp(hit.Identity);
            if (onlyOnce)
                spent = true;

            pendingEntrants.Add(hit.Identity);
        }

        void OnTriggerExit(Collider other)
        {
            if (!teleportOnEnter)
                return;

            ActorQuery.Hit hit = ActorQuery.Describe(other);
            if (hit.Identity == null)
                return;

            latch.Exit(hit.Identity);
        }

        void LateUpdate()
        {
            for (int i = 0; i < pendingEntrants.Count; i++)
                Schedule(pendingEntrants[i]);

            pendingEntrants.Clear();
            Flush();
        }

        void OnDisable()
        {
            latch.Clear();
            pendingEntrants.Clear();
            pendingMoves.Clear();
        }

        /// <summary>
        /// Для UnityEvent без аргумента. Двигает assignedObject либо объект с этим компонентом.
        /// </summary>
        public void Teleport()
        {
            Teleport(assignedObject != null ? assignedObject : gameObject);
        }

        /// <summary>
        /// Для события, которое передаёт вошедший объект (Zone Trigger → On Enter).
        /// </summary>
        public void Teleport(GameObject target)
        {
            Schedule(target);
            if (!isActiveAndEnabled)
                Flush();
        }

        public void ResetTeleporter()
        {
            spent = false;
            latch.Clear();
            pendingEntrants.Clear();
            pendingMoves.Clear();
            nextEnterTime.Clear();
        }

        void Schedule(GameObject target)
        {
            if (target == null)
                return;

            if (destination == null)
            {
                WarnMissingDestination();
                return;
            }

            pendingMoves.Add(target);
        }

        void Flush()
        {
            int count = pendingMoves.Count;
            for (int i = 0; i < count; i++)
                Move(pendingMoves[i]);

            if (count > 0)
                pendingMoves.RemoveRange(0, count);
        }

        void Move(GameObject target)
        {
            if (target == null || destination == null)
                return;

            Vector3 position = DestinationPosition();
            if (teleportOnEnter && IsInsideOwnTrigger(position))
            {
                Debug.LogWarning("Телепорт «" + name + "»: точка назначения внутри плиты, перенос отменён.", this);
                return;
            }

            Quaternion rotation = applyDestinationRotation ? destination.rotation : target.transform.rotation;
            PhysicsPlacement.Place(target, position, rotation, applyDestinationRotation);

            if (debugLog)
                Debug.Log("Телепорт «" + name + "»: перенёс " + target.name, this);

            onTeleported.Invoke();
        }

        Vector3 DestinationPosition()
        {
            return destination.position + destination.TransformVector(extraOffset);
        }

        bool IsInsideOwnTrigger(Vector3 position)
        {
            Collider[] colliders = GetComponents<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider col = colliders[i];
                if (col != null && col.enabled && col.isTrigger && col.bounds.Contains(position))
                    return true;
            }

            return false;
        }

        bool IsCoolingDown(GameObject actor)
        {
            if (cooldown <= 0f)
                return false;

            float readyAt;
            return nextEnterTime.TryGetValue(ActorId.Of(actor), out readyAt) && Time.time < readyAt;
        }

        void Stamp(GameObject actor)
        {
            if (cooldown <= 0f)
                return;

            nextEnterTime[ActorId.Of(actor)] = Time.time + cooldown;
        }

        void WarnMissingDestination()
        {
            if (warnedMissingDestination)
                return;

            warnedMissingDestination = true;
            Debug.LogWarning("Телепорт «" + name + "»: не задана точка назначения.", this);
        }

        void Reject(GameObject actor, string reason)
        {
            if (!debugLog || actor == null)
                return;

            Debug.Log("Телепорт «" + name + "»: " + actor.name + " — " + reason, this);
        }

        void OnDrawGizmos()
        {
            if (teleportOnEnter)
                ColliderGizmos.DrawAll(gameObject, new Color(0.2f, 0.75f, 0.95f, 1f));

            if (destination == null)
            {
                Gizmos.color = new Color(0.95f, 0.35f, 0.3f, 1f);
                Gizmos.DrawWireSphere(transform.position, 0.25f);
                return;
            }

            Gizmos.color = new Color(0.2f, 0.75f, 0.95f, 1f);
            Gizmos.DrawLine(transform.position, destination.position);
            ColliderGizmos.DrawAxes(destination, 0.75f);
        }
    }
}
