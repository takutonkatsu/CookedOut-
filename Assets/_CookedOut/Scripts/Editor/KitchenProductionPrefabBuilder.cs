using System;
using System.Collections.Generic;
using System.Linq;
using CookedOut.Domain;
using CookedOut.Presentation;
using UnityEditor;
using UnityEngine;

namespace CookedOut.Editor
{
    public static class KitchenProductionPrefabBuilder
    {
        public const string ModelFolder = "Assets/_CookedOut/Art/KitchenProduction/Models";
        public const string MaterialFolder = "Assets/_CookedOut/Art/KitchenProduction/Materials";
        public const string ResourceFolder = "Assets/_CookedOut/Art/Resources/KitchenProduction";
        private const float BlenderFbxImportScale = 100f;
        private static readonly Quaternion BlenderZUpToUnityYUp = Quaternion.Euler(-90f, 0f, 0f);

        private readonly struct AssetDefinition
        {
            public AssetDefinition(
                string name,
                string key,
                KitchenProductionAssetCategory category,
                float nominalSize)
            {
                Name = name;
                Key = key;
                Category = category;
                NominalSize = nominalSize;
            }

            public string Name { get; }
            public string Key { get; }
            public KitchenProductionAssetCategory Category { get; }
            public float NominalSize { get; }
        }

        private static readonly IReadOnlyList<AssetDefinition> Definitions = new[]
        {
            new AssetDefinition("FloorTileA", "terrain.floor.a", KitchenProductionAssetCategory.Terrain, 1.6f),
            new AssetDefinition("FloorTileB", "terrain.floor.b", KitchenProductionAssetCategory.Terrain, 1.6f),
            new AssetDefinition("Wall", KitchenArchetypeIds.Wall, KitchenProductionAssetCategory.Terrain, 1.6f),
            new AssetDefinition("Counter", KitchenArchetypeIds.Counter, KitchenProductionAssetCategory.Station, 1.6f),
            new AssetDefinition("IngredientCrate", KitchenArchetypeIds.IngredientCrate, KitchenProductionAssetCategory.Station, 1.6f),
            new AssetDefinition("ChoppingBoard", KitchenArchetypeIds.ChoppingBoard, KitchenProductionAssetCategory.Station, 1.6f),
            new AssetDefinition("PotHeatSource", KitchenArchetypeIds.PotHeatSource, KitchenProductionAssetCategory.Station, 1.6f),
            new AssetDefinition("AssemblyCounter", KitchenArchetypeIds.AssemblyCounter, KitchenProductionAssetCategory.Station, 1.6f),
            new AssetDefinition("ContainerDispenser", KitchenArchetypeIds.ContainerDispenser, KitchenProductionAssetCategory.Station, 1.6f),
            new AssetDefinition("ServingHatch", KitchenArchetypeIds.ServingHatch, KitchenProductionAssetCategory.Station, 1.6f),
            new AssetDefinition("TrashBin", KitchenArchetypeIds.TrashBin, KitchenProductionAssetCategory.Station, 1.6f),
            new AssetDefinition("BikeDock", KitchenArchetypeIds.BikeDock, KitchenProductionAssetCategory.Station, 1.6f),
            new AssetDefinition("DeliveryPoint", KitchenArchetypeIds.DeliveryPoint, KitchenProductionAssetCategory.Station, 1.6f),
            new AssetDefinition("RecoveryBin", KitchenArchetypeIds.RecoveryBin, KitchenProductionAssetCategory.Station, 1.6f),
            new AssetDefinition("ExtinguisherCabinet", KitchenArchetypeIds.ExtinguisherCabinet, KitchenProductionAssetCategory.Station, 1.6f),
            new AssetDefinition("WashingSink", KitchenArchetypeIds.WashingSink, KitchenProductionAssetCategory.Station, 1.6f),
            new AssetDefinition("PlateDispenser", KitchenArchetypeIds.PlateDispenser, KitchenProductionAssetCategory.Station, 1.6f),
            new AssetDefinition("DishReturn", KitchenArchetypeIds.DishReturn, KitchenProductionAssetCategory.Station, 1.6f),
            new AssetDefinition("CourierShelf", KitchenArchetypeIds.CourierShelf, KitchenProductionAssetCategory.Station, 1.6f),
            new AssetDefinition("DynamicBridge", "station.dynamic.bridge", KitchenProductionAssetCategory.Station, 1.6f),
            new AssetDefinition("FireExtinguisher", GameIds.FireExtinguisherTool, KitchenProductionAssetCategory.Tool, 1f),
            new AssetDefinition("LettuceRaw", GameIds.LettuceIngredient + ".raw", KitchenProductionAssetCategory.Ingredient, 1f),
            new AssetDefinition("LettuceChopped", GameIds.LettuceIngredient + ".chopped", KitchenProductionAssetCategory.Ingredient, 1f),
            new AssetDefinition("CarrotRaw", GameIds.CarrotIngredient + ".raw", KitchenProductionAssetCategory.Ingredient, 1f),
            new AssetDefinition("CarrotChopped", GameIds.CarrotIngredient + ".chopped", KitchenProductionAssetCategory.Ingredient, 1f),
            new AssetDefinition("OnionRaw", GameIds.OnionIngredient + ".raw", KitchenProductionAssetCategory.Ingredient, 1f),
            new AssetDefinition("OnionChopped", GameIds.OnionIngredient + ".chopped", KitchenProductionAssetCategory.Ingredient, 1f),
            new AssetDefinition("BeefRaw", GameIds.BeefIngredient + ".raw", KitchenProductionAssetCategory.Ingredient, 1f),
            new AssetDefinition("BeefChopped", GameIds.BeefIngredient + ".chopped", KitchenProductionAssetCategory.Ingredient, 1f),
            new AssetDefinition("BeefCooked", GameIds.BeefIngredient + ".cooked", KitchenProductionAssetCategory.Ingredient, 1f)
        };

