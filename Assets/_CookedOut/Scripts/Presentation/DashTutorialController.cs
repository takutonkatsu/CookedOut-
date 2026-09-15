using CookedOut.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace CookedOut.Presentation
{
    public sealed class DashTutorialController : MonoBehaviour
    {
        private KitchenGameController _game;
        private PlayerController _player;
        private Text _guide;
        private Text _dashStatus;
        private ConveyorBeltController _conveyor;

        public bool IsCompleted => _game != null && _game.IsFinished;
        public bool HasCrossedConveyor { get; private set; }

        public void Initialize(
            KitchenGameController game,
            PlayerController player,
            KitchenGridRuntime grid,
            Text guide,
            Text dashStatus)
        {
            _game = game;
            _player = player;
            _guide = guide;
            _dashStatus = dashStatus;
            _conveyor = FindFirstObjectByType<ConveyorBeltController>();
            if (_guide != null)
            {
                _guide.text = "MAKE SALAD: chop lettuce and fill a container";
            }
        }

        private void Update()
        {
            if (_player == null || _game == null)
            {
                return;
            }

            if (_dashStatus != null)
            {
                _dashStatus.text = _player.IsDashing
                    ? "DASHING - KEEP TAPPING"
                    : "DASH READY - NO RECHARGE";
                _dashStatus.color = _player.IsDashing
                    ? new Color(1f, 0.75f, 0.25f)
                    : new Color(0.35f, 0.92f, 0.88f);
            }

            if (_game.IsFinished)
            {
                if (_guide != null)
                {
                    _guide.text = "SERVICE COMPLETE - DASH DELIVERY SUCCESS";
                }

                return;
            }

            if (_conveyor != null && _player.transform.position.x > _conveyor.WorldBounds.max.x + 0.1f)
            {
                HasCrossedConveyor = true;
            }

            if (_guide == null)
            {
                return;
            }

            var held = _player.HeldItem;
            var carryingCompletedMeal = held != null && held.IsContainer && held.Container.IsComplete;
            _guide.text = !carryingCompletedMeal
                ? "MAKE SALAD: chop lettuce and fill a container"
                : HasCrossedConveyor
                    ? "SERVE THE SALAD at the hatch"
                    : "CARRY THE SALAD: tap DASH repeatedly through the reverse conveyor";
        }
    }
}
