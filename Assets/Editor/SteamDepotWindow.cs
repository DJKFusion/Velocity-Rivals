using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VelocityRivals.Editor
{
    public sealed class SteamDepotWindow : EditorWindow
    {
        [Serializable] sealed class Artifact { public string package, executable, version; }
        string appId = "", depotId = "", status = "";
        [MenuItem("Velocity Rivals/Prepare Steam Depot")]
        static void Open() => GetWindow<SteamDepotWindow>("Steam depot");
        void OnGUI()
        {
            EditorGUILayout.LabelField("Prepare the Windows Steam depot", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Build the current Windows ZIP first. Enter the IDs from your Steamworks dashboard. This creates local upload descriptors; it does not log in, upload, set a live branch or publish the game.", MessageType.Info);
            appId = EditorGUILayout.TextField("Steam App ID", appId);
            depotId = EditorGUILayout.TextField("Windows Depot ID", depotId);
            if (GUILayout.Button("Create local SteamPipe files"))
            {
                try
                {
                    var artifact = JsonUtility.FromJson<Artifact>(File.ReadAllText("Artifacts/latest-singleplayer-package.json"));
                    if (artifact == null || artifact.version != SinglePlayerBuildGuard.Version || !File.Exists(artifact.package))
                        throw new InvalidOperationException("Build and package this version first using Build-SinglePlayer.cmd.");
                    string output = Path.Combine("Artifacts/Steam", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0,6));
                    string manifest = SteamDepotManifest.Write(artifact.executable, appId, depotId, output, artifact.version);
                    status = "Prepared " + manifest + ". Follow docs/STEAM-RELEASE.md to upload and test a private branch.";
                    EditorUtility.RevealInFinder(manifest);
                }
                catch (Exception e) { status = e.Message; }
            }
            EditorGUILayout.HelpBox(status.Length == 0 ? "No Steam files prepared yet." : status, MessageType.None);
        }
    }
}
