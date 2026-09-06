using NUnit.Framework;
using Characters.Player.Lifecycle;
using Characters.Player.Combat.Hit;

namespace Tests.Player
{
    public sealed class PlayerHitPriorityTests
    {
        [TestCase(
            false, false, false, false, 100f, 25f,
            PlayerHitResult.Ignored)]
        [TestCase(
            true, true, true, true, 100f, 25f,
            PlayerHitResult.Ignored)]
        [TestCase(
            true, false, true, true, 100f, 25f,
            PlayerHitResult.Avoided)]
        [TestCase(
            true, false, false, true, 100f, 25f,
            PlayerHitResult.Blocked)]
        [TestCase(
            true, false, false, true, 25f, 25f,
            PlayerHitResult.GuardBroken)]
        [TestCase(
            true, false, false, false, 100f, 25f,
            PlayerHitResult.Damaged)]
        public void GetHitResultBeforeHealthDamage_보호판정을순서대로적용한다(
            bool hasDamage,
            bool isDead,
            bool isRollInvulnerable,
            bool canBlockHit,
            float currentStamina,
            float guardStaminaDamage,
            PlayerHitResult expectedResult)
        {
            PlayerHitResult result =
                PlayerWorldUnit.GetHitResultBeforeHealthDamage(
                    hasDamage,
                    isDead,
                    isRollInvulnerable,
                    canBlockHit,
                    currentStamina,
                    guardStaminaDamage);

            Assert.That(result, Is.EqualTo(expectedResult));
        }
    }
}
