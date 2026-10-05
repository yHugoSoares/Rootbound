#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Rootbound.Core;
using Rootbound.Unity;

namespace Rootbound.EditorTools
{
    public static class RootboundSceneBuilder
    {
        private const string RootFolder = "Assets/Rootbound";
        private const string DataFolder = RootFolder + "/Data";
        private const string ScenesFolder = RootFolder + "/Scenes";
        private const string ScenePath = ScenesFolder + "/CombatArena.unity";

        [MenuItem("Rootbound/Build Milestone 1 Content")]
        public static void BuildContent()
        {
            EnsureFolder(RootFolder, "Data");
            CreateCreature("RootGuardian", DefaultContent.RootGuardian());
            CreateCreature("EmberMoth", DefaultContent.EmberMoth());
            CreateEnemy("Blightling", DefaultContent.Blightling());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Rootbound] Definition assets written to " + DataFolder);
        }

        [MenuItem("Rootbound/Setup Project and Create Arena Scene")]
        public static void SetupAndCreateArenaScene()
        {
            RootboundProjectConfigurator.ConfigureAll();
            CreateArenaScene();
        }

        [MenuItem("Rootbound/Create Arena Scene")]
        public static void CreateArenaScene()
        {
            RootboundProjectConfigurator.ConfigureUrp();
            BuildContent();
            EnsureFolder(RootFolder, "Scenes");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraGo = new GameObject("Arena Camera");
            cameraGo.tag = "MainCamera";
            Camera camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 11f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.07f, 0.09f);
            cameraGo.AddComponent<AudioListener>();
            cameraGo.transform.position = new Vector3(0f, 20f, -20f);
            cameraGo.transform.rotation = Quaternion.Euler(35f, 45f, 0f);

            GameObject lightGo = new GameObject("Directional Light");
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Arena Ground";
            ground.transform.localScale = new Vector3(4f, 1f, 4f);
            ground.GetComponent<Renderer>().sharedMaterial = CreateGroundMaterial();

            GameObject arena = new GameObject("Arena");
            LocalGameRunner runner = arena.AddComponent<LocalGameRunner>();
            CombatView view = arena.AddComponent<CombatView>();
            CombatHud hud = arena.AddComponent<CombatHud>();
            IsometricCameraRig rig = arena.AddComponent<IsometricCameraRig>();
            RootboundMenu menu = arena.AddComponent<RootboundMenu>();

            rig.targetCamera = camera;
            runner.arenaCamera = camera;
            runner.view = view;
            runner.hud = hud;
            runner.cameraRig = rig;
            runner.player0Definition = LoadCreature("RootGuardian");
            runner.player1Definition = LoadCreature("EmberMoth");
            runner.enemyDefinition = LoadEnemy("Blightling");
            menu.runner = runner;

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            Debug.Log("[Rootbound] Arena scene created at " + ScenePath);
        }

        private static Material CreateGroundMaterial()
        {
            string path = DataFolder + "/GroundPlaceholder.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = PlaceholderVisuals.CreateMaterial(new Color(0.16f, 0.14f, 0.10f));
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                Color sand = new Color(0.16f, 0.14f, 0.10f);
                material.color = sand;
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", sand);
                EditorUtility.SetDirty(material);
            }
            return material;
        }

        private static void CreateCreature(string name, CreatureSpec spec)
        {
            string path = DataFolder + "/" + name + ".asset";
            CreatureDefinitionAsset asset = AssetDatabase.LoadAssetAtPath<CreatureDefinitionAsset>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CreatureDefinitionAsset>();
                AssetDatabase.CreateAsset(asset, path);
            }
            asset.spec = spec;
            EditorUtility.SetDirty(asset);
        }

        private static void CreateEnemy(string name, EnemySpec spec)
        {
            string path = DataFolder + "/" + name + ".asset";
            EnemyDefinitionAsset asset = AssetDatabase.LoadAssetAtPath<EnemyDefinitionAsset>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<EnemyDefinitionAsset>();
                AssetDatabase.CreateAsset(asset, path);
            }
            asset.spec = spec;
            EditorUtility.SetDirty(asset);
        }

        private static CreatureDefinitionAsset LoadCreature(string name)
        {
            return AssetDatabase.LoadAssetAtPath<CreatureDefinitionAsset>(DataFolder + "/" + name + ".asset");
        }

        private static EnemyDefinitionAsset LoadEnemy(string name)
        {
            return AssetDatabase.LoadAssetAtPath<EnemyDefinitionAsset>(DataFolder + "/" + name + ".asset");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string full = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(full))
                AssetDatabase.CreateFolder(parent, child);
        }

        private static void AddSceneToBuildSettings(string path)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
                if (scenes[i].path == path) return;

            EditorBuildSettingsScene[] next = new EditorBuildSettingsScene[scenes.Length + 1];
            System.Array.Copy(scenes, next, scenes.Length);
            next[scenes.Length] = new EditorBuildSettingsScene(path, true);
            EditorBuildSettings.scenes = next;
        }
    }
}
#endif
