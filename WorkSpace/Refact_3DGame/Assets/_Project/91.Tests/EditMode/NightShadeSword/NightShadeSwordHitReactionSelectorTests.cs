using NUnit.Framework;
using Characters.Combat;
using Characters.Enemies.NightShade;

namespace Tests.NightShade
{
    public sealed class NightShadeSwordHitReactionSelectorTests
    {
        [Test]
        public void 일반공격은경직한계미만에서SmallHit을고른다()
        {
            HitReaction reaction = NightShadeSwordHitReactionSelector.Select(
                AttackStrength.Light,
                false,
                false);

            Assert.That(reaction, Is.EqualTo(HitReaction.SmallHit));
        }

        [Test]
        public void 강공격은경직한계미만에서BigHit을고른다()
        {
            HitReaction reaction = NightShadeSwordHitReactionSelector.Select(
                AttackStrength.Heavy,
                false,
                true);

            Assert.That(reaction, Is.EqualTo(HitReaction.BigHit));
        }

        [Test]
        public void 일반공격은보호구간에서SmallHit을생략한다()
        {
            HitReaction reaction = NightShadeSwordHitReactionSelector.Select(
                AttackStrength.Light,
                false,
                true);

            Assert.That(reaction, Is.EqualTo(HitReaction.None));
        }

        [TestCase(AttackStrength.Light)]
        [TestCase(AttackStrength.Heavy)]
        [TestCase(AttackStrength.Knockdown)]
        public void 경직한계에도달하면공격세기와상관없이StaggerBreak을고른다(
            AttackStrength attackStrength)
        {
            HitReaction reaction = NightShadeSwordHitReactionSelector.Select(
                attackStrength,
                true,
                true);

            Assert.That(reaction, Is.EqualTo(HitReaction.StaggerBreak));
        }
    }
}
