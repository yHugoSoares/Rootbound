#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Fusion.Photon.Realtime;

namespace Rootbound.EditorTools
{
    public static class RootboundBuild
    {
        public static void PerformBuild()
        {
            InjectPhotonAppId();

            string targetName = Environment.GetEnvironmentVariable("DUATBORN_BUILD_TARGET");
            if (string.IsNullOrEmpty(targetName)) targetName = "StandaloneOSX";

            BuildTarget target;
            try
            {
                target = (BuildTarget)Enum.Parse(typeof(BuildTarget), targetName);
            }
            catch
            {
                Debug.LogError("[Rootbound] Unknown build target: " + targetName);
                EditorApplication.Exit(1);
                return;
            }

            string dir = "build/" + targetName;
            string output = target == BuildTarget.StandaloneOSX
                ? dir + "/Duatborn.app"
                : dir + "/Duatborn.exe";

            string[] scenes = new string[EditorBuildSettings.scenes.Length];
            int count = 0;
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                if (scene.enabled) scenes[count++] = scene.path;
            Array.Resize(ref scenes, count);

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = target,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError("[Rootbound] Build failed: " + report.summary.result);
                EditorApplication.Exit(1);
            }
            else
            {
                Debug.Log("[Rootbound] Build succeeded: " + output);
                EditorApplication.Exit(0);
            }
        }

        private static void InjectPhotonAppId()
        {
            string appId = Environment.GetEnvironmentVariable("PHOTON_APP_ID");
            if (string.IsNullOrEmpty(appId)) return;

            PhotonAppSettings settings = PhotonAppSettings.Global;
            if (settings == null || settings.AppSettings == null)
            {
                Debug.LogWarning("[Rootbound] PhotonAppSettings.Global unavailable; skipping App ID injection.");
                return;
            }

            settings.AppSettings.AppIdFusion = appId;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("[Rootbound] Injected Photon App ID from PHOTON_APP_ID.");
        }
    }
}
#endif
