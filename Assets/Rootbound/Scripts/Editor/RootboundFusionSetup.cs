#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using Fusion;
using Rootbound.Fusion;

namespace Rootbound.EditorTools
{
    public static class RootboundFusionSetup
    {
        private const string Folder = "Assets/Rootbound/Prefabs";
        private const string Path = Folder + "/FusionMatch.prefab";
        private const string FusionPrefabTag = "FusionPrefab";

        [MenuItem("Rootbound/Build Fusion Match Prefab")]
        public static void BuildMatchPrefab()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/Rootbound", "Prefabs");

            GameObject go = new GameObject("FusionMatch");
            go.AddComponent<NetworkObject>();
            go.AddComponent<FusionCombatHost>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, Path);
            Object.DestroyImmediate(go);

            AssetDatabase.SetLabels(prefab, new[] { FusionPrefabTag });
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(Path, ImportAssetOptions.ForceUpdate);
            Debug.Log("[Rootbound] Fusion match prefab created and labeled: " + Path);
        }

        private const string FusionEditorSource = "Assets/Photon/Fusion/Editor/Fusion.Unity.Editor.cs";
        private const string FusionMppmPatchMarker = "ROOTBOUND_PATCH(FusionInstaller-MPPM)";

        [MenuItem("Rootbound/Verify Fusion MPPM Patch")]
        public static void VerifyFusionMppmPatch()
        {
            if (!File.Exists(FusionEditorSource))
            {
                Debug.LogError("[Rootbound] Fusion editor source not found: " + FusionEditorSource);
                return;
            }

            if (File.ReadAllText(FusionEditorSource).Contains(FusionMppmPatchMarker))
                Debug.Log("[Rootbound] Fusion MPPM installer patch present.");
            else
                Debug.LogError("[Rootbound] Fusion MPPM installer patch MISSING; reapply docs/patches/fusion-installer-mppm.patch after SDK updates.");
        }
    }
}
#endif
