using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
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
        [Serializable] private sealed class BuildArtifact { public string package, executable, version; }
        public const string EntryScene = "Assets/Scenes/VelocityRivals.unity";
        private const string PipelinePath = "Assets/Settings/VelocityURP.asset";
        static ProjectBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && !BuildPipeline.isBuildingPlayer)
                {
                    ConfigureRendering();
                    // Only replace the untouched, unnamed default scene on the first import.
                    var scene = SceneManager.GetActiveScene();
                    if (!Application.isBatchMode && scene.IsValid() && string.IsNullOrEmpty(scene.path) && !scene.isDirty && scene.rootCount <= 2 && File.Exists(EntryScene))
                        EditorSceneManager.OpenScene(EntryScene);
                }
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
            var retained = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/VelocitySurface.mat");
            retained.SetTexture("_BumpMap", Resources.Load<Texture2D>("SurfaceTextures/AsphaltNormal"));
            retained.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(retained);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Velocity Rivals/Open Game")]
        public static void OpenGame()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(EntryScene);
        }

        [MenuItem("Velocity Rivals/Play Game")]
        public static void PlayGame()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(EntryScene);
            EditorApplication.isPlaying = true;
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
            string output = BuildWindowsPlayer();
            if (output == null) return;
            Debug.Log("Windows build succeeded: " + output);
            if (!Application.isBatchMode) EditorUtility.RevealInFinder(output);
        }

        [MenuItem("Velocity Rivals/Build Windows ZIP")]
        public static void BuildWindowsPackage()
        {
            string output = BuildWindowsPlayer();
            if (output == null) return;
            string name = "VelocityRivals-SinglePlayer-" + SinglePlayerBuildGuard.Version + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".zip";
            string archive = Path.GetFullPath(Path.Combine("Builds/Packages", name));
            WindowsArchive.Create(output, archive);
            using (var sha = System.Security.Cryptography.SHA256.Create())
            using (var stream = File.OpenRead(archive))
                File.WriteAllText(archive + ".sha256", BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant() + "  " + Path.GetFileName(archive) + "\n");
            Directory.CreateDirectory("Artifacts");
            File.WriteAllText("Artifacts/latest-singleplayer-package.json", JsonUtility.ToJson(new BuildArtifact { package = archive, executable = output, version = SinglePlayerBuildGuard.Version }, true));
            Debug.Log("Windows game package ready: " + archive);
            if (!Application.isBatchMode) EditorUtility.RevealInFinder(archive);
        }

        private static string BuildWindowsPlayer()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before building the game.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                throw new InvalidOperationException("Install Windows Build Support for this Unity Editor through Unity Hub → Installs → gear menu → Add modules.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return null;
            if (!File.Exists(EntryScene)) throw new FileNotFoundException("The game entry scene is missing.", EntryScene);
            ConfigureRendering();
            string output = Environment.GetEnvironmentVariable("VELOCITY_BUILD_PATH");
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Branding/VelocityRivalsLogo.png");
            if (icon)
            {
                int[] sizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Standalone);
                var icons = new Texture2D[sizes.Length];
                for (int i = 0; i < icons.Length; i++) icons[i] = icon;
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, icons);
            }
            SinglePlayerBuildGuard.Validate();
            if (string.IsNullOrEmpty(output)) output = "Builds/SinglePlayer/Windows/VelocityRivals.exe";
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
            string fullPath = Path.GetFullPath(output);
            if (!File.Exists(fullPath)) throw new FileNotFoundException("Unity reported success but the executable is missing.", fullPath);
            if (Directory.GetFiles(Path.GetDirectoryName(fullPath), "VelocityRivals.Multiplayer.dll", SearchOption.AllDirectories).Length > 0)
                throw new BuildFailedException("An old multiplayer DLL is present in the output. Choose a clean single-player output directory.");
            string instructions = "Velocity Rivals — Single Player " + SinglePlayerBuildGuard.Version + "\r\n\r\nExtract this entire ZIP before playing. Keep the EXE, Data folder and UnityPlayer.dll together.\r\n" +
                "Launch " + Path.GetFileName(fullPath) + ".\r\n\r\n" +
                "Start CAREER TOUR for 12 events across four cups. Bronze medals unlock the next event; replay for gold.\r\n" +
                "PLAY opens six quick-play modes. This edition plays offline and needs no account or relay server.\r\n\r\n" +
                "WASD: drive | Shift: boost | Space: jump | Ctrl: drift | Q/E: air roll | R: recover | Esc: pause\r\n" +
                "Controller: triggers and left stick drive, B boost, A jump, X drift, Y recover, Start pause.\r\n" +
                "Use HOW TO PLAY for event rules and SETTINGS for music and graphics.\r\n" +
                "Saves: %USERPROFILE%\\AppData\\LocalLow\\DJKFusion\\Velocity Rivals\r\n";
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(fullPath), "START HERE.txt"), instructions);
            File.Copy("docs/PLAYER-GUIDE.txt", Path.Combine(Path.GetDirectoryName(fullPath), "PLAYER GUIDE.txt"), true);
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(fullPath), "BUILD INFO.txt"),
                "Velocity Rivals Single Player " + SinglePlayerBuildGuard.Version + "\nUnity " + Application.unityVersion + "\nBuilt UTC: " + DateTime.UtcNow.ToString("O") +
                "\nBuildPipeline result: " + report.summary.result + "\nBuild success does not certify gameplay QA. Test reports are retained separately in the source project's Artifacts directory.\n");
            return fullPath;
        }
    }
}
