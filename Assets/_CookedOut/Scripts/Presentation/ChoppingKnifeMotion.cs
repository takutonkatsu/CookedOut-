using UnityEngine;

namespace CookedOut.Presentation
{
    [DefaultExecutionOrder(240)]
    public sealed class ChoppingKnifeMotion : MonoBehaviour
    {
        private const float CutsPerSecond = 1f / InteractableStation.ChopIntervalSeconds;
        private const float MaximumLiftDegrees = 100f;
        private const float MaximumLiftMeters = 1.00f;
        private const float ForegroundOffsetMeters = 0.18f;
        private const float ActivityGraceSeconds = 0.10f;
        private const float RestReturnSpeed = 8f;

        private Transform _knifeRoot;
        private Transform _bladeDirectionMarker;
        private Vector3 _restLocalPosition;
        private Quaternion _restLocalRotation;
        private Vector3 _bladeDirectionInRoot;
        private Vector3 _rotationAxisInParent;
        private Vector3 _upInParent;
        private float _activityRemaining;
        private float _cycle;
        private bool _boardOccupied;

        public Transform KnifeRoot => _knifeRoot;
        public bool IsAnimating => _knifeRoot != null && _activityRemaining > 0f;
        public bool IsVisible => _knifeRoot != null && _knifeRoot.gameObject.activeSelf;
        public float CurrentLift01 { get; private set; }
        public float SecondsPerCut => 1f / CutsPerSecond;
        public float MaximumLiftDistance => MaximumLiftMeters;
        public float ActiveForegroundDistance => ForegroundOffsetMeters;
        public bool IsInScreenForeground { get; private set; }
        public Vector3 BladeDirectionWorld
        {
            get
            {
                if (_knifeRoot == null)
                {
                    return Vector3.zero;
                }

                // The knife also pitches upward during a cut. Facing is the
                // horizontal direction of the blade, independent of that lift.
                var direction = _knifeRoot.TransformDirection(_bladeDirectionInRoot);
                direction.y = 0f;
                return direction.sqrMagnitude <= 0.0001f ? Vector3.zero : direction.normalized;
            }
        }
        public Vector3 RestLocalPosition => _restLocalPosition;
        public Quaternion RestLocalRotation => _restLocalRotation;

        public void Initialize(Transform searchRoot)
        {
            _knifeRoot = FindNamedChild(searchRoot, "Knife Motion Root");
            if (_knifeRoot == null || _knifeRoot.parent == null)
            {
                enabled = false;
                return;
            }

            _restLocalPosition = _knifeRoot.localPosition;
            _restLocalRotation = _knifeRoot.localRotation;
            _bladeDirectionMarker = FindNamedChild(searchRoot, "Knife Blade Direction");
            var bladeDirection = _bladeDirectionMarker == null
                ? _knifeRoot.TransformDirection(Vector3.right).normalized
                : (_bladeDirectionMarker.position - _knifeRoot.position).normalized;
            _bladeDirectionInRoot = _knifeRoot.InverseTransformDirection(bladeDirection).normalized;
            var rotationAxisWorld = Vector3.Cross(bladeDirection, Vector3.up).normalized;
            _rotationAxisInParent = _knifeRoot.parent.InverseTransformDirection(rotationAxisWorld).normalized;
            _upInParent = _knifeRoot.parent.InverseTransformDirection(Vector3.up).normalized;
            enabled = _rotationAxisInParent.sqrMagnitude > 0.9f;
            ApplyPose(0f);
            RefreshVisibility();
        }

        public void SetBoardOccupied(bool occupied)
        {
            _boardOccupied = occupied;
            Stop();
            RefreshVisibility();
        }

        public void NotifyWorkAdvanced(Vector3 playerFacingWorld)
        {
            if (_knifeRoot == null)
            {
                return;
            }

            _knifeRoot.gameObject.SetActive(true);
            AlignToPlayer(playerFacingWorld);

            if (_activityRemaining <= 0f)
            {
                // Start beyond the perfectly-flat first sample so even a very small
                // first frame gives immediate visual confirmation.
                _cycle = 0.12f;
                CurrentLift01 = EvaluateLift(_cycle);
            }

            // Facing can change while WORK remains active, so apply the newly
            // aligned rest pose immediately instead of waiting for LateUpdate.
            _activityRemaining = ActivityGraceSeconds;
            ApplyPose(CurrentLift01);
            RefreshVisibility();
        }

