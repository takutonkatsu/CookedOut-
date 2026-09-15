using CookedOut.Domain;
using UnityEngine;

namespace CookedOut.Presentation
{
    [DefaultExecutionOrder(260)]
    public sealed class CameraAlignedIngredientIcon : MonoBehaviour
    {
        private Transform _indicatorRoot;
        private Transform _itemRoot;
        private Vector3 _layoutOffset;
        private float _height;

        public void Initialize(Vector3 layoutOffset, float height)
        {
            _indicatorRoot = transform.parent;
            _itemRoot = _indicatorRoot == null ? null : _indicatorRoot.parent;
            _layoutOffset = layoutOffset;
            _height = height;
            RefreshNow();
        }

        public void RefreshNow()
        {
            var activeCamera = Camera.main;
            var screenUp = activeCamera == null
                ? Vector3.forward
                : Vector3.ProjectOnPlane(activeCamera.transform.up, Vector3.up);
            if (screenUp.sqrMagnitude <= 0.0001f)
            {
                screenUp = Vector3.forward;
            }
            screenUp.Normalize();
            var screenRight = Vector3.Cross(Vector3.up, screenUp).normalized;

            if (_indicatorRoot != null && _itemRoot != null)
            {
                var horizontalScale = Mathf.Max(
                    0.0001f,
                    (_itemRoot.lossyScale.x + _itemRoot.lossyScale.z) * 0.5f);
                var basePosition = _itemRoot.TransformPoint(Vector3.up * _height);
                _indicatorRoot.position = basePosition +
                                          screenRight * (_layoutOffset.x * horizontalScale) +
                                          screenUp * (_layoutOffset.z * horizontalScale);
            }

            // Keep the card horizontal while locking the photograph's top edge to
            // the fixed camera. Setting a world rotation here counteracts any turn
            // inherited from a held item or its player-facing parent.
            transform.rotation = Quaternion.LookRotation(Vector3.up, -screenUp);
        }

        private void LateUpdate()
        {
            RefreshNow();
        }
    }

    public sealed class WorldItem
    {
        private WorldItem(
            IngredientItem ingredient,
            DeliveryContainer container,
            CookingPot pot,
            FryingPan pan,
            string toolId)
        {
            Ingredient = ingredient;
            Container = container;
            Pot = pot;
            Pan = pan;
            ToolId = toolId;
        }

        public IngredientItem Ingredient { get; }
        public DeliveryContainer Container { get; }
        public CookingPot Pot { get; }
        public FryingPan Pan { get; }
        public string ToolId { get; }
        public bool IsIngredient => Ingredient != null;
        public bool IsContainer => Container != null;
        public bool IsPot => Pot != null;
        public bool IsPan => Pan != null;
        public bool IsTool => !string.IsNullOrEmpty(ToolId);

        public static WorldItem FromIngredient(IngredientItem ingredient)
        {
            return new WorldItem(ingredient, null, null, null, null);
        }

        public static WorldItem FromContainer(DeliveryContainer container)
        {
            return new WorldItem(null, container, null, null, null);
        }

        public static WorldItem FromPot(CookingPot pot)
        {
            return new WorldItem(null, null, pot, null, null);
        }

        public static WorldItem FromPan(FryingPan pan)
        {
            return new WorldItem(null, null, null, pan, null);
        }

        public static WorldItem FromTool(string toolId)
        {
            return new WorldItem(null, null, null, null, toolId);
        }
    }

    public static class WorldItemVisualFactory
    {
        public const float IngredientIndicatorCardSize = 0.70f;
        public const float IngredientIndicatorBadgeSize = IngredientIndicatorCardSize;
        public const float IngredientIndicatorGap = 0.06f;
        public const float IngredientIndicatorVerticalClearance = 0.78f;
        public const float IngredientIndicatorContentScale = 1.12f;
        public const float IngredientIndicatorBackgroundRadius = 0.43f;
        public const float IngredientIndicatorSubjectOverflow = 1f;

        public static GameObject Create(WorldItem item, Transform parent)
        {
            var root = new GameObject("ItemVisual");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            if (item.IsTool)
            {
                CreateToolVisual(item.ToolId, root.transform);
            }
            else if (item.IsPot)
            {
                CreatePotVisual(item.Pot, root.transform);
            }
            else if (item.IsPan)
            {
                CreatePanVisual(item.Pan, root.transform);
            }
            else if (item.IsIngredient && item.Ingredient.Preparation == IngredientPreparation.Raw)
            {
                CreateRawIngredientVisual(item.Ingredient, root.transform);
            }
            else if (item.IsIngredient)
            {
                CreatePreparedIngredientVisual(item.Ingredient, root.transform);
                CreateIngredientListIndicators(
                    new[] { item.Ingredient },
                    root.transform);
            }
            else if (item.Container.IsReusablePlate)
            {
                CreateReusablePlateVisual(item.Container, root.transform);
                CreateIngredientListIndicators(item.Container.Components, root.transform);
            }
            else
            {
                DeliveryContainerVisual.Build(item.Container, root.transform);
                CreateIngredientListIndicators(
                    item.Container.Components,
                    root.transform);
            }

            return root;
        }

