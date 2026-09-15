using CookedOut.Application;
using CookedOut.Domain;
using CookedOut.Infrastructure;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

namespace CookedOut.Presentation
{
    public sealed class KitchenGameController : MonoBehaviour
    {
        private Text _shiftTimerText;
        private Text _scoreText;
        private GameObject _clockPausedIcon;
        private OrderTicketView _orderTicket;
        private OrderTicketView[] _orderTickets;
        private GameObject _resultPanel;
        private Text _resultText;
        private string _stageLabel;
        private TutorialProgression _progression;
        private ServeResult _lastServeResult;
        private int _successfulPotThrowCount;
        private bool _resultRecorded;
        private int _washedPlateCount;
        private int _courierDeliveryCount;
        private int _selfDeliveryCount;

        public KitchenSession Session { get; private set; }
        public bool IsFinished => Session == null || Session.State.Status == ShiftStatus.Finished;
        public bool IsClockPaused { get; private set; }
        public TutorialProgressSnapshot Progression => _progression?.Current;
        public ServeResult LastServeResult => _lastServeResult;
        public int SuccessfulPotThrowCount => _successfulPotThrowCount;
        public int WashedPlateCount => _washedPlateCount;
        public int CourierDeliveryCount => _courierDeliveryCount;
        public int SelfDeliveryCount => _selfDeliveryCount;
        public bool RequiredThrowsCompleted =>
            Session != null &&
            (Session.Stage.Id != GameIds.TutorialThrowDeliveryStage ||
             _successfulPotThrowCount >= Session.Recipe.RequiredComponents.Count);

