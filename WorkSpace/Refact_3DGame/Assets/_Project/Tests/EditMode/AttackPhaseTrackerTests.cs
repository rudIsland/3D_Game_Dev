using NUnit.Framework;
using rudIsland.RPG3D.Combat;

namespace rudIsland.RPG3D.Tests
{
    public sealed class AttackPhaseTrackerTests
    {
        [Test]
        public void BeginAttack_StartsReadyAndAllowsTurn()
        {
            var tracker = new AttackPhaseTracker();

            tracker.BeginAttack();

            Assert.That(tracker.IsAttackActive, Is.True);
            Assert.That(
                tracker.CurrentPhase,
                Is.EqualTo(AttackPhase.Ready));
            Assert.That(tracker.CanTurn, Is.True);
        }

        [Test]
        public void BeginHit_StopsTurn()
        {
            var tracker = new AttackPhaseTracker();
            tracker.BeginAttack();

            bool changed = tracker.BeginHit();

            Assert.That(changed, Is.True);
            Assert.That(
                tracker.CurrentPhase,
                Is.EqualTo(AttackPhase.Hit));
            Assert.That(tracker.CanTurn, Is.False);
        }

        [Test]
        public void BeginRecovery_AfterHit_UsesRecovery()
        {
            var tracker = new AttackPhaseTracker();
            tracker.BeginAttack();
            tracker.BeginHit();

            bool changed = tracker.BeginRecovery();

            Assert.That(changed, Is.True);
            Assert.That(
                tracker.CurrentPhase,
                Is.EqualTo(AttackPhase.Recovery));
            Assert.That(tracker.CanTurn, Is.False);
        }

        [Test]
        public void BeginRecovery_WithoutHit_StillUsesRecovery()
        {
            var tracker = new AttackPhaseTracker();
            tracker.BeginAttack();

            bool changed = tracker.BeginRecovery();

            Assert.That(changed, Is.True);
            Assert.That(
                tracker.CurrentPhase,
                Is.EqualTo(AttackPhase.Recovery));
        }

        [Test]
        public void PhaseEvent_AfterEnd_DoesNothing()
        {
            var tracker = new AttackPhaseTracker();
            tracker.BeginAttack();
            tracker.EndAttack();

            bool hitChanged = tracker.BeginHit();
            bool recoveryChanged = tracker.BeginRecovery();

            Assert.That(hitChanged, Is.False);
            Assert.That(recoveryChanged, Is.False);
            Assert.That(tracker.IsAttackActive, Is.False);
            Assert.That(tracker.CanTurn, Is.False);
        }

        [Test]
        public void BeginAttack_AfterRecovery_StartsNewReadyPhase()
        {
            var tracker = new AttackPhaseTracker();
            tracker.BeginAttack();
            tracker.BeginHit();
            tracker.BeginRecovery();

            tracker.BeginAttack();

            Assert.That(
                tracker.CurrentPhase,
                Is.EqualTo(AttackPhase.Ready));
            Assert.That(tracker.CanTurn, Is.True);
        }
    }
}