        private static void CreateToolVisual(string toolId, Transform parent)
        {
            if (KitchenProductionAssetFactory.TryInstantiateTool(toolId, parent, out _))
            {
                return;
            }

            var bottle = CreatePart(PrimitiveType.Cylinder, parent, new Vector3(0f, 0.34f, 0f),
                new Vector3(0.22f, 0.34f, 0.22f), new Color(0.92f, 0.08f, 0.055f));
            bottle.name = "Fire Extinguisher Bottle";
            var handle = CreatePart(PrimitiveType.Cube, parent, new Vector3(0.08f, 0.72f, 0f),
                new Vector3(0.32f, 0.08f, 0.11f), KitchenArtPalette.DeepInset);
            handle.name = "Fire Extinguisher Handle";
        }

        private static void CreateReusablePlateVisual(DeliveryContainer container, Transform parent)
        {
            var basePlate = CreatePart(PrimitiveType.Cylinder, parent, new Vector3(0f, 0.08f, 0f),
                new Vector3(0.46f, 0.06f, 0.46f), KitchenArtPalette.BoardIvory);
            basePlate.name = container.IsDirty ? "Dirty Reusable Plate" : "Clean Reusable Plate";
            var rim = CreatePart(PrimitiveType.Cylinder, parent, new Vector3(0f, 0.145f, 0f),
                new Vector3(0.43f, 0.018f, 0.43f), KitchenArtPalette.Teal);
            rim.name = "Reusable Plate Rim";
            CreatePart(PrimitiveType.Cylinder, parent, new Vector3(0f, 0.165f, 0f),
                new Vector3(0.36f, 0.015f, 0.36f), KitchenArtPalette.BoardIvory);

            if (container.IsDirty)
            {
                var smear = CreatePart(PrimitiveType.Sphere, parent, new Vector3(-0.08f, 0.20f, 0.03f),
                    new Vector3(0.22f, 0.018f, 0.09f), new Color(0.48f, 0.20f, 0.06f));
                smear.name = "Dirty Sauce Smear";
                return;
            }

            DeliveryContainerVisual.BuildFoodContents(container, parent);
        }

        private static void CreateRawIngredientVisual(IngredientItem ingredient, Transform parent)
        {
            if (KitchenProductionAssetFactory.TryInstantiateIngredient(
                    ingredient.IngredientId,
                    IngredientPreparation.Raw,
                    parent,
                    out _))
            {
                return;
            }

            var color = IngredientColor(ingredient.IngredientId);
            if (ingredient.IngredientId == GameIds.BeefIngredient)
            {
                var beef = CreatePart(PrimitiveType.Sphere, parent, new Vector3(0f, 0.16f, 0f),
                    new Vector3(0.48f, 0.18f, 0.42f), KitchenArtPalette.BeefRaw);
                beef.name = "Raw Beef";
                return;
            }

            if (ingredient.IngredientId == GameIds.CarrotIngredient)
            {
                CreateRawCarrotVisual(parent);
                return;
            }

            if (ingredient.IngredientId == GameIds.OnionIngredient)
            {
                CreateRawOnionVisual(parent);
                return;
            }

            var leafPositions = new[]
            {
                new Vector3(0f, 0.2f, 0f),
                new Vector3(-0.18f, 0.17f, 0.03f),
                new Vector3(0.18f, 0.17f, 0.03f),
                new Vector3(-0.12f, 0.22f, -0.15f),
                new Vector3(0.12f, 0.22f, -0.15f),
                new Vector3(-0.1f, 0.13f, 0.17f),
                new Vector3(0.1f, 0.13f, 0.17f)
            };

            for (var index = 0; index < leafPositions.Length; index++)
            {
                var leaf = CreatePart(PrimitiveType.Sphere, parent, leafPositions[index],
                    new Vector3(0.28f, 0.3f, 0.18f), index % 2 == 0 ? color : color * 0.88f,
                    new Vector3(index % 3 * 9f - 9f, index * 51f, index % 2 == 0 ? -12f : 12f));
                leaf.name = "Raw Lettuce Leaf " + (index + 1);
            }

            var core = CreatePart(PrimitiveType.Cylinder, parent, new Vector3(0f, 0.05f, 0.12f),
                new Vector3(0.12f, 0.08f, 0.12f), new Color(0.78f, 0.9f, 0.38f));
            core.name = "Lettuce Core";
        }

