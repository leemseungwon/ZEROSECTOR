using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LSW._02._Code.Sos
{
    [CreateAssetMenu(fileName = "Input Data", menuName = "So/InputData", order = 0)]
    public class InputData : ScriptableObject, PlayerInputAction.IPlayerActions
    {
        private PlayerInputAction _playerInputActions;
        private bool _isInputEnabled = true;

        public Vector2 MousePosition { get; private set; }
        public float MoveInput { get; private set; }

        public event Action OnJumpPressed;
        public event Action OnAttackPressed;
        public event Action OnAttackReleased;
        public event Action OnDashPressed;
        public event Action OnDashReleased;
        public event Action OnReflectPressed;
        public event Action OnReflectReleased;
        public event Action OnSwitchPressed;
        public event Action OnGunPressed;

        private void OnEnable()
        {
            _isInputEnabled = true;
        }

        public void InitializeInput()
        {
            if (_playerInputActions == null)
                _playerInputActions = new PlayerInputAction();

            _playerInputActions.Player.SetCallbacks(this);
            _isInputEnabled = true;
            _playerInputActions.Enable();
        }

        public void EnableInput(bool enable)
        {
            _isInputEnabled = enable;

            if (!enable)
            {
                MoveInput = 0f;
                OnAttackReleased?.Invoke();
                OnDashReleased?.Invoke();
                OnReflectReleased?.Invoke();
            }
        }

        public void OnMousePos(InputAction.CallbackContext context)
        {
            MousePosition = context.ReadValue<Vector2>();
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            if (!_isInputEnabled)
            {
                MoveInput = 0f;
                return;
            }

            MoveInput = context.ReadValue<float>();
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (!_isInputEnabled)
                return;

            if (context.performed)
                OnJumpPressed?.Invoke();
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (!_isInputEnabled)
                return;

            if (context.performed)
                OnAttackPressed?.Invoke();

            if (context.canceled)
                OnAttackReleased?.Invoke();
        }

        public void OnDash(InputAction.CallbackContext context)
        {
            if (!_isInputEnabled)
                return;

            if (context.started)
                OnDashPressed?.Invoke();

            if (context.canceled)
                OnDashReleased?.Invoke();
        }

        public void OnReflect(InputAction.CallbackContext context)
        {
            if (!_isInputEnabled)
                return;

            if (context.started)
                OnReflectPressed?.Invoke();

            if (context.canceled)
                OnReflectReleased?.Invoke();
        }

        public void OnUseGun(InputAction.CallbackContext context)
        {
            if (_isInputEnabled && context.started)
            {
                OnSwitchPressed?.Invoke();
                OnGunPressed?.Invoke();
            }
        }

        private void OnDisable()
        {
            if (_playerInputActions == null)
                return;

            _playerInputActions.Player.RemoveCallbacks(this);
            _playerInputActions.Disable();
        }
    }
}
