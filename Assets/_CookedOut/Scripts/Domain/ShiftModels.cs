using System;
using System.Collections.Generic;
using System.Linq;

namespace CookedOut.Domain
{
    public enum OrderStatus
    {
        Active = 0,
        Served = 1,
        Expired = 2
    }

    public enum ShiftStatus
    {
        Ready = 0,
        Running = 1,
        Finished = 2
    }

    [Serializable]
    public sealed class OrderDefinition
    {
        public OrderDefinition(string id, string recipeId, float durationSeconds)
        {
            Id = id;
            RecipeId = recipeId;
            DurationSeconds = durationSeconds;
        }

        public string Id { get; }
        public string RecipeId { get; }
        public float DurationSeconds { get; }
    }

    [Serializable]
    public sealed class OrderState
    {
        public OrderState(string instanceId, string definitionId, string recipeId, float durationSeconds)
        {
            Id = instanceId;
            DefinitionId = definitionId;
            RecipeId = recipeId;
            RemainingSeconds = durationSeconds;
            Status = OrderStatus.Active;
        }

        public string Id { get; }
        public string DefinitionId { get; }
        public string RecipeId { get; }
        public float RemainingSeconds { get; private set; }
        public OrderStatus Status { get; private set; }
        public float TerminalElapsedSeconds { get; private set; }

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
            {
                return;
            }

            if (Status != OrderStatus.Active)
            {
                TerminalElapsedSeconds += deltaSeconds;
                return;
            }

            RemainingSeconds = Math.Max(0f, RemainingSeconds - deltaSeconds);
            if (RemainingSeconds <= 0f)
            {
                Status = OrderStatus.Expired;
            }
        }

