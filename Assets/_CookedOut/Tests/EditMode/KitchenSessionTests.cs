using System;
using System.Linq;
using CookedOut.Application;
using CookedOut.Domain;
using CookedOut.Infrastructure;
using NUnit.Framework;

namespace CookedOut.Tests.EditMode
{
    public sealed class KitchenSessionTests
    {
        [Test]
        public void IncompleteContainerCannotBeServed()
        {
            var session = CreateSession(7u);
            session.Start();
            var result = session.TryServe(session.TakeDeliveryContainer());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Reason, Does.Contain("完成"));
            Assert.That(session.State.Score, Is.Zero);
        }

        [Test]
        public void CompletedOrderAwardsBaseScoreAndTimeTip()
        {
            var session = CreateSession(7u);
            session.Start();
            session.Tick(5f);

            var result = CompleteAndServe(session);

            Assert.That(result.Succeeded, Is.True, result.Reason);
            Assert.That(result.BaseScore, Is.EqualTo(100));
            Assert.That(result.TimeTip, Is.EqualTo(80));
            Assert.That(session.State.Score, Is.EqualTo(180));
        }

        [Test]
        public void SelfDeliveryAwardsFiftyPercentBonusForSoloService()
        {
            var session = CreateSession(7u);
            session.Start();
            session.Tick(5f);
            var lettuce = session.TakeRawLettuce();
            Assert.That(session.TryChop(lettuce, out var chopReason), Is.True, chopReason);
            var container = session.TakeDeliveryContainer();
            Assert.That(session.TryAssemble(container, lettuce, out var assembleReason), Is.True, assembleReason);

            var result = session.TryServe(container, FulfillmentMethod.SelfDelivery, 1);

            Assert.That(result.BaseScore, Is.EqualTo(100));
            Assert.That(result.TimeTip, Is.EqualTo(80));
            Assert.That(result.DeliveryBonus, Is.EqualTo(90));
            Assert.That(result.TotalScore, Is.EqualTo(270));
            Assert.That(session.State.Score, Is.EqualTo(270));
        }

        [Test]
        public void SelfDeliveryBonusUsesConfiguredPartySizeRates()
        {
            Assert.That(ScoringPolicy.ScoreSelfDeliveryBonus(200, 1), Is.EqualTo(100));
            Assert.That(ScoringPolicy.ScoreSelfDeliveryBonus(200, 2), Is.EqualTo(60));
            Assert.That(ScoringPolicy.ScoreSelfDeliveryBonus(200, 3), Is.EqualTo(40));
            Assert.That(ScoringPolicy.ScoreSelfDeliveryBonus(200, 4), Is.EqualTo(30));
        }

        [Test]
        public void ExpiredOrderAwardsNoScore()
        {
            var session = CreateSession(7u);
            session.Start();

            session.Tick(46f);

            Assert.That(session.State.Orders[0].Status, Is.EqualTo(OrderStatus.Expired));
            Assert.That(session.State.Score, Is.Zero);
            Assert.That(session.State.ExpiredCount, Is.EqualTo(1));
        }

        [Test]
        public void TutorialKeepsRunningAfterOneOrderAndSpawnsAnotherOnSchedule()
        {
            var session = CreateSession(7u);
            session.Start();
            var firstOrderId = session.State.Orders[0].Id;

            var result = CompleteAndServe(session);

            Assert.That(result.Succeeded, Is.True, result.Reason);
            Assert.That(session.State.Status, Is.EqualTo(ShiftStatus.Running));
            Assert.That(session.State.ServedCount, Is.EqualTo(1));
            session.Tick(session.Stage.OrderSpawnIntervalSeconds);
            Assert.That(session.State.FirstActiveOrder(), Is.Not.Null);
            Assert.That(session.State.FirstActiveOrder().Id, Is.Not.EqualTo(firstOrderId));
        }

        [Test]
        public void PeriodicOrdersNeverExceedFiveVisibleTickets()
        {
            var session = CreateSession(17u);
            session.Start();
            var maximumVisible = session.State.Orders.Count;

            for (var elapsed = 0f; elapsed < session.Stage.ShiftDurationSeconds; elapsed += 0.5f)
            {
                session.Tick(0.5f);
                maximumVisible = Math.Max(maximumVisible, session.State.Orders.Count);
            }

            Assert.That(maximumVisible, Is.GreaterThanOrEqualTo(3));
            Assert.That(maximumVisible, Is.LessThanOrEqualTo(session.Stage.MaxVisibleOrders));
        }

