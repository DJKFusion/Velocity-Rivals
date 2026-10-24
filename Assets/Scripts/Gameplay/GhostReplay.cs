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
            file = Path.Combine(Application.persistentDataPath, "ghost-" + environment + "-" + vehicle + ".json");
            try
            {
                if (File.Exists(file) && new FileInfo(file).Length < 8 * 1024 * 1024)
                {
                    previous = JsonUtility.FromJson<Run>(File.ReadAllText(file));
                    if (previous == null || previous.Frames == null || previous.Frames.Count < 2) previous = null;
                }
            }
            catch (Exception e) { Debug.LogWarning("Ghost unavailable: " + e.Message); previous = null; }
            if (previous == null) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = "Personal best ghost"; go.transform.SetParent(transform);
            Destroy(go.GetComponent<Collider>()); go.transform.localScale = new Vector3(1.8f, .6f, 3.5f); visual = go.transform;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            material.color = new Color(.15f, .8f, .9f); material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", new Color(.1f, .4f, .5f));
            go.GetComponent<Renderer>().sharedMaterial = material;
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
            if (previous != null && previous.Duration <= duration) return;
            recording.Duration = duration;
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                string temp = file + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(recording));
                if (File.Exists(file)) File.Replace(temp, file, null);
                else File.Move(temp, file);
            }
            catch (Exception e) { Debug.LogWarning("Ghost save failed: " + e.Message); }
        }
        private void OnDestroy() { if (material != null) Destroy(material); }
    }
}
