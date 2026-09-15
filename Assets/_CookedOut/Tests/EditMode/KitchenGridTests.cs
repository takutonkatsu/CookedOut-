using System.Collections.Generic;
using System.Linq;
using CookedOut.Domain;
using NUnit.Framework;

namespace CookedOut.Tests.EditMode
{
    public sealed class KitchenGridTests
    {
        [TestCase(GridDirection.North)]
        [TestCase(GridDirection.East)]
        [TestCase(GridDirection.South)]
        [TestCase(GridDirection.West)]
        public void CellAndWorldCoordinatesRoundTrip(GridDirection direction)
        {
            var transform = new GridSpaceTransform(17.25f, -4.5f, 1.6f, direction);
            var expected = new GridCoordinate(3, 2);

            var world = transform.CellCenter(expected);
            var actual = transform.WorldToCell(world.X, world.Z);

            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        public void OverlappingFootprintsAreRejected(int width, int depth)
        {
            var footprint = new GridFootprint(width, depth);
            var placements = new[]
            {
                Placement("first", new GridCoordinate(1, 1), footprint),
                Placement("second", new GridCoordinate(1, 1), footprint)
            };

            var report = CreateOpenGrid(5, 5, placements).Validate();

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Errors.Any(error => error.StartsWith("PLACEMENT_OVERLAP:")), Is.True);
        }

        [Test]
        public void PlacementOutsideMapIsRejected()
        {
            var placements = new[]
            {
                Placement("wide", new GridCoordinate(3, 3), new GridFootprint(2, 2))
            };

            var report = CreateOpenGrid(4, 4, placements).Validate();

            Assert.That(report.Errors.Any(error => error.StartsWith("PLACEMENT_OUT_OF_BOUNDS:")), Is.True);
        }

        [Test]
        public void UnreachableRequiredStationIsRejected()
        {
            const int width = 5;
            const int depth = 5;
            var terrain = Enumerable.Repeat(GridTerrain.Floor, width * depth).ToArray();
            for (var x = 0; x < width; x++)
            {
                terrain[2 * width + x] = GridTerrain.Wall;
            }

            var grid = new KitchenGridDefinition(
                "blocked",
                width,
                depth,
                new GridSpaceTransform(0f, 0f, 1f),
                terrain,
                new[] { Placement("required", new GridCoordinate(2, 4), new GridFootprint(1, 1), true) },
                new GridCoordinate(2, 0));

            var report = grid.Validate();

            Assert.That(report.Errors, Does.Contain("REQUIRED_PLACEMENT_UNREACHABLE:required"));
        }

        [Test]
        public void MovingIslandKeepsItsLocalCellCoordinate()
        {
            var local = new GridCoordinate(2, 1);
            var before = new GridSpaceTransform(0f, 0f, 1.5f, GridDirection.North);
            var after = new GridSpaceTransform(22f, -9f, 1.5f, GridDirection.East);

            var beforeWorld = before.CellCenter(local);
            var afterWorld = after.CellCenter(local);

            Assert.That(before.WorldToCell(beforeWorld.X, beforeWorld.Z), Is.EqualTo(local));
            Assert.That(after.WorldToCell(afterWorld.X, afterWorld.Z), Is.EqualTo(local));
            Assert.That(afterWorld.X, Is.Not.EqualTo(beforeWorld.X));
        }

        [Test]
        public void TutorialLayoutIsValidAndDeterministic()
        {
            var first = TutorialKitchenGrid.Create();
            var second = TutorialKitchenGrid.Create();

            Assert.That(first.Validate().IsValid, Is.True, string.Join("\n", first.Validate().Errors));
            Assert.That(second.DeterministicSignature(), Is.EqualTo(first.DeterministicSignature()));
            Assert.That(first.Width, Is.EqualTo(10));
            Assert.That(first.Depth, Is.EqualTo(8));
            Assert.That(first.PlayerSpawn, Is.EqualTo(new GridCoordinate(7, 4)));
            Assert.That(first.Placements.Count, Is.EqualTo(19));
            var source = first.Placements.Single(item => item.ArchetypeId == KitchenArchetypeIds.IngredientCrate);
            Assert.That(source.ContentId, Is.EqualTo(GameIds.LettuceIngredient));
        }

        [Test]
        public void IngredientSourceWithoutContentIdIsRejected()
        {
            var source = new GridPlacement(
                "source",
                KitchenArchetypeIds.IngredientCrate,
                new GridCoordinate(1, 1),
                new GridFootprint(1, 1),
                GridDirection.North,
                true);

            var report = CreateOpenGrid(4, 4, new[] { source }).Validate();

            Assert.That(report.Errors, Does.Contain("INGREDIENT_SOURCE_CONTENT_MISSING:source"));
        }