        [Test]
        public void ExpiredOrderRemainsForExitAnimationThenLeavesQueue()
        {
            var session = CreateSession(23u);
            session.Start();
            var expiredId = session.State.Orders[0].Id;

            session.Tick(session.Stage.OrderDurationSeconds + 0.01f);

            Assert.That(session.State.Orders.Any(order => order.Id == expiredId), Is.True);
            Assert.That(session.State.Orders.Single(order => order.Id == expiredId).Status,
                Is.EqualTo(OrderStatus.Expired));
            session.Tick(session.Stage.OrderTicketExitSeconds * 0.5f);
            Assert.That(session.State.Orders.Any(order => order.Id == expiredId), Is.True);
            session.Tick(session.Stage.OrderTicketExitSeconds * 0.6f);
            Assert.That(session.State.Orders.Any(order => order.Id == expiredId), Is.False);
        }

        [TestCase(0, 0)]
        [TestCase(100, 1)]
        [TestCase(150, 2)]
        [TestCase(180, 3)]
        public void ShiftEndCalculatesStarsFromScore(int score, int expectedStars)
        {
            var stage = TutorialStage.Create();
            var state = new ShiftState(stage);
            state.Start();
            state.RecordServed(score);

            state.Finish(stage);

            Assert.That(state.Stars, Is.EqualTo(expectedStars));
        }

        [Test]
        public void SameSeedAndActionsProduceSameOrderOutcome()
        {
            var first = RunDeterministicSequence(12345u);
            var second = RunDeterministicSequence(12345u);

            Assert.That(second.OrderId, Is.EqualTo(first.OrderId));
            Assert.That(second.Score, Is.EqualTo(first.Score));
            Assert.That(second.Stars, Is.EqualTo(first.Stars));
        }

        [Test]
        public void ConfiguredIngredientSourceCreatesMatchingRawIngredient()
        {
            var session = CreateSession(7u);
            session.Start();

            var ingredient = session.TakeRawIngredient("ingredient.test.tomato");

            Assert.That(ingredient.IngredientId, Is.EqualTo("ingredient.test.tomato"));
            Assert.That(ingredient.Preparation, Is.EqualTo(IngredientPreparation.Raw));
        }

        [Test]
        public void TutorialProgressKeepsBestStarsAndUnlocksAfterStageOneThree()
        {
            var store = new InMemorySaveStore();
            var first = new TutorialProgression(store);
            first.Load();

            first.RecordStageResult(GameIds.TutorialStage, 2);
            first.RecordStageResult(GameIds.TutorialStage, 1);
            first.RecordStageResult(GameIds.TutorialSoupStage, 1);
            var completed = first.RecordStageResult(GameIds.TutorialThrowDeliveryStage, 1);

            Assert.That(completed.StageOneStars, Is.EqualTo(2));
            Assert.That(completed.StageTwoStars, Is.EqualTo(1));
            Assert.That(completed.StageThreeStars, Is.EqualTo(1));
            Assert.That(completed.MultiplayerUnlocked, Is.True);
            Assert.That(completed.BeginnerOutingsUnlocked, Is.True);

            var reloaded = new TutorialProgression(store);
            reloaded.Load();
            Assert.That(reloaded.Current.StageOneStars, Is.EqualTo(2));
            Assert.That(reloaded.Current.MultiplayerUnlocked, Is.True);
        }

        [Test]
        public void InvalidTutorialProgressFallsBackToLockedState()
        {
            var store = new InMemorySaveStore();
            store.Save(TutorialProgression.SaveSlotId, "broken|save");
            var progression = new TutorialProgression(store);

            progression.Load();

            Assert.That(progression.Current.StageOneStars, Is.Zero);
            Assert.That(progression.Current.MultiplayerUnlocked, Is.False);
        }