        public bool TryServe(out string reason)
        {
            if (Status != OrderStatus.Active)
            {
                reason = Status == OrderStatus.Expired ? "この注文は失効しています" : "この注文は提供済みです";
                return false;
            }

            Status = OrderStatus.Served;
            reason = string.Empty;
            return true;
        }
    }

    [Serializable]
    public sealed class StageDefinition
    {
        public StageDefinition(
            string id,
            float shiftDurationSeconds,
            float orderDurationSeconds,
            int baseScore,
            int tipPerRemainingSecond,
            int starOne,
            int starTwo,
            int starThree,
            float orderSpawnIntervalSeconds = 15f,
            int maxVisibleOrders = 5,
            float orderTicketExitSeconds = 0.65f,
            int minimumServedOrders = 1)
        {
            Id = id;
            ShiftDurationSeconds = shiftDurationSeconds;
            OrderDurationSeconds = orderDurationSeconds;
            BaseScore = baseScore;
            TipPerRemainingSecond = tipPerRemainingSecond;
            StarThresholds = new[] { starOne, starTwo, starThree };
            OrderSpawnIntervalSeconds = Math.Max(0.1f, orderSpawnIntervalSeconds);
            MaxVisibleOrders = Math.Max(1, maxVisibleOrders);
            OrderTicketExitSeconds = Math.Max(0.1f, orderTicketExitSeconds);
            MinimumServedOrders = Math.Max(1, minimumServedOrders);
        }

        public string Id { get; }
        public float ShiftDurationSeconds { get; }
        public float OrderDurationSeconds { get; }
        public int BaseScore { get; }
        public int TipPerRemainingSecond { get; }
        public IReadOnlyList<int> StarThresholds { get; }
        public float OrderSpawnIntervalSeconds { get; }
        public int MaxVisibleOrders { get; }
        public float OrderTicketExitSeconds { get; }
        public int MinimumServedOrders { get; }
    }

    [Serializable]
    public sealed class ShiftState
    {
        private readonly List<OrderState> _orders = new List<OrderState>();

        public ShiftState(StageDefinition stage)
        {
            StageId = stage.Id;
            RemainingSeconds = stage.ShiftDurationSeconds;
            Status = ShiftStatus.Ready;
        }

        public string StageId { get; }
        public float RemainingSeconds { get; private set; }
        public ShiftStatus Status { get; private set; }
        public int Score { get; private set; }
        public int Stars { get; private set; }
        public int ServedCount { get; private set; }
        public int ExpiredCount { get; private set; }
        public IReadOnlyList<OrderState> Orders => _orders;

        public void Start()
        {
            if (Status != ShiftStatus.Ready)
            {
                throw new InvalidOperationException("Shift can only start from Ready state.");
            }

            Status = ShiftStatus.Running;
        }

        public void AddOrder(OrderState order)
        {
            if (Status != ShiftStatus.Running)
            {
                throw new InvalidOperationException("Orders can only be added to a running shift.");
            }

            _orders.Add(order ?? throw new ArgumentNullException(nameof(order)));
        }

        public void Tick(float deltaSeconds, StageDefinition stage)
        {
            if (Status != ShiftStatus.Running || deltaSeconds <= 0f)
            {
                return;
            }

            var expiredBefore = _orders.Count(order => order.Status == OrderStatus.Expired);
            foreach (var order in _orders)
            {
                order.Tick(deltaSeconds);
            }

            ExpiredCount += _orders.Count(order => order.Status == OrderStatus.Expired) - expiredBefore;
            _orders.RemoveAll(order =>
                order.Status != OrderStatus.Active &&
                order.TerminalElapsedSeconds >= stage.OrderTicketExitSeconds);
            RemainingSeconds = Math.Max(0f, RemainingSeconds - deltaSeconds);
            if (RemainingSeconds <= 0f)
            {
                Finish(stage);
            }
        }

        public void RecordServed(int awardedScore)
        {
            Score += awardedScore;
            ServedCount++;
        }

        public void Finish(StageDefinition stage)
        {
            if (Status == ShiftStatus.Finished)
            {
                return;
            }

            Status = ShiftStatus.Finished;
            Stars = ServedCount < stage.MinimumServedOrders
                ? 0
                : stage.StarThresholds.Count(threshold => Score >= threshold);
        }

        public OrderState FirstActiveOrder()
        {
            return _orders.FirstOrDefault(order => order.Status == OrderStatus.Active);
        }

        public int ActiveOrderCount()
        {
            return _orders.Count(order => order.Status == OrderStatus.Active);
        }
    }

    public static class TutorialStage
    {
        public static StageDefinition Create()
        {
            return new StageDefinition(
                GameIds.TutorialStage,
                75f,
                45f,
                100,
                2,
                100,
                150,
                180);
        }
    }

    public static class TutorialSoupStage
    {
        public static StageDefinition Create()
        {
            return new StageDefinition(
                GameIds.TutorialSoupStage,
                150f,
                105f,
                40,
                1,
                110,
                170,
                230);
        }
    }

    public static class TutorialThrowDeliveryStage
    {
        public static StageDefinition Create()
        {
            return new StageDefinition(
                GameIds.TutorialThrowDeliveryStage,
                210f,
                150f,
                80,
                1,
                140,
                230,
                320);
        }
    }

    public static class TutorialDashStage
    {
        public static StageDefinition Create()
        {
            return new StageDefinition(
                GameIds.TutorialDashStage,
                75f,
                75f,
                100,
                1,
                100,
                140,
                170);
        }
    }

    public static class TutorialFryingStage
    {
        public static StageDefinition Create()
        {
            return new StageDefinition(
                GameIds.TutorialFryingStage,
                180f,
                105f,
                50,
                1,
                130,
                210,
                290,
                32f,
                2,
                0.65f,
                2);
        }
    }

    public static class TutorialFireRecoveryStage
    {
        public static StageDefinition Create()
        {
            return new StageDefinition(
                GameIds.TutorialFireRecoveryStage,
                180f,
                110f,
                50,
                1,
                130,
                220,
                310,
                29f,
                2,
                0.65f,
                2);
        }
    }

    public static class TutorialDishwashingStage
    {
        public static StageDefinition Create()
        {
            return new StageDefinition(
                GameIds.TutorialDishwashingStage,
                180f,
                100f,
                40,
                1,
                120,
                200,
                280,
                28f,
                2,
                0.65f,
                3);
        }
    }

    public static class TutorialCombinedDeliveryStage
    {
        public static StageDefinition Create()
        {
            return new StageDefinition(
                GameIds.TutorialCombinedDeliveryStage,
                240f,
                150f,
                70,
                1,
                170,
                300,
                430,
                32f,
                2,
                0.65f,
                3);
        }
    }

    public static class TutorialOrders
    {
        public static OrderDefinition CreateLettuceSaladOrder()
        {
            return new OrderDefinition(
                GameIds.LettuceSaladOrder,
                GameIds.LettuceSaladRecipe,
                TutorialStage.Create().OrderDurationSeconds);
        }


        public static OrderDefinition CreateVegetableSoupOrder()
        {
            return new OrderDefinition(
                GameIds.VegetableSoupOrder,
                GameIds.VegetableSoupRecipe,
                TutorialSoupStage.Create().OrderDurationSeconds);
        }

        public static OrderDefinition CreateThrowDeliverySoupOrder()
        {
            return new OrderDefinition(
                GameIds.ThrowDeliverySoupOrder,
                GameIds.VegetableSoupRecipe,
                TutorialThrowDeliveryStage.Create().OrderDurationSeconds);
        }

        public static OrderDefinition CreateHamburgerPlateOrder()
        {
            return new OrderDefinition(
                GameIds.HamburgerPlateOrder,
                GameIds.HamburgerPlateRecipe,
                TutorialFryingStage.Create().OrderDurationSeconds);
        }

        public static IReadOnlyList<OrderDefinition> CreateFireRecoveryOrders()
        {
            var duration = TutorialFireRecoveryStage.Create().OrderDurationSeconds;
            return new[]
            {
                new OrderDefinition(GameIds.HamburgerPlateOrder, GameIds.HamburgerPlateRecipe, duration),
                new OrderDefinition(GameIds.VegetableSoupOrder, GameIds.VegetableSoupRecipe, duration)
            };
        }

        public static IReadOnlyList<OrderDefinition> CreateDishwashingOrders()
        {
            var duration = TutorialDishwashingStage.Create().OrderDurationSeconds;
            return new[]
            {
                new OrderDefinition(GameIds.LettuceSaladOrder, GameIds.LettuceSaladRecipe, duration),
                new OrderDefinition(GameIds.VegetableSoupOrder, GameIds.VegetableSoupRecipe, duration)
            };
        }

        public static IReadOnlyList<OrderDefinition> CreateCombinedDeliveryOrders()
        {
            var duration = TutorialCombinedDeliveryStage.Create().OrderDurationSeconds;
            return new[]
            {
                new OrderDefinition(GameIds.LettuceSaladOrder, GameIds.LettuceSaladRecipe, duration),
                new OrderDefinition(GameIds.VegetableSoupOrder, GameIds.VegetableSoupRecipe, duration),
                new OrderDefinition(GameIds.HamburgerPlateOrder, GameIds.HamburgerPlateRecipe, duration)
            };
        }
    }

    [Serializable]
    public sealed class OrderSnapshot
    {
        public string id;
        public string definitionId;
        public string recipeId;
        public float remainingSeconds;
        public int status;
    }

    [Serializable]
    public sealed class ShiftSnapshot
    {
        public int schemaVersion = 1;
        public string stageId;
        public float remainingSeconds;
        public int status;
        public int score;
        public int stars;
        public OrderSnapshot[] orders;

        public static ShiftSnapshot From(ShiftState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return new ShiftSnapshot
            {
                stageId = state.StageId,
                remainingSeconds = state.RemainingSeconds,
                status = (int)state.Status,
                score = state.Score,
                stars = state.Stars,
                orders = state.Orders.Select(order => new OrderSnapshot
                {
                    id = order.Id,
                    definitionId = order.DefinitionId,
                    recipeId = order.RecipeId,
                    remainingSeconds = order.RemainingSeconds,
                    status = (int)order.Status
                }).ToArray()
            };
        }
    }
}
