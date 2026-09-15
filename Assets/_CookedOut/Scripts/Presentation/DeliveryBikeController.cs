using CookedOut.Application;
using CookedOut.Domain;
using UnityEngine;

namespace CookedOut.Presentation
{
    public enum DeliveryRidePhase
    {
        Parked,
        Delivering,
        Returning,
        Completed
    }

    [RequireComponent(typeof(CharacterController))]
    public sealed class DeliveryBikeController : MonoBehaviour
    {
        public const float RideSpeed = 6.4f;
        public const float ReverseSpeed = 3.2f;
        public const float Acceleration = 7.5f;
        public const float ReverseAcceleration = 6f;
        public const float BrakeDeceleration = 13f;
        public const float CoastingDeceleration = 1.8f;
        public const float SteeringDegreesPerSecond = 105f;
        private const float DeliveryRadius = 1.9f;
        private const float DockRadius = 1.4f;
        private KitchenGameController _game;
        private Transform _destination;
        private Transform _returnPoint;
        private CharacterController _controller;
        private PlayerController _rider;
        private WorldItem _cargo;
        private ServeResult _deliveryResult;
        private Transform _cargoAnchor;
        private GameObject _cargoVisual;
        private DeliveryCameraController _cameraController;
        private float _kitchenBoundaryZ;

        public bool HasCargo => _cargo != null;
        public bool IsMounted => _rider != null;
        public WorldItem Cargo => _cargo;
        public Vector3 RiderPosition => transform.position + Vector3.up * 0.05f;
        public Transform Destination => CurrentPhase == DeliveryRidePhase.Returning ? _returnPoint : _destination;
        public Transform DeliveryPoint => _destination;
        public Transform ReturnPoint => _returnPoint;
        public Vector3 StartPosition { get; private set; }
        public Quaternion StartRotation { get; private set; }
        public DeliveryRidePhase CurrentPhase { get; private set; } = DeliveryRidePhase.Parked;
        public float CurrentSpeed { get; private set; }
        public float KitchenBoundaryZ => _kitchenBoundaryZ;

        public void Initialize(
            KitchenGameController game,
            Transform destination,
            Transform returnPoint,
            Vector3 startPosition,
            float kitchenBoundaryZ)
        {
            _game = game;
            _destination = destination;
            _returnPoint = returnPoint;
            StartPosition = startPosition;
            StartRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            _kitchenBoundaryZ = kitchenBoundaryZ;
            transform.SetPositionAndRotation(StartPosition, StartRotation);
            _controller = GetComponent<CharacterController>();
            _controller.radius = 0.48f;
            _controller.height = 1f;
            _controller.center = new Vector3(0f, 0.5f, 0f);
            _controller.skinWidth = 0.04f;
            _controller.stepOffset = 0f;
            _cameraController = Object.FindFirstObjectByType<DeliveryCameraController>();
            BuildVisual();
        }

