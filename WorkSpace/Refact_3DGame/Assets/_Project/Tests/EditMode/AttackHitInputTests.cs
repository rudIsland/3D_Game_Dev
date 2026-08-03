using NUnit.Framework;
using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Combat;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class AttackHitInputTests
    {
        private sealed class RecordingHitReceiver : IAttackHitReceiver
        {
            public AttackHitInput LastHit { get; private set; } // 피격 또는 피해 관련 값
            public bool CanTakeHit => true;
            public int ActivationSequence => 1;

            public AttackHitResult ReceiveAttackHit(in AttackHitInput hit)
            {
                LastHit = hit;
                return AttackHitResult.Damaged;
            }
        }

        [Test]
        public void Constructor_StoresDamageTeamAndAttackNumber()
        {
            var hitData = new AttackHitInput(
                new AttackDamage(12.5f),
                UnitTeam.Player,
                3);

            Assert.That(hitData.Damage.HealthDamage, Is.EqualTo(12.5f));
            Assert.That(hitData.AttackerTeam, Is.EqualTo(UnitTeam.Player));
            Assert.That(hitData.AttackNumber, Is.EqualTo(3));
            Assert.That(hitData.Strength, Is.EqualTo(HitStrength.Light));
            Assert.That(hitData.StaggerDamage, Is.EqualTo(12.5f));
            Assert.That(hitData.PushDistance, Is.Zero);
        }

        [Test]
        public void Constructor_StoresSeparateStaggerDamage()
        {
            var hitData = new AttackHitInput(
                new AttackDamage(12.5f),
                UnitTeam.Player,
                3,
                7f,
                0.4f);

            Assert.That(hitData.Damage.HealthDamage, Is.EqualTo(12.5f));
            Assert.That(hitData.Strength, Is.EqualTo(HitStrength.Light));
            Assert.That(hitData.StaggerDamage, Is.EqualTo(7f));
            Assert.That(hitData.PushDistance, Is.EqualTo(0.4f));
        }

        [Test]
        public void Constructor_StoresHitStrength()
        {
            var hitData = new AttackHitInput(
                new AttackDamage(12.5f),
                UnitTeam.Player,
                3,
                HitStrength.Heavy,
                7f,
                0.4f);

            Assert.That(hitData.Strength, Is.EqualTo(HitStrength.Heavy));
        }

        [Test]
        public void CreateWithHitContact_StoresContactResult()
        {
            var hitData = new AttackHitInput(
                new AttackDamage(10f),
                UnitTeam.Player,
                1,
                0.35f);
            Vector3 hitPoint = new Vector3(1f, 2f, 3f);
            Vector3 hitNormal = Vector3.left;
            Vector3 hitDirection = Vector3.right;
            var contact = new HitContact(
                hitPoint,
                hitNormal,
                hitDirection,
                HitBodyPart.Head,
                4.5f);

            AttackHitInput hitWithContact =
                hitData.CreateWithHitContact(in contact);

            Assert.That(hitWithContact.HitPoint, Is.EqualTo(hitPoint));
            Assert.That(hitWithContact.HitNormal, Is.EqualTo(hitNormal));
            Assert.That(
                hitWithContact.HitDirection,
                Is.EqualTo(hitDirection));
            Assert.That(
                hitWithContact.HitBodyPart,
                Is.EqualTo(HitBodyPart.Head));
            Assert.That(hitWithContact.HitSpeed, Is.EqualTo(4.5f));
            Assert.That(hitWithContact.Strength, Is.EqualTo(HitStrength.Light));
            Assert.That(hitWithContact.StaggerDamage, Is.EqualTo(10f));
            Assert.That(hitWithContact.PushDistance, Is.EqualTo(0.35f));
            Assert.That(hitData.HitPoint, Is.EqualTo(Vector3.zero));
            Assert.That(
                hitData.HitBodyPart,
                Is.EqualTo(HitBodyPart.Unknown));
        }

        [Test]
        public void ReceiveAttackHit_PassesOneAttackHitInputValue()
        {
            var receiver = new RecordingHitReceiver();
            AttackHitInput hitData = new AttackHitInput(
                new AttackDamage(7.5f),
                UnitTeam.Enemy,
                2);
            var contact = new HitContact(
                new Vector3(2f, 1f, 3f),
                Vector3.back,
                Vector3.forward,
                HitBodyPart.Body);
            hitData = hitData.CreateWithHitContact(in contact);

            AttackHitResult hitResult = receiver.ReceiveAttackHit(in hitData);

            Assert.That(receiver.LastHit.Damage.HealthDamage, Is.EqualTo(7.5f));
            Assert.That(receiver.LastHit.AttackerTeam, Is.EqualTo(UnitTeam.Enemy));
            Assert.That(receiver.LastHit.AttackNumber, Is.EqualTo(2));
            Assert.That(receiver.LastHit.StaggerDamage, Is.EqualTo(7.5f));
            Assert.That(
                receiver.LastHit.HitPoint,
                Is.EqualTo(new Vector3(2f, 1f, 3f)));
            Assert.That(receiver.LastHit.HitNormal, Is.EqualTo(Vector3.back));
            Assert.That(
                receiver.LastHit.HitDirection,
                Is.EqualTo(Vector3.forward));
            Assert.That(
                receiver.LastHit.HitBodyPart,
                Is.EqualTo(HitBodyPart.Body));
            Assert.That(hitResult.Type, Is.EqualTo(AttackHitResultType.Damaged));
        }

        [Test]
        public void HitContact_InvalidSpeed_UsesZero()
        {
            var contact = new HitContact(
                Vector3.zero,
                Vector3.up,
                Vector3.forward,
                HitBodyPart.Body,
                float.NaN);

            Assert.That(contact.HitSpeed, Is.Zero);
        }
    }
}
