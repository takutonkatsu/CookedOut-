using System;
using System.Collections.Generic;
using System.Linq;
using CookedOut.Domain;

namespace CookedOut.Application
{
    public enum FulfillmentMethod
    {
        OnSite,
        SelfDelivery,
        Courier
    }

    public sealed class ServeResult
    {
        private ServeResult(bool succeeded, int baseScore, int timeTip, int deliveryBonus, string reason)
        {
            Succeeded = succeeded;
            BaseScore = baseScore;
            TimeTip = timeTip;
            DeliveryBonus = deliveryBonus;
            Reason = reason;
        }

        public bool Succeeded { get; }
        public int BaseScore { get; }
        public int TimeTip { get; }
        public int DeliveryBonus { get; }
        public int TotalScore => BaseScore + TimeTip + DeliveryBonus;
        public string Reason { get; }

        public static ServeResult Success(int baseScore, int timeTip, int deliveryBonus = 0)
        {
            return new ServeResult(true, baseScore, timeTip, deliveryBonus, string.Empty);
        }

        public static ServeResult Failure(string reason)
        {
            return new ServeResult(false, 0, 0, 0, reason);
        }
    }

    public sealed class KitchenSession
    {
        private readonly StageDefinition _stage;
        private readonly IReadOnlyList<RecipeDefinition> _recipes;
        private readonly IReadOnlyList<OrderDefinition> _orderDefinitions;
        private readonly IRandomSource _random;
        private readonly IClock _clock;
        private readonly IAnalyticsSink _analytics;
        private float _orderSpawnRemainingSeconds;
        private int _orderSerial;

        public KitchenSession(
            StageDefinition stage,
            RecipeDefinition recipe,
            OrderDefinition orderDefinition,
            IRandomSource random,
            IClock clock,
            IAnalyticsSink analytics)
            : this(
                stage,
                new[] { recipe },
                new[] { orderDefinition },
                random,
                clock,
                analytics)
        {
        }

        public KitchenSession(
            StageDefinition stage,
            IEnumerable<RecipeDefinition> recipes,
            IEnumerable<OrderDefinition> orderDefinitions,
            IRandomSource random,
            IClock clock,
            IAnalyticsSink analytics)
        {
            _stage = stage ?? throw new ArgumentNullException(nameof(stage));
            _recipes = recipes?.Where(recipe => recipe != null).ToArray()
                ?? throw new ArgumentNullException(nameof(recipes));
            _orderDefinitions = orderDefinitions?.Where(order => order != null).ToArray()
                ?? throw new ArgumentNullException(nameof(orderDefinitions));
            if (_recipes.Count == 0 || _orderDefinitions.Count == 0)
            {
                throw new ArgumentException("At least one recipe and order definition are required.");
            }

            if (_orderDefinitions.Any(order => _recipes.All(recipe => recipe.Id != order.RecipeId)))
            {
                throw new ArgumentException("Every order must reference one of the session recipes.");
            }
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
        }

        public ShiftState State { get; private set; }
        public DateTimeOffset StartedAtUtc { get; private set; }
        public RecipeDefinition Recipe => _recipes[0];
        public IReadOnlyList<RecipeDefinition> Recipes => _recipes;
        public StageDefinition Stage => _stage;

        public void Start()
        {
            if (State != null)
            {
                throw new InvalidOperationException("Session has already started.");
            }

            State = new ShiftState(_stage);
            State.Start();
            StartedAtUtc = _clock.UtcNow;
            SpawnOrder();
            _orderSpawnRemainingSeconds = _stage.OrderSpawnIntervalSeconds;
            _analytics.Track("shift_started", _stage.Id);
        }

        public IngredientItem TakeRawLettuce()
        {
            return TakeRawIngredient(GameIds.LettuceIngredient);
        }

        public IngredientItem TakeRawIngredient(string ingredientId)
        {
            RequireRunning();
            if (string.IsNullOrWhiteSpace(ingredientId))
            {
                throw new ArgumentException("Ingredient ID is required.", nameof(ingredientId));
            }

            return new IngredientItem(ingredientId, IngredientPreparation.Raw);
        }

        public DeliveryContainer TakeDeliveryContainer()
        {
            RequireRunning();
            return new DeliveryContainer(GameIds.CommonDeliveryContainer);
        }

        public DeliveryContainer TakeReusablePlate()
        {
            RequireRunning();
            return new DeliveryContainer(GameIds.ReusablePlate);
        }

        public bool TryChop(IngredientItem ingredient, out string reason)
        {
            return TryAdvanceChop(ingredient, IngredientItem.RequiredChopSeconds, out _, out reason);
        }

