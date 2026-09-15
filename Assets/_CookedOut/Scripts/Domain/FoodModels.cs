using System;
using System.Collections.Generic;
using System.Linq;

namespace CookedOut.Domain
{
    public enum IngredientPreparation
    {
        Raw = 0,
        Chopped = 1,
        Cooked = 2
    }

    [Serializable]
    public sealed class IngredientItem
    {
        public const float RequiredChopSeconds = 3.0f;

        public IngredientItem(string ingredientId, IngredientPreparation preparation)
        {
            if (string.IsNullOrWhiteSpace(ingredientId))
            {
                throw new ArgumentException("Ingredient ID is required.", nameof(ingredientId));
            }

            IngredientId = ingredientId;
            Preparation = preparation;
        }

        public string IngredientId { get; }
        public IngredientPreparation Preparation { get; private set; }
        public float PreparationProgress { get; private set; }

        public bool TryAdvanceChop(float deltaSeconds, out bool completed, out string reason)
        {
            completed = false;
            if (Preparation != IngredientPreparation.Raw)
            {
                reason = "この食材はすでに切られています";
                return false;
            }

            if (deltaSeconds <= 0f)
            {
                reason = "作業時間は0より大きくしてください";
                return false;
            }

            PreparationProgress = Math.Min(1f, PreparationProgress + deltaSeconds / RequiredChopSeconds);
            if (PreparationProgress >= 1f - 0.0001f)
            {
                PreparationProgress = 1f;
                Preparation = IngredientPreparation.Chopped;
                completed = true;
            }

            reason = string.Empty;
            return true;
        }

        public bool TryChop(out string reason)
        {
            return TryAdvanceChop(RequiredChopSeconds, out _, out reason);
        }

        public IngredientItem Copy()
        {
            var copy = new IngredientItem(IngredientId, Preparation);
            copy.PreparationProgress = PreparationProgress;
            return copy;
        }
    }

    [Serializable]
    public sealed class DeliveryContainer
    {
        public const float RequiredWashSeconds = 2.5f;
        private readonly List<IngredientItem> _components = new List<IngredientItem>();

        public DeliveryContainer(string containerId)
        {
            if (string.IsNullOrWhiteSpace(containerId))
            {
                throw new ArgumentException("Container ID is required.", nameof(containerId));
            }

            ContainerId = containerId;
        }

        public string ContainerId { get; }
        public string CompletedRecipeId { get; private set; }
        public IReadOnlyList<IngredientItem> Components => _components;
        public bool IsComplete => !string.IsNullOrEmpty(CompletedRecipeId);
        public bool IsReusablePlate => ContainerId == GameIds.ReusablePlate;
        public bool IsDirty { get; private set; }
        public float WashProgress { get; private set; }

        public bool TryAdd(IngredientItem item, RecipeDefinition recipe, out string reason)
        {
            if (item == null)
            {
                reason = "追加する食材がありません";
                return false;
            }

            if (IsComplete)
            {
                reason = "完成済みの容器には追加できません";
                return false;
            }

            if (IsDirty)
            {
                reason = "汚れた皿は洗ってから使ってください";
                return false;
            }

            if (recipe == null)
            {
                reason = "レシピがありません";
                return false;
            }

            if (recipe.RequiresHeating && !recipe.AllowsFinalAssembly)
            {
                reason = "この料理は加熱器具で完成させてください";
                return false;
            }

            var candidate = _components.Select(component => component.Copy()).ToList();
            candidate.Add(item.Copy());
            if (!recipe.CanAccept(candidate))
            {
                reason = item.Preparation != IngredientPreparation.Chopped
                    ? "食材を切ってから容器へ入れてください"
                    : "この食材は現在のレシピに入りません";
                return false;
            }

            _components.Add(item.Copy());
            CompletedRecipeId = recipe.Matches(_components) ? recipe.Id : null;
            reason = string.Empty;
            return true;
        }

