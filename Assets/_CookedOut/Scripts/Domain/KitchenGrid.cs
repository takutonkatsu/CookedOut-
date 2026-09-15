using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CookedOut.Domain
{
    public enum GridDirection
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    public enum GridTerrain
    {
        Void = 0,
        Floor = 1,
        Wall = 2
    }

    [Serializable]
    public readonly struct GridCoordinate : IEquatable<GridCoordinate>
    {
        public GridCoordinate(int x, int z)
        {
            X = x;
            Z = z;
        }

        public int X { get; }
        public int Z { get; }

        public int ManhattanDistance(GridCoordinate other)
        {
            return Math.Abs(X - other.X) + Math.Abs(Z - other.Z);
        }

        public bool Equals(GridCoordinate other) => X == other.X && Z == other.Z;
        public override bool Equals(object obj) => obj is GridCoordinate other && Equals(other);
        public override int GetHashCode() => unchecked((X * 397) ^ Z);
        public override string ToString() => $"({X},{Z})";
    }

    [Serializable]
    public readonly struct GridFootprint
    {
        public GridFootprint(int width, int depth)
        {
            if (width < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (depth < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(depth));
            }

            Width = width;
            Depth = depth;
        }

        public int Width { get; }
        public int Depth { get; }

        public IEnumerable<GridCoordinate> OccupiedCells(GridCoordinate anchor, GridDirection direction)
        {
            var width = direction == GridDirection.East || direction == GridDirection.West ? Depth : Width;
            var depth = direction == GridDirection.East || direction == GridDirection.West ? Width : Depth;
            for (var z = 0; z < depth; z++)
            {
                for (var x = 0; x < width; x++)
                {
                    yield return new GridCoordinate(anchor.X + x, anchor.Z + z);
                }
            }
        }
    }

    [Serializable]
    public readonly struct GridWorldPoint
    {
        public GridWorldPoint(float x, float z)
        {
            X = x;
            Z = z;
        }

        public float X { get; }
        public float Z { get; }
    }

    [Serializable]
    public sealed class GridSpaceTransform
    {
        public GridSpaceTransform(float originX, float originZ, float cellSize, GridDirection direction = GridDirection.North)
        {
            if (cellSize <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(cellSize));
            }

            OriginX = originX;
            OriginZ = originZ;
            CellSize = cellSize;
            Direction = direction;
        }

        public float OriginX { get; }
        public float OriginZ { get; }
        public float CellSize { get; }
        public GridDirection Direction { get; }

        public GridWorldPoint CellCenter(GridCoordinate cell)
        {
            var localX = (cell.X + 0.5f) * CellSize;
            var localZ = (cell.Z + 0.5f) * CellSize;
            RotateToWorld(localX, localZ, out var rotatedX, out var rotatedZ);
            return new GridWorldPoint(OriginX + rotatedX, OriginZ + rotatedZ);
        }

        public GridCoordinate WorldToCell(float worldX, float worldZ)
        {
            var deltaX = worldX - OriginX;
            var deltaZ = worldZ - OriginZ;
            RotateToLocal(deltaX, deltaZ, out var localX, out var localZ);
            return new GridCoordinate(
                (int)Math.Floor(localX / CellSize),
                (int)Math.Floor(localZ / CellSize));
        }

        private void RotateToWorld(float localX, float localZ, out float worldX, out float worldZ)
        {
            switch (Direction)
            {
                case GridDirection.East:
                    worldX = localZ;
                    worldZ = -localX;
                    break;
                case GridDirection.South:
                    worldX = -localX;
                    worldZ = -localZ;
                    break;
                case GridDirection.West:
                    worldX = -localZ;
                    worldZ = localX;
                    break;
                default:
                    worldX = localX;
                    worldZ = localZ;
                    break;
            }
        }

        private void RotateToLocal(float worldX, float worldZ, out float localX, out float localZ)
        {
            switch (Direction)
            {
                case GridDirection.East:
                    localX = -worldZ;
                    localZ = worldX;
                    break;
                case GridDirection.South:
                    localX = -worldX;
                    localZ = -worldZ;
                    break;
                case GridDirection.West:
                    localX = worldZ;
                    localZ = -worldX;
                    break;
                default:
                    localX = worldX;
                    localZ = worldZ;
                    break;
            }
        }
    }

    [Serializable]
    public sealed class GridPlacement
    {
        public GridPlacement(
            string id,
            string archetypeId,
            GridCoordinate anchor,
            GridFootprint footprint,
            GridDirection direction,
            bool requiredInteraction,
            string contentId = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            ArchetypeId = archetypeId ?? throw new ArgumentNullException(nameof(archetypeId));
            Anchor = anchor;
            Footprint = footprint;
            Direction = direction;
            RequiredInteraction = requiredInteraction;
            ContentId = contentId;
        }

        public string Id { get; }
        public string ArchetypeId { get; }
        public GridCoordinate Anchor { get; }
        public GridFootprint Footprint { get; }
        public GridDirection Direction { get; }
        public bool RequiredInteraction { get; }
        public string ContentId { get; }
        public IEnumerable<GridCoordinate> OccupiedCells => Footprint.OccupiedCells(Anchor, Direction);
    }

    public sealed class GridValidationReport
    {
        private readonly List<string> _errors = new List<string>();

        public bool IsValid => _errors.Count == 0;
        public IReadOnlyList<string> Errors => _errors;
        internal void Add(string error) => _errors.Add(error);
    }

    [Serializable]
    public sealed class KitchenGridDefinition
    {
        private static readonly GridCoordinate[] Neighbors =
        {
            new GridCoordinate(1, 0),
            new GridCoordinate(-1, 0),
            new GridCoordinate(0, 1),
            new GridCoordinate(0, -1)
        };

        private readonly GridTerrain[] _terrain;
        private readonly List<GridPlacement> _placements;

        public KitchenGridDefinition(
            string stageId,
            int width,
            int depth,
            GridSpaceTransform space,
            IEnumerable<GridTerrain> terrain,
            IEnumerable<GridPlacement> placements,
            GridCoordinate playerSpawn)
        {
            StageId = stageId ?? throw new ArgumentNullException(nameof(stageId));
            Width = width;
            Depth = depth;
            Space = space ?? throw new ArgumentNullException(nameof(space));
            _terrain = terrain?.ToArray() ?? throw new ArgumentNullException(nameof(terrain));
            _placements = placements?.ToList() ?? throw new ArgumentNullException(nameof(placements));
            PlayerSpawn = playerSpawn;
        }

        public string StageId { get; }
        public int Width { get; }
        public int Depth { get; }
        public GridSpaceTransform Space { get; }
        public GridCoordinate PlayerSpawn { get; }
        public IReadOnlyList<GridPlacement> Placements => _placements;

        public bool IsInBounds(GridCoordinate cell)
        {
            return cell.X >= 0 && cell.X < Width && cell.Z >= 0 && cell.Z < Depth;
        }

        public GridTerrain TerrainAt(GridCoordinate cell)
        {
            return IsInBounds(cell) && _terrain.Length == Width * Depth
                ? _terrain[cell.Z * Width + cell.X]
                : GridTerrain.Void;
        }

        public bool IsWalkable(GridCoordinate cell)
        {
            if (TerrainAt(cell) != GridTerrain.Floor)
            {
                return false;
            }

            return !_placements.Any(placement => placement.OccupiedCells.Contains(cell));
        }

        public bool IsAdjacent(GridCoordinate cell, GridPlacement placement)
        {
            return placement.OccupiedCells.Any(occupied => cell.ManhattanDistance(occupied) == 1);
        }

        public bool IsThrowPathClear(float startX, float startZ, float endX, float endZ, out string reason)
        {
            var deltaX = endX - startX;
            var deltaZ = endZ - startZ;
            var distance = (float)Math.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
            var steps = Math.Max(1, (int)Math.Ceiling(distance / (Space.CellSize * 0.2f)));
            for (var step = 0; step <= steps; step++)
            {
                var progress = step / (float)steps;
                var cell = Space.WorldToCell(startX + deltaX * progress, startZ + deltaZ * progress);
                if (!IsInBounds(cell))
                {
                    reason = "THROW_PATH_OUT_OF_BOUNDS:" + cell;
                    return false;
                }

                var terrain = TerrainAt(cell);
                if (terrain == GridTerrain.Wall || terrain == GridTerrain.Void)
                {
                    reason = "THROW_PATH_BLOCKED:" + cell;
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        public GridValidationReport Validate()
        {
            var report = new GridValidationReport();
            if (Width < 1 || Depth < 1)
            {
                report.Add("GRID_SIZE_INVALID");
                return report;
            }

            if (_terrain.Length != Width * Depth)
            {
                report.Add("TERRAIN_COUNT_MISMATCH");
                return report;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var occupied = new Dictionary<GridCoordinate, string>();
            foreach (var placement in _placements)
            {
                if (!ids.Add(placement.Id))
                {
                    report.Add("DUPLICATE_PLACEMENT_ID:" + placement.Id);
                }

                if (placement.ArchetypeId == KitchenArchetypeIds.IngredientCrate &&
                    string.IsNullOrWhiteSpace(placement.ContentId))
                {
                    report.Add("INGREDIENT_SOURCE_CONTENT_MISSING:" + placement.Id);
                }

                foreach (var cell in placement.OccupiedCells)
                {
                    if (!IsInBounds(cell))
                    {
                        report.Add("PLACEMENT_OUT_OF_BOUNDS:" + placement.Id + ":" + cell);
                        continue;
                    }

                    if (TerrainAt(cell) != GridTerrain.Floor)
                    {
                        report.Add("PLACEMENT_ON_NON_FLOOR:" + placement.Id + ":" + cell);
                    }

                    if (occupied.TryGetValue(cell, out var existing))
                    {
                        report.Add("PLACEMENT_OVERLAP:" + existing + ":" + placement.Id + ":" + cell);
                    }
                    else
                    {
                        occupied.Add(cell, placement.Id);
                    }
                }
            }

            if (!IsWalkable(PlayerSpawn))
            {
                report.Add("SPAWN_NOT_WALKABLE:" + PlayerSpawn);
                return report;
            }

            var reachable = FindReachableCells();
            foreach (var placement in _placements.Where(item => item.RequiredInteraction))
            {
                if (!reachable.Any(cell => IsAdjacent(cell, placement)))
                {
                    report.Add("REQUIRED_PLACEMENT_UNREACHABLE:" + placement.Id);
                }
            }

            return report;
        }

        public IReadOnlyCollection<GridCoordinate> FindReachableCells()
        {
            var visited = new HashSet<GridCoordinate>();
            if (!IsWalkable(PlayerSpawn))
            {
                return visited;
            }

            var pending = new Queue<GridCoordinate>();
            visited.Add(PlayerSpawn);
            pending.Enqueue(PlayerSpawn);
            while (pending.Count > 0)
            {
                var current = pending.Dequeue();
                foreach (var offset in Neighbors)
                {
                    var candidate = new GridCoordinate(current.X + offset.X, current.Z + offset.Z);
                    if (IsWalkable(candidate) && visited.Add(candidate))
                    {
                        pending.Enqueue(candidate);
                    }
                }
            }

            return visited;
        }

        public string DeterministicSignature()
        {
            var builder = new StringBuilder();
            builder.Append(StageId).Append('|').Append(Width).Append('x').Append(Depth).Append('|')
                .Append(Space.CellSize.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                .Append(PlayerSpawn).Append('|');
            for (var i = 0; i < _terrain.Length; i++)
            {
                builder.Append((int)_terrain[i]);
            }

            foreach (var placement in _placements.OrderBy(item => item.Id, StringComparer.Ordinal))
            {
                builder.Append('|').Append(placement.Id).Append(':').Append(placement.ArchetypeId)
                    .Append(':').Append(placement.Anchor).Append(':')
                    .Append(placement.Footprint.Width).Append('x').Append(placement.Footprint.Depth)
                    .Append(':').Append((int)placement.Direction).Append(':').Append(placement.RequiredInteraction ? '1' : '0')
                    .Append(':').Append(placement.ContentId ?? string.Empty);
            }

            return builder.ToString();
        }
    }

    public static class KitchenArchetypeIds
    {
        public const string Wall = "terrain.wall";
        public const string IngredientCrate = "station.source.ingredient";
        public const string LettuceCrate = IngredientCrate;
        public const string ChoppingBoard = "station.chop";
        public const string PotHeatSource = "station.heat.pot";
        public const string PanHeatSource = "station.heat.pan";
        public const string CookingPotUnit = PotHeatSource;
        public const string FryingPanUnit = PanHeatSource;
        public const string AssemblyPassCounter = "station.assemble";
        public const string AssemblyCounter = AssemblyPassCounter;
        public const string ServingHatch = "station.serve";
        public const string ContainerDispenser = "station.source.container";
        public const string Counter = "station.counter";
        public const string TrashBin = "station.trash";
        public const string BikeDock = "station.delivery.bike";
        public const string DeliveryPoint = "station.delivery.destination";
        public const string RecoveryBin = "station.recovery";
        public const string ExtinguisherCabinet = "station.safety.extinguisher";
        public const string WashingSink = "station.wash.sink";
        public const string PlateDispenser = "station.source.plate";
        public const string DishReturn = "station.return.dish";
        public const string CourierShelf = "station.delivery.courier";
        public const string IngredientSourceSlot = "station.home.source_slot";
        public const string DeliveryAgencySocket = "station.home.delivery_agency_socket";
        public const string KitchenExit = "station.home.exit";
    }

    public enum HomeKitchenSocketKind
    {
        GenericEquipment,
        AssemblyPassSurface,
        IngredientSource,
        ContainerSource,
        DeliveryAgency,
        LoadingDock,
        Exit
    }

    [Serializable]
    public sealed class HomeKitchenSocketDefinition
    {
        public HomeKitchenSocketDefinition(
            string id,
            HomeKitchenSocketKind kind,
            GridCoordinate coordinate,
            string placementId = null)
        {
            Id = string.IsNullOrWhiteSpace(id)
                ? throw new ArgumentException("Home kitchen socket ID is required.", nameof(id))
                : id;
            Kind = kind;
            Coordinate = coordinate;
            PlacementId = placementId;
        }

        public string Id { get; }
        public HomeKitchenSocketKind Kind { get; }
        public GridCoordinate Coordinate { get; }
        public string PlacementId { get; }
        public bool UsesFixedWorkSurface => !string.IsNullOrWhiteSpace(PlacementId);
    }

    public sealed class HomeKitchenDefinition
    {
        private readonly List<HomeKitchenSocketDefinition> _sockets;

        public HomeKitchenDefinition(
            KitchenGridDefinition grid,
            IEnumerable<HomeKitchenSocketDefinition> sockets)
        {
            Grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _sockets = sockets?.ToList() ?? throw new ArgumentNullException(nameof(sockets));

            var duplicateSocket = _sockets
                .GroupBy(socket => socket.Id, StringComparer.Ordinal)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateSocket != null)
            {
                throw new ArgumentException("Duplicate home kitchen socket ID: " + duplicateSocket.Key, nameof(sockets));
            }

            var placementIds = new HashSet<string>(Grid.Placements.Select(placement => placement.Id), StringComparer.Ordinal);
            var missingPlacement = _sockets.FirstOrDefault(
                socket => socket.UsesFixedWorkSurface && !placementIds.Contains(socket.PlacementId));
            if (missingPlacement != null)
            {
                throw new ArgumentException(
                    $"Home kitchen socket {missingPlacement.Id} references missing placement {missingPlacement.PlacementId}.",
                    nameof(sockets));
            }
        }

        public KitchenGridDefinition Grid { get; }
        public IReadOnlyList<HomeKitchenSocketDefinition> Sockets => _sockets;

        public bool IsSocketReachable(HomeKitchenSocketDefinition socket)
        {
            if (socket == null)
            {
                return false;
            }

            var reachable = Grid.FindReachableCells();
            if (!socket.UsesFixedWorkSurface)
            {
                return reachable.Contains(socket.Coordinate);
            }

            var placement = Grid.Placements.First(item => item.Id == socket.PlacementId);
            return reachable.Any(cell => Grid.IsAdjacent(cell, placement));
        }
    }

    public static class HomeKitchenV1
    {
        public const int Width = 12;
        public const int Depth = 9;
        public const float CellSize = 1.6f;
        public const int MinimumShopRank = 1;
        public const int MaximumShopRank = 10;

        public static HomeKitchenDefinition Create()
        {
            var terrain = Enumerable.Repeat(GridTerrain.Floor, Width * Depth).ToArray();
            for (var z = 0; z < Depth; z++)
            {
                terrain[z * Width] = GridTerrain.Wall;
                terrain[z * Width + Width - 1] = GridTerrain.Wall;
            }

            // The loading dock and exit replace two cells in the right wall.
            terrain[7 * Width + 11] = GridTerrain.Floor;
            terrain[6 * Width + 11] = GridTerrain.Floor;

            var placements = new List<GridPlacement>();
            var sockets = new List<HomeKitchenSocketDefinition>();

            AddSocketRow(
                placements,
                sockets,
                "generic.south",
                HomeKitchenSocketKind.GenericEquipment,
                KitchenArchetypeIds.Counter,
                1,
                10,
                0,
                GridDirection.North);

            AddSocketRow(
                placements,
                sockets,
                "generic.island.south",
                HomeKitchenSocketKind.GenericEquipment,
                KitchenArchetypeIds.Counter,
                3,
                4,
                3,
                GridDirection.South);
            AddSocketRow(
                placements,
                sockets,
                "generic.island.south",
                HomeKitchenSocketKind.GenericEquipment,
                KitchenArchetypeIds.Counter,
                7,
                8,
                3,
                GridDirection.South);
            AddSocketRow(
                placements,
                sockets,
                "generic.island.north",
                HomeKitchenSocketKind.GenericEquipment,
                KitchenArchetypeIds.Counter,
                3,
                4,
                6,
                GridDirection.North);
            AddSocketRow(
                placements,
                sockets,
                "generic.island.north",
                HomeKitchenSocketKind.GenericEquipment,
                KitchenArchetypeIds.Counter,
                7,
                8,
                6,
                GridDirection.North);

            AddSocketRow(
                placements,
                sockets,
                "assembly.south",
                HomeKitchenSocketKind.AssemblyPassSurface,
                KitchenArchetypeIds.AssemblyPassCounter,
                3,
                4,
                2,
                GridDirection.North);
            AddSocketRow(
                placements,
                sockets,
                "assembly.south",
                HomeKitchenSocketKind.AssemblyPassSurface,
                KitchenArchetypeIds.AssemblyPassCounter,
                7,
                8,
                2,
                GridDirection.North);
            AddSocketRow(
                placements,
                sockets,
                "assembly.north",
                HomeKitchenSocketKind.AssemblyPassSurface,
                KitchenArchetypeIds.AssemblyPassCounter,
                3,
                4,
                5,
                GridDirection.South);
            AddSocketRow(
                placements,
                sockets,
                "assembly.north",
                HomeKitchenSocketKind.AssemblyPassSurface,
                KitchenArchetypeIds.AssemblyPassCounter,
                7,
                8,
                5,
                GridDirection.South);

            AddSocketRow(
                placements,
                sockets,
                "ingredient",
                HomeKitchenSocketKind.IngredientSource,
                KitchenArchetypeIds.IngredientSourceSlot,
                1,
                6,
                8,
                GridDirection.South);
            AddFixedSocket(placements, sockets, "container", HomeKitchenSocketKind.ContainerSource,
                KitchenArchetypeIds.ContainerDispenser, new GridCoordinate(7, 8), GridDirection.South);
            AddFixedSocket(placements, sockets, "agency.1", HomeKitchenSocketKind.DeliveryAgency,
                KitchenArchetypeIds.DeliveryAgencySocket, new GridCoordinate(8, 8), GridDirection.South);
            AddFixedSocket(placements, sockets, "agency.2", HomeKitchenSocketKind.DeliveryAgency,
                KitchenArchetypeIds.DeliveryAgencySocket, new GridCoordinate(9, 8), GridDirection.South);
            AddFixedSocket(placements, sockets, "trash", null, KitchenArchetypeIds.TrashBin,
                new GridCoordinate(10, 8), GridDirection.South);
            AddFixedSocket(placements, sockets, "loading", HomeKitchenSocketKind.LoadingDock,
                KitchenArchetypeIds.BikeDock, new GridCoordinate(11, 7), GridDirection.West);

            sockets.Add(new HomeKitchenSocketDefinition(
                "exit",
                HomeKitchenSocketKind.Exit,
                new GridCoordinate(11, 6)));

            var grid = new KitchenGridDefinition(
                GameIds.HomeKitchenV1,
                Width,
                Depth,
                new GridSpaceTransform(-Width * CellSize * 0.5f, -Depth * CellSize * 0.5f, CellSize),
                terrain,
                placements,
                new GridCoordinate(1, 1));
            return new HomeKitchenDefinition(grid, sockets);
        }

        public static HomeKitchenDefinition CreateForShopRank(int shopRank)
        {
            if (shopRank < MinimumShopRank || shopRank > MaximumShopRank)
            {
                throw new ArgumentOutOfRangeException(nameof(shopRank));
            }

            // Shop rank changes equipment limits and service tiers, never this topology.
            return Create();
        }

        private static void AddSocketRow(
            ICollection<GridPlacement> placements,
            ICollection<HomeKitchenSocketDefinition> sockets,
            string idPrefix,
            HomeKitchenSocketKind kind,
            string archetypeId,
            int startX,
            int endX,
            int z,
            GridDirection direction)
        {
            for (var x = startX; x <= endX; x++)
            {
                AddFixedSocket(
                    placements,
                    sockets,
                    $"{idPrefix}.{x}.{z}",
                    kind,
                    archetypeId,
                    new GridCoordinate(x, z),
                    direction);
            }
        }

        private static void AddFixedSocket(
            ICollection<GridPlacement> placements,
            ICollection<HomeKitchenSocketDefinition> sockets,
            string id,
            HomeKitchenSocketKind? kind,
            string archetypeId,
            GridCoordinate coordinate,
            GridDirection direction)
        {
            var placementId = "home." + id;
            placements.Add(new GridPlacement(
                placementId,
                archetypeId,
                coordinate,
                new GridFootprint(1, 1),
                direction,
                true));
            if (kind.HasValue)
            {
                sockets.Add(new HomeKitchenSocketDefinition(id, kind.Value, coordinate, placementId));
            }
        }
    }

    public static class TutorialKitchenGrid
    {
        public const int Width = 10;
        public const int Depth = 8;
        public const float CellSize = 1.6f;

        public static KitchenGridDefinition Create()
        {
            var terrain = Enumerable.Repeat(GridTerrain.Floor, Width * Depth).ToArray();
            for (var x = 0; x < Width; x++)
            {
                terrain[x] = GridTerrain.Wall;
            }

            for (var z = 1; z < Depth; z++)
            {
                terrain[z * Width] = GridTerrain.Wall;
                terrain[z * Width + Width - 1] = GridTerrain.Wall;
            }

            var one = new GridFootprint(1, 1);
            var placements = new[]
            {
                new GridPlacement("lettuce", KitchenArchetypeIds.IngredientCrate, new GridCoordinate(1, 7), one, GridDirection.South, true, GameIds.LettuceIngredient),
                new GridPlacement("chop", KitchenArchetypeIds.ChoppingBoard, new GridCoordinate(2, 7), one, GridDirection.South, true),
                new GridPlacement("counter.top.1", KitchenArchetypeIds.Counter, new GridCoordinate(3, 7), one, GridDirection.South, false),
                new GridPlacement("serve", KitchenArchetypeIds.ServingHatch, new GridCoordinate(4, 7), one, GridDirection.South, true),
                new GridPlacement("container", KitchenArchetypeIds.ContainerDispenser, new GridCoordinate(5, 7), one, GridDirection.South, true),
                new GridPlacement("counter.top.2", KitchenArchetypeIds.Counter, new GridCoordinate(6, 7), one, GridDirection.South, false),
                new GridPlacement("counter.top.3", KitchenArchetypeIds.Counter, new GridCoordinate(7, 7), one, GridDirection.South, false),
                new GridPlacement("counter.top.4", KitchenArchetypeIds.Counter, new GridCoordinate(8, 7), one, GridDirection.South, false),
                new GridPlacement("counter.island.north.1", KitchenArchetypeIds.Counter, new GridCoordinate(3, 5), one, GridDirection.North, false),
                new GridPlacement("assemble", KitchenArchetypeIds.AssemblyCounter, new GridCoordinate(4, 5), one, GridDirection.North, true),
                new GridPlacement("counter.island.north.2", KitchenArchetypeIds.Counter, new GridCoordinate(5, 5), one, GridDirection.North, false),
                new GridPlacement("counter.island.south.1", KitchenArchetypeIds.Counter, new GridCoordinate(3, 4), one, GridDirection.South, false),
                new GridPlacement("counter.island.south.2", KitchenArchetypeIds.Counter, new GridCoordinate(4, 4), one, GridDirection.South, false),
                new GridPlacement("counter.island.south.3", KitchenArchetypeIds.Counter, new GridCoordinate(5, 4), one, GridDirection.South, false),
                new GridPlacement("counter.bottom.1", KitchenArchetypeIds.Counter, new GridCoordinate(1, 2), one, GridDirection.North, false),
                new GridPlacement("counter.bottom.2", KitchenArchetypeIds.Counter, new GridCoordinate(2, 2), one, GridDirection.North, false),
                new GridPlacement("counter.bottom.3", KitchenArchetypeIds.Counter, new GridCoordinate(7, 2), one, GridDirection.North, false),
                new GridPlacement("counter.bottom.4", KitchenArchetypeIds.Counter, new GridCoordinate(8, 2), one, GridDirection.North, false),
                new GridPlacement("trash", KitchenArchetypeIds.TrashBin, new GridCoordinate(8, 4), one, GridDirection.West, true)
            };

            return new KitchenGridDefinition(
                GameIds.TutorialStage,
                Width,
                Depth,
                new GridSpaceTransform(-Width * CellSize * 0.5f, -Depth * CellSize * 0.5f, CellSize),
                terrain,
                placements,
                new GridCoordinate(7, 4));
        }
    }

    public static class TutorialSoupKitchenGrid
    {
        public const int Width = 10;
        public const int Depth = 8;
        public const float CellSize = 1.6f;

        public static KitchenGridDefinition Create()
        {
            var terrain = Enumerable.Repeat(GridTerrain.Floor, Width * Depth).ToArray();
            for (var x = 0; x < Width; x++)
            {
                terrain[x] = GridTerrain.Wall;
            }

            for (var z = 1; z < Depth; z++)
            {
                terrain[z * Width] = GridTerrain.Wall;
                terrain[z * Width + Width - 1] = GridTerrain.Wall;
            }

            var one = new GridFootprint(1, 1);
            var placements = new[]
            {
                new GridPlacement("carrot", KitchenArchetypeIds.IngredientCrate, new GridCoordinate(1, 7), one, GridDirection.South, true, GameIds.CarrotIngredient),
                new GridPlacement("onion", KitchenArchetypeIds.IngredientCrate, new GridCoordinate(2, 7), one, GridDirection.South, true, GameIds.OnionIngredient),
                new GridPlacement("chop", KitchenArchetypeIds.ChoppingBoard, new GridCoordinate(3, 7), one, GridDirection.South, true),
                new GridPlacement("pot.heat", KitchenArchetypeIds.PotHeatSource, new GridCoordinate(4, 7), one, GridDirection.South, true),
                new GridPlacement("pot.rest", KitchenArchetypeIds.Counter, new GridCoordinate(5, 7), one, GridDirection.South, true),
                new GridPlacement("container", KitchenArchetypeIds.ContainerDispenser, new GridCoordinate(6, 7), one, GridDirection.South, true),
                new GridPlacement("serve", KitchenArchetypeIds.ServingHatch, new GridCoordinate(7, 7), one, GridDirection.South, true),
                new GridPlacement("trash", KitchenArchetypeIds.TrashBin, new GridCoordinate(8, 7), one, GridDirection.South, true),
                new GridPlacement("counter.island.1", KitchenArchetypeIds.Counter, new GridCoordinate(3, 4), one, GridDirection.North, false),
                new GridPlacement("counter.island.2", KitchenArchetypeIds.Counter, new GridCoordinate(4, 4), one, GridDirection.North, false),
                new GridPlacement("counter.island.3", KitchenArchetypeIds.Counter, new GridCoordinate(5, 4), one, GridDirection.North, false),
                new GridPlacement("counter.island.4", KitchenArchetypeIds.Counter, new GridCoordinate(3, 3), one, GridDirection.South, false),
                new GridPlacement("counter.island.5", KitchenArchetypeIds.Counter, new GridCoordinate(4, 3), one, GridDirection.South, false),
                new GridPlacement("counter.island.6", KitchenArchetypeIds.Counter, new GridCoordinate(5, 3), one, GridDirection.South, false)
            };

            return new KitchenGridDefinition(
                GameIds.TutorialSoupStage,
                Width,
                Depth,
                new GridSpaceTransform(-Width * CellSize * 0.5f, -Depth * CellSize * 0.5f, CellSize),
                terrain,
                placements,
                new GridCoordinate(7, 4));
        }
    }

    public static class TutorialThrowDeliveryKitchenGrid
    {
        public const int Width = 12;
        public const int Depth = 9;
        public const float CellSize = 1.6f;

        public static KitchenGridDefinition Create()
        {
            var terrain = Enumerable.Repeat(GridTerrain.Floor, Width * Depth).ToArray();
            for (var x = 0; x < Width; x++)
            {
                terrain[x] = GridTerrain.Wall;
            }

            for (var z = 1; z < Depth; z++)
            {
                terrain[z * Width] = GridTerrain.Wall;
                terrain[z * Width + Width - 1] = GridTerrain.Wall;
            }

            var one = new GridFootprint(1, 1);
            var placements = new[]
            {
                new GridPlacement("carrot", KitchenArchetypeIds.IngredientCrate, new GridCoordinate(1, 2), one, GridDirection.North, true, GameIds.CarrotIngredient),
                new GridPlacement("onion", KitchenArchetypeIds.IngredientCrate, new GridCoordinate(2, 2), one, GridDirection.North, true, GameIds.OnionIngredient),
                new GridPlacement("chop", KitchenArchetypeIds.ChoppingBoard, new GridCoordinate(3, 2), one, GridDirection.North, true),
                new GridPlacement("container", KitchenArchetypeIds.ContainerDispenser, new GridCoordinate(4, 2), one, GridDirection.North, true),
                new GridPlacement("trash", KitchenArchetypeIds.TrashBin, new GridCoordinate(5, 2), one, GridDirection.North, true),
                new GridPlacement("throw.wall.1", KitchenArchetypeIds.Counter, new GridCoordinate(3, 5), one, GridDirection.South, false),
                new GridPlacement("throw.wall.2", KitchenArchetypeIds.Counter, new GridCoordinate(4, 5), one, GridDirection.South, false),
                new GridPlacement("throw.wall.3", KitchenArchetypeIds.Counter, new GridCoordinate(5, 5), one, GridDirection.South, false),
                new GridPlacement("throw.wall.4", KitchenArchetypeIds.Counter, new GridCoordinate(6, 5), one, GridDirection.South, false),
                new GridPlacement("throw.wall.5", KitchenArchetypeIds.Counter, new GridCoordinate(7, 5), one, GridDirection.South, false),
                new GridPlacement("throw.wall.6", KitchenArchetypeIds.Counter, new GridCoordinate(8, 5), one, GridDirection.South, false),
                new GridPlacement("pot.heat", KitchenArchetypeIds.PotHeatSource, new GridCoordinate(6, 7), one, GridDirection.South, true),
                new GridPlacement("pot.rest", KitchenArchetypeIds.Counter, new GridCoordinate(7, 7), one, GridDirection.South, true),
                new GridPlacement("counter.north", KitchenArchetypeIds.Counter, new GridCoordinate(8, 7), one, GridDirection.South, false),
                new GridPlacement("bike.dock", KitchenArchetypeIds.BikeDock, new GridCoordinate(9, 8), one, GridDirection.South, true),
                new GridPlacement("delivery.point", KitchenArchetypeIds.DeliveryPoint, new GridCoordinate(10, 7), one, GridDirection.West, false)
            };

            return new KitchenGridDefinition(
                GameIds.TutorialThrowDeliveryStage,
                Width,
                Depth,
                new GridSpaceTransform(-Width * CellSize * 0.5f, -Depth * CellSize * 0.5f, CellSize),
                terrain,
                placements,
                new GridCoordinate(6, 3));
        }
    }

    public static class TutorialDashKitchenGrid
    {
        public const int Width = 12;
        public const int Depth = 5;
        public const float CellSize = 1.6f;

        public static KitchenGridDefinition Create()
        {
            var terrain = Enumerable.Repeat(GridTerrain.Wall, Width * Depth).ToArray();
            for (var x = 1; x <= 3; x++)
            {
                for (var z = 1; z <= 3; z++)
                {
                    terrain[z * Width + x] = GridTerrain.Floor;
                }
            }

            terrain[2 * Width + 4] = GridTerrain.Floor;
            terrain[2 * Width + 5] = GridTerrain.Floor;
            for (var x = 6; x < Width - 1; x++)
            {
                for (var z = 1; z <= 3; z++)
                {
                    terrain[z * Width + x] = GridTerrain.Floor;
                }
            }

            var one = new GridFootprint(1, 1);
            var placements = new[]
            {
                new GridPlacement("lettuce", KitchenArchetypeIds.IngredientCrate, new GridCoordinate(1, 3), one, GridDirection.South, true, GameIds.LettuceIngredient),
                new GridPlacement("chop", KitchenArchetypeIds.ChoppingBoard, new GridCoordinate(2, 3), one, GridDirection.South, true),
                new GridPlacement("container", KitchenArchetypeIds.ContainerDispenser, new GridCoordinate(3, 3), one, GridDirection.South, true),
                new GridPlacement("trash", KitchenArchetypeIds.TrashBin, new GridCoordinate(1, 1), one, GridDirection.North, false),
                new GridPlacement("serve", KitchenArchetypeIds.ServingHatch, new GridCoordinate(8, 3), one, GridDirection.South, true)
            };

            return new KitchenGridDefinition(
                GameIds.TutorialDashStage,
                Width,
                Depth,
                new GridSpaceTransform(-Width * CellSize * 0.5f, -Depth * CellSize * 0.5f, CellSize),
                terrain,
                placements,
                new GridCoordinate(1, 2));
        }
    }

    public static class TutorialFryingKitchenGrid
    {
        public const int Width = 10;
        public const int Depth = 8;
        public const float CellSize = 1.6f;

        public static KitchenGridDefinition Create()
        {
            var terrain = Enumerable.Repeat(GridTerrain.Floor, Width * Depth).ToArray();
            for (var x = 0; x < Width; x++)
            {
                terrain[x] = GridTerrain.Wall;
            }

            for (var z = 1; z < Depth; z++)
            {
                terrain[z * Width] = GridTerrain.Wall;
                terrain[z * Width + Width - 1] = GridTerrain.Wall;
            }

            var one = new GridFootprint(1, 1);
            var placements = new[]
            {
                new GridPlacement("beef", KitchenArchetypeIds.IngredientCrate, new GridCoordinate(1, 7), one, GridDirection.South, true, GameIds.BeefIngredient),
                new GridPlacement("chop", KitchenArchetypeIds.ChoppingBoard, new GridCoordinate(2, 7), one, GridDirection.South, true),
                new GridPlacement("pan.heat", KitchenArchetypeIds.PanHeatSource, new GridCoordinate(3, 7), one, GridDirection.South, true),
                new GridPlacement("pan.rest", KitchenArchetypeIds.Counter, new GridCoordinate(4, 7), one, GridDirection.South, true),
                new GridPlacement("cook.counter", KitchenArchetypeIds.Counter, new GridCoordinate(5, 7), one, GridDirection.South, false),
                new GridPlacement("lettuce", KitchenArchetypeIds.IngredientCrate, new GridCoordinate(1, 1), one, GridDirection.North, true, GameIds.LettuceIngredient),
                new GridPlacement("assembly", KitchenArchetypeIds.AssemblyCounter, new GridCoordinate(2, 1), one, GridDirection.North, true),
                new GridPlacement("container", KitchenArchetypeIds.ContainerDispenser, new GridCoordinate(3, 1), one, GridDirection.North, true),
                new GridPlacement("serve", KitchenArchetypeIds.ServingHatch, new GridCoordinate(4, 1), one, GridDirection.North, true),
                new GridPlacement("trash", KitchenArchetypeIds.TrashBin, new GridCoordinate(5, 1), one, GridDirection.North, true),
                new GridPlacement("assembly.counter", KitchenArchetypeIds.Counter, new GridCoordinate(6, 1), one, GridDirection.North, false)
            };

            return new KitchenGridDefinition(
                GameIds.TutorialFryingStage,
                Width,
                Depth,
                new GridSpaceTransform(-Width * CellSize * 0.5f, -Depth * CellSize * 0.5f, CellSize),
                terrain,
                placements,
                new GridCoordinate(5, 4));
        }
    }

    public static class TutorialFireRecoveryKitchenGrid
    {
        public const int Width = 12;
        public const int Depth = 9;
        public const float CellSize = 1.6f;

        public static KitchenGridDefinition Create()
        {
            var terrain = BorderedFloor(Width, Depth);
            var one = new GridFootprint(1, 1);
            var placements = new[]
            {
                new GridPlacement("carrot", KitchenArchetypeIds.IngredientCrate, new GridCoordinate(1, 8), one, GridDirection.South, true, GameIds.CarrotIngredient),
                new GridPlacement("onion", KitchenArchetypeIds.IngredientCrate, new GridCoordinate(2, 8), one, GridDirection.South, true, GameIds.OnionIngredient),
                new GridPlacement("chop", KitchenArchetypeIds.ChoppingBoard, new GridCoordinate(3, 8), one, GridDirection.South, true),
                new GridPlacement("pot.heat", KitchenArchetypeIds.PotHeatSource, new GridCoordinate(4, 8), one, GridDirection.South, true),
                new GridPlacement("extinguisher", KitchenArchetypeIds.ExtinguisherCabinet, new GridCoordinate(6, 8), one, GridDirection.South, true),
                new GridPlacement("pan.heat", KitchenArchetypeIds.PanHeatSource, new GridCoordinate(7, 8), one, GridDirection.South, true),
                new GridPlacement("beef", KitchenArchetypeIds.IngredientCrate, new GridCoordinate(8, 8), one, GridDirection.South, true, GameIds.BeefIngredient),
                new GridPlacement("lettuce", KitchenArchetypeIds.IngredientCrate, new GridCoordinate(9, 8), one, GridDirection.South, true, GameIds.LettuceIngredient),
                new GridPlacement("container", KitchenArchetypeIds.ContainerDispenser, new GridCoordinate(3, 1), one, GridDirection.North, true),
                new GridPlacement("assembly", KitchenArchetypeIds.AssemblyCounter, new GridCoordinate(4, 1), one, GridDirection.North, true),
                new GridPlacement("serve", KitchenArchetypeIds.ServingHatch, new GridCoordinate(5, 1), one, GridDirection.North, true),
                new GridPlacement("trash", KitchenArchetypeIds.TrashBin, new GridCoordinate(6, 1), one, GridDirection.North, true),
                new GridPlacement("staging", KitchenArchetypeIds.Counter, new GridCoordinate(7, 1), one, GridDirection.North, false)
            };
            return new KitchenGridDefinition(
                GameIds.TutorialFireRecoveryStage, Width, Depth,
                new GridSpaceTransform(-Width * CellSize * 0.5f, -Depth * CellSize * 0.5f, CellSize),
                terrain, placements, new GridCoordinate(6, 4));
        }

        private static GridTerrain[] BorderedFloor(int width, int depth)
        {
            var terrain = Enumerable.Repeat(GridTerrain.Floor, width * depth).ToArray();
            for (var x = 0; x < width; x++) terrain[x] = GridTerrain.Wall;
            for (var z = 1; z < depth; z++)
            {
                terrain[z * width] = GridTerrain.Wall;
                terrain[z * width + width - 1] = GridTerrain.Wall;
            }
            return terrain;
        }
    }

    public static class TutorialDishwashingKitchenGrid
    {
        public const int Width = 11;
        public const int Depth = 8;
        public const float CellSize = 1.6f;

        public static KitchenGridDefinition Create()
        {
            var terrain = Enumerable.Repeat(GridTerrain.Floor, Width * Depth).ToArray();
            for (var x = 0; x < Width; x++) terrain[x] = GridTerrain.Wall;
            for (var z = 1; z < Depth; z++)
            {
                terrain[z * Width] = GridTerrain.Wall;
                terrain[z * Width + Width - 1] = GridTerrain.Wall;
            }
            var one = new GridFootprint(1, 1);
            var placements = new[]
            {
                new GridPlacement("lettuce", KitchenArchetypeIds.IngredientCrate, new GridCoordinate(1, 7), one, GridDirection.South, true, GameIds.LettuceIngredient),
                new GridPlacement("carrot", KitchenArchetypeIds.IngredientCrate, new GridCoordinate(2, 7), one, GridDirection.South, true, GameIds.CarrotIngredient),
                new GridPlacement("onion", KitchenArchetypeIds.IngredientCrate, new GridCoordinate(3, 7), one, GridDirection.South, true, GameIds.OnionIngredient),
                new GridPlacement("chop", KitchenArchetypeIds.ChoppingBoard, new GridCoordinate(4, 7), one, GridDirection.South, true),
                new GridPlacement("pot.heat", KitchenArchetypeIds.PotHeatSource, new GridCoordinate(5, 7), one, GridDirection.South, true),
                new GridPlacement("plates", KitchenArchetypeIds.PlateDispenser, new GridCoordinate(1, 1), one, GridDirection.North, true),
                new GridPlacement("serve", KitchenArchetypeIds.ServingHatch, new GridCoordinate(3, 1), one, GridDirection.North, true),
                new GridPlacement("dish.return", KitchenArchetypeIds.DishReturn, new GridCoordinate(5, 1), one, GridDirection.North, true),
                new GridPlacement("wash", KitchenArchetypeIds.WashingSink, new GridCoordinate(6, 1), one, GridDirection.North, true),
                new GridPlacement("staging", KitchenArchetypeIds.Counter, new GridCoordinate(8, 1), one, GridDirection.North, false)
            };
            return new KitchenGridDefinition(
                GameIds.TutorialDishwashingStage, Width, Depth,
                new GridSpaceTransform(-Width * CellSize * 0.5f, -Depth * CellSize * 0.5f, CellSize),
                terrain, placements, new GridCoordinate(5, 4));
        }
    }

    public static class TutorialCombinedDeliveryKitchenGrid
    {
        public const int Width = 14;
        public const int Depth = 9;
        public const float CellSize = 1.6f;

        public static KitchenGridDefinition Create()
        {
            var terrain = Enumerable.Repeat(GridTerrain.Floor, Width * Depth).ToArray();
            for (var x = 0; x < Width; x++) terrain[x] = GridTerrain.Wall;
            for (var z = 1; z < Depth; z++)
            {
                terrain[z * Width] = GridTerrain.Wall;
                terrain[z * Width + Width - 1] = GridTerrain.Wall;
            }
            var one = new GridFootprint(1, 1);
            var placements = new[]
            {
                new GridPlacement("lettuce", KitchenArchetypeIds.IngredientCrate, new GridCoordinate(1, 8), one, GridDirection.South, true, GameIds.LettuceIngredient),
                new GridPlacement("carrot", KitchenArchetypeIds.IngredientCrate, new GridCoordinate(2, 8), one, GridDirection.South, true, GameIds.CarrotIngredient),
                new GridPlacement("onion", KitchenArchetypeIds.IngredientCrate, new GridCoordinate(3, 8), one, GridDirection.South, true, GameIds.OnionIngredient),
                new GridPlacement("beef", KitchenArchetypeIds.IngredientCrate, new GridCoordinate(4, 8), one, GridDirection.South, true, GameIds.BeefIngredient),
                new GridPlacement("chop", KitchenArchetypeIds.ChoppingBoard, new GridCoordinate(5, 8), one, GridDirection.South, true),
                new GridPlacement("pot.heat", KitchenArchetypeIds.PotHeatSource, new GridCoordinate(6, 8), one, GridDirection.South, true),
                new GridPlacement("pan.heat", KitchenArchetypeIds.PanHeatSource, new GridCoordinate(7, 8), one, GridDirection.South, true),
                new GridPlacement("container", KitchenArchetypeIds.ContainerDispenser, new GridCoordinate(8, 8), one, GridDirection.South, true),
                new GridPlacement("courier", KitchenArchetypeIds.CourierShelf, new GridCoordinate(9, 8), one, GridDirection.South, true),
                new GridPlacement("assembly", KitchenArchetypeIds.AssemblyCounter, new GridCoordinate(5, 1), one, GridDirection.North, true),
                new GridPlacement("trash", KitchenArchetypeIds.TrashBin, new GridCoordinate(7, 1), one, GridDirection.North, true),
                new GridPlacement("bike.dock", KitchenArchetypeIds.BikeDock, new GridCoordinate(11, 8), one, GridDirection.South, true),
                new GridPlacement("delivery.point", KitchenArchetypeIds.DeliveryPoint, new GridCoordinate(12, 4), one, GridDirection.West, false)
            };
            return new KitchenGridDefinition(
                GameIds.TutorialCombinedDeliveryStage, Width, Depth,
                new GridSpaceTransform(-Width * CellSize * 0.5f, -Depth * CellSize * 0.5f, CellSize),
                terrain, placements, new GridCoordinate(6, 4));
        }
    }
}