        private static readonly IReadOnlyDictionary<string, Color> MaterialColors =
            new Dictionary<string, Color>
            {
                ["MAT_FloorA"] = KitchenArtPalette.FloorA,
                ["MAT_FloorB"] = KitchenArtPalette.FloorB,
                ["MAT_Wall"] = KitchenArtPalette.Wall,
                ["MAT_Plum"] = KitchenArtPalette.Structure,
                ["MAT_Cream"] = KitchenArtPalette.Worktop,
                ["MAT_Teal"] = KitchenArtPalette.Teal,
                ["MAT_Ochre"] = KitchenArtPalette.Ochre,
                ["MAT_FlameBlue"] = KitchenArtPalette.FlameBlue,
                ["MAT_DeepInset"] = KitchenArtPalette.DeepInset,
                ["MAT_Metal"] = new Color(0.70f, 0.72f, 0.73f),
                ["MAT_Steel"] = KitchenArtPalette.Steel,
                ["MAT_SteelLight"] = KitchenArtPalette.SteelLight,
                ["MAT_SteelDark"] = KitchenArtPalette.SteelDark,
                ["MAT_Glass"] = new Color(0.70f, 0.91f, 0.96f, 0.28f),
                ["MAT_Wood"] = KitchenArtPalette.Wood,
                ["MAT_WoodDark"] = KitchenArtPalette.WoodDark,
                ["MAT_Container"] = KitchenArtPalette.Container,
                ["MAT_BoardIvory"] = KitchenArtPalette.BoardIvory,
                ["MAT_Lettuce"] = KitchenArtPalette.Lettuce,
                ["MAT_LettuceDark"] = KitchenArtPalette.LeafGreen,
                ["MAT_LettuceLight"] = KitchenArtPalette.LettuceLight,
                ["MAT_Carrot"] = KitchenArtPalette.Carrot,
                ["MAT_CarrotGroove"] = KitchenArtPalette.CarrotGroove,
                ["MAT_Leaf"] = KitchenArtPalette.LeafGreen,
                ["MAT_OnionSkin"] = KitchenArtPalette.OnionSkin,
                ["MAT_OnionFlesh"] = KitchenArtPalette.OnionFlesh,
                ["MAT_OnionHighlight"] = KitchenArtPalette.OnionHighlight,
                ["MAT_OnionReferenceSkin"] = Color.white,
                ["MAT_OnionReferenceRoot"] = new Color(0.73f, 0.51f, 0.26f),
                ["MAT_BeefRaw"] = KitchenArtPalette.BeefRaw,
                ["MAT_BeefFat"] = new Color(1f, 0.64f, 0.58f),
                ["MAT_BeefCooked"] = KitchenArtPalette.BeefCooked,
                ["MAT_Sauce"] = new Color(0.55f, 0.055f, 0.025f),
                ["MAT_Delivery"] = new Color(0.14f, 0.58f, 0.29f),
                ["MAT_Red"] = new Color(0.88f, 0.09f, 0.055f),
                ["MAT_Water"] = new Color(0.25f, 0.78f, 0.95f)
            };