        [Test]
        public void SoupTutorialLayoutIsValidAndContainsTwoSourcesAndPotHeat()
        {
            var grid = TutorialSoupKitchenGrid.Create();
            var report = grid.Validate();

            Assert.That(report.IsValid, Is.True, string.Join("\n", report.Errors));
            Assert.That(grid.StageId, Is.EqualTo(GameIds.TutorialSoupStage));
            Assert.That(grid.Placements.Count(item => item.ArchetypeId == KitchenArchetypeIds.IngredientCrate),
                Is.EqualTo(2));
            Assert.That(grid.Placements.Single(item => item.ArchetypeId == KitchenArchetypeIds.PotHeatSource).Id,
                Is.EqualTo("pot.heat"));
        }

        [Test]
        public void ThrowDeliveryTutorialLayoutIsValidAndContainsThrowWallBikeAndDestination()
        {
            var first = TutorialThrowDeliveryKitchenGrid.Create();
            var second = TutorialThrowDeliveryKitchenGrid.Create();
            var report = first.Validate();

            Assert.That(report.IsValid, Is.True, string.Join("\n", report.Errors));
            Assert.That(first.DeterministicSignature(), Is.EqualTo(second.DeterministicSignature()));
            Assert.That(first.StageId, Is.EqualTo(GameIds.TutorialThrowDeliveryStage));
            Assert.That(first.Width, Is.EqualTo(12));
            Assert.That(first.Depth, Is.EqualTo(9));
            Assert.That(first.Placements.Count(item => item.Id.StartsWith("throw.wall.")), Is.EqualTo(6));
            Assert.That(first.Placements.Count(item => item.ArchetypeId == KitchenArchetypeIds.IngredientCrate),
                Is.EqualTo(2));
            Assert.That(first.Placements.Single(item => item.ArchetypeId == KitchenArchetypeIds.BikeDock).RequiredInteraction,
                Is.True);
            Assert.That(first.Placements.Single(item => item.ArchetypeId == KitchenArchetypeIds.BikeDock).Anchor.Z,
                Is.EqualTo(first.Depth - 1), "The bike must sit at the kitchen/street boundary.");
            Assert.That(first.Placements.Single(item => item.ArchetypeId == KitchenArchetypeIds.DeliveryPoint).Id,
                Is.EqualTo("delivery.point"));
            Assert.That(first.Placements.Any(item => item.ArchetypeId == KitchenArchetypeIds.RecoveryBin), Is.False,
                "Out-of-bounds items return to the kitchen edge without a dedicated recovery workbench.");
        }

        [Test]
        public void DashTutorialLayoutIsValidAndRequiresCookingAcrossUnavoidableConveyor()
        {
            var grid = TutorialDashKitchenGrid.Create();
            var report = grid.Validate();

            Assert.That(report.IsValid, Is.True, string.Join("\n", report.Errors));
            Assert.That(grid.StageId, Is.EqualTo(GameIds.TutorialDashStage));
            Assert.That(grid.Width, Is.EqualTo(12));
            Assert.That(grid.Depth, Is.EqualTo(5));
            Assert.That(grid.TerrainAt(new GridCoordinate(4, 1)), Is.EqualTo(GridTerrain.Wall));
            Assert.That(grid.TerrainAt(new GridCoordinate(4, 2)), Is.EqualTo(GridTerrain.Floor));
            Assert.That(grid.TerrainAt(new GridCoordinate(4, 3)), Is.EqualTo(GridTerrain.Wall));
            Assert.That(grid.TerrainAt(new GridCoordinate(5, 1)), Is.EqualTo(GridTerrain.Wall));
            Assert.That(grid.TerrainAt(new GridCoordinate(5, 2)), Is.EqualTo(GridTerrain.Floor));
            Assert.That(grid.TerrainAt(new GridCoordinate(5, 3)), Is.EqualTo(GridTerrain.Wall));
            Assert.That(grid.Placements.Count(item => item.ArchetypeId == KitchenArchetypeIds.IngredientCrate),
                Is.EqualTo(1));
            Assert.That(grid.Placements.Count(item => item.ArchetypeId == KitchenArchetypeIds.ChoppingBoard),
                Is.EqualTo(1));
            Assert.That(grid.Placements.Count(item => item.ArchetypeId == KitchenArchetypeIds.ContainerDispenser),
                Is.EqualTo(1));
            Assert.That(grid.Placements.Count(item => item.ArchetypeId == KitchenArchetypeIds.ServingHatch),
                Is.EqualTo(1));
            Assert.That(grid.IsWalkable(grid.PlayerSpawn), Is.True);
        }

