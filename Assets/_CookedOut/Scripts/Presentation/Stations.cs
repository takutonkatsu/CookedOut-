using CookedOut.Domain;
using CookedOut.Application;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CookedOut.Presentation
{
    public enum StationKind
    {
        IngredientCrate,
        ChoppingBoard,
        Counter,
        PotHeatSource,
        PanHeatSource,
        ContainerDispenser,
        ServingHatch,
        TrashBin,
        GroundItem,
        BikeDock,
        DeliveryPoint,
        RecoveryBin,
        ExtinguisherCabinet,
        FireHazard,
        PlateDispenser,
        DishReturn,
        WashingSink,
        CourierShelf
    }

    public sealed class InteractableStation : MonoBehaviour
    {
        private const float PanBurnWarningSeconds = 2f;
        public const float ChopIntervalSeconds = 0.25f;
        public const float ChoppingBoardCarrotYawDegrees = 90f;
        private KitchenGameController _game;
        private StationKind _kind;
        private WorldItem _slot;
        private Transform _itemAnchor;
        private GameObject _itemVisual;
        private GameObject _selectionOutline;
        private WorldItem _incomingItem;
        private string _sourceIngredientId;
        private IngredientSourceCardView _sourceCard;
        private GameObject _workProgressRoot;
        private Transform _workProgressFill;
        private ChoppingKnifeMotion _knifeMotion;
        private GameObject _heatProgressRoot;
        private Transform _heatProgressFill;
        private bool _panBurnWarningShown;
        private bool _selected;
        private bool _guided;
        private DeliveryBikeController _bike;
        private float _chopAccumulatorSeconds;
        private int _availablePlateCount;
        private readonly Queue<WorldItem> _returnedDishes = new Queue<WorldItem>();
        private FireIncidentController _fireIncident;

        public StationKind Kind => _kind;
        public WorldItem SlottedItem => _slot;
        public GridPlacement GridPlacement { get; private set; }
        public Vector3 ItemAnchorPosition => _itemAnchor == null ? transform.position : _itemAnchor.position;
        public Transform ItemAnchor => _itemAnchor;
        public Transform ItemVisual => _itemVisual == null ? null : _itemVisual.transform;
        public string SourceIngredientId => _sourceIngredientId;
        public IngredientSourceCardView SourceCard => _sourceCard;
        public float WorkProgress => _slot != null && _slot.IsIngredient
            ? _slot.Ingredient.PreparationProgress
            : 0f;
        public float HeatProgress => _slot != null && _slot.IsPot
            ? _slot.Pot.HeatProgress
            : _slot != null && _slot.IsPan
                ? _slot.Pan.CookProgress
                : 0f;
        public int AvailablePlateCount => _availablePlateCount;
        public FireIncidentController FireIncident => _fireIncident;

        public void AttachBike(DeliveryBikeController bike)
        {
            _bike = bike;
        }

        public bool CanPrioritizeDirectUse(WorldItem heldItem)
        {
            return _kind == StationKind.BikeDock &&
                   _bike != null &&
                   _bike.CanLoad(heldItem, out _);
        }

        public static InteractableStation SpawnGroundItem(
            KitchenGameController game,
            WorldItem item,
            Vector3 position)
        {
            var root = new GameObject("Ground Item");
            root.transform.position = position;

            var anchor = new GameObject("Ground Item Anchor").transform;
            anchor.SetParent(root.transform, false);

            var outline = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            outline.name = "Ground Item Outline";
            outline.transform.SetParent(root.transform, false);
            outline.transform.localPosition = new Vector3(0f, 0.015f, 0f);
            outline.transform.localScale = new Vector3(0.62f, 0.015f, 0.62f);
            RemoveCollider(outline);
            outline.GetComponent<Renderer>().material = GrayboxMaterials.Create(new Color(1f, 0.82f, 0.12f));

            var station = root.AddComponent<InteractableStation>();
            station.Initialize(game, StationKind.GroundItem, anchor, outline, null);
            station._slot = item;
            station.RefreshSlotVisual();
            return station;
        }

        public void Initialize(
            KitchenGameController game,
            StationKind kind,
            Transform itemAnchor,
            GameObject selectionOutline,
            GridPlacement gridPlacement,
            string sourceIngredientId = null,
            IngredientSourceCardView sourceCard = null)
        {
            _game = game;
            _kind = kind;
            _itemAnchor = itemAnchor;
            _selectionOutline = selectionOutline;
            GridPlacement = gridPlacement;
            _sourceIngredientId = sourceIngredientId;
            _sourceCard = sourceCard;
            if (_kind == StationKind.ChoppingBoard)
            {
                CreateWorkProgressIndicator();
                _knifeMotion = gameObject.AddComponent<ChoppingKnifeMotion>();
                _knifeMotion.Initialize(transform);
            }
            else if (_kind == StationKind.PotHeatSource)
            {
                _slot = WorldItem.FromPot(new CookingPot(GameIds.CommonCookingPot));
                CreateHeatProgressIndicator();
                RefreshSlotVisual();
            }
            else if (_kind == StationKind.PanHeatSource)
            {
                _slot = WorldItem.FromPan(new FryingPan(GameIds.CommonFryingPan));
                CreateHeatProgressIndicator();
                RefreshSlotVisual();
            }
            else if (_kind == StationKind.ExtinguisherCabinet)
            {
                _slot = WorldItem.FromTool(GameIds.FireExtinguisherTool);
                RefreshSlotVisual();
            }
            else if (_kind == StationKind.PlateDispenser)
            {
                _availablePlateCount = 2;
            }
            else if (_kind == StationKind.WashingSink)
            {
                CreateWorkProgressIndicator();
            }

            SetSelected(false);
            RefreshWorkProgress();
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            RefreshOutline();
        }

        public void SetGuided(bool guided)
        {
            _guided = guided;
            RefreshOutline();
        }

        public void PickupOrPlace(PlayerController player)
        {
            if (_incomingItem != null)
            {
                _game.SetDebugMessage("An item is landing here");
                return;
            }

            switch (_kind)
            {
                case StationKind.IngredientCrate:
                    TakeFromIngredientCrate(player);
                    break;
                case StationKind.ContainerDispenser:
                    TakeFromContainerDispenser(player);
                    break;
                case StationKind.ChoppingBoard:
                    UseChoppingBoardPickupOrPlace(player);
                    break;
                case StationKind.Counter:
                    UseCounter(player);
                    break;
                case StationKind.PotHeatSource:
                    UsePotHeatSource(player);
                    break;
                case StationKind.PanHeatSource:
                    UsePanHeatSource(player);
                    break;
                case StationKind.ServingHatch:
                    UseServingHatch(player);
                    break;
                case StationKind.TrashBin:
                    UseTrashBin(player);
                    break;
                case StationKind.GroundItem:
                    TakeGroundItem(player);
                    break;
                case StationKind.BikeDock:
                    UseBikeDock(player);
                    break;
                case StationKind.DeliveryPoint:
                    _game.SetDebugMessage("Ride the loaded bike into this delivery point");
                    break;
                case StationKind.RecoveryBin:
                    TakeRecoveredItem(player);
                    break;
                case StationKind.ExtinguisherCabinet:
                    UseExtinguisherCabinet(player);
                    break;
                case StationKind.FireHazard:
                    _game.SetDebugMessage("Hold WORK while carrying the extinguisher");
                    break;
                case StationKind.PlateDispenser:
                    TakeFromPlateDispenser(player);
                    break;
                case StationKind.DishReturn:
                    TakeFromDishReturn(player);
                    break;
                case StationKind.WashingSink:
                    UseWashingSink(player);
                    break;
                case StationKind.CourierShelf:
                    UseCourierShelf(player);
                    break;
            }
        }

        public bool CanStoreRecoveredItem(out string reason)
        {
            if (_kind != StationKind.RecoveryBin)
            {
                reason = "This station is not a recovery box";
                return false;
            }

            if (_slot != null || _incomingItem != null)
            {
                reason = "Recovery box is occupied";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public bool StoreRecoveredItem(WorldItem item, out string reason)
        {
            if (item == null)
            {
                reason = "There is no item to recover";
                return false;
            }

            if (!CanStoreRecoveredItem(out reason))
            {
                return false;
            }

            _slot = item;
            RefreshSlotVisual();
            _game.SetDebugMessage("Lost item returned to RECOVERY");
            return true;
        }

        public bool Work(PlayerController player, float deltaSeconds, out string reason)
        {
            if (_incomingItem != null)
            {
                reason = "An item is landing here";
                return false;
            }

            if (_kind == StationKind.FireHazard)
            {
                if (player.HeldItem == null || !player.HeldItem.IsTool ||
                    player.HeldItem.ToolId != GameIds.FireExtinguisherTool)
                {
                    reason = "消火器を持ってください";
                    return false;
                }

                if (_fireIncident == null)
                {
                    reason = "消火対象がありません";
                    return false;
                }
                return _fireIncident.AdvanceExtinguish(deltaSeconds, out reason);
            }

            if (_kind == StationKind.WashingSink)
            {
                if (player.HeldItem != null)
                {
                    reason = "皿を洗い場へ置いてから洗ってください";
                    return false;
                }
                if (_slot == null || !_slot.IsContainer || !_slot.Container.IsDirty)
                {
                    reason = "洗う必要のある汚れ皿がありません";
                    return false;
                }
                if (!_slot.Container.TryAdvanceWash(deltaSeconds, out var washed, out reason))
                {
                    return false;
                }
                RefreshWorkProgress();
                if (washed)
                {
                    _game.OnPlateWashed();
                    RefreshSlotVisual();
                }
                return true;
            }

            if (_kind != StationKind.ChoppingBoard)
            {
                reason = "This station has no work action";
                return false;
            }

            if (player.HeldItem != null)
            {
                reason = "Put down the held item before working";
                return false;
            }

            if (_slot == null || !_slot.IsIngredient)
            {
                reason = "Place a raw ingredient on the cutting board";
                return false;
            }

            if (_slot.Ingredient.Preparation != IngredientPreparation.Raw)
            {
                _chopAccumulatorSeconds = 0f;
                _knifeMotion?.Stop();
                reason = "This ingredient is already chopped";
                return false;
            }

            if (deltaSeconds <= 0f)
            {
                reason = "Work time must be greater than zero";
                return false;
            }

            _chopAccumulatorSeconds += deltaSeconds;
            var completed = false;
            while (_chopAccumulatorSeconds + 0.0001f >= ChopIntervalSeconds)
            {
                if (!_game.Session.TryAdvanceChop(
                        _slot.Ingredient,
                        ChopIntervalSeconds,
                        out completed,
                        out reason))
                {
                    _chopAccumulatorSeconds = 0f;
                    return false;
                }

                _chopAccumulatorSeconds = Mathf.Max(0f, _chopAccumulatorSeconds - ChopIntervalSeconds);
                _knifeMotion?.NotifyWorkAdvanced(player.transform.forward);
                RefreshWorkProgress();
                if (completed)
                {
                    _chopAccumulatorSeconds = 0f;
                    break;
                }
            }

            reason = string.Empty;
            if (completed)
            {
                _knifeMotion?.Stop();
            }

            if (completed)
            {
                RefreshSlotVisual();
                _game.SetDebugMessage(_game.Session.Recipe.RequiresHeating
                    ? "Chopping complete. Use PICK/PLACE to take it"
                    : "Chopping complete. Pick it up or collect it with a held container");
            }

            return true;
        }

        public void AttachFireIncident(FireIncidentController incident)
        {
            _fireIncident = incident;
        }

        public bool ReceiveDirtyPlate(WorldItem item)
        {
            if (_kind != StationKind.DishReturn || item == null || !item.IsContainer || !item.Container.IsDirty)
            {
                return false;
            }

            if (_slot == null)
            {
                _slot = item;
                RefreshSlotVisual();
            }
            else
            {
                _returnedDishes.Enqueue(item);
            }
            return true;
        }

        private void UseExtinguisherCabinet(PlayerController player)
        {
            if (_slot != null && player.HeldItem == null)
            {
                if (player.TryGive(_slot, out var pickupReason))
                {
                    _slot = null;
                    RefreshSlotVisual();
                }
                else
                {
                    _game.SetDebugMessage(pickupReason);
                }
                return;
            }

            if (_slot == null && player.HeldItem != null && player.HeldItem.IsTool &&
                player.HeldItem.ToolId == GameIds.FireExtinguisherTool)
            {
                _slot = player.TakeHeld();
                RefreshSlotVisual();
                _game.SetDebugMessage("Fire extinguisher returned");
                return;
            }

            _game.SetDebugMessage("The extinguisher cabinet is occupied");
        }

        private void TakeFromPlateDispenser(PlayerController player)
        {
            if (player.HeldItem != null)
            {
                _game.SetDebugMessage("Hands are full");
                return;
            }
            if (_availablePlateCount <= 0)
            {
                _game.SetDebugMessage("Wash and reuse a returned plate");
                return;
            }
            if (player.TryGive(WorldItem.FromContainer(_game.Session.TakeReusablePlate()), out var reason))
            {
                _availablePlateCount--;
                _game.SetDebugMessage("Clean reusable plate picked up");
            }
            else
            {
                _game.SetDebugMessage(reason);
            }
        }

        private void TakeFromDishReturn(PlayerController player)
        {
            if (_slot == null)
            {
                _game.SetDebugMessage("No returned plate");
                return;
            }
            if (player.HeldItem != null)
            {
                _game.SetDebugMessage("Hands are full");
                return;
            }
            if (player.TryGive(_slot, out var reason))
            {
                _slot = _returnedDishes.Count > 0 ? _returnedDishes.Dequeue() : null;
                RefreshSlotVisual();
            }
            else
            {
                _game.SetDebugMessage(reason);
            }
        }

        private void UseWashingSink(PlayerController player)
        {
            if (_slot == null)
            {
                if (player.HeldItem == null || !player.HeldItem.IsContainer || !player.HeldItem.Container.IsDirty)
                {
                    _game.SetDebugMessage("Place a returned dirty plate in the sink");
                    return;
                }
                _slot = player.TakeHeld();
                RefreshSlotVisual();
                _game.SetDebugMessage("Hold WORK to wash the plate");
                return;
            }

            if (player.HeldItem != null)
            {
                _game.SetDebugMessage("Hands are full");
                return;
            }
            if (_slot.Container.IsDirty)
            {
                _game.SetDebugMessage("Finish washing before taking the plate");
                return;
            }
            if (player.TryGive(_slot, out var reason))
            {
                _slot = null;
                RefreshSlotVisual();
                _game.SetDebugMessage("Clean plate ready for reuse");
            }
            else
            {
                _game.SetDebugMessage(reason);
            }
        }

        private void UseCourierShelf(PlayerController player)
        {
            var item = player.HeldItem;
            var container = item != null && item.IsContainer ? item.Container : null;
            var result = _game.Session.TryServe(container, FulfillmentMethod.Courier);
            if (!result.Succeeded)
            {
                _game.SetDebugMessage(result.Reason);
                return;
            }
            player.TakeHeld();
            _game.OnCourierDispatched(result, transform.position + Vector3.up * 0.8f);
        }

        public bool AdvanceHeating(float deltaSeconds, out string reason)
        {
            if (_kind != StationKind.PotHeatSource || _slot == null || !_slot.IsPot)
            {
                reason = "There is no pot on the heat source";
                return false;
            }

            if (!_game.Session.TryAdvancePot(_slot.Pot, deltaSeconds, out var completed, out reason))
            {
                return false;
            }

            RefreshHeatProgress();
            if (completed)
            {
                RefreshSlotVisual();
                _game.SetDebugMessage("Vegetable soup complete. Remove the pot or fill a container");
            }

            return true;
        }

        public bool AdvancePanHeating(float deltaSeconds, out string reason)
        {
            if (_kind != StationKind.PanHeatSource || _slot == null || !_slot.IsPan)
            {
                reason = "There is no frying pan on the heat source";
                return false;
            }

            if (!_game.Session.TryAdvancePan(
                    _slot.Pan,
                    deltaSeconds,
                    out var completed,
                    out var burned,
                    out reason))
            {
                return false;
            }

            RefreshHeatProgress();
            if (burned)
            {
                RefreshSlotVisual();
                _game.SetDebugMessage("Patty burned. Pick up the pan and carry it to TRASH");
            }
            else if (completed)
            {
                RefreshSlotVisual();
                _game.SetDebugMessage($"Patty cooked. You have {FryingPan.BurnGraceSeconds:0} seconds to fill a container");
            }
            else if (!_panBurnWarningShown && _slot.Pan.IsCooked &&
                     _slot.Pan.RemainingBurnSeconds <= PanBurnWarningSeconds)
            {
                _panBurnWarningShown = true;
                _game.SetDebugMessage($"Warning: patty burns in {PanBurnWarningSeconds:0} seconds. Remove the pan or fill a container");
            }

            return true;
        }

        private void Update()
        {
            if (_kind == StationKind.PotHeatSource && _game != null && !_game.IsFinished &&
                _slot != null && _slot.IsPot && !_slot.Pot.IsCooked)
            {
                AdvanceHeating(Time.deltaTime, out _);
            }
            else if (_kind == StationKind.PanHeatSource && _game != null && !_game.IsFinished &&
                     _slot != null && _slot.IsPan && !_slot.Pan.IsEmpty && !_slot.Pan.IsBurned)
            {
                AdvancePanHeating(Time.deltaTime, out _);
            }
        }

        public bool CanReceiveThrown(WorldItem item, out string reason)
        {
            if (item == null)
            {
                reason = "Nothing to throw";
                return false;
            }

            if (_kind == StationKind.GroundItem)
            {
                reason = "Ground items cannot receive a throw";
                return false;
            }

            if (_kind == StationKind.BikeDock && _bike != null)
            {
                return _bike.CanLoad(item, out reason);
            }

            if (_incomingItem != null)
            {
                reason = "Landing station is occupied";
                return false;
            }

            if (_kind == StationKind.TrashBin)
            {
                reason = string.Empty;
                return true;
            }

            if (_kind == StationKind.PotHeatSource && _slot != null && _slot.IsPot && item.IsIngredient)
            {
                return _slot.Pot.CanAdd(item.Ingredient, _game.Session.Recipe, out reason);
            }

            if (_slot != null)
            {
                reason = "Landing station is occupied";
                return false;
            }

            if (_kind == StationKind.Counter ||
                (_kind == StationKind.ChoppingBoard && item.IsIngredient))
            {
                reason = string.Empty;
                return true;
            }

            reason = "This station cannot receive thrown items";
            return false;
        }

        public bool TryReserveThrown(WorldItem item, out string reason)
        {
            if (!CanReceiveThrown(item, out reason))
            {
                return false;
            }

            _incomingItem = item;
            RefreshOutline();
            return true;
        }

        public void CompleteThrownDelivery(WorldItem item)
        {
            if (!ReferenceEquals(_incomingItem, item))
            {
                return;
            }

            if (_kind == StationKind.BikeDock && _bike != null)
            {
                _incomingItem = null;
                if (_bike.TryLoad(item, out var loadReason))
                {
                    _game.SetDebugMessage("Completed container thrown onto bike. Use PICK/PLACE to ride");
                }
                else
                {
                    _game.SetDebugMessage(loadReason);
                }

                RefreshOutline();
                return;
            }

            if (_kind == StationKind.TrashBin)
            {
                _incomingItem = null;
                _game.SetDebugMessage("Thrown item discarded");
                RefreshOutline();
                return;
            }

            if (_kind == StationKind.PotHeatSource && _slot != null && _slot.IsPot && item.IsIngredient)
            {
                _incomingItem = null;
                if (_game.Session.TryAddToPot(_slot.Pot, item.Ingredient, out var addReason))
                {
                    _game.OnIngredientThrownIntoPot();
                    RefreshSlotVisual();
                    RefreshHeatProgress();
                    _game.SetDebugMessage(_slot.Pot.Contents.Count == _game.Session.Recipe.RequiredComponents.Count
                        ? "Thrown ingredients ready. Heating started"
                        : "Ingredient thrown into pot");
                }
                else
                {
                    _game.SetDebugMessage(addReason);
                }

                RefreshOutline();
                return;
            }

            _slot = item;
            _incomingItem = null;
            RefreshSlotVisual();
            RefreshWorkProgress();
            RefreshOutline();
        }

        private void RefreshOutline()
        {
            if (_selectionOutline != null)
            {
                _selectionOutline.SetActive(_selected || _guided || _incomingItem != null);
            }
        }

        private void TakeFromIngredientCrate(PlayerController player)
        {
            if (string.IsNullOrWhiteSpace(_sourceIngredientId))
            {
                _game.SetDebugMessage("Ingredient source is not configured");
                return;
            }

            if (player.HeldItem != null)
            {
                _game.SetDebugMessage("Hands are full");
                return;
            }

            var ingredient = _game.Session.TakeRawIngredient(_sourceIngredientId);
            if (player.TryGive(WorldItem.FromIngredient(ingredient), out var reason))
            {
                _sourceCard?.PlayDispenseFeedback();
                _game.SetDebugMessage("Picked up " + _sourceIngredientId);
            }
            else
            {
                _game.SetDebugMessage(reason);
            }
        }

        private void TakeGroundItem(PlayerController player)
        {
            if (_slot == null)
            {
                _game.SetDebugMessage("Ground item is no longer available");
                return;
            }

            if (player.TryGive(_slot, out var reason))
            {
                _slot = null;
                RefreshSlotVisual();
                _game.SetDebugMessage("Picked up item from ground");
                Destroy(gameObject);
            }
            else
            {
                _game.SetDebugMessage(reason);
            }
        }

        private void TakeRecoveredItem(PlayerController player)
        {
            if (_slot == null)
            {
                _game.SetDebugMessage("RECOVERY is empty");
                return;
            }

            if (player.TryGive(_slot, out var reason))
            {
                _slot = null;
                RefreshSlotVisual();
                _game.SetDebugMessage("Recovered lost item");
            }
            else
            {
                _game.SetDebugMessage(reason);
            }
        }

        private void UseBikeDock(PlayerController player)
        {
            if (_bike == null)
            {
                _game.SetDebugMessage("Bike is unavailable");
                return;
            }

            if (player.HeldItem != null)
            {
                if (_bike.TryLoad(player.HeldItem, out var loadReason))
                {
                    player.TakeHeld();
                    _game.SetDebugMessage("Container loaded. Use PICK/PLACE again to ride");
                }
                else
                {
                    _game.SetDebugMessage(loadReason);
                }

                return;
            }

            if (_bike.TryMount(player, out var mountReason))
            {
                _game.SetDebugMessage("Bike mounted. W/Up accelerator, S/Down reverse, A/D or arrows steer");
            }
            else
            {
                _game.SetDebugMessage(mountReason);
            }
        }

        private void TakeFromContainerDispenser(PlayerController player)
        {
            if (player.TryGive(WorldItem.FromContainer(_game.Session.TakeDeliveryContainer()), out var reason))
            {
                _game.SetDebugMessage("Picked up a delivery container");
            }
            else
            {
                _game.SetDebugMessage(reason);
            }
        }

        private void UseChoppingBoardPickupOrPlace(PlayerController player)
        {
            if (_slot == null)
            {
                if (player.HeldItem == null || !player.HeldItem.IsIngredient)
                {
                    _game.SetDebugMessage("Place raw lettuce on the cutting board");
                    return;
                }

                _slot = player.TakeHeld();
                RefreshSlotVisual();
                _game.SetDebugMessage("Ingredient placed. Hold WORK to chop");
                return;
            }

            if (!_slot.IsIngredient)
            {
                _game.SetDebugMessage("Only ingredients can be chopped here");
                return;
            }

            if (player.HeldItem != null && player.HeldItem.IsContainer)
            {
                FillHeldContainerFromSlottedIngredient(player, "Container filled from chopping board");
                return;
            }

            if (player.HeldItem != null)
            {
                _game.SetDebugMessage("Hands are full");
                return;
            }

            if (player.TryGive(_slot, out var pickupReason))
            {
                _slot = null;
                RefreshSlotVisual();
                _game.SetDebugMessage("Picked up ingredient");
            }
            else
            {
                _game.SetDebugMessage(pickupReason);
            }
        }

        private void UseCounter(PlayerController player)
        {
            if (_slot == null)
            {
                if (player.HeldItem == null)
                {
                    _game.SetDebugMessage("Counter is empty");
                    return;
                }

                _slot = player.TakeHeld();
                RefreshSlotVisual();
                _game.SetDebugMessage("Item placed on counter");
                return;
            }

            if (TryTransferCookwareAndContainer(player))
            {
                return;
            }

            if (_slot.IsIngredient && player.HeldItem != null && player.HeldItem.IsContainer)
            {
                FillHeldContainerFromSlottedIngredient(player, "Container filled from counter");
                return;
            }

            if (_slot.IsContainer && player.HeldItem != null && player.HeldItem.IsIngredient)
            {
                if (_game.Session.TryAssemble(_slot.Container, player.HeldItem.Ingredient, out var assembleReason))
                {
                    player.TakeHeld();
                    RefreshSlotVisual();
                    _game.SetDebugMessage(_slot.Container.IsComplete
                        ? "Lettuce salad complete"
                        : "Ingredient added, but recipe is incomplete");
                }
                else
                {
                    _game.SetDebugMessage(assembleReason);
                }

                return;
            }

            if (player.HeldItem != null)
            {
                _game.SetDebugMessage("Counter is occupied");
                return;
            }

            if (player.TryGive(_slot, out var pickupReason))
            {
                _slot = null;
                RefreshSlotVisual();
                _game.SetDebugMessage("Picked up item from counter");
            }
            else
            {
                _game.SetDebugMessage(pickupReason);
            }
        }

        private void FillHeldContainerFromSlottedIngredient(PlayerController player, string successMessage)
        {
            if (_game.Session.TryAssemble(
                    player.HeldItem.Container,
                    _slot.Ingredient,
                    out var assembleReason))
            {
                _slot = null;
                player.RefreshHeldVisual();
                RefreshSlotVisual();
                _game.SetDebugMessage(player.HeldItem.Container.IsComplete
                    ? successMessage + "; recipe complete"
                    : successMessage + "; recipe incomplete");
            }
            else
            {
                _game.SetDebugMessage(assembleReason);
            }
        }

        private bool TryTransferCookwareAndContainer(PlayerController player)
        {
            var held = player.HeldItem;
            if (_slot == null || held == null)
            {
                return false;
            }

            WorldItem cookware;
            WorldItem container;
            if ((_slot.IsPot || _slot.IsPan) && held.IsContainer)
            {
                cookware = _slot;
                container = held;
            }
            else if (_slot.IsContainer && (held.IsPot || held.IsPan))
            {
                cookware = held;
                container = _slot;
            }
            else
            {
                return false;
            }

            bool succeeded;
            string reason;
            if (cookware.IsPot)
            {
                succeeded = _game.Session.TryFillContainerFromPot(
                    cookware.Pot,
                    container.Container,
                    out reason);
            }
            else
            {
                succeeded = _game.Session.TryFillContainerFromPan(
                    cookware.Pan,
                    container.Container,
                    out reason);
            }

            if (!succeeded)
            {
                _game.SetDebugMessage(reason);
                return true;
            }

            _panBurnWarningShown = false;
            player.RefreshHeldVisual();
            RefreshSlotVisual();
            RefreshHeatProgress();
            _game.SetDebugMessage(cookware.IsPot
                ? "Cooked pot contents transferred to container"
                : "Cooked pan contents transferred to container");
            return true;
        }

        private void UsePotHeatSource(PlayerController player)
        {
            if (_slot == null)
            {
                if (player.HeldItem == null || !player.HeldItem.IsPot)
                {
                    _game.SetDebugMessage("Place a cooking pot on the heat source");
                    return;
                }

                _slot = player.TakeHeld();
                RefreshSlotVisual();
                RefreshHeatProgress();
                _game.SetDebugMessage("Pot returned to heat; cooking resumes");
                return;
            }

            if (!_slot.IsPot)
            {
                _game.SetDebugMessage("The heat source is occupied");
                return;
            }

            if (player.HeldItem == null)
            {
                if (player.TryGive(_slot, out var pickupReason))
                {
                    _slot = null;
                    RefreshSlotVisual();
                    RefreshHeatProgress();
                    _game.SetDebugMessage("Pot removed from heat; cooking paused");
                }
                else
                {
                    _game.SetDebugMessage(pickupReason);
                }

                return;
            }

            if (player.HeldItem.IsIngredient)
            {
                if (_game.Session.TryAddToPot(_slot.Pot, player.HeldItem.Ingredient, out var addReason))
                {
                    player.TakeHeld();
                    RefreshSlotVisual();
                    RefreshHeatProgress();
                    _game.SetDebugMessage(_slot.Pot.Contents.Count == _game.Session.Recipe.RequiredComponents.Count
                        ? "Ingredients ready. Heating started"
                        : "Ingredient added to pot");
                }
                else
                {
                    _game.SetDebugMessage(addReason);
                }

                return;
            }

            if (player.HeldItem.IsContainer)
            {
                TryTransferCookwareAndContainer(player);
                return;
            }

            _game.SetDebugMessage("The heat source already has a pot");
        }

        private void UsePanHeatSource(PlayerController player)
        {
            if (_slot == null)
            {
                if (player.HeldItem == null || !player.HeldItem.IsPan)
                {
                    _game.SetDebugMessage("Place a frying pan on the heat source");
                    return;
                }

                _slot = player.TakeHeld();
                _panBurnWarningShown = _slot.Pan.IsCooked && _slot.Pan.RemainingBurnSeconds <= 2f;
                RefreshSlotVisual();
                RefreshHeatProgress();
                _game.SetDebugMessage("Pan returned to heat; frying resumes");
                return;
            }

            if (!_slot.IsPan)
            {
                _game.SetDebugMessage("The heat source is occupied");
                return;
            }

            if (player.HeldItem == null)
            {
                var panWasBurned = _slot.Pan.IsBurned;
                if (player.TryGive(_slot, out var pickupReason))
                {
                    _slot = null;
                    RefreshSlotVisual();
                    RefreshHeatProgress();
                    _game.SetDebugMessage(panWasBurned
                        ? "Burned pan picked up. Carry it to TRASH"
                        : "Pan removed from heat; frying paused");
                }
                else
                {
                    _game.SetDebugMessage(pickupReason);
                }

                return;
            }

            if (player.HeldItem.IsIngredient)
            {
                if (_game.Session.TryAddToPan(_slot.Pan, player.HeldItem.Ingredient, out var addReason))
                {
                    player.TakeHeld();
                    _panBurnWarningShown = false;
                    RefreshSlotVisual();
                    RefreshHeatProgress();
                    _game.SetDebugMessage("Patty placed in pan. Watch the heat gauge");
                }
                else
                {
                    _game.SetDebugMessage(addReason);
                }

                return;
            }

            if (player.HeldItem.IsContainer)
            {
                TryTransferCookwareAndContainer(player);
                return;
            }

            _game.SetDebugMessage("The heat source already has a frying pan");
        }

        private void UseServingHatch(PlayerController player)
        {
            var container = player.HeldItem != null && player.HeldItem.IsContainer
                ? player.HeldItem.Container
                : null;
            var result = _game.Session.TryServe(container);
            if (!result.Succeeded)
            {
                _game.SetDebugMessage("Cannot serve: " + result.Reason);
                return;
            }

            var servedItem = player.TakeHeld();
            if (_game.Session.Stage.Id == GameIds.TutorialDishwashingStage &&
                servedItem != null && servedItem.IsContainer &&
                servedItem.Container.MarkDirtyAfterServing())
            {
                var dishReturn = Object.FindObjectsByType<InteractableStation>(FindObjectsSortMode.None)
                    .FirstOrDefault(station => station.Kind == StationKind.DishReturn);
                dishReturn?.ReceiveDirtyPlate(servedItem);
            }
            _game.OnOrderServed(result);
        }

        private void UseTrashBin(PlayerController player)
        {
            if (player.HeldItem == null)
            {
                _game.SetDebugMessage("Nothing to discard");
                return;
            }

            if (player.HeldItem.IsPan)
            {
                if (!player.HeldItem.Pan.DiscardBurnedContents())
                {
                    _game.SetDebugMessage("Only burned pan contents can be discarded here");
                    return;
                }

                player.RefreshHeldVisual();
                _game.SetDebugMessage("Burned patty discarded; empty pan remains in hand");
                return;
            }

            player.TakeHeld();
            _game.SetDebugMessage("Discarded item");
        }

        private void RefreshSlotVisual()
        {
            if (_itemVisual != null)
            {
                Destroy(_itemVisual);
            }

            if (_slot != null)
            {
                _itemVisual = WorldItemVisualFactory.Create(_slot, _itemAnchor);
                if (_kind == StationKind.ChoppingBoard &&
                    _slot.IsIngredient &&
                    _slot.Ingredient.IngredientId == GameIds.CarrotIngredient &&
                    _slot.Ingredient.Preparation == IngredientPreparation.Raw)
                {
                    _itemVisual.transform.localRotation =
                        Quaternion.Euler(0f, ChoppingBoardCarrotYawDegrees, 0f);
                }
                if (_kind == StationKind.WashingSink && _slot.IsContainer && _slot.Container.IsDirty)
                {
                    _itemVisual.AddComponent<DishwashingMotion>().Initialize(_slot.Container);
                }
            }

            if (_kind == StationKind.ChoppingBoard)
            {
                _knifeMotion?.SetBoardOccupied(_slot != null);
            }

            RefreshWorkProgress();
            RefreshHeatProgress();
        }

        private void CreateWorkProgressIndicator()
        {
            _workProgressRoot = new GameObject("Work Progress");
            _workProgressRoot.transform.SetParent(transform, false);
            _workProgressRoot.transform.localPosition = new Vector3(0f, 0.83f, -0.34f);

            var backing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backing.name = "Progress Backing";
            backing.transform.SetParent(_workProgressRoot.transform, false);
            backing.transform.localScale = new Vector3(0.8f, 0.045f, 0.14f);
            RemoveCollider(backing);
            backing.GetComponent<Renderer>().material = GrayboxMaterials.Create(new Color(0.12f, 0.08f, 0.15f));

            var fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fill.name = "Progress Fill";
            fill.transform.SetParent(_workProgressRoot.transform, false);
            fill.transform.localPosition = new Vector3(-0.35f, 0.03f, 0f);
            fill.transform.localScale = new Vector3(0f, 0.055f, 0.09f);
            RemoveCollider(fill);
            fill.GetComponent<Renderer>().material = GrayboxMaterials.Create(new Color(0.95f, 0.58f, 0.12f));
            _workProgressFill = fill.transform;
        }

        private void RefreshWorkProgress()
        {
            if (_workProgressRoot == null || _workProgressFill == null)
            {
                return;
            }

            var chopping = _kind == StationKind.ChoppingBoard && _slot != null && _slot.IsIngredient &&
                           _slot.Ingredient.Preparation == IngredientPreparation.Raw;
            var washing = _kind == StationKind.WashingSink && _slot != null && _slot.IsContainer &&
                          _slot.Container.IsDirty;
            var show = chopping || washing;
            _workProgressRoot.SetActive(show);
            if (!show)
            {
                _chopAccumulatorSeconds = 0f;
                return;
            }

            var progress = washing
                ? Mathf.Clamp01(_slot.Container.WashProgress)
                : Mathf.Clamp01(_slot.Ingredient.PreparationProgress);
            _workProgressFill.localScale = new Vector3(0.7f * progress, 0.055f, 0.09f);
            _workProgressFill.localPosition = new Vector3(-0.35f + 0.35f * progress, 0.03f, 0f);
        }

        private static void RemoveCollider(GameObject target)
        {
            var collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        private void CreateHeatProgressIndicator()
        {
            _heatProgressRoot = new GameObject("Heat Progress");
            _heatProgressRoot.transform.SetParent(transform, false);
            _heatProgressRoot.transform.localPosition = new Vector3(0f, 0.76f, -0.36f);

            var backing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backing.name = "Heat Backing";
            backing.transform.SetParent(_heatProgressRoot.transform, false);
            backing.transform.localScale = new Vector3(0.8f, 0.045f, 0.14f);
            RemoveCollider(backing);
            backing.GetComponent<Renderer>().material = GrayboxMaterials.Create(new Color(0.14f, 0.08f, 0.05f));

            var fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fill.name = "Heat Fill";
            fill.transform.SetParent(_heatProgressRoot.transform, false);
            fill.transform.localPosition = new Vector3(-0.35f, 0.03f, 0f);
            fill.transform.localScale = new Vector3(0f, 0.055f, 0.09f);
            RemoveCollider(fill);
            fill.GetComponent<Renderer>().material = GrayboxMaterials.Create(new Color(1f, 0.36f, 0.08f));
            _heatProgressFill = fill.transform;
            RefreshHeatProgress();
        }

        private void RefreshHeatProgress()
        {
            if (_heatProgressRoot == null || _heatProgressFill == null)
            {
                return;
            }

            var show = _slot != null &&
                       ((_slot.IsPot && !_slot.Pot.IsEmpty) ||
                        (_slot.IsPan && !_slot.Pan.IsEmpty));
            _heatProgressRoot.SetActive(show);
            if (!show)
            {
                return;
            }

            var progress = _slot.IsPot
                ? Mathf.Clamp01(_slot.Pot.HeatProgress)
                : _slot.Pan.HeatSeconds >= FryingPan.RequiredCookSeconds
                    ? Mathf.Clamp01(1f - _slot.Pan.BurnProgress)
                    : Mathf.Clamp01(_slot.Pan.CookProgress);
            _heatProgressFill.localScale = new Vector3(0.7f * progress, 0.055f, 0.09f);
            _heatProgressFill.localPosition = new Vector3(-0.35f + 0.35f * progress, 0.03f, 0f);
        }
    }

    public sealed class IngredientSourceCardView : MonoBehaviour
    {
        public const float IngredientIconScale = 0.80f;
        private const float FeedbackDuration = 0.14f;
        private Vector3 _restingScale = Vector3.one;
        private float _feedbackRemaining;

        public string IngredientId { get; private set; }
        public Texture DisplayTexture { get; private set; }
        public Texture2D PhotoTexture => DisplayTexture as Texture2D;

        public void Initialize(string ingredientId, Texture displayTexture)
        {
            IngredientId = ingredientId;
            DisplayTexture = displayTexture;
            _restingScale = transform.localScale;
        }

        public void PlayDispenseFeedback()
        {
            _feedbackRemaining = FeedbackDuration;
        }

        private void Update()
        {
            if (_feedbackRemaining <= 0f)
            {
                transform.localScale = _restingScale;
                return;
            }

            _feedbackRemaining = Mathf.Max(0f, _feedbackRemaining - Time.deltaTime);
            var progress = 1f - _feedbackRemaining / FeedbackDuration;
            var pulse = 1f + Mathf.Sin(progress * Mathf.PI) * 0.12f;
            transform.localScale = _restingScale * pulse;
        }
    }
}
