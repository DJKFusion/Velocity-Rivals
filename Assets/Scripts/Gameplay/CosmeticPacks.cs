using System;
using System.IO;
using UnityEngine;

namespace VelocityRivals
{
    /// <summary>DRM-free downloaded content, not a receipt or proof of payment.</summary>
    public static class CosmeticPacks
    {
        [Serializable] public sealed class Manifest { public int schema; public string packId; }
        public static string InstallDirectory => Path.Combine(Path.GetDirectoryName(SaveStore.FilePath), "CosmeticPacks");
        public static bool Installed { get; private set; }
        public static string Status { get; private set; } = "";
        static bool loaded;
        public static void Reload()
        {
            loaded=true; Installed=false; Status="Pack not installed.";
            try
            {
                Directory.CreateDirectory(InstallDirectory);
                string path=Path.Combine(InstallDirectory,CosmeticCatalog.PackId+".json");
                if(!File.Exists(path)) return;
                if(new FileInfo(path).Length>4096) { Status="Invalid pack file. Download it again from itch.io."; return; }
                var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(path));
                Installed=manifest!=null && manifest.schema==1 && manifest.packId==CosmeticCatalog.PackId;
                Status=Installed ? "Pack installed. Choose a paint below." : "Invalid pack file. Download it again from itch.io.";
            }
            catch(Exception e) when(e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { Status="Unable to read the pack. Check the installation folder and download."; }
        }
        public static Color SelectedColor(PlayerProfile profile)
        {
            if(!loaded) Reload();
            int index=CosmeticCatalog.Index(profile.cosmeticPaint);
            if(Installed && index>=0 && ColorUtility.TryParseHtmlString(CosmeticCatalog.Colors[index],out var color)) return color;
            return VelocityUI.Paints[Mathf.Clamp(profile.paint,0,VelocityUI.Paints.Length-1)];
        }
    }
}
