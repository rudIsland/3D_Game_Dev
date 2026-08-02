using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace rudIsland.RPG3D.Combat
{
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    // 같은 프레임에 발견한 모든 타격 후보를 모은 뒤 함께 적용한다.
    public sealed class CombatHitResolver : MonoBehaviour
    {
        private const int InitialHitCapacity = 32; // 피격 또는 피해 관련 값

        private List<PendingHit> pendingHits = // 피격 또는 피해 관련 값
            new List<PendingHit>(InitialHitCapacity);
        private List<PendingHit> resolvingHits = // 피격 또는 피해 관련 값
            new List<PendingHit>(InitialHitCapacity);
        private readonly HashSet<PendingHitKey> pendingHitKeys = // 피격 또는 피해 관련 값
            new HashSet<PendingHitKey>(InitialHitCapacity);

        internal int PendingHitCount => pendingHits.Count; // 피격 또는 피해 관련 값

        internal bool QueueHit(
            MeleeHitDetector sourceDetector,
            int attackSequence,
            IAttackHitReceiver receiver,
            in AttackHitData hit)
        {
            if (!isActiveAndEnabled ||
                sourceDetector == null ||
                !IsReceiverAvailable(receiver))
            {
                return false;
            }

            var hitKey = new PendingHitKey(
                sourceDetector,
                attackSequence,
                receiver);
            if (!pendingHitKeys.Add(hitKey))
            {
                return false;
            }

            pendingHits.Add(
                new PendingHit(
                    sourceDetector,
                    receiver,
                    hit));
            return true;
        }

        private void LateUpdate()
        {
            ResolvePendingHits();
        }

        // 해결 도중 새로 들어온 타격은 다음 묶음에 남도록 두 List를 교체한다.
        public void ResolvePendingHits()
        {
            if (pendingHits.Count == 0)
            {
                return;
            }

            List<PendingHit> hitsToResolve = pendingHits;
            pendingHits = resolvingHits;
            resolvingHits = hitsToResolve;
            pendingHitKeys.Clear();

            try
            {
                for (int index = 0;
                    index < resolvingHits.Count;
                    index++)
                {
                    ResolveHit(resolvingHits[index]);
                }
            }
            finally
            {
                resolvingHits.Clear();
            }
        }

        private static void ResolveHit(PendingHit pendingHit)
        {
            if (!IsReceiverAvailable(pendingHit.Receiver))
            {
                return;
            }

            AttackHitResult hitResult;
            try
            {
                AttackHitData hit = pendingHit.Hit;
                hitResult =
                    pendingHit.Receiver.ReceiveHit(in hit);
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception,
                    pendingHit.Receiver as UnityEngine.Object);
                return;
            }

            if (pendingHit.SourceDetector != null)
            {
                AttackHitData hit = pendingHit.Hit;
                pendingHit.SourceDetector.NotifyHitResolved(
                    hitResult,
                    in hit);
            }
        }

        private void OnDisable()
        {
            pendingHits.Clear();
            resolvingHits.Clear();
            pendingHitKeys.Clear();
        }

        private static bool IsReceiverAvailable(
            IAttackHitReceiver receiver)
        {
            if (receiver == null)
            {
                return false;
            }

            Component receiverComponent = receiver as Component;
            return ReferenceEquals(receiverComponent, null) ||
                receiverComponent != null;
        }

        private readonly struct PendingHit
        {
            internal MeleeHitDetector SourceDetector { get; } // 씬 또는 시스템 참조
            internal IAttackHitReceiver Receiver { get; } // 외부에 제공하는 읽기 값
            internal AttackHitData Hit { get; } // 피격 또는 피해 관련 값

            internal PendingHit(
                MeleeHitDetector sourceDetector,
                IAttackHitReceiver receiver,
                AttackHitData hit)
            {
                SourceDetector = sourceDetector;
                Receiver = receiver;
                Hit = hit;
            }
        }

        private readonly struct PendingHitKey :
            IEquatable<PendingHitKey>
        {
            private readonly MeleeHitDetector sourceDetector; // 씬 또는 시스템 참조
            private readonly int attackSequence; // 공격 관련 설정 또는 상태
            private readonly IAttackHitReceiver receiver; // 내부에서 사용하는 값

            internal PendingHitKey(
                MeleeHitDetector sourceDetector,
                int attackSequence,
                IAttackHitReceiver receiver)
            {
                this.sourceDetector = sourceDetector;
                this.attackSequence = attackSequence;
                this.receiver = receiver;
            }

            public bool Equals(PendingHitKey other)
            {
                return ReferenceEquals(
                        sourceDetector,
                        other.sourceDetector) &&
                    attackSequence == other.attackSequence &&
                    ReferenceEquals(receiver, other.receiver);
            }

            public override bool Equals(object other)
            {
                return other is PendingHitKey hitKey &&
                    Equals(hitKey);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = RuntimeHelpers.GetHashCode(
                        sourceDetector);
                    hash = (hash * 397) ^ attackSequence;
                    hash = (hash * 397) ^
                        RuntimeHelpers.GetHashCode(receiver);
                    return hash;
                }
            }
        }
    }
}
