using UnityEditor;
using UnityEngine;

namespace CookedOut.Editor
{
    /// <summary>Author native clips on a visual-only pivot; item placement stays authoritative.</summary>
    public static class OnionReferenceAnimationBuilder
    {
        public const string Folder = "Assets/_CookedOut/Art/KitchenProduction/Animations";
        private const string PivotPath = "Onion Presentation";

        public static void Configure(GameObject root, Transform model)
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/_CookedOut/Art/KitchenProduction", "Animations");

            var pivot = new GameObject(PivotPath).transform;
            pivot.SetParent(root.transform, false);
            model.SetParent(pivot, false);

            var settle = LoadOrCreate("OnionSettle");
            settle.ClearCurves();
            SetCurve(settle, "localScale.x", Curve(1f, 1.04f, 0.98f, 1f));
            SetCurve(settle, "localScale.y", Curve(1f, 0.92f, 1.04f, 1f));
            SetCurve(settle, "localScale.z", Curve(1f, 1.04f, 0.98f, 1f));
            SetCurve(settle, "localEulerAnglesRaw.z", Curve(0f, -2f, 1f, 0f));
            settle.wrapMode = WrapMode.Once;
            EditorUtility.SetDirty(settle);

            var turntable = LoadOrCreate("OnionTurntable");
            turntable.ClearCurves();
            SetCurve(turntable, "localEulerAnglesRaw.y", AnimationCurve.Linear(0f, 0f, 4f, 360f));
            turntable.wrapMode = WrapMode.Loop;
            EditorUtility.SetDirty(turntable);

            var animation = root.AddComponent<Animation>();
            animation.AddClip(settle, "OnionSettle");
            animation.AddClip(turntable, "OnionTurntable");
            animation.clip = settle;
            animation.playAutomatically = true;
            // A short one-shot must finish even if created outside the camera,
            // otherwise the onion can retain its squashed pose when picked up.
            animation.cullingType = AnimationCullingType.AlwaysAnimate;
        }

        private static AnimationClip LoadOrCreate(string name)
        {
            var path = $"{Folder}/{name}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { name = name };
                AssetDatabase.CreateAsset(clip, path);
            }
            clip.legacy = true;
            clip.frameRate = 30f;
            return clip;
        }

        private static AnimationCurve Curve(float start, float squash, float rebound, float end)
        {
            var curve = new AnimationCurve(new Keyframe(0f, start), new Keyframe(4f/30f, squash),
                new Keyframe(10f/30f, rebound), new Keyframe(0.6f, end));
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
            }
            return curve;
        }

        private static void SetCurve(AnimationClip clip, string property, AnimationCurve curve)
        {
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(PivotPath, typeof(Transform), property), curve);
        }
    }
}