        public bool TryAdvanceChop(
            IngredientItem ingredient,
            float deltaSeconds,
            out bool completed,
            out string reason)
        {
            RequireRunning();
            completed = false;
            if (ingredient == null)
            {
                reason = "まな板に食材がありません";
                return false;
            }

            if (!_recipes.Any(recipe => recipe.RequiredComponents.Any(component =>
                    component.IngredientId == ingredient.IngredientId))
                )
            {
                reason = "この食材は現在の工程では切れません";
                return false;
            }

            return ingredient.TryAdvanceChop(deltaSeconds, out completed, out reason);
        }

        public bool TryAssemble(
            DeliveryContainer container,
            IngredientItem ingredient,
            out string reason)
        {
            RequireRunning();
            if (container == null)
            {
                reason = "配達容器がありません";
                return false;
            }

            reason = "この食材は現在の注文へ追加できません";
            foreach (var recipe in OrderedCandidateRecipes())
            {
                if (recipe.RequiresHeating && !recipe.AllowsFinalAssembly)
                {
                    continue;
                }

                if (container.TryAdd(ingredient, recipe, out var candidateReason))
                {
                    reason = string.Empty;
                    return true;
                }

                reason = candidateReason;
            }

            return false;
        }

        public CookingPot TakeCookingPot()
        {
            RequireRunning();
            return new CookingPot(GameIds.CommonCookingPot);
        }

        public bool TryAddToPot(CookingPot pot, IngredientItem ingredient, out string reason)
        {
            RequireRunning();
            if (pot == null)
            {
                reason = "鍋がありません";
                return false;
            }

            reason = "この食材は現在の鍋料理に入りません";
            foreach (var recipe in OrderedCandidateRecipes().Where(recipe => recipe.RequiresHeating))
            {
                if (pot.TryAdd(ingredient, recipe, out var candidateReason))
                {
                    reason = string.Empty;
                    return true;
                }
                reason = candidateReason;
            }
            return false;
        }

        public bool TryAdvancePot(
            CookingPot pot,
            float deltaSeconds,
            out bool completed,
            out string reason)
        {
            RequireRunning();
            if (pot == null)
            {
                completed = false;
                reason = "鍋がありません";
                return false;
            }

            var recipe = OrderedCandidateRecipes().FirstOrDefault(candidate => candidate.Matches(pot.Contents));
            return pot.TryAdvanceHeat(deltaSeconds, recipe, out completed, out reason);
        }

        public bool TryFillContainerFromPot(
            CookingPot pot,
            DeliveryContainer container,
            out string reason)
        {
            RequireRunning();
            if (pot == null)
            {
                reason = "鍋がありません";
                return false;
            }

            reason = "完成した鍋料理ではありません";
            foreach (var recipe in OrderedCandidateRecipes().Where(recipe => recipe.Matches(pot.Contents)))
            {
                if (pot.TryTransferTo(container, recipe, out var candidateReason))
                {
                    reason = string.Empty;
                    return true;
                }
                reason = candidateReason;
            }
            return false;
        }

        public FryingPan TakeFryingPan()
        {
            RequireRunning();
            return new FryingPan(GameIds.CommonFryingPan);
        }

        public bool TryAddToPan(FryingPan pan, IngredientItem ingredient, out string reason)
        {
            RequireRunning();
            if (pan == null)
            {
                reason = "フライパンがありません";
                return false;
            }

            reason = "この食材は現在のフライパン料理に使えません";
            foreach (var recipe in OrderedCandidateRecipes().Where(recipe => recipe.RequiresHeating))
            {
                if (pan.TryAdd(ingredient, recipe, out var candidateReason))
                {
                    reason = string.Empty;
                    return true;
                }
                reason = candidateReason;
            }
            return false;
        }

        public bool TryAdvancePan(
            FryingPan pan,
            float deltaSeconds,
            out bool completed,
            out bool burned,
            out string reason)
        {
            RequireRunning();
            if (pan == null)
            {
                completed = false;
                burned = false;
                reason = "フライパンがありません";
                return false;
            }

            return pan.TryAdvanceHeat(deltaSeconds, out completed, out burned, out reason);
        }

        public bool TryFillContainerFromPan(
            FryingPan pan,
            DeliveryContainer container,
            out string reason)
        {
            RequireRunning();
            if (pan == null)
            {
                reason = "フライパンがありません";
                return false;
            }

            reason = "フライパン料理を現在の容器へ移せません";
            foreach (var recipe in OrderedCandidateRecipes().Where(recipe => recipe.RequiresHeating))
            {
                if (pan.TryTransferTo(container, recipe, out var candidateReason))
                {
                    reason = string.Empty;
                    return true;
                }
                reason = candidateReason;
            }
            return false;
        }

