using System.Collections.Generic;
using CookedOut.Domain;
using UnityEngine;

namespace CookedOut.Presentation
{
    public enum KitchenProductionAssetCategory
    {
        Terrain,
        Station,
        Ingredient,
        Tool
    }

    public sealed class KitchenProductionAsset : MonoBehaviour
    {
        [SerializeField] private string assetKey;
        [SerializeField] private KitchenProductionAssetCategory category;
        [SerializeField] private float nominalSize = 1f;

        public string AssetKey => assetKey;
        public KitchenProductionAssetCategory Category => category;
        public float NominalSize => nominalSize;

        public void Initialize(string key, KitchenProductionAssetCategory assetCategory, float size)
        {
            assetKey = key;
            category = assetCategory;
            nominalSize = size;
        }

        public bool TryValidate(string expectedKey, KitchenProductionAssetCategory expectedCategory, out string reason)
        {
            if (assetKey != expectedKey)
            {
                reason = $"Production asset key mismatch: expected {expectedKey}, found {assetKey}";
                return false;
            }

            if (category != expectedCategory)
            {
                reason = $"Production asset category mismatch for {assetKey}";
                return false;
            }

            if (nominalSize <= 0f)
            {
                reason = $"Production asset {assetKey} requires a positive nominal size";
                return false;
            }

            var renderers = GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length == 0)
            {
                reason = $"Production asset {assetKey} has no mesh renderer";
                return false;
            }

            foreach (var renderer in renderers)
            {
                if (!renderer.enabled)
                {
                    reason = $"Production asset {assetKey} contains a disabled mesh renderer";
                    return false;
                }

                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                {
                    reason = $"Production asset {assetKey} contains a renderer without a mesh";
                    return false;
                }

                if (renderer.sharedMaterials.Length == 0)
                {
                    reason = $"Production asset {assetKey} contains a renderer without a material";
                    return false;
                }

                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null || material.shader == null || !material.shader.isSupported)
                    {
                        reason = $"Production asset {assetKey} contains a missing or unsupported material";
                        return false;
                    }

