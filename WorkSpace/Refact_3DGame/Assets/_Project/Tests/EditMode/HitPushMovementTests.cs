using NUnit.Framework;
using rudIsland.RPG3D.Characters;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class HitPushMovementTests
    {
        [Test]
        public void GetNextMove_MovesHorizontallyAndStopsAtDistance()
        {
            var movement = new HitPushMovement(0.2f);
            movement.StartPush(
                new Vector3(1f, 5f, 0f),
                0.4f);

            Vector3 firstMove = movement.GetNextMove(0.1f);
            Vector3 secondMove = movement.GetNextMove(0.1f);
            Vector3 finishedMove = movement.GetNextMove(0.1f);

            Assert.That(
                Vector3.Distance(firstMove, Vector3.right * 0.2f),
                Is.LessThan(0.0001f));
            Assert.That(
                Vector3.Distance(secondMove, Vector3.right * 0.2f),
                Is.LessThan(0.0001f));
            Assert.That(finishedMove, Is.EqualTo(Vector3.zero));
            Assert.That(movement.IsMoving, Is.False);
        }

        [Test]
        public void GetNextMove_WithDifferentFrameSteps_ReachesSameDistance()
        {
            var oneFrameMovement = new HitPushMovement(0.2f);
            var twoFrameMovement = new HitPushMovement(0.2f);
            oneFrameMovement.StartPush(Vector3.back, 0.3f);
            twoFrameMovement.StartPush(Vector3.back, 0.3f);

            Vector3 oneFrameMove =
                oneFrameMovement.GetNextMove(0.2f);
            Vector3 twoFrameMove =
                twoFrameMovement.GetNextMove(0.1f) +
                twoFrameMovement.GetNextMove(0.1f);

            Assert.That(
                Vector3.Distance(oneFrameMove, twoFrameMove),
                Is.LessThan(0.0001f));
            Assert.That(oneFrameMove.z, Is.EqualTo(-0.3f).Within(0.0001f));
        }

        [Test]
        public void StartPush_WithNoHorizontalDirection_DoesNotMove()
        {
            var movement = new HitPushMovement(0.2f);
            movement.StartPush(Vector3.up, 0.3f);

            Vector3 move = movement.GetNextMove(0.2f);

            Assert.That(move, Is.EqualTo(Vector3.zero));
            Assert.That(movement.IsMoving, Is.False);
        }

        [Test]
        public void StartPush_WhileMoving_RestartsWithNewHit()
        {
            var movement = new HitPushMovement(0.2f);
            movement.StartPush(Vector3.right, 0.4f);

            Vector3 firstHitMove =
                movement.GetNextMove(0.1f);
            movement.StartPush(Vector3.back, 0.5f);
            Vector3 secondHitMove =
                movement.GetNextMove(0.2f);

            Assert.That(
                Vector3.Distance(
                    firstHitMove,
                    Vector3.right * 0.2f),
                Is.LessThan(0.0001f));
            Assert.That(
                Vector3.Distance(
                    secondHitMove,
                    Vector3.back * 0.5f),
                Is.LessThan(0.0001f));
            Assert.That(movement.IsMoving, Is.False);
        }
    }
}