        private static void CreatePreparedIngredientVisual(IngredientItem ingredient, Transform parent)
        {
            if (KitchenProductionAssetFactory.TryInstantiateIngredient(
                    ingredient.IngredientId,
                    ingredient.Preparation,
                    parent,
                    out _))
            {
                return;
            }

            var color = IngredientColor(ingredient.IngredientId);
            if (ingredient.IngredientId == GameIds.BeefIngredient)
            {
                var beef = CreatePart(PrimitiveType.Sphere, parent, new Vector3(0f, 0.12f, 0f),
                    new Vector3(0.48f, 0.14f, 0.42f),
                    ingredient.Preparation == IngredientPreparation.Cooked
                        ? KitchenArtPalette.BeefCooked
                        : KitchenArtPalette.BeefRaw * 0.88f);
                beef.name = ingredient.Preparation == IngredientPreparation.Cooked
                    ? "Cooked Hamburger Patty"
                    : "Chopped Beef Patty";
                return;
            }

            if (ingredient.IngredientId == GameIds.LettuceIngredient)
            {
                var positions = new[]
                {
                    new Vector3(-0.2f, 0.08f, -0.08f),
                    new Vector3(0f, 0.11f, -0.13f),
                    new Vector3(0.2f, 0.08f, -0.04f),
                    new Vector3(-0.11f, 0.1f, 0.13f),
                    new Vector3(0.14f, 0.12f, 0.14f)
                };
                for (var index = 0; index < positions.Length; index++)
                {
                    var leaf = CreatePart(PrimitiveType.Sphere, parent, positions[index],
                        new Vector3(0.27f, 0.09f, 0.18f), index % 2 == 0 ? color : color * 0.88f,
                        new Vector3(0f, index * 47f, index % 2 == 0 ? -8f : 8f));
                    leaf.name = "Chopped Lettuce Leaf " + (index + 1);
                }

                return;
            }

            if (ingredient.IngredientId == GameIds.CarrotIngredient)
            {
                var positions = new[]
                {
                    new Vector3(-0.22f, 0.10f, -0.10f),
                    new Vector3(0.02f, 0.12f, -0.13f),
                    new Vector3(0.23f, 0.09f, 0.02f),
                    new Vector3(-0.05f, 0.11f, 0.15f)
                };
                for (var index = 0; index < positions.Length; index++)
                {
                    var chunk = CreatePart(PrimitiveType.Sphere, parent, positions[index],
                        new Vector3(0.20f, 0.12f, 0.16f), color,
                        new Vector3(0f, index * 31f, index % 2 == 0 ? -8f : 8f));
                    chunk.name = "Chopped Carrot Chunk " + (index + 1);
                }
                return;
            }

            var onionPositions = new[]
            {
                new Vector3(-0.22f, 0.10f, -0.09f),
                new Vector3(0.03f, 0.12f, -0.13f),
                new Vector3(0.22f, 0.10f, 0.05f),
                new Vector3(-0.04f, 0.11f, 0.16f)
            };
            for (var index = 0; index < onionPositions.Length; index++)
            {
                var wedge = CreatePart(PrimitiveType.Sphere, parent, onionPositions[index],
                    new Vector3(0.22f, 0.09f, 0.18f), KitchenArtPalette.OnionFlesh,
                    new Vector3(0f, index * 43f, index % 2 == 0 ? -10f : 10f));
                wedge.name = "Chopped Onion Wedge " + (index + 1);
            }
        }

        private static void CreateRawCarrotVisual(Transform parent)
        {
            var body = CreatePart(PrimitiveType.Sphere, parent, new Vector3(0f, 0.19f, 0f),
                new Vector3(0.31f, 0.23f, 0.55f), KitchenArtPalette.Carrot,
                new Vector3(0f, 28f, 0f));
            body.name = "Whole Carrot Body";
            var leafPositions = new[]
            {
                new Vector3(-0.18f, 0.29f, -0.38f),
                new Vector3(-0.03f, 0.34f, -0.43f),
                new Vector3(0.13f, 0.30f, -0.40f)
            };
            for (var index = 0; index < leafPositions.Length; index++)
            {
                var leaf = CreatePart(PrimitiveType.Sphere, parent, leafPositions[index],
                    new Vector3(0.11f, 0.09f, 0.26f), KitchenArtPalette.LeafGreen,
                    new Vector3(index % 2 == 0 ? -16f : 16f, index * 28f, 0f));
                leaf.name = "Carrot Leaf " + (index + 1);
            }
        }

        private static void CreateRawOnionVisual(Transform parent)
        {
            var bulb = CreatePart(PrimitiveType.Sphere, parent, new Vector3(0f, 0.20f, 0f),
                new Vector3(0.44f, 0.36f, 0.44f), KitchenArtPalette.OnionSkin);
            bulb.name = "Whole Onion Bulb";
            var stem = CreatePart(PrimitiveType.Cylinder, parent, new Vector3(0f, 0.49f, 0f),
                new Vector3(0.10f, 0.12f, 0.10f), KitchenArtPalette.OnionSkin * 0.82f);
            stem.name = "Onion Stem";
            var root = CreatePart(PrimitiveType.Cylinder, parent, new Vector3(0f, 0.025f, 0f),
                new Vector3(0.13f, 0.025f, 0.13f), KitchenArtPalette.OnionSkin * 0.68f);
            root.name = "Onion Root";
        }

