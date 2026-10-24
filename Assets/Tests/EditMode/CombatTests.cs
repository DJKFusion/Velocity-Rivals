using NUnit.Framework;

namespace VelocityRivals.Tests
{
    public sealed class CombatTests
    {
        [Test]
        public void ShieldExpiryUsesMatchClock()
        {
            var v = new CombatVitals(); v.Shield(10);
            Assert.AreEqual(0, v.Damage(50, 15.9f));
            Assert.AreEqual(50, v.Damage(50, 16));
        }
        [Test]
        public void KnockoutCannotReceiveDuplicateDamageAndRespawnHasGracePeriod()
        {
            var v = new CombatVitals();
            Assert.AreEqual(100, v.Damage(1000, 5));
            Assert.AreEqual(0, v.Damage(50, 6));
            Assert.IsFalse(v.TryRespawn(7.9f));
            Assert.IsTrue(v.TryRespawn(8));
            Assert.AreEqual(100, v.Health);
            Assert.AreEqual(0, v.Damage(50, 9));
            Assert.AreEqual(50, v.Damage(50, 10));
        }
        [Test]
        public void KnockoutClearsPowerAndBlocksPickup()
        {
            var v = new CombatVitals(); v.Overdrive(0);
            Assert.AreEqual(2, v.DamageMultiplier(7));
            Assert.AreEqual(1, v.DamageMultiplier(8));
            v.Damage(100, 10); v.Overdrive(11); v.Shield(11);
            Assert.AreEqual(1, v.DamageMultiplier(11));
            Assert.IsFalse(v.Shielded(11));
        }
    }
}
