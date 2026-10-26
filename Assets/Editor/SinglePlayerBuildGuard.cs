using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEngine;

namespace VelocityRivals.Editor
{
    public sealed class SinglePlayerBuildGuard : IPreprocessBuildWithReport
    {
        public const string Version = "0.6.0";
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report) => Validate();
        [MenuItem("Velocity Rivals/Check Single Player Build")]
        public static void Validate()
        {
            string symbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
            if (Array.Exists(symbols.Split(';'), symbol => symbol.Trim() == "VELOCITY_MULTIPLAYER"))
                throw new BuildFailedException("Remove VELOCITY_MULTIPLAYER from Player Settings before building the single-player edition.");
            foreach (var assembly in CompilationPipeline.GetAssemblies(AssembliesType.Player))
                if (assembly.name == "VelocityRivals.Multiplayer") throw new BuildFailedException("The multiplayer assembly must be excluded from this build.");
            if (!File.Exists(ProjectBuilder.EntryScene)) throw new BuildFailedException("Entry scene is missing.");
            if (!File.Exists("docs/PLAYER-GUIDE.txt")) throw new BuildFailedException("The packaged player guide is missing.");
            foreach (string vehicle in GameSession.VehicleNames)
                if (!Resources.Load<GameObject>("VehicleModels/" + vehicle) || !Resources.Load<VehicleStats>("VehicleStats/" + vehicle))
                    throw new BuildFailedException("Vehicle model or stats missing: " + vehicle);
            foreach (string name in new[] { "VelocitySky", "VelocityEffects" })
            {
                var shader = Resources.Load<Shader>(name);
                if (!shader || ShaderUtil.ShaderHasError(shader)) throw new BuildFailedException("Resolve shader import/compilation errors: " + name);
            }
            foreach(var playlist in new[]{MusicLibrary.MenuTracks,MusicLibrary.RaceTracks})
            {
                if(playlist.Length==0)throw new BuildFailedException("MusicLibrary needs at least one song in each playlist.");
                foreach(var path in playlist)
                {
                    var clip=string.IsNullOrWhiteSpace(path)?null:Resources.Load<AudioClip>(path);
                    if(!clip || clip.length<=.1f)throw new BuildFailedException("Fix this MusicLibrary resource path: "+path);
                }
            }
            PlayerSettings.bundleVersion = Version;
            Debug.Log("Single-player build checks passed. Unity playtests and target-PC acceptance remain required.");
        }
    }
}
