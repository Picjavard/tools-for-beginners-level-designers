using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Arlycad
{
    /// <summary>
    /// Читает готовый файл действий. Студенты его не настраивают.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    [AddComponentMenu("Arlycad/Player Controls", 10)]
    public class PlayerControls : MonoBehaviour
    {
        [HideInInspector]
        public InputActionAsset actions;

        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool JumpPressed { get; private set; }

        public event Action CrouchPressed;

        InputActionMap map;
        bool warned;

        void Awake()
        {
            if (actions == null)
            {
                WarnMissing();
                return;
            }

            map = actions.FindActionMap("Player", true).Clone();
            map.FindAction("Crouch", true).performed += OnCrouchPerformed;
        }

        void OnEnable()
        {
            if (map != null)
                map.Enable();
        }

        void OnDisable()
        {
            if (map != null)
                map.Disable();
        }

        void OnDestroy()
        {
            if (map == null)
                return;

            map.FindAction("Crouch", true).performed -= OnCrouchPerformed;
            map.Dispose();
            map = null;
        }

        void Update()
        {
            if (map == null)
            {
                Move = Vector2.zero;
                Look = Vector2.zero;
                JumpPressed = false;
                return;
            }

            Move = map.FindAction("Move", true).ReadValue<Vector2>();
            Look = map.FindAction("Look", true).ReadValue<Vector2>();
            JumpPressed = map.FindAction("Jump", true).WasPressedThisFrame();
        }

        void OnCrouchPerformed(InputAction.CallbackContext context)
        {
            if (CrouchPressed != null)
                CrouchPressed();
        }

        void WarnMissing()
        {
            if (warned)
                return;

            warned = true;
            Debug.LogWarning("Player Controls: не назначен файл PlayerControls. Возьмите префаб Player из Assets/_Arlycad/Player.", this);
        }
    }
}
