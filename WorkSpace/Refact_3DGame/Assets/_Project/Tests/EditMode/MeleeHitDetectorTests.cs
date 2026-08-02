using NUnit.Framework;
using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Combat;
using UnityEditor;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class MeleeHitDetectorTests
    {
        private sealed class RecordingHitReceiver : MonoBehaviour,
            IAttackHitReceiver
        {
            public int HitCount { get; private set; }
            public AttackHitData LastHit { get; private set; }

            public void ReceiveHit(in AttackHitData hit)
            {
                HitCount++;
                LastHit = hit;
            }
        }

        private GameObject detectorObject;
        private GameObject targetObject;

        [TearDown]
        public void TearDown()
        {
            if (detectorObject != null)
            {
                Object.DestroyImmediate(detectorObject);
            }

            if (targetObject != null)
            {
                Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void ActiveHit_HitsTargetOnceAndNextHitCanHitAgain()
        {
            MeleeHitDetector detector = CreateDetector();
            RecordingHitReceiver receiver = CreateTarget();
            var hit = new AttackHitData(
                new AttackDamage(12.5f), UnitTeam.Player, 2);

            Physics.SyncTransforms();

            detector.StartHit(in hit);
            detector.DetectActiveHit();

            Assert.That(receiver.HitCount, Is.EqualTo(1));
            Assert.That(
                receiver.LastHit.Damage.HealthDamage,
                Is.EqualTo(12.5f));
            Assert.That(receiver.LastHit.AttackerTeam, Is.EqualTo(UnitTeam.Player));
            Assert.That(receiver.LastHit.AttackNumber, Is.EqualTo(2));

            detector.EndHit();
            detector.StartHit(in hit);

            Assert.That(receiver.HitCount, Is.EqualTo(2));
        }

        [Test]
        public void FastHitEndMovement_HitsTargetBetweenFramesOnce()
        {
            MeleeHitDetector detector = CreateDetector();
            Transform hitStart =
                detectorObject.transform.GetChild(0);
            Transform hitEnd =
                detectorObject.transform.GetChild(1);

            hitStart.position = Vector3.left;
            hitEnd.position = Vector3.left;

            RecordingHitReceiver receiver = CreateTarget();
            targetObject.transform.position = Vector3.zero;
            var hit = new AttackHitData(
                new AttackDamage(10f), UnitTeam.Player, 1);

            Physics.SyncTransforms();
            detector.StartHit(in hit);

            Assert.That(receiver.HitCount, Is.EqualTo(0));

            hitStart.position = Vector3.right;
            hitEnd.position = Vector3.right;
            Physics.SyncTransforms();

            detector.DetectActiveHit();
            detector.DetectActiveHit();

            Assert.That(receiver.HitCount, Is.EqualTo(1));
            Assert.That(
                receiver.LastHit.Damage.HealthDamage,
                Is.EqualTo(10f));
        }

        [Test]
        public void RotatingBlade_HitsTargetCrossedByMiddlePoint()
        {
            MeleeHitDetector detector = CreateDetector();
            Transform hitStart =
                detectorObject.transform.GetChild(0);
            Transform hitEnd =
                detectorObject.transform.GetChild(1);

            hitStart.position = Vector3.zero;
            hitEnd.position = Vector3.left * 2f;

            RecordingHitReceiver receiver = CreateTarget();
            targetObject.transform.position =
                new Vector3(-0.5f, 0f, 0.5f);
            targetObject.GetComponent<BoxCollider>().size =
                Vector3.one * 0.1f;
            var hit = new AttackHitData(
                new AttackDamage(10f), UnitTeam.Player, 1);

            Physics.SyncTransforms();
            detector.StartHit(in hit);

            Assert.That(receiver.HitCount, Is.EqualTo(0));

            hitEnd.position = Vector3.forward * 2f;
            Physics.SyncTransforms();

            detector.DetectActiveHit();

            Assert.That(receiver.HitCount, Is.EqualTo(1));
        }

        [Test]
        public void EndHit_StopsCurrentHit()
        {
            MeleeHitDetector detector = CreateDetector();
            var hit = new AttackHitData(
                new AttackDamage(10f), UnitTeam.Player, 1);

            detector.StartHit(in hit);
            detector.EndHit();

            Assert.That(detector.IsHitActive, Is.False);
        }

        private MeleeHitDetector CreateDetector()
        {
            detectorObject = new GameObject("MeleeHitDetectorTest");
            detectorObject.SetActive(false);

            Transform hitStart = CreateHitPoint("HitStart", 0f);
            Transform hitEnd = CreateHitPoint("HitEnd", 2f);
            MeleeHitDetector detector =
                detectorObject.AddComponent<MeleeHitDetector>();

            var serializedDetector = new SerializedObject(detector);
            serializedDetector.FindProperty("hitStart").objectReferenceValue =
                hitStart;
            serializedDetector.FindProperty("hitEnd").objectReferenceValue =
                hitEnd;
            serializedDetector.FindProperty("hitRadius").floatValue = 0.2f;
            serializedDetector.FindProperty("targetLayers").intValue =
                1 << 0;
            serializedDetector.ApplyModifiedPropertiesWithoutUndo();

            detectorObject.SetActive(true);
            return detector;
        }

        private Transform CreateHitPoint(string pointName, float zPosition)
        {
            var pointObject = new GameObject(pointName);
            pointObject.transform.SetParent(detectorObject.transform);
            pointObject.transform.position =
                new Vector3(0f, 0f, zPosition);
            return pointObject.transform;
        }

        private RecordingHitReceiver CreateTarget()
        {
            targetObject = new GameObject("HitTarget");
            targetObject.layer = 0;
            targetObject.transform.position = new Vector3(0f, 0f, 1f);
            targetObject.AddComponent<BoxCollider>();
            return targetObject.AddComponent<RecordingHitReceiver>();
        }
    }
}
