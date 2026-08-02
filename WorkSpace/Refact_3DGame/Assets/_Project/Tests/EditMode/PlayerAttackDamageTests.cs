using NUnit.Framework;
using rudIsland.RPG3D.Combat;

namespace rudIsland.RPG3D.Tests
{
    public sealed class PlayerAttackDamageTests
    {
        [Test]
        public void TryGetDamage_FindsFirstAndRunAttack()
        {
            PlayerAttackDamage[] settings = CreateSettings();

            bool foundFirst = PlayerAttackDamage.TryGetDamage(
                settings, 1, out AttackDamage firstDamage);
            bool foundRun = PlayerAttackDamage.TryGetDamage(
                settings, 6, out AttackDamage runDamage);

            Assert.That(foundFirst, Is.True);
            Assert.That(foundRun, Is.True);
            Assert.That(firstDamage.HealthDamage, Is.EqualTo(10f));
            Assert.That(runDamage.HealthDamage, Is.EqualTo(60f));
        }

        [Test]
        public void TryGetDamage_WithMissingNumber_ReturnsFalse()
        {
            bool found = PlayerAttackDamage.TryGetDamage(
                CreateSettings(), 7, out AttackDamage damage);

            Assert.That(found, Is.False);
            Assert.That(damage.IsValid, Is.False);
        }

        [Test]
        public void HasDuplicateAttackNumber_FindsDuplicate()
        {
            PlayerAttackDamage[] settings =
            {
                new PlayerAttackDamage(1, new AttackDamage(10f)),
                new PlayerAttackDamage(1, new AttackDamage(20f))
            };

            Assert.That(
                PlayerAttackDamage.HasDuplicateAttackNumber(settings),
                Is.True);
        }

        private static PlayerAttackDamage[] CreateSettings()
        {
            return new[]
            {
                new PlayerAttackDamage(1, new AttackDamage(10f)),
                new PlayerAttackDamage(6, new AttackDamage(60f))
            };
        }
    }
}
