#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Rootbound.EditorTools
{
    public static class RootboundProjectConfigurator
    {
        private const string SettingsFolder = "Assets/Rootbound/Settings";
        private const string UrpPackagePath = "Packages/com.unity.render-pipelines.universal";
        private const string DefaultPostProcessDataPath = "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset";

        [MenuItem("Rootbound/Configure URP and Input")]
        public static void ConfigureAll()
        {
            ConfigureUrp();
            ConfigureInputBackend();
        }

        public static void ConfigureUrp()
        {
            EnsureFolder("Assets/Rootbound", "Settings");

            string rendererPath = SettingsFolder + "/RootboundUniversalRenderer.asset";
            string pipelinePath = SettingsFolder + "/RootboundUrpAsset.asset";

            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                rendererData.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(DefaultPostProcessDataPath);
                AssetDatabase.CreateAsset(rendererData, rendererPath);
                ResourceReloader.ReloadAllNullIn(rendererData, UrpPackagePath);
            }

            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            AssetDatabase.SaveAssets();
            Debug.Log("[Rootbound] URP configured: " + pipelinePath);
        }

        public static void ConfigureInputBackend()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0) return;
            SerializedObject settings = new SerializedObject(assets[0]);
            SerializedProperty handler = settings.FindProperty("activeInputHandler");
            if (handler == null) return;
            if (handler.intValue != 2)
            {
                int previous = handler.intValue;
                handler.intValue = 2;
                settings.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                Debug.Log("[Rootbound] Active Input Handling set to Both (was " + previous + "). Restart the editor for the new backend to take effect.");
            }
            else
            {
                Debug.Log("[Rootbound] Active Input Handling already set to Both.");
            }
        }

        private static void EnsureFolder(string parent, string child)
        {
            string full = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(full))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
#endif
