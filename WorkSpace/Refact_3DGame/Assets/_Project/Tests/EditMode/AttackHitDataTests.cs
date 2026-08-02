using NUnit.Framework;
using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Combat;

namespace rudIsland.RPG3D.Tests
{
    public sealed class AttackHitDataTests
    {
        private sealed class RecordingHitReceiver : IAttackHitReceiver
        {
            public AttackHitData LastHit { get; private set; }

            public void ReceiveHit(in AttackHitData hit)
            {
                LastHit = hit;
            }
        }

        [Test]
        public void Constructor_StoresDamageTeamAndAttackNumber()
        {
            var hitData = new AttackHitData(
                new AttackDamage(12.5f),
                UnitTeam.Player,
                3);

            Assert.That(hitData.Damage.HealthDamage, Is.EqualTo(12.5f));
            Assert.That(hitData.AttackerTeam, Is.EqualTo(UnitTeam.Player));
            Assert.That(hitData.AttackNumber, Is.EqualTo(3));
        }

        [Test]
        public void ReceiveHit_PassesOneAttackHitDataValue()
        {
            var receiver = new RecordingHitReceiver();
            var hitData = new AttackHitData(
                new AttackDamage(7.5f),
                UnitTeam.Enemy,
                2);

            receiver.ReceiveHit(in hitData);

            Assert.That(receiver.LastHit.Damage.HealthDamage, Is.EqualTo(7.5f));
            Assert.That(receiver.LastHit.AttackerTeam, Is.EqualTo(UnitTeam.Enemy));
            Assert.That(receiver.LastHit.AttackNumber, Is.EqualTo(2));
        }
    }
}