        public ServeResult TryServe(
            DeliveryContainer container,
            FulfillmentMethod fulfillmentMethod = FulfillmentMethod.OnSite,
            int partySize = 1)
        {
            RequireRunning();
            if (container == null)
            {
                return ServeResult.Failure("配達容器を持っていません");
            }

            if (!container.IsComplete)
            {
                return ServeResult.Failure("料理が完成していません：必要な工程を完了してください");
            }

            var order = State.Orders.FirstOrDefault(candidate =>
                candidate.Status == OrderStatus.Active &&
                candidate.RecipeId == container.CompletedRecipeId);
            if (order == null)
            {
                return ServeResult.Failure("提供できる有効な注文がありません");
            }

            if (container.CompletedRecipeId != order.RecipeId)
            {
                return ServeResult.Failure("この注文とは異なる料理です");
            }

            if (_stage.Id == GameIds.TutorialCombinedDeliveryStage)
            {
                var requiredMethod = State.ServedCount == 0
                    ? FulfillmentMethod.Courier
                    : State.ServedCount == 1 ? FulfillmentMethod.SelfDelivery : fulfillmentMethod;
                if (fulfillmentMethod == FulfillmentMethod.OnSite || fulfillmentMethod != requiredMethod)
                {
                    return ServeResult.Failure(State.ServedCount == 0
                        ? "最初の注文は配達代行へ渡してください"
                        : "次の注文はバイクで自前配達してください");
                }
            }

            var total = ScoringPolicy.ScoreSuccessfulOrder(_stage, order);
            var timeTip = total - _stage.BaseScore;
            var deliveryBonus = fulfillmentMethod == FulfillmentMethod.SelfDelivery
                ? ScoringPolicy.ScoreSelfDeliveryBonus(total, partySize)
                : 0;
            if (!order.TryServe(out var reason))
            {
                return ServeResult.Failure(reason);
            }

            State.RecordServed(total + deliveryBonus);
            _analytics.Track("order_served", order.Id + ":" + (total + deliveryBonus) + ":" + fulfillmentMethod);
            return ServeResult.Success(_stage.BaseScore, timeTip, deliveryBonus);
        }

        public void Tick(float deltaSeconds)
        {
            if (State == null || State.Status != ShiftStatus.Running)
            {
                return;
            }

            var activeBefore = State.Orders
                .Where(order => order.Status == OrderStatus.Active)
                .ToDictionary(order => order.Id, order => order);
            State.Tick(deltaSeconds, _stage);
            foreach (var order in activeBefore.Values.Where(order => order.Status == OrderStatus.Expired))
            {
                _analytics.Track("order_expired", order.Id);
            }

            if (State.Status == ShiftStatus.Running)
            {
                _orderSpawnRemainingSeconds -= deltaSeconds;
                while (_orderSpawnRemainingSeconds <= 0f)
                {
                    if (State.Orders.Count >= _stage.MaxVisibleOrders)
                    {
                        _orderSpawnRemainingSeconds = 0f;
                        break;
                    }

                    SpawnOrder();
                    _orderSpawnRemainingSeconds += _stage.OrderSpawnIntervalSeconds;
                }
            }

            if (State.Status == ShiftStatus.Finished)
            {
                _analytics.Track("shift_finished", State.Score + ":" + State.Stars);
            }
        }

        public void Finish()
        {
            if (State == null)
            {
                return;
            }

            State.Finish(_stage);
            _analytics.Track("shift_finished", State.Score + ":" + State.Stars);
        }

        private void SpawnOrder()
        {
            var suffix = _random.NextInt(100000, 1000000);
            var definition = _orderDefinitions[_orderSerial % _orderDefinitions.Count];
            var order = new OrderState(
                "order.instance." + suffix + "." + _orderSerial++,
                definition.Id,
                definition.RecipeId,
                definition.DurationSeconds);
            State.AddOrder(order);
            _analytics.Track("order_spawned", order.Id);
        }

        public RecipeDefinition RecipeFor(string recipeId)
        {
            return _recipes.FirstOrDefault(recipe => recipe.Id == recipeId);
        }

        public bool HasActiveOrderForRecipe(string recipeId)
        {
            return State != null && State.Orders.Any(order =>
                order.Status == OrderStatus.Active && order.RecipeId == recipeId);
        }

        private IEnumerable<RecipeDefinition> OrderedCandidateRecipes()
        {
            var yielded = new HashSet<string>();
            if (State != null)
            {
                foreach (var order in State.Orders.Where(order => order.Status == OrderStatus.Active))
                {
                    var recipe = RecipeFor(order.RecipeId);
                    if (recipe != null && yielded.Add(recipe.Id))
                    {
                        yield return recipe;
                    }
                }
            }

            foreach (var recipe in _recipes)
            {
                if (yielded.Add(recipe.Id))
                {
                    yield return recipe;
                }
            }
        }

        private void RequireRunning()
        {
            if (State == null || State.Status != ShiftStatus.Running)
            {
                throw new InvalidOperationException("The kitchen session is not running.");
            }
        }
    }
}
