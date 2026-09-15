using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CookedOut.Domain;

namespace CookedOut.Presentation
{
    public static class TutorialSceneBootstrap
    {
        public const float FixedKitchenFieldOfView = 42.6f;
        public const float FixedKitchenAimHeight = 1.35f;
        public const float MainLightShadowStrength = 0.38f;
        private static readonly Color FloorColor = KitchenArtPalette.FloorA;
        private static readonly Color CounterColor = KitchenArtPalette.Structure;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallSceneLoadHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void EnsureInstalled()
        {
            if (!IsTutorialScene(SceneManager.GetActiveScene().name) ||
                Object.FindFirstObjectByType<KitchenGameController>() != null)
            {
                return;
            }

            BuildScene();
        }

        private static bool IsTutorialScene(string sceneName)
        {
            return sceneName == "Tutorial_1_1" ||
                   sceneName == "Tutorial_1_2" ||
                   sceneName == "Tutorial_1_3" ||
                   sceneName == "Tutorial_1_4" ||
                   sceneName == "Tutorial_1_5" ||
                   sceneName == "Tutorial_1_6" ||
                   sceneName == "Tutorial_1_7" ||
                   sceneName == "Tutorial_1_8";
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureInstalled();
        }

        private static void BuildScene()
        {
            var sceneName = SceneManager.GetActiveScene().name;
            var throwDeliveryStage = sceneName == "Tutorial_1_3";
            var dashStage = sceneName == "Tutorial_1_4";
            var fryingStage = sceneName == "Tutorial_1_5";
            var fireRecoveryStage = sceneName == "Tutorial_1_6";
            var dishwashingStage = sceneName == "Tutorial_1_7";
            var combinedDeliveryStage = sceneName == "Tutorial_1_8";
            var soupStage = sceneName == "Tutorial_1_2";
            var rootName = combinedDeliveryStage
                ? "CookedOut_Tutorial_1_8"
                : dishwashingStage
                ? "CookedOut_Tutorial_1_7"
                : fireRecoveryStage
                ? "CookedOut_Tutorial_1_6"
                : fryingStage
                ? "CookedOut_Tutorial_1_5"
                : dashStage
                ? "CookedOut_Tutorial_1_4"
                : throwDeliveryStage
                ? "CookedOut_Tutorial_1_3"
                : soupStage ? "CookedOut_Tutorial_1_2" : "CookedOut_Tutorial_1_1";
            var root = new GameObject(rootName);
            var game = root.AddComponent<KitchenGameController>();
            var input = root.AddComponent<KitchenInputRouter>();
            var gridDefinition = combinedDeliveryStage
                ? TutorialCombinedDeliveryKitchenGrid.Create()
                : dishwashingStage
                ? TutorialDishwashingKitchenGrid.Create()
                : fireRecoveryStage
                ? TutorialFireRecoveryKitchenGrid.Create()
                : fryingStage
                ? TutorialFryingKitchenGrid.Create()
                : dashStage
                ? TutorialDashKitchenGrid.Create()
                : throwDeliveryStage
                ? TutorialThrowDeliveryKitchenGrid.Create()
                : soupStage ? TutorialSoupKitchenGrid.Create() : TutorialKitchenGrid.Create();
            var validation = gridDefinition.Validate();
            if (!validation.IsValid)
            {
                throw new System.InvalidOperationException(
                    "Tutorial kitchen grid is invalid:\n" + string.Join("\n", validation.Errors));
            }

            var grid = root.AddComponent<KitchenGridRuntime>();
            grid.Initialize(gridDefinition);

            BuildLightingAndCamera(root.transform, grid);
            BuildKitchen(root.transform, game, grid);
            BuildPlayer(root.transform, input, game, grid);
            if (dashStage)
            {
                BuildDashConveyor(root.transform, Object.FindFirstObjectByType<PlayerController>(), grid);
            }
            if (throwDeliveryStage || combinedDeliveryStage)
            {
                BuildBikeDelivery(root.transform, game, grid);
            }
            BuildUi(
                root.transform,
                input,
                game,
                out var shiftTimer,
                out var score,
                out var clockPausedIcon,
                out var orderTicket,
                out var guide,
                out var resultPanel,
                out var resultText);
            game.Initialize(
                shiftTimer,
                score,
                clockPausedIcon,
                orderTicket,
                resultPanel,
                resultText,
                gridDefinition.StageId);
            if (fireRecoveryStage)
            {
                game.SetClockPaused(true);
                FireIncidentController.Spawn(game, root.transform, grid.CellCenter(new GridCoordinate(5, 7), 0.05f));
            }
            if (combinedDeliveryStage)
            {
                DynamicKitchenConnector.Spawn(root.transform, grid, new GridCoordinate(8, 4));
            }
            if (dashStage)
            {
                root.AddComponent<DashTutorialController>().Initialize(
                    game,
                    Object.FindFirstObjectByType<PlayerController>(),
                    grid,
                    guide,
                    null);
            }
            else if (throwDeliveryStage)
            {
                root.AddComponent<TutorialGuideController>().Initialize(
                    game,
                    Object.FindFirstObjectByType<PlayerController>(),
                    Object.FindFirstObjectByType<DeliveryBikeController>(),
                    guide);
            }
        }

        private static void BuildLightingAndCamera(Transform parent, KitchenGridRuntime grid)
        {
            var definition = grid.Definition;
            var center = new Vector3(
                definition.Space.OriginX + definition.Width * definition.Space.CellSize * 0.5f,
                0f,
                definition.Space.OriginZ + definition.Depth * definition.Space.CellSize * 0.5f);
            var cameraObject = new GameObject("Fixed Kitchen Camera");
            cameraObject.transform.SetParent(parent);
            var cameraDistance = definition.Width > 10 ? 18f : 15f;
            cameraObject.transform.position = center + new Vector3(
                0f,
                cameraDistance * 1.20f,
                -cameraDistance * 0.82f);
            cameraObject.transform.LookAt(center + Vector3.up * FixedKitchenAimHeight);
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = FixedKitchenFieldOfView;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.075f, 0.1f);
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<DeliveryCameraController>().CaptureKitchenView();

            var lightObject = new GameObject("Sun");
            lightObject.transform.SetParent(parent);
            lightObject.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.08f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = MainLightShadowStrength;

            RenderSettings.ambientLight = new Color(0.32f, 0.34f, 0.37f);
        }

        private static void BuildKitchen(
            Transform parent,
            KitchenGameController game,
            KitchenGridRuntime grid)
        {
            var kitchen = new GameObject("Grid Kitchen").transform;
            kitchen.SetParent(parent, false);
            var cellSize = grid.Definition.Space.CellSize;
            CreateContinuousFloorCollider(kitchen, grid);
            for (var z = 0; z < grid.Definition.Depth; z++)
            {
                for (var x = 0; x < grid.Definition.Width; x++)
                {
                    var cell = new GridCoordinate(x, z);
                    var terrain = grid.Definition.TerrainAt(cell);
                    var position = grid.CellCenter(cell, terrain == GridTerrain.Wall ? 0.55f : -0.12f);
                    if (terrain == GridTerrain.Wall)
                    {
                        var wall = CreatePrimitive(
                            $"Wall [{x},{z}]",
                            PrimitiveType.Cube,
                            kitchen,
                            position,
                            new Vector3(cellSize * 0.96f, 1.1f, cellSize * 0.96f),
                            KitchenArtPalette.Wall);
                        if (KitchenProductionAssetFactory.TryInstantiateTerrain(
                                terrain,
                                false,
                                kitchen,
                                position,
                                cellSize,
                                out _))
                        {
                            wall.GetComponent<Renderer>().enabled = false;
                        }
                    }
                    else if (terrain == GridTerrain.Floor)
                    {
                        var floorTile = CreatePrimitive(
                            $"Floor [{x},{z}]",
                            PrimitiveType.Cube,
                            kitchen,
                            position,
                            new Vector3(cellSize * 1.005f, 0.24f, cellSize * 1.005f),
                            (x + z) % 2 == 0 ? FloorColor : KitchenArtPalette.FloorB);
                        if (KitchenProductionAssetFactory.TryInstantiateTerrain(
                                terrain,
                                (x + z) % 2 != 0,
                                kitchen,
                                position,
                                cellSize,
                                out _))
                        {
                            floorTile.GetComponent<Renderer>().enabled = false;
                        }
                        var tileCollider = floorTile.GetComponent<Collider>();
                        if (tileCollider != null)
                        {
                            Object.Destroy(tileCollider);
                        }
                    }
                }
            }

            foreach (var placement in grid.Definition.Placements)
            {
                CreateGridStation(kitchen, game, grid, placement);
            }
        }

