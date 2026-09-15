using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace CookedOut.Editor
{
    /// <summary>Render the shipping prefab and native clips without opening or saving user scenes.</summary>
    public static class OnionReferencePreview
    {
        public const string SourceCardAssetPath =
            "Assets/_CookedOut/Art/Resources/IngredientSourceCards/ingredient_onion_source_card_sv3.png";

        [MenuItem("COOKED OUT!/Capture Reference Onion Preview")]
        public static void Capture()
        {
            CaptureInternal(false);
        }

        [MenuItem("COOKED OUT!/Build Reference Onion Source Card")]
        public static void BuildSourceCard()
        {
            CaptureInternal(true);
            File.Copy("docs/art/concepts/ingredient_onion_source_card_sv3_unity_v1.png", SourceCardAssetPath, true);
            AssetDatabase.ImportAsset(SourceCardAssetPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(SourceCardAssetPath);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 512;
            importer.SaveAndReimport();
            Debug.Log("Built onion source card sv3 from the shipping prefab; sv2 remains unchanged.");
        }

        private static void CaptureInternal(bool sourceCardOnly)
        {
            var output = Path.GetFullPath("docs/art/concepts");
            var frames = Path.GetFullPath("outputs/onion-reference/frames");
            Directory.CreateDirectory(output);
            Directory.CreateDirectory(frames);
            var previousScene = SceneManager.GetActiveScene();
            var reviewScene = EditorApplication.isPlaying
                ? SceneManager.CreateScene("Onion Reference Capture")
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            Material groundMaterial = null;
            var existingLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            var lightStates = new bool[existingLights.Length];
            for (var index = 0; index < existingLights.Length; index++) lightStates[index] = existingLights[index].enabled;
            try
            {
                // A Camera.scene override is not sufficient to isolate runtime
                // cameras in URP. Reserve a layer and restore every existing light
                // before the next editor/game frame can render.
                foreach (var light in existingLights) light.enabled = false;
                SceneManager.SetActiveScene(reviewScene);
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.38f, 0.36f, 0.33f);
                // Manual same-frame captures cannot wait for Unity's ambient-probe refresh.
                var ambientProbe = new SphericalHarmonicsL2();
                ambientProbe.AddAmbientLight(new Color(0.32f, 0.30f, 0.27f));
                RenderSettings.ambientProbe = ambientProbe;
                RenderSettings.skybox = null;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_CookedOut/Art/Resources/KitchenProduction/OnionRaw.prefab");
                var instance = Object.Instantiate(prefab);
                foreach (var child in instance.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
                var camera = new GameObject("Onion Review Camera").AddComponent<Camera>();
                camera.scene = reviewScene;
                camera.cullingMask = 1 << 31;
                camera.orthographic = true;
                camera.orthographicSize = 0.64f;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 20f;
                camera.transform.position = new Vector3(1.5f, 1.6f, -4f);
                camera.transform.LookAt(new Vector3(0f, 0.47f, 0f));
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.94f, 0.915f, 0.88f);
                var key = new GameObject("Onion Review Key").AddComponent<Light>();
                key.type = LightType.Directional;
                key.intensity = 0.80f;
                key.transform.rotation = Quaternion.Euler(40f,25f,0f);
                key.shadows = LightShadows.Soft;
                key.shadowStrength = 0.35f;
                RenderSettings.sun = key;
                var fill = new GameObject("Onion Review Fill").AddComponent<Light>();
                fill.type = LightType.Point;
                fill.transform.position = new Vector3(1.6f,0.8f,-2.5f);
                fill.range = 10f;
                fill.intensity = 4f;
                fill.shadows = LightShadows.None;
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.layer = 31;
                Object.DestroyImmediate(ground.GetComponent<Collider>());
                ground.transform.position = Vector3.down * 0.005f;
                groundMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                groundMaterial.color = new Color(0.88f,0.855f,0.82f);
                ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;
                var animation = instance.GetComponent<Animation>();
                animation.playAutomatically = false;
                animation.Stop();
                var settle = animation.GetClip("OnionSettle");
                var turntable = animation.GetClip("OnionTurntable");
                settle.SampleAnimation(instance, 0.6f);
                if (sourceCardOnly)
                {
                    // Match the clean warm-white card backdrop. No kitchen floor
                    // or long cast shadow may enter the circular ingredient badge.
                    ground.SetActive(false);
                    Save(camera, Path.Combine(output,"ingredient_onion_source_card_sv3_unity_v1.png"),512);
                    return;
                }
                Save(camera, Path.Combine(output,"onion_reference_sv1_unity_front_v1.png"),768);
                // 0.6 seconds of settle, a short hold, then one complete 4 second turn.
                for (var frame = 0; frame < 75; frame++)
                {
                    var time = frame / 15f;
                    settle.SampleAnimation(instance, Mathf.Min(time, 0.6f));
                    turntable.SampleAnimation(instance, Mathf.Max(0f, time-1f));
                    Save(camera, Path.Combine(frames,$"onion_{frame:D3}.png"),384);
                }
                Debug.Log("Captured shipping onion prefab and both native animation clips: " + output);
            }
            finally
            {
                SceneManager.SetActiveScene(previousScene);
                if (EditorApplication.isPlaying) SceneManager.UnloadSceneAsync(reviewScene);
                else EditorSceneManager.CloseScene(reviewScene, true);
                if (groundMaterial != null) Object.DestroyImmediate(groundMaterial);
                for (var index = 0; index < existingLights.Length; index++)
                    if (existingLights[index] != null) existingLights[index].enabled = lightStates[index];
            }
        }

        private static void Save(Camera camera, string path, int size)
        {
            var target = RenderTexture.GetTemporary(size,size,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active;
            var image = new Texture2D(size,size,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0,0,size,size),0,0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Object.DestroyImmediate(image);
            }
        }
    }
}
