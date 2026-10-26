using UnityEngine;
using UnityEngine.Events;

namespace Arlycad
{
    /// <summary>
    /// Доезжает из позы, в которой стоит в редакторе, в позу Б за заданные секунды.
    /// Поза Б — это сдвиг и поворот, либо отдельная точка. Обратно теми же методами.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Arlycad/Mover", 4)]
    public class Mover : MonoBehaviour
    {
        [InspectorName("Длительность")]
        [Tooltip("Сколько секунд занимает путь от начала до конца. 0 — сразу оказаться в позе, без проезда.")]
        [Min(0f)]
        public float duration = 1f;

        [InspectorName("Сдвиг")]
        [Tooltip("Насколько сдвинуться относительно позы в редакторе. Для подъёмной двери, например, Y = 3. Игнорируется, если задана точка назначения.")]
        public Vector3 offset;

        [InspectorName("Поворот")]
        [Tooltip("На сколько градусов повернуться относительно позы в редакторе. Для двери на петлях, например, Y = 90. Игнорируется, если задана точка назначения.")]
        public Vector3 angle;

        [InspectorName("Точка назначения")]
        [Tooltip("Пустой объект позы Б. Если задан, сдвиг и поворот не используются. Не делайте его дочерним к этой двери.")]
        public Transform destination;

        [InspectorName("Плавно")]
        [Tooltip("Медленный старт и остановка. Выключите для ровной скорости.")]
        public bool smooth = true;

        [InspectorName("Ехать при старте")]
        [Tooltip("Сразу поехать в позу Б, когда сцена запускается.")]
        public bool playOnStart;

        [InspectorName("Туда и обратно")]
        [Tooltip("Доехав до конца, сразу ехать назад, и так по кругу.")]
        public bool pingPong;

        [InspectorName("Когда поехал")]
        public UnityEvent whenStarted = new UnityEvent();

        [InspectorName("Когда приехал")]
        public UnityEvent whenFinished = new UnityEvent();

        Vector3 originLocalPosition;
        Quaternion originLocalRotation;
        Vector3 originWorldPosition;
        Quaternion originWorldRotation;
        Rigidbody body;
        float progress;
        float goal;
        bool moving;
        bool captured;
        bool usePhysics;
        bool blocked;
        bool warnedDynamic;
        bool warnedChild;

        public void ToEnd()
        {
            Command(1f);
        }

        public void ToStart()
        {
            Command(0f);
        }

        public void Toggle()
        {
            Command(goal > 0.5f ? 0f : 1f);
        }

        public void Stop()
        {
            moving = false;
        }

        void Awake()
        {
            Capture();
            body = GetComponent<Rigidbody>();
            if (body == null)
                return;

            if (!body.isKinematic)
            {
                blocked = true;
                WarnDynamic();
                return;
            }

            usePhysics = true;
            body.interpolation = RigidbodyInterpolation.None;
        }

        void Start()
        {
            if (playOnStart)
                ToEnd();
        }

        void Update()
        {
            if (blocked || usePhysics)
                return;

            Tick(Time.deltaTime);
        }

        void FixedUpdate()
        {
            if (usePhysics)
                Tick(Time.fixedDeltaTime);
        }

        void Command(float next)
        {
            if (!captured)
                Capture();

            goal = next;
            if (Approximately(progress, goal))
                return;

            bool wasMoving = moving;
            moving = true;
            if (!wasMoving)
                whenStarted.Invoke();
        }

        void Tick(float deltaTime)
        {
            if (!moving)
                return;

            if (duration <= 0f)
                progress = goal;
            else if (progress < goal)
                progress = Mathf.Min(goal, progress + deltaTime / duration);
            else
                progress = Mathf.Max(goal, progress - deltaTime / duration);

            Apply(progress);

            if (!Approximately(progress, goal))
                return;

            progress = goal;
            moving = false;
            whenFinished.Invoke();

            if (!pingPong || duration <= 0f)
                return;

            goal = goal > 0.5f ? 0f : 1f;
            moving = true;
            whenStarted.Invoke();
        }