        public bool CanLoad(WorldItem item, out string reason)
        {
            if (item == null || !item.IsContainer)
            {
                reason = "Only a completed delivery container can be loaded";
                return false;
            }

            if (_cargo != null)
            {
                reason = "Bike cargo rack is occupied";
                return false;
            }

            if (!item.Container.IsComplete ||
                !_game.Session.HasActiveOrderForRecipe(item.Container.CompletedRecipeId))
            {
                reason = "Complete the ordered meal before loading the bike";
                return false;
            }

            if (!_game.RequiredThrowsCompleted)
            {
                reason = "Throw every chopped ingredient into the pot before loading the bike";
                return false;
            }

            if (_game.Session.Stage.Id == GameIds.TutorialCombinedDeliveryStage &&
                _game.Session.State.ServedCount == 0)
            {
                reason = "最初の注文は配達代行へ渡してください";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public bool TryLoad(WorldItem item, out string reason)
        {
            if (!CanLoad(item, out reason))
            {
                return false;
            }

            _cargo = item;
            RefreshCargoVisual();
            return true;
        }

        public bool TryMount(PlayerController player, out string reason)
        {
            if (_cargo == null)
            {
                reason = "Load a completed container before riding";
                return false;
            }

            if (_rider != null)
            {
                reason = "Bike is already occupied";
                return false;
            }

            if (!player.TryBeginBikeRide(this, out reason))
            {
                return false;
            }

            _rider = player;
            CurrentSpeed = 0f;
            CurrentPhase = DeliveryRidePhase.Delivering;
            if (_destination != null)
            {
                _destination.gameObject.SetActive(true);
            }
            if (_returnPoint != null)
            {
                _returnPoint.gameObject.SetActive(false);
            }
            _cameraController?.FollowBike(transform);
            return true;
        }

        public void AdvanceRide(float accelerator, float reverse, float steering, float deltaSeconds)
        {
            if (_rider == null || _game == null || _game.IsFinished || deltaSeconds <= 0f)
            {
                return;
            }

            accelerator = Mathf.Clamp01(accelerator);
            reverse = Mathf.Clamp01(reverse);
            steering = Mathf.Clamp(steering, -1f, 1f);

            if (reverse > 0.001f)
            {
                var targetSpeed = CurrentSpeed > 0.001f ? 0f : -ReverseSpeed;
                var response = CurrentSpeed > 0.001f ? BrakeDeceleration : ReverseAcceleration;
                CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, targetSpeed, response * reverse * deltaSeconds);
            }
            else if (accelerator > 0.001f)
            {
                var targetSpeed = CurrentSpeed < -0.001f ? 0f : RideSpeed;
                var response = CurrentSpeed < -0.001f ? BrakeDeceleration : Acceleration;
                CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, targetSpeed, response * accelerator * deltaSeconds);
            }
            else
            {
                CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, 0f, CoastingDeceleration * deltaSeconds);
            }

            if (Mathf.Abs(CurrentSpeed) > 0.001f)
            {
                var speedRatio = Mathf.Clamp01(Mathf.Abs(CurrentSpeed) / RideSpeed);
                var steeringResponse = Mathf.Lerp(0.38f, 1f, speedRatio);
                transform.Rotate(
                    0f,
                    steering * SteeringDegreesPerSecond * steeringResponse * deltaSeconds,
                    0f,
                    Space.World);
                _controller.Move(transform.forward * CurrentSpeed * deltaSeconds);
                if (transform.position.z < _kitchenBoundaryZ)
                {
                    var clampedPosition = transform.position;
                    clampedPosition.z = _kitchenBoundaryZ;
                    transform.position = clampedPosition;
                    CurrentSpeed = 0f;
                    Physics.SyncTransforms();
                }
            }

            _rider.SyncToBike(this);

            if (CurrentPhase == DeliveryRidePhase.Delivering &&
                _destination != null &&
                Vector3.Distance(transform.position, _destination.position) <= DeliveryRadius)
            {
                CompleteDelivery();
            }
            else if (CurrentPhase == DeliveryRidePhase.Returning &&
                     _returnPoint != null &&
                     Vector3.Distance(transform.position, _returnPoint.position) <= DockRadius)
            {
                CompleteReturn();
            }
        }

        public bool TryDismount(out string reason)
        {
            if (_rider == null)
            {
                reason = "Bike is not mounted";
                return false;
            }

            if (_returnPoint == null || Vector3.Distance(transform.position, _returnPoint.position) > DockRadius)
            {
                reason = "Return to BIKE LOAD before dismounting";
                return false;
            }

            var rider = _rider;
            _rider = null;
            CurrentSpeed = 0f;
            rider.EndBikeRide(transform.position + Vector3.left * 1.15f);
            CurrentPhase = DeliveryRidePhase.Parked;
            _cameraController?.RestoreKitchenView();
            reason = "Bike dismounted";
            return true;
        }

