using NUnit.Framework;
using Characters.Combat;
using UnityEngine;

namespace Tests.Player
{
    public sealed class AttackTargetCorrectionTests
    {
        [TestCase(false, 10f, 0.5f)]
        [TestCase(true, 0.8f, 0f)]
        [TestCase(true, 1.4f, 0.55f)]
        [TestCase(true, 3f, 0.75f)]
        public void Begin_대상거리와추가이동한계로계획거리를정한다(
            bool hasTarget,
            float targetDistance,
            float expectedDistance)
        {
            var correction = new AttackTargetCorrection();

            correction.Begin(
                Vector3.zero,
                Vector3.forward,
                hasTarget,
                Vector3.forward * targetDistance,
                0.5f,
                0.85f,
                0.25f,
                30f,
                AnimationCurve.Linear(0f, 0f, 1f, 1f));

            Assert.That(
                correction.PlannedMoveDistance,
                Is.EqualTo(expectedDistance).Within(0.0001f));
        }

        [TestCase(90f, 30f)]
        [TestCase(-90f, -30f)]
        [TestCase(180f, 30f)]
        public void UpdateTargetDirection_시작방향기준최대각도로제한한다(
            float targetAngle,
            float expectedAngle)
        {
            var correction = new AttackTargetCorrection();
            Vector3 targetDirection = Quaternion.AngleAxis(
                targetAngle,
                Vector3.up) * Vector3.forward;
            correction.Begin(
                Vector3.zero,
                Vector3.forward,
                true,
                targetDirection * 3f,
                0.5f,
                0.85f,
                0.25f,
                30f,
                AnimationCurve.Linear(0f, 0f, 1f, 1f));

            float actualAngle = Vector3.SignedAngle(
                Vector3.forward,
                correction.TurnDirection,
                Vector3.up);

            if (Mathf.Abs(targetAngle) >= 179.9f)
            {
                Assert.That(
                    Mathf.Abs(actualAngle),
                    Is.EqualTo(Mathf.Abs(expectedAngle)).Within(0.001f));
                return;
            }

            Assert.That(
                actualAngle,
                Is.EqualTo(expectedAngle).Within(0.001f));
        }

        [Test]
        public void EvaluateDeltaDistance_곡선누적거리가계획거리와일치한다()
        {
            var correction = CreateCorrection(
                new AnimationCurve(
                    new Keyframe(0f, 0f),
                    new Keyframe(0.3f, 1f),
                    new Keyframe(1f, 1f)));

            float totalDistance = 0f;
            totalDistance += correction.EvaluateDeltaDistance(0.1f);
            totalDistance += correction.EvaluateDeltaDistance(0.3f);
            totalDistance += correction.EvaluateDeltaDistance(0.7f);
            totalDistance += correction.EvaluateDeltaDistance(1f);

            Assert.That(
                totalDistance,
                Is.EqualTo(correction.PlannedMoveDistance).Within(0.0001f));
        }

        [Test]
        public void Reset_이전공격거리방향과대상상태를지운다()
        {
            AttackTargetCorrection correction = CreateCorrection(
                AnimationCurve.Linear(0f, 0f, 1f, 1f));
            correction.EvaluateDeltaDistance(0.5f);

            correction.Reset();

            Assert.That(correction.IsActive, Is.False);
            Assert.That(correction.HasTarget, Is.False);
            Assert.That(correction.PlannedMoveDistance, Is.Zero);
            Assert.That(correction.TurnDirection, Is.EqualTo(Vector3.zero));
            Assert.That(correction.EvaluateDeltaDistance(1f), Is.Zero);
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        public void EvaluateDeltaDistance_프레임수와관계없이최종거리가같다(
            int framesPerSecond)
        {
            AttackTargetCorrection correction = CreateCorrection(
                AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));
            float totalDistance = 0f;

            for (int frame = 1; frame <= framesPerSecond; frame++)
            {
                totalDistance += correction.EvaluateDeltaDistance(
                    frame / (float)framesPerSecond);
            }

            Assert.That(
                totalDistance,
                Is.EqualTo(correction.PlannedMoveDistance).Within(0.0001f));
        }

        private static AttackTargetCorrection CreateCorrection(
            AnimationCurve movementCurve)
        {
            var correction = new AttackTargetCorrection();
            correction.Begin(
                Vector3.zero,
                Vector3.forward,
                true,
                Vector3.forward * 3f,
                0.5f,
                0.85f,
                0.25f,
                30f,
                movementCurve);
            return correction;
        }
    }
}
