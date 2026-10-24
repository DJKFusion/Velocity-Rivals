using UnityEditor;

namespace VelocityRivals.Editor
{
    public sealed class SurfaceTextureImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/SurfaceTextures/", System.StringComparison.Ordinal)) return;
            var importer = (TextureImporter)assetImporter;
            importer.mipmapEnabled = !assetPath.EndsWith("Smoke.png", System.StringComparison.Ordinal);
            importer.wrapMode = assetPath.EndsWith("Smoke.png", System.StringComparison.Ordinal) ? UnityEngine.TextureWrapMode.Clamp : UnityEngine.TextureWrapMode.Repeat;
            importer.anisoLevel = 8;
            importer.maxTextureSize = 512;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = assetPath.EndsWith("Smoke.png", System.StringComparison.Ordinal);
            if (assetPath.EndsWith("Normal.png", System.StringComparison.Ordinal)) importer.textureType = TextureImporterType.NormalMap;
        }
    }
}
