using UnityEngine;

namespace CookedOut.Presentation
{
    public sealed class AnimalChefVisual : MonoBehaviour
    {
        public static Vector3 StandardHeldItemLocalPosition => new Vector3(0f, 0.72f, 0.68f);

        [SerializeField] private string speciesId;
        [SerializeField] private bool productionAsset;
        [SerializeField] private Transform motionRoot;
        [SerializeField] private Transform commonUniformRoot;
        [SerializeField] private Transform speciesBodyRoot;
        [SerializeField] private Transform speciesHeadRoot;
        [SerializeField] private Transform capRoot;
        [SerializeField] private Transform heldItemAnchor;
        [SerializeField] private Transform leftArmPivot;
        [SerializeField] private Transform rightArmPivot;
        [SerializeField] private Transform leftFootPivot;
        [SerializeField] private Transform rightFootPivot;
        [SerializeField] private Transform leftBrow;
        [SerializeField] private Transform rightBrow;

        public string SpeciesId => speciesId;
        public bool IsProductionAsset => productionAsset;
        public Transform MotionRoot => motionRoot;
        public Transform CommonUniformRoot => commonUniformRoot;
        public Transform SpeciesBodyRoot => speciesBodyRoot;
        public Transform SpeciesHeadRoot => speciesHeadRoot;
        public Transform CapRoot => capRoot;
        public Transform HeldItemAnchor => heldItemAnchor;
        public Transform LeftArmPivot => leftArmPivot;
        public Transform RightArmPivot => rightArmPivot;
        public Transform LeftFootPivot => leftFootPivot;
        public Transform RightFootPivot => rightFootPivot;
        public Transform LeftBrow => leftBrow;
        public Transform RightBrow => rightBrow;

        public void NormalizeHeldItemAnchor()
        {
            if (heldItemAnchor == null)
            {
                return;
            }

            heldItemAnchor.localPosition = StandardHeldItemLocalPosition;
            heldItemAnchor.localRotation = Quaternion.identity;
            heldItemAnchor.localScale = Vector3.one;
        }

        public void Initialize(
            string speciesId,
            Transform motionRoot,
            Transform commonUniformRoot,
            Transform speciesBodyRoot,
            Transform speciesHeadRoot,
            Transform capRoot,
            Transform heldItemAnchor,
            Transform leftArmPivot,
            Transform rightArmPivot,
            Transform leftFootPivot,
            Transform rightFootPivot,
            Transform leftBrow,
            Transform rightBrow,
            bool isProductionAsset = false)
        {
            this.speciesId = speciesId;
            productionAsset = isProductionAsset;
            this.motionRoot = motionRoot;
            this.commonUniformRoot = commonUniformRoot;
            this.speciesBodyRoot = speciesBodyRoot;
            this.speciesHeadRoot = speciesHeadRoot;
            this.capRoot = capRoot;
            this.heldItemAnchor = heldItemAnchor;
            this.leftArmPivot = leftArmPivot;
            this.rightArmPivot = rightArmPivot;
            this.leftFootPivot = leftFootPivot;
            this.rightFootPivot = rightFootPivot;
            this.leftBrow = leftBrow;
            this.rightBrow = rightBrow;
        }

        public bool TryValidateBindings(out string reason)
        {
            if (string.IsNullOrWhiteSpace(speciesId))
            {
                reason = "Species id is required";
                return false;
            }

            var required = new[]
            {
                motionRoot,
                commonUniformRoot,
                speciesBodyRoot,
                speciesHeadRoot,
                capRoot,
                heldItemAnchor,
                leftArmPivot,
                rightArmPivot,
                leftFootPivot,
                rightFootPivot,
                leftBrow,
                rightBrow
            };
            foreach (var binding in required)
            {
                if (binding == null)
                {
                    reason = "All character rig bindings are required";
                    return false;
                }

                if (binding != transform && !binding.IsChildOf(transform))
                {
                    reason = $"Binding {binding.name} must belong to {name}";
                    return false;
                }
            }

            if (!commonUniformRoot.IsChildOf(motionRoot) ||
                !speciesBodyRoot.IsChildOf(motionRoot) ||
                !speciesHeadRoot.IsChildOf(motionRoot))
            {
                reason = "Visible body roots must be children of the motion root";
                return false;
            }

            if (heldItemAnchor == motionRoot || heldItemAnchor.IsChildOf(motionRoot))
            {
                reason = "Held item anchor must stay outside the animated motion root";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public bool TryValidateProductionAsset(out string reason)
        {
            if (!TryValidateBindings(out reason))
            {
                return false;
            }

            if (!productionAsset)
            {
                reason = "Character is marked as a graybox asset";
                return false;
            }

            if (GetComponentInChildren<SkinnedMeshRenderer>(true) == null)
            {
                reason = "Production character requires a SkinnedMeshRenderer";
                return false;
            }

            if (GetComponentInChildren<LODGroup>(true) == null)
            {
                reason = "Production character requires an LODGroup";
                return false;
            }

            var lodGroup = GetComponentInChildren<LODGroup>(true);
            if (lodGroup.fadeMode != LODFadeMode.None)
            {
                reason = "Production character LOD transitions must remain opaque";
                return false;
            }

            foreach (var renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!renderer.enabled || renderer.forceRenderingOff)
                {
                    reason = "Production character renderers must remain enabled";
                    return false;
                }

                foreach (var material in renderer.sharedMaterials)
                {
                    var colorProperty = material != null && material.HasProperty("_BaseColor")
                        ? "_BaseColor"
                        : "_Color";
                    if (material == null ||
                        (material.HasProperty(colorProperty) && material.GetColor(colorProperty).a < 0.999f) ||
                        (material.HasProperty("_Surface") && material.GetFloat("_Surface") > 0.001f))
                    {
                        reason = "Production character materials must be fully opaque";
                        return false;
                    }
                }
            }

            reason = string.Empty;
            return true;
        }
    }
}