        private static void CreatePotVisual(CookingPot pot, Transform parent)
        {
            var metal = new Color(0.28f, 0.34f, 0.4f);
            var body = CreatePart(PrimitiveType.Cylinder, parent, new Vector3(0f, 0.18f, 0f),
                new Vector3(0.72f, 0.18f, 0.72f), metal);
            body.name = "Pot Outer Body";
            var rimColor = metal * 0.82f;
            var rim = CreatePart(PrimitiveType.Cube, parent, new Vector3(0f, 0.41f, 0.34f),
                new Vector3(0.76f, 0.08f, 0.09f), rimColor);
            rim.name = "Pot Outer Rim";
            CreatePart(PrimitiveType.Cube, parent, new Vector3(0f, 0.41f, -0.34f),
                new Vector3(0.76f, 0.08f, 0.09f), rimColor).name = "Pot Outer Rim Back";
            CreatePart(PrimitiveType.Cube, parent, new Vector3(-0.34f, 0.41f, 0f),
                new Vector3(0.09f, 0.08f, 0.60f), rimColor).name = "Pot Outer Rim Left";
            CreatePart(PrimitiveType.Cube, parent, new Vector3(0.34f, 0.41f, 0f),
                new Vector3(0.09f, 0.08f, 0.60f), rimColor).name = "Pot Outer Rim Right";
            var cavity = CreatePart(PrimitiveType.Cylinder, parent, new Vector3(0f, 0.365f, 0f),
                new Vector3(0.56f, 0.004f, 0.56f), KitchenArtPalette.DeepInset);
            cavity.name = "Pot Inner Cavity";
            CreatePart(PrimitiveType.Cube, parent, new Vector3(-0.48f, 0.24f, 0f),
                new Vector3(0.3f, 0.09f, 0.14f), metal);
            CreatePart(PrimitiveType.Cube, parent, new Vector3(0.48f, 0.24f, 0f),
                new Vector3(0.3f, 0.09f, 0.14f), metal);

            if (pot.IsEmpty)
            {
                return;
            }

            var soupColor = pot.IsCooked
                ? new Color(1f, 0.68f, 0.18f)
                : Color.Lerp(new Color(0.45f, 0.28f, 0.12f), new Color(0.9f, 0.48f, 0.12f), pot.HeatProgress);
            CreatePart(PrimitiveType.Cylinder, parent, new Vector3(0f, 0.38f, 0f),
                new Vector3(0.58f, 0.035f, 0.58f), soupColor);
            var contents = pot.Contents;
            for (var index = 0; index < contents.Count; index++)
            {
                var ingredient = contents[index];
                var x = index == 0 ? -0.19f : 0.18f;
                var z = index == 0 ? -0.06f : 0.09f;
                CreatePart(PrimitiveType.Sphere, parent, new Vector3(x, 0.43f, z),
                    new Vector3(0.16f, 0.07f, 0.14f), IngredientColor(ingredient.IngredientId),
                    new Vector3(0f, index * 37f, 0f));
            }

            if (pot.IsCooked)
            {
                CreatePart(PrimitiveType.Sphere, parent, new Vector3(-0.16f, 0.72f, 0f),
                    new Vector3(0.13f, 0.28f, 0.13f), new Color(0.9f, 0.95f, 1f, 0.72f));
                CreatePart(PrimitiveType.Sphere, parent, new Vector3(0.14f, 0.86f, 0.05f),
                    new Vector3(0.11f, 0.24f, 0.11f), new Color(0.9f, 0.95f, 1f, 0.58f));
            }

            CreateIngredientListIndicators(contents, parent);
        }

        private static void CreatePanVisual(FryingPan pan, Transform parent)
        {
            var metal = new Color(0.16f, 0.19f, 0.23f);
            CreatePart(PrimitiveType.Cylinder, parent, new Vector3(0f, 0.14f, 0f),
                new Vector3(0.68f, 0.10f, 0.68f), metal);
            CreatePart(PrimitiveType.Cylinder, parent, new Vector3(0f, 0.24f, 0f),
                new Vector3(0.72f, 0.035f, 0.72f), metal * 0.78f);
            var handle = CreatePart(PrimitiveType.Cube, parent, new Vector3(0.62f, 0.16f, 0f),
                new Vector3(0.58f, 0.10f, 0.16f), metal);
            handle.name = "Frying Pan Handle";

            if (pan.IsEmpty)
            {
                return;
            }

            var pattyColor = pan.IsBurned
                ? KitchenArtPalette.Burned
                : Color.Lerp(KitchenArtPalette.BeefRaw, KitchenArtPalette.BeefCooked, pan.CookProgress);
            var patty = CreatePart(PrimitiveType.Sphere, parent, new Vector3(0f, 0.31f, 0f),
                new Vector3(0.46f, 0.10f, 0.42f), pattyColor);
            patty.name = pan.IsBurned ? "Burned Patty" : pan.IsCooked ? "Cooked Patty" : "Cooking Patty";

            if (!pan.IsBurned)
            {
                parent.gameObject.AddComponent<FryingPanCookingMotion>()
                    .Initialize(pan, patty.transform, patty.GetComponent<Renderer>());
            }

            if (pan.IsBurned)
            {
                var outerFlame = CreatePart(PrimitiveType.Sphere, parent, new Vector3(-0.08f, 0.55f, 0f),
                    new Vector3(0.22f, 0.42f, 0.18f), new Color(1f, 0.28f, 0.02f));
                outerFlame.name = "Pan Fire Outer";
                var innerFlame = CreatePart(PrimitiveType.Sphere, parent, new Vector3(0.09f, 0.50f, 0.04f),
                    new Vector3(0.14f, 0.30f, 0.12f), new Color(1f, 0.82f, 0.08f));
                innerFlame.name = "Pan Fire Core";
            }

            CreateIngredientListIndicators(new[] { pan.Contents }, parent);
        }

