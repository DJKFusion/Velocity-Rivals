using UnityEngine;

namespace VelocityRivals
{
    public sealed class WorldAtmosphere : MonoBehaviour
    {
        Material sky;
        public static void Build(Transform world, int theme)
        {
            var atmosphere = world.gameObject.AddComponent<WorldAtmosphere>();
            var shader = Resources.Load<Shader>("VelocitySky");
            if (!shader) return;
            atmosphere.sky = new Material(shader);
            Color[] zenith = { new Color(.005f,.012f,.05f), new Color(.06f,.15f,.3f), new Color(.13f,.24f,.43f), new Color(.018f,.005f,.045f), new Color(.04f,.15f,.22f) };
            Color[] horizon = { new Color(.24f,.08f,.23f), new Color(.85f,.45f,.36f), new Color(.95f,.58f,.29f), new Color(.24f,.04f,.23f), new Color(.4f,.7f,.78f) };
            atmosphere.sky.SetColor("_Zenith", zenith[theme]); atmosphere.sky.SetColor("_Horizon", horizon[theme]);
            atmosphere.sky.SetColor("_SunColor", theme == 2 || theme == 1 ? new Color(1,.72f,.4f) : new Color(.45f,.65f,1));
            var sun = world.GetComponentInChildren<Light>();
            atmosphere.sky.SetVector("_SunDirection", sun ? -sun.transform.forward : new Vector3(.2f,.4f,.8f));
            atmosphere.sky.SetFloat("_Stars", theme == 0 || theme == 3 ? 1 : .15f);
            atmosphere.sky.SetFloat("_Clouds", theme == 1 || theme == 4 ? .8f : .35f);
            RenderSettings.skybox = atmosphere.sky;
            RenderSettings.fogColor = Color.Lerp(horizon[theme], zenith[theme], .6f);
            RenderSettings.fogDensity = theme == 4 ? .0035f : .0018f;
            if (Camera.main) Camera.main.clearFlags = CameraClearFlags.Skybox;
        }
        void OnDestroy()
        {
            if (RenderSettings.skybox == sky) RenderSettings.skybox = null;
            if (sky) Destroy(sky);
        }
    }
}
