using NUnit.Framework;
using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Characters.Combat;
using rudIsland.RPG3D.Characters.Combat.AttackData;

namespace rudIsland.RPG3D.Tests
{
    public sealed class HitReactionRulesTests
    {
        [Test]
        public void HitDamageCalculator_체력피해와사망을몸반응과분리해반환한다()
        {
            var health = new UnitHealth(20f);

            HitDamageResult damaged =
                HitDamageCalculator.Apply(health, 5f);
            HitDamageResult killed =
                HitDamageCalculator.Apply(health, 15f);
            HitDamageResult ignored =
                HitDamageCalculator.Apply(health, 1f);

            Assert.That(damaged, Is.EqualTo(HitDamageResult.Damaged));
            Assert.That(killed, Is.EqualTo(HitDamageResult.Killed));
            Assert.That(ignored, Is.EqualTo(HitDamageResult.Ignored));
        }

        [Test]
        public void Select_일반상태한계미만이면SmallHit을반환한다()
        {
            HitReaction reaction = HitReactionSelector.Select(
                AttackStrength.Light,
                false,
                false,
                false,
                false);

            Assert.That(reaction, Is.EqualTo(HitReaction.SmallHit));
        }

        [Test]
        public void Select_보호구간한계미만이면None을반환한다()
        {
            HitReaction reaction = HitReactionSelector.Select(
                AttackStrength.Heavy,
                false,
                true,
                true,
                true);

            Assert.That(reaction, Is.EqualTo(HitReaction.None));
        }

        [TestCase(AttackStrength.Light, HitReaction.BigHit)]
        [TestCase(AttackStrength.Heavy, HitReaction.Knockback)]
        [TestCase(AttackStrength.Knockdown, HitReaction.Knockdown)]
        public void Select_한계에도달하면공격세기에맞는강한반응을반환한다(
            AttackStrength attackStrength,
            HitReaction expectedReaction)
        {
            HitReaction reaction = HitReactionSelector.Select(
                attackStrength,
                true,
                true,
                true,
                true);

            Assert.That(reaction, Is.EqualTo(expectedReaction));
        }

        [TestCase(AttackStrength.Heavy)]
        [TestCase(AttackStrength.Knockdown)]
        public void Select_미지원강제반응은BigHit으로낮춘다(
            AttackStrength attackStrength)
        {
            HitReaction reaction = HitReactionSelector.Select(
                attackStrength,
                true,
                false,
                false,
                false);

            Assert.That(reaction, Is.EqualTo(HitReaction.BigHit));
        }

        [Test]
        public void HitReactionPlayback_0점18초안의SmallHit재시작을막는다()
        {
            Assert.That(
                HitReactionPlayback.CanStart(
                    HitReaction.SmallHit,
                    HitReaction.SmallHit,
                    0.1799f),
                Is.False);
            Assert.That(
                HitReactionPlayback.CanStart(
                    HitReaction.SmallHit,
                    HitReaction.SmallHit,
                    0.18f),
                Is.True);
        }

        [Test]
        public void HitReactionPlayback_강한반응은약한현재반응을즉시덮어쓴다()
        {
            Assert.That(
                HitReactionPlayback.CanStart(
                    HitReaction.SmallHit,
                    HitReaction.BigHit,
                    0f),
                Is.True);
            Assert.That(
                HitReactionPlayback.CanStart(
                    HitReaction.Knockback,
                    HitReaction.BigHit,
                    1f),
                Is.False);
        }

        [Test]
        public void StopPoint_회복대기후에만설정속도로줄어든다()
        {
            var stopPoint = new StopPoint(50f, 3f, 5f);
            Assert.That(stopPoint.TryAccumulate(20f), Is.False);

            Assert.That(stopPoint.UpdateRecovery(2.9f), Is.False);
            Assert.That(stopPoint.CurrentPoint, Is.EqualTo(20f));
            Assert.That(stopPoint.UpdateRecovery(0.1f), Is.True);
            Assert.That(
                stopPoint.CurrentPoint,
                Is.EqualTo(19.5f).Within(0.001f));
            Assert.That(stopPoint.UpdateRecovery(1f), Is.True);
            Assert.That(
                stopPoint.CurrentPoint,
                Is.EqualTo(14.5f).Within(0.001f));
        }

        [Test]
        public void StopPoint_한계에도달하면알리고누적값을초기화한다()
        {
            var stopPoint = new StopPoint(50f, 3f, 5f);

            Assert.That(stopPoint.TryAccumulate(30f), Is.False);
            Assert.That(stopPoint.TryAccumulate(20f), Is.True);
            Assert.That(stopPoint.CurrentPoint, Is.Zero);
        }

        [Test]
        public void HitPushDistance_반응별최대거리규칙을적용한다()
        {
            Assert.That(
                HitPushDistance.GetDistance(
                    1f,
                    HitReaction.SmallHit),
                Is.EqualTo(0.08f));
            Assert.That(
                HitPushDistance.GetDistance(
                    1f,
                    HitReaction.BigHit),
                Is.EqualTo(0.25f));
            Assert.That(
                HitPushDistance.GetDistance(
                    1f,
                    HitReaction.Knockback),
                Is.EqualTo(1f));
        }

        [Test]
        public void AttackDamage_Strength는직렬화숫자를유지하는열거형이다()
        {
            var damage = new AttackDamage(
                1f,
                AttackStrength.Knockdown,
                1f,
                0f,
                0f,
                false);

            Assert.That(damage.Strength, Is.EqualTo(AttackStrength.Knockdown));
            Assert.That((int)damage.Strength, Is.EqualTo(2));
        }
    }
}
