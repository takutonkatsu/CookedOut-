using System.Linq;
using CookedOut.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace CookedOut.Presentation
{
    public enum ThrowDeliveryGuideStep
    {
        PrepareCarrot,
        ThrowCarrot,
        PrepareOnion,
        ThrowOnion,
        HeatSoup,
        FillContainer,
        LoadBike,
        MountBike,
        Deliver,
        Return,
        Complete
    }

    public sealed class TutorialGuideController : MonoBehaviour
    {
        private KitchenGameController _game;
        private PlayerController _player;
        private DeliveryBikeController _bike;
        private Text _guideText;
        private InteractableStation[] _stations;
        private InteractableStation _guidedStation;

        public ThrowDeliveryGuideStep CurrentStep { get; private set; }

        public void Initialize(
            KitchenGameController game,
            PlayerController player,
            DeliveryBikeController bike,
            Text guideText)
        {
            _game = game;
            _player = player;
            _bike = bike;
            _guideText = guideText;
            _stations = FindObjectsByType<InteractableStation>(FindObjectsSortMode.None);
            RefreshGuide();
        }

        private void Update()
        {
            if (_game == null || _game.Session == null)
            {
                return;
            }

            RefreshGuide();
        }

        private void OnDisable()
        {
            SetGuidedStation(null);
        }

        private void RefreshGuide()
        {
            var heat = Station(StationKind.PotHeatSource);
            var pot = heat == null || heat.SlottedItem == null ? null : heat.SlottedItem.Pot;
            CurrentStep = ResolveStep(pot);

            switch (CurrentStep)
            {
                case ThrowDeliveryGuideStep.PrepareCarrot:
                    Show("1 / 10  TAKE CARROT and hold WORK at CHOP", PreparationTarget(GameIds.CarrotIngredient));
                    break;
                case ThrowDeliveryGuideStep.ThrowCarrot:
                    Show("2 / 10  THROW chopped CARROT over the counter into POT", heat);
                    break;
                case ThrowDeliveryGuideStep.PrepareOnion:
                    Show("3 / 10  TAKE ONION and hold WORK at CHOP", PreparationTarget(GameIds.OnionIngredient));
                    break;
                case ThrowDeliveryGuideStep.ThrowOnion:
                    Show("4 / 10  THROW chopped ONION over the counter into POT", heat);
                    break;
                case ThrowDeliveryGuideStep.HeatSoup:
                    Show("5 / 10  WAIT for the POT to finish heating", heat);
                    break;
                case ThrowDeliveryGuideStep.FillContainer:
                    Show("6 / 10  TAKE CONTAINER, then PICK/PLACE at the cooked POT",
                        _player.HeldItem != null && _player.HeldItem.IsContainer
                            ? heat
                            : Station(StationKind.ContainerDispenser));
                    break;
                case ThrowDeliveryGuideStep.LoadBike:
                    Show("7 / 10  Carry the completed container to BIKE LOAD", Station(StationKind.BikeDock));
                    break;
                case ThrowDeliveryGuideStep.MountBike:
                    Show("8 / 10  PICK/PLACE at BIKE LOAD again to ride", Station(StationKind.BikeDock));
                    break;
                case ThrowDeliveryGuideStep.Deliver:
                    Show("9 / 10  W/UP accelerate, S/DOWN reverse, LEFT/RIGHT steer to DELIVERY", Station(StationKind.DeliveryPoint));
                    break;
                case ThrowDeliveryGuideStep.Return:
                    Show("10 / 10  Ride back to the yellow RETURN marker", null);
                    break;
                default:
                    Show("TUTORIAL COMPLETE  Multiplayer and beginner outings unlocked", null);
                    break;
            }
        }

        private ThrowDeliveryGuideStep ResolveStep(CookingPot pot)
        {
            if (_bike.CurrentPhase == DeliveryRidePhase.Completed || _game.IsFinished)
            {
                return ThrowDeliveryGuideStep.Complete;
            }

            if (_bike.CurrentPhase == DeliveryRidePhase.Returning)
            {
                return ThrowDeliveryGuideStep.Return;
            }

            if (_bike.CurrentPhase == DeliveryRidePhase.Delivering)
            {
                return ThrowDeliveryGuideStep.Deliver;
            }

            if (_bike.HasCargo)
            {
                return ThrowDeliveryGuideStep.MountBike;
            }

            if (HasCompletedContainer())
            {
                return ThrowDeliveryGuideStep.LoadBike;
            }

            if (pot != null && pot.IsCooked)
            {
                return ThrowDeliveryGuideStep.FillContainer;
            }

            if (pot != null && pot.Contents.Count >= 2)
            {
                return ThrowDeliveryGuideStep.HeatSoup;
            }

            if (PotContains(pot, GameIds.CarrotIngredient))
            {
                return HasChoppedIngredient(GameIds.OnionIngredient)
                    ? ThrowDeliveryGuideStep.ThrowOnion
                    : ThrowDeliveryGuideStep.PrepareOnion;
            }

            return HasChoppedIngredient(GameIds.CarrotIngredient)
                ? ThrowDeliveryGuideStep.ThrowCarrot
                : ThrowDeliveryGuideStep.PrepareCarrot;
        }

        private InteractableStation PreparationTarget(string ingredientId)
        {
            var held = _player.HeldItem;
            if (held != null && held.IsIngredient && held.Ingredient.IngredientId == ingredientId)
            {
                return held.Ingredient.Preparation == IngredientPreparation.Chopped
                    ? Station(StationKind.PotHeatSource)
                    : Station(StationKind.ChoppingBoard);
            }

            var board = Station(StationKind.ChoppingBoard);
            if (board != null && board.SlottedItem != null && board.SlottedItem.IsIngredient &&
                board.SlottedItem.Ingredient.IngredientId == ingredientId)
            {
                return board;
            }

            return _stations.FirstOrDefault(station => station.Kind == StationKind.IngredientCrate &&
                                                       station.SourceIngredientId == ingredientId);
        }

        private bool HasChoppedIngredient(string ingredientId)
        {
            if (IsChopped(_player.HeldItem, ingredientId))
            {
                return true;
            }

            return _stations.Any(station => IsChopped(station.SlottedItem, ingredientId));
        }

        private bool HasCompletedContainer()
        {
            if (IsCompletedContainer(_player.HeldItem))
            {
                return true;
            }

            return _stations.Any(station => IsCompletedContainer(station.SlottedItem));
        }

        private static bool PotContains(CookingPot pot, string ingredientId)
        {
            return pot != null && pot.Contents.Any(item => item.IngredientId == ingredientId);
        }

        private static bool IsChopped(WorldItem item, string ingredientId)
        {
            return item != null && item.IsIngredient && item.Ingredient.IngredientId == ingredientId &&
                   item.Ingredient.Preparation == IngredientPreparation.Chopped;
        }

        private static bool IsCompletedContainer(WorldItem item)
        {
            return item != null && item.IsContainer && item.Container.IsComplete;
        }

        private InteractableStation Station(StationKind kind)
        {
            return _stations.FirstOrDefault(station => station.Kind == kind);
        }

        private void Show(string message, InteractableStation station)
        {
            if (_guideText != null)
            {
                _guideText.text = message;
            }

            SetGuidedStation(station);
        }

        private void SetGuidedStation(InteractableStation station)
        {
            if (_guidedStation == station)
            {
                return;
            }

            _guidedStation?.SetGuided(false);
            _guidedStation = station;
            _guidedStation?.SetGuided(true);
        }
    }
}
