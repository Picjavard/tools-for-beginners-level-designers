using UnityEngine;
using UnityEngine.InputSystem;

namespace Arlycad
{
    /// <summary>
    /// Один из трёх видов. Переключается полем в инспекторе и методами для UnityEvent.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMotor))]
    [DefaultExecutionOrder(0)]
    [AddComponentMenu("Arlycad/Player Camera", 12)]
    public class PlayerCamera : MonoBehaviour
    {
        public enum ViewMode
        {
            FirstPerson,
            ThirdPerson,
            TopDown
        }

        [InspectorName("Вид")]
        public ViewMode mode = ViewMode.ThirdPerson;

        [InspectorName("Чувствительность мыши")]
        [Min(0f)]
        public float sensitivity = 0.12f;

        [InspectorName("Нижний предел взгляда")]
        public float pitchMin = -70f;

        [InspectorName("Верхний предел взгляда")]
        public float pitchMax = 75f;

        [Header("От третьего лица")]
        [InspectorName("Дистанция")]
        [Min(0.2f)]
        public float thirdDistance = 3.2f;

        [InspectorName("Высота плеча")]
        public float thirdShoulder = 0.4f;

        [Header("Сверху")]
        [InspectorName("Дистанция")]
        [Min(1f)]
        public float topDistance = 14f;

        [InspectorName("Наклон")]
        public float topPitch = 55f;

        [InspectorName("Поворот плана")]
        public float topYaw;

        [HideInInspector]
        public PlayerControls controls;

        [HideInInspector]
        public PlayerMotor motor;

        [HideInInspector]
        public Transform aim;

        [HideInInspector]
        public Renderer bodyRenderer;

        float yaw;
        float pitch = 12f;

        public ViewMode Mode
        {
            get { return mode; }
        }

        public Vector3 PlanarForward
        {
            get
            {
                float source = mode == ViewMode.TopDown ? topYaw : yaw;
                return Quaternion.Euler(0f, source, 0f) * Vector3.forward;
            }
        }

        void Awake()
        {
            if (controls == null)
                controls = GetComponent<PlayerControls>();
            if (motor == null)
                motor = GetComponent<PlayerMotor>();

            yaw = transform.eulerAngles.y;
            LockCursor();
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (Cursor.lockState != CursorLockMode.Locked
                && Mouse.current != null
                && Mouse.current.leftButton.wasPressedThisFrame)
            {
                LockCursor();
            }

            if (mode == ViewMode.TopDown || Cursor.lockState != CursorLockMode.Locked)
                return;

            Vector2 look = controls != null ? controls.Look : Vector2.zero;
            yaw += look.x * sensitivity;
            pitch = Mathf.Clamp(pitch - look.y * sensitivity, pitchMin, pitchMax);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        void LateUpdate()
        {
            if (aim == null)
                return;

            if (bodyRenderer != null)
                bodyRenderer.enabled = mode != ViewMode.FirstPerson;

            if (mode == ViewMode.FirstPerson)
                PlaceFirst();
            else if (mode == ViewMode.ThirdPerson)
                PlaceThird();
            else
                PlaceTop();
        }

        public void SetFirstPerson()
        {
            mode = ViewMode.FirstPerson;
        }

        public void SetThirdPerson()
        {
            mode = ViewMode.ThirdPerson;
        }

        public void SetTopDown()
        {
            mode = ViewMode.TopDown;
        }

        void PlaceFirst()
        {
            float eye = motor != null ? motor.EyeHeight : 1.6f;
            aim.position = transform.position + Vector3.up * eye;
            aim.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        void PlaceThird()
        {
            float eye = motor != null ? motor.EyeHeight : 1.6f;
            Vector3 pivot = transform.position + Vector3.up * eye;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desired = pivot + rotation * new Vector3(thirdShoulder, 0f, -thirdDistance);
            aim.position = PullIn(pivot, desired);
            aim.rotation = rotation;
        }

        void PlaceTop()
        {
            Quaternion rotation = Quaternion.Euler(topPitch, topYaw, 0f);
            Vector3 focus = transform.position + Vector3.up * 1.2f;
            aim.position = focus + rotation * new Vector3(0f, 0f, -topDistance);
            aim.rotation = rotation;
        }

        static readonly RaycastHit[] pullHits = new RaycastHit[16];

        Vector3 PullIn(Vector3 pivot, Vector3 desired)
        {
            Vector3 offset = desired - pivot;
            float distance = offset.magnitude;
            if (distance < 0.05f)
                return desired;

            Vector3 direction = offset / distance;
            int count = Physics.SphereCastNonAlloc(pivot, 0.15f, direction, pullHits, distance, ~0, QueryTriggerInteraction.Ignore);
            float nearest = distance;
            for (int i = 0; i < count; i++)
            {
                Transform hit = pullHits[i].transform;
                if (hit == transform || hit.IsChildOf(transform))
                    continue;

                if (pullHits[i].distance < nearest)
                    nearest = pullHits[i].distance;
            }

            if (nearest >= distance - 0.01f)
                return desired;

            return pivot + direction * Mathf.Max(0.25f, nearest - 0.15f);
        }

        static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