        void Apply(float t)
        {
            float shaped = smooth ? Mathf.SmoothStep(0f, 1f, t) : t;
            if (HasDestination())
            {
                ApplyWorld(
                    Vector3.LerpUnclamped(originWorldPosition, destination.position, shaped),
                    Quaternion.SlerpUnclamped(originWorldRotation, destination.rotation, shaped));
                return;
            }

            Vector3 localPosition = Vector3.LerpUnclamped(originLocalPosition, originLocalPosition + offset, shaped);
            Quaternion localRotation = Quaternion.SlerpUnclamped(originLocalRotation, originLocalRotation * Quaternion.Euler(angle), shaped);
            if (usePhysics && body != null)
            {
                Transform parent = transform.parent;
                Vector3 worldPosition = parent != null ? parent.TransformPoint(localPosition) : localPosition;
                Quaternion worldRotation = parent != null ? parent.rotation * localRotation : localRotation;
                ApplyWorld(worldPosition, worldRotation);
                return;
            }

            transform.localPosition = localPosition;
            transform.localRotation = localRotation;
        }

        void ApplyWorld(Vector3 worldPosition, Quaternion worldRotation)
        {
            if (usePhysics && body != null)
            {
                body.MovePosition(worldPosition);
                body.MoveRotation(worldRotation);
                return;
            }

            transform.SetPositionAndRotation(worldPosition, worldRotation);
        }

        void Capture()
        {
            originLocalPosition = transform.localPosition;
            originLocalRotation = transform.localRotation;
            originWorldPosition = transform.position;
            originWorldRotation = transform.rotation;
            captured = true;
        }

        bool HasDestination()
        {
            if (destination == null)
                return false;

            if (!destination.IsChildOf(transform))
                return true;

            if (!warnedChild)
            {
                warnedChild = true;
                Debug.LogWarning("Mover: точка назначения у «" + name + "» стоит внутри него и уедет вместе с дверью. Используется сдвиг.", this);
            }

            return false;
        }

        void WarnDynamic()
        {
            if (warnedDynamic)
                return;

            warnedDynamic = true;
            Debug.LogWarning("Mover: у «" + name + "» Rigidbody не кинематический. Включите Is Kinematic, иначе дверь будет драться с физикой.", this);
        }

        static bool Approximately(float a, float b)
        {
            return Mathf.Abs(a - b) <= 0.0001f;
        }

        void OnDrawGizmosSelected()
        {
            Vector3 from = Application.isPlaying && captured ? originWorldPosition : transform.position;
            Quaternion fromRotation = Application.isPlaying && captured ? originWorldRotation : transform.rotation;
            Vector3 to;
            Quaternion toRotation;
            if (destination != null && !destination.IsChildOf(transform))
            {
                to = destination.position;
                toRotation = destination.rotation;
            }
            else
            {
                Vector3 localStart = Application.isPlaying && captured ? originLocalPosition : transform.localPosition;
                Quaternion localStartRotation = Application.isPlaying && captured ? originLocalRotation : transform.localRotation;
                Vector3 localEnd = localStart + offset;
                Quaternion localEndRotation = localStartRotation * Quaternion.Euler(angle);
                Transform parent = transform.parent;
                to = parent != null ? parent.TransformPoint(localEnd) : localEnd;
                toRotation = parent != null ? parent.rotation * localEndRotation : localEndRotation;
                from = parent != null ? parent.TransformPoint(localStart) : localStart;
                fromRotation = parent != null ? parent.rotation * localStartRotation : localStartRotation;
            }

            Gizmos.color = new Color(1f, 0.55f, 0.1f);
            Gizmos.DrawLine(from, to);
            Gizmos.DrawRay(to, toRotation * Vector3.forward * 0.6f);
            Gizmos.color = new Color(1f, 0.75f, 0.35f);
            Gizmos.DrawRay(to, toRotation * Vector3.up * 0.35f);
        }
    }
}