        private static void CreateContinuousFloorCollider(Transform parent, KitchenGridRuntime grid)
        {
            var definition = grid.Definition;
            var cellSize = definition.Space.CellSize;
            var floor = new GameObject("Continuous Floor Collider");
            floor.transform.SetParent(parent, false);
            floor.transform.position = new Vector3(
                definition.Space.OriginX + definition.Width * cellSize * 0.5f,
                -0.12f,
                definition.Space.OriginZ + definition.Depth * cellSize * 0.5f);
            var collider = floor.AddComponent<BoxCollider>();
            collider.size = new Vector3(
                definition.Width * cellSize,
                0.24f,
                definition.Depth * cellSize);
        }

        private static void BuildPlayer(
            Transform parent,
            KitchenInputRouter input,
            KitchenGameController game,
            KitchenGridRuntime grid)
        {
            var player = new GameObject("Player Chef");
            player.transform.SetParent(parent);
            player.transform.position = grid.CellCenter(grid.Definition.PlayerSpawn, 0.05f);
            var characterController = player.AddComponent<CharacterController>();
            characterController.radius = 0.4f;
            characterController.height = 1.55f;
            characterController.center = new Vector3(0f, 0.78f, 0f);
            characterController.skinWidth = 0.05f;
            characterController.minMoveDistance = 0f;
            characterController.stepOffset = 0f;
            characterController.slopeLimit = 45f;
            characterController.detectCollisions = true;
            characterController.enableOverlapRecovery = true;

            var visualRoot = new GameObject("Player Visual Root").transform;
            visualRoot.SetParent(player.transform, false);
            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = Vector3.one;

            AnimalChefVisual characterVisual;
            if (!CharacterVisualFactory.TryInstantiateProduction("capybara", visualRoot, out characterVisual, out _))
            {
                characterVisual = BuildCapybaraChefVisual(visualRoot);
            }
            var playerController = player.AddComponent<PlayerController>();
            playerController.Initialize(input, game, characterVisual.HeldItemAnchor, grid);
            player.AddComponent<PlayerIdentityMarker>().Initialize(1);
            player.AddComponent<DashSpeedEffect>().Initialize(playerController);
            player.AddComponent<AnimalChefMotion>().Initialize(playerController, input, characterVisual);
        }

