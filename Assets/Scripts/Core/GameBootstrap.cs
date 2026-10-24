using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VelocityRivals
{
    /// <summary>The checked-in entry scene creates the integrated game from this component.</summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            Application.targetFrameRate = 60;
            Time.fixedDeltaTime = 1f / 60f;
            if (Camera.main == null)
            {
                var cameraObject = new GameObject("Game Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.015f, .025f, .06f);
                cameraObject.transform.SetParent(transform);
            }
            gameObject.AddComponent<GameSession>();
            gameObject.AddComponent<VelocityUI>();
            gameObject.AddComponent<LanRaceSession>();
            var volume = new GameObject("Global post processing").AddComponent<Volume>();
            volume.transform.SetParent(transform);
            volume.isGlobal = true;
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = volume.profile.Add<Bloom>(true);
            bloom.intensity.Override(.45f);
            bloom.threshold.Override(1f);
            var tone = volume.profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
        }
    }
}