        public bool TryFillCookedBatch(
            IReadOnlyList<IngredientItem> ingredients,
            RecipeDefinition recipe,
            out string reason)
        {
            if (ingredients == null || ingredients.Count == 0 || recipe == null || !recipe.RequiresHeating)
            {
                reason = "完成した鍋料理ではありません";
                return false;
            }

            if (IsComplete)
            {
                reason = "完成済みの容器には追加できません";
                return false;
            }

            if (IsDirty)
            {
                reason = "汚れた皿は洗ってから使ってください";
                return false;
            }

            if (recipe.AllowsFinalAssembly)
            {
                var candidate = _components.Select(component => component.Copy()).ToList();
                candidate.AddRange(ingredients.Select(item => item.Copy()));
                if (!recipe.CanAccept(candidate))
                {
                    reason = "この加熱済み食材は現在の料理に入りません";
                    return false;
                }

                _components.AddRange(ingredients.Select(item => item.Copy()));
                CompletedRecipeId = recipe.Matches(_components) ? recipe.Id : null;
                reason = string.Empty;
                return true;
            }

            if (!recipe.Matches(ingredients))
            {
                reason = "完成した鍋料理ではありません";
                return false;
            }

            if (_components.Count > 0)
            {
                reason = "空の配達容器が必要です";
                return false;
            }

            _components.AddRange(ingredients.Select(item => item.Copy()));
            CompletedRecipeId = recipe.Id;
            reason = string.Empty;
            return true;
        }

        public bool Contains(string ingredientId, IngredientPreparation preparation)
        {
            return _components.Any(component =>
                component.IngredientId == ingredientId && component.Preparation == preparation);
        }

        public bool MarkDirtyAfterServing()
        {
            if (!IsReusablePlate || !IsComplete)
            {
                return false;
            }

            _components.Clear();
            CompletedRecipeId = null;
            IsDirty = true;
            WashProgress = 0f;
            return true;
        }

        public bool TryAdvanceWash(float deltaSeconds, out bool completed, out string reason)
        {
            completed = false;
            if (!IsReusablePlate || !IsDirty)
            {
                reason = "洗う必要のある皿がありません";
                return false;
            }

            if (deltaSeconds <= 0f)
            {
                reason = "洗浄時間は0より大きくしてください";
                return false;
            }

            WashProgress = Math.Min(1f, WashProgress + deltaSeconds / RequiredWashSeconds);
            if (WashProgress >= 1f - 0.0001f)
            {
                WashProgress = 1f;
                IsDirty = false;
                completed = true;
            }

            reason = string.Empty;
            return true;
        }
    }

    [Serializable]
    public sealed class RecipeComponent
    {
        public RecipeComponent(string ingredientId, IngredientPreparation preparation, int count)
        {
            IngredientId = ingredientId;
            Preparation = preparation;
            Count = count;
        }

        public string IngredientId { get; }
        public IngredientPreparation Preparation { get; }
        public int Count { get; }
    }

    [Serializable]
    public sealed class RecipeDefinition
    {
        private readonly List<RecipeComponent> _requiredComponents;

        public RecipeDefinition(
            string id,
            IEnumerable<RecipeComponent> requiredComponents,
            bool requiresHeating = false,
            bool allowsFinalAssembly = false)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            _requiredComponents = requiredComponents?.ToList()
                ?? throw new ArgumentNullException(nameof(requiredComponents));
            RequiresHeating = requiresHeating;
            AllowsFinalAssembly = allowsFinalAssembly;
        }

        public string Id { get; }
        public IReadOnlyList<RecipeComponent> RequiredComponents => _requiredComponents;
        public bool RequiresHeating { get; }
        public bool AllowsFinalAssembly { get; }

        public bool Matches(IReadOnlyList<IngredientItem> components)
        {
            if (components == null || components.Count != _requiredComponents.Sum(x => x.Count))
            {
                return false;
            }

            return _requiredComponents.All(required =>
                components.Count(item => item.IngredientId == required.IngredientId &&
                                         item.Preparation == required.Preparation) == required.Count);
        }