        [Test]
        public void FryingTutorialLayoutIsValidAndContainsRequiredCookingStations()
        {
            var grid = TutorialFryingKitchenGrid.Create();
            var report = grid.Validate();

            Assert.That(report.IsValid, Is.True, string.Join("\n", report.Errors));
            Assert.That(grid.StageId, Is.EqualTo(GameIds.TutorialFryingStage));
            var ingredientSources = grid.Placements
                .Where(item => item.ArchetypeId == KitchenArchetypeIds.IngredientCrate)
                .Select(item => item.ContentId)
                .ToArray();
            Assert.That(ingredientSources, Is.EquivalentTo(new[]
            {
                GameIds.BeefIngredient,
                GameIds.LettuceIngredient
            }));
            Assert.That(grid.Placements.Count(item => item.ArchetypeId == KitchenArchetypeIds.ChoppingBoard),
                Is.EqualTo(1));
            Assert.That(grid.Placements.Count(item => item.ArchetypeId == KitchenArchetypeIds.PanHeatSource),
                Is.EqualTo(1));
            Assert.That(grid.Placements.Count(item => item.ArchetypeId == KitchenArchetypeIds.AssemblyCounter),
                Is.EqualTo(1));
            Assert.That(grid.Placements.Count(item => item.ArchetypeId == KitchenArchetypeIds.ServingHatch),
                Is.EqualTo(1));
        }

        [TestCase(GameIds.TutorialFireRecoveryStage)]
        [TestCase(GameIds.TutorialDishwashingStage)]
        [TestCase(GameIds.TutorialCombinedDeliveryStage)]
        public void LateTutorialLayoutsAreValidAndDeterministic(string stageId)
        {
            var first = stageId == GameIds.TutorialFireRecoveryStage
                ? TutorialFireRecoveryKitchenGrid.Create()
                : stageId == GameIds.TutorialDishwashingStage
                    ? TutorialDishwashingKitchenGrid.Create()
                    : TutorialCombinedDeliveryKitchenGrid.Create();
            var second = stageId == GameIds.TutorialFireRecoveryStage
                ? TutorialFireRecoveryKitchenGrid.Create()
                : stageId == GameIds.TutorialDishwashingStage
                    ? TutorialDishwashingKitchenGrid.Create()
                    : TutorialCombinedDeliveryKitchenGrid.Create();

            Assert.That(first.StageId, Is.EqualTo(stageId));
            Assert.That(first.Validate().IsValid, Is.True, string.Join("\n", first.Validate().Errors));
            Assert.That(second.DeterministicSignature(), Is.EqualTo(first.DeterministicSignature()));
        }

        [Test]
        public void DishwashingAndCombinedLayoutsContainTheirRequiredTeachingStations()
        {
            var dishes = TutorialDishwashingKitchenGrid.Create();
            Assert.That(dishes.Placements.Any(item => item.ArchetypeId == KitchenArchetypeIds.PlateDispenser), Is.True);
            Assert.That(dishes.Placements.Any(item => item.ArchetypeId == KitchenArchetypeIds.DishReturn), Is.True);
            Assert.That(dishes.Placements.Any(item => item.ArchetypeId == KitchenArchetypeIds.WashingSink), Is.True);

            var combined = TutorialCombinedDeliveryKitchenGrid.Create();
            Assert.That(combined.Placements.Any(item => item.ArchetypeId == KitchenArchetypeIds.CourierShelf), Is.True);
            Assert.That(combined.Placements.Any(item => item.ArchetypeId == KitchenArchetypeIds.BikeDock), Is.True);
            Assert.That(combined.Placements.Count(item => item.ArchetypeId == KitchenArchetypeIds.IngredientCrate), Is.EqualTo(4));
        }