        [MenuItem("COOKED OUT!/Build Kitchen Production Prefabs")]
        public static void BuildFromMenu()
        {
            Build();
        }

        public static void BuildFromCommandLine()
        {
            Build();
        }

        [MenuItem("COOKED OUT!/Build Reference Onion Prefab")]
        public static void BuildReferenceOnion()
        {
            EnsureFolder(MaterialFolder);
            EnsureFolder(ResourceFolder);
            BuildPrefab(Definitions.First(definition => definition.Name == "OnionRaw"));
            AssetDatabase.SaveAssets();
            Debug.Log("Built reference onion with skin texture, settle and turntable clips.");
        }

        public static void Build()
        {
            EnsureFolder(MaterialFolder);
            EnsureFolder(ResourceFolder);

            foreach (var definition in Definitions)
            {
                BuildPrefab(definition);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!TryValidateAll(out var errors))
            {
                throw new InvalidOperationException(string.Join("\n", errors));
            }

            Debug.Log($"Built {Definitions.Count} kitchen production prefabs in {ResourceFolder}");
        }

        [MenuItem("COOKED OUT!/Validate Kitchen Production Prefabs")]
        public static void ValidateFromMenu()
        {
            if (TryValidateAll(out var errors))
            {
                Debug.Log($"COOKED OUT! kitchen production assets are valid ({Definitions.Count}).");
                return;
            }

            Debug.LogWarning("Kitchen production assets are incomplete:\n" + string.Join("\n", errors));
        }

        public static bool TryValidateAll(out IReadOnlyList<string> errors)
        {
            var failures = new List<string>();
            foreach (var definition in Definitions)
            {
                var path = $"{ResourceFolder}/{definition.Name}.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    failures.Add($"Missing prefab: {path}");
                    continue;
                }

                var productionAsset = prefab.GetComponent<KitchenProductionAsset>();
                var reason = string.Empty;
                if (productionAsset == null ||
                    !productionAsset.TryValidate(definition.Key, definition.Category, out reason))
                {
                    failures.Add(productionAsset == null
                        ? $"Missing KitchenProductionAsset marker: {path}"
                        : reason);
                }
            }

            errors = failures;
            return failures.Count == 0;
        }

