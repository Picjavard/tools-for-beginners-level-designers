using UnityEngine;

namespace Arlycad
{
    /// <summary>
    /// Ходьба, прыжок и приседание. Камеру не переключает.
    /// Кинематическое тело висит на дочернем Sensor, не на контроллере:
    /// иначе физика толкает капсулу и ход дёргается.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerControls))]
    [DefaultExecutionOrder(10)]
    [AddComponentMenu("Arlycad/Player Motor", 11)]
    public class PlayerMotor : MonoBehaviour
    {
        [InspectorName("Скорость")]
        [Min(0f)]
        public float walkSpeed = 5f;

        [InspectorName("Скорость в приседе")]
        [Min(0f)]
        public float crouchSpeed = 2.5f;

        [InspectorName("Высота прыжка")]
        [Min(0f)]
        public float jumpHeight = 1.2f;

        [InspectorName("Гравитация")]
        [Min(0f)]
        public float gravity = 25f;

        [InspectorName("Рост стоя")]
        [Min(0.5f)]
        public float standingHeight = 1.8f;

        [InspectorName("Рост в приседе")]
        [Min(0.5f)]
        public float crouchHeight = 1.1f;

        [HideInInspector]
        public PlayerControls controls;

        [HideInInspector]
        public PlayerCamera view;

        [HideInInspector]
        public Transform bodyVisual;

        CharacterController controller;
        CapsuleCollider sensor;
        float verticalVelocity;
        bool wantsCrouch;

        public bool IsCrouched
        {
            get { return wantsCrouch; }
        }

        public float EyeHeight
        {
            get { return wantsCrouch ? crouchHeight * 0.85f : standingHeight * 0.9f; }
        }

        void Reset()
        {
            PreparePhysics();
        }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (controls == null)
                controls = GetComponent<PlayerControls>();
            if (view == null)
                view = GetComponent<PlayerCamera>();

            PreparePhysics();
            ApplyHeight(standingHeight);
        }

        void OnEnable()
        {
            if (controls != null)
                controls.CrouchPressed += ToggleCrouch;
        }

        void OnDisable()
        {
            if (controls != null)
                controls.CrouchPressed -= ToggleCrouch;
        }

        void Update()
        {
            if (wantsCrouch)
                ApplyHeight(crouchHeight);
            else if (controller.height < standingHeight - 0.01f && HasHeadroom())
                ApplyHeight(standingHeight);

            Vector2 input = controls != null ? controls.Move : Vector2.zero;
            if (input.sqrMagnitude > 1f)
                input.Normalize();

            Vector3 forward = FlatForward();
            Vector3 right = new Vector3(forward.z, 0f, -forward.x);
            float speed = wantsCrouch ? crouchSpeed : walkSpeed;
            Vector3 wish = (forward * input.y + right * input.x) * speed;

            if (view != null && view.Mode == PlayerCamera.ViewMode.TopDown && wish.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(wish.normalized);

            bool grounded = controller.isGrounded;
            if (grounded && verticalVelocity < 0f)
                verticalVelocity = -2f;

            if (grounded && controls != null && controls.JumpPressed)
                verticalVelocity = Mathf.Sqrt(jumpHeight * 2f * gravity);

            verticalVelocity -= gravity * Time.deltaTime;
            controller.Move((wish + Vector3.up * verticalVelocity) * Time.deltaTime);
        }

        public void Crouch()
        {
            wantsCrouch = true;
        }

        public void Stand()
        {
            wantsCrouch = false;
        }

        public void ToggleCrouch()
        {
            if (wantsCrouch)
                Stand();
            else
                Crouch();
        }

        void PreparePhysics()
        {
            Rigidbody own = GetComponent<Rigidbody>();
            if (own != null)
            {
                own.detectCollisions = false;
                own.isKinematic = true;
                own.useGravity = false;
                own.interpolation = RigidbodyInterpolation.None;
                if (Application.isPlaying)
                    Destroy(own);
                else
                    DestroyImmediate(own);
            }

            Transform existing = transform.Find("Sensor");
            GameObject sensorObject = existing != null ? existing.gameObject : new GameObject("Sensor");
            if (existing == null)
                sensorObject.transform.SetParent(transform, false);

            sensor = sensorObject.GetComponent<CapsuleCollider>();
            if (sensor == null)
                sensor = sensorObject.AddComponent<CapsuleCollider>();

            sensor.isTrigger = true;
            sensor.direction = 1;

            Rigidbody body = sensorObject.GetComponent<Rigidbody>();
            if (body == null)
                body = sensorObject.AddComponent<Rigidbody>();

            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.None;
            body.constraints = RigidbodyConstraints.FreezeRotation;
        }

        void ApplyHeight(float height)
        {
            controller.height = height;
            controller.center = new Vector3(0f, height * 0.5f, 0f);
            if (controller.stepOffset > controller.radius)
                controller.stepOffset = controller.radius * 0.8f;
            if (controller.stepOffset > height)
                controller.stepOffset = height * 0.3f;

            if (sensor != null)
            {
                sensor.height = height;
                sensor.radius = controller.radius;
                sensor.center = controller.center;
            }

            if (bodyVisual == null)
                return;

            float radius = controller.radius;
            bodyVisual.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            bodyVisual.localPosition = new Vector3(0f, height * 0.5f, 0f);
        }

        bool HasHeadroom()
        {
            float radius = controller.radius * 0.9f;
            if (standingHeight <= radius * 2f)
                return true;

            Vector3 bottom = transform.position + Vector3.up * (radius + 0.05f);
            Vector3 top = transform.position + Vector3.up * (standingHeight - radius);
            Collider[] hits = Physics.OverlapCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                Transform hit = hits[i].transform;
                if (hit == transform || hit.IsChildOf(transform))
                    continue;

                return false;
            }

            return true;
        }

        Vector3 FlatForward()
        {
            if (view != null)
                return view.PlanarForward;

            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                return Vector3.forward;

            return forward.normalized;
        }
    }
}