        private static AnimalChefVisual BuildCapybaraChefVisual(Transform visualRoot)
        {
            var cream = new Color(0.93f, 0.86f, 0.72f);
            var ivory = new Color(0.98f, 0.94f, 0.86f);
            var ochre = new Color(0.88f, 0.51f, 0.08f);
            var plum = new Color(0.18f, 0.12f, 0.19f);
            var fur = new Color(0.58f, 0.31f, 0.14f);
            var innerEar = new Color(0.34f, 0.16f, 0.11f);
            var muzzle = new Color(0.34f, 0.20f, 0.16f);

            var motionRoot = new GameObject("Character Motion Root").transform;
            motionRoot.SetParent(visualRoot, false);

            var commonUniformRoot = new GameObject("Common Cook Body").transform;
            commonUniformRoot.SetParent(motionRoot, false);

            CreateVisualPrimitive("Chef Body", PrimitiveType.Capsule, commonUniformRoot,
                new Vector3(0f, 0.78f, 0f), new Vector3(0.72f, 0.78f, 0.64f), cream);

            CreateVisualPrimitive("Apron Bib", PrimitiveType.Cube, commonUniformRoot,
                new Vector3(0f, 0.82f, 0.5f), new Vector3(0.48f, 0.48f, 0.08f), plum);
            CreateVisualPrimitive("Apron Skirt", PrimitiveType.Cube, commonUniformRoot,
                new Vector3(0f, 0.48f, 0.39f), new Vector3(0.68f, 0.32f, 0.12f), plum);
            CreateVisualPrimitive("Left Fastening Disc", PrimitiveType.Sphere, commonUniformRoot,
                new Vector3(-0.18f, 0.98f, 0.54f), new Vector3(0.1f, 0.1f, 0.06f), plum);
            CreateVisualPrimitive("Right Fastening Disc", PrimitiveType.Sphere, commonUniformRoot,
                new Vector3(0.18f, 0.98f, 0.54f), new Vector3(0.1f, 0.1f, 0.06f), plum);
            CreateVisualPrimitive("Ochre Neck Tab", PrimitiveType.Cube, commonUniformRoot,
                new Vector3(0f, 1.12f, 0.48f), new Vector3(0.16f, 0.18f, 0.08f), ochre);
            CreateVisualPrimitive("Left Sleeve", PrimitiveType.Sphere, commonUniformRoot,
                new Vector3(-0.43f, 0.82f, 0f), new Vector3(0.27f, 0.3f, 0.3f), cream);
            CreateVisualPrimitive("Right Sleeve", PrimitiveType.Sphere, commonUniformRoot,
                new Vector3(0.43f, 0.82f, 0f), new Vector3(0.27f, 0.3f, 0.3f), cream);

            var speciesBodyRoot = new GameObject("Capybara Limbs").transform;
            speciesBodyRoot.SetParent(motionRoot, false);
            var leftArmPivot = new GameObject("Left Arm Pivot").transform;
            leftArmPivot.SetParent(speciesBodyRoot, false);
            leftArmPivot.localPosition = new Vector3(-0.43f, 0.84f, 0f);
            leftArmPivot.localRotation = Quaternion.Euler(0f, 0f, -18f);
            CreateVisualPrimitive("Left Capybara Forearm", PrimitiveType.Capsule, leftArmPivot,
                new Vector3(-0.05f, -0.18f, 0.13f), new Vector3(0.2f, 0.34f, 0.2f), fur);
            var rightArmPivot = new GameObject("Right Arm Pivot").transform;
            rightArmPivot.SetParent(speciesBodyRoot, false);
            rightArmPivot.localPosition = new Vector3(0.43f, 0.84f, 0f);
            rightArmPivot.localRotation = Quaternion.Euler(0f, 0f, 18f);
            CreateVisualPrimitive("Right Capybara Forearm", PrimitiveType.Capsule, rightArmPivot,
                new Vector3(0.05f, -0.18f, 0.13f), new Vector3(0.2f, 0.34f, 0.2f), fur);
            var leftFootPivot = new GameObject("Left Foot Pivot").transform;
            leftFootPivot.SetParent(speciesBodyRoot, false);
            leftFootPivot.localPosition = new Vector3(-0.22f, 0.2f, 0.08f);
            CreateVisualPrimitive("Left Capybara Foot", PrimitiveType.Sphere, leftFootPivot,
                Vector3.zero, new Vector3(0.25f, 0.18f, 0.31f), fur);
            var rightFootPivot = new GameObject("Right Foot Pivot").transform;
            rightFootPivot.SetParent(speciesBodyRoot, false);
            rightFootPivot.localPosition = new Vector3(0.22f, 0.2f, 0.08f);
            CreateVisualPrimitive("Right Capybara Foot", PrimitiveType.Sphere, rightFootPivot,
                Vector3.zero, new Vector3(0.25f, 0.18f, 0.31f), fur);

            var speciesHeadRoot = new GameObject("Capybara Head").transform;
            speciesHeadRoot.SetParent(motionRoot, false);
            speciesHeadRoot.localPosition = new Vector3(0f, 1.32f, 0f);
            CreateVisualPrimitive("Capybara Face", PrimitiveType.Sphere, speciesHeadRoot,
                Vector3.zero, new Vector3(0.82f, 0.66f, 0.66f), fur);
            CreateVisualPrimitive("Blocky Muzzle", PrimitiveType.Cube, speciesHeadRoot,
                new Vector3(0f, -0.06f, 0.52f), new Vector3(0.52f, 0.34f, 0.32f), muzzle);
            CreateVisualPrimitive("Nose", PrimitiveType.Sphere, speciesHeadRoot,
                new Vector3(0f, 0.02f, 0.7f), new Vector3(0.2f, 0.13f, 0.09f), innerEar);
            CreateVisualPrimitive("Left Eye", PrimitiveType.Sphere, speciesHeadRoot,
                new Vector3(-0.25f, 0.15f, 0.55f), new Vector3(0.11f, 0.15f, 0.07f), Color.black);
            CreateVisualPrimitive("Right Eye", PrimitiveType.Sphere, speciesHeadRoot,
                new Vector3(0.25f, 0.15f, 0.55f), new Vector3(0.11f, 0.15f, 0.07f), Color.black);
            CreateVisualPrimitive("Left Ear", PrimitiveType.Sphere, speciesHeadRoot,
                new Vector3(-0.42f, 0.28f, 0f), new Vector3(0.22f, 0.22f, 0.14f), innerEar);
            CreateVisualPrimitive("Right Ear", PrimitiveType.Sphere, speciesHeadRoot,
                new Vector3(0.42f, 0.28f, 0f), new Vector3(0.22f, 0.22f, 0.14f), innerEar);
            var leftBrow = CreateVisualPrimitive("Left Brow", PrimitiveType.Cube, speciesHeadRoot,
                new Vector3(-0.24f, 0.31f, 0.59f), new Vector3(0.18f, 0.045f, 0.045f), plum).transform;
            leftBrow.localRotation = Quaternion.Euler(0f, 0f, 8f);
            var rightBrow = CreateVisualPrimitive("Right Brow", PrimitiveType.Cube, speciesHeadRoot,
                new Vector3(0.24f, 0.31f, 0.59f), new Vector3(0.18f, 0.045f, 0.045f), plum).transform;
            rightBrow.localRotation = Quaternion.Euler(0f, 0f, -8f);
            CreateVisualPrimitive("Smile", PrimitiveType.Sphere, speciesHeadRoot,
                new Vector3(0f, -0.22f, 0.7f), new Vector3(0.2f, 0.11f, 0.06f), plum);
            CreateVisualPrimitive("Left Incisor", PrimitiveType.Cube, speciesHeadRoot,
                new Vector3(-0.055f, -0.15f, 0.76f), new Vector3(0.09f, 0.1f, 0.04f), ivory);
            CreateVisualPrimitive("Right Incisor", PrimitiveType.Cube, speciesHeadRoot,
                new Vector3(0.055f, -0.15f, 0.76f), new Vector3(0.09f, 0.1f, 0.04f), ivory);

            var capRoot = new GameObject("Common Wedge Chef Cap").transform;
            capRoot.SetParent(speciesHeadRoot, false);
            CreateVisualPrimitive("Ochre Cap Band", PrimitiveType.Cylinder, capRoot,
                new Vector3(0f, 0.36f, 0f), new Vector3(0.5f, 0.08f, 0.5f), ochre);
            var crown = CreateVisualPrimitive("Leaning Ivory Crown", PrimitiveType.Sphere, capRoot,
                new Vector3(-0.1f, 0.51f, 0f), new Vector3(0.62f, 0.2f, 0.5f), ivory);
            crown.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            CreateVisualPrimitive("Shallow Rear Fold", PrimitiveType.Sphere, capRoot,
                new Vector3(0.24f, 0.43f, -0.05f), new Vector3(0.32f, 0.13f, 0.4f), ivory);

            var heldAnchor = new GameObject("Held Item Anchor").transform;
            heldAnchor.SetParent(visualRoot, false);
            heldAnchor.localPosition = AnimalChefVisual.StandardHeldItemLocalPosition;
            heldAnchor.localRotation = Quaternion.identity;
            heldAnchor.localScale = Vector3.one;

            var visual = visualRoot.gameObject.AddComponent<AnimalChefVisual>();
            visual.Initialize(
                "capybara",
                motionRoot,
                commonUniformRoot,
                speciesBodyRoot,
                speciesHeadRoot,
                capRoot,
                heldAnchor,
                leftArmPivot,
                rightArmPivot,
                leftFootPivot,
                rightFootPivot,
                leftBrow,
                rightBrow);
            return visual;
        }

        private static GameObject CreateVisualPrimitive(
            string name,
            PrimitiveType primitive,
            Transform parent,
            Vector3 localPosition,
            Vector3 scale,
            Color color)
        {
            var gameObject = CreatePrimitive(name, primitive, parent, localPosition, scale, color);
            var collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            return gameObject;
        }

        private static void CreateGridStation(
            Transform parent,
            KitchenGameController game,
            KitchenGridRuntime grid,
            GridPlacement placement)
        {
            var kind = StationKind.Counter;
            var label = "COUNTER";
            var color = CounterColor;
            var primitive = PrimitiveType.Cube;
            string sourceIngredientId = null;
            switch (placement.ArchetypeId)
            {
                case KitchenArchetypeIds.IngredientCrate:
                    kind = StationKind.IngredientCrate;
                    label = "INGREDIENT";
                    color = new Color(0.24f, 0.18f, 0.28f);
                    sourceIngredientId = placement.ContentId;
                    break;
                case KitchenArchetypeIds.ChoppingBoard:
                    kind = StationKind.ChoppingBoard;
                    label = "CHOP";
                    color = new Color(0.65f, 0.43f, 0.2f);
                    break;
                case KitchenArchetypeIds.PotHeatSource:
                    kind = StationKind.PotHeatSource;
                    label = "POT HEAT";
                    color = new Color(0.72f, 0.28f, 0.08f);
                    break;
                case KitchenArchetypeIds.PanHeatSource:
                    kind = StationKind.PanHeatSource;
                    label = "PAN HEAT";
                    color = new Color(0.72f, 0.28f, 0.08f);
                    break;
                case KitchenArchetypeIds.AssemblyCounter:
                    label = "ASSEMBLE";
                    break;
                case KitchenArchetypeIds.ContainerDispenser:
                    kind = StationKind.ContainerDispenser;
                    label = "CONTAINER";
                    color = new Color(0.82f, 0.65f, 0.3f);
                    break;
                case KitchenArchetypeIds.ServingHatch:
                    kind = StationKind.ServingHatch;
                    label = "SERVE";
                    color = new Color(0.15f, 0.55f, 0.8f);
                    break;
                case KitchenArchetypeIds.TrashBin:
                    kind = StationKind.TrashBin;
                    label = "TRASH";
                    color = new Color(0.48f, 0.25f, 0.25f);
                    primitive = PrimitiveType.Cylinder;
                    break;
                case KitchenArchetypeIds.BikeDock:
                    kind = StationKind.BikeDock;
                    label = "BIKE LOAD";
                    color = new Color(0.1f, 0.58f, 0.62f);
                    break;
                case KitchenArchetypeIds.DeliveryPoint:
                    kind = StationKind.DeliveryPoint;
                    label = "DELIVERY";
                    color = new Color(0.2f, 0.72f, 0.34f);
                    primitive = PrimitiveType.Cylinder;
                    break;
                case KitchenArchetypeIds.RecoveryBin:
                    kind = StationKind.RecoveryBin;
                    label = "RECOVERY";
                    color = new Color(0.85f, 0.52f, 0.12f);
                    break;
                case KitchenArchetypeIds.ExtinguisherCabinet:
                    kind = StationKind.ExtinguisherCabinet;
                    label = "消火器";
                    color = new Color(0.82f, 0.15f, 0.11f);
                    break;
                case KitchenArchetypeIds.PlateDispenser:
                    kind = StationKind.PlateDispenser;
                    label = "清潔な皿";
                    color = new Color(0.12f, 0.62f, 0.68f);
                    break;
                case KitchenArchetypeIds.DishReturn:
                    kind = StationKind.DishReturn;
                    label = "返却口";
                    color = new Color(0.78f, 0.42f, 0.14f);
                    break;
                case KitchenArchetypeIds.WashingSink:
                    kind = StationKind.WashingSink;
                    label = "洗い場";
                    color = new Color(0.18f, 0.58f, 0.75f);
                    break;
                case KitchenArchetypeIds.CourierShelf:
                    kind = StationKind.CourierShelf;
                    label = "配達代行";
                    color = new Color(0.12f, 0.64f, 0.52f);
                    break;
            }

            color = KitchenStationArtFactory.BodyColor(placement.ArchetypeId);

            CreateStation(
                parent,
                game,
                kind,
                label,
                grid.CellCenter(placement.Anchor, 0.45f),
                color,
                primitive,
                placement,
                grid.Definition.Space.CellSize,
                sourceIngredientId);
        }

