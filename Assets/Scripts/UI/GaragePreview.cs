using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VelocityRivals
{
    /// <summary>Isolated rotating model preview with an owned render target.</summary>
    public sealed class GaragePreview : MonoBehaviour
    {
        GameObject stage;
        Transform model;
        RenderTexture texture;
        readonly List<Material> materials = new List<Material>();
        Material paint;
        void Start()
        {
            stage = new GameObject("Garage preview stage");
            stage.transform.position = new Vector3(0, 1000, 0);
            var asset = Resources.Load<GameObject>("VehicleModels/" + GameSession.VehicleNames[Mathf.Clamp(SaveStore.Current.selectedVehicle, 0, 5)]);
            if (asset == null) return;
            model = Instantiate(asset, stage.transform).transform;
            foreach (var item in model.GetComponentsInChildren<Transform>(true)) item.gameObject.layer = 30;
            paint = MakeMaterial(Color.cyan);
            var trim = MakeMaterial(new Color(.07f, .11f, .16f));
            var glass = MakeMaterial(new Color(.035f, .10f, .15f));
            glass.SetFloat("_Metallic", .75f); glass.SetFloat("_Smoothness", .95f);
            var alloy = MakeMaterial(new Color(.45f, .5f, .6f)); alloy.SetFloat("_Metallic", .9f);
            var glow = MakeMaterial(Color.cyan); glow.EnableKeyword("_EMISSION"); glow.SetColor("_EmissionColor", Color.cyan * 3);
            var tail = MakeMaterial(Color.red); tail.EnableKeyword("_EMISSION"); tail.SetColor("_EmissionColor", Color.red * 3);
            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                var slots = renderer.sharedMaterials;
                string part = renderer.name;
                Material finish = part.StartsWith("Body") ? paint : part.StartsWith("Glass") ? glass :
                    part.StartsWith("Tail") ? tail : part.StartsWith("Trim") || part.StartsWith("WheelHub") ? glow :
                    part.StartsWith("WheelRim") || part.StartsWith("WheelSpoke") || part.StartsWith("BrakeDisc") ? alloy : trim;
                for (int i = 0; i < slots.Length; i++) slots[i] = finish;
                renderer.sharedMaterials = slots;
            }
            var cameraObject = new GameObject("Garage camera", typeof(Camera));
            cameraObject.transform.SetParent(stage.transform, false);
            cameraObject.transform.localPosition = new Vector3(4, 2.6f, 5);
            cameraObject.transform.LookAt(stage.transform.position + Vector3.up * .55f);
            var camera = cameraObject.GetComponent<Camera>();
            camera.cullingMask = 1 << 30; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.025f, .045f, .09f);
            camera.nearClipPlane = .1f; camera.farClipPlane = 20;
            camera.fieldOfView = 38;
            texture = new RenderTexture(960, 140, 24);
            texture.Create(); camera.targetTexture = texture;
            GetComponent<RawImage>().texture = texture;
            var lightObject = new GameObject("Garage light", typeof(Light));
            lightObject.transform.SetParent(stage.transform, false);
            lightObject.transform.localRotation = Quaternion.Euler(45, -35, 0);
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional; light.intensity = 2; light.cullingMask = 1 << 30;
        }
        Material MakeMaterial(Color colour)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = colour; materials.Add(material); return material;
        }
        void Update()
        {
            if (model != null) model.Rotate(Vector3.up, 20 * Time.unscaledDeltaTime, Space.Self);
            if (paint != null)
            {
                paint.color = VelocityUI.Paints[Mathf.Clamp(SaveStore.Current.paint, 0, 7)];
                paint.SetFloat("_Smoothness", SaveStore.Current.finish == 1 ? .15f : .65f);
                paint.SetFloat("_Metallic", SaveStore.Current.finish == 2 ? .9f : .35f);
            }
        }
        void OnDisable() { if (stage != null) stage.SetActive(false); }
        void OnDestroy()
        {
            if (stage != null) Destroy(stage);
            if (texture != null) { texture.Release(); Destroy(texture); }
            foreach (var material in materials) if (material != null) Destroy(material);
        }
    }
}
