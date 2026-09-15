using CookedOut.Domain;
using NUnit.Framework;

namespace CookedOut.Tests.EditMode
{
    public sealed class FoodPreparationTests
    {
        [Test]
        public void ChoppingRequiresThreeSecondsOfWork()
        {
            Assert.That(IngredientItem.RequiredChopSeconds, Is.EqualTo(3f));
        }

        [Test]
        public void ChoppingRawLettuceChangesItsState()
        {
            var lettuce = new IngredientItem(GameIds.LettuceIngredient, IngredientPreparation.Raw);

            var succeeded = lettuce.TryChop(out var reason);

            Assert.That(succeeded, Is.True, reason);
            Assert.That(lettuce.Preparation, Is.EqualTo(IngredientPreparation.Chopped));
        }

        [Test]
        public void ChoppingProgressAccumulatesAndSurvivesItemCopy()
        {
            var lettuce = new IngredientItem(GameIds.LettuceIngredient, IngredientPreparation.Raw);

            var advanced = lettuce.TryAdvanceChop(
                IngredientItem.RequiredChopSeconds * 0.4f,
                out var completed,
                out var reason);

            Assert.That(advanced, Is.True, reason);
            Assert.That(completed, Is.False);
            Assert.That(lettuce.Preparation, Is.EqualTo(IngredientPreparation.Raw));
            Assert.That(lettuce.PreparationProgress, Is.EqualTo(0.4f).Within(0.001f));

            var copy = lettuce.Copy();
            Assert.That(copy.PreparationProgress, Is.EqualTo(0.4f).Within(0.001f));

            advanced = lettuce.TryAdvanceChop(
                IngredientItem.RequiredChopSeconds * 0.6f,
                out completed,
                out reason);

            Assert.That(advanced, Is.True, reason);
            Assert.That(completed, Is.True);
            Assert.That(lettuce.Preparation, Is.EqualTo(IngredientPreparation.Chopped));
            Assert.That(lettuce.PreparationProgress, Is.EqualTo(1f));
        }

        [Test]
        public void RawLettuceDoesNotCompleteSalad()
        {
            var recipe = TutorialContent.CreateLettuceSaladRecipe();
            var container = new DeliveryContainer(GameIds.CommonDeliveryContainer);

            var succeeded = container.TryAdd(
                new IngredientItem(GameIds.LettuceIngredient, IngredientPreparation.Raw),
                recipe,
                out var reason);

            Assert.That(succeeded, Is.False);
            Assert.That(reason, Does.Contain("切って"));
            Assert.That(container.IsComplete, Is.False);
            Assert.That(container.Components, Is.Empty);
        }

        [Test]
        public void ChoppedLettuceCompletesSaladInDeliveryContainer()
        {
            var recipe = TutorialContent.CreateLettuceSaladRecipe();
            var container = new DeliveryContainer(GameIds.CommonDeliveryContainer);

            container.TryAdd(
                new IngredientItem(GameIds.LettuceIngredient, IngredientPreparation.Chopped),
                recipe,
                out var reason);

            Assert.That(container.IsComplete, Is.True, reason);
            Assert.That(container.CompletedRecipeId, Is.EqualTo(GameIds.LettuceSaladRecipe));
        }

