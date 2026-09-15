using UnityEngine;
using UnityEngine.EventSystems;

namespace CookedOut.Presentation
{
    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler,
        IEndDragHandler
    {
        private const float DeadZone = 0.12f;
        private KitchenInputRouter _input;
        private RectTransform _baseRect;
        private RectTransform _knobRect;
        private float _radius;
        private bool _isDragging;
        private int _pointerId = int.MinValue;

        public void Initialize(KitchenInputRouter input, RectTransform baseRect, RectTransform knobRect)
        {
            _input = input;
            _baseRect = baseRect;
            _knobRect = knobRect;
            _radius = Mathf.Min(baseRect.rect.width, baseRect.rect.height) * 0.32f;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_isDragging && eventData.pointerId != _pointerId)
            {
                return;
            }

            _isDragging = true;
            _pointerId = eventData.pointerId;
            _input.BeginTouchMove();
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || eventData.pointerId != _pointerId)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _baseRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var localPoint))
            {
                return;
            }

            var offsetFromCenter = localPoint - _baseRect.rect.center;
            var raw = Vector2.ClampMagnitude(offsetFromCenter / _radius, 1f);
            _knobRect.anchoredPosition = raw * _radius;
            if (raw.magnitude <= DeadZone)
            {
                _input.UpdateTouchMove(Vector2.zero);
                return;
            }

            var magnitude = Mathf.InverseLerp(DeadZone, 1f, raw.magnitude);
            _input.UpdateTouchMove(raw.normalized * magnitude);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == _pointerId)
            {
                ResetJoystick();
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == _pointerId)
            {
                ResetJoystick();
            }
        }

        private void OnDisable()
        {
            ResetJoystick();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                ResetJoystick();
            }
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused)
            {
                ResetJoystick();
            }
        }

        private void ResetJoystick()
        {
            _isDragging = false;
            _pointerId = int.MinValue;
            if (_knobRect != null)
            {
                _knobRect.anchoredPosition = Vector2.zero;
            }

            if (_input != null)
            {
                _input.EndTouchMove();
            }
        }
    }
}
