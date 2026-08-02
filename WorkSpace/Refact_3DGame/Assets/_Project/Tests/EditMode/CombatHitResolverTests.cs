using NUnit.Framework;
using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Combat;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class CombatHitResolverTests
    {
        private sealed class RecordingHitReceiver : IAttackHitReceiver
        {
            public int HitCount { get; private set; } // 피격 또는 피해 관련 값
            public MeleeHitDetector DetectorToStop { get; set; } // 씬 또는 시스템 참조
            public CombatHitResolver ResolverToQueue { get; set; } // 외부에 제공하는 읽기 값
            public MeleeHitDetector DetectorToQueue { get; set; } // 씬 또는 시스템 참조
            public RecordingHitReceiver ReceiverToQueue { get; set; } // 외부에 제공하는 읽기 값
            public AttackHitData HitToQueue { get; set; } // 피격 또는 피해 관련 값
            public int AttackSequenceToQueue { get; set; } // 공격 관련 설정 또는 상태

            public AttackHitResult ReceiveHit(in AttackHitData hit)
            {
                DetectorToStop?.EndHit();
                QueueNextHit();
                HitCount++;
                return AttackHitResult.Damaged;
            }

            private void QueueNextHit()
            {
                if (ResolverToQueue == null ||
                    DetectorToQueue == null ||
                    ReceiverToQueue == null)
                {
                    return;
                }

                AttackHitData hit = HitToQueue;
                ResolverToQueue.QueueHit(
                    DetectorToQueue,
                    AttackSequenceToQueue,
                    ReceiverToQueue,
                    in hit);
                ResolverToQueue = null;
            }
        }

        private GameObject resolverObject; // 씬 또는 시스템 참조
        private GameObject firstDetectorObject; // 씬 또는 시스템 참조
        private GameObject secondDetectorObject; // 씬 또는 시스템 참조

        [TearDown]
        public void TearDown()
        {
            if (firstDetectorObject != null)
            {
                Object.DestroyImmediate(firstDetectorObject);
            }

            if (secondDetectorObject != null)
            {
                Object.DestroyImmediate(secondDetectorObject);
            }

            if (resolverObject != null)
            {
                Object.DestroyImmediate(resolverObject);
            }
        }

        [Test]
        public void OpposingHits_AlreadyQueuedBothApply()
        {
            CombatHitResolver resolver = CreateResolver();
            MeleeHitDetector firstDetector =
                CreateDetector("FirstDetector", out firstDetectorObject);
            MeleeHitDetector secondDetector =
                CreateDetector("SecondDetector", out secondDetectorObject);
            var firstReceiver = new RecordingHitReceiver
            {
                DetectorToStop = secondDetector
            };
            var secondReceiver = new RecordingHitReceiver();
            var firstHit = new AttackHitData(
                new AttackDamage(10f), UnitTeam.Player, 1);
            var secondHit = new AttackHitData(
                new AttackDamage(10f), UnitTeam.Enemy, 1);

            bool firstQueued = resolver.QueueHit(
                firstDetector,
                1,
                firstReceiver,
                in firstHit);
            bool secondQueued = resolver.QueueHit(
                secondDetector,
                1,
                secondReceiver,
                in secondHit);

            Assert.That(firstQueued, Is.True);
            Assert.That(secondQueued, Is.True);
            Assert.That(firstReceiver.HitCount, Is.Zero);
            Assert.That(secondReceiver.HitCount, Is.Zero);

            resolver.ResolvePendingHits();

            Assert.That(firstReceiver.HitCount, Is.EqualTo(1));
            Assert.That(secondReceiver.HitCount, Is.EqualTo(1));
        }

        [Test]
        public void SameAttackAndTarget_QueuesOnce()
        {
            CombatHitResolver resolver = CreateResolver();
            MeleeHitDetector detector =
                CreateDetector("Detector", out firstDetectorObject);
            var receiver = new RecordingHitReceiver();
            var hit = new AttackHitData(
                new AttackDamage(10f), UnitTeam.Player, 1);

            bool firstQueued = resolver.QueueHit(
                detector,
                7,
                receiver,
                in hit);
            bool duplicateQueued = resolver.QueueHit(
                detector,
                7,
                receiver,
                in hit);

            resolver.ResolvePendingHits();

            Assert.That(firstQueued, Is.True);
            Assert.That(duplicateQueued, Is.False);
            Assert.That(receiver.HitCount, Is.EqualTo(1));
        }

        [Test]
        public void HitQueuedWhileResolving_WaitsForNextResolve()
        {
            CombatHitResolver resolver = CreateResolver();
            MeleeHitDetector firstDetector =
                CreateDetector("FirstDetector", out firstDetectorObject);
            MeleeHitDetector secondDetector =
                CreateDetector("SecondDetector", out secondDetectorObject);
            var secondReceiver = new RecordingHitReceiver();
            var secondHit = new AttackHitData(
                new AttackDamage(10f), UnitTeam.Enemy, 2);
            var firstReceiver = new RecordingHitReceiver
            {
                ResolverToQueue = resolver,
                DetectorToQueue = secondDetector,
                ReceiverToQueue = secondReceiver,
                HitToQueue = secondHit,
                AttackSequenceToQueue = 2
            };
            var firstHit = new AttackHitData(
                new AttackDamage(10f), UnitTeam.Player, 1);

            resolver.QueueHit(
                firstDetector,
                1,
                firstReceiver,
                in firstHit);

            resolver.ResolvePendingHits();

            Assert.That(firstReceiver.HitCount, Is.EqualTo(1));
            Assert.That(secondReceiver.HitCount, Is.Zero);
            Assert.That(resolver.PendingHitCount, Is.EqualTo(1));

            resolver.ResolvePendingHits();

            Assert.That(secondReceiver.HitCount, Is.EqualTo(1));
            Assert.That(resolver.PendingHitCount, Is.Zero);
        }

        private CombatHitResolver CreateResolver()
        {
            resolverObject = new GameObject("CombatHitResolverTest");
            return resolverObject.AddComponent<CombatHitResolver>();
        }

        private static MeleeHitDetector CreateDetector(
            string objectName,
            out GameObject detectorObject)
        {
            detectorObject = new GameObject(objectName);
            return detectorObject.AddComponent<MeleeHitDetector>();
        }
    }
}