        [Test]
        public void VegetableSoupRequiresBothChoppedIngredientsAndContinuousHeatingProgress()
        {
            var recipe = TutorialContent.CreateVegetableSoupRecipe();
            var pot = new CookingPot(GameIds.CommonCookingPot);
            var carrot = new IngredientItem(GameIds.CarrotIngredient, IngredientPreparation.Raw);
            var onion = new IngredientItem(GameIds.OnionIngredient, IngredientPreparation.Raw);

            Assert.That(pot.TryAdd(carrot, recipe, out var rawReason), Is.False);
            Assert.That(rawReason, Does.Contain("切って"));
            Assert.That(carrot.TryChop(out var chopReason), Is.True, chopReason);
            Assert.That(onion.TryChop(out chopReason), Is.True, chopReason);
            Assert.That(pot.TryAdd(carrot, recipe, out var addReason), Is.True, addReason);
            Assert.That(
                pot.TryAdvanceHeat(1f, recipe, out _, out var missingReason),
                Is.False);
            Assert.That(missingReason, Does.Contain("揃って"));
            Assert.That(pot.TryAdd(onion, recipe, out addReason), Is.True, addReason);

            Assert.That(
                pot.TryAdvanceHeat(CookingPot.RequiredHeatSeconds * 0.4f, recipe, out var completed, out var heatReason),
                Is.True,
                heatReason);
            Assert.That(completed, Is.False);
            Assert.That(pot.HeatProgress, Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(pot.IsCooked, Is.False);

            Assert.That(
                pot.TryAdvanceHeat(CookingPot.RequiredHeatSeconds * 0.6f, recipe, out completed, out heatReason),
                Is.True,
                heatReason);
            Assert.That(completed, Is.True);
            Assert.That(pot.IsCooked, Is.True);
        }

        [Test]
        public void CookedSoupTransfersToEmptyContainerAndResetsThePot()
        {
            var recipe = TutorialContent.CreateVegetableSoupRecipe();
            var pot = new CookingPot(GameIds.CommonCookingPot);
            var carrot = new IngredientItem(GameIds.CarrotIngredient, IngredientPreparation.Chopped);
            var onion = new IngredientItem(GameIds.OnionIngredient, IngredientPreparation.Chopped);
            var container = new DeliveryContainer(GameIds.CommonDeliveryContainer);
            Assert.That(pot.TryAdd(carrot, recipe, out var reason), Is.True, reason);
            Assert.That(pot.TryAdd(onion, recipe, out reason), Is.True, reason);
            Assert.That(
                pot.TryAdvanceHeat(CookingPot.RequiredHeatSeconds, recipe, out _, out reason),
                Is.True,
                reason);

            Assert.That(pot.TryTransferTo(container, recipe, out reason), Is.True, reason);

            Assert.That(container.IsComplete, Is.True);
            Assert.That(container.CompletedRecipeId, Is.EqualTo(GameIds.VegetableSoupRecipe));
            Assert.That(pot.IsEmpty, Is.True);
            Assert.That(pot.IsCooked, Is.False);
            Assert.That(pot.HeatProgress, Is.Zero);
        }

        [Test]
        public void HeatedRecipeCannotBeCompletedByAddingIngredientsDirectlyToContainer()
        {
            var recipe = TutorialContent.CreateVegetableSoupRecipe();
            var container = new DeliveryContainer(GameIds.CommonDeliveryContainer);
            var carrot = new IngredientItem(GameIds.CarrotIngredient, IngredientPreparation.Chopped);

            Assert.That(container.TryAdd(carrot, recipe, out var reason), Is.False);
            Assert.That(reason, Does.Contain("加熱"));
            Assert.That(container.Components, Is.Empty);
        }

        [Test]
        public void ReusablePlateMustBeWashedContinuouslyBeforeItCanBeReused()
        {
            var recipe = TutorialContent.CreateLettuceSaladRecipe();
            var plate = new DeliveryContainer(GameIds.ReusablePlate);
            var lettuce = new IngredientItem(GameIds.LettuceIngredient, IngredientPreparation.Chopped);
            Assert.That(plate.TryAdd(lettuce, recipe, out var reason), Is.True, reason);
            Assert.That(plate.MarkDirtyAfterServing(), Is.True);
            Assert.That(plate.TryAdd(lettuce, recipe, out reason), Is.False);
            Assert.That(reason, Does.Contain("洗って"));

            Assert.That(plate.TryAdvanceWash(1f, out var completed, out reason), Is.True, reason);
            Assert.That(completed, Is.False);
            Assert.That(plate.WashProgress, Is.EqualTo(1f / DeliveryContainer.RequiredWashSeconds).Within(0.001f));
            Assert.That(plate.TryAdvanceWash(DeliveryContainer.RequiredWashSeconds - 1f, out completed, out reason), Is.True, reason);
            Assert.That(completed, Is.True);
            Assert.That(plate.IsDirty, Is.False);
            Assert.That(plate.TryAdd(lettuce, recipe, out reason), Is.True, reason);
        }
    }
}
