using System;
using System.Collections.Generic;
using System.Linq;
using CookedOut.Presentation;
using UnityEditor;
using UnityEngine;

namespace CookedOut.Editor
{
    public static class CapybaraProductionPrefabBuilder
    {
        public const string ModelPath =
            "Assets/_CookedOut/Art/Characters/Capybara/CapybaraChef_Model.fbx";
        private const string MaterialFolder =
            "Assets/_CookedOut/Art/Characters/Capybara/Materials";

        private static readonly IReadOnlyDictionary<string, Color> MaterialColors =
            new Dictionary<string, Color>
            {
                ["MAT_Fur"] = new Color(0.55f, 0.16f, 0.025f),
                ["MAT_InnerEar"] = new Color(0.18f, 0.035f, 0.02f),
                ["MAT_Muzzle"] = new Color(0.18f, 0.055f, 0.02f),
                ["MAT_Cream"] = new Color(0.80f, 0.67f, 0.48f),
                ["MAT_Ivory"] = new Color(1.0f, 0.965f, 0.88f),
                ["MAT_Ochre"] = new Color(0.76f, 0.25f, 0.012f),
                ["MAT_Plum"] = new Color(0.075f, 0.014f, 0.07f),
                ["MAT_Black"] = new Color(0.012f, 0.009f, 0.012f)
            };

        [MenuItem("COOKED OUT!/Build Capybara Production Prefab")]
        public static void BuildFromMenu()
        {
            Build();
        }

        public static void BuildFromCommandLine()
        {
            Build();
        }

        public static void Build()
        {
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport);
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (modelAsset == null)
            {
                throw new InvalidOperationException($"Capybara FBX is missing: {ModelPath}");
            }

            EnsureFolder("Assets/_CookedOut/Art/Resources/Characters");
            EnsureFolder(MaterialFolder);
            ConfigureModelImporter();

            var root = new GameObject("CapybaraChef");
            try
            {
                var motionRoot = new GameObject("Character Motion Root").transform;
                motionRoot.SetParent(root.transform, false);
                var modelInstance = UnityEngine.Object.Instantiate(modelAsset, motionRoot, false);
                modelInstance.name = "CapybaraChef Model";

                var rigRoot = RequireTransform(modelInstance.transform, "root");
                var body = RequireTransform(modelInstance.transform, "body");
                var head = RequireTransform(modelInstance.transform, "head");
                var cap = RequireTransform(modelInstance.transform, "cap");
                var armLeft = RequireTransform(modelInstance.transform, "arm_L");
                var armRight = RequireTransform(modelInstance.transform, "arm_R");
                var footLeft = RequireTransform(modelInstance.transform, "foot_L");
                var footRight = RequireTransform(modelInstance.transform, "foot_R");
                var browLeft = RequireTransform(modelInstance.transform, "brow_L");
                var browRight = RequireTransform(modelInstance.transform, "brow_R");

                var heldAnchor = new GameObject("Held Item Anchor").transform;
                heldAnchor.SetParent(root.transform, false);
                heldAnchor.localPosition = AnimalChefVisual.StandardHeldItemLocalPosition;

                var renderers = modelInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (renderers.Length < 3)
                {
                    throw new InvalidOperationException(
                        $"Expected three skinned LOD meshes, found {renderers.Length}");
                }

                foreach (var importedLodGroup in modelInstance.GetComponentsInChildren<LODGroup>(true))
                {
                    UnityEngine.Object.DestroyImmediate(importedLodGroup);
                }

                foreach (var renderer in renderers)
                {
                    renderer.sharedMaterials = renderer.sharedMaterials
                        .Select(source => LoadOrCreateMaterial(source == null ? "MAT_Cream" : source.name))
                        .ToArray();
                    renderer.updateWhenOffscreen = false;
                }

                var lod0 = RequireRenderer(renderers, "LOD0");
                var lod1 = RequireRenderer(renderers, "LOD1");
                var lod2 = RequireRenderer(renderers, "LOD2");
                var lodGroup = root.AddComponent<LODGroup>();
                lodGroup.SetLODs(new[]
                {
                    new LOD(0.55f, new Renderer[] { lod0 }),
                    new LOD(0.25f, new Renderer[] { lod1 }),
                    new LOD(0.08f, new Renderer[] { lod2 })
                });
                // iOS Metal can expose the dithered transition as a translucent
                // character at this small fixed-camera screen size. Hard swaps keep
                // every production renderer fully opaque on device.
                lodGroup.fadeMode = LODFadeMode.None;
                lodGroup.animateCrossFading = false;
                lodGroup.RecalculateBounds();

                var visual = root.AddComponent<AnimalChefVisual>();
                visual.Initialize(
                    "capybara",
                    motionRoot,
                    body,
                    rigRoot,
                    head,
                    cap,
                    heldAnchor,
                    armLeft,
                    armRight,
                    footLeft,
                    footRight,
                    browLeft,
                    browRight,
                    true);

                if (!visual.TryValidateProductionAsset(out var validationReason))
                {
                    throw new InvalidOperationException(validationReason);
                }

                PrefabUtility.SaveAsPrefabAsset(root, ProductionCharacterAssetValidator.CapybaraPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!ProductionCharacterAssetValidator.TryValidateCapybara(out var errors))
            {
                throw new InvalidOperationException(string.Join("\n", errors));
            }

            Debug.Log($"Built production capybara prefab: {ProductionCharacterAssetValidator.CapybaraPrefabPath}");
        }

        private static void ConfigureModelImporter()
        {
            if (AssetImporter.GetAtPath(ModelPath) is not ModelImporter importer)
            {
                throw new InvalidOperationException("Capybara FBX has no ModelImporter");
            }

            importer.globalScale = 1f;
            importer.importAnimation = false;
            importer.importBlendShapes = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.optimizeGameObjects = false;
            importer.SaveAndReimport();
        }

        private static Material LoadOrCreateMaterial(string sourceName)
        {
            var canonicalName = MaterialColors.Keys.FirstOrDefault(sourceName.StartsWith) ?? "MAT_Cream";
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
                material.SetFloat("_Smoothness", 0.22f);
            }
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 0f);
            }
            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }
            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            }
            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
            }
            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 1f);
            }
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = -1;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static SkinnedMeshRenderer RequireRenderer(
            IEnumerable<SkinnedMeshRenderer> renderers,
            string lodToken)
        {
            var renderer = renderers.FirstOrDefault(candidate => candidate.name.Contains(lodToken));
            return renderer != null
                ? renderer
                : throw new InvalidOperationException($"Missing skinned renderer containing {lodToken}");
        }

        private static Transform RequireTransform(Transform root, string name)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name == name)
                {
                    return transform;
                }
            }

            throw new InvalidOperationException($"Missing rig transform: {name}");
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
