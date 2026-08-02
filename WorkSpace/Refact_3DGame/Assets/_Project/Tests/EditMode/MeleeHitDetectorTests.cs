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
            public int HitCount { get; private set; } // 피격 또는 피해 관련 값
            public AttackHitData LastHit { get; private set; } // 피격 또는 피해 관련 값
            public AttackHitResult ResultToReturn { get; set; } = // 외부에 제공하는 읽기 값
                AttackHitResult.Damaged;

            public AttackHitResult ReceiveHit(in AttackHitData hit)
            {
                HitCount++;
                LastHit = hit;
                return ResultToReturn;
            }
        }

        private GameObject detectorObject; // 씬 또는 시스템 참조
        private GameObject targetObject; // 대상 참조
        private GameObject resolverObject; // 씬 또는 시스템 참조
        private CombatHitResolver hitResolver; // 피격 또는 피해 관련 값

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

            if (resolverObject != null)
            {
                Object.DestroyImmediate(resolverObject);
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

            Assert.That(receiver.HitCount, Is.Zero);
            ResolvePendingHits();

            Assert.That(receiver.HitCount, Is.EqualTo(1));
            Assert.That(
                receiver.LastHit.Damage.HealthDamage,
                Is.EqualTo(12.5f));
            Assert.That(receiver.LastHit.AttackerTeam, Is.EqualTo(UnitTeam.Player));
            Assert.That(receiver.LastHit.AttackNumber, Is.EqualTo(2));

            detector.EndHit();
            detector.StartHit(in hit);
            ResolvePendingHits();

            Assert.That(receiver.HitCount, Is.EqualTo(2));
        }

        [Test]
        public void CurrentBladeHit_StoresClosestPointNormalAndDirection()
        {
            MeleeHitDetector detector = CreateDetector();
            RecordingHitReceiver receiver = CreateTarget();
            targetObject.transform.position = new Vector3(0.55f, 0f, 1f);
            var hit = new AttackHitData(
                new AttackDamage(10f), UnitTeam.Player, 1);

            Physics.SyncTransforms();
            detector.StartHit(in hit);
            ResolvePendingHits();

            Assert.That(receiver.HitCount, Is.EqualTo(1));
            AssertVectorApproximately(
                receiver.LastHit.HitPoint,
                new Vector3(0.05f, 0f, 1f));
            AssertVectorApproximately(
                receiver.LastHit.HitNormal,
                Vector3.left);
            AssertVectorApproximately(
                receiver.LastHit.HitDirection,
                Vector3.right);
            Assert.That(
                receiver.LastHit.HitBodyPart,
                Is.EqualTo(HitBodyPart.Body));
        }

        [Test]
        public void ActiveHit_SendsReceiverResultToAttacker()
        {
            MeleeHitDetector detector = CreateDetector();
            RecordingHitReceiver receiver = CreateTarget();
            receiver.ResultToReturn = AttackHitResult.Blocked;
            int resultCount = 0;
            AttackHitResult receivedResult = AttackHitResult.Ignored;
            AttackHitData receivedHit = default;
            detector.HitResultReady += (hitResult, hit) =>
            {
                resultCount++;
                receivedResult = hitResult;
                receivedHit = hit;
            };
            var hitData = new AttackHitData(
                new AttackDamage(10f), UnitTeam.Player, 1);

            Physics.SyncTransforms();
            detector.StartHit(in hitData);
            detector.DetectActiveHit();

            Assert.That(resultCount, Is.Zero);
            ResolvePendingHits();

            Assert.That(resultCount, Is.EqualTo(1));
            Assert.That(
                receivedResult,
                Is.EqualTo(AttackHitResult.Blocked));
            Assert.That(
                receivedHit.Damage.HealthDamage,
                Is.EqualTo(10f));
        }

        [Test]
        public void UnitHitBox_StoresItsBodyPart()
        {
            MeleeHitDetector detector = CreateDetector();
            RecordingHitReceiver receiver =
                CreateTargetWithHitBox(
                    HitBodyPart.Head,
                    Vector3.zero);
            var hit = new AttackHitData(
                new AttackDamage(10f), UnitTeam.Player, 1);

            Physics.SyncTransforms();
            detector.StartHit(in hit);
            ResolvePendingHits();

            Assert.That(receiver.HitCount, Is.EqualTo(1));
            Assert.That(
                receiver.LastHit.HitBodyPart,
                Is.EqualTo(HitBodyPart.Head));
        }

        [Test]
        public void UnitHitBox_TargetIgnoresMovementCollider()
        {
            MeleeHitDetector detector = CreateDetector();
            RecordingHitReceiver receiver =
                CreateTargetWithHitBox(
                    HitBodyPart.Body,
                    Vector3.forward * 5f);
            var hit = new AttackHitData(
                new AttackDamage(10f), UnitTeam.Player, 1);

            Physics.SyncTransforms();
            detector.StartHit(in hit);
            ResolvePendingHits();

            Assert.That(receiver.HitCount, Is.Zero);
        }

        [Test]
        public void SphereShape_UsesOneAttackPoint()
        {
            MeleeHitDetector detector =
                CreateDetector(AttackShapeType.Sphere);
            RecordingHitReceiver receiver = CreateTarget();
            targetObject.transform.position =
                new Vector3(0.1f, 0f, 0f);
            targetObject.GetComponent<BoxCollider>().size =
                Vector3.one * 0.1f;
            var hit = new AttackHitData(
                new AttackDamage(10f), UnitTeam.Enemy, 1);

            Physics.SyncTransforms();
            detector.StartHit(in hit);
            ResolvePendingHits();

            Assert.That(receiver.HitCount, Is.EqualTo(1));
        }

        [Test]
        public void BoxShape_HitsTargetInsideBox()
        {
            MeleeHitDetector detector =
                CreateDetector(AttackShapeType.Box);
            RecordingHitReceiver receiver = CreateTarget();
            targetObject.transform.position =
                new Vector3(0.4f, 0f, 0f);
            targetObject.GetComponent<BoxCollider>().size =
                Vector3.one * 0.1f;
            var hit = new AttackHitData(
                new AttackDamage(10f), UnitTeam.Enemy, 1);

            Physics.SyncTransforms();
            detector.StartHit(in hit);
            ResolvePendingHits();

            Assert.That(receiver.HitCount, Is.EqualTo(1));
        }

        [Test]
        public void FastSphereMovement_HitsTargetBetweenFramesOnce()
        {
            MeleeHitDetector detector =
                CreateDetector(AttackShapeType.Sphere);
            Transform attackPoint =
                detectorObject.transform.GetChild(0);
            attackPoint.position = Vector3.left;

            RecordingHitReceiver receiver = CreateTarget();
            targetObject.transform.position = Vector3.zero;
            targetObject.GetComponent<BoxCollider>().size =
                Vector3.one * 0.1f;
            var hit = new AttackHitData(
                new AttackDamage(10f), UnitTeam.Enemy, 1);

            Physics.SyncTransforms();
            detector.StartHit(in hit);
            ResolvePendingHits();

            Assert.That(receiver.HitCount, Is.Zero);

            attackPoint.position = Vector3.right;
            Physics.SyncTransforms();
            detector.DetectActiveHit();
            detector.DetectActiveHit();
            ResolvePendingHits();

            Assert.That(receiver.HitCount, Is.EqualTo(1));
        }

        [Test]
        public void FastBoxMovement_HitsTargetBetweenFramesOnce()
        {
            MeleeHitDetector detector =
                CreateDetector(AttackShapeType.Box);
            Transform boxCenter =
                detectorObject.transform.GetChild(0);
            boxCenter.position = Vector3.left;

            RecordingHitReceiver receiver = CreateTarget();
            targetObject.transform.position = Vector3.zero;
            targetObject.GetComponent<BoxCollider>().size =
                Vector3.one * 0.1f;
            var hit = new AttackHitData(
                new AttackDamage(10f), UnitTeam.Enemy, 1);

            Physics.SyncTransforms();
            detector.StartHit(in hit);
            ResolvePendingHits();

            Assert.That(receiver.HitCount, Is.Zero);

            boxCenter.position = Vector3.right;
            Physics.SyncTransforms();
            detector.DetectActiveHit();
            detector.DetectActiveHit();
            ResolvePendingHits();

            Assert.That(receiver.HitCount, Is.EqualTo(1));
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
            ResolvePendingHits();

            Assert.That(receiver.HitCount, Is.EqualTo(0));

            hitStart.position = Vector3.right;
            hitEnd.position = Vector3.right;
            Physics.SyncTransforms();

            detector.DetectActiveHit();
            detector.DetectActiveHit();
            ResolvePendingHits();

            Assert.That(receiver.HitCount, Is.EqualTo(1));
            Assert.That(
                receiver.LastHit.Damage.HealthDamage,
                Is.EqualTo(10f));
            Assert.That(receiver.LastHit.HitPoint.x, Is.LessThan(0f));
            AssertVectorApproximately(
                receiver.LastHit.HitNormal,
                Vector3.left);
            AssertVectorApproximately(
                receiver.LastHit.HitDirection,
                Vector3.right);
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
            ResolvePendingHits();

            Assert.That(receiver.HitCount, Is.EqualTo(0));

            hitEnd.position = Vector3.forward * 2f;
            Physics.SyncTransforms();

            detector.DetectActiveHit();
            ResolvePendingHits();

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

        private MeleeHitDetector CreateDetector(
            AttackShapeType shapeType = AttackShapeType.Capsule)
        {
            resolverObject = new GameObject("CombatHitResolverTest");
            hitResolver =
                resolverObject.AddComponent<CombatHitResolver>();

            detectorObject = new GameObject("MeleeHitDetectorTest");
            detectorObject.SetActive(false);
            detectorObject.transform.SetParent(resolverObject.transform);

            Transform hitStart = CreateHitPoint("HitStart", 0f);
            Transform hitEnd = CreateHitPoint("HitEnd", 2f);
            MeleeHitDetector detector =
                detectorObject.AddComponent<MeleeHitDetector>();

            var serializedDetector = new SerializedObject(detector);
            SerializedProperty attackShape =
                serializedDetector.FindProperty("attackShape");
            attackShape.FindPropertyRelative("shapeType").enumValueIndex =
                (int)shapeType;
            attackShape.FindPropertyRelative("startPoint").objectReferenceValue =
                hitStart;
            attackShape.FindPropertyRelative("endPoint").objectReferenceValue =
                hitEnd;
            attackShape.FindPropertyRelative("radius").floatValue = 0.2f;
            attackShape.FindPropertyRelative("boxSize").vector3Value =
                Vector3.one;
            serializedDetector.FindProperty("targetLayers").intValue =
                1 << 0;
            serializedDetector.ApplyModifiedPropertiesWithoutUndo();

            detectorObject.SetActive(true);
            return detector;
        }

        private void ResolvePendingHits()
        {
            hitResolver.ResolvePendingHits();
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

        private RecordingHitReceiver CreateTargetWithHitBox(
            HitBodyPart bodyPart,
            Vector3 localPosition)
        {
            RecordingHitReceiver receiver = CreateTarget();

            var hitBoxObject = new GameObject("UnitHitBox");
            hitBoxObject.layer = 0;
            hitBoxObject.transform.SetParent(targetObject.transform);
            hitBoxObject.transform.localPosition = localPosition;
            hitBoxObject.transform.localRotation = Quaternion.identity;

            SphereCollider hitCollider =
                hitBoxObject.AddComponent<SphereCollider>();
            hitCollider.radius = 0.5f;
            hitCollider.isTrigger = true;

            UnitHitBox unitHitBox =
                hitBoxObject.AddComponent<UnitHitBox>();
            var serializedHitBox = new SerializedObject(unitHitBox);
            serializedHitBox.FindProperty("bodyPart").enumValueIndex =
                (int)bodyPart;
            serializedHitBox.ApplyModifiedPropertiesWithoutUndo();

            return receiver;
        }

        private static void AssertVectorApproximately(
            Vector3 actual,
            Vector3 expected)
        {
            Assert.That(
                Vector3.Distance(actual, expected),
                Is.LessThan(0.001f));
        }
    }
}
