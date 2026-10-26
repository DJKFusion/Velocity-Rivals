using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VelocityRivals
{
    /// <summary>Local best complete Time Attack run. Visual ghost has no collision or racing authority.</summary>
    public sealed class GhostReplay : MonoBehaviour
    {
        [Serializable] public struct Frame { public float Time; public Vector3 Position; public Quaternion Rotation; }
        [Serializable] public sealed class Run { public float Duration; public List<Frame> Frames = new List<Frame>(); }
        private Run recording = new Run(), previous;
        private Transform player, visual;
        private string file;
        private float nextSample;
        private int cursor;
        private Material material;
        public void Begin(int environment, int vehicle, Transform target)
        {
            player = target;
            file = Path.Combine(Path.GetDirectoryName(SaveStore.FilePath), "ghost-v" + CircuitLayout.Revision + "-h" + HandlingModel.Revision + "-" + environment + "-" + vehicle + ".json");
            try
            {
                if (File.Exists(file) && new FileInfo(file).Length < 8 * 1024 * 1024)
                {
                    previous = JsonUtility.FromJson<Run>(File.ReadAllText(file));
                    if (!ValidRun(previous)) previous = null;
                }
            }
            catch (Exception e) { Debug.LogWarning("Ghost unavailable: " + e.Message); previous = null; }
            if (previous == null) return;
            var model = Resources.Load<GameObject>("VehicleModels/" + GameSession.VehicleNames[Mathf.Clamp(vehicle, 0, GameSession.VehicleNames.Length - 1)]);
            visual = new GameObject("Personal best ghost").transform;
            visual.SetParent(transform, false);
            var go = model ? Instantiate(model, visual) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Ghost car"; go.transform.SetParent(visual, false);
            foreach (var collider in go.GetComponentsInChildren<Collider>()) { collider.enabled = false; Destroy(collider); }
            if (model) go.transform.localPosition = new Vector3(0, -.22f, 0);
            else go.transform.localScale = new Vector3(1.8f, .6f, 3.5f);
            material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            material.color = new Color(.15f, .8f, .9f); material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", new Color(.1f, .4f, .5f));
            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            {
                var surfaces = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < surfaces.Length; i++) surfaces[i] = material;
                renderer.sharedMaterials = surfaces;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            VehicleMeshBatch.Bake(go.transform, new List<Transform>());
            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            visual.gameObject.SetActive(false);
        }
        public void Tick(float time)
        {
            if (time >= nextSample && recording.Frames.Count < 12000)
            {
                recording.Frames.Add(new Frame { Time = time, Position = player.position, Rotation = player.rotation });
                nextSample = time + .1f;
            }
            if (previous == null || visual == null) return;
            visual.gameObject.SetActive(time <= previous.Duration);
            while (cursor < previous.Frames.Count - 2 && previous.Frames[cursor + 1].Time < time) cursor++;
            var a = previous.Frames[cursor]; var b = previous.Frames[cursor + 1];
            float t = Mathf.InverseLerp(a.Time, b.Time, time);
            visual.SetPositionAndRotation(Vector3.Lerp(a.Position, b.Position, t), Quaternion.Slerp(a.Rotation, b.Rotation, t));
        }
        public void Complete(float duration)
        {
            recording.Duration = duration;
            if (!ValidRun(recording) || (previous != null && previous.Duration <= duration)) return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                string temp = file + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(recording));
                if (File.Exists(file)) File.Replace(temp, file, null);
                else File.Move(temp, file);
            }
            catch (Exception e) { Debug.LogWarning("Ghost save failed: " + e.Message); }
        }
        public static bool ValidRun(Run run)
        {
            if (run == null || !Finite(run.Duration) || run.Duration <= 0 || run.Duration > 1200 ||
                run.Frames == null || run.Frames.Count < 2 || run.Frames.Count > 12000) return false;
            float last = -1;
            foreach (var frame in run.Frames)
            {
                if (!Finite(frame.Time) || frame.Time < 0 || frame.Time <= last || frame.Time > run.Duration ||
                    !Finite(frame.Position.x) || !Finite(frame.Position.y) || !Finite(frame.Position.z) || frame.Position.sqrMagnitude > 100000000 ||
                    !Finite(frame.Rotation.x) || !Finite(frame.Rotation.y) || !Finite(frame.Rotation.z) || !Finite(frame.Rotation.w)) return false;
                float norm = frame.Rotation.x * frame.Rotation.x + frame.Rotation.y * frame.Rotation.y + frame.Rotation.z * frame.Rotation.z + frame.Rotation.w * frame.Rotation.w;
                if (Mathf.Abs(norm - 1) > .05f) return false;
                last = frame.Time;
            }
            return true;
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private void OnDestroy() { if (material != null) Destroy(material); }
    }
}
