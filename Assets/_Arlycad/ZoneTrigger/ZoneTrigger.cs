using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Arlycad
{
    /// <summary>
    /// Объём, который вызывает UnityEvent, когда в него входит подходящий объект.
    /// Сам ничего не открывает и не телепортирует: эти действия вешаются на события.
    /// Старые поля OnEnter и OnExit сохранены и по-прежнему передают объект.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("Arlycad/Zone Trigger", 1)]
    public class ZoneTrigger : MonoBehaviour
    {
        [Serializable]
        public class TriggerEvent : UnityEvent<GameObject> { }

        [InspectorName("Реагировать на всех")]
        [Tooltip("Реагировать на любой физический объект. Метки и роли тогда не проверяются.")]
        public bool reactToAny;

        [InspectorName("Роли")]
        [Tooltip("Роли-ассеты. Объект подходит, если у него или у родителя есть Role Holder с одной из этих ролей. Новая роль создаётся без кода: Create > Arlycad > Role.")]
        public List<Role> roles = new List<Role>();

        [InspectorName("Метки")]
        [Tooltip("Имена компонентов-меток из папки Tags. Объект подходит, если на нём или на родителе висит хотя бы одна.")]
        public List<string> targetScriptNames = new List<string>();

        [InspectorName("Только один раз")]
        [Tooltip("После первого входа зона замолкает до перезапуска сцены или вызова ResetZone.")]
        public bool onlyOnce;

        [InspectorName("Пауза, секунды")]
        [Tooltip("Повторный вход того же объекта не раньше чем через столько секунд. Пока объект стоит внутри, зона сама не срабатывает снова.")]
        [Min(0f)]
        public float cooldown;

        [InspectorName("Повторять, пока внутри")]
        [Tooltip("Пока объект внутри, вызывать событие «Пока внутри» с паузой. Для урона и звука.")]
        public bool repeatWhileInside;

        [InspectorName("Интервал повтора")]
        [Tooltip("Пауза между повторами, в секундах.")]
        [Min(0.05f)]
        public float repeatInterval = 1f;

        [InspectorName("Писать в консоль")]
        [Tooltip("Писать в Console, почему зона приняла или не приняла объект. Включайте, когда у студента «не работает».")]
        public bool debugLog;

        [Header("События без аргумента")]
        [InspectorName("При входе")]
        [Tooltip("Действия при входе. Сюда вешайте SetActive, Play, Spawn, Teleport: методы, которым не нужен объект.")]
        public UnityEvent whenEntered = new UnityEvent();

        [InspectorName("При выходе")]
        [Tooltip("Действия, когда объект вышел.")]
        public UnityEvent whenLeft = new UnityEvent();

        [InspectorName("Пока внутри")]
        [Tooltip("Повтор, пока объект внутри. Число (урон, интервал) задаётся на том компоненте, который вы вызываете, а не в этом событии.")]
        public UnityEvent whileInside = new UnityEvent();

        [Header("События с объектом")]
        [InspectorName("При входе, с объектом")]
        [Tooltip("Прежнее событие входа. В аргументе корень вошедшего объекта (тот, на ком Rigidbody). Уже настроенные вызовы остаются здесь.")]
        public TriggerEvent OnEnter = new TriggerEvent();

        [InspectorName("При выходе, с объектом")]
        [Tooltip("Прежнее событие выхода. В аргументе тот же объект, что приходил в OnEnter.")]
        public TriggerEvent OnExit = new TriggerEvent();

        readonly TriggerLatch latch = new TriggerLatch();
        readonly List<GameObject> entered = new List<GameObject>();
        readonly List<GameObject> exited = new List<GameObject>();
        readonly Dictionary<ActorId, float> nextEnterTime = new Dictionary<ActorId, float>();
        readonly Dictionary<GameObject, float> nextRepeatAt = new Dictionary<GameObject, float>();
        readonly List<GameObject> repeatKeys = new List<GameObject>();

        bool spent;

        public int InsideCount
        {
            get { return latch.Count; }
        }

        void Reset()
        {
            Collider[] colliders = GetComponents<Collider>();
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            ActorQuery.Hit hit = ActorQuery.Describe(other);
            if (hit.Identity == null || hit.Identity == gameObject)
                return;

            if (!ActorQuery.Matches(hit.SearchFrom, reactToAny, targetScriptNames, roles))
            {
                Reject(hit.Identity, "нет подходящей метки или роли");
                return;
            }

            bool first = latch.Enter(hit.Identity);
            if (!first)
                return;

            if (spent)
            {
                latch.Exit(hit.Identity);
                Reject(hit.Identity, "зона уже сработала и включено «только один раз»");
                return;
            }

            if (IsCoolingDown(hit.Identity))
            {
                latch.Exit(hit.Identity);
                Reject(hit.Identity, "ещё не прошла пауза между входами");
                return;
            }

            Stamp(hit.Identity);
            if (onlyOnce)
                spent = true;

            entered.Add(hit.Identity);
        }

        void OnTriggerExit(Collider other)
        {
            ActorQuery.Hit hit = ActorQuery.Describe(other);
            if (hit.Identity == null)
                return;

            if (!latch.Exit(hit.Identity))
                return;

            exited.Add(hit.Identity);
        }

        void Update()
        {
            if (!repeatWhileInside || nextRepeatAt.Count == 0)
                return;

            repeatKeys.Clear();
            foreach (KeyValuePair<GameObject, float> pair in nextRepeatAt)
                repeatKeys.Add(pair.Key);

            float interval = Mathf.Max(0.05f, repeatInterval);
            for (int i = 0; i < repeatKeys.Count; i++)
            {
                GameObject actor = repeatKeys[i];
                if (actor == null)
                {
                    nextRepeatAt.Remove(actor);
                    continue;
                }

                float due;
                if (!nextRepeatAt.TryGetValue(actor, out due) || Time.time < due)
                    continue;

                nextRepeatAt[actor] = Time.time + interval;
                whileInside.Invoke();
            }
        }

        void LateUpdate()
        {
            for (int i = 0; i < entered.Count; i++)
            {
                GameObject actor = entered[i];
                if (actor == null)
                    continue;

                if (debugLog)
                    Debug.Log("Зона «" + name + "»: вошёл " + actor.name, this);

                whenEntered.Invoke();
                OnEnter.Invoke(actor);

                if (repeatWhileInside)
                    nextRepeatAt[actor] = Time.time + Mathf.Max(0.05f, repeatInterval);
            }

            entered.Clear();

            for (int i = 0; i < exited.Count; i++)
            {
                GameObject actor = exited[i];
                nextRepeatAt.Remove(actor);
                if (actor == null)
                    continue;

                if (debugLog)
                    Debug.Log("Зона «" + name + "»: вышел " + actor.name, this);

                whenLeft.Invoke();
                OnExit.Invoke(actor);
            }

            exited.Clear();
        }

        void OnDisable()
        {
            latch.Clear();
            entered.Clear();
            exited.Clear();
            nextRepeatAt.Clear();
        }

        /// <summary>
        /// Снова разрешает вход. Уже стоящие внутри объекты сработают, только если выйдут и зайдут ещё раз.
        /// </summary>
        public void ResetZone()
        {
            spent = false;
            latch.Clear();
            entered.Clear();
            exited.Clear();
            nextEnterTime.Clear();
            nextRepeatAt.Clear();
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

        void Reject(GameObject actor, string reason)
        {
            if (!debugLog || actor == null)
                return;

            Debug.Log("Зона «" + name + "»: " + actor.name + " — " + reason, this);
        }

        void OnDrawGizmos()
        {
            ColliderGizmos.DrawAll(gameObject, new Color(0.25f, 0.85f, 0.4f, 1f));
        }
    }
}