        private void CompleteDelivery()
        {
            _deliveryResult = _game.Session.TryServe(
                _cargo == null ? null : _cargo.Container,
                FulfillmentMethod.SelfDelivery,
                1);
            if (!_deliveryResult.Succeeded)
            {
                _game.SetDebugMessage("Cannot deliver: " + _deliveryResult.Reason);
                _deliveryResult = null;
                return;
            }

            _cargo = null;
            RefreshCargoVisual();
            CurrentPhase = DeliveryRidePhase.Returning;
            if (_destination != null)
            {
                _destination.gameObject.SetActive(false);
            }

            if (_returnPoint != null)
            {
                _returnPoint.gameObject.SetActive(true);
            }

            _game.OnDeliveryDroppedOff(_deliveryResult);
        }

        private void CompleteReturn()
        {
            var rider = _rider;
            _rider = null;
            CurrentSpeed = 0f;
            CurrentPhase = DeliveryRidePhase.Parked;
            if (_returnPoint != null)
            {
                _returnPoint.gameObject.SetActive(false);
            }

            ResetToStartPose();
            rider?.EndBikeRide(StartPosition + Vector3.left * 1.15f);
            _cameraController?.RestoreKitchenView();
            _game.OnBikeReturned(_deliveryResult);
        }

        private void ResetToStartPose()
        {
            var controllerWasEnabled = _controller != null && _controller.enabled;
            if (controllerWasEnabled)
            {
                _controller.enabled = false;
            }

            transform.SetPositionAndRotation(StartPosition, StartRotation);

            if (controllerWasEnabled)
            {
                _controller.enabled = true;
            }

            Physics.SyncTransforms();
        }

        private void BuildVisual()
        {
            var visual = new GameObject("Bike Visual").transform;
            visual.SetParent(transform, false);
            CreatePart(PrimitiveType.Cube, visual, new Vector3(0f, 0.42f, 0f),
                new Vector3(0.24f, 0.22f, 1.25f), new Color(0.1f, 0.65f, 0.68f));
            CreatePart(PrimitiveType.Cylinder, visual, new Vector3(0f, 0.34f, -0.55f),
                new Vector3(0.42f, 0.09f, 0.42f), new Color(0.08f, 0.08f, 0.1f), new Vector3(0f, 0f, 90f));
            CreatePart(PrimitiveType.Cylinder, visual, new Vector3(0f, 0.34f, 0.55f),
                new Vector3(0.42f, 0.09f, 0.42f), new Color(0.08f, 0.08f, 0.1f), new Vector3(0f, 0f, 90f));
            CreatePart(PrimitiveType.Cube, visual, new Vector3(0f, 0.78f, -0.18f),
                new Vector3(0.52f, 0.12f, 0.42f), new Color(0.22f, 0.16f, 0.12f));
            CreatePart(PrimitiveType.Cube, visual, new Vector3(0f, 0.88f, 0.56f),
                new Vector3(0.72f, 0.07f, 0.08f), new Color(0.9f, 0.72f, 0.25f));

            _cargoAnchor = new GameObject("Bike Cargo Anchor").transform;
            _cargoAnchor.SetParent(visual, false);
            _cargoAnchor.localPosition = new Vector3(0f, 0.82f, -0.62f);
        }

        private void RefreshCargoVisual()
        {
            if (_cargoVisual != null)
            {
                Destroy(_cargoVisual);
            }

            if (_cargo != null)
            {
                _cargoVisual = WorldItemVisualFactory.Create(_cargo, _cargoAnchor);
                _cargoVisual.transform.localScale = Vector3.one * 0.7f;
            }
        }

        private static GameObject CreatePart(
            PrimitiveType primitive,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            Vector3? localEuler = null)
        {
            var part = GameObject.CreatePrimitive(primitive);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.transform.localEulerAngles = localEuler ?? Vector3.zero;
            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            part.GetComponent<Renderer>().material = GrayboxMaterials.Create(color);
            return part;
        }
    }
}
