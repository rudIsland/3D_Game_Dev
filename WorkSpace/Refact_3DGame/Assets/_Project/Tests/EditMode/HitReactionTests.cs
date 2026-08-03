using NUnit.Framework;
using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Combat;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class HitReactionTests
    {
        [Test]
        public void Create_FromFront_ReturnsFront()
        {
            HitReaction reaction = CreateReaction(Vector3.back);

            Assert.That(
                reaction.Direction,
                Is.EqualTo(HitReactionDirection.Front));
        }

        [Test]
        public void Create_FromBack_ReturnsBack()
        {
            HitReaction reaction = CreateReaction(Vector3.forward);

            Assert.That(
                reaction.Direction,
                Is.EqualTo(HitReactionDirection.Back));
        }

        [Test]
        public void Create_FromLeft_ReturnsLeft()
        {
            HitReaction reaction = CreateReaction(Vector3.right);

            Assert.That(
                reaction.Direction,
                Is.EqualTo(HitReactionDirection.Left));
        }

        [Test]
        public void Create_FromRight_ReturnsRight()
        {
            HitReaction reaction = CreateReaction(Vector3.left);

            Assert.That(
                reaction.Direction,
                Is.EqualTo(HitReactionDirection.Right));
        }

        [Test]
        public void Create_OnDiagonalBoundary_PrefersFrontOrBack()
        {
            HitReaction reaction = CreateReaction(
                new Vector3(-1f, 0f, -1f));

            Assert.That(
                reaction.Direction,
                Is.EqualTo(HitReactionDirection.Front));
        }

        [Test]
        public void Create_WithInvalidDirection_UsesFrontAndNoPush()
        {
            HitReaction reaction = CreateReaction(
                new Vector3(float.NaN, 0f, 0f));

            Assert.That(
                reaction.Direction,
                Is.EqualTo(HitReactionDirection.Front));
            Assert.That(reaction.PushDirection, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void Create_StoresStrengthBodyPartAndPush()
        {
            HitReaction reaction = CreateReaction(
                Vector3.back,
                HitStrength.Heavy,
                HitBodyPart.Head);

            Assert.That(
                reaction.Strength,
                Is.EqualTo(HitStrength.Heavy));
            Assert.That(
                reaction.BodyPart,
                Is.EqualTo(HitBodyPart.Head));
            Assert.That(
                reaction.PushDirection,
                Is.EqualTo(Vector3.back));
            Assert.That(reaction.PushDistance, Is.EqualTo(0.4f));
        }

        [Test]
        public void Create_WithInvalidStrength_UsesLight()
        {
            HitReaction reaction = CreateReaction(
                Vector3.back,
                (HitStrength)99,
                HitBodyPart.Body);

            Assert.That(
                reaction.Strength,
                Is.EqualTo(HitStrength.Light));
        }

        private static HitReaction CreateReaction(
            Vector3 pushDirection,
            HitStrength strength = HitStrength.Light,
            HitBodyPart bodyPart = HitBodyPart.Body)
        {
            var contact = new HitContact(
                Vector3.zero,
                Vector3.forward,
                pushDirection,
                bodyPart);
            var hit = new AttackHitInput(
                new AttackDamage(10f),
                UnitTeam.Player,
                1,
                strength,
                10f,
                0.4f,
                contact);

            return HitReaction.Create(
                in hit,
                Vector3.forward,
                Vector3.right);
        }
    }
}