        public bool CanAccept(IReadOnlyList<IngredientItem> components)
        {
            if (components == null)
            {
                return false;
            }

            return components.All(component =>
                _requiredComponents.Any(required =>
                    required.IngredientId == component.IngredientId &&
                    required.Preparation == component.Preparation)) &&
                _requiredComponents.All(required =>
                    components.Count(item => item.IngredientId == required.IngredientId &&
                                             item.Preparation == required.Preparation) <= required.Count);
        }
    }

    [Serializable]
    public sealed class CookingPot
    {
        public const float RequiredHeatSeconds = 4f;
        private readonly List<IngredientItem> _contents = new List<IngredientItem>();

        public CookingPot(string cookwareId)
        {
            CookwareId = string.IsNullOrWhiteSpace(cookwareId)
                ? throw new ArgumentException("Cookware ID is required.", nameof(cookwareId))
                : cookwareId;
        }

        public string CookwareId { get; }
        public IReadOnlyList<IngredientItem> Contents => _contents;
        public float HeatProgress { get; private set; }
        public bool IsCooked { get; private set; }
        public bool IsEmpty => _contents.Count == 0;

        public bool TryAdd(IngredientItem ingredient, RecipeDefinition recipe, out string reason)
        {
            if (!CanAdd(ingredient, recipe, out reason))
            {
                return false;
            }

            _contents.Add(ingredient.Copy());
            reason = string.Empty;
            return true;
        }

        public bool CanAdd(IngredientItem ingredient, RecipeDefinition recipe, out string reason)
        {
            if (ingredient == null)
            {
                reason = "鍋へ入れる食材がありません";
                return false;
            }

            if (recipe == null || !recipe.RequiresHeating)
            {
                reason = "この鍋に対応する加熱レシピがありません";
                return false;
            }

            if (IsCooked || HeatProgress > 0f)
            {
                reason = "加熱中または完成済みの鍋には追加できません";
                return false;
            }

            var candidate = _contents.Select(item => item.Copy()).ToList();
            candidate.Add(ingredient.Copy());
            if (!recipe.CanAccept(candidate))
            {
                reason = ingredient.Preparation != IngredientPreparation.Chopped
                    ? "食材を切ってから鍋へ入れてください"
                    : "この食材は現在の鍋レシピに入りません";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public bool TryAdvanceHeat(
            float deltaSeconds,
            RecipeDefinition recipe,
            out bool completed,
            out string reason)
        {
            completed = false;
            if (IsCooked)
            {
                reason = "鍋料理は完成しています";
                return false;
            }

            if (recipe == null || !recipe.RequiresHeating || !recipe.Matches(_contents))
            {
                reason = "加熱を始めるための材料が揃っていません";
                return false;
            }

            if (deltaSeconds <= 0f)
            {
                reason = "加熱時間は0より大きくしてください";
                return false;
            }

            HeatProgress = Math.Min(1f, HeatProgress + deltaSeconds / RequiredHeatSeconds);
            if (HeatProgress >= 1f)
            {
                IsCooked = true;
                completed = true;
            }

            reason = string.Empty;
            return true;
        }

        public bool TryTransferTo(
            DeliveryContainer container,
            RecipeDefinition recipe,
            out string reason)
        {
            if (!IsCooked)
            {
                reason = "鍋料理はまだ完成していません";
                return false;
            }

            if (container == null)
            {
                reason = "空の配達容器が必要です";
                return false;
            }

            if (!container.TryFillCookedBatch(_contents, recipe, out reason))
            {
                return false;
            }

            _contents.Clear();
            HeatProgress = 0f;
            IsCooked = false;
            return true;
        }
    }

    [Serializable]
    public sealed class FryingPan
    {
        public const float RequiredCookSeconds = 4f;
        public const float BurnGraceSeconds = 6f;
        private IngredientItem _contents;

        public FryingPan(string cookwareId)
        {
            CookwareId = string.IsNullOrWhiteSpace(cookwareId)
                ? throw new ArgumentException("Cookware ID is required.", nameof(cookwareId))
                : cookwareId;
        }

        public string CookwareId { get; }
        public IngredientItem Contents => _contents;
        public float HeatSeconds { get; private set; }
        public float CookProgress => Math.Min(1f, HeatSeconds / RequiredCookSeconds);
        public float BurnProgress => Math.Min(1f, Math.Max(0f, HeatSeconds - RequiredCookSeconds) / BurnGraceSeconds);
        public float RemainingBurnSeconds => IsEmpty
            ? 0f
            : Math.Max(0f, RequiredCookSeconds + BurnGraceSeconds - HeatSeconds);
        public bool IsEmpty => _contents == null;
        public bool IsBurned => !IsEmpty && HeatSeconds >= RequiredCookSeconds + BurnGraceSeconds;
        public bool IsCooked => !IsEmpty && HeatSeconds >= RequiredCookSeconds && !IsBurned;

        public bool CanAdd(IngredientItem ingredient, RecipeDefinition recipe, out string reason)
        {
            if (ingredient == null)
            {
                reason = "フライパンへ入れる食材がありません";
                return false;
            }

            if (!IsEmpty)
            {
                reason = "フライパンは使用中です";
                return false;
            }

            if (ingredient.Preparation != IngredientPreparation.Chopped)
            {
                reason = "食材を切ってからフライパンへ入れてください";
                return false;
            }

            if (recipe == null || !recipe.RequiresHeating ||
                !recipe.RequiredComponents.Any(component =>
                    component.IngredientId == ingredient.IngredientId &&
                    component.Preparation == IngredientPreparation.Cooked))
            {
                reason = "この食材は現在のフライパン料理に使えません";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public bool TryAdd(IngredientItem ingredient, RecipeDefinition recipe, out string reason)
        {
            if (!CanAdd(ingredient, recipe, out reason))
            {
                return false;
            }

            _contents = ingredient.Copy();
            HeatSeconds = 0f;
            return true;
        }

        public bool TryAdvanceHeat(
            float deltaSeconds,
            out bool completed,
            out bool burned,
            out string reason)
        {
            completed = false;
            burned = false;
            if (IsEmpty)
            {
                reason = "フライパンに食材がありません";
                return false;
            }

            if (IsBurned)
            {
                reason = "食材は焦げています";
                return false;
            }

            if (deltaSeconds <= 0f)
            {
                reason = "加熱時間は0より大きくしてください";
                return false;
            }

            var wasCooked = IsCooked;
            HeatSeconds = Math.Min(RequiredCookSeconds + BurnGraceSeconds, HeatSeconds + deltaSeconds);
            completed = !wasCooked && IsCooked;
            burned = IsBurned;
            reason = string.Empty;
            return true;
        }

        public bool TryTransferTo(
            DeliveryContainer container,
            RecipeDefinition recipe,
            out string reason)
        {
            if (IsBurned)
            {
                reason = "焦げた食材は提供できません";
                return false;
            }

            if (!IsCooked)
            {
                reason = "フライパン料理はまだ完成していません";
                return false;
            }

            var cooked = new IngredientItem(_contents.IngredientId, IngredientPreparation.Cooked);
            if (!container.TryFillCookedBatch(new[] { cooked }, recipe, out reason))
            {
                return false;
            }

            _contents = null;
            HeatSeconds = 0f;
            return true;
        }

        public bool DiscardBurnedContents()
        {
            if (!IsBurned)
            {
                return false;
            }

            _contents = null;
            HeatSeconds = 0f;
            return true;
        }
    }

    public static class TutorialContent
    {
        public static RecipeDefinition CreateLettuceSaladRecipe()
        {
            return new RecipeDefinition(
                GameIds.LettuceSaladRecipe,
                new[]
                {
                    new RecipeComponent(GameIds.LettuceIngredient, IngredientPreparation.Chopped, 1)
                });
        }

        public static RecipeDefinition CreateVegetableSoupRecipe()
        {
            return new RecipeDefinition(
                GameIds.VegetableSoupRecipe,
                new[]
                {
                    new RecipeComponent(GameIds.CarrotIngredient, IngredientPreparation.Chopped, 1),
                    new RecipeComponent(GameIds.OnionIngredient, IngredientPreparation.Chopped, 1)
                },
                true);
        }

        public static RecipeDefinition CreateHamburgerPlateRecipe()
        {
            return new RecipeDefinition(
                GameIds.HamburgerPlateRecipe,
                new[]
                {
                    new RecipeComponent(GameIds.BeefIngredient, IngredientPreparation.Cooked, 1),
                    new RecipeComponent(GameIds.LettuceIngredient, IngredientPreparation.Chopped, 1)
                },
                true,
                true);
        }
    }
}