        private static void BuildPrefab(AssetDefinition definition)
        {
            var modelPath = $"{ModelFolder}/{definition.Name}.fbx";
            AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceSynchronousImport);
            ConfigureModelImporter(modelPath);
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelAsset == null)
            {
                throw new InvalidOperationException($"Kitchen production FBX is missing: {modelPath}");
            }

            var root = new GameObject(definition.Name);
            try
            {
                var modelInstance = UnityEngine.Object.Instantiate(modelAsset, root.transform, false);
                modelInstance.name = definition.Name + " Model";
                modelInstance.transform.localPosition = Vector3.zero;
                // Static Blender FBX meshes arrive with Z-up geometry even though the FBX
                // metadata declares Unity-compatible axes. Rotate the visual hierarchy once;
                // gameplay roots, grid coordinates and colliders remain Unity Y-up.
                modelInstance.transform.localRotation = BlenderZUpToUnityYUp;
                modelInstance.transform.localScale = Vector3.one;

                foreach (var collider in modelInstance.GetComponentsInChildren<Collider>(true))
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }

                foreach (var renderer in modelInstance.GetComponentsInChildren<MeshRenderer>(true))
                {
                    // The FBX importer can retain an older material-slot name when a Blender
                    // source material is replaced in place. The generated mesh object name is
                    // canonical and stable, so prefer it when it carries a known material key.
                    var objectMaterialHint = MaterialColors.Keys
                        .OrderByDescending(name => name.Length)
                        .FirstOrDefault(name => renderer.gameObject.name.StartsWith(name));
                    renderer.sharedMaterials = renderer.sharedMaterials
                        .Select(source => LoadOrCreateMaterial(
                            objectMaterialHint ?? (source == null ? "MAT_Cream" : source.name)))
                        .ToArray();
                }

                root.AddComponent<KitchenProductionAsset>().Initialize(
                    definition.Key,
                    definition.Category,
                    definition.NominalSize);
                if (definition.Name == "OnionRaw")
                {
                    OnionReferenceAnimationBuilder.Configure(root, modelInstance.transform);
                }
                PrefabUtility.SaveAsPrefabAsset(root, $"{ResourceFolder}/{definition.Name}.prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ConfigureModelImporter(string modelPath)
        {
            if (AssetImporter.GetAtPath(modelPath) is not ModelImporter importer)
            {
                throw new InvalidOperationException($"FBX has no ModelImporter: {modelPath}");
            }

            // Blender's static FBX exporter stores meter-authored coordinates in centimeters.
            // Unity imports these files at 0.01x unless the importer scale restores meters.
            importer.globalScale = BlenderFbxImportScale;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = false;
            importer.meshCompression = modelPath.EndsWith("/OnionRaw.fbx")
                ? ModelImporterMeshCompression.Off
                : ModelImporterMeshCompression.Medium;
            importer.animationType = ModelImporterAnimationType.None;
            importer.optimizeGameObjects = false;
            importer.SaveAndReimport();
        }

        private static Material LoadOrCreateMaterial(string sourceName)
        {
            // Prefer the longest matching canonical name so MAT_WoodDark does not
            // collapse into MAT_Wood (and MAT_LettuceDark into MAT_Lettuce).
            var canonicalName = MaterialColors.Keys
                .OrderByDescending(name => name.Length)
                .FirstOrDefault(sourceName.StartsWith) ?? "MAT_Cream";
            var path = $"{MaterialFolder}/{canonicalName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = canonicalName };
                AssetDatabase.CreateAsset(material, path);
            }

            var color = MaterialColors[canonicalName];
            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            if (material.HasProperty("_Smoothness"))
            {
                var isSteel = canonicalName == "MAT_Metal" || canonicalName.StartsWith("MAT_Steel");
                material.SetFloat("_Smoothness", isSteel ? 0.44f : 0.22f);
            }
            if (material.HasProperty("_Metallic"))
            {
                var isSteel = canonicalName == "MAT_Metal" || canonicalName.StartsWith("MAT_Steel");
                material.SetFloat("_Metallic", isSteel ? 0.48f : 0f);
            }
            if (canonicalName == "MAT_OnionReferenceSkin")
            {
                const string texturePath = "Assets/_CookedOut/Art/KitchenProduction/Textures/onion_reference_skin_sv1.png";
                AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (texture == null)
                {
                    throw new InvalidOperationException("Missing onion skin texture. Run tools/blender/onion_reference.py first.");
                }
                material.mainTexture = texture;
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.40f);
            }
            if (canonicalName == "MAT_Glass")
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = $"{current}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }
                current = next;
            }
        }
    }
}
