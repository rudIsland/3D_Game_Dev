using UnityEngine;

namespace rudIsland.RPG3D.Animation
{
    // Animator의 현재 재생 상태와 전환 상태만 읽는다.
    public sealed class AnimatorPlaybackReader
    {
        private readonly Animator animator;

        public AnimatorPlaybackReader(Animator animator)
        {
            this.animator = animator;
        }

        public bool CanRead(int layerIndex)
        {
            return animator != null &&
                animator.isActiveAndEnabled &&
                animator.runtimeAnimatorController != null &&
                layerIndex >= 0 &&
                animator.layerCount > layerIndex;
        }

        public bool IsInTransition(int layerIndex)
        {
            return CanRead(layerIndex) && animator.IsInTransition(layerIndex);
        }

        public bool TryGetCurrentState(
            int layerIndex,
            out AnimatorStateInfo stateInfo)
        {
            stateInfo = default;
            if (!CanRead(layerIndex))
            {
                return false;
            }

            stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
            return true;
        }

        public bool IsCurrentState(int layerIndex, int stateHash)
        {
            return TryGetCurrentState(layerIndex, out AnimatorStateInfo stateInfo) &&
                stateInfo.shortNameHash == stateHash;
        }

        public bool IsChangingTo(int layerIndex, int stateHash)
        {
            if (!CanRead(layerIndex) || !animator.IsInTransition(layerIndex))
            {
                return false;
            }

            return animator.GetNextAnimatorStateInfo(layerIndex).shortNameHash ==
                stateHash;
        }

        public bool IsCurrentOrNextState(int layerIndex, int stateHash)
        {
            return IsCurrentState(layerIndex, stateHash) ||
                IsChangingTo(layerIndex, stateHash);
        }

        public bool TryGetCurrentStateTime(
            int layerIndex,
            int stateHash,
            out float normalizedTime)
        {
            normalizedTime = 0f;
            if (!TryGetCurrentState(layerIndex, out AnimatorStateInfo stateInfo) ||
                stateInfo.shortNameHash != stateHash)
            {
                return false;
            }

            normalizedTime = stateInfo.normalizedTime;
            return true;
        }

        public bool TryGetCurrentFullPathStateTime(
            int layerIndex,
            int stateHash,
            out float normalizedTime)
        {
            normalizedTime = 0f;
            if (!TryGetCurrentState(layerIndex, out AnimatorStateInfo stateInfo) ||
                stateInfo.fullPathHash != stateHash)
            {
                return false;
            }

            normalizedTime = stateInfo.normalizedTime;
            return true;
        }

        public bool TryGetCurrentOrNextStateTime(
            int layerIndex,
            int stateHash,
            out float normalizedTime)
        {
            normalizedTime = 0f;
            if (!TryGetCurrentState(layerIndex, out AnimatorStateInfo stateInfo))
            {
                return false;
            }

            if (stateInfo.shortNameHash == stateHash)
            {
                normalizedTime = stateInfo.normalizedTime;
                return true;
            }

            if (!animator.IsInTransition(layerIndex))
            {
                return false;
            }

            stateInfo = animator.GetNextAnimatorStateInfo(layerIndex);
            if (stateInfo.shortNameHash != stateHash)
            {
                return false;
            }

            normalizedTime = stateInfo.normalizedTime;
            return true;
        }
    }
}
