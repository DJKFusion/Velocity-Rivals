using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace VelocityRivals.Editor
{
    [InitializeOnLoad]
    public static class ProjectBuilder
    {
        public const string EntryScene = "Assets/Scenes/VelocityRivals.unity";
        private const string PipelinePath = "Assets/Settings/VelocityURP.asset";
        static ProjectBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && !BuildPipeline.isBuildingPlayer)
                    ConfigureRendering();
            };
        }

        [MenuItem("Velocity Rivals/Configure Rendering")]
        public static void ConfigureRendering()
        {
            Directory.CreateDirectory("Assets/Settings");
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (!pipeline)
            {
                var data = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(data, "Assets/Settings/VelocityRenderer.asset");
                pipeline = UniversalRenderPipelineAsset.Create(data);
                pipeline.name = "Velocity URP";
                pipeline.supportsHDR = true;
                pipeline.msaaSampleCount = 4;
                pipeline.shadowDistance = 100;
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            // Runtime-created materials use Shader.Find: retain the Lit shader in player builds.
            Directory.CreateDirectory("Assets/Resources");
            if (!AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/VelocitySurface.mat"))
            {
                var surface = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                surface.EnableKeyword("_EMISSION");
                surface.SetColor("_EmissionColor", Color.cyan * 3);
                AssetDatabase.CreateAsset(surface, "Assets/Resources/VelocitySurface.mat");
            }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Velocity Rivals/Open Game")]
        public static void OpenGame()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(EntryScene);
        }

        [MenuItem("Velocity Rivals/Regenerate Entry Scene and Vehicle Assets")]
        public static void Generate()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ConfigureRendering();
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory("Assets/Prefabs");
            Directory.CreateDirectory("Assets/Resources/VehicleStats");
            Directory.CreateDirectory("Assets/Materials/Generated");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Velocity Rivals").AddComponent<GameBootstrap>();
            EditorSceneManager.SaveScene(scene, EntryScene);
            for (int i = 0; i < GameSession.VehicleNames.Length; i++)
            {
                string name = GameSession.VehicleNames[i];
                string statsPath = "Assets/Resources/VehicleStats/" + name + ".asset";
                var stats = AssetDatabase.LoadAssetAtPath<VehicleStats>(statsPath);
                if (!stats)
                {
                    stats = ScriptableObject.CreateInstance<VehicleStats>();
                    stats.DisplayName = name;
                    stats.MaxSpeed = 35 + i * 2;
                    stats.Acceleration = 27 - i;
                    AssetDatabase.CreateAsset(stats, statsPath);
                }
                var car = VehicleController.Spawn(name, Vector3.zero, Quaternion.identity, stats, Color.cyan, false);
                int materialIndex = 0;
                foreach (var renderer in car.GetComponentsInChildren<Renderer>(true))
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material == null || EditorUtility.IsPersistent(material)) continue;
                        string materialPath = AssetDatabase.GenerateUniqueAssetPath("Assets/Materials/Generated/" + name + "_" + materialIndex++ + ".mat");
                        AssetDatabase.CreateAsset(material, materialPath);
                    }
                PrefabUtility.SaveAsPrefabAsset(car.gameObject, "Assets/Prefabs/" + name + ".prefab");
                UnityEngine.Object.DestroyImmediate(car.gameObject);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(EntryScene, true) };
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Velocity Rivals/Build Windows")]
        public static void BuildWindows()
        {
            ConfigureRendering();
            string output = Environment.GetEnvironmentVariable("VELOCITY_BUILD_PATH");
            if (string.IsNullOrEmpty(output)) output = "Builds/Windows/VelocityRivals.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { EntryScene },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Windows build failed: " + report.summary.result);
            Debug.Log("Windows build succeeded: " + output);
        }
    }
}
