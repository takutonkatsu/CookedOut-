using System;
using System.Linq;
using CookedOut.Domain;

namespace CookedOut.Application
{
    public sealed class TutorialProgressSnapshot
    {
        public TutorialProgressSnapshot(
            int stageOneStars,
            int stageTwoStars,
            int stageThreeStars,
            int stageFourStars = 0,
            int stageFiveStars = 0,
            int stageSixStars = 0,
            int stageSevenStars = 0,
            int stageEightStars = 0)
        {
            StageOneStars = ClampStars(stageOneStars);
            StageTwoStars = ClampStars(stageTwoStars);
            StageThreeStars = ClampStars(stageThreeStars);
            StageFourStars = ClampStars(stageFourStars);
            StageFiveStars = ClampStars(stageFiveStars);
            StageSixStars = ClampStars(stageSixStars);
            StageSevenStars = ClampStars(stageSevenStars);
            StageEightStars = ClampStars(stageEightStars);
        }

        public int StageOneStars { get; }
        public int StageTwoStars { get; }
        public int StageThreeStars { get; }
        public int StageFourStars { get; }
        public int StageFiveStars { get; }
        public int StageSixStars { get; }
        public int StageSevenStars { get; }
        public int StageEightStars { get; }
        public bool TutorialOneTwoUnlocked => StageOneStars >= 1;
        public bool TutorialOneThreeUnlocked => StageTwoStars >= 1;
        public bool MultiplayerUnlocked => StageThreeStars >= 1;
        public bool BeginnerOutingsUnlocked => StageThreeStars >= 1;
        public bool NormalBusinessUnlocked => StageEightStars >= 1;
        public int ChapterOneProgressStars => StageOneStars + StageTwoStars + StageThreeStars +
                                              StageFourStars + StageFiveStars + StageSixStars + StageSevenStars +
                                              StageEightStars;

        public int StarsFor(string stageId)
        {
            if (stageId == GameIds.TutorialStage)
            {
                return StageOneStars;
            }

            if (stageId == GameIds.TutorialSoupStage)
            {
                return StageTwoStars;
            }

            if (stageId == GameIds.TutorialThrowDeliveryStage) return StageThreeStars;
            if (stageId == GameIds.TutorialDashStage) return StageFourStars;
            if (stageId == GameIds.TutorialFryingStage) return StageFiveStars;
            if (stageId == GameIds.TutorialFireRecoveryStage) return StageSixStars;
            if (stageId == GameIds.TutorialDishwashingStage) return StageSevenStars;
            return stageId == GameIds.TutorialCombinedDeliveryStage ? StageEightStars : 0;
        }

        private static int ClampStars(int stars)
        {
            return Math.Max(0, Math.Min(3, stars));
        }
    }

    public sealed class TutorialProgression
    {
        public const string SaveSlotId = "progress.tutorial.v1";
        private readonly ISaveStore _saveStore;

        public TutorialProgression(ISaveStore saveStore)
        {
            _saveStore = saveStore ?? throw new ArgumentNullException(nameof(saveStore));
            Current = new TutorialProgressSnapshot(0, 0, 0);
        }

        public TutorialProgressSnapshot Current { get; private set; }

        public void Load()
        {
            if (!_saveStore.TryLoad(SaveSlotId, out var value) || string.IsNullOrWhiteSpace(value))
            {
                Current = new TutorialProgressSnapshot(0, 0, 0);
                return;
            }

            var fields = value.Split('|');
            if (fields.Length == 4 && fields[0] == "v1" &&
                int.TryParse(fields[1], out var legacyOne) &&
                int.TryParse(fields[2], out var legacyTwo) &&
                int.TryParse(fields[3], out var legacyThree))
            {
                Current = new TutorialProgressSnapshot(legacyOne, legacyTwo, legacyThree);
                return;
            }

            if (fields.Length != 9 || fields[0] != "v2" ||
                fields.Skip(1).Any(field => !int.TryParse(field, out _)))
            {
                Current = new TutorialProgressSnapshot(0, 0, 0);
                return;
            }

            Current = new TutorialProgressSnapshot(
                int.Parse(fields[1]), int.Parse(fields[2]), int.Parse(fields[3]), int.Parse(fields[4]),
                int.Parse(fields[5]), int.Parse(fields[6]), int.Parse(fields[7]), int.Parse(fields[8]));
        }

        public TutorialProgressSnapshot RecordStageResult(string stageId, int stars)
        {
            var one = Current.StageOneStars;
            var two = Current.StageTwoStars;
            var three = Current.StageThreeStars;
            var four = Current.StageFourStars;
            var five = Current.StageFiveStars;
            var six = Current.StageSixStars;
            var seven = Current.StageSevenStars;
            var eight = Current.StageEightStars;
            if (stageId == GameIds.TutorialStage)
            {
                one = Math.Max(one, stars);
            }
            else if (stageId == GameIds.TutorialSoupStage)
            {
                two = Math.Max(two, stars);
            }
            else if (stageId == GameIds.TutorialThrowDeliveryStage)
            {
                three = Math.Max(three, stars);
            }
            else if (stageId == GameIds.TutorialDashStage) four = Math.Max(four, stars);
            else if (stageId == GameIds.TutorialFryingStage) five = Math.Max(five, stars);
            else if (stageId == GameIds.TutorialFireRecoveryStage) six = Math.Max(six, stars);
            else if (stageId == GameIds.TutorialDishwashingStage) seven = Math.Max(seven, stars);
            else if (stageId == GameIds.TutorialCombinedDeliveryStage) eight = Math.Max(eight, stars);

            Current = new TutorialProgressSnapshot(one, two, three, four, five, six, seven, eight);
            _saveStore.Save(SaveSlotId,
                $"v2|{one}|{two}|{three}|{four}|{five}|{six}|{seven}|{eight}");
            return Current;
        }
    }
}
