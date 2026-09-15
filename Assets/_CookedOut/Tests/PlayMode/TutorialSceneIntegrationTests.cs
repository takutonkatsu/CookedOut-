using System.Collections;
using System.Linq;
using CookedOut.Application;
using CookedOut.Domain;
using CookedOut.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CookedOut.Tests.PlayMode
{
    public sealed class TutorialSceneIntegrationTests
    {
        [UnityTest]
        public IEnumerator TutorialSceneCanCompleteAndServeOneOrder()
        {
            SceneManager.LoadScene("Tutorial_1_1");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var game = Object.FindFirstObjectByType<KitchenGameController>();
            Assert.That(game, Is.Not.Null);
            Assert.That(game.Session.State.Status, Is.EqualTo(ShiftStatus.Running));

            var player = Object.FindFirstObjectByType<PlayerController>();
            var stations = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None);
            var lettuceCrate = stations.Single(station => station.Kind == StationKind.IngredientCrate);
            var board = stations.Single(station => station.Kind == StationKind.ChoppingBoard);
            var containerDispenser = stations.Single(station => station.Kind == StationKind.ContainerDispenser);
            var assemblyCounter = stations
                .Where(station => station.Kind == StationKind.Counter)
                .OrderBy(station => station.transform.position.z)
                .First();
            var servingHatch = stations.Single(station => station.Kind == StationKind.ServingHatch);

            lettuceCrate.PickupOrPlace(player);
            board.PickupOrPlace(player);
            Assert.That(
                board.Work(player, IngredientItem.RequiredChopSeconds, out var workReason),
                Is.True,
                workReason);
            containerDispenser.PickupOrPlace(player);
            assemblyCounter.PickupOrPlace(player);
            board.PickupOrPlace(player);
            assemblyCounter.PickupOrPlace(player);
            assemblyCounter.PickupOrPlace(player);
            servingHatch.PickupOrPlace(player);
            yield return null;

            Assert.That(game.Session.State.ServedCount, Is.EqualTo(1));
            Assert.That(game.Session.State.Score, Is.GreaterThanOrEqualTo(100));
            Assert.That(game.Session.State.Stars, Is.Zero,
                "Stars are awarded when the shift timer ends, not after the first dish.");
            Assert.That(game.Session.State.Status, Is.EqualTo(ShiftStatus.Running));
            var ticket = Object.FindFirstObjectByType<OrderTicketView>();
            Assert.That(ticket.Urgency, Is.EqualTo(OrderTicketUrgency.Served));
            Assert.That(ticket.IsServedIconVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator HeldContainerCollectsChoppedLettuceDirectlyFromBoard()
        {
            SceneManager.LoadScene("Tutorial_1_1");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var player = Object.FindFirstObjectByType<PlayerController>();
            var stations = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None);
            var lettuceCrate = stations.Single(station => station.Kind == StationKind.IngredientCrate);
            var board = stations.Single(station => station.Kind == StationKind.ChoppingBoard);
            var containerDispenser = stations.Single(station => station.Kind == StationKind.ContainerDispenser);

            lettuceCrate.PickupOrPlace(player);
            board.PickupOrPlace(player);
            Assert.That(
                board.Work(player, IngredientItem.RequiredChopSeconds, out var workReason),
                Is.True,
                workReason);
            containerDispenser.PickupOrPlace(player);

            board.PickupOrPlace(player);

            Assert.That(board.SlottedItem, Is.Null);
            Assert.That(player.HeldItem, Is.Not.Null);
            Assert.That(player.HeldItem.IsContainer, Is.True);
            Assert.That(player.HeldItem.Container.IsComplete, Is.True);
            Assert.That(player.HeldItem.Container.CompletedRecipeId, Is.EqualTo(GameIds.LettuceSaladRecipe));
            Assert.That(player.HeldItem.Container.Components, Has.Count.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SoupTutorialCanCookPauseResumeFillAndServeOnePot()
        {
            SceneManager.LoadScene("Tutorial_1_2");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var game = Object.FindFirstObjectByType<KitchenGameController>();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var stations = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None);
            var carrotCrate = stations.Single(station => station.SourceIngredientId == GameIds.CarrotIngredient);
            var onionCrate = stations.Single(station => station.SourceIngredientId == GameIds.OnionIngredient);
            var board = stations.Single(station => station.Kind == StationKind.ChoppingBoard);
            var heat = stations.Single(station => station.Kind == StationKind.PotHeatSource);
            var restCounter = stations.Single(station => station.GridPlacement.Id == "pot.rest");
            var containerDispenser = stations.Single(station => station.Kind == StationKind.ContainerDispenser);
            var servingHatch = stations.Single(station => station.Kind == StationKind.ServingHatch);

            Assert.That(game.Session.Stage.Id, Is.EqualTo(GameIds.TutorialSoupStage));
            Assert.That(game.Session.Recipe.Id, Is.EqualTo(GameIds.VegetableSoupRecipe));
            Assert.That(heat.SlottedItem, Is.Not.Null);
            Assert.That(heat.SlottedItem.IsPot, Is.True);

            AddChoppedIngredientToPot(carrotCrate, board, heat, player);
            AddChoppedIngredientToPot(onionCrate, board, heat, player);
            Assert.That(heat.SlottedItem.Pot.Contents.Count, Is.EqualTo(2));

            Assert.That(
                heat.AdvanceHeating(CookingPot.RequiredHeatSeconds * 0.4f, out var heatReason),
                Is.True,
                heatReason);
            Assert.That(heat.HeatProgress, Is.EqualTo(0.4f).Within(0.001f));

            heat.PickupOrPlace(player);
            Assert.That(player.HeldItem.IsPot, Is.True);
            Assert.That(player.TryThrowHeld(out var throwReason), Is.False);
            Assert.That(throwReason, Does.Contain("cannot be thrown"));
            restCounter.PickupOrPlace(player);
            Assert.That(restCounter.SlottedItem.Pot.HeatProgress, Is.EqualTo(0.4f).Within(0.001f));
            restCounter.PickupOrPlace(player);
            heat.PickupOrPlace(player);

            Assert.That(
                heat.AdvanceHeating(CookingPot.RequiredHeatSeconds * 0.6f, out heatReason),
                Is.True,
                heatReason);
            Assert.That(heat.SlottedItem.Pot.IsCooked, Is.True);

            containerDispenser.PickupOrPlace(player);
            heat.PickupOrPlace(player);
            Assert.That(player.HeldItem.IsContainer, Is.True);
            Assert.That(player.HeldItem.Container.CompletedRecipeId, Is.EqualTo(GameIds.VegetableSoupRecipe));
            Assert.That(heat.SlottedItem.Pot.IsEmpty, Is.True);

            servingHatch.PickupOrPlace(player);
            Assert.That(game.Session.State.ServedCount, Is.EqualTo(1));
            Assert.That(game.Session.State.Status, Is.EqualTo(ShiftStatus.Running));
        }

        [UnityTest]
        public IEnumerator ThrowDeliveryTutorialLoadsMountsRidesAndCompletesDelivery()
        {
            PlayerPrefs.DeleteKey(PlayerPrefsSaveStore.KeyFor(TutorialProgression.SaveSlotId));
            SceneManager.LoadScene("Tutorial_1_3");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var game = Object.FindFirstObjectByType<KitchenGameController>();
            var grid = Object.FindFirstObjectByType<KitchenGridRuntime>();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var bike = Object.FindFirstObjectByType<DeliveryBikeController>();
            var deliveryCamera = Object.FindFirstObjectByType<DeliveryCameraController>();
            var deliveryHud = Object.FindFirstObjectByType<DeliveryHudController>();
            var input = Object.FindFirstObjectByType<KitchenInputRouter>();
            var guide = Object.FindFirstObjectByType<TutorialGuideController>();
            var stations = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None);
            var dock = stations.Single(station => station.Kind == StationKind.BikeDock);
            var heat = stations.Single(station => station.Kind == StationKind.PotHeatSource);

            Assert.That(game.Session.Stage.Id, Is.EqualTo(GameIds.TutorialThrowDeliveryStage));
            Assert.That(game.Session.Recipe.Id, Is.EqualTo(GameIds.VegetableSoupRecipe));
            Assert.That(bike, Is.Not.Null);
            Assert.That(bike.Destination, Is.Not.Null);
            Assert.That(bike.ReturnPoint, Is.Not.Null);
            Assert.That(deliveryCamera, Is.Not.Null);
            Assert.That(deliveryHud, Is.Not.Null);
            Assert.That(deliveryHud.IsDeliveryVisible, Is.False);
            Assert.That(deliveryHud.AreKitchenControlsVisible, Is.True);
            Assert.That(guide, Is.Not.Null);
            Assert.That(guide.CurrentStep, Is.EqualTo(ThrowDeliveryGuideStep.PrepareCarrot));
            Assert.That(game.RequiredThrowsCompleted, Is.False);
            Assert.That(GameObject.Find("Delivery Road"), Is.Not.Null);
            Assert.That(stations.Any(station => station.Kind == StationKind.RecoveryBin), Is.False);
            Assert.That(Vector2.Distance(
                    new Vector2(dock.transform.position.x, dock.transform.position.z),
                    new Vector2(bike.transform.position.x, bike.transform.position.z)),
                Is.LessThan(0.01f), "The bike itself must be the loading target at the boundary.");
            Assert.That(dock.transform.Find("Station Body").GetComponent<Collider>(), Is.Null,
                "The removed loading workbench must not leave an invisible collision block.");
            Assert.That(bike.transform.position.z, Is.EqualTo(bike.KitchenBoundaryZ).Within(0.01f));
            Assert.That(bike.Destination.position.z,
                Is.GreaterThan(grid.Definition.Space.OriginZ + grid.Definition.Depth * grid.Definition.Space.CellSize));

            var incomplete = WorldItem.FromContainer(game.Session.TakeDeliveryContainer());
            Assert.That(bike.CanLoad(incomplete, out var incompleteReason), Is.False);
            Assert.That(incompleteReason, Does.Contain("Complete"));

            var bypassPot = game.Session.TakeCookingPot();
            var bypassCarrot = game.Session.TakeRawIngredient(GameIds.CarrotIngredient);
            var bypassOnion = game.Session.TakeRawIngredient(GameIds.OnionIngredient);
            Assert.That(game.Session.TryChop(bypassCarrot, out var bypassReason), Is.True, bypassReason);
            Assert.That(game.Session.TryChop(bypassOnion, out bypassReason), Is.True, bypassReason);
            Assert.That(game.Session.TryAddToPot(bypassPot, bypassCarrot, out bypassReason), Is.True, bypassReason);
            Assert.That(game.Session.TryAddToPot(bypassPot, bypassOnion, out bypassReason), Is.True, bypassReason);
            Assert.That(game.Session.TryAdvancePot(
                bypassPot,
                CookingPot.RequiredHeatSeconds,
                out var bypassCooked,
                out bypassReason), Is.True, bypassReason);
            Assert.That(bypassCooked, Is.True);
            var bypassContainer = game.Session.TakeDeliveryContainer();
            Assert.That(game.Session.TryFillContainerFromPot(bypassPot, bypassContainer, out bypassReason),
                Is.True,
                bypassReason);
            Assert.That(bike.CanLoad(WorldItem.FromContainer(bypassContainer), out bypassReason), Is.False);
            Assert.That(bypassReason, Does.Contain("Throw"));

            player.Teleport(grid.CellCenter(new GridCoordinate(6, 4), 0.05f));
            player.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            var carrot = game.Session.TakeRawIngredient(GameIds.CarrotIngredient);
            Assert.That(game.Session.TryChop(carrot, out var cookReason), Is.True, cookReason);
            Assert.That(player.TryGive(WorldItem.FromIngredient(carrot), out var pickupReason),
                Is.True,
                pickupReason);
            Assert.That(player.TryThrowHeld(out var throwReason), Is.True, throwReason);
            var landingTimeout = 1.2f;
            while (heat.SlottedItem.Pot.Contents.Count < 1 && landingTimeout > 0f)
            {
                yield return null;
                landingTimeout -= Time.deltaTime;
            }
            Assert.That(heat.SlottedItem.Pot.Contents, Has.Count.EqualTo(1));
            Assert.That(game.SuccessfulPotThrowCount, Is.EqualTo(1));
            Assert.That(game.RequiredThrowsCompleted, Is.False);
            yield return null;
            Assert.That(guide.CurrentStep, Is.EqualTo(ThrowDeliveryGuideStep.PrepareOnion));

            var onion = game.Session.TakeRawIngredient(GameIds.OnionIngredient);
            Assert.That(game.Session.TryChop(onion, out cookReason), Is.True, cookReason);
            Assert.That(player.TryGive(WorldItem.FromIngredient(onion), out pickupReason),
                Is.True,
                pickupReason);
            Assert.That(player.TryThrowHeld(out throwReason), Is.True, throwReason);
            landingTimeout = 1.2f;
            while (heat.SlottedItem.Pot.Contents.Count < 2 && landingTimeout > 0f)
            {
                yield return null;
                landingTimeout -= Time.deltaTime;
            }
            Assert.That(heat.SlottedItem.Pot.Contents, Has.Count.EqualTo(2));
            Assert.That(game.SuccessfulPotThrowCount, Is.EqualTo(2));
            Assert.That(game.RequiredThrowsCompleted, Is.True);
            yield return null;
            Assert.That(guide.CurrentStep, Is.EqualTo(ThrowDeliveryGuideStep.HeatSoup));

            var pot = heat.SlottedItem.Pot;
            Assert.That(game.Session.TryAdvancePot(
                pot,
                CookingPot.RequiredHeatSeconds,
                out var cookingCompleted,
                out cookReason), Is.True, cookReason);
            Assert.That(cookingCompleted, Is.True);

            var completedContainer = game.Session.TakeDeliveryContainer();
            Assert.That(game.Session.TryFillContainerFromPot(pot, completedContainer, out var fillReason),
                Is.True,
                fillReason);
            Assert.That(player.TryGive(WorldItem.FromContainer(completedContainer), out var giveReason),
                Is.True,
                giveReason);

            player.Teleport(dock.transform.position + new Vector3(-1.55f, 0f, -1.55f));
            player.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
            input.TouchPickupPlace();
            yield return null;
            Assert.That(player.HeldItem, Is.Null);
            Assert.That(bike.HasCargo, Is.True);
            yield return null;
            Assert.That(guide.CurrentStep, Is.EqualTo(ThrowDeliveryGuideStep.MountBike));

            dock.PickupOrPlace(player);
            Assert.That(player.IsRidingBike, Is.True);
            Assert.That(bike.IsMounted, Is.True);
            Assert.That(bike.CurrentPhase, Is.EqualTo(DeliveryRidePhase.Delivering));
            Assert.That(game.IsClockPaused, Is.False,
                "The service clock must keep running for the whole delivery ride.");
            Assert.That(deliveryCamera.IsFollowingBike, Is.True);
            yield return null;
            Assert.That(guide.CurrentStep, Is.EqualTo(ThrowDeliveryGuideStep.Deliver));
            Assert.That(deliveryHud.IsDeliveryVisible, Is.True);
            Assert.That(deliveryHud.AreKitchenControlsVisible, Is.False);
            Assert.That(deliveryHud.NavigationLabel, Does.Not.Contain("DELIVERY"));
            Assert.That(deliveryHud.NavigationLabel, Does.Contain("m"));
            var initialMapBikePosition = deliveryHud.MapBikePosition;
            Assert.That(deliveryHud.MapTargetPosition.y, Is.GreaterThan(initialMapBikePosition.y));

            bike.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
            bike.AdvanceRide(1f, 0f, 0f, 0.5f);
            Assert.That(bike.transform.position.z, Is.GreaterThanOrEqualTo(bike.KitchenBoundaryZ - 0.001f),
                "A mounted bike must not cross from the street into the kitchen.");
            bike.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);

            input.SetTouchBikeControl(BikeControl.Accelerator, true);
            input.SetTouchBikeControl(BikeControl.Right, true);
            Assert.That(input.Commands.Move.x, Is.GreaterThan(0f));
            Assert.That(input.Commands.Move.y, Is.GreaterThan(0f));
            input.ClearTouchBikeControls();
            Assert.That(input.Commands.Move, Is.EqualTo(Vector2.zero));
            input.SetTouchBikeControl(BikeControl.Reverse, true);
            Assert.That(input.Commands.Move.y, Is.LessThan(0f));
            input.ClearTouchBikeControls();

            var initialBikeHeading = bike.transform.forward;
            bike.AdvanceRide(1f, 0f, 1f, 0.2f);
            deliveryHud.RefreshNow();
            var speedAfterAccelerating = bike.CurrentSpeed;
            var headingAfterRight = bike.transform.forward;
            Assert.That(speedAfterAccelerating, Is.GreaterThan(0f), "Accelerator input did not move the bike.");
            Assert.That(deliveryHud.SpeedLabel, Does.Contain("km/h"));
            Assert.That(deliveryHud.SpeedLabel, Does.Not.Contain("SPEED"));
            Assert.That(Vector2.Distance(deliveryHud.MapBikePosition, initialMapBikePosition), Is.GreaterThan(0f),
                "The minimap bike marker did not follow the bike.");
            Assert.That(Vector3.SignedAngle(initialBikeHeading, headingAfterRight, Vector3.up), Is.GreaterThan(0f),
                "Right steering input did not turn the bike right.");

            bike.AdvanceRide(1f, 0f, -1f, 0.2f);
            Assert.That(Vector3.SignedAngle(headingAfterRight, bike.transform.forward, Vector3.up), Is.LessThan(0f),
                "Left steering input did not turn the bike left.");
            var speedBeforeReverse = bike.CurrentSpeed;
            bike.AdvanceRide(0f, 1f, 0f, 1f);
            Assert.That(bike.CurrentSpeed, Is.LessThan(speedBeforeReverse),
                "Reverse input must first decelerate forward travel.");
            bike.AdvanceRide(0f, 1f, 0f, 0.25f);
            Assert.That(bike.CurrentSpeed, Is.LessThan(0f), "Held reverse input did not engage reverse travel.");
            Assert.That(bike.CurrentSpeed, Is.GreaterThanOrEqualTo(-DeliveryBikeController.ReverseSpeed));
            var positionBeforeReverse = bike.transform.position;
            var headingBeforeReverse = bike.transform.forward;
            bike.AdvanceRide(0f, 1f, 0f, 0.2f);
            Assert.That(Vector3.Dot(bike.transform.position - positionBeforeReverse, headingBeforeReverse), Is.LessThan(0f),
                "The bike did not move backward along its current heading.");

            var reverseButton = Object.FindObjectsByType<KitchenActionButtonView>(FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Single(view => view.ActionId == "REVERSE");
            Assert.That(reverseButton.Icon, Is.EqualTo(KitchenActionIcon.Reverse));
            var reverseIcon = reverseButton.transform.Find("Face/Button Content/Icon");
            Assert.That(reverseIcon, Is.Not.Null);
            Assert.That(reverseIcon.Find("Arrow Head"), Is.Not.Null,
                "Reverse must use the nonverbal down-arrow pictogram.");
            Assert.That(reverseIcon.GetComponentsInChildren<Text>(true), Is.Empty);

            var initialCameraHeading = deliveryCamera.FollowHeading;
            bike.transform.rotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
            yield return null;
            Assert.That(Vector3.Angle(deliveryCamera.FollowHeading, Vector3.right), Is.GreaterThan(20f),
                "The delivery camera snapped completely to a 90-degree bike turn.");
            Assert.That(Vector3.Angle(deliveryCamera.FollowHeading, initialCameraHeading), Is.GreaterThan(0.01f),
                "The delivery camera did not begin following the bike turn.");
            var deliveryStartShiftTime = game.Session.State.RemainingSeconds;
            var deliveryStartOrderTime = game.Session.State.Orders[0].RemainingSeconds;

            var timeout = 5f;
            while (bike.CurrentPhase == DeliveryRidePhase.Delivering && timeout > 0f)
            {
                var direction = bike.Destination.position - bike.transform.position;
                direction.y = 0f;
                bike.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                bike.AdvanceRide(1f, 0f, 0f, 0.1f);
                timeout -= 0.1f;
                yield return null;
            }

            Assert.That(timeout, Is.GreaterThan(0f), "The loaded bike did not reach the delivery point.");
            Assert.That(game.Session.State.ServedCount, Is.EqualTo(1));
            Assert.That(game.Session.State.RemainingSeconds, Is.LessThan(deliveryStartShiftTime - 0.001f));
            Assert.That(game.Session.State.Orders[0].RemainingSeconds, Is.LessThan(deliveryStartOrderTime - 0.001f));
            var commonDeliveryScore = game.Session.Stage.BaseScore +
                Mathf.FloorToInt(game.Session.State.Orders[0].RemainingSeconds) *
                game.Session.Stage.TipPerRemainingSecond;
            var expectedSelfDeliveryBonus = Mathf.FloorToInt(commonDeliveryScore * 0.5f);
            Assert.That(game.Session.State.Score, Is.EqualTo(commonDeliveryScore + expectedSelfDeliveryBonus));
            Assert.That(game.LastServeResult.DeliveryBonus, Is.EqualTo(expectedSelfDeliveryBonus));
            Assert.That(game.LastServeResult.TotalScore, Is.EqualTo(game.Session.State.Score));
            Assert.That(game.Session.State.Status, Is.EqualTo(ShiftStatus.Running));
            Assert.That(bike.HasCargo, Is.False);
            Assert.That(bike.IsMounted, Is.True);
            Assert.That(player.IsRidingBike, Is.True);
            Assert.That(bike.CurrentPhase, Is.EqualTo(DeliveryRidePhase.Returning));
            Assert.That(guide.CurrentStep, Is.EqualTo(ThrowDeliveryGuideStep.Return));
            Assert.That(Vector3.Distance(deliveryCamera.transform.position, deliveryCamera.KitchenPosition),
                Is.GreaterThan(0.001f),
                "The camera did not transition from the kitchen view to the bike follow view.");
            Assert.That(bike.TryDismount(out var dismountReason), Is.False);
            Assert.That(dismountReason, Does.Contain("BIKE LOAD"));
            Assert.That(game.Session.State.RemainingSeconds, Is.LessThan(deliveryStartShiftTime));
            Assert.That(game.Session.State.Orders[0].RemainingSeconds, Is.LessThan(deliveryStartOrderTime));

            timeout = 5f;
            while (bike.CurrentPhase == DeliveryRidePhase.Returning && timeout > 0f)
            {
                var direction = bike.Destination.position - bike.transform.position;
                direction.y = 0f;
                bike.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                bike.AdvanceRide(1f, 0f, 0f, 0.1f);
                timeout -= 0.1f;
                yield return null;
            }

            Assert.That(timeout, Is.GreaterThan(0f), "The bike did not return to the kitchen.");
            Assert.That(game.Session.State.Status, Is.EqualTo(ShiftStatus.Running));
            Assert.That(bike.CurrentPhase, Is.EqualTo(DeliveryRidePhase.Parked));
            Assert.That(bike.IsMounted, Is.False);
            Assert.That(Vector3.Distance(bike.transform.position, bike.StartPosition), Is.LessThan(0.001f),
                "The returned bike must snap back to its authored kitchen parking position.");
            Assert.That(Quaternion.Angle(bike.transform.rotation, bike.StartRotation), Is.LessThan(0.1f),
                "The returned bike must restore its authored kitchen parking direction.");
            Assert.That(player.IsRidingBike, Is.False);
            Assert.That(game.IsClockPaused, Is.False);
            Assert.That(deliveryCamera.IsFollowingBike, Is.False);
            Assert.That(deliveryHud.IsDeliveryVisible, Is.False);
            Assert.That(deliveryHud.AreKitchenControlsVisible, Is.True);
            Assert.That(deliveryCamera.transform.position, Is.EqualTo(deliveryCamera.KitchenPosition));
            Assert.That(game.Session.State.Score, Is.GreaterThanOrEqualTo(140));
            game.Session.Tick(game.Session.State.RemainingSeconds);
            yield return null;
            Assert.That(game.Session.State.Status, Is.EqualTo(ShiftStatus.Finished));
            Assert.That(game.Progression.StageThreeStars, Is.GreaterThanOrEqualTo(1));
            Assert.That(game.Progression.MultiplayerUnlocked, Is.True);
            Assert.That(game.Progression.BeginnerOutingsUnlocked, Is.True);
            PlayerPrefs.DeleteKey(PlayerPrefsSaveStore.KeyFor(TutorialProgression.SaveSlotId));
        }

        [UnityTest]
        public IEnumerator OutOfBoundsThrowReturnsToKitchenEdgeWithoutRecoveryWorkbench()
        {
            SceneManager.LoadScene("Tutorial_1_3");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var game = Object.FindFirstObjectByType<KitchenGameController>();
            var grid = Object.FindFirstObjectByType<KitchenGridRuntime>();
            var player = Object.FindFirstObjectByType<PlayerController>();
            Assert.That(Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None)
                .Any(station => station.Kind == StationKind.RecoveryBin), Is.False);
            var item = WorldItem.FromIngredient(game.Session.TakeRawIngredient(GameIds.CarrotIngredient));

            player.Teleport(grid.CellCenter(new GridCoordinate(9, 7), 0.05f));
            player.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            Assert.That(player.TryGive(item, out var giveReason), Is.True, giveReason);
            Assert.That(player.TryThrowHeld(out var throwReason), Is.True, throwReason);

            var motion = Object.FindFirstObjectByType<ThrownItemMotion>();
            Assert.That(motion, Is.Not.Null);
            Assert.That(motion.AssistedTarget, Is.Null);
            Assert.That(motion.OutOfBoundsReturnPoint.HasValue, Is.True);

            var timeout = 2f;
            InteractableStation groundItem = null;
            while (groundItem == null && timeout > 0f)
            {
                yield return null;
                timeout -= Time.deltaTime;
                groundItem = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None)
                    .SingleOrDefault(station => station.Kind == StationKind.GroundItem);
            }

            Assert.That(timeout, Is.GreaterThan(0f), "The lost item did not return to the kitchen edge.");
            Assert.That(groundItem, Is.Not.Null);
            Assert.That(groundItem.SlottedItem, Is.SameAs(item));
            Assert.That(grid.Definition.IsInBounds(grid.WorldToCell(groundItem.transform.position)), Is.True);

            player.Teleport(groundItem.transform.position - Vector3.forward * 0.8f);
            player.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            groundItem.PickupOrPlace(player);
            Assert.That(player.HeldItem, Is.SameAs(item));
        }

        [UnityTest]
        public IEnumerator DashTutorialRequiresCookedMealAndRepeatedDashesBeforeServing()
        {
            SceneManager.LoadScene("Tutorial_1_4");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var game = Object.FindFirstObjectByType<KitchenGameController>();
            var grid = Object.FindFirstObjectByType<KitchenGridRuntime>();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var input = Object.FindFirstObjectByType<KitchenInputRouter>();
            var tutorial = Object.FindFirstObjectByType<DashTutorialController>();
            var conveyor = Object.FindFirstObjectByType<ConveyorBeltController>();
            var stations = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None);
            var servingHatch = stations.Single(station => station.Kind == StationKind.ServingHatch);

            Assert.That(game.Session.Stage.Id, Is.EqualTo(GameIds.TutorialDashStage));
            Assert.That(game.Session.Recipe.Id, Is.EqualTo(GameIds.LettuceSaladRecipe));
            Assert.That(tutorial, Is.Not.Null);
            Assert.That(conveyor, Is.Not.Null);
            Assert.That(servingHatch, Is.Not.Null);
            Assert.That(ConveyorBeltController.ReverseSpeed, Is.GreaterThan(PlayerController.WalkSpeed));
            Assert.That(ConveyorBeltController.ReverseSpeed, Is.LessThan(PlayerController.DashSpeed));

            servingHatch.PickupOrPlace(player);
            Assert.That(game.Session.State.Status, Is.EqualTo(ShiftStatus.Running),
                "Reaching the serving side without cooking must not clear the tutorial.");

            player.Teleport(conveyor.WorldBounds.center + Vector3.down * 0.65f);
            input.SetTouchMove(Vector2.right);
            var walkingStartX = player.transform.position.x;
            for (var frame = 0; frame < 20; frame++)
            {
                yield return null;
            }

            Assert.That(player.transform.position.x, Is.LessThanOrEqualTo(walkingStartX + 0.03f),
                "Walking alone advanced against a faster reverse conveyor.");

            var lettuce = game.Session.TakeRawLettuce();
            Assert.That(game.Session.TryChop(lettuce, out var cookReason), Is.True, cookReason);
            var container = game.Session.TakeDeliveryContainer();
            Assert.That(game.Session.TryAssemble(container, lettuce, out cookReason), Is.True, cookReason);
            var carried = WorldItem.FromContainer(container);
            Assert.That(player.TryGive(carried, out var giveReason), Is.True, giveReason);

            player.Teleport(grid.CellCenter(new GridCoordinate(3, 2), 0.05f));
            var timeout = 8f;
            while (!tutorial.HasCrossedConveyor && timeout > 0f)
            {
                input.Commands.RequestDash();
                yield return null;
                timeout -= Time.deltaTime;
            }

            input.ClearMovement();
            Assert.That(timeout, Is.GreaterThan(0f), "Repeated dashes did not overcome the conveyor.");
            Assert.That(tutorial.HasCrossedConveyor, Is.True);
            Assert.That(game.Session.State.Status, Is.EqualTo(ShiftStatus.Running),
                "Crossing the conveyor alone must not clear the cooking tutorial.");
            Assert.That(player.HeldItem, Is.SameAs(carried), "Dashing must retain the cooked meal.");
            Assert.That(player.DashStartedCount, Is.GreaterThanOrEqualTo(2));

            servingHatch.PickupOrPlace(player);
            yield return null;

            Assert.That(tutorial.IsCompleted, Is.False,
                "Serving one order must not end a timed tutorial shift.");
            Assert.That(game.Session.State.Status, Is.EqualTo(ShiftStatus.Running));
            Assert.That(game.Session.State.ServedCount, Is.EqualTo(1));
            Assert.That(game.Session.State.Score, Is.GreaterThanOrEqualTo(100));
            Assert.That(player.HeldItem, Is.Null);
            game.Session.Tick(game.Session.State.RemainingSeconds);
            yield return null;
            Assert.That(tutorial.IsCompleted, Is.True);
            Assert.That(game.Session.State.Stars, Is.GreaterThanOrEqualTo(1));
        }

        [UnityTest]
        public IEnumerator FryingTutorialChopsCooksContainersAndServesHamburger()
        {
            SceneManager.LoadScene("Tutorial_1_5");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var game = Object.FindFirstObjectByType<KitchenGameController>();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var stations = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None);
            var beefCrate = stations.Single(station => station.SourceIngredientId == GameIds.BeefIngredient);
            var lettuceCrate = stations.Single(station => station.SourceIngredientId == GameIds.LettuceIngredient);
            var board = stations.Single(station => station.Kind == StationKind.ChoppingBoard);
            var panHeat = stations.Single(station => station.Kind == StationKind.PanHeatSource);
            var assembly = stations.Single(station => station.GridPlacement != null && station.GridPlacement.Id == "assembly");
            var containerDispenser = stations.Single(station => station.Kind == StationKind.ContainerDispenser);
            var servingHatch = stations.Single(station => station.Kind == StationKind.ServingHatch);

            Assert.That(game.Session.Stage.Id, Is.EqualTo(GameIds.TutorialFryingStage));
            Assert.That(game.Session.Recipe.Id, Is.EqualTo(GameIds.HamburgerPlateRecipe));
            Assert.That(panHeat.SlottedItem, Is.Not.Null);
            Assert.That(panHeat.SlottedItem.IsPan, Is.True);

            beefCrate.PickupOrPlace(player);
            panHeat.PickupOrPlace(player);
            Assert.That(player.HeldItem, Is.Not.Null, "Raw beef must not enter the frying pan.");
            board.PickupOrPlace(player);
            Assert.That(board.Work(player, IngredientItem.RequiredChopSeconds, out var reason), Is.True, reason);
            board.PickupOrPlace(player);
            panHeat.PickupOrPlace(player);
            Assert.That(player.HeldItem, Is.Null);
            Assert.That(panHeat.SlottedItem.Pan.IsEmpty, Is.False);
            Assert.That(panHeat.GetComponentsInChildren<Transform>(true)
                .Any(child => child.name == "Ingredient List Indicator ingredient.beef"), Is.True,
                string.Join(", ", panHeat.GetComponentsInChildren<Transform>(true).Select(child => child.name)));
            var panBeefSourceCard = panHeat.GetComponentsInChildren<Transform>(true)
                .Single(child => child.name == "Ingredient Source Card ingredient.beef");
            Assert.That(panBeefSourceCard.GetComponent<Renderer>().sharedMaterial.mainTexture.name,
                Is.EqualTo("ingredient_beef_source_card_sv2"));
            Assert.That(panHeat.GetComponentInChildren<FryingPanCookingMotion>(), Is.Not.Null,
                "Raw beef in the pan must visibly sizzle and steam while it cooks.");
            Assert.That(panHeat.GetComponentsInChildren<Transform>(true)
                .Count(child => child.name.StartsWith("Pan Steam Puff")), Is.EqualTo(3));

            Assert.That(panHeat.AdvancePanHeating(FryingPan.RequiredCookSeconds, out reason), Is.True, reason);
            Assert.That(panHeat.SlottedItem.Pan.IsCooked, Is.True);
            containerDispenser.PickupOrPlace(player);
            panHeat.PickupOrPlace(player);
            Assert.That(player.HeldItem.IsContainer, Is.True);
            Assert.That(player.HeldItem.Container.IsComplete, Is.False);
            assembly.PickupOrPlace(player);
            lettuceCrate.PickupOrPlace(player);
            board.PickupOrPlace(player);
            Assert.That(board.Work(player, IngredientItem.RequiredChopSeconds, out reason), Is.True, reason);
            board.PickupOrPlace(player);
            assembly.PickupOrPlace(player);
            Assert.That(player.HeldItem, Is.Null);
            assembly.PickupOrPlace(player);
            Assert.That(player.HeldItem.Container.CompletedRecipeId, Is.EqualTo(GameIds.HamburgerPlateRecipe));

            servingHatch.PickupOrPlace(player);
            yield return null;

            Assert.That(game.Session.State.ServedCount, Is.EqualTo(1));
            Assert.That(game.Session.State.Status, Is.EqualTo(ShiftStatus.Running));
            Assert.That(game.Session.State.Stars, Is.Zero);
        }

        [UnityTest]
        public IEnumerator CookedPotTransfersOnCounterInBothCursorDirections()
        {
            SceneManager.LoadScene("Tutorial_1_2");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var game = Object.FindFirstObjectByType<KitchenGameController>();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var counters = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None)
                .Where(station => station.Kind == StationKind.Counter)
                .Take(2)
                .ToArray();
            Assert.That(counters, Has.Length.EqualTo(2));

            CookingPot CookedPot()
            {
                var pot = game.Session.TakeCookingPot();
                Assert.That(game.Session.TryAddToPot(
                    pot,
                    new IngredientItem(GameIds.CarrotIngredient, IngredientPreparation.Chopped),
                    out var reason), Is.True, reason);
                Assert.That(game.Session.TryAddToPot(
                    pot,
                    new IngredientItem(GameIds.OnionIngredient, IngredientPreparation.Chopped),
                    out reason), Is.True, reason);
                Assert.That(game.Session.TryAdvancePot(
                    pot,
                    CookingPot.RequiredHeatSeconds,
                    out var completed,
                    out reason), Is.True, reason);
                Assert.That(completed, Is.True);
                return pot;
            }

            var firstPot = WorldItem.FromPot(CookedPot());
            Assert.That(player.TryGive(firstPot, out var pickupReason), Is.True, pickupReason);
            counters[0].PickupOrPlace(player);
            var heldContainer = WorldItem.FromContainer(game.Session.TakeDeliveryContainer());
            Assert.That(player.TryGive(heldContainer, out pickupReason), Is.True, pickupReason);
            counters[0].PickupOrPlace(player);
            Assert.That(player.HeldItem, Is.SameAs(heldContainer));
            Assert.That(heldContainer.Container.IsComplete, Is.True);
            Assert.That(firstPot.Pot.IsEmpty, Is.True);
            Assert.That(counters[0].SlottedItem, Is.SameAs(firstPot));

            player.TakeHeld();
            var slottedContainer = WorldItem.FromContainer(game.Session.TakeDeliveryContainer());
            Assert.That(player.TryGive(slottedContainer, out pickupReason), Is.True, pickupReason);
            counters[1].PickupOrPlace(player);
            var heldPot = WorldItem.FromPot(CookedPot());
            Assert.That(player.TryGive(heldPot, out pickupReason), Is.True, pickupReason);
            counters[1].PickupOrPlace(player);
            Assert.That(player.HeldItem, Is.SameAs(heldPot));
            Assert.That(heldPot.Pot.IsEmpty, Is.True);
            Assert.That(counters[1].SlottedItem, Is.SameAs(slottedContainer));
            Assert.That(slottedContainer.Container.IsComplete, Is.True);
        }

        [UnityTest]
        public IEnumerator CookedPanTransfersOnCounterInBothCursorDirections()
        {
            SceneManager.LoadScene("Tutorial_1_5");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var game = Object.FindFirstObjectByType<KitchenGameController>();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var counters = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None)
                .Where(station => station.Kind == StationKind.Counter)
                .Take(2)
                .ToArray();
            Assert.That(counters, Has.Length.EqualTo(2));

            FryingPan CookedPan()
            {
                var pan = game.Session.TakeFryingPan();
                Assert.That(game.Session.TryAddToPan(
                    pan,
                    new IngredientItem(GameIds.BeefIngredient, IngredientPreparation.Chopped),
                    out var reason), Is.True, reason);
                Assert.That(game.Session.TryAdvancePan(
                    pan,
                    FryingPan.RequiredCookSeconds,
                    out var completed,
                    out var burned,
                    out reason), Is.True, reason);
                Assert.That(completed, Is.True);
                Assert.That(burned, Is.False);
                return pan;
            }

            DeliveryContainer ContainerWithLettuce()
            {
                var container = game.Session.TakeDeliveryContainer();
                Assert.That(container.TryAdd(
                    new IngredientItem(GameIds.LettuceIngredient, IngredientPreparation.Chopped),
                    game.Session.Recipe,
                    out var reason), Is.True, reason);
                Assert.That(container.IsComplete, Is.False);
                return container;
            }

            var firstPan = WorldItem.FromPan(CookedPan());
            Assert.That(player.TryGive(firstPan, out var pickupReason), Is.True, pickupReason);
            counters[0].PickupOrPlace(player);
            var heldContainer = WorldItem.FromContainer(ContainerWithLettuce());
            Assert.That(player.TryGive(heldContainer, out pickupReason), Is.True, pickupReason);
            counters[0].PickupOrPlace(player);
            Assert.That(player.HeldItem, Is.SameAs(heldContainer));
            Assert.That(heldContainer.Container.IsComplete, Is.True);
            Assert.That(firstPan.Pan.IsEmpty, Is.True);
            Assert.That(counters[0].SlottedItem, Is.SameAs(firstPan));

            player.TakeHeld();
            var slottedContainer = WorldItem.FromContainer(ContainerWithLettuce());
            Assert.That(player.TryGive(slottedContainer, out pickupReason), Is.True, pickupReason);
            counters[1].PickupOrPlace(player);
            var heldPan = WorldItem.FromPan(CookedPan());
            Assert.That(player.TryGive(heldPan, out pickupReason), Is.True, pickupReason);
            counters[1].PickupOrPlace(player);
            Assert.That(player.HeldItem, Is.SameAs(heldPan));
            Assert.That(heldPan.Pan.IsEmpty, Is.True);
            Assert.That(counters[1].SlottedItem, Is.SameAs(slottedContainer));
            Assert.That(slottedContainer.Container.IsComplete, Is.True);
        }

        [UnityTest]
        public IEnumerator BurnedPattyMustBeCarriedInPanToTrashAndPanIsReused()
        {
            SceneManager.LoadScene("Tutorial_1_5");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var player = Object.FindFirstObjectByType<PlayerController>();
            var stations = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None);
            var beefCrate = stations.Single(station => station.SourceIngredientId == GameIds.BeefIngredient);
            var board = stations.Single(station => station.Kind == StationKind.ChoppingBoard);
            var panHeat = stations.Single(station => station.Kind == StationKind.PanHeatSource);
            var trash = stations.Single(station => station.Kind == StationKind.TrashBin);

            beefCrate.PickupOrPlace(player);
            board.PickupOrPlace(player);
            Assert.That(board.Work(player, IngredientItem.RequiredChopSeconds, out var reason), Is.True, reason);
            board.PickupOrPlace(player);
            panHeat.PickupOrPlace(player);

            var panItem = panHeat.SlottedItem;
            Assert.That(panHeat.AdvancePanHeating(
                FryingPan.RequiredCookSeconds + FryingPan.BurnGraceSeconds,
                out reason), Is.True, reason);
            Assert.That(panItem.Pan.IsBurned, Is.True);
            Assert.That(panHeat.SlottedItem, Is.SameAs(panItem),
                "Burning must not automatically discard the contents or the pan.");

            panHeat.PickupOrPlace(player);
            Assert.That(panHeat.SlottedItem, Is.Null,
                "Pressing E at the heat source must pick up a burned pan, not discard its contents.");
            Assert.That(player.HeldItem, Is.SameAs(panItem));
            Assert.That(player.HeldItem.Pan.IsBurned, Is.True);
            Assert.That(player.HeldVisual.GetComponentsInChildren<Transform>()
                .Any(child => child.name == "Pan Fire Outer"), Is.True);

            trash.PickupOrPlace(player);
            Assert.That(player.HeldItem, Is.SameAs(panItem),
                "Trash must discard only the burned contents and leave the reusable pan in hand.");
            Assert.That(player.HeldItem.Pan.IsEmpty, Is.True);
            Assert.That(player.HeldVisual.GetComponentsInChildren<Transform>()
                .Any(child => child.name == "Pan Fire Outer"), Is.False);

            panHeat.PickupOrPlace(player);
            Assert.That(player.HeldItem, Is.Null);
            Assert.That(panHeat.SlottedItem, Is.SameAs(panItem));
            Assert.That(panHeat.SlottedItem.Pan.IsEmpty, Is.True);
        }

        [UnityTest]
        public IEnumerator RestartRebuildsTheRuntimeGeneratedScene()
        {
            SceneManager.LoadScene("Tutorial_1_2");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var firstGame = Object.FindFirstObjectByType<KitchenGameController>();
            Assert.That(firstGame, Is.Not.Null);

            firstGame.Restart();
            yield return null;
            yield return null;

            var games = Object.FindObjectsByType<KitchenGameController>(FindObjectsSortMode.None);
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            Assert.That(games, Has.Length.EqualTo(1));
            Assert.That(games[0].Session.State.Status, Is.EqualTo(ShiftStatus.Running));
            Assert.That(games[0].Session.Stage.Id, Is.EqualTo(GameIds.TutorialSoupStage));
            Assert.That(cameras.Count(camera => camera.isActiveAndEnabled), Is.EqualTo(1));
            Assert.That(eventSystems, Has.Length.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator IngredientCrateDisplaysConfiguredPhotoAndDispensesMatchingRawIngredient()
        {
            SceneManager.LoadScene("Tutorial_1_1");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var player = Object.FindFirstObjectByType<PlayerController>();
            var crate = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None)
                .Single(station => station.Kind == StationKind.IngredientCrate);

            Assert.That(crate.SourceIngredientId, Is.EqualTo(GameIds.LettuceIngredient));
            Assert.That(crate.SourceCard, Is.Not.Null);
            Assert.That(crate.SourceCard.IngredientId, Is.EqualTo(GameIds.LettuceIngredient));
            Assert.That(crate.SourceCard.PhotoTexture, Is.Not.Null);
            Assert.That(crate.SourceCard.PhotoTexture.name, Is.EqualTo("ingredient_lettuce_source_card_sv3"));
            Assert.That(crate.SourceCard.transform.Find("Photo Card Backing"), Is.Null);
            var circularPhoto = crate.SourceCard.transform.Find("Circular Ingredient Source Photo");
            Assert.That(circularPhoto, Is.Not.Null);
            Assert.That(circularPhoto.localScale.x, Is.EqualTo(1.005f).Within(0.001f));
            Assert.That(circularPhoto.localScale.y, Is.EqualTo(1.005f).Within(0.001f));
            Assert.That(circularPhoto.GetComponent<Renderer>().material.shader.name,
                Is.EqualTo("CookedOut/CircularIngredientCard"));
            var productionCrate = crate.GetComponentInChildren<KitchenProductionAsset>();
            Assert.That(productionCrate, Is.Not.Null);
            Assert.That(productionCrate.TryGetVisualBounds(out var localCrateBounds), Is.True);
            Assert.That(localCrateBounds.min.y, Is.LessThanOrEqualTo(-0.42f));
            var crateBottom = productionCrate.GetComponentsInChildren<Renderer>()
                .Min(renderer => renderer.bounds.min.y);
            var crateTop = productionCrate.GetComponentsInChildren<Renderer>()
                .Max(renderer => renderer.bounds.max.y);
            Assert.That(crateBottom, Is.LessThanOrEqualTo(0.06f),
                "The ingredient source must stand on the floor rather than float above it.");
            Assert.That(crate.SourceCard.transform.position.y - crateTop, Is.InRange(-0.005f, 0.01f),
                "The circular IngredientSourceCards image must lie on the bin rim rather than hover above it.");

            crate.PickupOrPlace(player);

            Assert.That(player.HeldItem, Is.Not.Null);
            Assert.That(player.HeldItem.IsIngredient, Is.True);
            Assert.That(player.HeldItem.Ingredient.IngredientId, Is.EqualTo(GameIds.LettuceIngredient));
            Assert.That(player.HeldItem.Ingredient.Preparation, Is.EqualTo(IngredientPreparation.Raw));

            var firstItem = player.HeldItem;
            crate.PickupOrPlace(player);
            Assert.That(player.HeldItem, Is.SameAs(firstItem), "A full-handed player must not receive another item.");
        }

        [UnityTest]
        public IEnumerator SoupIngredientCratesUseTheirMatchingGeneratedPhotoCards()
        {
            SceneManager.LoadScene("Tutorial_1_2");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var crates = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None)
                .Where(station => station.Kind == StationKind.IngredientCrate)
                .OrderBy(station => station.SourceIngredientId)
                .ToArray();

            Assert.That(crates, Has.Length.EqualTo(2));
            Assert.That(crates.Select(crate => crate.SourceIngredientId), Is.EquivalentTo(new[]
            {
                GameIds.CarrotIngredient,
                GameIds.OnionIngredient
            }));
            Assert.That(
                crates.Single(crate => crate.SourceIngredientId == GameIds.CarrotIngredient)
                    .SourceCard.PhotoTexture.name,
                Is.EqualTo("ingredient_carrot_source_card_sv2"));
            Assert.That(
                crates.Single(crate => crate.SourceIngredientId == GameIds.OnionIngredient)
                    .SourceCard.PhotoTexture.name,
                Is.EqualTo("ingredient_onion_source_card_sv3"));
            Assert.That(IngredientSourceCardView.IngredientIconScale, Is.EqualTo(0.80f).Within(0.001f));
            Assert.That(crates.All(crate =>
                    Vector3.Distance(crate.SourceCard.transform.localScale, Vector3.one * 0.80f) < 0.001f),
                Is.True,
                "Every ingredient-crate top photo should be 20% smaller than its previous authored size.");
        }

        [UnityTest]
        public IEnumerator RawCarrotTurnsNinetyDegreesWhenPlacedOnChoppingBoard()
        {
            SceneManager.LoadScene("Tutorial_1_2");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var player = Object.FindFirstObjectByType<PlayerController>();
            var stations = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None);
            var carrotCrate = stations.Single(station =>
                station.Kind == StationKind.IngredientCrate &&
                station.SourceIngredientId == GameIds.CarrotIngredient);
            var board = stations.Single(station => station.Kind == StationKind.ChoppingBoard);

            carrotCrate.PickupOrPlace(player);
            board.PickupOrPlace(player);

            Assert.That(board.ItemVisual, Is.Not.Null);
            Assert.That(Mathf.DeltaAngle(
                    board.ItemVisual.localEulerAngles.y,
                    InteractableStation.ChoppingBoardCarrotYawDegrees),
                Is.Zero.Within(0.1f));
        }

        [UnityTest]
        public IEnumerator OrderFoodImageUsesOpenLidRecipeArtworkForEachTutorialRecipe()
        {
            SceneManager.LoadScene("Tutorial_1_1");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var saladImage = GameObject.Find("Order Food Image").GetComponent<RawImage>();
            Assert.That(saladImage.texture, Is.Not.Null);
            Assert.That(saladImage.texture.name, Is.EqualTo("order_lettuce_salad_sv1"));
            var saladTicket = Object.FindFirstObjectByType<OrderTicketView>();
            Assert.That(saladTicket.IngredientCardCount, Is.EqualTo(1));
            Assert.That(saladTicket.IngredientCards[0].IngredientId, Is.EqualTo(GameIds.LettuceIngredient));
            Assert.That(saladTicket.IngredientCards[0].ShowsChop, Is.False);
            Assert.That(saladTicket.IngredientCards[0].ShowsHeat, Is.False);
            Assert.That(GameObject.Find("Order Required Process"), Is.Not.Null);
            Assert.That(GameObject.Find("Process Chop"), Is.Null);

            SceneManager.LoadScene("Tutorial_1_2");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var soupImage = GameObject.Find("Order Food Image").GetComponent<RawImage>();
            Assert.That(soupImage.texture, Is.Not.Null);
            Assert.That(soupImage.texture.name, Is.EqualTo("order_vegetable_soup_sv1"));

            SceneManager.LoadScene("Tutorial_1_3");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var deliveryTicket = Object.FindFirstObjectByType<OrderTicketView>();
            Assert.That(deliveryTicket.Destination, Is.EqualTo(OrderTicketDestination.Bike));
            Assert.That(GameObject.Find("Destination Bike"), Is.Null,
                "Route guidance belongs to the minimap rather than the compact order ticket.");

            SceneManager.LoadScene("Tutorial_1_5");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var hamburgerImage = GameObject.Find("Order Food Image").GetComponent<RawImage>();
            Assert.That(hamburgerImage.texture, Is.Not.Null);
            Assert.That(hamburgerImage.texture.name, Is.EqualTo("order_hamburger_plate_sv1"));
            Assert.That(hamburgerImage.gameObject.activeSelf, Is.True);
            var hamburgerTicket = Object.FindFirstObjectByType<OrderTicketView>();
            Assert.That(hamburgerTicket.IngredientCardCount, Is.EqualTo(2));
            Assert.That(hamburgerTicket.IngredientCards.Select(card => card.IngredientId),
                Is.EquivalentTo(new[] { GameIds.BeefIngredient, GameIds.LettuceIngredient }));
            Assert.That(hamburgerTicket.IngredientCards.Select(card => card.SourceTexture.name),
                Is.EquivalentTo(new[]
                {
                    "ingredient_beef_source_card_sv2",
                    "ingredient_lettuce_source_card_sv3"
                }));
            Assert.That(hamburgerTicket.IngredientCards.All(card => !card.ShowsChop), Is.True);
            Assert.That(hamburgerTicket.IngredientCards.All(card => card.ShowsHeat), Is.True);
            Assert.That(GameObject.Find("Process Fry"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator OrderTicketCommunicatesRecipeProcessesRouteAndDeadlineWithoutWords()
        {
            SceneManager.LoadScene("Tutorial_1_2");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var game = Object.FindFirstObjectByType<KitchenGameController>();
            var ticket = Object.FindFirstObjectByType<OrderTicketView>();
            var order = game.Session.State.Orders[0];

            Assert.That(ticket, Is.Not.Null);
            Assert.That(ticket.GetComponentsInChildren<Text>(true), Is.Empty,
                "The order ticket must remain understandable without text labels.");
            Assert.That(ticket.IngredientCardCount, Is.EqualTo(2));
            Assert.That(ticket.IngredientCards.Select(card => card.IngredientId),
                Is.EquivalentTo(new[] { GameIds.CarrotIngredient, GameIds.OnionIngredient }));
            Assert.That(ticket.IngredientCards.All(card => !card.ShowsChop), Is.True);
            Assert.That(ticket.IngredientCards.All(card => card.ShowsHeat), Is.True);
            Assert.That(ticket.IngredientCards.Select(card => card.SourceTexture.name),
                Is.EquivalentTo(new[]
                {
                    "ingredient_carrot_source_card_sv2",
                    "ingredient_onion_source_card_sv3"
                }));
            var potIcon = GameObject.Find("Generated Pot Silhouette").GetComponent<RawImage>();
            Assert.That(potIcon.texture, Is.Not.Null);
            Assert.That(potIcon.texture.name, Is.EqualTo("order_process_pot_silhouette_sv1"));
            Assert.That(potIcon.rectTransform.sizeDelta,
                Is.EqualTo(Vector2.one * OrderTicketView.PotProcessIconSize));
            Assert.That(OrderTicketView.PotProcessIconSize,
                Is.LessThan(OrderTicketView.IngredientPhotoSize));
            Assert.That(OrderTicketView.PotProcessIconSize,
                Is.GreaterThan(OrderTicketView.IngredientPhotoSize * 0.9f),
                "The pot silhouette should be only slightly smaller than a source ingredient photo.");
            Assert.That(GameObject.Find("Process Chop"), Is.Null);
            Assert.That(GameObject.Find("Ticket Paper Tab"), Is.Null,
                "The decorative diamond tab must not remain below the ticket.");
            var ingredientMask = GameObject.Find("Ingredient Source Mask").GetComponent<RectTransform>();
            Assert.That(ingredientMask.sizeDelta,
                Is.EqualTo(Vector2.one * OrderTicketView.IngredientPhotoSize));
            var ingredientPhoto = GameObject.Find("Ingredient Source Image").GetComponent<RectTransform>();
            Assert.That(ingredientPhoto.sizeDelta,
                Is.EqualTo(Vector2.one * OrderTicketView.IngredientTextureSize));
            Assert.That(OrderTicketView.IngredientPhotoSize,
                Is.EqualTo(128f * 0.70f).Within(0.5f),
                "The ingredient photo itself must be about 30% smaller than its previous authored size.");
            Assert.That(OrderTicketView.IngredientPhotoSize * ticket.transform.localScale.x,
                Is.EqualTo(63f).Within(0.1f),
                "The existing 70% ticket scale remains independent from this photo-size revision.");
            Assert.That(ticket.FoodImage.rectTransform.sizeDelta,
                Is.EqualTo(Vector2.one * OrderTicketView.CompletedFoodPhotoSize));
            Assert.That(OrderTicketView.CompletedFoodPhotoSize,
                Is.EqualTo(120f * 1.10f).Within(0.01f));
            Assert.That(ticket.Destination, Is.EqualTo(OrderTicketDestination.ServingHatch));
            Assert.That(ticket.Holder, Is.EqualTo(OrderTicketHolder.Kitchen));
            Assert.That(ticket.Urgency, Is.EqualTo(OrderTicketUrgency.Safe));
            Assert.That(ticket.TimerNormalized, Is.EqualTo(1f).Within(0.01f));
            Assert.That(ticket.HasContinuousTimer, Is.True);
            Assert.That(ticket.TimerFillWidth, Is.EqualTo(230f).Within(0.20f),
                "The live timer may advance by a few milliseconds during scene setup.");
            Assert.That(GameObject.Find("Order Timer Fill"), Is.Not.Null);

            game.SetClockPaused(true);
            Assert.That(ticket.IsPauseIconVisible, Is.True);
            game.SetClockPaused(false);
            Assert.That(ticket.IsPauseIconVisible, Is.False);

            game.Session.Tick(game.Session.Stage.OrderDurationSeconds * 0.4f);
            ticket.RefreshNow(false);
            Assert.That(ticket.Urgency, Is.EqualTo(OrderTicketUrgency.Normal));
            Assert.That(ticket.TimerNormalized, Is.EqualTo(0.6f).Within(0.01f));
            Assert.That(ticket.TimerFillWidth, Is.EqualTo(230f * 0.6f).Within(0.05f));

            game.Session.Tick(game.Session.Stage.OrderDurationSeconds * 0.35f);
            ticket.RefreshNow(false);
            Assert.That(ticket.Urgency, Is.EqualTo(OrderTicketUrgency.Urgent));
            Assert.That(ticket.TimerNormalized, Is.EqualTo(0.25f).Within(0.01f));
            Assert.That(ticket.TimerFillWidth, Is.EqualTo(230f * 0.25f).Within(0.05f));

            game.Session.Tick(order.RemainingSeconds + 0.01f);
            ticket.RefreshNow(false);
            Assert.That(order.Status, Is.EqualTo(OrderStatus.Expired));
            Assert.That(ticket.Urgency, Is.EqualTo(OrderTicketUrgency.Expired));
            Assert.That(ticket.TimerNormalized, Is.Zero);
            Assert.That(ticket.TimerFillWidth, Is.Zero);
            Assert.That(ticket.IsExpiredIconVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator TutorialOrderQueueSpawnsUpToFiveAndExpiredTicketAnimatesOut()
        {
            SceneManager.LoadScene("Tutorial_1_1");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var game = Object.FindFirstObjectByType<KitchenGameController>();
            var tickets = Object.FindObjectsByType<OrderTicketView>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .OrderBy(ticket => ticket.OrderIndex)
                .ToArray();
            Assert.That(tickets, Has.Length.EqualTo(5));
            Assert.That(tickets.Count(ticket => ticket.gameObject.activeSelf), Is.EqualTo(1));

            game.Session.Tick(game.Session.Stage.OrderSpawnIntervalSeconds);
            foreach (var ticket in tickets)
            {
                ticket.RefreshNow(false);
            }
            Assert.That(tickets.Count(ticket => ticket.gameObject.activeSelf), Is.EqualTo(2));

            var expiredOrder = game.Session.State.Orders[0];
            var expiredOrderId = expiredOrder.Id;
            game.Session.Tick(expiredOrder.RemainingSeconds + 0.01f);
            tickets[0].RefreshNow(false);
            Assert.That(tickets[0].DisplayedOrderId, Is.EqualTo(expiredOrderId));
            Assert.That(tickets[0].Urgency, Is.EqualTo(OrderTicketUrgency.Expired));

            game.Session.Tick(game.Session.Stage.OrderTicketExitSeconds * 0.5f);
            tickets[0].RefreshNow(false);
            Assert.That(tickets[0].ExitAnimationAlpha, Is.InRange(0.45f, 0.55f));

            game.Session.Tick(game.Session.Stage.OrderTicketExitSeconds * 0.6f);
            foreach (var ticket in tickets)
            {
                ticket.RefreshNow(false);
            }
            Assert.That(tickets[0].DisplayedOrderId, Is.Not.EqualTo(expiredOrderId));
            Assert.That(tickets[0].ExitAnimationAlpha, Is.EqualTo(1f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator TrashBinAcceptsAndConsumesReservedThrownItem()
        {
            SceneManager.LoadScene("Tutorial_1_1");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var game = Object.FindFirstObjectByType<KitchenGameController>();
            var trash = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None)
                .Single(station => station.Kind == StationKind.TrashBin);
            var thrownItem = WorldItem.FromIngredient(game.Session.TakeRawLettuce());

            Assert.That(trash.CanReceiveThrown(thrownItem, out var reason), Is.True, reason);
            Assert.That(trash.TryReserveThrown(thrownItem, out reason), Is.True, reason);
            trash.CompleteThrownDelivery(thrownItem);

            Assert.That(trash.SlottedItem, Is.Null);
            Assert.That(trash.CanReceiveThrown(
                WorldItem.FromIngredient(game.Session.TakeRawLettuce()), out reason), Is.True, reason);
        }

        [UnityTest]
        public IEnumerator GameplayHudUsesProductionTopBarWithoutInstructionalEnglish()
        {
            SceneManager.LoadScene("Tutorial_1_1");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var timerPanel = GameObject.Find("Shift Timer").GetComponent<RectTransform>();
            var scorePanel = GameObject.Find("Score").GetComponent<RectTransform>();
            var ticket = GameObject.Find("Order Ticket").GetComponent<RectTransform>();
            var timerValue = GameObject.Find("Shift Time Value").GetComponent<Text>();
            var scoreValue = GameObject.Find("Score Value").GetComponent<Text>();

            Assert.That(timerPanel.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(scorePanel.anchorMin, Is.EqualTo(new Vector2(1f, 1f)));
            Assert.That(ticket.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(ticket.localScale, Is.EqualTo(Vector3.one * 0.70f));
            Assert.That(
                ticket.anchoredPosition.x - ticket.sizeDelta.x * ticket.localScale.x * 0.5f,
                Is.GreaterThan(timerPanel.anchoredPosition.x + timerPanel.sizeDelta.x * 0.5f),
                "The order queue must begin to the right of the shift timer.");
            var ingredientBackground = GameObject.Find("Ingredient Area Background").GetComponent<RectTransform>();
            Assert.That(ingredientBackground.sizeDelta.y, Is.EqualTo(164f));
            var foodRect = GameObject.Find("Order Food Image").GetComponent<RectTransform>();
            Assert.That(foodRect.sizeDelta.x, Is.EqualTo(foodRect.sizeDelta.y).Within(0.01f));
            Assert.That(timerValue.text, Does.Match("^[0-9]+:[0-9]{2}$"));
            Assert.That(scoreValue.text, Is.EqualTo("0"));
            Assert.That(GameObject.Find("HUD Text"), Is.Null);
            Assert.That(GameObject.Find("Debug Reason"), Is.Null);
            Assert.That(GameObject.Find("Keyboard Hint"), Is.Null);
            Assert.That(GameObject.Find("Tutorial Guide"), Is.Null);
            Assert.That(GameObject.Find("Dash Status"), Is.Null);
        }

        [UnityTest]
        public IEnumerator KitchenStationsUseTheApprovedToyEquipmentSilhouetteLanguage()
        {
            SceneManager.LoadScene("Tutorial_1_2");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var stations = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None);
            var board = stations.Single(station => station.Kind == StationKind.ChoppingBoard);
            var pot = stations.Single(station => station.Kind == StationKind.PotHeatSource);
            var crate = stations.First(station => station.Kind == StationKind.IngredientCrate);

            var boardProduction = board.GetComponentInChildren<KitchenProductionAsset>();
            var potProduction = pot.GetComponentInChildren<KitchenProductionAsset>();
            var crateProduction = crate.GetComponentInChildren<KitchenProductionAsset>();
            Assert.That(boardProduction, Is.Not.Null);
            Assert.That(potProduction, Is.Not.Null);
            Assert.That(crateProduction, Is.Not.Null);
            Assert.That(boardProduction.AssetKey, Is.EqualTo(KitchenArchetypeIds.ChoppingBoard));
            Assert.That(potProduction.AssetKey, Is.EqualTo(KitchenArchetypeIds.PotHeatSource));
            Assert.That(crateProduction.AssetKey, Is.EqualTo(KitchenArchetypeIds.IngredientCrate));
            Assert.That(board.transform.Find("Station Body").GetComponent<Renderer>().enabled, Is.False);
            var boardMaterials = boardProduction.GetComponentsInChildren<MeshRenderer>()
                .SelectMany(renderer => renderer.sharedMaterials)
                .Select(material => material.name)
                .ToArray();
            var potMaterials = potProduction.GetComponentsInChildren<MeshRenderer>()
                .SelectMany(renderer => renderer.sharedMaterials)
                .Select(material => material.name)
                .ToArray();
            var crateMaterials = crateProduction.GetComponentsInChildren<MeshRenderer>()
                .SelectMany(renderer => renderer.sharedMaterials)
                .Select(material => material.name)
                .ToArray();
            Assert.That(boardMaterials, Does.Contain("MAT_Steel"));
            Assert.That(boardMaterials, Does.Contain("MAT_SteelLight"));
            Assert.That(boardMaterials, Does.Contain("MAT_SteelDark"));
            Assert.That(boardMaterials, Does.Contain("MAT_BoardIvory"));
            Assert.That(boardMaterials, Does.Contain("MAT_Metal"));
            Assert.That(potMaterials, Does.Contain("MAT_DeepInset"));
            Assert.That(potMaterials, Does.Contain("MAT_Steel"));
            Assert.That(potMaterials, Does.Contain("MAT_SteelLight"));
            Assert.That(potMaterials, Does.Contain("MAT_SteelDark"));
            Assert.That(potMaterials, Does.Contain("MAT_FlameBlue"),
                "The heat source needs a readable blue gas-flame ring.");
            Assert.That(crateMaterials, Does.Contain("MAT_DeepInset"));
            Assert.That(crateMaterials, Does.Contain("MAT_Steel"));
            Assert.That(crateMaterials, Does.Contain("MAT_SteelDark"));
            Assert.That(crateMaterials, Does.Contain("MAT_Teal"));
            Assert.That(crateMaterials, Does.Not.Contain("MAT_Cream"),
                "Ingredient sources must read as open stainless produce bins rather than work counters.");
        }

        [Test]
        public void KitchenProductionRevisionUsesGaplessMutedTerrainAndNonverbalStationCues()
        {
            var floorA = Resources.Load<GameObject>("KitchenProduction/FloorTileA")
                .GetComponent<KitchenProductionAsset>();
            var floorB = Resources.Load<GameObject>("KitchenProduction/FloorTileB")
                .GetComponent<KitchenProductionAsset>();
            var carrot = Resources.Load<GameObject>("KitchenProduction/CarrotRaw")
                .GetComponent<KitchenProductionAsset>();
            var dispenser = Resources.Load<GameObject>("KitchenProduction/ContainerDispenser")
                .GetComponent<KitchenProductionAsset>();
            var serving = Resources.Load<GameObject>("KitchenProduction/ServingHatch")
                .GetComponent<KitchenProductionAsset>();
            var trash = Resources.Load<GameObject>("KitchenProduction/TrashBin")
                .GetComponent<KitchenProductionAsset>();
            var counter = Resources.Load<GameObject>("KitchenProduction/Counter")
                .GetComponent<KitchenProductionAsset>();
            var crate = Resources.Load<GameObject>("KitchenProduction/IngredientCrate")
                .GetComponent<KitchenProductionAsset>();
            var bikeDock = Resources.Load<GameObject>("KitchenProduction/BikeDock")
                .GetComponent<KitchenProductionAsset>();

            Assert.That(floorA.TryGetVisualBounds(out var floorABounds), Is.True);
            Assert.That(floorB.TryGetVisualBounds(out var floorBBounds), Is.True);
            Assert.That(floorABounds.size.x, Is.GreaterThanOrEqualTo(KitchenProductionAssetFactory.NominalCellSize));
            Assert.That(floorABounds.size.z, Is.GreaterThanOrEqualTo(KitchenProductionAssetFactory.NominalCellSize));
            Assert.That(floorBBounds.size.x, Is.GreaterThanOrEqualTo(KitchenProductionAssetFactory.NominalCellSize));
            Assert.That(floorBBounds.size.z, Is.GreaterThanOrEqualTo(KitchenProductionAssetFactory.NominalCellSize));
            Assert.That(MaxRgb(MaterialColor(floorA, "MAT_FloorA")), Is.LessThan(0.55f));
            Assert.That(MaxRgb(MaterialColor(floorB, "MAT_FloorB")), Is.LessThan(0.55f));
            Assert.That(counter.TryGetVisualBounds(out var counterBounds), Is.True);
            Assert.That(counterBounds.size.x, Is.GreaterThanOrEqualTo(KitchenProductionAssetFactory.NominalCellSize));
            Assert.That(counterBounds.size.z, Is.GreaterThanOrEqualTo(KitchenProductionAssetFactory.NominalCellSize));
            Assert.That(crate.TryGetVisualBounds(out var crateBounds), Is.True);
            Assert.That(crateBounds.min.y, Is.LessThanOrEqualTo(-0.42f),
                "The produce bin must reach the station root's floor plane instead of floating.");
            Assert.That(bikeDock.TryGetVisualBounds(out var bikeDockBounds), Is.True);
            Assert.That(bikeDockBounds.size.y, Is.LessThan(0.30f),
                "Bike loading must not use a counter-height workbench.");

            Assert.That(carrot.TryGetVisualBounds(out var carrotBounds), Is.True);
            Assert.That(
                Mathf.Max(carrotBounds.size.x, carrotBounds.size.z) /
                Mathf.Min(carrotBounds.size.x, carrotBounds.size.z),
                Is.GreaterThan(1.6f),
                "A raw carrot must have a clearly tapered, elongated silhouette rather than an oval blob.");
            Assert.That(MaterialNames(carrot), Does.Contain("MAT_Carrot"));
            Assert.That(MaterialNames(carrot), Does.Contain("MAT_CarrotGroove"),
                "The whole carrot should carry the darker growth grooves shown on its source card.");
            Assert.That(MaterialNames(carrot), Does.Contain("MAT_Leaf"));

            Assert.That(MaterialNames(dispenser), Does.Contain("MAT_Container"));
            Assert.That(MaterialNames(dispenser), Does.Contain("MAT_DeepInset"));
            Assert.That(dispenser.TryGetVisualBounds(out var dispenserBounds), Is.True);
            Assert.That(dispenserBounds.center.z, Is.LessThan(-0.03f),
                "The container pickup lip must project toward the authored local -Z interaction side.");
            Assert.That(MaterialNames(serving), Does.Contain("MAT_Steel"));
            Assert.That(MaterialNames(serving), Does.Contain("MAT_SteelLight"));
            Assert.That(MaterialNames(serving), Does.Contain("MAT_Teal"));
            Assert.That(MaterialNames(trash), Does.Contain("MAT_Steel"));
            Assert.That(MaterialNames(trash), Does.Contain("MAT_Cream"));
            Assert.That(trash.TryGetVisualBounds(out var trashBounds), Is.True);
            Assert.That(trashBounds.size.y, Is.LessThan(1.1f),
                "The fish-skeleton cue must sit on the bin body rather than on a raised sign.");
            Assert.That(MaterialNames(trash), Does.Contain("MAT_SteelDark"));
            Assert.That(MaterialNames(trash), Does.Contain("MAT_Plum"));
            Assert.That(serving.TryGetVisualBounds(out var servingBounds), Is.True);
            Assert.That(dispenserBounds.size.y, Is.GreaterThan(1f));
            Assert.That(servingBounds.size.y, Is.GreaterThan(1.2f));
        }

        [Test]
        public void KitchenProductionLibraryContainsThirtyValidatedReusablePrefabs()
        {
            var expected = new[]
            {
                ("FloorTileA", "terrain.floor.a", KitchenProductionAssetCategory.Terrain),
                ("FloorTileB", "terrain.floor.b", KitchenProductionAssetCategory.Terrain),
                ("Wall", KitchenArchetypeIds.Wall, KitchenProductionAssetCategory.Terrain),
                ("Counter", KitchenArchetypeIds.Counter, KitchenProductionAssetCategory.Station),
                ("IngredientCrate", KitchenArchetypeIds.IngredientCrate, KitchenProductionAssetCategory.Station),
                ("ChoppingBoard", KitchenArchetypeIds.ChoppingBoard, KitchenProductionAssetCategory.Station),
                ("PotHeatSource", KitchenArchetypeIds.PotHeatSource, KitchenProductionAssetCategory.Station),
                ("AssemblyCounter", KitchenArchetypeIds.AssemblyCounter, KitchenProductionAssetCategory.Station),
                ("ContainerDispenser", KitchenArchetypeIds.ContainerDispenser, KitchenProductionAssetCategory.Station),
                ("ServingHatch", KitchenArchetypeIds.ServingHatch, KitchenProductionAssetCategory.Station),
                ("TrashBin", KitchenArchetypeIds.TrashBin, KitchenProductionAssetCategory.Station),
                ("BikeDock", KitchenArchetypeIds.BikeDock, KitchenProductionAssetCategory.Station),
                ("DeliveryPoint", KitchenArchetypeIds.DeliveryPoint, KitchenProductionAssetCategory.Station),
                ("RecoveryBin", KitchenArchetypeIds.RecoveryBin, KitchenProductionAssetCategory.Station),
                ("ExtinguisherCabinet", KitchenArchetypeIds.ExtinguisherCabinet, KitchenProductionAssetCategory.Station),
                ("WashingSink", KitchenArchetypeIds.WashingSink, KitchenProductionAssetCategory.Station),
                ("PlateDispenser", KitchenArchetypeIds.PlateDispenser, KitchenProductionAssetCategory.Station),
                ("DishReturn", KitchenArchetypeIds.DishReturn, KitchenProductionAssetCategory.Station),
                ("CourierShelf", KitchenArchetypeIds.CourierShelf, KitchenProductionAssetCategory.Station),
                ("DynamicBridge", "station.dynamic.bridge", KitchenProductionAssetCategory.Station),
                ("FireExtinguisher", GameIds.FireExtinguisherTool, KitchenProductionAssetCategory.Tool),
                ("LettuceRaw", GameIds.LettuceIngredient + ".raw", KitchenProductionAssetCategory.Ingredient),
                ("LettuceChopped", GameIds.LettuceIngredient + ".chopped", KitchenProductionAssetCategory.Ingredient),
                ("CarrotRaw", GameIds.CarrotIngredient + ".raw", KitchenProductionAssetCategory.Ingredient),
                ("CarrotChopped", GameIds.CarrotIngredient + ".chopped", KitchenProductionAssetCategory.Ingredient),
                ("OnionRaw", GameIds.OnionIngredient + ".raw", KitchenProductionAssetCategory.Ingredient),
                ("OnionChopped", GameIds.OnionIngredient + ".chopped", KitchenProductionAssetCategory.Ingredient),
                ("BeefRaw", GameIds.BeefIngredient + ".raw", KitchenProductionAssetCategory.Ingredient),
                ("BeefChopped", GameIds.BeefIngredient + ".chopped", KitchenProductionAssetCategory.Ingredient),
                ("BeefCooked", GameIds.BeefIngredient + ".cooked", KitchenProductionAssetCategory.Ingredient)
            };

            Assert.That(expected, Has.Length.EqualTo(30));
            foreach (var entry in expected)
            {
                var prefab = Resources.Load<GameObject>("KitchenProduction/" + entry.Item1);
                Assert.That(prefab, Is.Not.Null, entry.Item1);
                var productionAsset = prefab.GetComponent<KitchenProductionAsset>();
                Assert.That(productionAsset, Is.Not.Null, entry.Item1);
                Assert.That(
                    productionAsset.TryValidate(entry.Item2, entry.Item3, out var reason),
                    Is.True,
                    entry.Item1 + ": " + reason);
                Assert.That(productionAsset.TryGetVisualBounds(out var visualBounds), Is.True, entry.Item1);
                Assert.That(
                    Mathf.Max(visualBounds.size.x, visualBounds.size.y, visualBounds.size.z),
                    Is.GreaterThanOrEqualTo(productionAsset.NominalSize * 0.25f),
                    entry.Item1 + " must import at a visible meter-scale size");
                if (entry.Item1 == "FloorTileA" || entry.Item1 == "FloorTileB")
                {
                    Assert.That(
                        visualBounds.size.y,
                        Is.LessThan(visualBounds.size.x * 0.3f),
                        entry.Item1 + " must be horizontal in Unity Y-up space");
                    Assert.That(
                        visualBounds.size.y,
                        Is.LessThan(visualBounds.size.z * 0.3f),
                        entry.Item1 + " must not be rotated 90 degrees");
                }
                Assert.That(
                    productionAsset.GetComponentsInChildren<MeshRenderer>(true).Length,
                    Is.LessThanOrEqualTo(6),
                    entry.Item1 + " must stay within the mobile renderer budget");
                var triangleCount = productionAsset.GetComponentsInChildren<MeshFilter>(true)
                    .Where(filter => filter.sharedMesh != null)
                    .Sum(filter => Enumerable.Range(0, filter.sharedMesh.subMeshCount)
                        .Sum(subMesh => (long)filter.sharedMesh.GetIndexCount(subMesh) / 3L));
                Assert.That(triangleCount, Is.GreaterThan(0), entry.Item1);
                Assert.That(
                    triangleCount,
                    Is.LessThanOrEqualTo(12000),
                    entry.Item1 + " must stay within the per-asset triangle budget");
            }
        }

        [UnityTest]
        public IEnumerator KitchenControlsUseStyledFixedSafeAreaLayout()
        {
            SceneManager.LoadScene("Tutorial_1_1");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;
            Canvas.ForceUpdateCanvases();

            var safeArea = Object.FindFirstObjectByType<SafeAreaFitter>();
            Assert.That(safeArea, Is.Not.Null);
            Assert.That(safeArea.gameObject.name, Is.EqualTo("Safe Area"));
            Assert.That(safeArea.AppliedSafeArea.width, Is.GreaterThan(0f));

            var expected = new[]
            {
                ("PICK / PLACE Button", KitchenActionIcon.PickPlace, "action_pick_silhouette_sv1"),
                ("WORK Button", KitchenActionIcon.Work, "action_work_silhouette_sv1"),
                ("THROW Button", KitchenActionIcon.Throw, "action_throw_silhouette_sv1"),
                ("DASH Button", KitchenActionIcon.Dash, "action_dash_silhouette_sv1")
            };
            foreach (var entry in expected)
            {
                var buttonObject = GameObject.Find(entry.Item1);
                Assert.That(buttonObject, Is.Not.Null, entry.Item1);
                Assert.That(buttonObject.GetComponent<Button>(), Is.Not.Null, entry.Item1);
                var view = buttonObject.GetComponent<KitchenActionButtonView>();
                Assert.That(view, Is.Not.Null, entry.Item1);
                Assert.That(view.Icon, Is.EqualTo(entry.Item2), entry.Item1);
                Assert.That(buttonObject.transform.Find("Face"), Is.Not.Null, entry.Item1);
                Assert.That(buttonObject.GetComponent<Image>().sprite, Is.Not.Null, entry.Item1);
                Assert.That(buttonObject.GetComponentInChildren<Text>(), Is.Null,
                    entry.Item1 + " must communicate through its pictogram without a text label.");
                var iconRoot = buttonObject.transform.Find("Face/Button Content/Icon");
                Assert.That(iconRoot, Is.Not.Null, entry.Item1);
                var silhouette = iconRoot.Find("Generated Silhouette " + entry.Item2);
                Assert.That(silhouette, Is.Not.Null, entry.Item1);
                Assert.That(silhouette.GetComponent<Image>().sprite.texture.name, Is.EqualTo(entry.Item3));
                Assert.That(iconRoot.Find("Figure Head"), Is.Null,
                    entry.Item1 + " must not retain the discarded code-built icon design.");
                Assert.That(buttonObject.transform.Find("Face/Highlight"), Is.Null,
                    entry.Item1 + " must not retain the discarded glossy button face.");
                Assert.That(buttonObject.transform.Find("Face/Action Color Tab"), Is.Not.Null, entry.Item1);
                Assert.That(buttonObject.GetComponentsInChildren<Image>(true).Length,
                    Is.GreaterThanOrEqualTo(4), entry.Item1 + " must include raster icon and new button chrome");
            }

            var pickup = GameObject.Find("PICK / PLACE Button").GetComponent<RectTransform>();
            var work = GameObject.Find("WORK Button").GetComponent<RectTransform>();
            var throwButton = GameObject.Find("THROW Button").GetComponent<RectTransform>();
            var dash = GameObject.Find("DASH Button").GetComponent<RectTransform>();
            foreach (var rect in new[] { pickup, work, throwButton, dash })
            {
                Assert.That(rect.rect.width, Is.EqualTo(rect.rect.height).Within(0.01f));
                Assert.That(rect.rect.width, Is.EqualTo(pickup.rect.width).Within(0.01f));
            }
            Assert.That(work.anchoredPosition.x, Is.LessThan(pickup.anchoredPosition.x));
            Assert.That(work.anchoredPosition.y, Is.EqualTo(pickup.anchoredPosition.y).Within(0.01f));
            Assert.That(throwButton.anchoredPosition.y, Is.GreaterThan(pickup.anchoredPosition.y));
            Assert.That(throwButton.anchoredPosition.x, Is.EqualTo(work.anchoredPosition.x).Within(0.01f));
            Assert.That(dash.anchoredPosition.x, Is.EqualTo(pickup.anchoredPosition.x).Within(0.01f));
            Assert.That(dash.anchoredPosition.y, Is.EqualTo(throwButton.anchoredPosition.y).Within(0.01f));

            var throwSelectable = throwButton.GetComponent<Button>();
            Assert.That(throwSelectable.interactable, Is.False,
                "THROW must remain visible but disabled while the player has nothing throwable.");
            Assert.That(throwButton.GetComponent<CanvasGroup>().alpha, Is.LessThan(0.6f));
            var player = Object.FindFirstObjectByType<PlayerController>();
            var crate = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None)
                .Single(station => station.Kind == StationKind.IngredientCrate);
            crate.PickupOrPlace(player);
            yield return null;
            Assert.That(throwSelectable.interactable, Is.True,
                "THROW must enable without moving once the player carries a throwable item.");
            Assert.That(throwButton.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));

            var pointer = new PointerEventData(EventSystem.current) { pointerId = 909 };
            ExecuteEvents.Execute(pickup.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            Assert.That(pickup.localScale.x, Is.EqualTo(0.93f).Within(0.001f));
            ExecuteEvents.Execute(pickup.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            Assert.That(pickup.localScale, Is.EqualTo(Vector3.one));

            var joystick = Object.FindFirstObjectByType<VirtualJoystick>();
            Assert.That(joystick, Is.Not.Null);
            Assert.That(joystick.GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(joystick.transform.Find("Knob"), Is.Not.Null);
            Assert.That(joystick.transform.Find("Knob").GetComponent<RectTransform>().sizeDelta,
                Is.EqualTo(new Vector2(124f, 124f)));
            Assert.That(joystick.transform.childCount, Is.EqualTo(1),
                "The movement stick must remain visually simple: one base and one knob.");
        }

        [UnityTest]
        public IEnumerator PickupPlaceAndHeldWorkUseSeparateCommandsAndPreserveProgress()
        {
            SceneManager.LoadScene("Tutorial_1_1");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var player = Object.FindFirstObjectByType<PlayerController>();
            var input = Object.FindFirstObjectByType<KitchenInputRouter>();
            var stations = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None);
            var crate = stations.Single(station => station.Kind == StationKind.IngredientCrate);
            var board = stations.Single(station => station.Kind == StationKind.ChoppingBoard);

            var pickPlaceButton = GameObject.Find("PICK / PLACE Button");
            var workButton = GameObject.Find("WORK Button");
            Assert.That(pickPlaceButton, Is.Not.Null);
            Assert.That(workButton, Is.Not.Null);

            pickPlaceButton.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(input.Commands.ConsumePickupPlace(), Is.True);

            crate.PickupOrPlace(player);
            board.PickupOrPlace(player);
            Assert.That(board.SlottedItem.Ingredient.Preparation, Is.EqualTo(IngredientPreparation.Raw));

            Assert.That(
                board.Work(player, 1.25f, out var workReason),
                Is.True,
                workReason);
            Assert.That(board.SlottedItem.Ingredient.Preparation, Is.EqualTo(IngredientPreparation.Raw));
            Assert.That(board.WorkProgress, Is.EqualTo(5f / 12f).Within(0.001f));

            board.PickupOrPlace(player);
            Assert.That(player.HeldItem.Ingredient.PreparationProgress, Is.EqualTo(5f / 12f).Within(0.001f));
            board.PickupOrPlace(player);
            Assert.That(board.WorkProgress, Is.EqualTo(5f / 12f).Within(0.001f));

            var workPointer = new PointerEventData(EventSystem.current) { pointerId = 303 };
            ExecuteEvents.Execute(workButton, workPointer, ExecuteEvents.pointerDownHandler);
            Assert.That(input.Commands.WorkHeld, Is.True);
            ExecuteEvents.Execute(workButton, workPointer, ExecuteEvents.pointerUpHandler);
            Assert.That(input.Commands.WorkHeld, Is.False);

            Assert.That(
                board.Work(player, 1.75f, out workReason),
                Is.True,
                workReason);
            Assert.That(board.SlottedItem.Ingredient.Preparation, Is.EqualTo(IngredientPreparation.Chopped));
            Assert.That(board.WorkProgress, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator ChoppingProgressAdvancesOncePerQuarterSecondCut()
        {
            SceneManager.LoadScene("Tutorial_1_1");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var player = Object.FindFirstObjectByType<PlayerController>();
            var stations = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None);
            var crate = stations.Single(station => station.Kind == StationKind.IngredientCrate);
            var board = stations.Single(station => station.Kind == StationKind.ChoppingBoard);
            crate.PickupOrPlace(player);
            board.PickupOrPlace(player);

            Assert.That(board.Work(player, 0.24f, out var reason), Is.True, reason);
            Assert.That(board.WorkProgress, Is.Zero);
            Assert.That(board.Work(player, 0.01f, out reason), Is.True, reason);
            Assert.That(board.WorkProgress, Is.EqualTo(1f / 12f).Within(0.001f));

            var progress = board.transform.Find("Work Progress");
            Assert.That(progress, Is.Not.Null);
            Assert.That(progress.localPosition.y, Is.EqualTo(0.83f).Within(0.001f));
            Assert.That(progress.gameObject.activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator ProductionCharacterAndHeldItemStayBoundedAcrossEveryFacingDirection()
        {
            SceneManager.LoadScene("Tutorial_1_1");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var grid = Object.FindFirstObjectByType<KitchenGridRuntime>();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var input = Object.FindFirstObjectByType<KitchenInputRouter>();
            var crate = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None)
                .Single(station => station.Kind == StationKind.IngredientCrate);
            var visualRoot = player.transform.Find("Player Visual Root");
            var characterVisual = visualRoot == null
                ? null
                : visualRoot.GetComponentInChildren<AnimalChefVisual>(true);

            Assert.That(visualRoot, Is.Not.Null);
            Assert.That(visualRoot.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(visualRoot.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(visualRoot.localScale, Is.EqualTo(Vector3.one));
            Assert.That(characterVisual, Is.Not.Null);
            Assert.That(characterVisual.SpeciesId, Is.EqualTo("capybara"));
            Assert.That(characterVisual.TryValidateBindings(out var bindingReason), Is.True, bindingReason);
            Assert.That(
                characterVisual.TryValidateProductionAsset(out var productionReason),
                Is.True,
                productionReason);
            Assert.That(characterVisual.IsProductionAsset, Is.True);
            Assert.That(characterVisual.MotionRoot.name, Is.EqualTo("Character Motion Root"));
            Assert.That(characterVisual.MotionRoot.IsChildOf(visualRoot), Is.True);
            Assert.That(characterVisual.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length,
                Is.GreaterThanOrEqualTo(3));
            var characterLod = characterVisual.GetComponentInChildren<LODGroup>(true);
            Assert.That(characterLod, Is.Not.Null);
            Assert.That(characterLod.fadeMode, Is.EqualTo(LODFadeMode.None),
                "The iOS character must use opaque LOD swaps instead of dithered cross-fades.");
            foreach (var renderer in characterVisual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Assert.That(renderer.forceRenderingOff, Is.False);
                Assert.That(renderer.sharedMaterials, Is.Not.Empty);
                foreach (var material in renderer.sharedMaterials)
                {
                    Assert.That(material, Is.Not.Null);
                    var colorProperty = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
                    if (material.HasProperty(colorProperty))
                    {
                        Assert.That(material.GetColor(colorProperty).a, Is.EqualTo(1f).Within(0.001f));
                    }
                    if (material.HasProperty("_Surface"))
                    {
                        Assert.That(material.GetFloat("_Surface"), Is.EqualTo(0f).Within(0.001f));
                    }
                }
            }
            Assert.That(player.HeldAnchor, Is.Not.Null);
            Assert.That(player.HeldAnchor, Is.SameAs(characterVisual.HeldItemAnchor));
            Assert.That(player.HeldAnchor.IsChildOf(visualRoot), Is.True);
            Assert.That(player.HeldAnchor.IsChildOf(characterVisual.MotionRoot), Is.False);
            Assert.That(player.HeldAnchor.localPosition, Is.EqualTo(AnimalChefVisual.StandardHeldItemLocalPosition));

            player.Teleport(grid.CellCenter(new GridCoordinate(6, 3), 0.05f));
            crate.PickupOrPlace(player);
            Assert.That(player.HeldVisual, Is.Not.Null);
            Assert.That(player.HeldVisual.parent, Is.SameAs(player.HeldAnchor));

            var directions = new[] { Vector2.right, Vector2.up, Vector2.left, Vector2.down };
            foreach (var direction in directions)
            {
                input.SetTouchMove(direction);
                yield return null;

                Assert.That(characterVisual.MotionRoot.localPosition.magnitude,
                    Is.LessThan(0.09f), "The animated model must stay close to its visual root.");
                Assert.That(Vector3.Distance(player.HeldVisual.position, player.HeldAnchor.position),
                    Is.LessThan(0.001f), "The held visual must stay exactly on its socket.");
                Assert.That(Vector3.Distance(
                        player.transform.InverseTransformPoint(player.HeldVisual.position),
                        AnimalChefVisual.StandardHeldItemLocalPosition),
                    Is.LessThan(0.001f));
                Assert.That(Vector3.Dot(player.transform.forward, player.HorizontalVelocity.normalized),
                    Is.GreaterThan(0.999f), "Facing and movement must use the same newest input frame.");
            }

            input.ClearMovement();
        }

        [UnityTest]
        public IEnumerator CharacterMotionSelectsIdleWalkWorkAndCarryPoses()
        {
            SceneManager.LoadScene("Tutorial_1_1");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var player = Object.FindFirstObjectByType<PlayerController>();
            var input = Object.FindFirstObjectByType<KitchenInputRouter>();
            var grid = Object.FindFirstObjectByType<KitchenGridRuntime>();
            var motion = player.GetComponent<AnimalChefMotion>();
            var crate = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None)
                .Single(station => station.Kind == StationKind.IngredientCrate);
            var board = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None)
                .Single(station => station.Kind == StationKind.ChoppingBoard);

            Assert.That(motion, Is.Not.Null);
            Assert.That(motion.CurrentPose, Is.EqualTo(AnimalChefPose.Idle));

            input.SetTouchMove(Vector2.right);
            yield return null;
            Assert.That(motion.CurrentPose, Is.EqualTo(AnimalChefPose.Walk));

            input.ClearMovement();
            input.TouchWorkDown();
            yield return null;
            Assert.That(motion.CurrentPose, Is.EqualTo(AnimalChefPose.Idle),
                "Holding WORK without a valid selected board must not play the chopping pose.");
            Assert.That(player.IsPerformingWork, Is.False);

            input.TouchWorkUp();
            crate.PickupOrPlace(player);
            board.PickupOrPlace(player);
            player.Teleport(board.transform.position + Vector3.back * grid.Definition.Space.CellSize);
            var boardDirection = board.transform.position - player.transform.position;
            boardDirection.y = 0f;
            player.transform.rotation = Quaternion.LookRotation(boardDirection);
            yield return null;
            input.TouchWorkDown();
            yield return null;
            Assert.That(player.IsPerformingWork, Is.True);
            Assert.That(motion.CurrentPose, Is.EqualTo(AnimalChefPose.Work));

            Assert.That(board.Work(player, IngredientItem.RequiredChopSeconds, out var reason), Is.True, reason);
            yield return null;
            Assert.That(player.IsPerformingWork, Is.False,
                "Completing the ingredient must stop the chopping feedback even while WORK remains held.");
            Assert.That(motion.CurrentPose, Is.EqualTo(AnimalChefPose.Idle));

            input.TouchWorkUp();
            board.PickupOrPlace(player);
            yield return null;
            Assert.That(motion.CurrentPose, Is.EqualTo(AnimalChefPose.Carry));
            Assert.That(player.HeldAnchor.localPosition, Is.EqualTo(AnimalChefVisual.StandardHeldItemLocalPosition));
        }

        [UnityTest]
        public IEnumerator ChoppingKnifeHidesAtIdleAnimatesDuringWorkAndReturnsWhenCleared()
        {
            SceneManager.LoadScene("Tutorial_1_1");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var player = Object.FindFirstObjectByType<PlayerController>();
            var stations = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None);
            var crate = stations.Single(station => station.Kind == StationKind.IngredientCrate);
            var board = stations.Single(station => station.Kind == StationKind.ChoppingBoard);
            var knifeMotion = board.GetComponent<ChoppingKnifeMotion>();

            Assert.That(knifeMotion, Is.Not.Null);
            Assert.That(knifeMotion.KnifeRoot, Is.Not.Null);
            Assert.That(knifeMotion.KnifeRoot.name, Is.EqualTo("Knife Motion Root"));
            Assert.That(knifeMotion.KnifeRoot.GetComponentsInChildren<MeshRenderer>(true).Length,
                Is.GreaterThanOrEqualTo(1));
            var knifeGeometry = knifeMotion.KnifeRoot.GetComponentsInChildren<MeshRenderer>(true)
                .Single(renderer => renderer.name == "Knife Geometry");
            Assert.That(knifeGeometry.sharedMaterials.Select(material => material.name),
                Does.Contain("MAT_Metal"));
            Assert.That(knifeGeometry.sharedMaterials.Select(material => material.name),
                Does.Contain("MAT_Teal"));
            Assert.That(knifeGeometry.bounds.size.y, Is.GreaterThan(0.15f),
                "The knife blade must stand on its cutting edge instead of lying flat on the board.");
            Assert.That(knifeMotion.IsAnimating, Is.False);
            Assert.That(knifeMotion.IsVisible, Is.True);
            Assert.That(knifeMotion.CurrentLift01, Is.Zero);
            Assert.That(knifeMotion.SecondsPerCut,
                Is.EqualTo(InteractableStation.ChopIntervalSeconds).Within(0.001f));
            Assert.That(knifeMotion.MaximumLiftDistance, Is.EqualTo(1.0f).Within(0.001f));

            Assert.That(board.Work(player, 0.05f, out _), Is.False,
                "An empty board must not animate the knife.");
            Assert.That(knifeMotion.IsAnimating, Is.False);
            Assert.That(knifeMotion.IsVisible, Is.True,
                "The resting knife remains available while the board is empty.");

            crate.PickupOrPlace(player);
            board.PickupOrPlace(player);
            Assert.That(knifeMotion.IsVisible, Is.False,
                "Placing an ingredient must remove the resting knife from the board surface.");
            Assert.That(knifeMotion.KnifeRoot.gameObject.activeSelf, Is.False);

            Assert.That(board.Work(player, InteractableStation.ChopIntervalSeconds, out var reason),
                Is.True,
                reason);
            Assert.That(knifeMotion.IsAnimating, Is.True);
            Assert.That(knifeMotion.IsVisible, Is.True,
                "The knife must reappear only while an actual cut is being animated.");
            Assert.That(knifeMotion.IsInScreenForeground, Is.True);
            Assert.That(knifeMotion.ActiveForegroundDistance, Is.EqualTo(0.18f).Within(0.001f));
            var horizontalBladeDirection = Vector3.ProjectOnPlane(
                knifeMotion.BladeDirectionWorld,
                Vector3.up).normalized;
            Assert.That(Vector3.Dot(horizontalBladeDirection, player.transform.forward),
                Is.GreaterThan(0.98f),
                "The active knife must retain the previous player-facing animation.");
            yield return null;
            Assert.That(knifeMotion.CurrentLift01, Is.GreaterThan(0f));
            Assert.That(Vector3.Distance(
                    knifeMotion.RestLocalPosition,
                    knifeMotion.KnifeRoot.localPosition),
                Is.GreaterThan(0.29f));

            yield return new WaitForSeconds(0.35f);
            yield return null;
            Assert.That(knifeMotion.IsAnimating, Is.False);
            Assert.That(knifeMotion.IsVisible, Is.False,
                "Once the cut animation settles, the occupied board must hide the knife again.");
            Assert.That(knifeMotion.IsInScreenForeground, Is.False);
            Assert.That(knifeMotion.CurrentLift01, Is.Zero.Within(0.001f));

            Assert.That(board.Work(player, IngredientItem.RequiredChopSeconds, out reason), Is.True, reason);
            Assert.That(knifeMotion.IsAnimating, Is.False,
                "Completing the chop must leave the knife at rest.");
            Assert.That(knifeMotion.IsVisible, Is.False,
                "The knife stays hidden while the chopped ingredient remains on the board.");

            board.PickupOrPlace(player);
            Assert.That(player.HeldItem, Is.Not.Null);
            Assert.That(knifeMotion.IsVisible, Is.True,
                "Clearing the board must restore its resting knife.");
        }

        [UnityTest]
        public IEnumerator PlayerMarkerAndDashStreaksProvideTransientNonGameplayFeedback()
        {
            SceneManager.LoadScene("Tutorial_1_1");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var player = Object.FindFirstObjectByType<PlayerController>();
            var input = Object.FindFirstObjectByType<KitchenInputRouter>();
            var marker = player.GetComponent<PlayerIdentityMarker>();
            var dashEffect = player.GetComponent<DashSpeedEffect>();

            Assert.That(marker, Is.Not.Null);
            Assert.That(marker.PlayerIndex, Is.EqualTo(1));
            Assert.That(marker.MarkerRenderer, Is.Not.Null);
            Assert.That(marker.MarkerColor.b, Is.GreaterThan(marker.MarkerColor.r));
            Assert.That(marker.MarkerColor.a, Is.LessThan(0.7f));
            Assert.That(marker.MarkerRenderer.GetComponentsInChildren<Collider>(true), Is.Empty,
                "The position marker must never affect collision.");
            Assert.That(PlayerIdentityMarker.RingOuterRadius, Is.EqualTo(0.70f).Within(0.001f));
            Assert.That(PlayerIdentityMarker.RingThickness, Is.EqualTo(0.255f).Within(0.001f));
            Assert.That(marker.MarkerRenderer.GetComponent<MeshFilter>().sharedMesh.bounds.extents.x,
                Is.EqualTo(PlayerIdentityMarker.RingOuterRadius).Within(0.01f));

            Assert.That(dashEffect, Is.Not.Null);
            Assert.That(dashEffect.IsVisible, Is.False);
            Assert.That(dashEffect.EffectRoot.childCount, Is.EqualTo(1));
            Assert.That(dashEffect.SmokeRoot, Is.Not.Null);
            Assert.That(dashEffect.SmokeRoot.gameObject.name, Is.EqualTo("Dash Smoke Puffs"));
            Assert.That(dashEffect.SmokeRoot.parent, Is.EqualTo(dashEffect.EffectRoot));
            Assert.That(dashEffect.SmokeRoot.childCount, Is.EqualTo(5));
            Assert.That(dashEffect.EffectRoot.GetComponentsInChildren<Transform>(true)
                    .Any(child => child.name.StartsWith("Dash Streak")),
                Is.False,
                "The cyan straight-line dash effect must be removed completely.");
            Assert.That(dashEffect.EffectRoot.GetComponentsInChildren<Collider>(true), Is.Empty,
                "Dash smoke must be visual-only.");
            Assert.That(dashEffect.SmokeRoot.GetComponentsInChildren<Collider>(true), Is.Empty,
                "Dash smoke must be visual-only.");

            input.SetTouchMove(Vector2.right);
            input.TouchDash();
            yield return null;
            Assert.That(player.IsDashing, Is.True);
            Assert.That(dashEffect.IsVisible, Is.True);
            Assert.That(dashEffect.SmokeRoot.gameObject.activeSelf, Is.True);
            Assert.That(dashEffect.SmokeRoot.GetComponentsInChildren<Renderer>(true), Has.Length.EqualTo(5));
            Assert.That(DashSpeedEffect.SmokeScaleMultiplier, Is.EqualTo(1.30f).Within(0.001f));
            Assert.That(dashEffect.SmokeRoot.GetChild(0).localScale.x,
                Is.GreaterThanOrEqualTo(0.16f * DashSpeedEffect.SmokeScaleMultiplier));

            var timeout = 1f;
            while (player.IsDashing && timeout > 0f)
            {
                yield return null;
                timeout -= Time.deltaTime;
            }
            yield return null;
            Assert.That(timeout, Is.GreaterThan(0f));
            Assert.That(dashEffect.IsVisible, Is.False,
                "Dash smoke must not remain after the dash ends.");
            Assert.That(dashEffect.SmokeRoot.gameObject.activeSelf, Is.False,
                "Dash smoke must disappear when the dash ends.");
            input.ClearMovement();
        }

        [UnityTest]
        public IEnumerator StationItemSocketDoesNotInheritTheStationBodyScale()
        {
            SceneManager.LoadScene("Tutorial_1_1");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var game = Object.FindFirstObjectByType<KitchenGameController>();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var counter = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None)
                .First(station => station.Kind == StationKind.Counter);
            var item = WorldItem.FromIngredient(game.Session.TakeRawLettuce());

            Assert.That(counter.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(counter.ItemAnchor.lossyScale, Is.EqualTo(Vector3.one));
            Assert.That(player.TryGive(item, out var reason), Is.True, reason);
            counter.PickupOrPlace(player);

            Assert.That(counter.SlottedItem, Is.SameAs(item));
            Assert.That(counter.ItemVisual, Is.Not.Null);
            Assert.That(counter.ItemVisual.parent, Is.SameAs(counter.ItemAnchor));
            Assert.That(counter.ItemVisual.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(counter.ItemVisual.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(counter.ItemVisual.lossyScale, Is.EqualTo(Vector3.one));
            Assert.That(Vector3.Distance(counter.ItemVisual.position, counter.ItemAnchorPosition),
                Is.LessThan(0.001f));
        }

        [UnityTest]
        public IEnumerator TutorialUsesGridPlacementWithoutSnappingPlayerMovement()
        {
            SceneManager.LoadScene("Tutorial_1_1");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var grid = Object.FindFirstObjectByType<KitchenGridRuntime>();
            var player = Object.FindFirstObjectByType<PlayerController>();
            Assert.That(grid, Is.Not.Null);
            Assert.That(grid.Definition.Validate().IsValid, Is.True);
            Assert.That(grid.Definition.Width, Is.EqualTo(10));
            Assert.That(grid.Definition.Depth, Is.EqualTo(8));

            var center = grid.CellCenter(grid.Definition.PlayerSpawn, player.transform.position.y);
            var continuousPosition = center + new Vector3(0.23f, 0f, 0.17f);
            player.Teleport(continuousPosition);
            yield return null;

            Assert.That(grid.WorldToCell(player.PhysicsPosition), Is.EqualTo(grid.Definition.PlayerSpawn));
            Assert.That(player.PhysicsPosition.x, Is.EqualTo(continuousPosition.x).Within(0.01f));
            Assert.That(player.PhysicsPosition.z, Is.EqualTo(continuousPosition.z).Within(0.01f));

            var signature = grid.Definition.DeterministicSignature();
            var reachableCount = grid.Definition.FindReachableCells().Count;
            var background = new GameObject("Moving Background");
            background.transform.position = new Vector3(40f, 3f, -12f);
            background.transform.position += new Vector3(8f, 0f, 5f);

            Assert.That(grid.Definition.DeterministicSignature(), Is.EqualTo(signature));
            Assert.That(grid.Definition.FindReachableCells().Count, Is.EqualTo(reachableCount));
            Object.Destroy(background);
        }

        [Test]
        public void ThrowTrajectoryIsLowAndWalkingSpeedIsSlightlyFaster()
        {
            var start = new Vector3(0f, 1.1f, 0f);
            var destination = new Vector3(PlayerController.ThrowDistance, 0.02f, 0f);
            var midpoint = ThrownItemMotion.EvaluateTrajectory(start, destination, 0.5f);
            var linearMidpoint = Vector3.Lerp(start, destination, 0.5f);

            Assert.That(midpoint.y - linearMidpoint.y, Is.EqualTo(ThrownItemMotion.ArcHeight).Within(0.001f));
            Assert.That(ThrownItemMotion.ArcHeight, Is.LessThan(1f));
            Assert.That(ThrownItemMotion.RollDegreesPerSecond, Is.LessThan(120f));
            Assert.That(PlayerController.WalkSpeed, Is.EqualTo(4.45f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator IngredientAndCompletedContainerCanBeThrownToCounters()
        {
            SceneManager.LoadScene("Tutorial_1_1");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var game = Object.FindFirstObjectByType<KitchenGameController>();
            var grid = Object.FindFirstObjectByType<KitchenGridRuntime>();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var stations = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None);
            var assembly = stations.Single(station => station.GridPlacement.Id == "assemble");

            player.Teleport(grid.CellCenter(new GridCoordinate(4, 2), 0.05f));
            player.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            var lettuce = WorldItem.FromIngredient(game.Session.TakeRawLettuce());
            Assert.That(player.TryGive(lettuce, out var pickupReason), Is.True, pickupReason);
            Assert.That(player.TryThrowHeld(out var throwReason), Is.True, throwReason);
            Assert.That(player.HeldItem, Is.Null);
            var landingTimeout = 1.2f;
            while (assembly.SlottedItem == null && landingTimeout > 0f)
            {
                yield return null;
                landingTimeout -= Time.deltaTime;
            }
            Assert.That(assembly.SlottedItem, Is.SameAs(lettuce));

            var topCounter = stations.Single(station => station.GridPlacement.Id == "counter.top.3");
            var container = game.Session.TakeDeliveryContainer();
            var chopped = game.Session.TakeRawLettuce();
            Assert.That(game.Session.TryChop(chopped, out var chopReason), Is.True, chopReason);
            Assert.That(game.Session.TryAssemble(container, chopped, out var assembleReason), Is.True, assembleReason);
            var completedContainer = WorldItem.FromContainer(container);

            player.Teleport(grid.CellCenter(new GridCoordinate(7, 4), 0.05f));
            var targetDirection = topCounter.transform.position - player.transform.position;
            targetDirection.y = 0f;
            player.transform.rotation = Quaternion.LookRotation(targetDirection);
            Assert.That(player.TryGive(completedContainer, out pickupReason), Is.True, pickupReason);
            Assert.That(player.TryThrowHeld(out throwReason), Is.True, throwReason);
            landingTimeout = 1.2f;
            while (topCounter.SlottedItem == null && landingTimeout > 0f)
            {
                yield return null;
                landingTimeout -= Time.deltaTime;
            }
            Assert.That(topCounter.SlottedItem, Is.SameAs(completedContainer));
        }

        [UnityTest]
        public IEnumerator ChoppedIngredientCanBeThrownDirectlyIntoMatchingPot()
        {
            SceneManager.LoadScene("Tutorial_1_2");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var game = Object.FindFirstObjectByType<KitchenGameController>();
            var grid = Object.FindFirstObjectByType<KitchenGridRuntime>();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var heat = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None)
                .Single(station => station.Kind == StationKind.PotHeatSource);
            var carrot = game.Session.TakeRawIngredient(GameIds.CarrotIngredient);
            Assert.That(game.Session.TryChop(carrot, out var chopReason), Is.True, chopReason);
            var carrotItem = WorldItem.FromIngredient(carrot);

            player.Teleport(grid.CellCenter(new GridCoordinate(6, 5), 0.05f));
            var targetDirection = heat.transform.position - player.transform.position;
            targetDirection.y = 0f;
            player.transform.rotation = Quaternion.LookRotation(targetDirection);
            Assert.That(player.TryGive(carrotItem, out var pickupReason), Is.True, pickupReason);
            Assert.That(player.TryThrowHeld(out var throwReason), Is.True, throwReason);

            var landingTimeout = 1.2f;
            while (heat.SlottedItem.Pot.Contents.Count == 0 && landingTimeout > 0f)
            {
                yield return null;
                landingTimeout -= Time.deltaTime;
            }

            Assert.That(heat.SlottedItem.Pot.Contents, Has.Count.EqualTo(1));
            Assert.That(heat.SlottedItem.Pot.Contents[0].IngredientId, Is.EqualTo(GameIds.CarrotIngredient));
            Assert.That(heat.GetComponentsInChildren<Transform>(true)
                .Any(child => child.name == "Ingredient List Indicator ingredient.carrot"), Is.True,
                string.Join(", ", heat.GetComponentsInChildren<Transform>(true).Select(child => child.name)));
            Assert.That(player.HeldItem, Is.Null);

            var rawOnion = WorldItem.FromIngredient(
                game.Session.TakeRawIngredient(GameIds.OnionIngredient));
            Assert.That(heat.CanReceiveThrown(rawOnion, out var invalidReason), Is.False);
            Assert.That(invalidReason, Does.Contain("切って"));
        }

        [UnityTest]
        public IEnumerator OffTrajectoryStationDoesNotPullThrowAndGroundItemCanBeRecovered()
        {
            SceneManager.LoadScene("Tutorial_1_1");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var game = Object.FindFirstObjectByType<KitchenGameController>();
            var grid = Object.FindFirstObjectByType<KitchenGridRuntime>();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var input = Object.FindFirstObjectByType<KitchenInputRouter>();
            var assembly = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None)
                .Single(station => station.GridPlacement != null && station.GridPlacement.Id == "assemble");
            var lettuce = WorldItem.FromIngredient(game.Session.TakeRawLettuce());

            player.Teleport(grid.CellCenter(new GridCoordinate(4, 3), 0.05f));
            player.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            var naturalLanding = player.transform.position + Vector3.forward * PlayerController.ThrowDistance;
            Assert.That(player.TryGive(lettuce, out var pickupReason), Is.True, pickupReason);
            Assert.That(player.TryThrowHeld(out var throwReason), Is.True, throwReason);

            var motion = Object.FindFirstObjectByType<ThrownItemMotion>();
            Assert.That(motion, Is.Not.Null);
            Assert.That(motion.AssistedTarget, Is.Null);
            Assert.That(Vector2.Distance(
                    new Vector2(motion.Destination.x, motion.Destination.z),
                    new Vector2(naturalLanding.x, naturalLanding.z)),
                Is.LessThan(0.01f));

            var landingTimeout = 1.2f;
            InteractableStation groundItem = null;
            while (groundItem == null && landingTimeout > 0f)
            {
                yield return null;
                landingTimeout -= Time.deltaTime;
                groundItem = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None)
                    .SingleOrDefault(station => station.Kind == StationKind.GroundItem);
            }

            Assert.That(assembly.SlottedItem, Is.Null);
            Assert.That(groundItem, Is.Not.Null);
            Assert.That(groundItem.SlottedItem, Is.SameAs(lettuce));
            Assert.That(Vector2.Distance(
                    new Vector2(groundItem.transform.position.x, groundItem.transform.position.z),
                    new Vector2(naturalLanding.x, naturalLanding.z)),
                Is.LessThan(0.01f));

            player.Teleport(grid.CellCenter(new GridCoordinate(5, 6), 0.05f));
            var pickupDirection = groundItem.transform.position - player.transform.position;
            pickupDirection.y = 0f;
            player.transform.rotation = Quaternion.LookRotation(pickupDirection);
            yield return null;
            input.TouchPickupPlace();
            yield return null;

            Assert.That(player.HeldItem, Is.SameAs(lettuce));
            Assert.That(Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None)
                .Any(station => station.Kind == StationKind.GroundItem), Is.False);
        }

        [UnityTest]
        public IEnumerator CardinalDirectionsMoveOneRenderFrameWithoutTeleportingOrDrifting()
        {
            SceneManager.LoadScene("Tutorial_1_1");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var grid = Object.FindFirstObjectByType<KitchenGridRuntime>();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var input = Object.FindFirstObjectByType<KitchenInputRouter>();
            player.Teleport(grid.CellCenter(new GridCoordinate(6, 3), 0.05f));

            var directions = new[]
            {
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(-1f, 0f),
                new Vector2(0f, -1f)
            };
            foreach (var direction in directions)
            {
                input.SetTouchMove(direction);
                var before = player.PhysicsPosition;
                yield return null;
                var delta = player.PhysicsPosition - before;
                var expectedDirection = new Vector3(direction.x, 0f, direction.y);

                Assert.That(Vector3.Dot(delta.normalized, expectedDirection), Is.GreaterThan(0.99f));
                Assert.That(delta.magnitude, Is.GreaterThan(0f));
                Assert.That(delta.magnitude, Is.LessThan(0.15f),
                    "One rendered frame must never move the player by a grid-sized distance.");
            }

            input.SetTouchMove(Vector2.zero);
            yield return null;
            Assert.That(player.HorizontalVelocity, Is.EqualTo(Vector3.zero));
        }

        [UnityTest]
        public IEnumerator OppositeDirectionChangesUseTheLatestInputWithoutWarping()
        {
            SceneManager.LoadScene("Tutorial_1_1");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var grid = Object.FindFirstObjectByType<KitchenGridRuntime>();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var input = Object.FindFirstObjectByType<KitchenInputRouter>();
            player.Teleport(grid.CellCenter(new GridCoordinate(6, 3), 0.05f));

            var directionChanges = new[]
            {
                Vector2.right, Vector2.left,
                Vector2.up, Vector2.down,
                Vector2.right, Vector2.left,
                Vector2.up, Vector2.down
            };

            foreach (var direction in directionChanges)
            {
                input.SetTouchMove(direction);
                Assert.That(input.Commands.Move, Is.EqualTo(direction),
                    "A joystick direction change must reach the movement command immediately.");

                var before = player.PhysicsPosition;
                yield return null;
                var after = player.PhysicsPosition;
                var displacement = after - before;
                var expectedDirection = new Vector3(direction.x, 0f, direction.y);

                Assert.That(displacement.magnitude, Is.GreaterThan(0.00001f));
                Assert.That(displacement.magnitude, Is.LessThan(0.15f),
                    "Direction reversal must remain one continuous rendered frame.");
                Assert.That(Vector3.Dot(displacement.normalized, expectedDirection), Is.GreaterThan(0.99f),
                    "The first step after a reversal must use the new direction, not the previous input.");
                Assert.That(Vector3.Distance(player.transform.position, player.PhysicsPosition), Is.LessThan(0.001f),
                    "The rendered transform and collision position must share one authoritative coordinate.");
            }

            input.ClearMovement();
        }

        [UnityTest]
        public IEnumerator VirtualJoystickPointerEventsReverseOnScreenWithoutLatchedInput()
        {
            SceneManager.LoadScene("Tutorial_1_1");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;
            Canvas.ForceUpdateCanvases();

            var grid = Object.FindFirstObjectByType<KitchenGridRuntime>();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var input = Object.FindFirstObjectByType<KitchenInputRouter>();
            var joystick = Object.FindFirstObjectByType<VirtualJoystick>();
            var joystickRect = joystick.GetComponent<RectTransform>();
            var eventSystem = EventSystem.current;
            var camera = Camera.main;
            player.Teleport(grid.CellCenter(new GridCoordinate(6, 3), 0.05f));

            var pointer = PointerAt(joystickRect, Vector2.right * 0.8f, 101, eventSystem);
            ExecuteEvents.Execute(joystick.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            Assert.That(input.Commands.Move.x, Is.GreaterThan(0.7f));
            var before = player.PhysicsPosition;
            var beforeScreen = camera.WorldToScreenPoint(before);
            yield return null;
            var rightDisplacement = player.PhysicsPosition - before;
            var rightScreen = camera.WorldToScreenPoint(player.PhysicsPosition) - beforeScreen;
            Assert.That(rightDisplacement.x, Is.GreaterThan(0f));
            Assert.That(rightScreen.x, Is.GreaterThan(0f));

            pointer.position = ScreenPointAt(joystickRect, Vector2.left * 0.8f);
            ExecuteEvents.Execute(joystick.gameObject, pointer, ExecuteEvents.dragHandler);
            Assert.That(input.Commands.Move.x, Is.LessThan(-0.7f));
            before = player.PhysicsPosition;
            beforeScreen = camera.WorldToScreenPoint(before);
            yield return null;
            var leftDisplacement = player.PhysicsPosition - before;
            var leftScreen = camera.WorldToScreenPoint(player.PhysicsPosition) - beforeScreen;
            Assert.That(leftDisplacement.x, Is.LessThan(0f));
            Assert.That(leftScreen.x, Is.LessThan(0f));
            Assert.That(leftDisplacement.magnitude, Is.LessThan(0.15f));

            var unrelatedPointer = PointerAt(joystickRect, Vector2.up, 202, eventSystem);
            ExecuteEvents.Execute(joystick.gameObject, unrelatedPointer, ExecuteEvents.pointerDownHandler);
            Assert.That(input.Commands.Move.x, Is.LessThan(-0.7f),
                "A second pointer must not take ownership of the movement stick.");

            ExecuteEvents.Execute(joystick.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            Assert.That(input.Commands.Move, Is.EqualTo(Vector2.zero));
            var releasedPosition = player.PhysicsPosition;
            yield return null;
            yield return null;
            Assert.That(Vector3.Distance(player.PhysicsPosition, releasedPosition), Is.LessThan(0.001f));
        }

        [UnityTest]
        public IEnumerator RenderHitchCannotAccumulateMovementIntoAWarp()
        {
            SceneManager.LoadScene("Tutorial_1_1");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var grid = Object.FindFirstObjectByType<KitchenGridRuntime>();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var input = Object.FindFirstObjectByType<KitchenInputRouter>();
            player.Teleport(grid.CellCenter(new GridCoordinate(6, 3), 0.05f));
            input.SetTouchMove(Vector2.left);
            yield return null;

            var before = player.PhysicsPosition;
            System.Threading.Thread.Sleep(150);
            yield return null;
            yield return null;
            var displacement = player.PhysicsPosition - before;

            Assert.That(displacement.x, Is.LessThan(0f));
            Assert.That(displacement.magnitude, Is.LessThan(0.25f),
                "A slow rendered frame must not replay several physics ticks as one visible warp.");
            input.ClearMovement();
        }

        [UnityTest]
        public IEnumerator CameraFramesTheKitchenSlightlyBelowTheTopHud()
        {
            SceneManager.LoadScene("Tutorial_1_1");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var grid = Object.FindFirstObjectByType<KitchenGridRuntime>();
            var definition = grid.Definition;
            var geometricCenter = new Vector3(
                definition.Space.OriginX + definition.Width * definition.Space.CellSize * 0.5f,
                0f,
                definition.Space.OriginZ + definition.Depth * definition.Space.CellSize * 0.5f);
            var viewportPoint = Camera.main.WorldToViewportPoint(geometricCenter);

            Assert.That(viewportPoint.x, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(viewportPoint.y, Is.LessThan(0.5f));
            Assert.That(viewportPoint.y, Is.GreaterThan(0.40f));
            Assert.That(Camera.main.fieldOfView,
                Is.EqualTo(TutorialSceneBootstrap.FixedKitchenFieldOfView).Within(0.01f));
            Assert.That(Camera.main.fieldOfView, Is.LessThan(43f),
                "The kitchen should appear about fifteen percent larger than the prior 48-degree framing.");
            Assert.That(Vector3.Dot(Camera.main.transform.forward, Vector3.down), Is.GreaterThan(0.80f),
                "The fixed kitchen camera must use the revised, slightly steeper overview angle.");
            var sun = GameObject.Find("Sun").GetComponent<Light>();
            Assert.That(sun.shadowStrength,
                Is.EqualTo(TutorialSceneBootstrap.MainLightShadowStrength).Within(0.001f));
            Assert.That(sun.shadowStrength, Is.LessThan(0.40f),
                "Kitchen cast shadows should remain light enough for mobile readability.");
        }

        [UnityTest]
        public IEnumerator PlayerStopsBeforeWallAndRemainsStillAfterInputReset()
        {
            SceneManager.LoadScene("Tutorial_1_1");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var grid = Object.FindFirstObjectByType<KitchenGridRuntime>();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var input = Object.FindFirstObjectByType<KitchenInputRouter>();
            var playerCollider = player.GetComponent<CharacterController>();
            var wall = GameObject.Find("Wall [0,1]");
            Assert.That(wall, Is.Not.Null);
            var wallCollider = wall.GetComponent<BoxCollider>();
            Assert.That(playerCollider, Is.Not.Null);
            Assert.That(player.GetComponent<Rigidbody>(), Is.Null,
                "CharacterController must be the sole owner of player movement.");

            player.Teleport(grid.CellCenter(new GridCoordinate(1, 1), 0.05f));
            input.SetTouchMove(Vector2.left);
            yield return null;

            for (var step = 0; step < 30; step++)
            {
                var before = player.PhysicsPosition;
                yield return null;
                var after = player.PhysicsPosition;
                Assert.That(Vector3.Distance(before, after), Is.LessThan(0.15f),
                    "Collision handling must never teleport the player.");
                Assert.That(after.x - playerCollider.radius,
                    Is.GreaterThanOrEqualTo(wallCollider.bounds.max.x - playerCollider.skinWidth - 0.01f),
                    "The player capsule may enter only its configured skin width, never the visible wall.");
            }

            input.ClearMovement();
            yield return null;
            var stoppedPosition = player.PhysicsPosition;
            for (var step = 0; step < 8; step++)
            {
                yield return null;
            }

            Assert.That(Vector3.Distance(player.PhysicsPosition, stoppedPosition), Is.LessThan(0.001f),
                "Cleared movement input must not remain latched.");
            Assert.That(player.HorizontalVelocity, Is.EqualTo(Vector3.zero));
        }

        [UnityTest]
        public IEnumerator PlayerCrossesVisualTileSeamsOnOneContinuousFloor()
        {
            SceneManager.LoadScene("Tutorial_1_1");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var grid = Object.FindFirstObjectByType<KitchenGridRuntime>();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var input = Object.FindFirstObjectByType<KitchenInputRouter>();
            var continuousFloor = GameObject.Find("Continuous Floor Collider");
            Assert.That(continuousFloor, Is.Not.Null);
            Assert.That(continuousFloor.GetComponent<BoxCollider>(), Is.Not.Null);

            var gridKitchen = GameObject.Find("Grid Kitchen");
            var collidingVisualTiles = gridKitchen.GetComponentsInChildren<BoxCollider>()
                .Where(collider => collider.gameObject.name.StartsWith("Floor ["))
                .ToArray();
            Assert.That(collidingVisualTiles, Is.Empty,
                "Visual tile seams must not participate in player collision.");

            player.Teleport(grid.CellCenter(new GridCoordinate(6, 3), 0.05f));
            var directions = new[] { Vector2.right, Vector2.up, Vector2.left, Vector2.down };
            var previous = player.PhysicsPosition;
            var initialY = previous.y;
            foreach (var direction in directions)
            {
                input.SetTouchMove(direction);
                var elapsed = 0f;
                while (elapsed < 0.35f)
                {
                    yield return null;
                    elapsed += Time.deltaTime;
                    var current = player.PhysicsPosition;
                    var planarDisplacement = Vector2.Distance(
                        new Vector2(previous.x, previous.z),
                        new Vector2(current.x, current.z));
                    Assert.That(planarDisplacement, Is.LessThan(0.2f));
                    Assert.That(Mathf.Abs(current.y - initialY), Is.LessThan(0.08f));
                    previous = current;
                }
            }

            input.SetTouchMove(Vector2.zero);
        }

        [Test]
        public void EmptyDeliveryContainerUsesSharedBodyAndFullyOpenLid()
        {
            var parent = new GameObject("Container Visual Test Root");
            try
            {
                var item = WorldItem.FromContainer(new DeliveryContainer(GameIds.CommonDeliveryContainer));
                var root = WorldItemVisualFactory.Create(item, parent.transform);
                var visual = root.GetComponentInChildren<DeliveryContainerVisual>();

                Assert.That(visual, Is.Not.Null);
                Assert.That(visual.IsLidClosed, Is.False);
                Assert.That(
                    Quaternion.Angle(
                        Quaternion.Euler(DeliveryContainerVisual.OpenLidAngle, 0f, 0f),
                        visual.LidPivot.localRotation),
                    Is.LessThan(0.01f));
                Assert.That(visual.ContentRoot.childCount, Is.Zero);
                Assert.That(
                    visual.GetComponentsInChildren<Transform>()
                        .Count(child => child.name.StartsWith("Teal Corner Guard")),
                    Is.EqualTo(4));
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void CompletedDeliveryContainerClosesClearLidAndShowsFood()
        {
            var recipe = new RecipeDefinition(
                GameIds.LettuceSaladRecipe,
                new[]
                {
                    new RecipeComponent(GameIds.LettuceIngredient, IngredientPreparation.Chopped, 1)
                });
            var container = new DeliveryContainer(GameIds.CommonDeliveryContainer);
            Assert.That(
                container.TryAdd(
                    new IngredientItem(GameIds.LettuceIngredient, IngredientPreparation.Chopped),
                    recipe,
                    out var reason),
                Is.True,
                reason);

            var parent = new GameObject("Completed Container Visual Test Root");
            try
            {
                var root = WorldItemVisualFactory.Create(WorldItem.FromContainer(container), parent.transform);
                var visual = root.GetComponentInChildren<DeliveryContainerVisual>();

                Assert.That(visual, Is.Not.Null);
                Assert.That(visual.IsLidClosed, Is.True);
                Assert.That(
                    Quaternion.Angle(Quaternion.identity, visual.LidPivot.localRotation),
                    Is.LessThan(0.01f));
                Assert.That(visual.ContentRoot.childCount, Is.GreaterThan(0));
                Assert.That(
                    visual.GetComponentsInChildren<Transform>().Any(child => child.name == "Clear Lid"),
                    Is.True);
                var lid = visual.GetComponentsInChildren<Transform>()
                    .Single(child => child.name == "Clear Lid");
                Assert.That(lid.GetComponent<Renderer>().sharedMaterial.color.a, Is.LessThanOrEqualTo(0.20f),
                    "The completed lid window must preserve a clear view of the food.");
                var highestFoodPoint = visual.ContentRoot.GetComponentsInChildren<Renderer>()
                    .Max(renderer => renderer.bounds.max.y);
                Assert.That(highestFoodPoint, Is.LessThanOrEqualTo(lid.GetComponent<Renderer>().bounds.max.y + 0.01f),
                    "Completed food must sit visibly beneath the closed lid instead of clipping through it.");
                Assert.That(
                    visual.GetComponentsInChildren<Transform>()
                        .Count(child => child.name.StartsWith("Lid ") && child.name.EndsWith("Edge")),
                    Is.EqualTo(4),
                    "The clear window needs a readable four-sided lid frame.");
                Assert.That(
                    visual.GetComponentsInChildren<Transform>().Any(child => child.name == "Lid Handle"),
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void PreparedItemsAndMealsShowEveryIngredientIndicatorAboveFoodWithoutOverlap()
        {
            var parent = new GameObject("Ingredient Indicator Test Root");
            try
            {
                var emptyPotRoot = WorldItemVisualFactory.Create(
                    WorldItem.FromPot(new CookingPot(GameIds.CommonCookingPot)),
                    parent.transform);
                var emptyPotRim = emptyPotRoot.GetComponentsInChildren<Transform>(true)
                    .Single(child => child.name == "Pot Outer Rim");
                var emptyPotCavity = emptyPotRoot.GetComponentsInChildren<Transform>(true)
                    .Single(child => child.name == "Pot Inner Cavity");
                Assert.That(emptyPotRoot.GetComponentsInChildren<Transform>(true)
                    .Count(child => child.name.StartsWith("Pot Outer Rim")), Is.EqualTo(4),
                    "The pot rim must be an open frame rather than a solid cap over the cavity.");
                Assert.That(emptyPotCavity.localScale.x, Is.LessThan(emptyPotRim.localScale.x),
                    "An empty pot needs a smaller recessed inner cavity inside its visible rim.");
                Assert.That(emptyPotCavity.localPosition.y + emptyPotCavity.localScale.y,
                    Is.LessThan(emptyPotRim.localPosition.y + emptyPotRim.localScale.y - 0.10f),
                    "The dark inner surface must sit visibly below the rim rather than cap the pot.");

                var raw = WorldItemVisualFactory.Create(
                    WorldItem.FromIngredient(new IngredientItem(
                        GameIds.CarrotIngredient,
                        IngredientPreparation.Raw)),
                    parent.transform);
                Assert.That(
                    raw.GetComponentsInChildren<Transform>(true)
                        .Count(child => child.name.StartsWith("Ingredient List Indicator")),
                    Is.Zero,
                    "Unprocessed raw ingredients must remain readable without a redundant indicator.");

                var chopped = WorldItemVisualFactory.Create(
                    WorldItem.FromIngredient(new IngredientItem(
                        GameIds.CarrotIngredient,
                        IngredientPreparation.Chopped)),
                    parent.transform);
                var choppedIndicator = chopped.GetComponentsInChildren<Transform>(true)
                    .Single(child => child.name.StartsWith("Ingredient List Indicator"));
                Assert.That(WorldItemVisualFactory.IngredientIndicatorCardSize,
                    Is.EqualTo(0.70f).Within(0.001f));
                Assert.That(WorldItemVisualFactory.IngredientIndicatorVerticalClearance,
                    Is.EqualTo(0.78f).Within(0.001f));
                var choppedSourceCard = choppedIndicator.Find("Ingredient Source Card ingredient.carrot");
                Assert.That(choppedSourceCard, Is.Not.Null);
                Assert.That(choppedSourceCard.localScale.x,
                    Is.EqualTo(WorldItemVisualFactory.IngredientIndicatorCardSize).Within(0.001f));
                var choppedSourceMaterial = choppedSourceCard.GetComponent<Renderer>().sharedMaterial;
                Assert.That(choppedSourceMaterial.mainTexture.name,
                    Is.EqualTo("ingredient_carrot_source_card_sv2"));
                Assert.That(choppedSourceMaterial.GetFloat("_ContentScale"),
                    Is.EqualTo(WorldItemVisualFactory.IngredientIndicatorContentScale).Within(0.001f),
                    "The ingredient photograph must fill the white circle instead of retaining card margins.");
                Assert.That(choppedSourceMaterial.GetFloat("_BackgroundRadius"),
                    Is.EqualTo(WorldItemVisualFactory.IngredientIndicatorBackgroundRadius).Within(0.001f));
                Assert.That(choppedSourceMaterial.GetFloat("_SubjectOverflow"),
                    Is.EqualTo(1f).Within(0.001f),
                    "Prepared-content ingredients may extend slightly beyond the white circle.");
                Assert.That(choppedIndicator.Find("Indicator Badge"), Is.Null,
                    "Prepared-item indicators should use IngredientSourceCards instead of procedural badges.");
                var choppedAlignment = choppedSourceCard.GetComponent<CameraAlignedIngredientIcon>();
                Assert.That(choppedAlignment, Is.Not.Null);
                var alignedWorldRotation = choppedSourceCard.rotation;
                var initialLocalRotation = choppedSourceCard.localRotation;
                foreach (var yaw in new[] { 90f, 180f, 270f })
                {
                    chopped.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                    choppedAlignment.RefreshNow();
                    Assert.That(Quaternion.Angle(choppedSourceCard.rotation, alignedWorldRotation),
                        Is.LessThan(0.01f),
                        "Turning a held item with the player must not rotate its ingredient photograph on screen.");
                    Assert.That(Vector3.Dot(choppedSourceCard.forward, Vector3.up),
                        Is.GreaterThan(0.999f),
                        "Ingredient photographs must remain horizontal and readable from the fixed camera.");
                }
                Assert.That(Quaternion.Angle(choppedSourceCard.localRotation, initialLocalRotation),
                    Is.GreaterThan(1f),
                    "The icon must counter-rotate locally when its player-facing parent turns.");
                var choppedIndicatorRenderer = choppedSourceCard.GetComponent<Renderer>();
                var choppedFoodTop = chopped.GetComponentsInChildren<Renderer>()
                    .Where(renderer => renderer != choppedIndicatorRenderer)
                    .Max(renderer => renderer.bounds.max.y);
                Assert.That(choppedIndicatorRenderer.bounds.min.y,
                    Is.GreaterThan(choppedFoodTop + 0.68f),
                    "A prepared ingredient icon must float clearly above the ingredient instead of covering it.");

                var recipe = new RecipeDefinition(
                    "recipe.test.four.ingredients",
                    new[]
                    {
                        new RecipeComponent(GameIds.LettuceIngredient, IngredientPreparation.Chopped, 1),
                        new RecipeComponent(GameIds.CarrotIngredient, IngredientPreparation.Chopped, 1),
                        new RecipeComponent(GameIds.OnionIngredient, IngredientPreparation.Chopped, 1),
                        new RecipeComponent(GameIds.BeefIngredient, IngredientPreparation.Chopped, 1)
                    },
                    true);
                var pot = new CookingPot(GameIds.CommonCookingPot);
                foreach (var ingredientId in new[]
                         {
                             GameIds.LettuceIngredient,
                             GameIds.CarrotIngredient,
                             GameIds.OnionIngredient,
                             GameIds.BeefIngredient
                         })
                {
                    Assert.That(
                        pot.TryAdd(
                            new IngredientItem(ingredientId, IngredientPreparation.Chopped),
                            recipe,
                            out var addReason),
                        Is.True,
                        addReason);
                }

                var potRoot = WorldItemVisualFactory.Create(WorldItem.FromPot(pot), parent.transform);
                var indicators = potRoot.GetComponentsInChildren<Transform>(true)
                    .Where(child => child.name.StartsWith("Ingredient List Indicator"))
                    .ToArray();
                Assert.That(indicators, Has.Length.EqualTo(4));
                Assert.That(indicators.Select(indicator => indicator.localPosition.x).Distinct().Count(), Is.EqualTo(2));
                Assert.That(indicators.Select(indicator => indicator.localPosition.z).Distinct().Count(), Is.EqualTo(2));
                Assert.That(indicators.Select(indicator => indicator.GetComponentInChildren<Renderer>()
                        .sharedMaterial.mainTexture.name),
                    Is.EquivalentTo(new[]
                    {
                        "ingredient_lettuce_source_card_sv3",
                        "ingredient_carrot_source_card_sv2",
                        "ingredient_onion_source_card_sv3",
                        "ingredient_beef_source_card_sv2"
                    }));
                Assert.That(indicators.Max(indicator => Mathf.Abs(indicator.localPosition.x)) +
                            WorldItemVisualFactory.IngredientIndicatorCardSize * 0.5f,
                    Is.LessThan(KitchenProductionAssetFactory.NominalCellSize * 0.5f),
                    "A four-item 2x2 indicator group must stay inside one logical grid cell.");
                var indicatorRenderers = indicators
                    .Select(indicator => indicator.GetComponentInChildren<Renderer>())
                    .ToArray();
                var cameraAlignedIcons = indicatorRenderers
                    .Select(renderer => renderer.GetComponent<CameraAlignedIngredientIcon>())
                    .ToArray();
                Assert.That(cameraAlignedIcons, Has.All.Not.Null,
                    "Every processed-content icon must use the same camera-aligned orientation.");
                potRoot.transform.rotation = Quaternion.Euler(0f, 135f, 0f);
                foreach (var cameraAlignedIcon in cameraAlignedIcons)
                {
                    cameraAlignedIcon.RefreshNow();
                    Assert.That(Quaternion.Angle(cameraAlignedIcon.transform.rotation, alignedWorldRotation),
                        Is.LessThan(0.01f),
                        "All ingredient photographs must share one readable world orientation.");
                }
                for (var first = 0; first < indicatorRenderers.Length; first++)
                {
                    for (var second = first + 1; second < indicatorRenderers.Length; second++)
                    {
                        var firstBounds = indicatorRenderers[first].bounds;
                        var secondBounds = indicatorRenderers[second].bounds;
                        var overlapsX = firstBounds.max.x > secondBounds.min.x &&
                                        secondBounds.max.x > firstBounds.min.x;
                        var overlapsZ = firstBounds.max.z > secondBounds.min.z &&
                                        secondBounds.max.z > firstBounds.min.z;
                        Assert.That(overlapsX && overlapsZ, Is.False,
                            "Ingredient icons must have a visible gap and never overlap each other.");
                    }
                }
                var potFoodTop = potRoot.GetComponentsInChildren<Renderer>()
                    .Except(indicatorRenderers)
                    .Max(renderer => renderer.bounds.max.y);
                Assert.That(indicatorRenderers.Min(renderer => renderer.bounds.min.y),
                    Is.GreaterThan(potFoodTop + 0.68f),
                    "Every pot ingredient icon must sit above the pot, food, and steam geometry.");

                Assert.That(
                    pot.TryAdvanceHeat(CookingPot.RequiredHeatSeconds, recipe, out _, out var heatReason),
                    Is.True,
                    heatReason);
                var container = new DeliveryContainer(GameIds.CommonDeliveryContainer);
                Assert.That(pot.TryTransferTo(container, recipe, out var transferReason), Is.True, transferReason);
                var containerRoot = WorldItemVisualFactory.Create(
                    WorldItem.FromContainer(container),
                    parent.transform);
                var containerIndicators = containerRoot.GetComponentsInChildren<Transform>(true)
                    .Where(child => child.name.StartsWith("Ingredient List Indicator"))
                    .ToArray();
                Assert.That(containerIndicators, Has.Length.EqualTo(container.Components.Count),
                    "A mixed delivery container must retain the source-ingredient list above it.");
                var containerIndicatorRenderers = containerIndicators
                    .Select(indicator => indicator.GetComponentInChildren<Renderer>())
                    .ToArray();
                var containerFoodTop = containerRoot.GetComponentsInChildren<Renderer>()
                    .Except(containerIndicatorRenderers)
                    .Max(renderer => renderer.bounds.max.y);
                Assert.That(containerIndicatorRenderers.Min(renderer => renderer.bounds.min.y),
                    Is.GreaterThan(containerFoodTop + 0.68f),
                    "Completed-meal ingredient icons must clear the food and transparent lid.");
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [UnityTest]
        public IEnumerator StationArtFacesItsAuthoredGridDirectionWithoutRotatingGameplayRoots()
        {
            SceneManager.LoadScene("Tutorial_1_3");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var stations = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None);
            var directionalStations = stations
                .Where(station => station.Kind == StationKind.IngredientCrate ||
                                  station.Kind == StationKind.ContainerDispenser ||
                                  station.Kind == StationKind.PotHeatSource ||
                                  station.Kind == StationKind.TrashBin)
                .ToArray();
            Assert.That(directionalStations.Any(station => station.GridPlacement.Direction == GridDirection.North), Is.True);
            Assert.That(directionalStations.Any(station => station.GridPlacement.Direction == GridDirection.South), Is.True);

            foreach (var station in directionalStations)
            {
                var visualOrientation = station.transform.Find("Station Visual Orientation");
                Assert.That(visualOrientation, Is.Not.Null, station.name);
                var expectedFront = station.GridPlacement.Direction switch
                {
                    GridDirection.North => Vector3.forward,
                    GridDirection.East => Vector3.right,
                    GridDirection.West => Vector3.left,
                    _ => Vector3.back
                };
                Assert.That(Vector3.Angle(
                        visualOrientation.TransformDirection(Vector3.back),
                        expectedFront),
                    Is.LessThan(0.01f),
                    station.name);
                Assert.That(Quaternion.Angle(station.transform.rotation, Quaternion.identity), Is.LessThan(0.01f));
                Assert.That(Quaternion.Angle(station.ItemAnchor.localRotation, Quaternion.identity), Is.LessThan(0.01f));
            }
        }

        [Test]
        public void LettuceProgressionUsesLargeLeafClustersInEveryState()
        {
            var parent = new GameObject("Lettuce Visual Test Root");
            try
            {
                var raw = WorldItemVisualFactory.Create(
                    WorldItem.FromIngredient(new IngredientItem(
                        GameIds.LettuceIngredient,
                        IngredientPreparation.Raw)),
                    parent.transform);
                var chopped = WorldItemVisualFactory.Create(
                    WorldItem.FromIngredient(new IngredientItem(
                        GameIds.LettuceIngredient,
                        IngredientPreparation.Chopped)),
                    parent.transform);

                var rawProduction = raw.GetComponentInChildren<KitchenProductionAsset>();
                var choppedProduction = chopped.GetComponentInChildren<KitchenProductionAsset>();
                Assert.That(rawProduction, Is.Not.Null);
                Assert.That(choppedProduction, Is.Not.Null);
                Assert.That(rawProduction.AssetKey, Is.EqualTo(GameIds.LettuceIngredient + ".raw"));
                Assert.That(choppedProduction.AssetKey, Is.EqualTo(GameIds.LettuceIngredient + ".chopped"));
                Assert.That(rawProduction.GetComponentsInChildren<MeshRenderer>(), Has.Length.LessThanOrEqualTo(3));
                Assert.That(choppedProduction.GetComponentsInChildren<MeshRenderer>(), Has.Length.LessThanOrEqualTo(3));
                Assert.That(
                    rawProduction.GetComponentsInChildren<MeshRenderer>()
                        .SelectMany(renderer => renderer.sharedMaterials)
                        .Any(material => material.name == "MAT_Lettuce"),
                    Is.True);
                Assert.That(
                    choppedProduction.GetComponentsInChildren<MeshRenderer>()
                        .SelectMany(renderer => renderer.sharedMaterials)
                        .Any(material => material.name == "MAT_Lettuce"),
                    Is.True);
                Assert.That(MaterialNames(rawProduction), Does.Contain("MAT_LettuceLight"));
                Assert.That(MaterialNames(choppedProduction), Does.Contain("MAT_LettuceLight"),
                    "Whole and chopped lettuce should share the pale ribs visible in IngredientSourceCards.");

                var recipe = new RecipeDefinition(
                    GameIds.LettuceSaladRecipe,
                    new[]
                    {
                        new RecipeComponent(GameIds.LettuceIngredient, IngredientPreparation.Chopped, 1)
                    });
                var container = new DeliveryContainer(GameIds.CommonDeliveryContainer);
                Assert.That(
                    container.TryAdd(
                        new IngredientItem(GameIds.LettuceIngredient, IngredientPreparation.Chopped),
                        recipe,
                        out var reason),
                    Is.True,
                    reason);
                var completed = WorldItemVisualFactory.Create(
                    WorldItem.FromContainer(container),
                    parent.transform);

                var completedLettuce = completed.GetComponentsInChildren<KitchenProductionAsset>(true)
                    .Single(asset => asset.AssetKey == GameIds.LettuceIngredient + ".chopped");
                Assert.That(completedLettuce.gameObject.name,
                    Is.EqualTo("Salad Lettuce Card-Matched Cluster"));
                Assert.That(MaterialNames(completedLettuce), Does.Contain("MAT_LettuceLight"));
                Assert.That(
                    completed.GetComponentsInChildren<Transform>()
                        .Count(child => child.name.StartsWith("Tomato Wedge")),
                    Is.Zero,
                    "The one-component tutorial salad must not depict an unlisted tomato ingredient.");
                Assert.That(
                    completed.GetComponentsInChildren<Transform>()
                        .Count(child => child.name.StartsWith("Cucumber Round")),
                    Is.Zero,
                    "The one-component tutorial salad must not depict an unlisted cucumber ingredient.");
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [UnityTest]
        public IEnumerator ReferenceOnionAnimatesItsVisualAndSettlesWithoutMovingTheItem()
        {
            var owner = new GameObject("Onion Animation Owner");
            try
            {
                owner.transform.SetPositionAndRotation(new Vector3(3f, 2f, -1f), Quaternion.Euler(0f, 75f, 0f));
                var item = WorldItemVisualFactory.Create(WorldItem.FromIngredient(
                    new IngredientItem(GameIds.OnionIngredient, IngredientPreparation.Raw)), owner.transform);
                var production = item.GetComponentInChildren<KitchenProductionAsset>();
                Assert.That(production, Is.Not.Null, "The raw onion must use the shipping reference prefab.");
                var animation = production.GetComponent<Animation>();
                Assert.That(animation, Is.Not.Null);
                Assert.That(animation.GetClip("OnionTurntable"), Is.Not.Null);
                var pivot = production.transform.Find("Onion Presentation");
                var itemPosition = item.transform.position;
                var itemRotation = item.transform.rotation;
                yield return new WaitForSeconds(0.15f);
                Assert.That(Vector3.Distance(pivot.localScale, Vector3.one), Is.GreaterThan(0.01f),
                    "The authored settle clip must actually deform the visual during Play Mode.");
                yield return new WaitForSeconds(0.55f);
                Assert.That(Vector3.Distance(pivot.localScale, Vector3.one), Is.LessThan(0.001f));
                Assert.That(Quaternion.Angle(pivot.localRotation, Quaternion.identity), Is.LessThan(0.01f));
                Assert.That(animation.isPlaying, Is.False, "Gameplay onions settle once instead of spinning forever.");
                Assert.That(Vector3.Distance(item.transform.position, itemPosition), Is.LessThan(0.001f));
                Assert.That(Quaternion.Angle(item.transform.rotation, itemRotation), Is.LessThan(0.01f));
                Assert.That(production.GetComponentsInChildren<Collider>(), Is.Empty);
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [Test]
        public void IngredientModelsCarrySourceCardCuesAndCompletedMealsReuseThem()
        {
            var lettuce = Resources.Load<GameObject>("KitchenProduction/LettuceRaw")
                .GetComponent<KitchenProductionAsset>();
            var carrot = Resources.Load<GameObject>("KitchenProduction/CarrotRaw")
                .GetComponent<KitchenProductionAsset>();
            var onion = Resources.Load<GameObject>("KitchenProduction/OnionRaw")
                .GetComponent<KitchenProductionAsset>();
            var beef = Resources.Load<GameObject>("KitchenProduction/BeefRaw")
                .GetComponent<KitchenProductionAsset>();

            Assert.That(MaterialNames(lettuce), Does.Contain("MAT_LettuceLight"));
            Assert.That(MaterialNames(carrot), Does.Contain("MAT_CarrotGroove"));
            Assert.That(MaterialNames(onion), Does.Contain("MAT_OnionReferenceSkin"));
            Assert.That(MaterialNames(beef), Does.Contain("MAT_BeefFat"));

            var parent = new GameObject("Completed Meal Card Match Test Root");
            try
            {
                var soupRecipe = TutorialContent.CreateVegetableSoupRecipe();
                var soup = new DeliveryContainer(GameIds.CommonDeliveryContainer);
                Assert.That(soup.TryFillCookedBatch(new[]
                {
                    new IngredientItem(GameIds.CarrotIngredient, IngredientPreparation.Chopped),
                    new IngredientItem(GameIds.OnionIngredient, IngredientPreparation.Chopped)
                }, soupRecipe, out var soupReason), Is.True, soupReason);
                var soupVisual = WorldItemVisualFactory.Create(WorldItem.FromContainer(soup), parent.transform);
                var soupKeys = soupVisual.GetComponentsInChildren<KitchenProductionAsset>(true)
                    .Select(asset => asset.AssetKey)
                    .ToArray();
                Assert.That(soupKeys, Does.Contain(GameIds.CarrotIngredient + ".chopped"));
                Assert.That(soupKeys, Does.Contain(GameIds.OnionIngredient + ".chopped"));

                var hamburgerRecipe = TutorialContent.CreateHamburgerPlateRecipe();
                var hamburger = new DeliveryContainer(GameIds.CommonDeliveryContainer);
                Assert.That(hamburger.TryAdd(
                    new IngredientItem(GameIds.BeefIngredient, IngredientPreparation.Cooked),
                    hamburgerRecipe,
                    out var beefReason), Is.True, beefReason);
                Assert.That(hamburger.TryAdd(
                    new IngredientItem(GameIds.LettuceIngredient, IngredientPreparation.Chopped),
                    hamburgerRecipe,
                    out var lettuceReason), Is.True, lettuceReason);
                var hamburgerVisual = WorldItemVisualFactory.Create(
                    WorldItem.FromContainer(hamburger),
                    parent.transform);
                var hamburgerKeys = hamburgerVisual.GetComponentsInChildren<KitchenProductionAsset>(true)
                    .Select(asset => asset.AssetKey)
                    .ToArray();
                Assert.That(hamburgerKeys, Does.Contain(GameIds.BeefIngredient + ".cooked"));
                Assert.That(hamburgerKeys, Does.Contain(GameIds.LettuceIngredient + ".chopped"));
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [UnityTest]
        public IEnumerator HomeSceneBuildsEightPlayableStageButtonsWithoutAKitchen()
        {
            SceneManager.LoadScene(HomeScreenController.HomeSceneName);
            yield return null;
            HomeSceneBootstrap.EnsureInstalled();
            yield return null;

            var home = Object.FindFirstObjectByType<HomeScreenController>();
            var stageButtons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Where(button => button.name.StartsWith("Stage "))
                .OrderBy(button => button.name)
                .ToArray();

            Assert.That(home, Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<KitchenGameController>(), Is.Null);
            Assert.That(stageButtons, Has.Length.EqualTo(8));
            Assert.That(stageButtons.Select(button => button.name), Is.EqualTo(new[]
            {
                "Stage 1-1 Button",
                "Stage 1-2 Button",
                "Stage 1-3 Button",
                "Stage 1-4 Button",
                "Stage 1-5 Button",
                "Stage 1-6 Button",
                "Stage 1-7 Button",
                "Stage 1-8 Button"
            }));
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator HomeStageButtonLoadsTheSelectedKitchen()
        {
            SceneManager.LoadScene(HomeScreenController.HomeSceneName);
            yield return null;
            HomeSceneBootstrap.EnsureInstalled();
            yield return null;

            GameObject.Find("Stage 1-5 Button").GetComponent<Button>().onClick.Invoke();
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var game = Object.FindFirstObjectByType<KitchenGameController>();
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Tutorial_1_5"));
            Assert.That(game, Is.Not.Null);
            Assert.That(game.Session.Stage.Id, Is.EqualTo(GameIds.TutorialFryingStage));
        }

        [UnityTest]
        public IEnumerator FireRecoveryTutorialStartsPausedAndExtinguishingStartsService()
        {
            SceneManager.LoadScene("Tutorial_1_6");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var game = Object.FindFirstObjectByType<KitchenGameController>();
            var incident = Object.FindFirstObjectByType<FireIncidentController>();
            var cabinet = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None)
                .Single(station => station.Kind == StationKind.ExtinguisherCabinet);
            Assert.That(game.Session.Stage.Id, Is.EqualTo(GameIds.TutorialFireRecoveryStage));
            Assert.That(game.IsClockPaused, Is.True);
            Assert.That(incident, Is.Not.Null);
            Assert.That(cabinet.SlottedItem.IsTool, Is.True);
            Assert.That(cabinet.GetComponentsInChildren<Transform>(true)
                .Any(child => child.name == "Nonverbal Equipment Card"), Is.True);
            Assert.That(incident.AdvanceExtinguish(FireIncidentController.RequiredExtinguishSeconds, out var reason), Is.True, reason);
            Assert.That(incident.IsExtinguished, Is.True);
            Assert.That(game.IsClockPaused, Is.False);
        }

        [UnityTest]
        public IEnumerator DishwashingTutorialProvidesOnlyTwoPlatesAndCompleteWashLoop()
        {
            SceneManager.LoadScene("Tutorial_1_7");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var game = Object.FindFirstObjectByType<KitchenGameController>();
            var stations = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None);
            var dispenser = stations.Single(station => station.Kind == StationKind.PlateDispenser);
            Assert.That(game.Session.Stage.Id, Is.EqualTo(GameIds.TutorialDishwashingStage));
            Assert.That(game.Session.Stage.MinimumServedOrders, Is.EqualTo(3));
            Assert.That(dispenser.AvailablePlateCount, Is.EqualTo(2));
            Assert.That(stations.Any(station => station.Kind == StationKind.DishReturn), Is.True);
            Assert.That(stations.Any(station => station.Kind == StationKind.WashingSink), Is.True);
            Assert.That(dispenser.GetComponentsInChildren<Transform>(true)
                .Any(child => child.name == "Nonverbal Equipment Card"), Is.True);
        }

        [UnityTest]
        public IEnumerator CombinedTutorialBuildsCourierBikeAndAnimatedKitchenConnector()
        {
            SceneManager.LoadScene("Tutorial_1_8");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var game = Object.FindFirstObjectByType<KitchenGameController>();
            var courier = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None)
                .Single(station => station.Kind == StationKind.CourierShelf);
            var connector = Object.FindFirstObjectByType<DynamicKitchenConnector>();
            Assert.That(game.Session.Stage.Id, Is.EqualTo(GameIds.TutorialCombinedDeliveryStage));
            Assert.That(game.Session.Recipes.Count, Is.EqualTo(3));
            Assert.That(Object.FindFirstObjectByType<DeliveryBikeController>(), Is.Not.Null);
            Assert.That(courier.GetComponentsInChildren<Transform>(true)
                .Any(child => child.name == "Nonverbal Equipment Card"), Is.True);
            Assert.That(connector, Is.Not.Null);
            Assert.That(connector.GetComponent<BoxCollider>(), Is.Not.Null);
            Assert.That(connector.GetComponentsInChildren<KitchenProductionAsset>(true)
                .Any(asset => asset.AssetKey == "station.dynamic.bridge"), Is.True);
        }

        [UnityTest]
        public IEnumerator CompletedStageResultButtonReturnsToHome()
        {
            SceneManager.LoadScene("Tutorial_1_1");
            yield return null;
            TutorialSceneBootstrap.EnsureInstalled();
            yield return null;

            var game = Object.FindFirstObjectByType<KitchenGameController>();
            game.OnOrderServed(ServeResult.Success(100, 0));
            game.Session.Finish();
            yield return null;

            var resultPanel = GameObject.Find("Result Panel");
            var homeButton = GameObject.Find("HOME Button").GetComponent<Button>();
            Assert.That(resultPanel.activeInHierarchy, Is.True);
            Assert.That(homeButton.gameObject.activeInHierarchy, Is.True);

            homeButton.onClick.Invoke();
            yield return null;
            HomeSceneBootstrap.EnsureInstalled();
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(HomeScreenController.HomeSceneName));
            Assert.That(Object.FindFirstObjectByType<HomeScreenController>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<KitchenGameController>(), Is.Null);
        }

        private static string[] MaterialNames(KitchenProductionAsset asset)
        {
            return asset.GetComponentsInChildren<MeshRenderer>(true)
                .SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null)
                .Select(material => material.name)
                .Distinct()
                .ToArray();
        }

        private static Color MaterialColor(KitchenProductionAsset asset, string materialName)
        {
            var material = asset.GetComponentsInChildren<MeshRenderer>(true)
                .SelectMany(renderer => renderer.sharedMaterials)
                .First(candidate => candidate != null && candidate.name == materialName);
            var property = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
            return material.GetColor(property);
        }

        private static float MaxRgb(Color color)
        {
            return Mathf.Max(color.r, Mathf.Max(color.g, color.b));
        }

        private static PointerEventData PointerAt(
            RectTransform rect,
            Vector2 normalizedOffset,
            int pointerId,
            EventSystem eventSystem)
        {
            return new PointerEventData(eventSystem)
            {
                pointerId = pointerId,
                position = ScreenPointAt(rect, normalizedOffset)
            };
        }

        private static void AddChoppedIngredientToPot(
            InteractableStation crate,
            InteractableStation board,
            InteractableStation heat,
            PlayerController player)
        {
            crate.PickupOrPlace(player);
            board.PickupOrPlace(player);
            Assert.That(
                board.Work(player, IngredientItem.RequiredChopSeconds, out var workReason),
                Is.True,
                workReason);
            board.PickupOrPlace(player);
            heat.PickupOrPlace(player);
            Assert.That(player.HeldItem, Is.Null);
        }

        private static Vector2 ScreenPointAt(RectTransform rect, Vector2 normalizedOffset)
        {
            var localPoint = rect.rect.center + Vector2.Scale(
                normalizedOffset,
                rect.rect.size * 0.5f);
            return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(localPoint));
        }
    }
}