        private static void CreateStation(
            Transform parent,
            KitchenGameController game,
            StationKind kind,
            string label,
            Vector3 position,
            Color color,
            PrimitiveType primitive,
            GridPlacement gridPlacement,
            float cellSize,
            string sourceIngredientId)
        {
            var stationObject = new GameObject(
                label + " [" + gridPlacement.Anchor.X + "," + gridPlacement.Anchor.Z + "]");
            stationObject.transform.SetParent(parent, false);
            stationObject.transform.position = position;
            stationObject.transform.rotation = Quaternion.identity;
            stationObject.transform.localScale = Vector3.one;

            var stationBody = CreatePrimitive(
                "Station Body",
                primitive,
                stationObject.transform,
                Vector3.zero,
                new Vector3(cellSize * 0.88f, 0.9f, cellSize * 0.88f),
                color);
            if (kind == StationKind.BikeDock)
            {
                // The bike itself is the pickup/place target. Retain the logical
                // station for interaction, but remove the old workbench collision.
                var bodyCollider = stationBody.GetComponent<Collider>();
                if (bodyCollider != null)
                {
                    bodyCollider.enabled = false;
                    Object.Destroy(bodyCollider);
                }
            }
            var visualOrientation = new GameObject("Station Visual Orientation").transform;
            visualOrientation.SetParent(stationObject.transform, false);
            visualOrientation.localPosition = Vector3.zero;
            visualOrientation.localRotation = Quaternion.Euler(
                0f,
                StationVisualYaw(gridPlacement.Direction),
                0f);
            visualOrientation.localScale = Vector3.one;
            if (KitchenStationArtFactory.Decorate(
                    gridPlacement.ArchetypeId,
                    visualOrientation,
                    cellSize))
            {
                stationBody.GetComponent<Renderer>().enabled = false;
            }
            var station = stationObject.AddComponent<InteractableStation>();

            var anchor = new GameObject("Item Anchor").transform;
            anchor.SetParent(stationObject.transform, false);
            anchor.localPosition = new Vector3(0f, 0.55f, 0f);
            anchor.localRotation = Quaternion.identity;
            anchor.localScale = Vector3.one;

            var outline = CreateSelectionOutline(stationObject.transform);
            IngredientSourceCardView sourceCard = null;
            if (kind == StationKind.IngredientCrate)
            {
                sourceCard = CreateIngredientSourceCard(visualOrientation, sourceIngredientId);
            }

            station.Initialize(game, kind, anchor, outline, gridPlacement, sourceIngredientId, sourceCard);
            var equipmentResource = EquipmentCardResource(kind);
            if (equipmentResource != null)
            {
                CreateEquipmentCard(visualOrientation, equipmentResource);
            }
        }

        private static string EquipmentCardResource(StationKind kind)
        {
            switch (kind)
            {
                case StationKind.ExtinguisherCabinet:
                    return "TutorialEquipmentCards/tutorial_fire_extinguisher_sv1";
                case StationKind.PlateDispenser:
                    return "TutorialEquipmentCards/tutorial_clean_plate_sv1";
                case StationKind.DishReturn:
                case StationKind.WashingSink:
                    return "TutorialEquipmentCards/tutorial_dirty_plate_sv1";
                case StationKind.CourierShelf:
                    return "TutorialEquipmentCards/tutorial_courier_delivery_sv1";
                default:
                    return null;
            }
        }

