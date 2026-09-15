using System.Collections.Generic;
using CookedOut.Application;
using CookedOut.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CookedOut.Presentation
{
    public enum OrderTicketUrgency
    {
        Safe,
        Normal,
        Urgent,
        Served,
        Expired
    }

    public enum OrderTicketDestination
    {
        ServingHatch,
        Bike
    }

    public enum OrderTicketHolder
    {
        Kitchen,
        Player,
        Bike
    }

    public sealed class OrderIngredientCardView : MonoBehaviour
    {
        public string IngredientId { get; private set; }
        public IngredientPreparation Preparation { get; private set; }
        public bool ShowsChop { get; private set; }
        public bool ShowsHeat { get; private set; }
        public Texture SourceTexture { get; private set; }

        public void Initialize(
            string ingredientId,
            IngredientPreparation preparation,
            bool showsChop,
            bool showsHeat,
            Texture sourceTexture)
        {
            IngredientId = ingredientId;
            Preparation = preparation;
            ShowsChop = showsChop;
            ShowsHeat = showsHeat;
            SourceTexture = sourceTexture;
        }
    }

    public sealed class OrderTicketView : MonoBehaviour
    {
        public const float IngredientPhotoSize = 90f;
        public const float IngredientTextureSize = 92f;
        public const float CompletedFoodPhotoSize = 132f;
        public const float PotProcessIconSize = 84f;

        private const float SafeThreshold = 2f / 3f;
        private const float UrgentThreshold = 1f / 3f;
        private const float TimerWidth = 230f;

        private static readonly Color SafeColor = new Color(0.18f, 0.72f, 0.38f);
        private static readonly Color NormalColor = new Color(0.95f, 0.68f, 0.12f);
        private static readonly Color UrgentColor = new Color(0.9f, 0.22f, 0.18f);
        private static readonly Color ServedColor = new Color(0.12f, 0.68f, 0.62f);
        private static readonly Color ExpiredColor = new Color(0.36f, 0.30f, 0.34f);
        private static readonly Color PaperColor = new Color(0.96f, 0.97f, 0.94f, 0.98f);
        private static readonly Dictionary<string, Texture2D> FallbackFoodTextures =
            new Dictionary<string, Texture2D>();

        private Image _frame;
        private RawImage _foodImage;
        private RectTransform _ingredientRow;
        private RectTransform _processRow;
        private RectTransform _timerFill;
        private Image _timerFillImage;
        private readonly List<OrderIngredientCardView> _ingredientCards =
            new List<OrderIngredientCardView>();
        private GameObject _processIcons;
        private GameObject _pauseIcon;
        private GameObject _servedIcon;
        private GameObject _expiredIcon;
        private GameObject _statusBadge;
        private KitchenSession _session;
        private PlayerController _player;
        private DeliveryBikeController _bike;
        private RectTransform _ticketRect;
        private Vector2 _restAnchoredPosition;
        private Vector3 _restScale;
        private string _displayedOrderId;
        private string _displayedRecipeId;
        private int _orderIndex;
        private float _exitAnimationAlpha = 1f;

        public float TimerNormalized { get; private set; }
        public OrderTicketUrgency Urgency { get; private set; }
        public OrderTicketDestination Destination { get; private set; }
        public OrderTicketHolder Holder { get; private set; }
        public int IngredientCardCount => _ingredientCards.Count;
        public float TimerFillWidth => _timerFill == null ? 0f : _timerFill.sizeDelta.x;
        public bool HasContinuousTimer => _timerFillImage != null;
        public bool IsPauseIconVisible => _pauseIcon != null && _pauseIcon.activeSelf;
        public bool IsServedIconVisible => _servedIcon != null && _servedIcon.activeSelf;
        public bool IsExpiredIconVisible => _expiredIcon != null && _expiredIcon.activeSelf;
        public RawImage FoodImage => _foodImage;
        public IReadOnlyList<OrderIngredientCardView> IngredientCards => _ingredientCards;
        public int OrderIndex => _orderIndex;
        public string DisplayedOrderId => _displayedOrderId;
        public float ExitAnimationAlpha => _exitAnimationAlpha;

        public void Initialize(Image frame, RawImage foodImage, int orderIndex = 0)
        {
            _frame = frame;
            _foodImage = foodImage;
            _orderIndex = Mathf.Max(0, orderIndex);
            _ticketRect = transform as RectTransform;
            _restAnchoredPosition = _ticketRect == null ? Vector2.zero : _ticketRect.anchoredPosition;
            _restScale = transform.localScale;
            BuildNonverbalVisuals();
        }

        public void Configure(KitchenSession session)
        {
            Configure(session, _orderIndex);
        }

        public void Configure(KitchenSession session, int orderIndex)
        {
            _session = session;
            _orderIndex = Mathf.Max(0, orderIndex);
            if (_session == null)
            {
                gameObject.SetActive(false);
                return;
            }

            _player = Object.FindFirstObjectByType<PlayerController>();
            _bike = Object.FindFirstObjectByType<DeliveryBikeController>();
            Destination = _session.Stage.Id == GameIds.TutorialThrowDeliveryStage ||
                          _session.Stage.Id == GameIds.TutorialCombinedDeliveryStage
                ? OrderTicketDestination.Bike
                : OrderTicketDestination.ServingHatch;
            RefreshNow(false);
        }

        public void RefreshNow(bool paused)
        {
            if (_session == null || _session.State == null || _orderIndex >= _session.State.Orders.Count)
            {
                _displayedOrderId = null;
                ResetExitAnimation();
                gameObject.SetActive(false);
                return;
            }

            var order = _session.State.Orders[_orderIndex];
            if (_displayedOrderId != order.Id)
            {
                _displayedOrderId = order.Id;
                ResetExitAnimation();
            }
            if (_displayedRecipeId != order.RecipeId)
            {
                _displayedRecipeId = order.RecipeId;
                var recipe = _session.RecipeFor(order.RecipeId);
                BuildIngredientCards(recipe);
                ConfigureFoodImage(order.RecipeId);
            }
            gameObject.SetActive(true);
            var duration = Mathf.Max(0.001f, _session.Stage.OrderDurationSeconds);
            TimerNormalized = Mathf.Clamp01(order.RemainingSeconds / duration);
            var color = ResolveColor(order.Status, TimerNormalized, out var urgency);
            Urgency = urgency;
            Holder = HoldsOrderedMeal(_player == null ? null : _player.HeldItem)
                ? OrderTicketHolder.Player
                : HoldsOrderedMeal(_bike == null ? null : _bike.Cargo)
                    ? OrderTicketHolder.Bike
                    : OrderTicketHolder.Kitchen;
            _pauseIcon.SetActive(paused && order.Status == OrderStatus.Active);
            _servedIcon.SetActive(order.Status == OrderStatus.Served);
            _expiredIcon.SetActive(order.Status == OrderStatus.Expired);
            _statusBadge.SetActive(
                _pauseIcon.activeSelf || _servedIcon.activeSelf || _expiredIcon.activeSelf);
            RefreshContinuousTimer(order.Status, color);

            if (_frame != null)
            {
                var paperColor = order.Status == OrderStatus.Expired
                    ? new Color(0.58f, 0.53f, 0.52f, 0.97f)
                    : PaperColor;
                _frame.color = paperColor;
            }

            if (order.Status == OrderStatus.Expired)
            {
                ApplyExpiredExitAnimation(order.TerminalElapsedSeconds);
            }
            else
            {
                ResetExitAnimation();
            }
        }

        private void ApplyExpiredExitAnimation(float elapsedSeconds)
        {
            var duration = Mathf.Max(0.1f, _session.Stage.OrderTicketExitSeconds);
            var progress = Mathf.Clamp01(elapsedSeconds / duration);
            var shake = Mathf.Sin(progress * Mathf.PI * 8f) * 10f * (1f - progress);
            if (_ticketRect != null)
            {
                _ticketRect.anchoredPosition = _restAnchoredPosition + Vector2.right * shake;
            }
            transform.localScale = _restScale * Mathf.Lerp(1f, 0.82f, progress);
            ApplyTicketAlpha(1f - progress);
        }

        private void ResetExitAnimation()
        {
            if (_ticketRect != null)
            {
                _ticketRect.anchoredPosition = _restAnchoredPosition;
            }
            transform.localScale = _restScale;
            ApplyTicketAlpha(1f);
        }

        private void ApplyTicketAlpha(float alpha)
        {
            _exitAnimationAlpha = Mathf.Clamp01(alpha);
            foreach (var graphic in GetComponentsInChildren<Graphic>(true))
            {
                graphic.canvasRenderer.SetAlpha(_exitAnimationAlpha);
            }
        }

        private void BuildNonverbalVisuals()
        {
            if (_frame == null || _foodImage == null)
            {
                return;
            }

            var lowerBackground = CreateImage(transform, "Ingredient Area Background", new Vector2(0f, -82f),
                new Vector2(268f, 164f), new Color(0.69f, 0.88f, 0.95f, 1f));
            lowerBackground.transform.SetAsFirstSibling();

            var timerBacking = CreateImage(transform, "Order Timer Backing", new Vector2(0f, 147f),
                new Vector2(TimerWidth, 12f), new Color(0.46f, 0.53f, 0.52f, 0.34f));
            KitchenHudVisuals.ApplyRounded(timerBacking);
            _timerFillImage = CreateImage(timerBacking.transform, "Order Timer Fill", Vector2.zero,
                new Vector2(TimerWidth, 8f), SafeColor);
            KitchenHudVisuals.ApplyRounded(_timerFillImage);
            _timerFill = _timerFillImage.rectTransform;
            _timerFill.anchorMin = new Vector2(0f, 0.5f);
            _timerFill.anchorMax = new Vector2(0f, 0.5f);
            _timerFill.pivot = new Vector2(0f, 0.5f);
            _timerFill.anchoredPosition = Vector2.zero;

            _ingredientRow = CreateRect(transform, "Order Ingredient Cards", new Vector2(0f, -70f),
                new Vector2(264f, 132f));
            _processRow = CreateRect(transform, "Order Required Process", new Vector2(0f, -134f),
                new Vector2(116f, 48f));

            var statusBadge = CreateImage(transform, "Order State Badge", new Vector2(123f, 160f),
                new Vector2(42f, 42f), new Color(0.98f, 0.95f, 0.86f, 1f));
            _statusBadge = statusBadge.gameObject;
            KitchenHudVisuals.ApplyCircle(statusBadge);
            _pauseIcon = KitchenHudVisuals.BuildOrderStatusIcon(
                statusBadge.transform, OrderTicketUrgency.Normal, NormalColor);
            _servedIcon = KitchenHudVisuals.BuildOrderStatusIcon(
                statusBadge.transform, OrderTicketUrgency.Served, ServedColor);
            _expiredIcon = KitchenHudVisuals.BuildOrderStatusIcon(
                statusBadge.transform, OrderTicketUrgency.Expired, UrgentColor);
        }

        private void BuildIngredientCards(RecipeDefinition recipe)
        {
            foreach (var card in _ingredientCards)
            {
                if (card != null)
                {
                    Object.Destroy(card.gameObject);
                }
            }
            _ingredientCards.Clear();

            if (recipe == null)
            {
                return;
            }

            var componentCount = 0;
            foreach (var component in recipe.RequiredComponents)
            {
                componentCount += component.Count;
            }

            if (_processIcons != null)
            {
                Object.Destroy(_processIcons);
                _processIcons = null;
            }

            const float cardWidth = 96f;
            const float cardGap = 2f;
            var totalWidth = componentCount * cardWidth + Mathf.Max(0, componentCount - 1) * cardGap;
            var cardIndex = 0;
            foreach (var component in recipe.RequiredComponents)
            {
                for (var count = 0; count < component.Count; count++)
                {
                    var x = -totalWidth * 0.5f + cardWidth * 0.5f + cardIndex * (cardWidth + cardGap);
                    var cardImage = CreateImage(_ingredientRow, "Ingredient Card " + component.IngredientId,
                        new Vector2(x, 0f), new Vector2(cardWidth, 104f),
                        new Color(0f, 0f, 0f, 0f));
                    var pictureMask = CreateImage(cardImage.transform, "Ingredient Source Mask", Vector2.zero,
                        new Vector2(IngredientPhotoSize, IngredientPhotoSize), new Color(0.97f, 0.95f, 0.91f, 1f));
                    KitchenHudVisuals.ApplyCircle(pictureMask);
                    var mask = pictureMask.gameObject.AddComponent<Mask>();
                    mask.showMaskGraphic = true;
                    var sourceTexture = ResolveIngredientSourceTexture(component.IngredientId);
                    var sourceImage = CreateRawImage(pictureMask.transform, "Ingredient Source Image",
                        Vector2.zero, new Vector2(IngredientTextureSize, IngredientTextureSize), sourceTexture);
                    sourceImage.uvRect = new Rect(0f, 0f, 1f, 1f);
                    var showsChop = false;
                    var showsHeat = recipe.RequiresHeating;

                    var cardView = cardImage.gameObject.AddComponent<OrderIngredientCardView>();
                    cardView.Initialize(
                        component.IngredientId,
                        component.Preparation,
                        showsChop,
                        showsHeat,
                        sourceTexture);
                    _ingredientCards.Add(cardView);
                    cardIndex++;
                }
            }

            if (!recipe.RequiresHeating)
            {
                return;
            }

            if (recipe.Id == GameIds.VegetableSoupRecipe)
            {
                var potTexture = Resources.Load<Texture2D>("OrderIcons/order_process_pot_silhouette_sv1");
                var potImage = CreateRawImage(_processRow, "Generated Pot Silhouette", Vector2.zero,
                    Vector2.one * PotProcessIconSize, potTexture);
                _processIcons = potImage.gameObject;
            }
            else
            {
                _processIcons = KitchenHudVisuals.BuildOrderProcessIcons(
                    _processRow,
                    false,
                    true,
                    true,
                    KitchenHudVisuals.DeepPlum);
                _processIcons.transform.localScale = Vector3.one * 1.35f;
            }
        }

        private void RefreshContinuousTimer(OrderStatus status, Color activeColor)
        {
            if (_timerFill == null || _timerFillImage == null)
            {
                return;
            }

            var normalized = status == OrderStatus.Expired ? 0f : TimerNormalized;
            _timerFill.sizeDelta = new Vector2(TimerWidth * normalized, _timerFill.sizeDelta.y);
            _timerFillImage.color = activeColor;
        }

        private static Texture ResolveIngredientSourceTexture(string ingredientId)
        {
            var sourceCards = Object.FindObjectsByType<IngredientSourceCardView>(FindObjectsSortMode.None);
            foreach (var sourceCard in sourceCards)
            {
                if (sourceCard.IngredientId == ingredientId && sourceCard.DisplayTexture != null)
                {
                    return sourceCard.DisplayTexture;
                }
            }

            var resourcePath = ingredientId == GameIds.LettuceIngredient
                ? "IngredientSourceCards/ingredient_lettuce_source_card_sv3"
                : ingredientId == GameIds.OnionIngredient
                ? "IngredientSourceCards/ingredient_onion_source_card_sv3"
                : "IngredientSourceCards/" + ingredientId.Replace('.', '_') + "_source_card_sv2";
            return Resources.Load<Texture2D>(resourcePath);
        }

        private static RectTransform CreateRect(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Image CreateImage(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = gameObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static RawImage CreateRawImage(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            Texture texture)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = gameObject.GetComponent<RawImage>();
            image.texture = texture;
            image.color = texture == null ? new Color(0.86f, 0.86f, 0.80f) : Color.white;
            image.raycastTarget = false;
            return image;
        }

        private void ConfigureFoodImage(string recipeId)
        {
            if (_foodImage == null)
            {
                return;
            }

            var resourcePath = recipeId == GameIds.HamburgerPlateRecipe
                ? "OrderFoodCards/order_hamburger_plate_sv1"
                : recipeId == GameIds.VegetableSoupRecipe
                    ? "OrderFoodCards/order_vegetable_soup_sv1"
                    : "OrderFoodCards/order_lettuce_salad_sv1";
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                texture = GetFallbackFoodTexture(recipeId);
            }
            _foodImage.texture = texture;
            _foodImage.color = Color.white;
            _foodImage.gameObject.SetActive(texture != null);
        }

        private static Texture2D GetFallbackFoodTexture(string recipeId)
        {
            if (FallbackFoodTextures.TryGetValue(recipeId, out var existing))
            {
                return existing;
            }

            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                name = recipeId == GameIds.HamburgerPlateRecipe
                    ? "Runtime Order Hamburger Plate"
                    : "Runtime Order Food",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[size * size];
            FillEllipse(pixels, size, new Vector2(64f, 60f), new Vector2(56f, 34f),
                new Color32(238, 226, 190, 255));
            FillRectangle(pixels, size, 28, 66, 100, 76, new Color32(61, 154, 69, 255));
            FillRectangle(pixels, size, 31, 52, 97, 67, new Color32(93, 48, 30, 255));
            FillRectangle(pixels, size, 34, 44, 94, 53, new Color32(245, 184, 49, 255));
            FillEllipse(pixels, size, new Vector2(64f, 78f), new Vector2(46f, 25f),
                new Color32(224, 151, 61, 255));
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            FallbackFoodTextures.Add(recipeId, texture);
            return texture;
        }

        private static void FillRectangle(
            Color32[] pixels,
            int textureSize,
            int xMin,
            int yMin,
            int xMax,
            int yMax,
            Color32 color)
        {
            for (var y = yMin; y <= yMax; y++)
            {
                for (var x = xMin; x <= xMax; x++)
                {
                    pixels[y * textureSize + x] = color;
                }
            }
        }

        private static void FillEllipse(
            Color32[] pixels,
            int textureSize,
            Vector2 center,
            Vector2 radii,
            Color32 color)
        {
            for (var y = 0; y < textureSize; y++)
            {
                for (var x = 0; x < textureSize; x++)
                {
                    var normalizedX = (x - center.x) / radii.x;
                    var normalizedY = (y - center.y) / radii.y;
                    if (normalizedX * normalizedX + normalizedY * normalizedY <= 1f)
                    {
                        pixels[y * textureSize + x] = color;
                    }
                }
            }
        }

        private static Color ResolveColor(
            OrderStatus status,
            float normalized,
            out OrderTicketUrgency urgency)
        {
            if (status == OrderStatus.Served)
            {
                urgency = OrderTicketUrgency.Served;
                return ServedColor;
            }

            if (status == OrderStatus.Expired)
            {
                urgency = OrderTicketUrgency.Expired;
                return ExpiredColor;
            }

            if (normalized > SafeThreshold)
            {
                urgency = OrderTicketUrgency.Safe;
                return SafeColor;
            }

            if (normalized > UrgentThreshold)
            {
                urgency = OrderTicketUrgency.Normal;
                return NormalColor;
            }

            urgency = OrderTicketUrgency.Urgent;
            return UrgentColor;
        }

        private bool HoldsOrderedMeal(WorldItem item)
        {
            return item != null && item.IsContainer && item.Container.IsComplete &&
                   item.Container.CompletedRecipeId == _displayedRecipeId;
        }
    }

    public enum KitchenActionIcon
    {
        None,
        PickPlace,
        Work,
        Throw,
        Dash,
        Accelerator,
        Reverse,
        Left,
        Right,
        Dismount,
        Restart
    }

    public sealed class KitchenActionButtonView : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler
    {
        [SerializeField] private string actionId;
        [SerializeField] private KitchenActionIcon icon;
        private RectTransform _rect;

        public string ActionId => actionId;
        public KitchenActionIcon Icon => icon;

        public void Initialize(string id, KitchenActionIcon actionIcon)
        {
            actionId = id;
            icon = actionIcon;
            _rect = transform as RectTransform;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (TryGetComponent<Button>(out var button) && !button.interactable)
            {
                return;
            }

            SetPressed(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            SetPressed(false);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetPressed(false);
        }

        private void OnDisable()
        {
            SetPressed(false);
        }

        private void SetPressed(bool pressed)
        {
            if (_rect == null)
            {
                _rect = transform as RectTransform;
            }

            if (_rect != null)
            {
                _rect.localScale = pressed ? Vector3.one * 0.93f : Vector3.one;
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;

        public Rect AppliedSafeArea => _lastSafeArea;

        private void OnEnable()
        {
            _rect = transform as RectTransform;
            ApplySafeArea();
        }

        private void Update()
        {
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            if (_lastSafeArea != Screen.safeArea || _lastScreenSize != screenSize)
            {
                ApplySafeArea();
            }
        }

        public void ApplySafeArea()
        {
            if (_rect == null)
            {
                _rect = transform as RectTransform;
            }

            if (_rect == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            var safeArea = Screen.safeArea;
            if (safeArea.width <= 0f || safeArea.height <= 0f)
            {
                safeArea = new Rect(0f, 0f, Screen.width, Screen.height);
            }

            _rect.anchorMin = new Vector2(safeArea.xMin / Screen.width, safeArea.yMin / Screen.height);
            _rect.anchorMax = new Vector2(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
            _lastSafeArea = safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        }
    }

    public sealed class KitchenActionAvailability : MonoBehaviour
    {
        private PlayerController _player;
        private Button _throwButton;
        private CanvasGroup _throwGroup;

        public void Initialize(PlayerController player, Button throwButton)
        {
            _player = player;
            _throwButton = throwButton;
            _throwGroup = throwButton == null
                ? null
                : throwButton.GetComponent<CanvasGroup>() ?? throwButton.gameObject.AddComponent<CanvasGroup>();
            Refresh();
        }

        private void LateUpdate()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (_throwButton == null)
            {
                return;
            }

            var held = _player == null ? null : _player.HeldItem;
            var canThrow = held != null && !held.IsPot && !held.IsPan;
            _throwButton.interactable = canThrow;
            if (_throwGroup != null)
            {
                _throwGroup.alpha = canThrow ? 1f : 0.48f;
                _throwGroup.blocksRaycasts = canThrow;
                _throwGroup.interactable = canThrow;
            }
        }
    }

    public static class KitchenHudVisuals
    {
        public static readonly Color DeepPlum = new Color(0.105f, 0.035f, 0.12f, 1f);
        public static readonly Color Cream = new Color(1f, 0.92f, 0.72f, 1f);
        public static readonly Color PickPlace = new Color(0.98f, 0.49f, 0.10f, 1f);
        public static readonly Color Work = new Color(0.04f, 0.65f, 0.62f, 1f);
        public static readonly Color Throw = new Color(0.53f, 0.20f, 0.62f, 1f);
        public static readonly Color Dash = new Color(0.17f, 0.58f, 0.86f, 1f);

        private static Sprite _roundedRectangle;
        private static Sprite _circle;
        private static readonly Dictionary<KitchenActionIcon, Sprite> ActionSprites =
            new Dictionary<KitchenActionIcon, Sprite>();

        public static Sprite RoundedRectangle => _roundedRectangle ??= CreateShapeSprite(false);
        public static Sprite Circle => _circle ??= CreateShapeSprite(true);

        public static void ApplyRounded(Image image)
        {
            image.sprite = RoundedRectangle;
            image.type = Image.Type.Sliced;
        }

        public static void ApplyCircle(Image image)
        {
            image.sprite = Circle;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
        }

        public static GameObject BuildClockIcon(Transform parent, Vector2 position, Color color)
        {
            var root = PictogramRoot(parent, "Clock Icon", new Vector2(62f, 62f));
            root.GetComponent<RectTransform>().anchoredPosition = position;
            CirclePart(root.transform, "Clock Rim", Vector2.zero, 50f, color);
            CirclePart(root.transform, "Clock Face", Vector2.zero, 38f, DeepPlum);
            Part(root.transform, "Clock Hour Hand", new Vector2(-5f, 3f), new Vector2(6f, 17f), color, -42f);
            Part(root.transform, "Clock Minute Hand", new Vector2(5f, -3f), new Vector2(6f, 24f), color, 32f);
            CirclePart(root.transform, "Clock Center", Vector2.zero, 7f, color);
            return root;
        }

        public static GameObject BuildScoreIcon(Transform parent, Vector2 position)
        {
            var root = PictogramRoot(parent, "Score Icon", new Vector2(62f, 62f));
            root.GetComponent<RectTransform>().anchoredPosition = position;
            var gold = new Color(0.98f, 0.66f, 0.10f);
            CirclePart(root.transform, "Score Coin", Vector2.zero, 52f, gold);
            CirclePart(root.transform, "Score Coin Inset", Vector2.zero, 37f,
                new Color(0.78f, 0.39f, 0.08f));
            Part(root.transform, "Score Spark Vertical", Vector2.zero, new Vector2(7f, 25f), Cream);
            Part(root.transform, "Score Spark Horizontal", Vector2.zero, new Vector2(25f, 7f), Cream);
            return root;
        }

        public static GameObject BuildPauseBadge(Transform parent, Vector2 position)
        {
            var root = PictogramRoot(parent, "Clock Pause", new Vector2(38f, 38f));
            root.GetComponent<RectTransform>().anchoredPosition = position;
            CirclePart(root.transform, "Pause Badge", Vector2.zero, 34f, new Color(0.95f, 0.68f, 0.12f));
            Part(root.transform, "Pause Left", new Vector2(-5f, 0f), new Vector2(5f, 18f), DeepPlum);
            Part(root.transform, "Pause Right", new Vector2(5f, 0f), new Vector2(5f, 18f), DeepPlum);
            return root;
        }

        public static GameObject BuildOrderIngredientIcon(Transform parent, string ingredientId)
        {
            var root = PictogramRoot(parent, "Ingredient Pictogram " + ingredientId, new Vector2(78f, 62f));
            if (ingredientId == GameIds.CarrotIngredient)
            {
                EllipsePart(root.transform, "Carrot Body", new Vector2(1f, -2f), new Vector2(23f, 48f),
                    new Color(0.95f, 0.42f, 0.10f), -28f);
                Part(root.transform, "Carrot Leaf Left", new Vector2(-11f, 21f), new Vector2(8f, 23f),
                    new Color(0.22f, 0.62f, 0.25f), -38f);
                Part(root.transform, "Carrot Leaf Right", new Vector2(1f, 24f), new Vector2(8f, 24f),
                    new Color(0.28f, 0.72f, 0.29f), 12f);
                Part(root.transform, "Carrot Detail", new Vector2(5f, 1f), new Vector2(12f, 3f),
                    new Color(1f, 0.72f, 0.26f), -28f);
            }
            else if (ingredientId == GameIds.OnionIngredient)
            {
                EllipsePart(root.transform, "Onion Bulb", new Vector2(0f, -4f), new Vector2(43f, 42f),
                    new Color(0.92f, 0.70f, 0.75f));
                EllipsePart(root.transform, "Onion Center", new Vector2(0f, -5f), new Vector2(25f, 34f),
                    new Color(0.98f, 0.86f, 0.82f));
                Part(root.transform, "Onion Stem", new Vector2(0f, 20f), new Vector2(9f, 15f),
                    new Color(0.54f, 0.67f, 0.37f));
                Part(root.transform, "Onion Root", new Vector2(0f, -27f), new Vector2(20f, 4f),
                    new Color(0.56f, 0.31f, 0.26f));
            }
            else if (ingredientId == GameIds.BeefIngredient)
            {
                EllipsePart(root.transform, "Beef Patty", Vector2.zero, new Vector2(56f, 43f),
                    new Color(0.63f, 0.17f, 0.14f), -8f);
                EllipsePart(root.transform, "Beef Marbling One", new Vector2(-12f, 4f), new Vector2(13f, 7f),
                    new Color(0.96f, 0.66f, 0.57f), 24f);
                EllipsePart(root.transform, "Beef Marbling Two", new Vector2(13f, -7f), new Vector2(17f, 6f),
                    new Color(0.96f, 0.66f, 0.57f), -20f);
            }
            else
            {
                var dark = new Color(0.18f, 0.60f, 0.24f);
                var light = new Color(0.45f, 0.78f, 0.28f);
                CirclePart(root.transform, "Lettuce Leaf Left", new Vector2(-18f, 2f), 31f, dark);
                CirclePart(root.transform, "Lettuce Leaf Right", new Vector2(18f, 2f), 31f, dark);
                CirclePart(root.transform, "Lettuce Leaf Top", new Vector2(0f, 15f), 34f, light);
                CirclePart(root.transform, "Lettuce Leaf Center", new Vector2(0f, -5f), 38f, light);
                Part(root.transform, "Lettuce Stem", new Vector2(0f, -21f), new Vector2(11f, 20f),
                    new Color(0.78f, 0.88f, 0.50f));
            }
            return root;
        }

        public static GameObject BuildOrderProcessIcons(
            Transform parent,
            bool showChop,
            bool showHeat,
            bool useFryingPan,
            Color color)
        {
            var root = PictogramRoot(parent, "Required Processes", new Vector2(80f, 26f));
            if (showChop)
            {
                var chop = PictogramRoot(root.transform, "Process Chop",
                    showHeat ? new Vector2(36f, 24f) : new Vector2(46f, 24f));
                chop.GetComponent<RectTransform>().anchoredPosition = new Vector2(showHeat ? -21f : 0f, 0f);
                Part(chop.transform, "Chopping Board", new Vector2(-4f, -3f), new Vector2(26f, 13f),
                    new Color(0.70f, 0.47f, 0.24f));
                Part(chop.transform, "Knife Blade", new Vector2(3f, 3f), new Vector2(25f, 6f), color, -23f);
                Part(chop.transform, "Knife Handle", new Vector2(15f, 8f), new Vector2(10f, 8f),
                    new Color(0.25f, 0.14f, 0.15f), -23f);
            }

            if (showHeat)
            {
                var heat = PictogramRoot(root.transform, useFryingPan ? "Process Fry" : "Process Boil",
                    new Vector2(42f, 24f));
                heat.GetComponent<RectTransform>().anchoredPosition = new Vector2(showChop ? 21f : 0f, 0f);
                if (useFryingPan)
                {
                    EllipsePart(heat.transform, "Pan Bowl", new Vector2(-5f, -2f), new Vector2(25f, 11f), color);
                    Part(heat.transform, "Pan Handle", new Vector2(11f, 1f), new Vector2(19f, 5f), color, -8f);
                }
                else
                {
                    Part(heat.transform, "Pot", new Vector2(0f, -3f), new Vector2(27f, 13f), color);
                    Part(heat.transform, "Pot Lid", new Vector2(0f, 5f), new Vector2(23f, 3f), color);
                    Part(heat.transform, "Pot Left Handle", new Vector2(-16f, 0f), new Vector2(7f, 4f), color);
                    Part(heat.transform, "Pot Right Handle", new Vector2(16f, 0f), new Vector2(7f, 4f), color);
                }
                Part(heat.transform, "Heat One", new Vector2(-7f, -11f), new Vector2(3f, 7f),
                    new Color(0.91f, 0.28f, 0.14f));
                Part(heat.transform, "Heat Two", new Vector2(1f, -12f), new Vector2(3f, 8f),
                    new Color(0.91f, 0.28f, 0.14f));
                Part(heat.transform, "Heat Three", new Vector2(9f, -11f), new Vector2(3f, 7f),
                    new Color(0.91f, 0.28f, 0.14f));
            }
            return root;
        }

        public static void BuildOrderRouteArrow(Transform parent, Color color)
        {
            Part(parent, "Route Arrow Shaft", new Vector2(-3f, 0f), new Vector2(62f, 7f), color);
            Part(parent, "Route Arrow Upper", new Vector2(27f, 7f), new Vector2(8f, 22f), color, -45f);
            Part(parent, "Route Arrow Lower", new Vector2(27f, -7f), new Vector2(8f, 22f), color, 45f);
        }

        public static GameObject BuildOrderDestinationIcon(
            Transform parent,
            OrderTicketDestination destination,
            Color color)
        {
            var root = PictogramRoot(parent, "Destination " + destination, new Vector2(52f, 66f));
            if (destination == OrderTicketDestination.Bike)
            {
                BuildBike(root.transform, color);
                Part(root.transform, "Delivery Pin Stem", new Vector2(0f, 23f), new Vector2(5f, 12f), color);
                CirclePart(root.transform, "Delivery Pin", new Vector2(0f, 31f), 12f,
                    new Color(0.18f, 0.68f, 0.38f));
            }
            else
            {
                Part(root.transform, "Hatch Counter", new Vector2(0f, -17f), new Vector2(47f, 9f), color);
                EllipsePart(root.transform, "Serving Plate", new Vector2(-4f, -3f), new Vector2(31f, 10f),
                    new Color(0.98f, 0.82f, 0.42f));
                Part(root.transform, "Serve Arrow Shaft", new Vector2(0f, 19f), new Vector2(6f, 23f), color);
                Part(root.transform, "Serve Arrow Left", new Vector2(-6f, 10f), new Vector2(6f, 15f), color, -45f);
                Part(root.transform, "Serve Arrow Right", new Vector2(6f, 10f), new Vector2(6f, 15f), color, 45f);
            }
            return root;
        }

        public static GameObject BuildOrderHolderIcon(
            Transform parent,
            OrderTicketHolder holder,
            Color color)
        {
            var root = PictogramRoot(parent, "Holder " + holder, new Vector2(52f, 66f));
            if (holder == OrderTicketHolder.Player)
            {
                CirclePart(root.transform, "Player Head", new Vector2(0f, 14f), 19f,
                    new Color(0.95f, 0.69f, 0.45f));
                EllipsePart(root.transform, "Player Body", new Vector2(0f, -10f), new Vector2(35f, 35f), color);
                EllipsePart(root.transform, "Held Plate", new Vector2(14f, -22f), new Vector2(28f, 8f),
                    new Color(0.98f, 0.82f, 0.42f));
            }
            else if (holder == OrderTicketHolder.Bike)
            {
                BuildBike(root.transform, color);
            }
            else
            {
                Part(root.transform, "Kitchen Counter Top", new Vector2(0f, 2f), new Vector2(45f, 10f),
                    new Color(0.62f, 0.39f, 0.24f));
                Part(root.transform, "Kitchen Counter", new Vector2(0f, -14f), new Vector2(38f, 27f), color);
                EllipsePart(root.transform, "Waiting Plate", new Vector2(0f, 13f), new Vector2(28f, 8f),
                    new Color(0.98f, 0.82f, 0.42f));
            }
            return root;
        }

        public static GameObject BuildOrderStatusIcon(
            Transform parent,
            OrderTicketUrgency status,
            Color color)
        {
            var root = PictogramRoot(parent, "Status " + status, new Vector2(42f, 42f));
            if (status == OrderTicketUrgency.Served)
            {
                Part(root.transform, "Check Short", new Vector2(-7f, -2f), new Vector2(8f, 20f), color, -42f);
                Part(root.transform, "Check Long", new Vector2(7f, 1f), new Vector2(8f, 31f), color, 43f);
            }
            else if (status == OrderTicketUrgency.Expired)
            {
                Part(root.transform, "Expired Slash One", Vector2.zero, new Vector2(8f, 31f), color, 45f);
                Part(root.transform, "Expired Slash Two", Vector2.zero, new Vector2(8f, 31f), color, -45f);
            }
            else
            {
                Part(root.transform, "Pause Left", new Vector2(-7f, 0f), new Vector2(7f, 25f), color);
                Part(root.transform, "Pause Right", new Vector2(7f, 0f), new Vector2(7f, 25f), color);
            }
            return root;
        }

        private static GameObject PictogramRoot(Transform parent, string name, Vector2 size)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            return root;
        }

        private static void BuildBike(Transform parent, Color color)
        {
            CirclePart(parent, "Bike Wheel Left", new Vector2(-15f, -16f), 15f, color);
            CirclePart(parent, "Bike Wheel Right", new Vector2(17f, -16f), 15f, color);
            Part(parent, "Bike Chassis", new Vector2(0f, -8f), new Vector2(34f, 7f), color, -7f);
            Part(parent, "Bike Fork", new Vector2(13f, -3f), new Vector2(5f, 26f), color, -23f);
            Part(parent, "Bike Seat", new Vector2(-6f, 3f), new Vector2(17f, 6f), color);
            Part(parent, "Bike Cargo", new Vector2(-15f, 11f), new Vector2(19f, 17f),
                new Color(0.94f, 0.48f, 0.12f));
        }

        public static void BuildIcon(
            Transform parent,
            KitchenActionIcon icon,
            Color color,
            float scale)
        {
            if (icon == KitchenActionIcon.None)
            {
                return;
            }

            var root = new GameObject("Icon", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            var kitchenAction = icon == KitchenActionIcon.PickPlace ||
                                icon == KitchenActionIcon.Work ||
                                icon == KitchenActionIcon.Throw ||
                                icon == KitchenActionIcon.Dash;
            rootRect.anchoredPosition = kitchenAction ? Vector2.zero : new Vector2(0f, 15f * scale);
            rootRect.sizeDelta = (kitchenAction ? new Vector2(132f, 122f) : new Vector2(110f, 90f)) * scale;

            switch (icon)
            {
                case KitchenActionIcon.PickPlace:
                    BuildAssetIcon(root.transform, icon);
                    break;
                case KitchenActionIcon.Work:
                    BuildAssetIcon(root.transform, icon);
                    break;
                case KitchenActionIcon.Throw:
                    BuildAssetIcon(root.transform, icon);
                    break;
                case KitchenActionIcon.Dash:
                    BuildAssetIcon(root.transform, icon);
                    break;
                case KitchenActionIcon.Accelerator:
                    Arrow(root.transform, Vector2.up, color);
                    break;
                case KitchenActionIcon.Reverse:
                    Arrow(root.transform, Vector2.down, color);
                    break;
                case KitchenActionIcon.Left:
                    Arrow(root.transform, Vector2.left, color);
                    break;
                case KitchenActionIcon.Right:
                    Arrow(root.transform, Vector2.right, color);
                    break;
                case KitchenActionIcon.Dismount:
                    Arrow(root.transform, Vector2.down, color);
                    Part(root.transform, "Ground", new Vector2(0f, -38f), new Vector2(72f, 9f), color);
                    break;
                case KitchenActionIcon.Restart:
                    Chevron(root.transform, -15f, color);
                    Chevron(root.transform, 18f, color);
                    break;
            }
        }

        private static void BuildAssetIcon(Transform parent, KitchenActionIcon icon)
        {
            var resourcePath = icon switch
            {
                KitchenActionIcon.PickPlace => "ActionIcons/action_pick_silhouette_sv1",
                KitchenActionIcon.Work => "ActionIcons/action_work_silhouette_sv1",
                KitchenActionIcon.Throw => "ActionIcons/action_throw_silhouette_sv1",
                KitchenActionIcon.Dash => "ActionIcons/action_dash_silhouette_sv1",
                _ => null
            };
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return;
            }

            if (!ActionSprites.TryGetValue(icon, out var sprite) || sprite == null)
            {
                var texture = Resources.Load<Texture2D>(resourcePath);
                if (texture == null)
                {
                    Debug.LogError("Missing action icon texture: " + resourcePath);
                    return;
                }

                sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect);
                sprite.name = icon + " Silhouette Sprite";
                ActionSprites[icon] = sprite;
            }

            var imageObject = new GameObject(
                "Generated Silhouette " + icon,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            imageObject.transform.SetParent(parent, false);
            var rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(4f, 4f);
            rect.offsetMax = new Vector2(-4f, -4f);
            var image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private static void Chevron(Transform parent, float x, Color color)
        {
            Part(parent, "Chevron Upper", new Vector2(x, 16f), new Vector2(11f, 45f), color, 45f);
            Part(parent, "Chevron Lower", new Vector2(x, -16f), new Vector2(11f, 45f), color, -45f);
        }

        private static void Arrow(Transform parent, Vector2 direction, Color color)
        {
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            Part(parent, "Arrow Shaft", Vector2.zero, new Vector2(13f, 60f), color, angle);
            var tip = direction * 32f;
            var headObject = new GameObject("Arrow Head", typeof(RectTransform));
            headObject.transform.SetParent(parent, false);
            var headRect = headObject.GetComponent<RectTransform>();
            headRect.anchorMin = new Vector2(0.5f, 0.5f);
            headRect.anchorMax = new Vector2(0.5f, 0.5f);
            headRect.pivot = new Vector2(0.5f, 0.5f);
            headRect.anchoredPosition = tip;
            headRect.sizeDelta = new Vector2(44f, 44f);
            Part(headObject.transform, "Arrow Wing Left", Rotate(new Vector2(-10f, -7f), angle),
                new Vector2(12f, 32f), color, angle - 45f);
            Part(headObject.transform, "Arrow Wing Right", Rotate(new Vector2(10f, -7f), angle),
                new Vector2(12f, 32f), color, angle + 45f);
        }

        private static Vector2 Rotate(Vector2 point, float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad;
            var cosine = Mathf.Cos(radians);
            var sine = Mathf.Sin(radians);
            return new Vector2(
                point.x * cosine - point.y * sine,
                point.x * sine + point.y * cosine);
        }

        private static void Part(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            Color color,
            float rotation = 0f)
        {
            var part = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            part.transform.SetParent(parent, false);
            var rect = part.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            var image = part.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            ApplyRounded(image);
        }

        private static void CirclePart(
            Transform parent,
            string name,
            Vector2 position,
            float diameter,
            Color color)
        {
            Part(parent, name, position, Vector2.one * diameter, color);
            ApplyCircle(parent.Find(name).GetComponent<Image>());
        }

        private static void EllipsePart(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            Color color,
            float rotation = 0f)
        {
            Part(parent, name, position, size, color, rotation);
            var image = parent.Find(name).GetComponent<Image>();
            image.sprite = Circle;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
        }

        private static Sprite CreateShapeSprite(bool circle)
        {
            const int size = 64;
            const float edgeSoftness = 1.15f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                name = circle ? "Runtime UI Circle" : "Runtime UI Rounded Rectangle",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[size * size];
            var center = (size - 1f) * 0.5f;
            const float radius = 17f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    float distance;
                    if (circle)
                    {
                        distance = Vector2.Distance(new Vector2(x, y), Vector2.one * center) - 30.25f;
                    }
                    else
                    {
                        var q = new Vector2(Mathf.Abs(x - center), Mathf.Abs(y - center)) -
                                Vector2.one * (center - radius);
                        var outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
                        distance = outside.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
                    }

                    var alpha = (byte)Mathf.RoundToInt(
                        Mathf.Clamp01(0.5f - distance / edgeSoftness) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var border = circle ? Vector4.zero : new Vector4(19f, 19f, 19f, 19f);
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                border);
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
