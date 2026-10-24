using NUnit.Framework;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace VelocityRivals.Tests
{
    public sealed class SinglePlayerImportTests
    {
        [Test]
        public void DefaultPlayerDoesNotCompileTheMultiplayerAssembly()
        {
            foreach (var assembly in CompilationPipeline.GetAssemblies(AssembliesType.Player))
                Assert.AreNotEqual("VelocityRivals.Multiplayer", assembly.name);
        }
        [TestCase("VelocitySky")]
        [TestCase("VelocityEffects")]
        public void ShippedCustomShadersImportWithoutCompilerErrors(string name)
        {
            var shader = Resources.Load<Shader>(name);
            Assert.IsNotNull(shader, name);
            Assert.IsFalse(ShaderUtil.ShaderHasError(shader), name);
        }
        [Test]
        public void RoadNormalIsImportedAsANormalMap()
        {
            var importer = AssetImporter.GetAtPath("Assets/Resources/SurfaceTextures/AsphaltNormal.png") as TextureImporter;
            Assert.IsNotNull(importer);
            Assert.AreEqual(TextureImporterType.NormalMap, importer.textureType);
        }
    }
}
