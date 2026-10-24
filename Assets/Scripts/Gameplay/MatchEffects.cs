using UnityEngine;

namespace VelocityRivals
{
    /// <summary>Short-lived, bounded particle bursts owned by the current match.</summary>
    public sealed class MatchEffects : MonoBehaviour
    {
        Material material;
        public static void Burst(Vector3 position, Color colour)
        {
            var root = new GameObject("Impact particles");
            var session = GameSession.Instance;
            if (session != null && session.Player != null) root.transform.SetParent(session.Player.transform.parent);
            root.transform.position = position;
            var owner = root.AddComponent<MatchEffects>();
            var particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main; main.loop = false; main.duration = .15f;
            main.startLifetime = .5f; main.startSpeed = 7; main.startSize = .15f;
            main.startColor = colour; main.gravityModifier = .6f; main.maxParticles = 24;
            var emission = particles.emission; emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, 20) });
            owner.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            owner.material.color = colour; owner.material.EnableKeyword("_EMISSION");
            owner.material.SetColor("_EmissionColor", colour * 3);
            root.GetComponent<ParticleSystemRenderer>().sharedMaterial = owner.material;
            particles.Play(); Destroy(root, 1);
        }
        void OnDestroy() { if (material != null) Destroy(material); }
    }
}
