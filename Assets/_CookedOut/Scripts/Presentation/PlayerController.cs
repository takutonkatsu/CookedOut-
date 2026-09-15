using System.Linq;
using CookedOut.Domain;
using UnityEngine;

namespace CookedOut.Presentation
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        private const float InteractionRange = 2.35f;
        public const float ThrowDistance = 4.8f;
        public const float ThrowAssistRadius = 0.9f;
        public const float WalkSpeed = 4.45f;
        public const float DashSpeed = 7.5f;
        private const float DashDuration = 0.28f;
        private const float CollisionSlowDuration = 0.18f;
        private const float CollisionSpeedMultiplier = 0.45f;
        private const float MaximumMovementFrameTime = 1f / 30f;
        private KitchenInputRouter _input;
        private KitchenGameController _game;
        private CharacterController _characterController;
        private Transform _movementCamera;
        private Transform _heldAnchor;
        private KitchenGridRuntime _grid;
        private GameObject _heldVisual;
        private InteractableStation _selected;
        private DeliveryBikeController _mountedBike;
        private float _dashRemaining;
        private float _collisionSlowRemaining;
        private Vector3 _dashDirection;
        private Vector3 _horizontalVelocity;
        private Vector3 _externalVelocity;

        public WorldItem HeldItem { get; private set; }
        public Vector3 HorizontalVelocity => _horizontalVelocity;
        public Vector3 PhysicsPosition => transform.position;
        public Transform HeldAnchor => _heldAnchor;
        public Transform HeldVisual => _heldVisual == null ? null : _heldVisual.transform;
        public bool IsRidingBike => _mountedBike != null;
        public bool IsDashing => _dashRemaining > 0f;
        public bool IsPerformingWork { get; private set; }
        public int DashStartedCount { get; private set; }

        public void Initialize(
            KitchenInputRouter input,
            KitchenGameController game,
            Transform heldAnchor,
            KitchenGridRuntime grid)
        {
            _input = input;
            _game = game;
            _heldAnchor = heldAnchor;
            _grid = grid;
            _characterController = GetComponent<CharacterController>();
            _movementCamera = Camera.main == null ? null : Camera.main.transform;
        }

        private void LateUpdate()
        {
            IsPerformingWork = false;
            if (_input == null || _game == null || _game.IsFinished)
            {
                StopMovement();
                return;
            }

            var frameTime = Mathf.Min(Time.deltaTime, MaximumMovementFrameTime);
            if (_mountedBike != null)
            {
                UpdateBikeRide(frameTime);
                return;
            }

            _collisionSlowRemaining = Mathf.Max(0f, _collisionSlowRemaining - Time.deltaTime);
            if (_input.Commands.ConsumeDash())
            {
                var moveInput = Vector2.ClampMagnitude(_input.Commands.Move, 1f);
                _dashDirection = InputToWorldDirection(moveInput);
                if (_dashDirection.sqrMagnitude <= 0.0001f)
                {
                    _dashDirection = transform.forward;
                    _dashDirection.y = 0f;
                    _dashDirection.Normalize();
                }

                _dashRemaining = DashDuration;
                DashStartedCount++;
            }

            UpdateMovement(frameTime);

            if (_input.Commands.ConsumeThrow())
            {
                if (TryThrowHeld(out var throwReason))
                {
                    _game.SetDebugMessage("Item thrown");
                }
                else
                {
                    _game.SetDebugMessage(throwReason);
                }
            }

            _dashRemaining = Mathf.Max(0f, _dashRemaining - Time.deltaTime);

            SelectFacingStation();
            if (_input.Commands.ConsumePickupPlace())
            {
                if (_selected == null)
                {
                    _game.SetDebugMessage("No pickup/place target in front");
                }
                else
                {
                    _selected.PickupOrPlace(this);
                }
            }

            var workStarted = _input.Commands.ConsumeWorkStarted();
            if (_input.Commands.WorkHeld)
            {
                if (_horizontalVelocity.sqrMagnitude > 0.0001f)
                {
                    if (workStarted)
                    {
                        _game.SetDebugMessage("Stop moving to work");
                    }
                }
                else if (_selected == null)
                {
                    if (workStarted)
                    {
                        _game.SetDebugMessage("No work station in front");
                    }
                }
                else if (_selected.Work(this, frameTime, out var workReason))
                {
                    IsPerformingWork = true;
                }
                else if (workStarted)
                {
                    _game.SetDebugMessage(workReason);
                }
            }
        }

        private void UpdateMovement(float frameTime)
        {
            if (_characterController == null || !_characterController.enabled)
            {
                StopMovement();
                return;
            }

            var moveInput = Vector2.ClampMagnitude(_input.Commands.Move, 1f);
            var dashing = _dashRemaining > 0f;
            var speed = dashing ? DashSpeed : WalkSpeed;
            if (!dashing && _collisionSlowRemaining > 0f)
            {
                speed *= CollisionSpeedMultiplier;
            }

            var worldDirection = dashing ? _dashDirection : InputToWorldDirection(moveInput);
            var locomotionVelocity = worldDirection * speed;
            _horizontalVelocity = locomotionVelocity + _externalVelocity;

            if (locomotionVelocity.sqrMagnitude > 0.0001f)
            {
                var targetRotation = Quaternion.LookRotation(locomotionVelocity, Vector3.up);
                // Facing is gameplay state. Apply it before movement so interaction,
                // collision, and the held-item socket all use the newest input frame.
                transform.rotation = targetRotation;
            }

            if (_horizontalVelocity.sqrMagnitude > 0.0001f)
            {
                var collision = _characterController.Move(_horizontalVelocity * frameTime);
                if (dashing && (collision & CollisionFlags.Sides) != 0)
                {
                    _dashRemaining = 0f;
                    _collisionSlowRemaining = CollisionSlowDuration;
                    _horizontalVelocity *= CollisionSpeedMultiplier;
                }
            }

            _externalVelocity = Vector3.zero;
        }

        private Vector3 InputToWorldDirection(Vector2 input)
        {
            if (input.sqrMagnitude <= 0.0001f)
            {
                return Vector3.zero;
            }

            if (_movementCamera == null && Camera.main != null)
            {
                _movementCamera = Camera.main.transform;
            }

            var right = _movementCamera == null ? Vector3.right : _movementCamera.right;
            var forward = _movementCamera == null ? Vector3.forward : _movementCamera.forward;
            right.y = 0f;
            forward.y = 0f;
            right.Normalize();
            forward.Normalize();
            return Vector3.ClampMagnitude(right * input.x + forward * input.y, 1f);
        }

        public void Teleport(Vector3 position)
        {
            if (_characterController == null)
            {
                transform.position = position;
                return;
            }

            var wasEnabled = _characterController.enabled;
            _characterController.enabled = false;
            transform.position = position;
            _characterController.enabled = wasEnabled;
            StopMovement();
            Physics.SyncTransforms();
        }

        private void StopMovement()
        {
            _horizontalVelocity = Vector3.zero;
            _externalVelocity = Vector3.zero;
        }

        public void ApplyExternalVelocity(Vector3 velocity)
        {
            velocity.y = 0f;
            _externalVelocity += velocity;
        }

        private void UpdateBikeRide(float frameTime)
        {
            var bike = _mountedBike;
            var rideInput = Vector2.ClampMagnitude(_input.Commands.Move, 1f);
            var accelerator = Mathf.Max(0f, rideInput.y);
            var reverse = Mathf.Max(0f, -rideInput.y);
            bike.AdvanceRide(accelerator, reverse, rideInput.x, frameTime);
            _horizontalVelocity = bike.transform.forward * bike.CurrentSpeed;
            if (_mountedBike != null)
            {
                SyncToBike(_mountedBike);
            }

            _input.Commands.ConsumeDash();
            _input.Commands.ConsumeThrow();
            _input.Commands.ConsumeWorkStarted();
            if (_input.Commands.ConsumePickupPlace() && _mountedBike != null)
            {
                _mountedBike.TryDismount(out var reason);
                _game.SetDebugMessage(reason);
            }
        }

        public bool TryBeginBikeRide(DeliveryBikeController bike, out string reason)
        {
            if (bike == null)
            {
                reason = "Bike is unavailable";
                return false;
            }

            if (HeldItem != null)
            {
                reason = "Load or put down the held item before riding";
                return false;
            }

            if (_mountedBike != null)
            {
                reason = "Already riding a bike";
                return false;
            }

            if (_selected != null)
            {
                _selected.SetSelected(false);
                _selected = null;
            }

            _mountedBike = bike;
            if (_characterController != null)
            {
                _characterController.enabled = false;
            }

            SyncToBike(bike);
            reason = string.Empty;
            return true;
        }

        public void EndBikeRide(Vector3 dismountPosition)
        {
            _mountedBike = null;
            transform.position = dismountPosition;
            if (_characterController != null)
            {
                _characterController.enabled = true;
            }

            StopMovement();
            Physics.SyncTransforms();
        }

        public void SyncToBike(DeliveryBikeController bike)
        {
            if (bike == null)
            {
                return;
            }

            transform.position = bike.RiderPosition;
            transform.rotation = bike.transform.rotation;
        }

        public bool TryGive(WorldItem item, out string reason)
        {
            if (HeldItem != null)
            {
                reason = "Hands are full";
                return false;
            }

            HeldItem = item;
            RefreshHeldVisual();
            reason = string.Empty;
            return true;
        }

        public WorldItem TakeHeld()
        {
            var item = HeldItem;
            HeldItem = null;
            RefreshHeldVisual();
            return item;
        }

        public void RefreshHeldVisual()
        {
            if (_heldVisual != null)
            {
                Destroy(_heldVisual);
            }

            if (HeldItem != null)
            {
                _heldVisual = WorldItemVisualFactory.Create(HeldItem, _heldAnchor);
            }
        }

        public bool TryThrowHeld(out string reason)
        {
            if (HeldItem == null)
            {
                reason = "Nothing to throw";
                return false;
            }

            if (HeldItem.IsPot || HeldItem.IsPan)
            {
                reason = "Cookware cannot be thrown";
                return false;
            }

            var naturalLanding = transform.position + transform.forward * ThrowDistance;
            naturalLanding.y = 0.02f;
            var target = FindThrowAssistTarget(HeldItem, naturalLanding);
            Vector3? outOfBoundsReturn = null;
            if (target != null && !target.TryReserveThrown(HeldItem, out _))
            {
                target = null;
            }

            Vector3 landing;
            if (target != null)
            {
                landing = target.ItemAnchorPosition;
            }
            else if (!TryResolveOutOfBoundsReturn(naturalLanding, out outOfBoundsReturn, out landing) &&
                     !TryResolveGroundLanding(naturalLanding, out landing))
            {
                reason = "No safe landing point ahead";
                return false;
            }

            var start = _heldAnchor.position;
            var item = TakeHeld();
            ThrownItemMotion.Launch(item, start, landing, target, outOfBoundsReturn, _game);
            reason = string.Empty;
            return true;
        }

        private bool TryResolveOutOfBoundsReturn(
            Vector3 naturalLanding,
            out Vector3? returnPoint,
            out Vector3 landing)
        {
            returnPoint = null;
            landing = naturalLanding;
            if (_grid == null || _grid.Definition.IsInBounds(_grid.WorldToCell(naturalLanding)))
            {
                return false;
            }

            const int samples = 30;
            var lastSafePoint = transform.position;
            lastSafePoint.y = 0.02f;
            for (var sample = 1; sample <= samples; sample++)
            {
                var distance = ThrowDistance * sample / samples;
                var candidate = transform.position + transform.forward * distance;
                var cell = _grid.WorldToCell(candidate);
                if (!_grid.Definition.IsInBounds(cell))
                {
                    landing = new Vector3(candidate.x, 0.02f, candidate.z);
                    returnPoint = lastSafePoint;
                    return true;
                }

                var terrain = _grid.Definition.TerrainAt(cell);
                if (terrain == GridTerrain.Wall || terrain == GridTerrain.Void)
                {
                    returnPoint = null;
                    return false;
                }

                lastSafePoint = new Vector3(candidate.x, 0.02f, candidate.z);
            }

            returnPoint = null;
            return false;
        }

        private InteractableStation FindThrowAssistTarget(WorldItem item, Vector3 naturalLanding)
        {
            InteractableStation best = null;
            var bestLandingDistance = float.PositiveInfinity;
            foreach (var station in FindObjectsByType<InteractableStation>(FindObjectsSortMode.None))
            {
                if (!station.CanReceiveThrown(item, out _))
                {
                    continue;
                }

                var offset = station.ItemAnchorPosition - transform.position;
                offset.y = 0f;
                var distance = offset.magnitude;
                if (distance > ThrowDistance + ThrowAssistRadius)
                {
                    continue;
                }

                var landingOffset = station.ItemAnchorPosition - naturalLanding;
                landingOffset.y = 0f;
                var landingDistance = landingOffset.magnitude;
                if (landingDistance > ThrowAssistRadius)
                {
                    continue;
                }

                if (_grid != null && !_grid.Definition.IsThrowPathClear(
                        transform.position.x,
                        transform.position.z,
                        station.ItemAnchorPosition.x,
                        station.ItemAnchorPosition.z,
                        out _))
                {
                    continue;
                }

                if (landingDistance < bestLandingDistance)
                {
                    best = station;
                    bestLandingDistance = landingDistance;
                }
            }

            return best;
        }

        private bool TryResolveGroundLanding(Vector3 naturalLanding, out Vector3 landing)
        {
            if (_grid == null)
            {
                landing = naturalLanding;
                return true;
            }

            const int samples = 30;
            for (var sample = samples; sample >= 1; sample--)
            {
                var distance = ThrowDistance * sample / samples;
                var candidate = transform.position + transform.forward * distance;
                var cell = _grid.WorldToCell(candidate);
                if (!_grid.Definition.IsWalkable(cell))
                {
                    continue;
                }

                if (!_grid.Definition.IsThrowPathClear(
                        transform.position.x,
                        transform.position.z,
                        candidate.x,
                        candidate.z,
                        out _))
                {
                    continue;
                }

                landing = new Vector3(candidate.x, 0.02f, candidate.z);
                return true;
            }

            landing = default;
            return false;
        }

        private void SelectFacingStation()
        {
            InteractableStation best = null;
            var bestScore = float.NegativeInfinity;
            foreach (var station in FindObjectsByType<InteractableStation>(FindObjectsSortMode.None))
            {
                var priorityDirectUse = station.CanPrioritizeDirectUse(HeldItem);
                if (_grid != null && station.GridPlacement != null &&
                    !priorityDirectUse &&
                    !_grid.Definition.IsAdjacent(_grid.WorldToCell(transform.position), station.GridPlacement))
                {
                    continue;
                }

                var offset = station.transform.position - transform.position;
                offset.y = 0f;
                var distance = offset.magnitude;
                if (distance > InteractionRange || distance < 0.01f)
                {
                    continue;
                }

                var facing = Vector3.Dot(transform.forward, offset.normalized);
                if (!priorityDirectUse && facing < 0.15f)
                {
                    continue;
                }

                // A valid completed meal should select the nearby bike even from a
                // diagonal cell or after the player turned slightly on touch input.
                var score = (priorityDirectUse ? 4f : facing * 2f) - distance * 0.2f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = station;
                }
            }

            if (_selected == best)
            {
                return;
            }

            if (_selected != null)
            {
                _selected.SetSelected(false);
            }

            _selected = best;
            if (_selected != null)
            {
                _selected.SetSelected(true);
            }
        }
    }
}
