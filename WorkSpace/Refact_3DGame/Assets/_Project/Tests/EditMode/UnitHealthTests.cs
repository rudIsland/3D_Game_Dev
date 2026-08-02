using NUnit.Framework;
using rudIsland.RPG3D.Characters;

namespace rudIsland.RPG3D.Tests
{
    public sealed class UnitHealthTests
    {
        [Test]
        public void TakeDamage_WhenHealthReachesZero_RaisesDeathOnce()
        {
            var health = new UnitHealth(10f);
            int diedCount = 0;
            health.Died += () => diedCount++;

            health.TakeDamage(4f);
            health.TakeDamage(6f);
            health.TakeDamage(1f);

            Assert.That(health.CurrentHealth, Is.Zero);
            Assert.That(health.IsDead, Is.True);
            Assert.That(diedCount, Is.EqualTo(1));
        }

        [Test]
        public void Reset_AfterDeath_RestoresMaximumHealth()
        {
            var health = new UnitHealth(10f);
            health.TakeDamage(10f);

            health.Reset();

            Assert.That(health.CurrentHealth, Is.EqualTo(10f));
            Assert.That(health.IsDead, Is.False);
        }

        [Test]
        public void HealthChanges_RaiseOneEventOnlyWhenValueChanges()
        {
            var health = new UnitHealth(10f);
            int changedCount = 0;
            UnitHealth lastChangedHealth = null;
            health.HealthChanged += changedHealth =>
            {
                changedCount++;
                lastChangedHealth = changedHealth;
            };

            health.TakeDamage(0f);
            health.TakeDamage(float.NaN);
            health.TakeDamage(float.PositiveInfinity);
            health.TakeDamage(4f);
            health.Heal(0f);
            health.Heal(float.NaN);
            health.Heal(float.PositiveInfinity);
            health.Heal(2f);
            health.Heal(100f);
            health.Heal(1f);
            health.Reset();

            Assert.That(changedCount, Is.EqualTo(3));
            Assert.That(lastChangedHealth, Is.SameAs(health));
            Assert.That(health.CurrentHealth, Is.EqualTo(10f));
        }

        [Test]
        public void TakeDamage_WhenHealthReachesZero_RaisesChangeBeforeDeath()
        {
            var health = new UnitHealth(10f);
            string callOrder = string.Empty;
            health.HealthChanged += _ => callOrder += "changed ";
            health.Died += () => callOrder += "died";

            health.TakeDamage(10f);

            Assert.That(callOrder, Is.EqualTo("changed died"));
        }
    }
}
