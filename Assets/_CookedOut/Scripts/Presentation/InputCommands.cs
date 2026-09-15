using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CookedOut.Presentation
{
    public enum BikeControl
    {
        Accelerator,
        Reverse,
        Left,
        Right
    }

    public sealed class KitchenCommandBuffer
    {
        private bool _pickupPlaceRequested;
        private bool _workStarted;
        private bool _dashRequested;
        private bool _throwRequested;

        public Vector2 Move { get; private set; }
        public bool WorkHeld { get; private set; }

        public void SetMove(Vector2 value)
        {
            Move = Vector2.ClampMagnitude(value, 1f);
        }

        public void RequestPickupPlace()
        {
            _pickupPlaceRequested = true;
        }

        public void SetWorkHeld(bool held)
        {
            if (held && !WorkHeld)
            {
                _workStarted = true;
            }

            WorkHeld = held;
        }

        public void RequestDash()
        {
            _dashRequested = true;
        }

        public void RequestThrow()
        {
            _throwRequested = true;
        }

        public bool ConsumePickupPlace()
        {
            var result = _pickupPlaceRequested;
            _pickupPlaceRequested = false;
            return result;
        }

        public bool ConsumeWorkStarted()
        {
            var result = _workStarted;
            _workStarted = false;
            return result;
        }

        public bool ConsumeDash()
        {
            var result = _dashRequested;
            _dashRequested = false;
            return result;
        }

        public bool ConsumeThrow()
        {
            var result = _throwRequested;
            _throwRequested = false;
            return result;
        }
    }

    [DefaultExecutionOrder(-100)]
    public sealed class KitchenInputRouter : MonoBehaviour
    {
        private Vector2 _touchMove;
        private bool _touchMoveActive;
        private bool _touchWorkHeld;
        private bool _touchAcceleratorHeld;
        private bool _touchReverseHeld;
        private bool _touchLeftHeld;
        private bool _touchRightHeld;

        public KitchenCommandBuffer Commands { get; } = new KitchenCommandBuffer();

        private void Update()
        {
            var keyboardMove = ReadKeyboardMove();
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.eKey.wasPressedThisFrame)
                {
                    Commands.RequestPickupPlace();
                }

                if (keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame)
                {
                    Commands.RequestDash();
                }

                if (keyboard.qKey.wasPressedThisFrame)
                {
                    Commands.RequestThrow();
                }
            }

            Commands.SetWorkHeld(_touchWorkHeld ||
                                 (keyboard != null && (keyboard.spaceKey.isPressed || keyboard.fKey.isPressed)));
            Commands.SetMove(HasTouchBikeInput()
                ? ReadTouchBikeInput()
                : _touchMoveActive ? _touchMove : keyboardMove);
        }

        public void SetTouchMove(Vector2 value)
        {
            if (value.sqrMagnitude <= 0.0001f)
            {
                EndTouchMove();
                return;
            }

            BeginTouchMove();
            UpdateTouchMove(value);
        }

        public void BeginTouchMove()
        {
            _touchMoveActive = true;
            _touchMove = Vector2.zero;
            Commands.SetMove(Vector2.zero);
        }

        public void UpdateTouchMove(Vector2 value)
        {
            if (!_touchMoveActive)
            {
                return;
            }

            _touchMove = Vector2.ClampMagnitude(value, 1f);
            Commands.SetMove(_touchMove);
        }

        public void EndTouchMove()
        {
            _touchMoveActive = false;
            _touchMove = Vector2.zero;
            Commands.SetMove(Vector2.zero);
        }

        public void ClearMovement()
        {
            _touchMoveActive = false;
            _touchMove = Vector2.zero;
            Commands.SetMove(Vector2.zero);
        }

        public void TouchPickupPlace()
        {
            Commands.RequestPickupPlace();
        }

        public void TouchWorkDown()
        {
            _touchWorkHeld = true;
            Commands.SetWorkHeld(true);
        }

        public void TouchWorkUp()
        {
            _touchWorkHeld = false;
            Commands.SetWorkHeld(false);
        }

        public void TouchDash()
        {
            Commands.RequestDash();
        }

        public void TouchThrow()
        {
            Commands.RequestThrow();
        }

        public void SetTouchBikeControl(BikeControl control, bool held)
        {
            switch (control)
            {
                case BikeControl.Accelerator:
                    _touchAcceleratorHeld = held;
                    break;
                case BikeControl.Reverse:
                    _touchReverseHeld = held;
                    break;
                case BikeControl.Left:
                    _touchLeftHeld = held;
                    break;
                case BikeControl.Right:
                    _touchRightHeld = held;
                    break;
            }

            Commands.SetMove(HasTouchBikeInput() ? ReadTouchBikeInput() : Vector2.zero);
        }

        public void ClearTouchBikeControls()
        {
            _touchAcceleratorHeld = false;
            _touchReverseHeld = false;
            _touchLeftHeld = false;
            _touchRightHeld = false;
            Commands.SetMove(Vector2.zero);
        }

        private void OnDisable()
        {
            ClearMovement();
            ClearTouchBikeControls();
            TouchWorkUp();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                ClearMovement();
                ClearTouchBikeControls();
                TouchWorkUp();
            }
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused)
            {
                ClearMovement();
                ClearTouchBikeControls();
                TouchWorkUp();
            }
        }

        private static float ReadAxis(bool negative, bool positive)
        {
            return (positive ? 1f : 0f) - (negative ? 1f : 0f);
        }

        private static Vector2 ReadKeyboardMove()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            return new Vector2(
                ReadAxis(
                    keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed,
                    keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed),
                ReadAxis(
                    keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed,
                    keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed));
        }

        private bool HasTouchBikeInput()
        {
            return _touchAcceleratorHeld || _touchReverseHeld || _touchLeftHeld || _touchRightHeld;
        }

        private Vector2 ReadTouchBikeInput()
        {
            return new Vector2(
                ReadAxis(_touchLeftHeld, _touchRightHeld),
                ReadAxis(_touchReverseHeld, _touchAcceleratorHeld));
        }
    }

    public sealed class HoldWorkButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private KitchenInputRouter _input;

        public void Initialize(KitchenInputRouter input)
        {
            _input = input;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _input?.TouchWorkDown();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _input?.TouchWorkUp();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _input?.TouchWorkUp();
        }

        private void OnDisable()
        {
            _input?.TouchWorkUp();
        }
    }

    public sealed class HoldBikeControlButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private KitchenInputRouter _input;
        private BikeControl _control;

        public void Initialize(KitchenInputRouter input, BikeControl control)
        {
            _input = input;
            _control = control;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _input?.SetTouchBikeControl(_control, true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Release();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Release();
        }

        private void OnDisable()
        {
            Release();
        }

        private void Release()
        {
            _input?.SetTouchBikeControl(_control, false);
        }
    }
}
