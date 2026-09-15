using System.Collections.Generic;
using CookedOut.Presentation;
using UnityEditor;
using UnityEngine;

namespace CookedOut.Editor
{
    public static class ProductionCharacterAssetValidator
    {
        public const string CapybaraPrefabPath =
            "Assets/_CookedOut/Art/Resources/Characters/CapybaraChef.prefab";

        [MenuItem("COOKED OUT!/Validate Production Character Assets")]
        public static void ValidateFromMenu()
        {
            if (TryValidateCapybara(out var errors))
            {
                Debug.Log("COOKED OUT! production capybara character is valid.");
                return;
            }

            Debug.LogWarning("COOKED OUT! production character is not ready:\n" + string.Join("\n", errors));
        }

        public static bool TryValidateCapybara(out IReadOnlyList<string> errors)
        {
            var failures = new List<string>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CapybaraPrefabPath);
            if (prefab == null)
            {
                failures.Add($"Missing prefab: {CapybaraPrefabPath}");
                errors = failures;
                return false;
            }

            var visual = prefab.GetComponentInChildren<AnimalChefVisual>(true);
            if (visual == null)
            {
                failures.Add("Prefab requires an AnimalChefVisual component");
            }
            else
            {
                if (visual.SpeciesId != "capybara")
                {
                    failures.Add("AnimalChefVisual speciesId must be capybara");
                }

                if (!visual.TryValidateProductionAsset(out var reason))
                {
                    failures.Add(reason);
                }
            }

            if (prefab.GetComponentInChildren<SkinnedMeshRenderer>(true) == null)
            {
                failures.Add("Prefab requires at least one SkinnedMeshRenderer");
            }

            if (prefab.GetComponentInChildren<LODGroup>(true) == null)
            {
                failures.Add("Prefab requires an LODGroup");
            }

            errors = failures;
            return failures.Count == 0;
        }
    }
}
