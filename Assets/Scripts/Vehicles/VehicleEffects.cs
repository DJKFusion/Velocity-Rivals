using UnityEngine;

namespace VelocityRivals
{
    /// <summary>Short, bounded tyre trails, drift smoke and a quality-scaled headlight.</summary>
    public sealed class VehicleEffects : MonoBehaviour
    {
        VehicleController car;
        readonly TrailRenderer[] skid = new TrailRenderer[2];
        Material skidMaterial, smokeMaterial;
        ParticleSystem smoke;
        Light headlight;
        Vector3 previous;
        void Start()
        {
            car = GetComponent<VehicleController>(); previous = transform.position;
            Shader shader = Resources.Load<Shader>("VelocityEffects");
            if (!car || !shader) { enabled = false; return; }
            skidMaterial = new Material(shader); skidMaterial.SetColor("_Color", new Color(.025f,.03f,.04f,.7f));
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject("Rear tyre trail"); go.transform.SetParent(transform, false);
                skid[i] = go.AddComponent<TrailRenderer>(); skid[i].sharedMaterial = skidMaterial;
                skid[i].time = 8; skid[i].minVertexDistance = .25f; skid[i].widthMultiplier = .26f;
                skid[i].startColor = Color.white; skid[i].endColor = new Color(1,1,1,0);
                skid[i].alignment = LineAlignment.TransformZ; go.transform.rotation = Quaternion.Euler(90,0,0);
                skid[i].emitting = false; skid[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            var smokeObject = new GameObject("Tyre smoke"); smokeObject.transform.SetParent(transform, false); smokeObject.transform.localPosition = new Vector3(0,.15f,-1.3f);
            smoke = smokeObject.AddComponent<ParticleSystem>(); smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = smoke.main; main.loop = true; main.maxParticles = 48; main.startLifetime = new ParticleSystem.MinMaxCurve(.5f,1.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(.25f,.65f); main.startSpeed = .7f; main.startColor = new Color(.7f,.75f,.8f,.4f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = smoke.emission; emission.rateOverTime = 0;
            var shape = smoke.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .65f;
            var fade = smoke.colorOverLifetime; fade.enabled = true;
            var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Color.white,0), new GradientColorKey(Color.white,1) }, new[] { new GradientAlphaKey(0,0), new GradientAlphaKey(1,.15f), new GradientAlphaKey(0,1) }); fade.color = gradient;
            smokeMaterial = new Material(shader); smokeMaterial.SetTexture("_MainTex", Resources.Load<Texture2D>("SurfaceTextures/Smoke"));
            smokeObject.GetComponent<ParticleSystemRenderer>().sharedMaterial = smokeMaterial; smoke.Play();
            var lamp = new GameObject("Headlight", typeof(Light)); lamp.transform.SetParent(transform, false);
            lamp.transform.localPosition = new Vector3(0,.6f,1.8f); lamp.transform.localRotation = Quaternion.Euler(8,0,0);
            headlight = lamp.GetComponent<Light>(); headlight.type = LightType.Spot; headlight.range = 36;
            headlight.spotAngle = 68; headlight.intensity = 2.2f; headlight.color = new Color(.8f,.92f,1); headlight.shadows = LightShadows.None;
        }
        void Update()
        {
            if (!car || !smoke) return;
            bool drifting = car.enabled && car.IsDrifting;
            bool teleport = Vector3.Distance(previous, transform.position) > 10;
            previous = transform.position;
            for (int i = 0; i < 2; i++)
            {
                if (teleport) skid[i].Clear();
                Vector3 origin = transform.TransformPoint(new Vector3(i == 0 ? -.76f : .76f,.1f,-1.05f));
                bool contact = Physics.Raycast(origin, -transform.up, out var hit, 1.2f, ~0, QueryTriggerInteraction.Ignore) && !hit.collider.transform.IsChildOf(transform);
                skid[i].emitting = drifting && contact && !teleport;
                if (contact) { skid[i].transform.position = hit.point + hit.normal * .02f; skid[i].transform.rotation = Quaternion.LookRotation(hit.normal, transform.forward); }
            }
            var emission = smoke.emission; emission.rateOverTime = drifting && SaveStore.Current.quality > 0 ? 30 : 0;
            headlight.enabled = car.enabled && SaveStore.Current.quality > 0;
        }
        void OnDestroy() { if (skidMaterial) Destroy(skidMaterial); if (smokeMaterial) Destroy(smokeMaterial); }
    }
}