        [Test]
        public void HomeKitchenV1IsFixedTwelveByNineAndShopRankNeverChangesItsSockets()
        {
            var rankOne = HomeKitchenV1.CreateForShopRank(1);
            var rankTen = HomeKitchenV1.CreateForShopRank(10);
            var grid = rankOne.Grid;
            var report = grid.Validate();

            Assert.That(report.IsValid, Is.True, string.Join("\n", report.Errors));
            Assert.That(grid.StageId, Is.EqualTo(GameIds.HomeKitchenV1));
            Assert.That(grid.Width, Is.EqualTo(12));
            Assert.That(grid.Depth, Is.EqualTo(9));
            Assert.That(rankTen.Grid.DeterministicSignature(), Is.EqualTo(grid.DeterministicSignature()));
            Assert.That(rankTen.Sockets.Select(SocketSignature),
                Is.EqualTo(rankOne.Sockets.Select(SocketSignature)));

            Assert.That(rankOne.Sockets.Count(socket => socket.Kind == HomeKitchenSocketKind.GenericEquipment),
                Is.EqualTo(18));
            Assert.That(rankOne.Sockets.Count(socket => socket.Kind == HomeKitchenSocketKind.AssemblyPassSurface),
                Is.EqualTo(8));
            Assert.That(rankOne.Sockets.Count(socket => socket.Kind == HomeKitchenSocketKind.IngredientSource),
                Is.EqualTo(6));
            Assert.That(rankOne.Sockets.Count(socket => socket.Kind == HomeKitchenSocketKind.ContainerSource),
                Is.EqualTo(1));
            Assert.That(rankOne.Sockets.Count(socket => socket.Kind == HomeKitchenSocketKind.DeliveryAgency),
                Is.EqualTo(2));
            Assert.That(rankOne.Sockets.Count(socket => socket.Kind == HomeKitchenSocketKind.LoadingDock),
                Is.EqualTo(1));
            Assert.That(rankOne.Sockets.Count(socket => socket.Kind == HomeKitchenSocketKind.Exit),
                Is.EqualTo(1));
            Assert.That(rankOne.Sockets.All(rankOne.IsSocketReachable), Is.True,
                "Every fixed socket and the exit must be reachable from the home spawn.");

            var genericPlacementIds = rankOne.Sockets
                .Where(socket => socket.Kind == HomeKitchenSocketKind.GenericEquipment)
                .Select(socket => socket.PlacementId)
                .ToHashSet();
            Assert.That(grid.Placements
                .Where(placement => genericPlacementIds.Contains(placement.Id))
                .All(placement => placement.ArchetypeId == KitchenArchetypeIds.Counter), Is.True,
                "Unused generic sockets must behave as ordinary counters.");
            Assert.That(grid.Placements.Any(placement =>
                placement.ArchetypeId == KitchenArchetypeIds.PotHeatSource ||
                placement.ArchetypeId == KitchenArchetypeIds.PanHeatSource), Is.False,
                "Player-owned cooking units belong to presets, not the fixed home layout.");
            Assert.That(KitchenArchetypeIds.CookingPotUnit, Is.EqualTo(KitchenArchetypeIds.PotHeatSource));
            Assert.That(KitchenArchetypeIds.FryingPanUnit, Is.EqualTo(KitchenArchetypeIds.PanHeatSource));
            Assert.That(KitchenArchetypeIds.AssemblyPassCounter,
                Is.EqualTo(KitchenArchetypeIds.AssemblyCounter));

            Assert.That(TutorialKitchenGrid.Create().Width, Is.EqualTo(10));
            Assert.That(TutorialSoupKitchenGrid.Create().Width, Is.EqualTo(10));
            Assert.That(TutorialThrowDeliveryKitchenGrid.Create().Width, Is.EqualTo(12));
            Assert.That(TutorialFryingKitchenGrid.Create().Width, Is.EqualTo(10));
        }

        [Test]
        public void ThrowPathCanCrossCounterButNotWallOrMapEdge()
        {
            var grid = TutorialKitchenGrid.Create();
            var start = grid.Space.CellCenter(new GridCoordinate(4, 3));
            var beyondIsland = grid.Space.CellCenter(new GridCoordinate(4, 6));
            var outside = grid.Space.CellCenter(new GridCoordinate(4, 8));
            var southFloor = grid.Space.CellCenter(new GridCoordinate(4, 1));
            var southWall = grid.Space.CellCenter(new GridCoordinate(4, 0));

            Assert.That(
                grid.IsThrowPathClear(start.X, start.Z, beyondIsland.X, beyondIsland.Z, out var clearReason),
                Is.True,
                clearReason);
            Assert.That(
                grid.IsThrowPathClear(start.X, start.Z, outside.X, outside.Z, out var blockedReason),
                Is.False);
            Assert.That(blockedReason, Does.StartWith("THROW_PATH_OUT_OF_BOUNDS:"));
            Assert.That(
                grid.IsThrowPathClear(southFloor.X, southFloor.Z, southWall.X, southWall.Z, out var wallReason),
                Is.False);
            Assert.That(wallReason, Does.StartWith("THROW_PATH_BLOCKED:"));
        }

        private static GridPlacement Placement(
            string id,
            GridCoordinate anchor,
            GridFootprint footprint,
            bool required = false)
        {
            return new GridPlacement(id, KitchenArchetypeIds.Counter, anchor, footprint, GridDirection.North, required);
        }

        private static string SocketSignature(HomeKitchenSocketDefinition socket)
        {
            return $"{socket.Id}|{socket.Kind}|{socket.Coordinate}|{socket.PlacementId}";
        }

        private static KitchenGridDefinition CreateOpenGrid(
            int width,
            int depth,
            IEnumerable<GridPlacement> placements)
        {
            return new KitchenGridDefinition(
                "test",
                width,
                depth,
                new GridSpaceTransform(0f, 0f, 1f),
                Enumerable.Repeat(GridTerrain.Floor, width * depth),
                placements,
                new GridCoordinate(0, 0));
        }
    }
}
