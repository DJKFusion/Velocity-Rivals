using UnityEngine;

namespace VelocityRivals
{
    /// <summary>Readable approaching either way; hide overhead text close to the chase camera.</summary>
    public sealed class ReadableWorldSign : MonoBehaviour
    {
        MeshRenderer label;
        void Awake() { label = GetComponent<MeshRenderer>(); }
        void LateUpdate()
        {
            var camera = Camera.main;
            if (!camera || !label) return;
            Vector3 delta = transform.position - camera.transform.position;
            label.enabled = delta.sqrMagnitude >= 18 * 18 && delta.sqrMagnitude <= 120 * 120 && Vector3.Dot(camera.transform.forward, delta) > 0;
            transform.rotation = camera.transform.rotation;
        }
    }
}