        private static void CreateIngredientListIndicators(
            System.Collections.Generic.IReadOnlyList<IngredientItem> ingredients,
            Transform parent)
        {
            var count = ingredients.Count;
            if (count == 0)
            {
                return;
            }

            var height = IngredientIndicatorHeight(parent);
            for (var index = 0; index < count; index++)
            {
                CreateIngredientListIndicator(
                    ingredients[index],
                    parent,
                    IngredientIndicatorOffset(index, count),
                    height);
            }
        }

        private static Vector3 IngredientIndicatorOffset(int index, int count)
        {
            if (count <= 1)
            {
                return Vector3.zero;
            }

            var columns = Mathf.CeilToInt(Mathf.Sqrt(count));
            var rows = Mathf.CeilToInt(count / (float)columns);
            var column = index % columns;
            var row = index / columns;
            var spacing = IngredientIndicatorCardSize + IngredientIndicatorGap;
            return new Vector3(
                (column - (columns - 1) * 0.5f) * spacing,
                0f,
                (row - (rows - 1) * 0.5f) * spacing);
        }

        private static float IngredientIndicatorHeight(Transform parent)
        {
            var highestLocalPoint = 0f;
            foreach (var renderer in parent.GetComponentsInChildren<Renderer>(true))
            {
                var bounds = renderer.bounds;
                var topCenter = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
                highestLocalPoint = Mathf.Max(
                    highestLocalPoint,
                    parent.InverseTransformPoint(topCenter).y);
            }

            return highestLocalPoint + IngredientIndicatorVerticalClearance;
        }

        private static void CreateIngredientListIndicator(
            IngredientItem ingredient,
            Transform parent,
            Vector3 offset,
            float height)
        {
            var indicator = new GameObject("Ingredient List Indicator " + ingredient.IngredientId);
            indicator.transform.SetParent(parent, false);
            indicator.transform.localPosition = offset + new Vector3(0f, height, -0.05f);

            var sourceCard = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sourceCard.name = "Ingredient Source Card " + ingredient.IngredientId;
            sourceCard.transform.SetParent(indicator.transform, false);
            sourceCard.transform.localPosition = new Vector3(0f, 0.035f, 0f);
            sourceCard.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            sourceCard.transform.localScale = Vector3.one * IngredientIndicatorCardSize;
            sourceCard.AddComponent<CameraAlignedIngredientIcon>().Initialize(offset, height);
            var collider = sourceCard.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            var texture = Resources.Load<Texture2D>(IngredientSourceCardResourcePath(ingredient.IngredientId));
            var shader = Shader.Find("CookedOut/CircularIngredientCard") ??
                         Shader.Find("Sprites/Default") ??
                         Shader.Find("Unlit/Transparent") ??
                         Shader.Find("Standard");
            var material = new Material(shader)
            {
                name = "Ingredient Source Card Material " + ingredient.IngredientId,
                mainTexture = texture,
                color = Color.white
            };
            if (material.HasProperty("_ContentScale"))
            {
                material.SetFloat("_ContentScale", IngredientIndicatorContentScale);
            }
            if (material.HasProperty("_BackgroundRadius"))
            {
                material.SetFloat("_BackgroundRadius", IngredientIndicatorBackgroundRadius);
            }
            if (material.HasProperty("_SubjectOverflow"))
            {
                material.SetFloat("_SubjectOverflow", IngredientIndicatorSubjectOverflow);
            }
            sourceCard.GetComponent<Renderer>().material = material;
        }

        private static string IngredientSourceCardResourcePath(string ingredientId)
        {
            if (ingredientId == GameIds.OnionIngredient)
            {
                return "IngredientSourceCards/ingredient_onion_source_card_sv3";
            }
            if (ingredientId == GameIds.LettuceIngredient)
            {
                return "IngredientSourceCards/ingredient_lettuce_source_card_sv3";
            }

            return "IngredientSourceCards/" + ingredientId.Replace('.', '_') + "_source_card_sv2";
        }

        private static Color IngredientColor(string ingredientId)
        {
            if (ingredientId == GameIds.BeefIngredient)
            {
                return KitchenArtPalette.BeefCooked;
            }

            if (ingredientId == GameIds.CarrotIngredient)
            {
                return KitchenArtPalette.Carrot;
            }

            if (ingredientId == GameIds.OnionIngredient)
            {
                return KitchenArtPalette.OnionFlesh;
            }

            return KitchenArtPalette.Lettuce;
        }

