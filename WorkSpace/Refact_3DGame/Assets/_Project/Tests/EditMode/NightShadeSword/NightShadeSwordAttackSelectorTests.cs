using NUnit.Framework;
using rudIsland.RPG3D.Characters.Enemies.NightShade;

namespace rudIsland.RPG3D.Tests
{
    public sealed class NightShadeSwordAttackSelectorTests
    {
        private NightShadeSwordAttackSelector selector;

        [SetUp]
        public void SetUp()
        {
            selector = new NightShadeSwordAttackSelector(100f);
        }

        [TestCase(36f, 59, 1)]
        [TestCase(36f, 60, 0)]
        [TestCase(36.01f, 29, 0)]
        [TestCase(36.01f, 30, 1)]
        [TestCase(75f, 44, 1)]
        [TestCase(75f, 45, 3)]
        [TestCase(75.01f, 59, 2)]
        [TestCase(75.01f, 60, 3)]
        public void ChooseByRoll_거리와확률구간에맞는공격을고른다(
            float distanceSquared,
            int roll,
            int expectedAttackValue)
        {
            NightShadeSwordAttackType actual = selector.ChooseByRoll(
                distanceSquared,
                false,
                NightShadeSwordAttackType.Light,
                roll);

            Assert.That(
                actual,
                Is.EqualTo(
                    (NightShadeSwordAttackType)expectedAttackValue));
        }

        [TestCase(20f, 0, 1, 0)]
        [TestCase(20f, 99, 0, 1)]
        [TestCase(50f, 0, 0, 3)]
        [TestCase(50f, 99, 3, 0)]
        [TestCase(90f, 0, 2, 3)]
        [TestCase(90f, 99, 3, 2)]
        public void ChooseByRoll_직전공격과같으면현재거리의다른공격을고른다(
            float distanceSquared,
            int roll,
            int previousAttackValue,
            int expectedAttackValue)
        {
            NightShadeSwordAttackType actual = selector.ChooseByRoll(
                distanceSquared,
                true,
                (NightShadeSwordAttackType)previousAttackValue,
                roll);

            Assert.That(
                actual,
                Is.EqualTo(
                    (NightShadeSwordAttackType)expectedAttackValue));
        }

        [Test]
        public void FightMemory_초기화하면공격기억과대기시간이사라진다()
        {
            var memory = new NightShadeSwordFightMemory();
            memory.Reset();
            memory.RecordAttack(NightShadeSwordAttackType.Heavy);
            memory.CompleteAttack(2f);

            memory.Reset();

            Assert.That(memory.HasPreviousAttack, Is.False);
            Assert.That(memory.RemainingAttackCooldown, Is.Zero);
            Assert.That(memory.CompletedAttackCount, Is.Zero);
            Assert.That(memory.HasPendingComboSecond, Is.False);
            Assert.That(
                memory.ChooseCombatMove(false),
                Is.EqualTo(NightShadeCombatMoveType.Left));
        }

        [Test]
        public void FightMemory_ComboSecond예약은대기시간과함께저장되고한번만꺼낸다()
        {
            var memory = new NightShadeSwordFightMemory();
            memory.Reset();

            memory.ReserveComboSecond(0.7f);

            Assert.That(memory.HasPendingComboSecond, Is.True);
            Assert.That(memory.RemainingAttackCooldown, Is.EqualTo(0.7f));
            Assert.That(memory.TakePendingComboSecond(), Is.True);
            Assert.That(memory.TakePendingComboSecond(), Is.False);
        }

        [Test]
        public void FightMemory_공격대기시간은0까지만감소한다()
        {
            var memory = new NightShadeSwordFightMemory();
            memory.Reset();
            memory.CompleteAttack(1f);

            memory.UpdateAttackCooldown(2f);

            Assert.That(memory.RemainingAttackCooldown, Is.Zero);
        }

        [Test]
        public void FightMemory_좌우전투이동은번갈아선택한다()
        {
            var memory = new NightShadeSwordFightMemory();
            memory.Reset();

            Assert.That(
                memory.ChooseCombatMove(false),
                Is.EqualTo(NightShadeCombatMoveType.Left));
            Assert.That(
                memory.ChooseCombatMove(false),
                Is.EqualTo(NightShadeCombatMoveType.Right));
            Assert.That(
                memory.ChooseCombatMove(true),
                Is.EqualTo(NightShadeCombatMoveType.Backward));
        }
    }
}