        [Test]
        public void HamburgerRequiresCookedBeefAndChoppedLettuceToMergeInContainer()
        {
            var session = CreateFryingSession();
            session.Start();
            var beef = session.TakeRawIngredient(GameIds.BeefIngredient);
            var pan = session.TakeFryingPan();

            Assert.That(session.TryAddToPan(pan, beef, out var reason), Is.False);
            Assert.That(reason, Does.Contain("切って"));
            Assert.That(session.TryChop(beef, out reason), Is.True, reason);
            Assert.That(session.TryAddToPan(pan, beef, out reason), Is.True, reason);
            Assert.That(session.TryAdvancePan(
                pan,
                FryingPan.RequiredCookSeconds,
                out var cooked,
                out var burned,
                out reason), Is.True, reason);
            Assert.That(cooked, Is.True);
            Assert.That(burned, Is.False);

            var container = session.TakeDeliveryContainer();
            Assert.That(session.TryFillContainerFromPan(pan, container, out reason), Is.True, reason);
            Assert.That(container.IsComplete, Is.False, "The cooked patty still needs its lettuce side.");
            var lettuce = session.TakeRawIngredient(GameIds.LettuceIngredient);
            Assert.That(session.TryChop(lettuce, out reason), Is.True, reason);
            Assert.That(session.TryAssemble(container, lettuce, out reason), Is.True, reason);
            Assert.That(container.IsComplete, Is.True);
            Assert.That(container.CompletedRecipeId, Is.EqualTo(GameIds.HamburgerPlateRecipe));
            Assert.That(session.TryServe(container).Succeeded, Is.True);
        }

        [Test]
        public void FryingTutorialAwardsNoStarsUntilTwoMealsAreServed()
        {
            var stage = TutorialFryingStage.Create();
            var state = new ShiftState(stage);
            state.Start();
            state.RecordServed(400);
            state.Finish(stage);

            Assert.That(stage.MinimumServedOrders, Is.EqualTo(2));
            Assert.That(state.Stars, Is.Zero);

            var cleared = new ShiftState(stage);
            cleared.Start();
            cleared.RecordServed(150);
            cleared.RecordServed(150);
            cleared.Finish(stage);
            Assert.That(cleared.Stars, Is.EqualTo(3));
        }

        [Test]
        public void HamburgerBurnsAfterGracePeriodAndCannotBeServed()
        {
            var session = CreateFryingSession();
            session.Start();
            var beef = session.TakeRawIngredient(GameIds.BeefIngredient);
            Assert.That(session.TryChop(beef, out var reason), Is.True, reason);
            var pan = session.TakeFryingPan();
            Assert.That(session.TryAddToPan(pan, beef, out reason), Is.True, reason);

            Assert.That(session.TryAdvancePan(
                pan,
                FryingPan.RequiredCookSeconds,
                out var cooked,
                out var burned,
                out reason), Is.True, reason);
            Assert.That(cooked, Is.True);
            Assert.That(burned, Is.False);
            Assert.That(pan.RemainingBurnSeconds, Is.EqualTo(FryingPan.BurnGraceSeconds).Within(0.001f));

            Assert.That(session.TryAdvancePan(
                pan,
                FryingPan.BurnGraceSeconds - 0.01f,
                out _,
                out burned,
                out reason), Is.True, reason);
            Assert.That(burned, Is.False, "The full extended grace period must remain playable.");
            Assert.That(pan.IsCooked, Is.True);

            Assert.That(session.TryAdvancePan(
                pan,
                0.01f,
                out _,
                out burned,
                out reason), Is.True, reason);
            Assert.That(burned, Is.True);
            Assert.That(pan.IsBurned, Is.True);
            Assert.That(session.TryFillContainerFromPan(pan, session.TakeDeliveryContainer(), out reason), Is.False);
            Assert.That(reason, Does.Contain("焦げ"));
            Assert.That(pan.DiscardBurnedContents(), Is.True);
            Assert.That(pan.IsEmpty, Is.True);
        }

