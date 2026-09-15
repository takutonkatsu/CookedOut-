using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace CookedOut.Editor
{
    [InitializeOnLoad]
    public static class ProjectConfigurator
    {
        private const string SettingsFolder = "Assets/_CookedOut/Settings";
        private const string RendererPath = SettingsFolder + "/CookedOut_UniversalRenderer.asset";
        private const string PipelinePath = SettingsFolder + "/CookedOut_URP.asset";

        static ProjectConfigurator()
        {
            EditorApplication.delayCall += ConfigureNow;
        }

        public static void ConfigureNow()
        {
            ConfigurePlayer();
            EnsureUrpAssets();
            AssetDatabase.SaveAssets();
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "CookedOutPrototype";
            PlayerSettings.productName = "COOKED OUT!";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.cookedout.prototype");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS, new[] { GraphicsDeviceType.Metal });
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneOnly;
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;

        }

        private static void EnsureUrpAssets()
        {
            var existing = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PipelinePath);
            if (existing != null)
            {
                GraphicsSettings.defaultRenderPipeline = existing;
                QualitySettings.renderPipeline = existing;
                return;
            }

            if (!AssetDatabase.IsValidFolder(SettingsFolder))
            {
                AssetDatabase.CreateFolder("Assets/_CookedOut", "Settings");
            }

            var rendererType = Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalRendererData, Unity.RenderPipelines.Universal.Runtime");
            var pipelineType = Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset, Unity.RenderPipelines.Universal.Runtime");
            if (rendererType == null || pipelineType == null)
            {
                Debug.LogWarning("URP package is not available yet; pipeline asset setup will retry after package import.");
                return;
            }

            var renderer = ScriptableObject.CreateInstance(rendererType);
            renderer.name = "CookedOut Universal Renderer";
            AssetDatabase.CreateAsset(renderer, RendererPath);

            var pipeline = CreatePipelineAsset(pipelineType, rendererType, renderer);
            pipeline.name = "CookedOut URP";
            var serializedPipeline = new SerializedObject(pipeline);
            var rendererList = serializedPipeline.FindProperty("m_RendererDataList");
            if (rendererList != null)
            {
                rendererList.arraySize = 1;
                rendererList.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            }

            var defaultRenderer = serializedPipeline.FindProperty("m_DefaultRendererIndex");
            if (defaultRenderer != null)
            {
                defaultRenderer.intValue = 0;
            }

            serializedPipeline.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(pipeline, PipelinePath);
            var renderPipelineAsset = pipeline as RenderPipelineAsset;
            GraphicsSettings.defaultRenderPipeline = renderPipelineAsset;
            QualitySettings.renderPipeline = renderPipelineAsset;
            EditorUtility.SetDirty(pipeline);
            Debug.Log("Cooked Out URP assets created and assigned.");
        }

        private static ScriptableObject CreatePipelineAsset(Type pipelineType, Type rendererType, ScriptableObject renderer)
        {
            foreach (var method in pipelineType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                var parameters = method.GetParameters();
                if (method.Name != "Create" || parameters.Length != 1 ||
                    !parameters[0].ParameterType.IsAssignableFrom(rendererType))
                {
                    continue;
                }

                try
                {
                    var created = method.Invoke(null, new object[] { renderer }) as ScriptableObject;
                    if (created != null)
                    {
                        return created;
                    }
                }
                catch (TargetInvocationException)
                {
                    break;
                }
            }

            return ScriptableObject.CreateInstance(pipelineType);
        }
    }

    public static class IosBuildCommand
    {
        public static void BuildDevelopment()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                              ?? throw new InvalidOperationException("Project root was not found.");
            var outputPath = Path.Combine(projectRoot, "Builds/iOS");
            Directory.CreateDirectory(outputPath);

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
            var options = new BuildPlayerOptions
            {
                scenes = new[]
                {
                    "Assets/_CookedOut/Scenes/Home.unity",
                    "Assets/_CookedOut/Scenes/Tutorial_1_1.unity",
                    "Assets/_CookedOut/Scenes/Tutorial_1_2.unity",
                    "Assets/_CookedOut/Scenes/Tutorial_1_3.unity",
                    "Assets/_CookedOut/Scenes/Tutorial_1_4.unity",
                    "Assets/_CookedOut/Scenes/Tutorial_1_5.unity",
                    "Assets/_CookedOut/Scenes/Tutorial_1_6.unity",
                    "Assets/_CookedOut/Scenes/Tutorial_1_7.unity",
                    "Assets/_CookedOut/Scenes/Tutorial_1_8.unity"
                },
                locationPathName = outputPath,
                target = BuildTarget.iOS,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new InvalidOperationException("iOS build failed: " + report.summary.result);
            }

            Debug.Log("iOS Xcode project generated at " + outputPath);
        }
    }
}
