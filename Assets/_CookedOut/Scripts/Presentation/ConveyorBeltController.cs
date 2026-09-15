using UnityEngine;

namespace CookedOut.Presentation
{
    [DefaultExecutionOrder(-50)]
    public sealed class ConveyorBeltController : MonoBehaviour
    {
        public const float ReverseSpeed = 4.8f;
        private PlayerController _player;
        private Bounds _worldBounds;
        private Vector3 _flowDirection;

        public bool IsPlayerOnBelt { get; private set; }
        public Vector3 FlowVelocity => _flowDirection * ReverseSpeed;
        public Bounds WorldBounds => _worldBounds;

        public void Initialize(PlayerController player, Bounds worldBounds, Vector3 flowDirection)
        {
            _player = player;
            _worldBounds = worldBounds;
            flowDirection.y = 0f;
            _flowDirection = flowDirection.sqrMagnitude > 0.0001f
                ? flowDirection.normalized
                : Vector3.left;
        }

        private void Update()
        {
            IsPlayerOnBelt = _player != null && _worldBounds.Contains(_player.transform.position);
            if (IsPlayerOnBelt)
            {
                _player.ApplyExternalVelocity(FlowVelocity);
            }
        }
    }
}
