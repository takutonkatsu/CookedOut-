using System;

namespace CookedOut.Domain
{
    public static class ScoringPolicy
    {
        public static int ScoreSuccessfulOrder(StageDefinition stage, OrderState order)
        {
            if (stage == null)
            {
                throw new ArgumentNullException(nameof(stage));
            }

            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }

            var remainingWholeSeconds = (int)Math.Floor(order.RemainingSeconds);
            return stage.BaseScore + remainingWholeSeconds * stage.TipPerRemainingSecond;
        }

        public static int ScoreSelfDeliveryBonus(int commonDeliveryScore, int partySize)
        {
            if (commonDeliveryScore < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(commonDeliveryScore));
            }

            float rate;
            switch (Math.Max(1, Math.Min(4, partySize)))
            {
                case 1:
                    rate = 0.5f;
                    break;
                case 2:
                    rate = 0.3f;
                    break;
                case 3:
                    rate = 0.2f;
                    break;
                default:
                    rate = 0.15f;
                    break;
            }

            return (int)Math.Floor(commonDeliveryScore * rate);
        }
    }
}