                    var colorProperty = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
                    if (material.HasProperty(colorProperty) &&
                        material.GetColor(colorProperty).a < 0.95f &&
                        !material.name.StartsWith("MAT_Glass"))
                    {
                        reason = $"Production asset {assetKey} contains a transparent material";
                        return false;
                    }
                }
            }

            if (!TryGetVisualBounds(out var visualBounds))
            {
                reason = $"Production asset {assetKey} has no visual bounds";
                return false;
            }

            var maximumSize = Mathf.Max(visualBounds.size.x, visualBounds.size.y, visualBounds.size.z);
            if (maximumSize < nominalSize * 0.25f)
            {
                reason =
                    $"Production asset {assetKey} is too small ({maximumSize:0.###}m for {nominalSize:0.###}m nominal size)";
                return false;
            }

            if (maximumSize > nominalSize * 3f)
            {
                reason =
                    $"Production asset {assetKey} is too large ({maximumSize:0.###}m for {nominalSize:0.###}m nominal size)";
                return false;
            }

            if (GetComponentInChildren<Collider>(true) != null)
            {
                reason = $"Production asset {assetKey} must not contain gameplay colliders";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public bool TryGetVisualBounds(out Bounds bounds)
        {
            bounds = default;
            var hasBounds = false;
            var rootWorldToLocal = transform.worldToLocalMatrix;
            foreach (var filter in GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }

                var meshBounds = filter.sharedMesh.bounds;
                var meshToRoot = rootWorldToLocal * filter.transform.localToWorldMatrix;
                for (var x = -1; x <= 1; x += 2)
                {
                    for (var y = -1; y <= 1; y += 2)
                    {
                        for (var z = -1; z <= 1; z += 2)
                        {
                            var corner = meshBounds.center + Vector3.Scale(
                                meshBounds.extents,
                                new Vector3(x, y, z));
                            var rootPoint = meshToRoot.MultiplyPoint3x4(corner);
                            if (!hasBounds)
                            {
                                bounds = new Bounds(rootPoint, Vector3.zero);
                                hasBounds = true;
                            }
                            else
                            {
                                bounds.Encapsulate(rootPoint);
                            }
                        }
                    }
                }
            }

            return hasBounds;
        }
    }

    public static class KitchenProductionAssetFactory
    {
        public const float NominalCellSize = 1.6f;
        private const string ResourceRoot = "KitchenProduction";
        private static readonly HashSet<string> ReportedLoadFailures = new HashSet<string>();

        private static readonly IReadOnlyDictionary<string, string> StationResourceNames =
            new Dictionary<string, string>
            {
                [KitchenArchetypeIds.Counter] = "Counter",
                [KitchenArchetypeIds.IngredientCrate] = "IngredientCrate",
                [KitchenArchetypeIds.ChoppingBoard] = "ChoppingBoard",
                [KitchenArchetypeIds.PotHeatSource] = "PotHeatSource",
                [KitchenArchetypeIds.AssemblyCounter] = "AssemblyCounter",
                [KitchenArchetypeIds.ContainerDispenser] = "ContainerDispenser",
                [KitchenArchetypeIds.ServingHatch] = "ServingHatch",
                [KitchenArchetypeIds.TrashBin] = "TrashBin",
                [KitchenArchetypeIds.BikeDock] = "BikeDock",
                [KitchenArchetypeIds.DeliveryPoint] = "DeliveryPoint",
                [KitchenArchetypeIds.RecoveryBin] = "RecoveryBin",
                [KitchenArchetypeIds.ExtinguisherCabinet] = "ExtinguisherCabinet",
                [KitchenArchetypeIds.WashingSink] = "WashingSink",
                [KitchenArchetypeIds.PlateDispenser] = "PlateDispenser",
                [KitchenArchetypeIds.DishReturn] = "DishReturn",
                [KitchenArchetypeIds.CourierShelf] = "CourierShelf"
            };

        public static bool TryInstantiateTerrain(
            GridTerrain terrain,
            bool alternateFloor,
            Transform parent,
            Vector3 worldPosition,
            float cellSize,
            out KitchenProductionAsset asset)
        {
            var resourceName = terrain == GridTerrain.Wall
                ? "Wall"
                : alternateFloor ? "FloorTileB" : "FloorTileA";
            var key = terrain == GridTerrain.Wall
                ? KitchenArchetypeIds.Wall
                : alternateFloor ? "terrain.floor.b" : "terrain.floor.a";
            if (!TryInstantiate(resourceName, key, KitchenProductionAssetCategory.Terrain, parent, out asset))
            {
                return false;
            }

            asset.transform.position = worldPosition;
            asset.transform.rotation = Quaternion.identity;
            asset.transform.localScale = Vector3.one * (cellSize / asset.NominalSize);
            return true;
        }

        public static bool TryInstantiateStation(
            string archetypeId,
            Transform parent,
            float cellSize,
            out KitchenProductionAsset asset)
        {
            asset = null;
            if (!StationResourceNames.TryGetValue(archetypeId, out var resourceName) ||
                !TryInstantiate(
                    resourceName,
                    archetypeId,
                    KitchenProductionAssetCategory.Station,
                    parent,
                    out asset))
            {
                return false;
            }

            asset.transform.localPosition = Vector3.zero;
            asset.transform.localRotation = Quaternion.identity;
            asset.transform.localScale = Vector3.one * (cellSize / asset.NominalSize);
            return true;
        }

        public static bool TryInstantiateIngredient(
            string ingredientId,
            IngredientPreparation preparation,
            Transform parent,
            out KitchenProductionAsset asset)
        {
            if (preparation != IngredientPreparation.Raw &&
                preparation != IngredientPreparation.Chopped &&
                preparation != IngredientPreparation.Cooked)
            {
                asset = null;
                return false;
            }

            var suffix = preparation == IngredientPreparation.Raw
                ? "Raw"
                : preparation == IngredientPreparation.Chopped ? "Chopped" : "Cooked";
            string resourceName;
            if (ingredientId == GameIds.LettuceIngredient)
            {
                resourceName = "Lettuce" + suffix;
            }
            else if (ingredientId == GameIds.CarrotIngredient)
            {
                resourceName = "Carrot" + suffix;
            }
            else if (ingredientId == GameIds.OnionIngredient)
            {
                resourceName = "Onion" + suffix;
            }
            else if (ingredientId == GameIds.BeefIngredient)
            {
                resourceName = "Beef" + suffix;
            }
            else
            {
                asset = null;
                return false;
            }

            var key = ingredientId + "." + preparation.ToString().ToLowerInvariant();
            if (!TryInstantiate(resourceName, key, KitchenProductionAssetCategory.Ingredient, parent, out asset))
            {
                return false;
            }

            asset.transform.localPosition = Vector3.zero;
            asset.transform.localRotation = Quaternion.identity;
            asset.transform.localScale = Vector3.one;
            return true;
        }

        public static bool TryInstantiateTool(string toolId, Transform parent, out KitchenProductionAsset asset)
        {
            if (toolId != GameIds.FireExtinguisherTool ||
                !TryInstantiate(
                    "FireExtinguisher",
                    toolId,
                    KitchenProductionAssetCategory.Tool,
                    parent,
                    out asset))
            {
                asset = null;
                return false;
            }

            asset.transform.localPosition = Vector3.zero;
            asset.transform.localRotation = Quaternion.identity;
            asset.transform.localScale = Vector3.one;
            return true;
        }

        public static bool TryInstantiateSpecial(
            string resourceName,
            string key,
            Transform parent,
            float cellSize,
            out KitchenProductionAsset asset)
        {
            if (!TryInstantiate(resourceName, key, KitchenProductionAssetCategory.Station, parent, out asset))
            {
                return false;
            }

            asset.transform.localPosition = Vector3.zero;
            asset.transform.localRotation = Quaternion.identity;
            asset.transform.localScale = Vector3.one * (cellSize / asset.NominalSize);
            return true;
        }

        private static bool TryInstantiate(
            string resourceName,
            string expectedKey,
            KitchenProductionAssetCategory expectedCategory,
            Transform parent,
            out KitchenProductionAsset asset)
        {
            var prefab = Resources.Load<GameObject>(ResourceRoot + "/" + resourceName);
            if (prefab == null)
            {
                asset = null;
                ReportLoadFailure(resourceName, "prefab was not found in Resources");
                return false;
            }

            var prefabAsset = prefab.GetComponent<KitchenProductionAsset>();
            var reason = string.Empty;
            if (prefabAsset == null || !prefabAsset.TryValidate(expectedKey, expectedCategory, out reason))
            {
                asset = null;
                ReportLoadFailure(
                    resourceName,
                    prefabAsset == null ? "KitchenProductionAsset marker is missing" : reason);
                return false;
            }

            var instance = Object.Instantiate(prefab, parent, false);
            instance.name = "Production " + resourceName;
            asset = instance.GetComponent<KitchenProductionAsset>();
            if (!asset.TryValidate(expectedKey, expectedCategory, out reason))
            {
                Object.Destroy(instance);
                asset = null;
                ReportLoadFailure(resourceName, reason);
                return false;
            }
            return true;
        }

        private static void ReportLoadFailure(string resourceName, string reason)
        {
            if (ReportedLoadFailures.Add(resourceName))
            {
                Debug.LogWarning(
                    $"Kitchen production asset {resourceName} was rejected: {reason}. " +
                    "The visible procedural fallback will be used instead.");
            }
        }
    }
}
