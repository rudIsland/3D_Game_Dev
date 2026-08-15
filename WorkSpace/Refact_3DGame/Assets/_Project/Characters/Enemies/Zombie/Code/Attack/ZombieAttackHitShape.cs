using System;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    // 한 공격이 사용하는 신체 부위의 시작점, 끝점과 두께를 보관한다.
    [Serializable]
    internal sealed class ZombieAttackHitShape
    {
        [SerializeField] private Transform startPoint;
        [SerializeField] private Transform endPoint;
        [SerializeField, Min(0f)] private float radius = 0.18f;

        internal Transform StartPoint => startPoint;
        internal Transform EndPoint => endPoint;
        internal float Radius => Mathf.Max(0f, radius);
        internal bool IsReady =>
            startPoint != null &&
            endPoint != null &&
            Radius > 0f;

#if UNITY_EDITOR
        internal void Validate()
        {
            radius = Mathf.Max(0f, radius);
        }
#endif
    }
}
