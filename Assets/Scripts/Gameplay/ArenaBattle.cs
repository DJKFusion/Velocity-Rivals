using System.Collections.Generic;
using UnityEngine;

namespace VelocityRivals
{
    /// <summary>Match-owned combat simulation, shared by player and AI vehicles.</summary>
    public sealed class ArenaBattle : MonoBehaviour
    {
        sealed class Fighter
        {
            public VehicleController Car;
            public CombatVitals Vitals = new CombatVitals();
            public float NextHit;
            public Vector3 Spawn;
        }
        sealed class Pickup
        {
            public Transform Visual;
            public Vector3 Position;
            public int Kind;
            public float ReadyAt;
        }
        readonly List<Fighter> fighters = new List<Fighter>();
        readonly List<Pickup> pickups = new List<Pickup>();
        readonly List<Material> materials = new List<Material>();
        GameSession session;
        static readonly Color[] Colours = { Color.cyan, new Color(1, .3f, .05f), new Color(.7f, .2f, 1) };
        public void Initialize(GameSession owner, List<VehicleController> cars)
        {
            session = owner;
            foreach (var car in cars) fighters.Add(new Fighter { Car = car, Spawn = car.transform.position });
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4;
                Vector3 position = new Vector3(Mathf.Sin(angle) * 18, 1.4f, Mathf.Cos(angle) * 28);
                var pickup = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pickup.name = new[] { "Flux Shield", "Overdrive", "Pulse Burst" }[i % 3];
                pickup.transform.SetParent(transform);
                pickup.transform.position = position;
                pickup.transform.localScale = Vector3.one * 1.3f;
                Destroy(pickup.GetComponent<Collider>());
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.color = Colours[i % 3]; material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Colours[i % 3] * 3);
                pickup.GetComponent<Renderer>().sharedMaterial = material;
                materials.Add(material);
                pickups.Add(new Pickup { Visual = pickup.transform, Position = position, Kind = i % 3 });
            }
        }

        public string PlayerStatus
        {
            get
            {
                var fighter = Find(session.Player);
                if (fighter == null) return "";
                var v = fighter.Vitals; float now = session.Elapsed;
                if (v.KnockedOut) return "REBUILDING · " + Mathf.CeilToInt(v.RespawnAt - now) + "s";
                return "ARMOR " + v.Health + "%" + (v.Shielded(now) ? " · SHIELD " + Mathf.CeilToInt(v.ShieldUntil - now) + "s" : "") +
                    (v.DamageMultiplier(now) > 1 ? " · OVERDRIVE " + Mathf.CeilToInt(v.OverdriveUntil - now) + "s" : "");
            }
        }
        Fighter Find(VehicleController car) => fighters.Find(f => f.Car == car);
        public bool TryPickupTarget(VehicleController car, out Vector3 target)
        {
            target = Vector3.zero; float best = 22 * 22;
            foreach (var pickup in pickups)
            {
                float distance = (pickup.Position - car.transform.position).sqrMagnitude;
                if (session.Elapsed >= pickup.ReadyAt && distance < best)
                { best = distance; target = pickup.Position; }
            }
            return best < 22 * 22;
        }
        public void Hit(VehicleController attacker, VehicleController victim, float speed)
        {
            if (!session.ActionEnabled || speed < 9) return;
            var source = Find(attacker); var destination = Find(victim);
            if (source == null || destination == null || source == destination || source.Vitals.KnockedOut || session.Elapsed < source.NextHit) return;
            if (attacker.SpeedKmh < victim.SpeedKmh) return;
            source.NextHit = session.Elapsed + 1.5f;
            Damage(source, destination, Mathf.Clamp(Mathf.RoundToInt(speed * 1.8f), 10, 50));
        }
        void Damage(Fighter source, Fighter destination, int amount)
        {
            float now = session.Elapsed;
            int dealt = destination.Vitals.Damage(amount * source.Vitals.DamageMultiplier(now), now);
            if (dealt == 0) { MatchEffects.Burst(destination.Car.transform.position, Color.cyan); return; }
            session.AwardCombatPoints(source.Car, destination.Car, destination.Vitals.KnockedOut ? 3 : 1);
            var body = destination.Car.GetComponent<Rigidbody>();
            body.AddForce((destination.Car.transform.position - source.Car.transform.position).normalized * 12 + Vector3.up * 4, ForceMode.VelocityChange);
            MatchEffects.Burst(destination.Car.transform.position + Vector3.up, destination.Vitals.KnockedOut ? Color.red : Color.yellow);
            SoundDirector.Instance?.Cue(destination.Vitals.KnockedOut ? GameCue.Knockout : GameCue.Impact);
            if (destination.Vitals.KnockedOut) SetVisible(destination, false);
        }
        void SetVisible(Fighter fighter, bool visible)
        {
            fighter.Car.ControlsEnabled = visible && session.ActionEnabled;
            fighter.Car.enabled = visible;
            foreach (var audio in fighter.Car.GetComponents<AudioSource>()) { if (visible) audio.UnPause(); else audio.Pause(); }
            foreach (var renderer in fighter.Car.GetComponentsInChildren<Renderer>()) renderer.enabled = visible;
            foreach (var collider in fighter.Car.GetComponentsInChildren<Collider>()) collider.enabled = visible;
            fighter.Car.GetComponent<Rigidbody>().isKinematic = !visible;
        }
        void Update()
        {
            if (session == null || !session.ActionEnabled) return;
            float now = session.Elapsed;
            foreach (var fighter in fighters)
            {
                if (!fighter.Vitals.TryRespawn(now)) continue;
                SetVisible(fighter, true);
                fighter.Car.Respawn(fighter.Spawn + Vector3.up, Quaternion.identity);
                MatchEffects.Burst(fighter.Car.transform.position, Color.cyan);
            }
            foreach (var pickup in pickups)
            {
                bool ready = now >= pickup.ReadyAt;
                pickup.Visual.gameObject.SetActive(ready);
                if (!ready) continue;
                pickup.Visual.position = pickup.Position + Vector3.up * Mathf.Sin(now * 2) * .25f;
                pickup.Visual.rotation = Quaternion.Euler(45, now * 75, 45);
                foreach (var fighter in fighters)
                {
                    if (fighter.Vitals.KnockedOut || (fighter.Car.transform.position - pickup.Position).sqrMagnitude > 10) continue;
                    pickup.ReadyAt = now + 12;
                    pickup.Visual.gameObject.SetActive(false);
                    SoundDirector.Instance?.Cue(GameCue.Pickup);
                    MatchEffects.Burst(fighter.Car.transform.position + Vector3.up, Colours[pickup.Kind]);
                    if (pickup.Kind == 0) fighter.Vitals.Shield(now);
                    else if (pickup.Kind == 1) fighter.Vitals.Overdrive(now);
                    else foreach (var opponent in fighters)
                        if (opponent != fighter && !opponent.Vitals.KnockedOut && (opponent.Car.transform.position - fighter.Car.transform.position).sqrMagnitude < 18 * 18)
                            Damage(fighter, opponent, 35);
                    break;
                }
            }
        }
        void OnDestroy() { foreach (var material in materials) if (material != null) Destroy(material); }
    }
}
