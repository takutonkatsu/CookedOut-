using UnityEngine;

namespace CookedOut.Presentation
{
    [RequireComponent(typeof(Camera))]
    public sealed class DeliveryCameraController : MonoBehaviour
    {
        private const float FollowDistance = 7.2f;
        private const float FollowHeight = 5.4f;
        private const float LookHeight = 0.8f;
        private const float LookAheadDistance = 2.2f;
        private const float PositionSmoothTime = 0.34f;
        private const float TargetSmoothTime = 0.2f;
        private const float HeadingTurnSpeed = 105f;
        private Transform _bike;
        private Vector3 _kitchenPosition;
        private Quaternion _kitchenRotation;
        private Vector3 _followHeading = Vector3.forward;
        private Vector3 _smoothedLookTarget;
        private Vector3 _positionVelocity;
        private Vector3 _targetVelocity;

        public bool IsFollowingBike => _bike != null;
        public Vector3 KitchenPosition => _kitchenPosition;
        public Vector3 FollowHeading => _followHeading;

        public void CaptureKitchenView()
        {
            _kitchenPosition = transform.position;
            _kitchenRotation = transform.rotation;
        }

        public void FollowBike(Transform bike)
        {
            _bike = bike;
            _followHeading = FlattenDirection(bike == null ? Vector3.forward : bike.forward, Vector3.forward);
            _smoothedLookTarget = bike == null
                ? transform.position + transform.forward * FollowDistance
                : bike.position + _followHeading * LookAheadDistance + Vector3.up * LookHeight;
            _positionVelocity = Vector3.zero;
            _targetVelocity = Vector3.zero;
        }

        public void RestoreKitchenView()
        {
            _bike = null;
            _positionVelocity = Vector3.zero;
            _targetVelocity = Vector3.zero;
            transform.position = _kitchenPosition;
            transform.rotation = _kitchenRotation;
        }

        private void LateUpdate()
        {
            if (_bike == null)
            {
                return;
            }

            var desiredHeading = FlattenDirection(_bike.forward, _followHeading);
            _followHeading = Vector3.RotateTowards(
                _followHeading,
                desiredHeading,
                HeadingTurnSpeed * Mathf.Deg2Rad * Time.deltaTime,
                0f).normalized;

            var desiredPosition = _bike.position - _followHeading * FollowDistance + Vector3.up * FollowHeight;
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref _positionVelocity,
                PositionSmoothTime,
                Mathf.Infinity,
                Time.deltaTime);

            var desiredLookTarget = _bike.position +
                                    _followHeading * LookAheadDistance +
                                    Vector3.up * LookHeight;
            _smoothedLookTarget = Vector3.SmoothDamp(
                _smoothedLookTarget,
                desiredLookTarget,
                ref _targetVelocity,
                TargetSmoothTime,
                Mathf.Infinity,
                Time.deltaTime);
            var lookDirection = _smoothedLookTarget - transform.position;
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            }
        }

        private static Vector3 FlattenDirection(Vector3 direction, Vector3 fallback)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                return direction.normalized;
            }

            fallback.y = 0f;
            return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.forward;
        }
    }
}
