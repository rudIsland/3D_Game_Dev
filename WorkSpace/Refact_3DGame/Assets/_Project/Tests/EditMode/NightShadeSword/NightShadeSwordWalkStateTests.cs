using NUnit.Framework;
using rudIsland.RPG3D.Characters.Enemies.NightShade;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class NightShadeSwordWalkStateTests
    {
        [Test]
        public void Positioning_Chase는5미터이하에서WalkApproach로넘긴다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 5.5f));
            NightShadeSwordStateMachine machine = scope.CreateStateMachine(
                scope.CreateSettings());
            machine.Enable();
            machine.Update(0.1f);
            Assert.That(machine.CurrentActionId, Is.EqualTo(NightShadeSwordActionId.Chase));

            scope.TargetObject.transform.position = new Vector3(0f, 0f, 5f);
            machine.Update(0.1f);

            Assert.That(machine.CurrentActionId, Is.EqualTo(NightShadeSwordActionId.WalkApproach));
        }

        [Test]
        public void Positioning_Walk과Chase는5미터와6미터사이에서이전Action을유지한다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 4.5f));
            NightShadeSwordStateMachine machine = scope.CreateStateMachine(
                scope.CreateSettings());
            machine.Enable();
            machine.Update(0.1f);
            Assert.That(machine.CurrentActionId, Is.EqualTo(NightShadeSwordActionId.WalkApproach));

            scope.TargetObject.transform.position = new Vector3(0f, 0f, 5.5f);
            machine.Update(0.1f);
            Assert.That(machine.CurrentActionId, Is.EqualTo(NightShadeSwordActionId.WalkApproach));

            scope.TargetObject.transform.position = new Vector3(0f, 0f, 6f);
            machine.Update(0.1f);
            Assert.That(machine.CurrentActionId, Is.EqualTo(NightShadeSwordActionId.Chase));

            scope.TargetObject.transform.position = new Vector3(0f, 0f, 5.5f);
            machine.Update(0.1f);
            Assert.That(machine.CurrentActionId, Is.EqualTo(NightShadeSwordActionId.Chase));
        }

        [Test]
        public void WatchTarget_방향이맞지않으면Idle로회전만한다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 3f));
            scope.Movement.Forward = Vector3.back;
            NightShadeSwordStateMachine machine = scope.CreateStateMachine(
                scope.CreateSettings());
            machine.Enable();
            machine.Update(0.1f);
            machine.Update(0.1f);

            Assert.That(machine.CurrentActionId, Is.EqualTo(NightShadeSwordActionId.WatchTarget));
            Assert.That(scope.Movement.TurnToCount, Is.EqualTo(1));
            Assert.That(scope.Animation.AttackCount, Is.Zero);
        }

        [Test]
        public void Positioning_대상이감지범위를벗어나면Idle로돌아간다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 4.5f));
            NightShadeSwordStateMachine machine = scope.CreateStateMachine(
                scope.CreateSettings());
            machine.Enable();
            machine.Update(0.1f);

            scope.TargetObject.transform.position = new Vector3(0f, 0f, 20f);
            machine.Update(0.1f);

            Assert.That(machine.CurrentStateId, Is.EqualTo(NightShadeSwordStateId.Idle));
            Assert.That(machine.CurrentActionId, Is.EqualTo(NightShadeSwordActionId.None));
        }
    }
}