        private static GameObject CreatePart(
            PrimitiveType primitive,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            Vector3? localEuler = null)
        {
            var part = GameObject.CreatePrimitive(primitive);
            part.name = primitive.ToString();
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.transform.localEulerAngles = localEuler ?? Vector3.zero;
            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            part.GetComponent<Renderer>().material = GrayboxMaterials.Create(color);
            return part;
        }
    }

    public sealed class DeliveryContainerVisual : MonoBehaviour
    {
        public const float OpenLidAngle = -108f;

        private static readonly Color BodyColor = new Color(0.91f, 0.84f, 0.68f);
        private static readonly Color BodyShadowColor = new Color(0.31f, 0.27f, 0.33f);
        private static readonly Color GuardColor = new Color(0.02f, 0.59f, 0.64f);
        private static readonly Color HingeColor = new Color(0.92f, 0.57f, 0.12f);

        public Transform LidPivot { get; private set; }
        public Transform ContentRoot { get; private set; }
        public bool IsLidClosed { get; private set; }

        public static DeliveryContainerVisual Build(DeliveryContainer container, Transform parent)
        {
            var visualObject = new GameObject("Delivery Container Visual");
            visualObject.transform.SetParent(parent, false);
            var visual = visualObject.AddComponent<DeliveryContainerVisual>();
            visual.BuildGeometry(container);
            return visual;
        }

        private void BuildGeometry(DeliveryContainer container)
        {
            var body = new GameObject("Body").transform;
            body.SetParent(transform, false);

            CreatePart("Lower Body", PrimitiveType.Cube, body, new Vector3(0f, 0.12f, 0f),
                new Vector3(0.78f, 0.24f, 0.64f), BodyShadowColor);
            CreatePart("Inner Basin", PrimitiveType.Cube, body, new Vector3(0f, 0.24f, 0f),
                new Vector3(0.68f, 0.2f, 0.54f), BodyColor);
            CreateRim(body);
            CreateCornerGuards(body);
            CreatePart("Front Latch", PrimitiveType.Cube, body, new Vector3(0f, 0.28f, 0.345f),
                new Vector3(0.25f, 0.13f, 0.06f), BodyColor);

            CreatePart("Left Hinge", PrimitiveType.Cube, body, new Vector3(-0.2f, 0.34f, -0.34f),
                new Vector3(0.16f, 0.08f, 0.08f), HingeColor);
            CreatePart("Right Hinge", PrimitiveType.Cube, body, new Vector3(0.2f, 0.34f, -0.34f),
                new Vector3(0.16f, 0.08f, 0.08f), HingeColor);

            ContentRoot = new GameObject("Food Contents").transform;
            ContentRoot.SetParent(transform, false);
            ContentRoot.localPosition = new Vector3(0f, container.IsComplete ? 0.26f : 0.37f, 0f);
            CreateContents(container, ContentRoot);

            LidPivot = new GameObject("Lid Pivot").transform;
            LidPivot.SetParent(transform, false);
            LidPivot.localPosition = new Vector3(0f, 0.38f, -0.34f);
            LidPivot.localRotation = Quaternion.identity;

            CreatePart("Clear Lid", PrimitiveType.Cube, LidPivot, new Vector3(0f, 0.035f, 0.32f),
                new Vector3(0.68f, 0.035f, 0.52f), new Color(0.82f, 0.96f, 1f, 0.18f), true);
            var frameColor = new Color(0.73f, 0.92f, 0.96f, 0.68f);
            CreatePart("Lid Front Edge", PrimitiveType.Cube, LidPivot, new Vector3(0f, 0.055f, 0.625f),
                new Vector3(0.80f, 0.065f, 0.09f), frameColor, true);
            CreatePart("Lid Back Edge", PrimitiveType.Cube, LidPivot, new Vector3(0f, 0.055f, 0.015f),
                new Vector3(0.80f, 0.065f, 0.09f), frameColor, true);
            CreatePart("Lid Left Edge", PrimitiveType.Cube, LidPivot, new Vector3(-0.355f, 0.055f, 0.32f),
                new Vector3(0.09f, 0.065f, 0.52f), frameColor, true);
            CreatePart("Lid Right Edge", PrimitiveType.Cube, LidPivot, new Vector3(0.355f, 0.055f, 0.32f),
                new Vector3(0.09f, 0.065f, 0.52f), frameColor, true);
            CreatePart("Lid Handle", PrimitiveType.Cube, LidPivot, new Vector3(0f, 0.105f, 0.51f),
                new Vector3(0.28f, 0.07f, 0.08f), frameColor, true);

            IsLidClosed = container.IsComplete;
            LidPivot.localRotation = IsLidClosed
                ? Quaternion.identity
                : Quaternion.Euler(OpenLidAngle, 0f, 0f);
        }

