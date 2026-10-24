using System;

namespace VelocityRivals
{
    /// <summary>Deterministic combat rules. All timers use the match clock, so pause stops them.</summary>
    public sealed class CombatVitals
    {
        public int Health { get; private set; } = 100;
        public float ShieldUntil { get; private set; }
        public float OverdriveUntil { get; private set; }
        public float RespawnAt { get; private set; }
        public bool KnockedOut => Health == 0;
        public bool Shielded(float now) => now < ShieldUntil;
        public int DamageMultiplier(float now) => now < OverdriveUntil ? 2 : 1;
        public void Shield(float now) { if (!KnockedOut) ShieldUntil = Math.Max(ShieldUntil, now + 6); }
        public void Overdrive(float now) { if (!KnockedOut) OverdriveUntil = Math.Max(OverdriveUntil, now + 8); }
        public int Damage(int amount, float now)
        {
            if (KnockedOut || Shielded(now) || amount <= 0) return 0;
            int dealt = Math.Min(Health, amount);
            Health -= dealt;
            if (KnockedOut) { RespawnAt = now + 3; OverdriveUntil = 0; }
            return dealt;
        }
        public bool TryRespawn(float now)
        {
            if (!KnockedOut || now < RespawnAt) return false;
            Health = 100; ShieldUntil = now + 2; OverdriveUntil = 0;
            return true;
        }
    }
}
