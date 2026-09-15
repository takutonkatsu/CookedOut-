using UnityEngine;

namespace CookedOut.Presentation
{
    public static class CharacterVisualFactory
    {
        public const string CapybaraResourcePath = "Characters/CapybaraChef";

        public static string ResourcePathFor(string speciesId)
        {
            return speciesId == "capybara" ? CapybaraResourcePath : $"Characters/{speciesId}";
        }

        public static bool TryInstantiateProduction(
            string speciesId,
            Transform parent,
            out AnimalChefVisual visual,
            out string reason)
        {
            visual = null;
            reason = string.Empty;
            var path = ResourcePathFor(speciesId);
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                reason = $"Production character prefab is missing at Resources/{path}";
                return false;
            }

            var instance = Object.Instantiate(prefab, parent, false);
            instance.name = prefab.name;
            visual = instance.GetComponentInChildren<AnimalChefVisual>(true);
            visual?.NormalizeHeldItemAnchor();
            if (visual != null && visual.SpeciesId == speciesId && visual.TryValidateProductionAsset(out reason))
            {
                return true;
            }

            if (visual == null)
            {
                reason = "Production character prefab has no AnimalChefVisual bindings";
            }
            else if (visual.SpeciesId != speciesId)
            {
                reason = $"Expected species {speciesId}, found {visual.SpeciesId}";
            }

            Object.Destroy(instance);
            visual = null;
            return false;
        }
    }
}