        private static void CreateRim(Transform parent)
        {
            CreatePart("Rim Front", PrimitiveType.Cube, parent, new Vector3(0f, 0.37f, 0.29f),
                new Vector3(0.72f, 0.08f, 0.07f), BodyColor);
            CreatePart("Rim Back", PrimitiveType.Cube, parent, new Vector3(0f, 0.37f, -0.29f),
                new Vector3(0.72f, 0.08f, 0.07f), BodyColor);
            CreatePart("Rim Left", PrimitiveType.Cube, parent, new Vector3(-0.35f, 0.37f, 0f),
                new Vector3(0.07f, 0.08f, 0.52f), BodyColor);
            CreatePart("Rim Right", PrimitiveType.Cube, parent, new Vector3(0.35f, 0.37f, 0f),
                new Vector3(0.07f, 0.08f, 0.52f), BodyColor);
        }

        private static void CreateCornerGuards(Transform parent)
        {
            var positions = new[]
            {
                new Vector3(-0.36f, 0.2f, -0.29f),
                new Vector3(0.36f, 0.2f, -0.29f),
                new Vector3(-0.36f, 0.2f, 0.29f),
                new Vector3(0.36f, 0.2f, 0.29f)
            };

            for (var index = 0; index < positions.Length; index++)
            {
                CreatePart("Teal Corner Guard " + (index + 1), PrimitiveType.Cube, parent, positions[index],
                    new Vector3(0.14f, 0.3f, 0.14f), GuardColor);
            }
        }

