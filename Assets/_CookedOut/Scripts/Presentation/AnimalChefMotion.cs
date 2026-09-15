using UnityEngine;

namespace CookedOut.Presentation
{
    public enum AnimalChefPose
    {
        Idle,
        Walk,
        Carry,
        Work,
        Ride
    }

    [DefaultExecutionOrder(200)]
    public sealed class AnimalChefMotion : MonoBehaviour
    {
        private PlayerController _player;
        private KitchenInputRouter _input;
        private AnimalChefVisual _visual;
        private Vector3 _motionRestPosition;
        private Quaternion _motionRestRotation;
        private Quaternion _headRestRotation;
        private Quaternion _capRestRotation;
        private Quaternion _leftArmRestRotation;
        private Quaternion _rightArmRestRotation;
        private Quaternion _leftFootRestRotation;
        private Quaternion _rightFootRestRotation;
        private Quaternion _leftBrowRestRotation;
        private Quaternion _rightBrowRestRotation;
        private float _phase;

        public AnimalChefPose CurrentPose { get; private set; } = AnimalChefPose.Idle;

        public void Initialize(PlayerController player, KitchenInputRouter input, AnimalChefVisual visual)
        {
            _player = player;
            _input = input;
            _visual = visual;
            _motionRestPosition = visual.MotionRoot.localPosition;
            _motionRestRotation = visual.MotionRoot.localRotation;
            _headRestRotation = visual.SpeciesHeadRoot.localRotation;
            _capRestRotation = visual.CapRoot.localRotation;
            _leftArmRestRotation = visual.LeftArmPivot.localRotation;
            _rightArmRestRotation = visual.RightArmPivot.localRotation;
            _leftFootRestRotation = visual.LeftFootPivot.localRotation;
            _rightFootRestRotation = visual.RightFootPivot.localRotation;
            _leftBrowRestRotation = visual.LeftBrow.localRotation;
            _rightBrowRestRotation = visual.RightBrow.localRotation;
        }

        private void LateUpdate()
        {
            if (_player == null || _input == null || _visual == null)
            {
                return;
            }

            CurrentPose = ResolvePose();
            var moving = _player.HorizontalVelocity.sqrMagnitude > 0.01f;
            var carrying = _player.HeldItem != null;
            _phase += Time.deltaTime * (moving ? 9f : CurrentPose == AnimalChefPose.Work ? 15f : 3f);
            var wave = Mathf.Sin(_phase);
            var step = Mathf.Sin(_phase * 1.1f);

            var bob = 0.012f * wave;
            var bodyRoll = 1.2f * wave;
            var headPitch = 0f;
            var capRoll = 1.5f * wave;
            var leftArmPitch = 0f;
            var rightArmPitch = 0f;
            var leftFootPitch = 0f;
            var rightFootPitch = 0f;
            var browAngle = 0f;

            switch (CurrentPose)
            {
                case AnimalChefPose.Walk:
                    bob = Mathf.Abs(step) * 0.055f;
                    bodyRoll = -step * 3.5f;
                    headPitch = -Mathf.Abs(step) * 2f;
                    capRoll = step * 4f;
                    leftFootPitch = step * 24f;
                    rightFootPitch = -step * 24f;
                    if (carrying)
                    {
                        leftArmPitch = -52f;
                        rightArmPitch = -52f;
                    }
                    else
                    {
                        leftArmPitch = -step * 22f;
                        rightArmPitch = step * 22f;
                    }
                    break;
                case AnimalChefPose.Carry:
                    bob = 0.016f * wave;
                    headPitch = 3f;
                    leftArmPitch = -52f;
                    rightArmPitch = -52f;
                    break;
                case AnimalChefPose.Work:
                    bob = Mathf.Abs(wave) * 0.018f;
                    bodyRoll = wave * 1.8f;
                    headPitch = 8f + Mathf.Abs(wave) * 4f;
                    capRoll = wave * 2.5f;
                    leftArmPitch = -58f + wave * 4f;
                    rightArmPitch = -42f - Mathf.Abs(wave) * 42f;
                    browAngle = 18f;
                    break;
                case AnimalChefPose.Ride:
                    headPitch = -5f;
                    leftArmPitch = -62f;
                    rightArmPitch = -62f;
                    leftFootPitch = -28f;
                    rightFootPitch = -28f;
                    break;
            }

            var blend = Mathf.Clamp01(Time.deltaTime * 12f);
            _visual.MotionRoot.localPosition = Vector3.Lerp(
                _visual.MotionRoot.localPosition,
                _motionRestPosition + Vector3.up * bob,
                blend);
            _visual.MotionRoot.localRotation = Quaternion.Slerp(
                _visual.MotionRoot.localRotation,
                _motionRestRotation * Quaternion.Euler(0f, 0f, bodyRoll),
                blend);
            _visual.SpeciesHeadRoot.localRotation = Quaternion.Slerp(
                _visual.SpeciesHeadRoot.localRotation,
                _headRestRotation * Quaternion.Euler(headPitch, 0f, 0f),
                blend);
            _visual.CapRoot.localRotation = Quaternion.Slerp(
                _visual.CapRoot.localRotation,
                _capRestRotation * Quaternion.Euler(0f, 0f, capRoll),
                blend);
            _visual.LeftArmPivot.localRotation = Quaternion.Slerp(
                _visual.LeftArmPivot.localRotation,
                _leftArmRestRotation * Quaternion.Euler(leftArmPitch, 0f, 0f),
                blend);
            _visual.RightArmPivot.localRotation = Quaternion.Slerp(
                _visual.RightArmPivot.localRotation,
                _rightArmRestRotation * Quaternion.Euler(rightArmPitch, 0f, 0f),
                blend);
            _visual.LeftFootPivot.localRotation = Quaternion.Slerp(
                _visual.LeftFootPivot.localRotation,
                _leftFootRestRotation * Quaternion.Euler(leftFootPitch, 0f, 0f),
                blend);
            _visual.RightFootPivot.localRotation = Quaternion.Slerp(
                _visual.RightFootPivot.localRotation,
                _rightFootRestRotation * Quaternion.Euler(rightFootPitch, 0f, 0f),
                blend);
            _visual.LeftBrow.localRotation = Quaternion.Slerp(
                _visual.LeftBrow.localRotation,
                _leftBrowRestRotation * Quaternion.Euler(0f, 0f, -browAngle),
                blend);
            _visual.RightBrow.localRotation = Quaternion.Slerp(
                _visual.RightBrow.localRotation,
                _rightBrowRestRotation * Quaternion.Euler(0f, 0f, browAngle),
                blend);
        }

        private AnimalChefPose ResolvePose()
        {
            if (_player.IsRidingBike)
            {
                return AnimalChefPose.Ride;
            }

            if (_player.HorizontalVelocity.sqrMagnitude > 0.01f)
            {
                return AnimalChefPose.Walk;
            }

            if (_player.IsPerformingWork)
            {
                return AnimalChefPose.Work;
            }

            return _player.HeldItem == null ? AnimalChefPose.Idle : AnimalChefPose.Carry;
        }
    }
}
