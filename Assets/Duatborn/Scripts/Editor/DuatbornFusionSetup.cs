#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using Fusion;
using Duatborn.Fusion;

namespace Duatborn.EditorTools
{
    public static class DuatbornFusionSetup
    {
        private const string Folder = "Assets/Duatborn/Prefabs";
        private const string Path = Folder + "/FusionMatch.prefab";
        private const string FusionPrefabTag = "FusionPrefab";

        [MenuItem("Duatborn/Build Fusion Match Prefab")]
        public static void BuildMatchPrefab()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/Duatborn", "Prefabs");

            GameObject go = new GameObject("FusionMatch");
            go.AddComponent<NetworkObject>();
            go.AddComponent<FusionCombatHost>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, Path);
            Object.DestroyImmediate(go);

            AssetDatabase.SetLabels(prefab, new[] { FusionPrefabTag });
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(Path, ImportAssetOptions.ForceUpdate);
            Debug.Log("[Duatborn] Fusion match prefab created and labeled: " + Path);
        }

        private const string FusionEditorSource = "Assets/Photon/Fusion/Editor/Fusion.Unity.Editor.cs";
        private const string FusionMppmPatchMarker = "ROOTBOUND_PATCH(FusionInstaller-MPPM)";

        [MenuItem("Duatborn/Verify Fusion MPPM Patch")]
        public static void VerifyFusionMppmPatch()
        {
            if (!File.Exists(FusionEditorSource))
            {
                Debug.LogError("[Duatborn] Fusion editor source not found: " + FusionEditorSource);
                return;
            }

            if (File.ReadAllText(FusionEditorSource).Contains(FusionMppmPatchMarker))
                Debug.Log("[Duatborn] Fusion MPPM installer patch present.");
            else
                Debug.LogError("[Duatborn] Fusion MPPM installer patch MISSING; reapply docs/patches/fusion-installer-mppm.patch after SDK updates.");
        }
    }
}
#endif
