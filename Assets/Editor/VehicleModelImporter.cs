using UnityEditor;

namespace VelocityRivals.Editor
{
    public sealed class VehicleModelImporter : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith("Assets/Resources/VehicleModels/")) return;
            var importer = (ModelImporter)assetImporter;
            importer.isReadable = true; // Required by wheel-preserving runtime mesh batching.
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
        }
    }
}
