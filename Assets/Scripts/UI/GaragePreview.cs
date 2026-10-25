using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace VelocityRivals
{
    /// <summary>Lit showroom using the same vehicle cosmetics as the playable car.</summary>
    public sealed class GaragePreview : MonoBehaviour
    {
        GameObject stage;
        VehicleController car;
        RenderTexture texture;
        readonly List<Material> materials = new List<Material>();
        int vehicle = -1;
        int cosmetics = -1;
        void Start()
        {
            stage = new GameObject("Vehicle showroom stage");
            stage.transform.position = new Vector3(0, 1000, 0);
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            platform.name = "Showroom plinth"; platform.layer = 30;
            platform.transform.SetParent(stage.transform, false);
            platform.transform.localPosition = new Vector3(0,-.16f,0);
            platform.transform.localScale = new Vector3(4.8f,.12f,4.8f);
            platform.GetComponent<Collider>().enabled = false; Destroy(platform.GetComponent<Collider>());
            platform.GetComponent<Renderer>().sharedMaterial = Material(new Color(.035f,.05f,.07f), .7f, .8f);
            var ringObject = new GameObject("Showroom light ring", typeof(LineRenderer)); ringObject.layer = 30;
            ringObject.transform.SetParent(stage.transform, false);
            var ring = ringObject.GetComponent<LineRenderer>();
            ring.useWorldSpace = false; ring.loop = true; ring.positionCount = 64; ring.widthMultiplier = .035f;
            var neon = Material(Color.cyan, 0, .5f); neon.EnableKeyword("_EMISSION"); neon.SetColor("_EmissionColor", Color.cyan * 3);
            ring.sharedMaterial = neon;
            for (int i = 0; i < 64; i++) { float angle = i * Mathf.PI * 2 / 64; ring.SetPosition(i, new Vector3(Mathf.Sin(angle)*2.25f,-.03f,Mathf.Cos(angle)*2.25f)); }
            var cameraObject = new GameObject("Showroom camera", typeof(Camera)); cameraObject.transform.SetParent(stage.transform, false);
            cameraObject.transform.localPosition = new Vector3(4.1f,1.9f,5.1f);
            cameraObject.transform.LookAt(stage.transform.position + Vector3.up * .6f);
            var camera = cameraObject.GetComponent<Camera>(); camera.cullingMask = 1 << 30;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.055f,.075f,.11f);
            camera.nearClipPlane = .1f; camera.farClipPlane = 25; camera.fieldOfView = 25; camera.allowHDR = true;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            Canvas.ForceUpdateCanvases();
            var rect = GetComponent<RectTransform>().rect;
            int width = Mathf.Clamp(Mathf.RoundToInt(rect.width), 512, 1536);
            int height = Mathf.Clamp(Mathf.RoundToInt(rect.height), 128, 768);
            texture = new RenderTexture(width, height, 24, RenderTextureFormat.DefaultHDR); texture.Create();
            camera.targetTexture = texture; GetComponent<RawImage>().texture = texture;
            Light("Showroom key", LightType.Directional, Vector3.zero, new Vector3(35,-145,0), Color.white, 2.5f);
            Light("Showroom cyan rim", LightType.Point, new Vector3(-3,2,-2), Vector3.zero, new Color(.65f,.85f,1), 12);
            Light("Showroom warm fill", LightType.Point, new Vector3(3,1,2), Vector3.zero, new Color(1,.85f,.65f), 12);
            BuildStudio();
            UpdateModel();
            var probeObject=new GameObject("Studio reflections",typeof(ReflectionProbe));
            probeObject.transform.SetParent(stage.transform,false);probeObject.transform.localPosition=Vector3.up;
            var probe=probeObject.GetComponent<ReflectionProbe>();probe.mode=UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            probe.refreshMode=UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode=UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.IndividualFaces;
            probe.resolution=SaveStore.Current.quality==0?64:128;probe.cullingMask=1<<30;
            probe.size=new Vector3(18,12,18);probe.boxProjection=true;probe.farClipPlane=20;
            probe.clearFlags=UnityEngine.Rendering.ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=new Color(.12f,.15f,.2f);
        }
        void StudioBlock(string name,Vector3 position,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.layer=30;
            go.transform.SetParent(stage.transform,false);go.transform.localPosition=position;go.transform.localScale=size;
            go.GetComponent<Collider>().enabled=false;Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial=material;
        }
        void BuildStudio()
        {
            var graphite=Material(new Color(.055f,.065f,.085f),.35f,.45f);
            var white=Material(new Color(.7f,.83f,1),0,.3f);white.EnableKeyword("_EMISSION");white.SetColor("_EmissionColor",new Color(.7f,.83f,1)*2.5f);
            StudioBlock("Studio floor",new Vector3(0,-.35f,0),new Vector3(18,.1f,18),graphite);
            StudioBlock("Studio backdrop",new Vector3(0,3,-5),new Vector3(18,7,.15f),graphite);
            for(int i=-3;i<=3;i++)StudioBlock("Backdrop light strip",new Vector3(i*1.7f,2.5f,-4.9f),new Vector3(.045f,4,.03f),white);
            StudioBlock("Overhead softbox",new Vector3(0,5,0),new Vector3(5,.08f,1.3f),white);
            StudioBlock("Side softbox",new Vector3(-4,2,0),new Vector3(.08f,2.5f,4),white);
        }
        Material Material(Color color, float metallic, float smoothness)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            mat.SetFloat("_Metallic",metallic); mat.SetFloat("_Smoothness",smoothness); materials.Add(mat); return mat;
        }
        void Light(string name, LightType type, Vector3 position, Vector3 rotation, Color color, float intensity)
        {
            var go = new GameObject(name, typeof(Light)); go.transform.SetParent(stage.transform, false);
            go.transform.localPosition = position; go.transform.localRotation = Quaternion.Euler(rotation);
            var light = go.GetComponent<Light>(); light.type = type; light.color = color; light.intensity = intensity;
            light.range = 15; light.cullingMask = 1 << 30; light.shadows = LightShadows.None;
        }
        void UpdateModel()
        {
            var p = SaveStore.Current;
            if (vehicle != p.selectedVehicle)
            {
                if (car) { car.gameObject.SetActive(false); Destroy(car.gameObject); }
                vehicle = Mathf.Clamp(p.selectedVehicle, 0, 5); cosmetics = -1;
                var stats = Resources.Load<VehicleStats>("VehicleStats/" + GameSession.VehicleNames[vehicle]);
                car = VehicleController.Spawn("Showroom " + GameSession.VehicleNames[vehicle], stage.transform.position + Vector3.up * .22f, Quaternion.Euler(0,25,0), stats, VelocityUI.Paints[p.paint], false);
                car.transform.SetParent(stage.transform, true); car.ControlsEnabled = false; car.enabled = false;
                car.GetComponent<Rigidbody>().isKinematic = true;
                foreach (var collider in car.GetComponentsInChildren<Collider>()) collider.enabled = false;
                foreach (var item in car.GetComponentsInChildren<Transform>(true)) item.gameObject.layer = 30;
            }
            int selection = p.paint + p.wheels * 10 + p.trail * 100 + p.decal * 1000 + p.spoiler * 10000 + p.finish * 100000;
            if (cosmetics != selection)
            {
                cosmetics = selection; car.SetPaint(VelocityUI.Paints[p.paint]);
                car.SetCosmetics(p.wheels,p.trail,p.decal,p.spoiler,p.finish);
            }
        }
        void Update()
        {
            if (!stage) return;
            UpdateModel();
            if (car && !SaveStore.Current.reducedEffects) car.transform.Rotate(Vector3.up, 12 * Time.unscaledDeltaTime, Space.Self);
        }
        void OnDisable() { if (stage) stage.SetActive(false); }
        void OnDestroy()
        {
            if (stage) Destroy(stage);
            if (texture) { texture.Release(); Destroy(texture); }
            foreach (var mat in materials) if (mat) Destroy(mat);
        }
    }
}
