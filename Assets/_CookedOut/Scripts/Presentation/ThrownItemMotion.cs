using UnityEngine;

namespace CookedOut.Presentation
{
    public sealed class ThrownItemMotion : MonoBehaviour
    {
        public const float FlightDuration = 0.44f;
        public const float ArcHeight = 0.82f;
        public const float YawDegreesPerSecond = 250f;
        public const float RollDegreesPerSecond = 95f;
        private const float RecoveryFallDuration = 0.7f;
        private WorldItem _item;
        private InteractableStation _target;
        private Vector3? _outOfBoundsReturn;
        private KitchenGameController _game;
        private Vector3 _start;
        private Vector3 _destination;
        private float _elapsed;
        private bool _fallingOutOfBounds;

        public Vector3 Destination => _destination;
        public InteractableStation AssistedTarget => _target;
        public Vector3? OutOfBoundsReturnPoint => _outOfBoundsReturn;
        public bool IsFallingOutOfBounds => _fallingOutOfBounds;

        public static void Launch(
            WorldItem item,
            Vector3 start,
            Vector3 destination,
            InteractableStation target,
            Vector3? outOfBoundsReturn,
            KitchenGameController game)
        {
            var visual = WorldItemVisualFactory.Create(item, null);
            visual.name = "Thrown Item";
            visual.transform.position = start;
            var motion = visual.AddComponent<ThrownItemMotion>();
            motion._item = item;
            motion._target = target;
            motion._outOfBoundsReturn = outOfBoundsReturn;
            motion._game = game;
            motion._start = start;
            motion._destination = destination;
        }

        private void Update()
        {
            if (_fallingOutOfBounds)
            {
                UpdateRecoveryFall();
                return;
            }

            _elapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(_elapsed / FlightDuration);
            transform.position = EvaluateTrajectory(_start, _destination, progress);
            transform.Rotate(0f, YawDegreesPerSecond * Time.deltaTime,
                RollDegreesPerSecond * Time.deltaTime, Space.World);
            if (progress < 1f)
            {
                return;
            }

            if (_target != null)
            {
                _target.CompleteThrownDelivery(_item);
            }
            else if (_outOfBoundsReturn.HasValue)
            {
                _fallingOutOfBounds = true;
                _elapsed = 0f;
                _start = transform.position;
                _game?.SetDebugMessage("Item fell out. Returning to the kitchen edge...");
                return;
            }
            else
            {
                InteractableStation.SpawnGroundItem(_game, _item, _destination);
                _game?.SetDebugMessage("Item landed on ground");
            }

            Destroy(gameObject);
        }

        public static Vector3 EvaluateTrajectory(Vector3 start, Vector3 destination, float normalizedTime)
        {
            var progress = Mathf.Clamp01(normalizedTime);
            var horizontal = Vector3.Lerp(start, destination, progress);
            var lowArc = 4f * ArcHeight * progress * (1f - progress);
            return horizontal + Vector3.up * lowArc;
        }

        private void UpdateRecoveryFall()
        {
            _elapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(_elapsed / RecoveryFallDuration);
            transform.position = _start + Vector3.down * (3.5f * progress * progress);
            transform.Rotate(0f, 720f * Time.deltaTime, 240f * Time.deltaTime, Space.World);
            if (progress < 1f)
            {
                return;
            }

            InteractableStation.SpawnGroundItem(_game, _item, _outOfBoundsReturn.Value);
            _game?.SetDebugMessage("Lost item returned to the kitchen edge");

            Destroy(gameObject);
        }
    }
}