        private static void CreateContents(DeliveryContainer container, Transform parent)
        {
            if (container.Components.Count == 0)
            {
                return;
            }

            if (container.CompletedRecipeId == GameIds.VegetableSoupRecipe)
            {
                CreatePart("Soup", PrimitiveType.Cube, parent, new Vector3(0f, 0f, 0f),
                    new Vector3(0.56f, 0.045f, 0.42f), KitchenArtPalette.Soup);
                if (!TryCreateCardMatchedIngredient(
                        "Soup Carrot Card-Matched Cluster",
                        GameIds.CarrotIngredient,
                        IngredientPreparation.Chopped,
                        parent,
                        new Vector3(-0.14f, 0.045f, -0.06f),
                        0.34f,
                        -12f))
                {
                    CreatePart("Soup Carrot Fallback", PrimitiveType.Sphere, parent,
                        new Vector3(-0.14f, 0.07f, -0.06f), new Vector3(0.15f, 0.07f, 0.12f),
                        KitchenArtPalette.Carrot);
                }

                if (!TryCreateCardMatchedIngredient(
                        "Soup Onion Card-Matched Cluster",
                        GameIds.OnionIngredient,
                        IngredientPreparation.Chopped,
                        parent,
                        new Vector3(0.15f, 0.05f, 0.06f),
                        0.34f,
                        24f))
                {
                    CreatePart("Soup Onion Fallback", PrimitiveType.Sphere, parent,
                        new Vector3(0.15f, 0.075f, 0.06f), new Vector3(0.14f, 0.065f, 0.11f),
                        KitchenArtPalette.OnionFlesh);
                }

                return;
            }

            if (container.CompletedRecipeId == GameIds.LettuceSaladRecipe)
            {
                CreateSaladContents(parent);
                return;
            }

            if (container.CompletedRecipeId == GameIds.HamburgerPlateRecipe)
            {
                CreateHamburgerPlateContents(parent);
                return;
            }

            var columns = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(container.Components.Count)));
            for (var index = 0; index < container.Components.Count; index++)
            {
                var component = container.Components[index];
                var x = (index % columns - (columns - 1) * 0.5f) * 0.2f;
                var z = (index / columns - 0.5f) * 0.17f;
                var presentationPreparation = component.Preparation == IngredientPreparation.Raw
                    ? IngredientPreparation.Raw
                    : component.IngredientId == GameIds.BeefIngredient &&
                      component.Preparation == IngredientPreparation.Cooked
                        ? IngredientPreparation.Cooked
                        : IngredientPreparation.Chopped;
                var scale = component.IngredientId == GameIds.LettuceIngredient
                    ? 0.38f
                    : 0.31f;
                if (TryCreateCardMatchedIngredient(
                        "Food " + component.IngredientId + " Card-Matched Model",
                        component.IngredientId,
                        presentationPreparation,
                        parent,
                        new Vector3(x, 0.04f, z),
                        scale,
                        index * 37f))
                {
                    continue;
                }

                var fallbackScale = component.IngredientId == GameIds.LettuceIngredient
                    ? new Vector3(0.24f, 0.08f, 0.2f)
                    : new Vector3(0.16f, 0.09f, 0.16f);
                CreatePart("Food " + component.IngredientId, PrimitiveType.Sphere, parent,
                    new Vector3(x, 0.055f, z), fallbackScale, IngredientColor(component.IngredientId));
            }
        }

        public static void BuildFoodContents(DeliveryContainer container, Transform parent)
        {
            CreateContents(container, parent);
        }

        private static void CreateSaladContents(Transform parent)
        {
            if (TryCreateCardMatchedIngredient(
                    "Salad Lettuce Card-Matched Cluster",
                    GameIds.LettuceIngredient,
                    IngredientPreparation.Chopped,
                    parent,
                    new Vector3(0f, 0.02f, 0f),
                    0.58f,
                    -8f))
            {
                return;
            }

            var lettuce = IngredientColor(GameIds.LettuceIngredient);
            var positions = new[]
            {
                new Vector3(-0.2f, 0.05f, -0.1f),
                new Vector3(0f, 0.07f, -0.12f),
                new Vector3(0.2f, 0.05f, -0.05f),
                new Vector3(-0.12f, 0.08f, 0.13f),
                new Vector3(0.14f, 0.09f, 0.12f)
            };
            for (var index = 0; index < positions.Length; index++)
            {
                var leaf = CreatePart("Salad Lettuce Leaf " + (index + 1), PrimitiveType.Sphere, parent,
                    positions[index], new Vector3(0.25f, 0.07f, 0.17f),
                    index % 2 == 0 ? lettuce : lettuce * 0.88f);
                leaf.transform.localEulerAngles = new Vector3(0f, index * 43f, index % 2 == 0 ? -7f : 7f);
            }
        }

        private static void CreateHamburgerPlateContents(Transform parent)
        {
            var plate = CreatePart("Hamburger Plate", PrimitiveType.Cylinder, parent,
                new Vector3(0f, 0.015f, 0f), new Vector3(0.30f, 0.025f, 0.24f),
                KitchenArtPalette.BoardIvory);
            plate.transform.localEulerAngles = Vector3.zero;

            if (!TryCreateCardMatchedIngredient(
                    "Hamburger Beef Card-Matched Patty",
                    GameIds.BeefIngredient,
                    IngredientPreparation.Cooked,
                    parent,
                    new Vector3(-0.11f, 0.035f, 0f),
                    0.56f,
                    -12f))
            {
                var patty = CreatePart("Browned Hamburger Patty", PrimitiveType.Sphere, parent,
                    new Vector3(-0.11f, 0.075f, 0f), new Vector3(0.25f, 0.075f, 0.22f),
                    KitchenArtPalette.BeefCooked);
                patty.transform.localEulerAngles = new Vector3(0f, -12f, 0f);
            }

            if (TryCreateCardMatchedIngredient(
                    "Hamburger Lettuce Card-Matched Cluster",
                    GameIds.LettuceIngredient,
                    IngredientPreparation.Chopped,
                    parent,
                    new Vector3(0.20f, 0.04f, 0f),
                    0.34f,
                    18f))
            {
                return;
            }

            var lettucePositions = new[]
            {
                new Vector3(0.19f, 0.06f, -0.11f),
                new Vector3(0.23f, 0.075f, 0f),
                new Vector3(0.18f, 0.065f, 0.12f)
            };
            for (var index = 0; index < lettucePositions.Length; index++)
            {
                var leaf = CreatePart("Hamburger Side Lettuce " + (index + 1), PrimitiveType.Sphere, parent,
                    lettucePositions[index], new Vector3(0.17f, 0.045f, 0.115f),
                    index == 1 ? KitchenArtPalette.Lettuce : KitchenArtPalette.LeafGreen);
                leaf.transform.localEulerAngles = new Vector3(0f, index * 48f - 35f, index % 2 == 0 ? -7f : 7f);
            }
        }

        private static bool TryCreateCardMatchedIngredient(
            string objectName,
            string ingredientId,
            IngredientPreparation preparation,
            Transform parent,
            Vector3 localPosition,
            float uniformScale,
            float yaw)
        {
            if (!KitchenProductionAssetFactory.TryInstantiateIngredient(
                    ingredientId,
                    preparation,
                    parent,
                    out var asset))
            {
                return false;
            }

            asset.gameObject.name = objectName;
            asset.transform.localPosition = localPosition;
            asset.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            asset.transform.localScale = Vector3.one * uniformScale;
            return true;
        }

        private static Color IngredientColor(string ingredientId)
        {
            if (ingredientId == GameIds.BeefIngredient)
            {
                return KitchenArtPalette.BeefCooked;
            }

            if (ingredientId == GameIds.CarrotIngredient)
            {
                return KitchenArtPalette.Carrot;
            }

            if (ingredientId == GameIds.OnionIngredient)
            {
                return KitchenArtPalette.OnionFlesh;
            }

            return KitchenArtPalette.Lettuce;
        }

        private static GameObject CreatePart(
            string name,
            PrimitiveType primitive,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            bool transparent = false)
        {
            var part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            part.GetComponent<Renderer>().material = transparent
                ? GrayboxMaterials.CreateTransparent(color)
                : GrayboxMaterials.Create(color);
            return part;
        }
    }

    public static class GrayboxMaterials
    {
        public static Material Create(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader);
            material.color = color;
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.18f);
            }
            return material;
        }

        public static Material CreateTransparent(Color color)
        {
            var material = Create(color);
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return material;
        }
    }
}