        public void Initialize(
            Text shiftTimerText,
            Text scoreText,
            GameObject clockPausedIcon,
            OrderTicketView orderTicket,
            GameObject resultPanel,
            Text resultText,
            string stageId)
        {
            _shiftTimerText = shiftTimerText;
            _scoreText = scoreText;
            _clockPausedIcon = clockPausedIcon;
            _orderTicket = orderTicket;
            _resultPanel = resultPanel;
            _resultText = resultText;
            _progression = new TutorialProgression(new PlayerPrefsSaveStore());
            _progression.Load();

            var throwDeliveryStage = stageId == GameIds.TutorialThrowDeliveryStage;
            var dashStage = stageId == GameIds.TutorialDashStage;
            var fryingStage = stageId == GameIds.TutorialFryingStage;
            var fireRecoveryStage = stageId == GameIds.TutorialFireRecoveryStage;
            var dishwashingStage = stageId == GameIds.TutorialDishwashingStage;
            var combinedDeliveryStage = stageId == GameIds.TutorialCombinedDeliveryStage;
            var soupStage = stageId == GameIds.TutorialSoupStage;
            _stageLabel = combinedDeliveryStage
                ? "1-8"
                : dishwashingStage
                ? "1-7"
                : fireRecoveryStage
                ? "1-6"
                : fryingStage
                ? "1-5"
                : dashStage ? "1-4" : throwDeliveryStage ? "1-3" : soupStage ? "1-2" : "1-1";
            Session = combinedDeliveryStage
                ? new KitchenSession(
                    TutorialCombinedDeliveryStage.Create(),
                    new[]
                    {
                        TutorialContent.CreateLettuceSaladRecipe(),
                        TutorialContent.CreateVegetableSoupRecipe(),
                        TutorialContent.CreateHamburgerPlateRecipe()
                    },
                    TutorialOrders.CreateCombinedDeliveryOrders(),
                    new SeededRandomSource(1801u),
                    new SystemClock(),
                    new NullAnalyticsSink())
                : dishwashingStage
                ? new KitchenSession(
                    TutorialDishwashingStage.Create(),
                    new[]
                    {
                        TutorialContent.CreateLettuceSaladRecipe(),
                        TutorialContent.CreateVegetableSoupRecipe()
                    },
                    TutorialOrders.CreateDishwashingOrders(),
                    new SeededRandomSource(1701u),
                    new SystemClock(),
                    new NullAnalyticsSink())
                : fireRecoveryStage
                ? new KitchenSession(
                    TutorialFireRecoveryStage.Create(),
                    new[]
                    {
                        TutorialContent.CreateHamburgerPlateRecipe(),
                        TutorialContent.CreateVegetableSoupRecipe()
                    },
                    TutorialOrders.CreateFireRecoveryOrders(),
                    new SeededRandomSource(1601u),
                    new SystemClock(),
                    new NullAnalyticsSink())
                : fryingStage
                ? new KitchenSession(
                    TutorialFryingStage.Create(),
                    TutorialContent.CreateHamburgerPlateRecipe(),
                    TutorialOrders.CreateHamburgerPlateOrder(),
                    new SeededRandomSource(1501u),
                    new SystemClock(),
                    new NullAnalyticsSink())
                : dashStage
                ? new KitchenSession(
                    TutorialDashStage.Create(),
                    TutorialContent.CreateLettuceSaladRecipe(),
                    TutorialOrders.CreateLettuceSaladOrder(),
                    new SeededRandomSource(1401u),
                    new SystemClock(),
                    new NullAnalyticsSink())
                : throwDeliveryStage
                ? new KitchenSession(
                    TutorialThrowDeliveryStage.Create(),
                    TutorialContent.CreateVegetableSoupRecipe(),
                    TutorialOrders.CreateThrowDeliverySoupOrder(),
                    new SeededRandomSource(1301u),
                    new SystemClock(),
                    new NullAnalyticsSink())
                : soupStage
                    ? new KitchenSession(
                    TutorialSoupStage.Create(),
                    TutorialContent.CreateVegetableSoupRecipe(),
                    TutorialOrders.CreateVegetableSoupOrder(),
                    new SeededRandomSource(1201u),
                    new SystemClock(),
                    new NullAnalyticsSink())
                : new KitchenSession(
                    TutorialStage.Create(),
                    TutorialContent.CreateLettuceSaladRecipe(),
                    TutorialOrders.CreateLettuceSaladOrder(),
                    new SeededRandomSource(1101u),
                    new SystemClock(),
                    new NullAnalyticsSink());
            Session.Start();
            _orderTickets = Object.FindObjectsByType<OrderTicketView>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .OrderBy(ticket => ticket.OrderIndex)
                .ToArray();
            if (_orderTickets.Length == 0 && _orderTicket != null)
            {
                _orderTickets = new[] { _orderTicket };
            }
            foreach (var ticket in _orderTickets)
            {
                ticket.Configure(Session, ticket.OrderIndex);
            }
            SetDebugMessage(combinedDeliveryStage
                ? "First send by COURIER > next deliver by BIKE > then choose either route"
                : dishwashingStage
                ? "Serve on reusable plates > collect DIRTY plate > hold WORK at SINK > reuse"
                : fireRecoveryStage
                ? "Take EXTINGUISHER > hold WORK at FIRE > return tool > cook two meals"
                : fryingStage
                ? "CHOP beef > fry in PAN > fill container before it burns > SERVE"
                : dashStage
                ? "CHOP lettuce > fill container > DASH across reverse conveyor > SERVE"
                : throwDeliveryStage
                ? "CHOP > THROW into POT > fill container > load BIKE > DELIVERY > RETURN"
                : soupStage
                    ? "CHOP carrot + onion > add to POT > heat > fill container > serve"
                    : "CHOP lettuce > PICK container > collect from board > serve");
            RefreshHud();
        }

        private void Update()
        {
            if (Session == null)
            {
                return;
            }

            if (IsFinished)
            {
                CompleteFinishedShift();
                return;
            }

            if (!IsClockPaused)
            {
                Session.Tick(Time.deltaTime);
            }
            RefreshHud();
            if (IsFinished)
            {
                CompleteFinishedShift();
            }
        }

        private void CompleteFinishedShift()
        {
            if (!_resultRecorded)
            {
                _resultRecorded = true;
                RecordProgression();
            }
            RefreshHud();
            ShowResults();
        }

        public void SetDebugMessage(string message)
        {
            Debug.Log("[Kitchen] " + message);
        }