        private static void CreateEquipmentCard(Transform parent, string resourcePath)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return;
            }
            var card = GameObject.CreatePrimitive(PrimitiveType.Quad);
            card.name = "Nonverbal Equipment Card";
            card.transform.SetParent(parent, false);
            card.transform.localPosition = new Vector3(0f, 0.76f, -0.48f);
            card.transform.localRotation = Quaternion.Euler(28f, 0f, 0f);
            card.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
            var collider = card.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent") ?? Shader.Find("Standard");
            var material = new Material(shader) { mainTexture = texture, color = Color.white };
            card.GetComponent<Renderer>().material = material;
        }

        private static float StationVisualYaw(GridDirection direction)
        {
            // Production art is authored with its customer-facing side toward local -Z.
            // Rotate only the visual hierarchy; the logical station, collider and Item Anchor
            // remain unchanged in the grid coordinate system.
            switch (direction)
            {
                case GridDirection.North:
                    return 180f;
                case GridDirection.East:
                    return -90f;
                case GridDirection.West:
                    return 90f;
                default:
                    return 0f;
            }
        }

        private static void BuildBikeDelivery(
            Transform parent,
            KitchenGameController game,
            KitchenGridRuntime grid)
        {
            var stations = parent.GetComponentsInChildren<InteractableStation>(true);
            var dock = stations.Single(station => station.Kind == StationKind.BikeDock);
            var destination = stations.Single(station => station.Kind == StationKind.DeliveryPoint);
            var dockCell = dock.GridPlacement.Anchor;
            var bikeStart = grid.CellCenter(dockCell, 0.05f);
            var cellSize = grid.Definition.Space.CellSize;
            var northEdge = grid.Definition.Space.OriginZ + grid.Definition.Depth * cellSize;
            var laneX = bikeStart.x;
            const float roadLength = 14.4f;
            var roadCenter = new Vector3(laneX, -0.12f, northEdge + roadLength * 0.5f);
            CreatePrimitive(
                "Delivery Road",
                PrimitiveType.Cube,
                parent,
                roadCenter,
                new Vector3(4.8f, 0.24f, roadLength),
                new Color(0.12f, 0.14f, 0.17f));
            CreatePrimitive(
                "Left Road Curb",
                PrimitiveType.Cube,
                parent,
                roadCenter + Vector3.left * 2.65f + Vector3.up * 0.12f,
                new Vector3(0.42f, 0.34f, roadLength),
                new Color(0.62f, 0.65f, 0.67f));
            CreatePrimitive(
                "Right Road Curb",
                PrimitiveType.Cube,
                parent,
                roadCenter + Vector3.right * 2.65f + Vector3.up * 0.12f,
                new Vector3(0.42f, 0.34f, roadLength),
                new Color(0.62f, 0.65f, 0.67f));

            for (var index = 0; index < 3; index++)
            {
                var buildingZ = northEdge + 2.2f + index * 4.6f;
                CreatePrimitive(
                    "Left Street Building " + (index + 1),
                    PrimitiveType.Cube,
                    parent,
                    new Vector3(laneX - 5.1f, 1.25f, buildingZ),
                    new Vector3(3.9f, 2.5f + index * 0.35f, 3.5f),
                    index % 2 == 0 ? new Color(0.3f, 0.38f, 0.48f) : new Color(0.46f, 0.31f, 0.35f));
                CreatePrimitive(
                    "Right Street Building " + (index + 1),
                    PrimitiveType.Cube,
                    parent,
                    new Vector3(laneX + 5.1f, 1.25f, buildingZ),
                    new Vector3(3.9f, 2.5f + (2 - index) * 0.3f, 3.5f),
                    index % 2 == 0 ? new Color(0.48f, 0.39f, 0.25f) : new Color(0.28f, 0.43f, 0.4f));
            }

            destination.transform.position = new Vector3(
                laneX,
                destination.transform.position.y,
                northEdge + roadLength - 1.6f);
            var returnPoint = BuildReturnPoint(parent, bikeStart);
            var bikeObject = new GameObject("Delivery Bike");
            bikeObject.transform.SetParent(parent, false);
            bikeObject.AddComponent<CharacterController>();
            var bike = bikeObject.AddComponent<DeliveryBikeController>();
            bike.Initialize(game, destination.transform, returnPoint, bikeStart, northEdge - cellSize * 0.50f);
            dock.AttachBike(bike);
        }

        private static Transform BuildReturnPoint(Transform parent, Vector3 position)
        {
            var root = new GameObject("Bike Return Point");
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            var marker = CreatePrimitive(
                "Return Marker",
                PrimitiveType.Cylinder,
                root.transform,
                new Vector3(0f, 0.02f, 0f),
                new Vector3(0.9f, 0.04f, 0.9f),
                new Color(1f, 0.72f, 0.12f));
            var markerCollider = marker.GetComponent<Collider>();
            if (markerCollider != null)
            {
                Object.Destroy(markerCollider);
            }

            var labelObject = new GameObject("Return Label");
            labelObject.transform.SetParent(root.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            labelObject.transform.localRotation = Quaternion.Euler(42f, 0f, 0f);
            var label = labelObject.AddComponent<TextMesh>();
            label.text = "↩";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.14f;
            label.fontSize = 36;
            label.color = new Color(1f, 0.82f, 0.25f);
            root.SetActive(false);
            return root.transform;
        }

        private static void BuildDashConveyor(
            Transform parent,
            PlayerController player,
            KitchenGridRuntime grid)
        {
            var start = grid.CellCenter(new GridCoordinate(4, 2), 0f);
            var end = grid.CellCenter(new GridCoordinate(5, 2), 0f);
            var center = (start + end) * 0.5f;
            var cellSize = grid.Definition.Space.CellSize;
            var beltLength = cellSize * 2f;
            var root = new GameObject("Reverse Conveyor");
            root.transform.SetParent(parent, false);
            var surface = CreatePrimitive(
                "Conveyor Surface",
                PrimitiveType.Cube,
                root.transform,
                center + Vector3.down * 0.03f,
                new Vector3(beltLength * 0.98f, 0.1f, cellSize * 0.86f),
                new Color(0.12f, 0.48f, 0.58f));
            var surfaceCollider = surface.GetComponent<Collider>();
            if (surfaceCollider != null)
            {
                Object.Destroy(surfaceCollider);
            }

            for (var stripe = -2; stripe <= 2; stripe++)
            {
                var marker = CreatePrimitive(
                    "Reverse Stripe " + (stripe + 2),
                    PrimitiveType.Cube,
                    root.transform,
                    center + new Vector3(stripe * 0.52f, 0.04f, 0f),
                    new Vector3(0.1f, 0.03f, cellSize * 0.7f),
                    new Color(0.75f, 0.92f, 0.95f));
                var collider = marker.GetComponent<Collider>();
                if (collider != null)
                {
                    Object.Destroy(collider);
                }
            }

            var labelObject = new GameObject("Reverse Conveyor Label");
            labelObject.transform.SetParent(root.transform, false);
            labelObject.transform.position = center + Vector3.up * 0.72f;
            labelObject.transform.rotation = Quaternion.Euler(70f, 0f, 0f);
            var label = labelObject.AddComponent<TextMesh>();
            label.text = "◀  ◀  ◀";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.12f;
            label.fontSize = 34;
            label.color = Color.white;

            root.AddComponent<ConveyorBeltController>().Initialize(
                player,
                new Bounds(center + Vector3.up * 0.7f, new Vector3(beltLength, 2.4f, cellSize * 0.9f)),
                Vector3.left);
        }

        private static IngredientSourceCardView CreateIngredientSourceCard(
            Transform parent,
            string ingredientId)
        {
            var cardObject = new GameObject("Ingredient Photo Card");
            cardObject.transform.SetParent(parent, false);
            // The production produce bin is deliberately lower than a counter. Keep
            // this cue directly on its rim instead of leaving a hovering placard.
            cardObject.transform.localPosition = new Vector3(0f, 0.446f, 0f);
            cardObject.transform.localScale = Vector3.one * IngredientSourceCardView.IngredientIconScale;

            var photo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            photo.name = "Circular Ingredient Source Photo";
            photo.transform.SetParent(cardObject.transform, false);
            photo.transform.localPosition = new Vector3(0f, 0.002f, 0f);
            photo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            photo.transform.localScale = new Vector3(1.005f, 1.005f, 1f);
            var photoCollider = photo.GetComponent<Collider>();
            if (photoCollider != null)
            {
                Object.Destroy(photoCollider);
            }

            var texture = Resources.Load<Texture2D>(IngredientPhotoResourcePath(ingredientId));
            if (texture == null)
            {
                texture = new Texture2D(1, 1);
                texture.name = string.IsNullOrWhiteSpace(ingredientId)
                    ? "ingredient_unknown_placeholder"
                    : ingredientId.Replace('.', '_') + "_placeholder";
                texture.SetPixel(0, 0, IngredientPlaceholderColor(ingredientId));
                texture.Apply();
            }
            var shader = Shader.Find("CookedOut/CircularIngredientCard") ??
                         Shader.Find("Sprites/Default") ??
                         Shader.Find("Unlit/Transparent") ??
                         Shader.Find("Standard");
            var material = new Material(shader);
            material.mainTexture = texture;
            material.color = Color.white;
            photo.GetComponent<Renderer>().material = material;

            var sourceCard = cardObject.AddComponent<IngredientSourceCardView>();
            sourceCard.Initialize(ingredientId, texture);
            return sourceCard;
        }

        private static string IngredientPhotoResourcePath(string ingredientId)
        {
            if (ingredientId == GameIds.OnionIngredient)
            {
                return "IngredientSourceCards/ingredient_onion_source_card_sv3";
            }
            if (string.IsNullOrWhiteSpace(ingredientId))
            {
                return "IngredientSourceCards/ingredient_unknown_source_card_sv2";
            }

            if (ingredientId == GameIds.LettuceIngredient)
            {
                return "IngredientSourceCards/ingredient_lettuce_source_card_sv3";
            }

            return "IngredientSourceCards/" + ingredientId.Replace('.', '_') + "_source_card_sv2";
        }

        private static Color IngredientPlaceholderColor(string ingredientId)
        {
            if (ingredientId == GameIds.CarrotIngredient)
            {
                return new Color(0.95f, 0.42f, 0.08f);
            }

            if (ingredientId == GameIds.OnionIngredient)
            {
                return new Color(0.88f, 0.8f, 0.56f);
            }

            return new Color(0.25f, 0.8f, 0.3f);
        }

        private static GameObject CreateSelectionOutline(Transform parent)
        {
            var outline = new GameObject("Selection Outline");
            outline.transform.SetParent(parent, false);
            var color = new Color(1f, 0.85f, 0.1f);
            CreateOutlineBar(outline.transform, new Vector3(0f, 0.55f, 0.54f), new Vector3(1.12f, 0.08f, 0.08f), color);
            CreateOutlineBar(outline.transform, new Vector3(0f, 0.55f, -0.54f), new Vector3(1.12f, 0.08f, 0.08f), color);
            CreateOutlineBar(outline.transform, new Vector3(0.54f, 0.55f, 0f), new Vector3(0.08f, 0.08f, 1.12f), color);
            CreateOutlineBar(outline.transform, new Vector3(-0.54f, 0.55f, 0f), new Vector3(0.08f, 0.08f, 1.12f), color);
            return outline;
        }

        private static void CreateOutlineBar(Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "Outline Bar";
            bar.transform.SetParent(parent, false);
            bar.transform.localPosition = position;
            bar.transform.localScale = scale;
            var collider = bar.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            bar.GetComponent<Renderer>().material = GrayboxMaterials.Create(color);
        }

        private static void BuildUi(
            Transform parent,
            KitchenInputRouter input,
            KitchenGameController game,
            out Text shiftTimer,
            out Text score,
            out GameObject clockPausedIcon,
            out OrderTicketView orderTicket,
            out Text guide,
            out GameObject resultPanel,
            out Text resultText)
        {
            var canvasObject = new GameObject("HUD");
            canvasObject.transform.SetParent(parent);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
            canvasObject.AddComponent<GraphicRaycaster>();

            var safeAreaObject = new GameObject("Safe Area", typeof(RectTransform));
            safeAreaObject.transform.SetParent(canvasObject.transform, false);
            StretchToParent(safeAreaObject.GetComponent<RectTransform>());
            safeAreaObject.AddComponent<SafeAreaFitter>().ApplySafeArea();
            var hudRoot = safeAreaObject.transform;

            var eventSystem = new GameObject("EventSystem");
            eventSystem.transform.SetParent(parent);
            eventSystem.AddComponent<EventSystem>();
            var inputModule = eventSystem.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();

            var timerPanel = CreateUiImage(hudRoot, "Shift Timer", new Vector2(0f, 1f),
                new Vector2(126f, -72f), new Vector2(210f, 92f),
                new Color(0.055f, 0.065f, 0.075f, 0.92f));
            KitchenHudVisuals.ApplyRounded(timerPanel.GetComponent<Image>());
            AddHudShadow(timerPanel);
            KitchenHudVisuals.BuildClockIcon(timerPanel.transform, new Vector2(-69f, 0f),
                KitchenHudVisuals.Cream);
            shiftTimer = CreateText(timerPanel.transform, "Shift Time Value", new Vector2(0.5f, 0.5f),
                new Vector2(27f, 0f), new Vector2(112f, 60f), 40, TextAnchor.MiddleCenter);
            shiftTimer.color = Color.white;
            shiftTimer.fontStyle = FontStyle.Bold;
            clockPausedIcon = KitchenHudVisuals.BuildPauseBadge(timerPanel.transform, new Vector2(92f, 34f));

            var scorePanel = CreateUiImage(hudRoot, "Score", new Vector2(1f, 1f),
                new Vector2(-126f, -72f), new Vector2(210f, 92f),
                new Color(0.055f, 0.065f, 0.075f, 0.92f));
            KitchenHudVisuals.ApplyRounded(scorePanel.GetComponent<Image>());
            AddHudShadow(scorePanel);
            KitchenHudVisuals.BuildScoreIcon(scorePanel.transform, new Vector2(-68f, 0f));
            score = CreateText(scorePanel.transform, "Score Value", new Vector2(0.5f, 0.5f),
                new Vector2(28f, 0f), new Vector2(116f, 60f), 40, TextAnchor.MiddleCenter);
            score.color = Color.white;
            score.fontStyle = FontStyle.Bold;

            var orderFrame = CreateUiImage(hudRoot, "Order Ticket", new Vector2(0.5f, 1f),
                new Vector2(336f, -141f), new Vector2(270f, 330f),
                new Color(0.96f, 0.97f, 0.94f, 0.98f));
            var orderRect = orderFrame.GetComponent<RectTransform>();
            orderRect.anchorMin = new Vector2(0f, 1f);
            orderRect.anchorMax = new Vector2(0f, 1f);
            orderRect.localScale = Vector3.one * 0.70f;
            var orderFrameImage = orderFrame.GetComponent<Image>();
            KitchenHudVisuals.ApplyRounded(orderFrameImage);
            orderFrameImage.raycastTarget = false;
            var orderOutline = orderFrame.AddComponent<UnityEngine.UI.Outline>();
            orderOutline.effectColor = new Color(0.03f, 0.025f, 0.03f, 0.88f);
            orderOutline.effectDistance = new Vector2(2f, -2f);
            orderOutline.useGraphicAlpha = true;
            var orderShadow = orderFrame.AddComponent<Shadow>();
            orderShadow.effectColor = new Color(0.06f, 0.03f, 0.07f, 0.50f);
            orderShadow.effectDistance = new Vector2(0f, -8f);
            orderShadow.useGraphicAlpha = true;

            var imageBackdrop = CreateUiImage(orderFrame.transform, "Order Food Frame", new Vector2(0.5f, 0.5f),
                new Vector2(0f, 64f), new Vector2(130f, 130f),
                new Color(0.92f, 0.91f, 0.89f, 1f));
            KitchenHudVisuals.ApplyRounded(imageBackdrop.GetComponent<Image>());
            var orderImageObject = new GameObject(
                "Order Food Image",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage));
            orderImageObject.transform.SetParent(imageBackdrop.transform, false);
            var orderImageRect = orderImageObject.GetComponent<RectTransform>();
            orderImageRect.anchorMin = new Vector2(0.5f, 0.5f);
            orderImageRect.anchorMax = new Vector2(0.5f, 0.5f);
            orderImageRect.pivot = new Vector2(0.5f, 0.5f);
            orderImageRect.anchoredPosition = Vector2.zero;
            orderImageRect.sizeDelta = new Vector2(
                OrderTicketView.CompletedFoodPhotoSize,
                OrderTicketView.CompletedFoodPhotoSize);
            var orderFoodImage = orderImageObject.GetComponent<RawImage>();
            orderFoodImage.color = Color.white;
            orderFoodImage.raycastTarget = false;

            orderTicket = orderFrame.AddComponent<OrderTicketView>();
            orderTicket.Initialize(orderFrameImage, orderFoodImage, 0);

            const float ticketSpacing = 197f;
            for (var orderIndex = 1; orderIndex < 5; orderIndex++)
            {
                var suffix = " " + (orderIndex + 1);
                var queuedFrame = CreateUiImage(hudRoot, "Order Ticket" + suffix, new Vector2(0.5f, 1f),
                    new Vector2(336f + ticketSpacing * orderIndex, -141f), new Vector2(270f, 330f),
                    new Color(0.96f, 0.97f, 0.94f, 0.98f));
                var queuedRect = queuedFrame.GetComponent<RectTransform>();
                queuedRect.anchorMin = new Vector2(0f, 1f);
                queuedRect.anchorMax = new Vector2(0f, 1f);
                queuedRect.localScale = Vector3.one * 0.70f;
                var queuedImage = queuedFrame.GetComponent<Image>();
                KitchenHudVisuals.ApplyRounded(queuedImage);
                queuedImage.raycastTarget = false;
                var queuedOutline = queuedFrame.AddComponent<UnityEngine.UI.Outline>();
                queuedOutline.effectColor = new Color(0.03f, 0.025f, 0.03f, 0.88f);
                queuedOutline.effectDistance = new Vector2(2f, -2f);
                queuedOutline.useGraphicAlpha = true;
                var queuedShadow = queuedFrame.AddComponent<Shadow>();
                queuedShadow.effectColor = new Color(0.06f, 0.03f, 0.07f, 0.50f);
                queuedShadow.effectDistance = new Vector2(0f, -8f);
                queuedShadow.useGraphicAlpha = true;

                var queuedBackdrop = CreateUiImage(queuedFrame.transform, "Order Food Frame" + suffix,
                    new Vector2(0.5f, 0.5f), new Vector2(0f, 64f), new Vector2(130f, 130f),
                    new Color(0.92f, 0.91f, 0.89f, 1f));
                KitchenHudVisuals.ApplyRounded(queuedBackdrop.GetComponent<Image>());
                var queuedFoodObject = new GameObject(
                    "Order Food Image" + suffix,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(RawImage));
                queuedFoodObject.transform.SetParent(queuedBackdrop.transform, false);
                var queuedFoodRect = queuedFoodObject.GetComponent<RectTransform>();
                queuedFoodRect.anchorMin = new Vector2(0.5f, 0.5f);
                queuedFoodRect.anchorMax = new Vector2(0.5f, 0.5f);
                queuedFoodRect.pivot = new Vector2(0.5f, 0.5f);
                queuedFoodRect.anchoredPosition = Vector2.zero;
                queuedFoodRect.sizeDelta = Vector2.one * OrderTicketView.CompletedFoodPhotoSize;
                var queuedFoodImage = queuedFoodObject.GetComponent<RawImage>();
                queuedFoodImage.color = Color.white;
                queuedFoodImage.raycastTarget = false;

                var queuedTicket = queuedFrame.AddComponent<OrderTicketView>();
                queuedTicket.Initialize(queuedImage, queuedFoodImage, orderIndex);
                queuedFrame.SetActive(false);
            }

            guide = null;

            var kitchenControls = new GameObject("Kitchen Controls", typeof(RectTransform));
            kitchenControls.transform.SetParent(hudRoot, false);
            StretchToParent(kitchenControls.GetComponent<RectTransform>());
            BuildJoystick(kitchenControls.transform, input);
            const float actionButtonSize = 172f;
            var actionSize = new Vector2(actionButtonSize, actionButtonSize);
            var actionCluster = CreateUiImage(kitchenControls.transform, "Action Cluster Backdrop",
                new Vector2(1f, 0f), new Vector2(-207f, 207f), new Vector2(388f, 388f),
                new Color(0.10f, 0.13f, 0.15f, 0.28f));
            KitchenHudVisuals.ApplyRounded(actionCluster.GetComponent<Image>());
            actionCluster.GetComponent<Image>().raycastTarget = false;
            CreateButton(kitchenControls.transform, "PICK / PLACE", new Vector2(1f, 0f),
                new Vector2(-114f, 114f), actionSize, KitchenHudVisuals.PickPlace, input.TouchPickupPlace,
                KitchenActionIcon.PickPlace);
            CreateHoldWorkButton(kitchenControls.transform, input, new Vector2(-300f, 114f), actionSize);
            var throwButton = CreateButton(kitchenControls.transform, "THROW", new Vector2(1f, 0f),
                new Vector2(-300f, 300f), actionSize, KitchenHudVisuals.Throw, input.TouchThrow,
                KitchenActionIcon.Throw);
            CreateButton(kitchenControls.transform, "DASH", new Vector2(1f, 0f),
                new Vector2(-114f, 300f), actionSize, KitchenHudVisuals.Dash, input.TouchDash,
                KitchenActionIcon.Dash);
            kitchenControls.AddComponent<KitchenActionAvailability>().Initialize(
                Object.FindFirstObjectByType<PlayerController>(),
                throwButton.GetComponent<Button>());

            var deliveryControls = new GameObject("Delivery Controls", typeof(RectTransform));
            deliveryControls.transform.SetParent(hudRoot, false);
            StretchToParent(deliveryControls.GetComponent<RectTransform>());
            CreateHoldBikeButton(deliveryControls.transform, input, BikeControl.Accelerator, "ACCEL", new Vector2(0f, 0f),
                new Vector2(165f, 255f), new Color(0.16f, 0.68f, 0.42f));
            CreateHoldBikeButton(deliveryControls.transform, input, BikeControl.Reverse, "REVERSE", new Vector2(0f, 0f),
                new Vector2(165f, 90f), new Color(0.82f, 0.3f, 0.22f));
            CreateHoldBikeButton(deliveryControls.transform, input, BikeControl.Left, "LEFT", new Vector2(1f, 0f),
                new Vector2(-300f, 120f), new Color(0.2f, 0.55f, 0.82f));
            CreateHoldBikeButton(deliveryControls.transform, input, BikeControl.Right, "RIGHT", new Vector2(1f, 0f),
                new Vector2(-90f, 120f), new Color(0.2f, 0.55f, 0.82f));
            CreateButton(deliveryControls.transform, "DISMOUNT", new Vector2(1f, 1f), new Vector2(-145f, -315f),
                new Vector2(230f, 92f), new Color(0.44f, 0.48f, 0.54f), input.TouchPickupPlace,
                KitchenActionIcon.Dismount);

            var speedText = CreateText(deliveryControls.transform, "Bike Speed", new Vector2(0.5f, 1f),
                new Vector2(0f, -285f), new Vector2(420f, 60f), 32, TextAnchor.MiddleCenter);
            speedText.color = Color.white;
            var routePanel = CreateUiImage(deliveryControls.transform, "Mini Route Panel", new Vector2(1f, 1f),
                new Vector2(-210f, -205f), new Vector2(340f, 250f), new Color(0.04f, 0.08f, 0.12f, 0.88f));
            var navigationText = CreateText(routePanel.transform, "Navigation", new Vector2(0.5f, 1f),
                new Vector2(0f, -48f), new Vector2(310f, 82f), 27, TextAnchor.MiddleCenter);
            navigationText.color = new Color(0.35f, 0.92f, 0.88f);
            CreateUiImage(routePanel.transform, "Route Line", new Vector2(0.5f, 0.5f),
                new Vector2(0f, -12f), new Vector2(18f, 132f), new Color(0.32f, 0.38f, 0.43f));
            CreateUiImage(routePanel.transform, "Kitchen Map Marker", new Vector2(0.5f, 0.5f),
                new Vector2(0f, -70f), new Vector2(28f, 28f), new Color(1f, 0.72f, 0.12f));
            var targetMapMarker = CreateUiImage(routePanel.transform, "Target Map Marker", new Vector2(0.5f, 0.5f),
                new Vector2(0f, 48f), new Vector2(34f, 34f), new Color(0.2f, 0.9f, 0.4f));
            var bikeMapMarker = CreateUiImage(routePanel.transform, "Bike Map Marker", new Vector2(0.5f, 0.5f),
                new Vector2(0f, -70f), new Vector2(20f, 28f), new Color(0.25f, 0.82f, 1f));
            deliveryControls.SetActive(false);

            var bike = Object.FindFirstObjectByType<DeliveryBikeController>();
            if (bike != null)
            {
                parent.gameObject.AddComponent<DeliveryHudController>().Initialize(
                    input,
                    bike,
                    kitchenControls,
                    deliveryControls,
                    speedText,
                    navigationText,
                    bikeMapMarker.GetComponent<RectTransform>(),
                    targetMapMarker.GetComponent<RectTransform>());
            }

            resultPanel = CreatePanel(hudRoot, "Result Panel", new Color(0.04f, 0.06f, 0.09f, 0.95f));
            resultText = CreateText(resultPanel.transform, "Result Text", new Vector2(0.5f, 0.58f),
                Vector2.zero, new Vector2(900f, 460f), 52, TextAnchor.MiddleCenter);
            var restart = CreateButton(resultPanel.transform, "RESTART", new Vector2(0.5f, 0.18f),
                new Vector2(-190f, 0f), new Vector2(320f, 105f), new Color(0.18f, 0.65f, 0.42f), game.Restart,
                KitchenActionIcon.None);
            restart.GetComponentInChildren<Text>().text = "もう一度";
            restart.GetComponentInChildren<Text>().fontSize = 30;
            var home = CreateButton(resultPanel.transform, "HOME", new Vector2(0.5f, 0.18f),
                new Vector2(190f, 0f), new Vector2(320f, 105f), new Color(0.53f, 0.20f, 0.62f),
                game.ReturnHome, KitchenActionIcon.None);
            home.GetComponentInChildren<Text>().text = "ホーム";
            home.GetComponentInChildren<Text>().fontSize = 30;
            resultPanel.SetActive(false);
        }

        private static void BuildJoystick(Transform canvas, KitchenInputRouter input)
        {
            var joystickBase = CreateUiImage(canvas, "Move Stick", new Vector2(0f, 0f),
                new Vector2(178f, 178f), new Vector2(276f, 276f),
                new Color(0.38f, 0.43f, 0.45f, 0.46f));
            KitchenHudVisuals.ApplyCircle(joystickBase.GetComponent<Image>());
            var knob = CreateUiImage(joystickBase.transform, "Knob", new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(124f, 124f), new Color(0.88f, 0.91f, 0.91f, 0.92f));
            KitchenHudVisuals.ApplyCircle(knob.GetComponent<Image>());
            knob.GetComponent<Image>().raycastTarget = false;
            joystickBase.AddComponent<VirtualJoystick>().Initialize(
                input,
                joystickBase.GetComponent<RectTransform>(),
                knob.GetComponent<RectTransform>());
        }

        private static GameObject CreateButton(
            Transform parent,
            string label,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            Color color,
            UnityEngine.Events.UnityAction action,
            KitchenActionIcon icon = KitchenActionIcon.None)
        {
            var buttonObject = CreateStyledButtonShell(
                parent, label, anchor, position, size, color, icon, out var button);
            button.onClick.AddListener(action);
            return buttonObject;
        }

        private static GameObject CreateHoldWorkButton(
            Transform parent,
            KitchenInputRouter input,
            Vector2 position,
            Vector2 size)
        {
            var buttonObject = CreateStyledButtonShell(
                parent, "WORK", new Vector2(1f, 0f), position, size,
                KitchenHudVisuals.Work, KitchenActionIcon.Work, out _);
            buttonObject.AddComponent<HoldWorkButton>().Initialize(input);
            return buttonObject;
        }

        private static GameObject CreateHoldBikeButton(
            Transform parent,
            KitchenInputRouter input,
            BikeControl control,
            string label,
            Vector2 anchor,
            Vector2 position,
            Color color)
        {
            var size = new Vector2(190f, 140f);
            var icon = control switch
            {
                BikeControl.Accelerator => KitchenActionIcon.Accelerator,
                BikeControl.Reverse => KitchenActionIcon.Reverse,
                BikeControl.Left => KitchenActionIcon.Left,
                _ => KitchenActionIcon.Right
            };
            var buttonObject = CreateStyledButtonShell(
                parent, label, anchor, position, size, color, icon, out _);
            buttonObject.AddComponent<HoldBikeControlButton>().Initialize(input, control);
            return buttonObject;
        }

        private static GameObject CreateStyledButtonShell(
            Transform parent,
            string label,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            Color faceColor,
            KitchenActionIcon icon,
            out Button button)
        {
            var buttonObject = CreateUiImage(
                parent,
                label + " Button",
                anchor,
                position,
                size,
                new Color(0.045f, 0.06f, 0.075f, 0.97f));
            var rootImage = buttonObject.GetComponent<Image>();
            KitchenHudVisuals.ApplyRounded(rootImage);
            buttonObject.AddComponent<CanvasGroup>();
            var shadow = buttonObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.66f);
            shadow.effectDistance = new Vector2(0f, -7f);
            shadow.useGraphicAlpha = true;
            var outline = buttonObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(0.64f, 0.72f, 0.74f, 0.78f);
            outline.effectDistance = new Vector2(2f, 2f);
            outline.useGraphicAlpha = true;

            var face = CreateUiImage(
                buttonObject.transform,
                "Face",
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 2f),
                new Vector2(Mathf.Max(1f, size.x - 16f), Mathf.Max(1f, size.y - 16f)),
                Color.Lerp(new Color(0.13f, 0.16f, 0.18f, 1f), faceColor, 0.20f));
            var faceImage = face.GetComponent<Image>();
            KitchenHudVisuals.ApplyRounded(faceImage);
            faceImage.raycastTarget = false;

            var accent = CreateUiImage(
                face.transform,
                "Action Color Tab",
                new Vector2(0.5f, 0f),
                new Vector2(0f, 11f),
                new Vector2(size.x * 0.48f, 8f),
                faceColor);
            KitchenHudVisuals.ApplyRounded(accent.GetComponent<Image>());
            accent.GetComponent<Image>().raycastTarget = false;

            var content = new GameObject("Button Content", typeof(RectTransform));
            content.transform.SetParent(face.transform, false);
            StretchToParent(content.GetComponent<RectTransform>());
            var iconScale = Mathf.Clamp(Mathf.Min(size.x, size.y) / 205f, 0.50f, 1f);
            KitchenHudVisuals.BuildIcon(content.transform, icon, KitchenHudVisuals.Cream, iconScale);

            if (icon == KitchenActionIcon.None)
            {
                var text = CreateText(
                    content.transform,
                    "Label",
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(size.x - 20f, Mathf.Max(34f, size.y * 0.24f)),
                    28,
                    TextAnchor.MiddleCenter);
                text.text = label;
                text.color = Color.white;
                var textShadow = text.gameObject.AddComponent<Shadow>();
                textShadow.effectColor = new Color(0.08f, 0.02f, 0.09f, 0.8f);
                textShadow.effectDistance = new Vector2(1.5f, -2f);
            }

            button = buttonObject.AddComponent<Button>();
            button.targetGraphic = faceImage;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(1f, 1f, 0.92f, 1f),
                pressedColor = new Color(0.76f, 0.76f, 0.76f, 1f),
                selectedColor = Color.white,
                disabledColor = new Color(0.40f, 0.40f, 0.40f, 0.58f),
                colorMultiplier = 1f,
                fadeDuration = 0.06f
            };
            buttonObject.AddComponent<KitchenActionButtonView>().Initialize(label, icon);
            return buttonObject;
        }

        private static void AddHudShadow(GameObject target)
        {
            var shadow = target.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.54f);
            shadow.effectDistance = new Vector2(0f, -6f);
            shadow.useGraphicAlpha = true;
        }

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var panel = CreateUiImage(parent, name, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(1080f, 720f), color);
            return panel;
        }

        private static GameObject CreateUiImage(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            gameObject.GetComponent<Image>().color = color;
            return gameObject;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            int fontSize,
            TextAnchor alignment)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = gameObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreatePrimitive(
            string name,
            PrimitiveType primitive,
            Transform parent,
            Vector3 localPosition,
            Vector3 scale,
            Color color)
        {
            var gameObject = GameObject.CreatePrimitive(primitive);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localRotation = Quaternion.identity;
            gameObject.transform.localScale = scale;
            gameObject.GetComponent<Renderer>().material = GrayboxMaterials.Create(color);
            return gameObject;
        }
    }

}
