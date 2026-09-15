using UnityEngine;
using UnityEngine.UI;

namespace CookedOut.Presentation
{
    public sealed class DeliveryHudController : MonoBehaviour
    {
        private KitchenInputRouter _input;
        private DeliveryBikeController _bike;
        private GameObject _kitchenControls;
        private GameObject _deliveryControls;
        private Text _speedText;
        private Text _navigationText;
        private RectTransform _mapBikeMarker;
        private RectTransform _mapTargetMarker;
        private bool _wasRiding;

        public bool IsDeliveryVisible => _deliveryControls != null && _deliveryControls.activeSelf;
        public bool AreKitchenControlsVisible => _kitchenControls != null && _kitchenControls.activeSelf;
        public string SpeedLabel => _speedText == null ? string.Empty : _speedText.text;
        public string NavigationLabel => _navigationText == null ? string.Empty : _navigationText.text;
        public Vector2 MapBikePosition => _mapBikeMarker == null ? Vector2.zero : _mapBikeMarker.anchoredPosition;
        public Vector2 MapTargetPosition => _mapTargetMarker == null ? Vector2.zero : _mapTargetMarker.anchoredPosition;

        public void Initialize(
            KitchenInputRouter input,
            DeliveryBikeController bike,
            GameObject kitchenControls,
            GameObject deliveryControls,
            Text speedText,
            Text navigationText,
            RectTransform mapBikeMarker,
            RectTransform mapTargetMarker)
        {
            _input = input;
            _bike = bike;
            _kitchenControls = kitchenControls;
            _deliveryControls = deliveryControls;
            _speedText = speedText;
            _navigationText = navigationText;
            _mapBikeMarker = mapBikeMarker;
            _mapTargetMarker = mapTargetMarker;
            _wasRiding = false;
            RefreshNow();
        }

        private void Update()
        {
            RefreshNow();
        }

        public void RefreshNow()
        {
            var riding = _bike != null && _bike.IsMounted;
            if (_kitchenControls != null)
            {
                _kitchenControls.SetActive(!riding);
            }

            if (_deliveryControls != null)
            {
                _deliveryControls.SetActive(riding);
            }

            if (!riding)
            {
                if (_wasRiding)
                {
                    _input?.ClearTouchBikeControls();
                }

                _wasRiding = false;
                return;
            }

            _wasRiding = true;
            if (_speedText != null)
            {
                _speedText.text = Mathf.RoundToInt(Mathf.Abs(_bike.CurrentSpeed) * 3.6f) + " km/h";
            }

            if (_navigationText == null)
            {
                return;
            }

            var target = _bike.Destination;
            if (target == null)
            {
                _navigationText.text = "✓";
                return;
            }

            var toTarget = target.position - _bike.transform.position;
            toTarget.y = 0f;
            var distance = toTarget.magnitude;
            var direction = DirectionLabel(toTarget);
            _navigationText.text = Mathf.CeilToInt(distance) + " m\n" + direction;
            UpdateMap(target.position);
        }

        private void UpdateMap(Vector3 targetPosition)
        {
            if (_mapBikeMarker == null || _mapTargetMarker == null || _bike.DeliveryPoint == null)
            {
                return;
            }

            _mapBikeMarker.anchoredPosition = MapPosition(_bike.transform.position);
            _mapTargetMarker.anchoredPosition = MapPosition(targetPosition);
            var targetImage = _mapTargetMarker.GetComponent<Image>();
            if (targetImage != null)
            {
                targetImage.color = _bike.CurrentPhase == DeliveryRidePhase.Returning
                    ? new Color(1f, 0.72f, 0.12f)
                    : new Color(0.2f, 0.9f, 0.4f);
            }
        }

        private Vector2 MapPosition(Vector3 worldPosition)
        {
            var start = _bike.StartPosition;
            var route = _bike.DeliveryPoint.position - start;
            route.y = 0f;
            var length = Mathf.Max(0.01f, route.magnitude);
            var forward = route / length;
            var right = Vector3.Cross(Vector3.up, forward);
            var offset = worldPosition - start;
            offset.y = 0f;
            var progress = Mathf.Clamp01(Vector3.Dot(offset, forward) / length);
            var lateral = Mathf.Clamp(Vector3.Dot(offset, right), -3f, 3f) / 3f;
            return new Vector2(lateral * 72f, Mathf.Lerp(-70f, 48f, progress));
        }

        private string DirectionLabel(Vector3 toTarget)
        {
            if (toTarget.sqrMagnitude < 0.01f)
            {
                return "●";
            }

            var angle = Vector3.SignedAngle(_bike.transform.forward, toTarget, Vector3.up);
            if (Mathf.Abs(angle) > 135f)
            {
                return "↶";
            }

            if (angle > 18f)
            {
                return "→";
            }

            if (angle < -18f)
            {
                return "←";
            }

            return "↑";
        }
    }
}