        public void OnOrderServed(ServeResult result)
        {
            _lastServeResult = result;
            SetDebugMessage("Served! Base " + result.BaseScore + " + time tip " + result.TimeTip);
            RefreshHud();
        }

        public void SetClockPaused(bool paused)
        {
            IsClockPaused = paused;
            RefreshHud();
        }

        public void OnDeliveryDroppedOff(ServeResult result)
        {
            _lastServeResult = result;
            if (Session != null && Session.Stage.Id == GameIds.TutorialCombinedDeliveryStage)
            {
                _selfDeliveryCount++;
            }
            SetDebugMessage("Delivered! Base " + result.BaseScore + " + time tip " + result.TimeTip +
                            " + self-delivery " + result.DeliveryBonus + ". Ride back to BIKE LOAD");
            RefreshHud();
        }

        public void OnCourierDispatched(ServeResult result, Vector3 origin)
        {
            _lastServeResult = result;
            _courierDeliveryCount++;
            CourierDispatchMotion.Spawn(origin);
            SetDebugMessage("Courier accepted the completed meal");
            RefreshHud();
        }

        public void OnPlateWashed()
        {
            _washedPlateCount++;
            SetDebugMessage("Plate washed and ready to reuse");
        }

        public void OnFireExtinguished()
        {
            IsClockPaused = false;
            SetDebugMessage("Fire extinguished. Normal service begins");
            RefreshHud();
        }

        public void OnBikeReturned(ServeResult result)
        {
            _lastServeResult = result;
            SetDebugMessage("Delivery complete. Bike returned with " + result.TotalScore + " points");
            RefreshHud();
        }

        public void OnIngredientThrownIntoPot()
        {
            if (Session == null || Session.Stage.Id != GameIds.TutorialThrowDeliveryStage)
            {
                return;
            }

            _successfulPotThrowCount++;
        }

        public void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void ReturnHome()
        {
            HomeScreenController.ReturnHome();
        }

        private void RefreshHud()
        {
            if (Session == null)
            {
                return;
            }

            var remainingSeconds = Mathf.Max(0, Mathf.CeilToInt(Session.State.RemainingSeconds));
            if (_shiftTimerText != null)
            {
                _shiftTimerText.text = $"{remainingSeconds / 60}:{remainingSeconds % 60:00}";
            }
            if (_scoreText != null)
            {
                _scoreText.text = Session.State.Score.ToString();
            }
            if (_clockPausedIcon != null)
            {
                _clockPausedIcon.SetActive(IsClockPaused);
            }
            if (_orderTickets != null)
            {
                foreach (var ticket in _orderTickets)
                {
                    ticket?.RefreshNow(IsClockPaused);
                }
            }
        }

        private void ShowResults()
        {
            if (_resultPanel == null || _resultText == null)
            {
                return;
            }

            _resultPanel.SetActive(true);
            var unlockText = Session.Stage.Id == GameIds.TutorialThrowDeliveryStage &&
                             Progression != null && Progression.MultiplayerUnlocked
                ? "\n\nマルチプレイと初心者出張を解放"
                : Session.Stage.Id == GameIds.TutorialCombinedDeliveryStage &&
                  Progression != null && Progression.NormalBusinessUnlocked
                    ? "\n\n通常営業を解放"
                    : string.Empty;
            var scoreBreakdown = _lastServeResult == null
                ? string.Empty
                : "\n基本点 " + _lastServeResult.BaseScore +
                  "  時間ボーナス " + _lastServeResult.TimeTip +
                  (_lastServeResult.DeliveryBonus > 0
                      ? "  自前配達 +" + _lastServeResult.DeliveryBonus
                      : string.Empty);
            _resultText.text =
                "営業完了\n\n得点  " + Session.State.Score +
                scoreBreakdown +
                "\n星  " + new string('★', Session.State.Stars) + new string('☆', 3 - Session.State.Stars) +
                "\n\nステージ " + _stageLabel + unlockText;
        }

        private void RecordProgression()
        {
            _progression?.RecordStageResult(Session.Stage.Id, Session.State.Stars);
        }
    }
}