        private void AlignToPlayer(Vector3 playerFacingWorld)
        {
            playerFacingWorld.y = 0f;
            if (playerFacingWorld.sqrMagnitude <= 0.0001f || _knifeRoot.parent == null)
            {
                return;
            }

            var desiredInParent = _knifeRoot.parent
                .InverseTransformDirection(playerFacingWorld.normalized);
            desiredInParent -= _upInParent * Vector3.Dot(desiredInParent, _upInParent);
            if (desiredInParent.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            desiredInParent.Normalize();
            var currentInParent = _restLocalRotation * _bladeDirectionInRoot;
            currentInParent -= _upInParent * Vector3.Dot(currentInParent, _upInParent);
            currentInParent.Normalize();

            var alignmentAngle = Vector3.SignedAngle(currentInParent, desiredInParent, _upInParent);
            var alignment = Quaternion.AngleAxis(alignmentAngle, _upInParent);
            var height = Vector3.Dot(_restLocalPosition, _upInParent);
            var horizontalOffset = _restLocalPosition - _upInParent * height;
            _restLocalPosition = alignment * horizontalOffset + _upInParent * height;
            _restLocalRotation = alignment * _restLocalRotation;
            _rotationAxisInParent = Vector3.Cross(desiredInParent, _upInParent).normalized;
        }

        public void Stop()
        {
            _activityRemaining = 0f;
            _cycle = 0f;
            CurrentLift01 = 0f;
            ApplyPose(0f);
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            if (_knifeRoot != null)
            {
                _knifeRoot.gameObject.SetActive(
                    !_boardOccupied || _activityRemaining > 0f || CurrentLift01 > 0f);
            }
        }

        private void LateUpdate()
        {
            if (_knifeRoot == null)
            {
                return;
            }

            if (_activityRemaining > 0f)
            {
                _activityRemaining = Mathf.Max(0f, _activityRemaining - Time.deltaTime);
                _cycle = Mathf.Repeat(_cycle + Time.deltaTime * CutsPerSecond, 1f);
                CurrentLift01 = EvaluateLift(_cycle);
            }
            else
            {
                CurrentLift01 = Mathf.MoveTowards(CurrentLift01, 0f, Time.deltaTime * RestReturnSpeed);
                if (CurrentLift01 <= 0f)
                {
                    _cycle = 0f;
                }
            }

            ApplyPose(CurrentLift01);
            RefreshVisibility();
        }

        private void OnDisable()
        {
            if (_knifeRoot != null)
            {
                _knifeRoot.localPosition = _restLocalPosition;
                _knifeRoot.localRotation = _restLocalRotation;
                IsInScreenForeground = false;
            }
        }

        private void ApplyPose(float lift01)
        {
            if (_knifeRoot == null)
            {
                return;
            }

            var clampedLift = Mathf.Clamp01(lift01);
            IsInScreenForeground = _activityRemaining > 0f || clampedLift > 0f;
            var foregroundOffset = Vector3.zero;
            var activeCamera = Camera.main;
            if (IsInScreenForeground && activeCamera != null && _knifeRoot.parent != null)
            {
                var directionToCamera = (activeCamera.transform.position - _knifeRoot.position).normalized;
                foregroundOffset = _knifeRoot.parent.InverseTransformDirection(directionToCamera) *
                                   ForegroundOffsetMeters;
            }

            _knifeRoot.localPosition = _restLocalPosition +
                                       _upInParent * (MaximumLiftMeters * clampedLift) +
                                       foregroundOffset;
            _knifeRoot.localRotation =
                Quaternion.AngleAxis(MaximumLiftDegrees * clampedLift, _rotationAxisInParent) *
                _restLocalRotation;
        }

        private static float EvaluateLift(float cycle)
        {
            // A readable rhythm from the fixed camera: deliberate lift, quick cut,
            // then a short contact pause before the next stroke.
            if (cycle < 0.34f)
            {
                return Mathf.SmoothStep(0f, 1f, cycle / 0.34f);
            }

            if (cycle < 0.56f)
            {
                return 1f - Mathf.SmoothStep(0f, 1f, (cycle - 0.34f) / 0.22f);
            }

            return 0f;
        }

        private static Transform FindNamedChild(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