        [Test]
        public void CombinedTutorialRequiresCourierThenSelfDelivery()
        {
            var session = new KitchenSession(
                TutorialCombinedDeliveryStage.Create(),
                new[]
                {
                    TutorialContent.CreateLettuceSaladRecipe(),
                    TutorialContent.CreateVegetableSoupRecipe(),
                    TutorialContent.CreateHamburgerPlateRecipe()
                },
                TutorialOrders.CreateCombinedDeliveryOrders(),
                new SeededRandomSource(1801u),
                new FixedClock(new DateTimeOffset(2026, 9, 12, 0, 0, 0, TimeSpan.Zero)),
                new NullAnalyticsSink());
            session.Start();

            var salad = session.TakeDeliveryContainer();
            Assert.That(session.TryAssemble(salad,
                new IngredientItem(GameIds.LettuceIngredient, IngredientPreparation.Chopped), out var reason), Is.True, reason);
            Assert.That(session.TryServe(salad, FulfillmentMethod.SelfDelivery).Succeeded, Is.False);
            Assert.That(session.TryServe(salad, FulfillmentMethod.Courier).Succeeded, Is.True);

            session.Tick(session.Stage.OrderSpawnIntervalSeconds);
            var soup = session.TakeDeliveryContainer();
            var cookedSoup = new[]
            {
                new IngredientItem(GameIds.CarrotIngredient, IngredientPreparation.Chopped),
                new IngredientItem(GameIds.OnionIngredient, IngredientPreparation.Chopped)
            };
            Assert.That(soup.TryFillCookedBatch(cookedSoup, TutorialContent.CreateVegetableSoupRecipe(), out reason), Is.True, reason);
            Assert.That(session.TryServe(soup, FulfillmentMethod.Courier).Succeeded, Is.False);
            Assert.That(session.TryServe(soup, FulfillmentMethod.SelfDelivery).Succeeded, Is.True);
        }

        [Test]
        public void CompletingStageEightUnlocksNormalBusinessAndPersistsAllStages()
        {
            var store = new InMemorySaveStore();
            var progression = new TutorialProgression(store);
            progression.Load();
            progression.RecordStageResult(GameIds.TutorialFireRecoveryStage, 1);
            progression.RecordStageResult(GameIds.TutorialDishwashingStage, 2);
            var result = progression.RecordStageResult(GameIds.TutorialCombinedDeliveryStage, 3);

            Assert.That(result.NormalBusinessUnlocked, Is.True);
            var reloaded = new TutorialProgression(store);
            reloaded.Load();
            Assert.That(reloaded.Current.StageSixStars, Is.EqualTo(1));
            Assert.That(reloaded.Current.StageSevenStars, Is.EqualTo(2));
            Assert.That(reloaded.Current.StageEightStars, Is.EqualTo(3));
        }

        private static KitchenSession CreateSession(uint seed)
        {
            return new KitchenSession(
                TutorialStage.Create(),
                TutorialContent.CreateLettuceSaladRecipe(),
                TutorialOrders.CreateLettuceSaladOrder(),
                new SeededRandomSource(seed),
                new FixedClock(new DateTimeOffset(2026, 9, 11, 0, 0, 0, TimeSpan.Zero)),
                new NullAnalyticsSink());
        }

        private static KitchenSession CreateFryingSession()
        {
            return new KitchenSession(
                TutorialFryingStage.Create(),
                TutorialContent.CreateHamburgerPlateRecipe(),
                TutorialOrders.CreateHamburgerPlateOrder(),
                new SeededRandomSource(1501u),
                new FixedClock(new DateTimeOffset(2026, 9, 11, 0, 0, 0, TimeSpan.Zero)),
                new NullAnalyticsSink());
        }

        private static ServeResult CompleteAndServe(KitchenSession session)
        {
            var lettuce = session.TakeRawLettuce();
            Assert.That(session.TryChop(lettuce, out var chopReason), Is.True, chopReason);
            var container = session.TakeDeliveryContainer();
            Assert.That(session.TryAssemble(container, lettuce, out var assembleReason), Is.True, assembleReason);
            return session.TryServe(container);
        }

        private static (string OrderId, int Score, int Stars) RunDeterministicSequence(uint seed)
        {
            var session = CreateSession(seed);
            session.Start();
            session.Tick(7.25f);
            var orderId = session.State.Orders[0].Id;
            var result = CompleteAndServe(session);
            Assert.That(result.Succeeded, Is.True, result.Reason);
            session.Finish();
            return (orderId, session.State.Score, session.State.Stars);
        }
    }
}
