using System;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // RustySword 검날의 시작점과 끝점으로 공격 Capsule을 만든다.
    [Serializable]
    public sealed class NightShadeSwordHitShape
    {
        [SerializeField] private Transform startPoint;
        [SerializeField] private Transform endPoint;
        [SerializeField, Min(0.01f)] private float radius = 0.16f;

        public Transform StartPoint => startPoint;
        public Transform EndPoint => endPoint;
        public float Radius => radius;
        public bool IsReady =>
            startPoint != null && endPoint != null && radius > 0f;

#if UNITY_EDITOR
        public void ConnectForEditor(
            Transform swordStartPoint,
            Transform swordEndPoint,
            float swordRadius)
        {
            startPoint = swordStartPoint;
            endPoint = swordEndPoint;
            radius = Mathf.Max(0.01f, swordRadius);
        }

        public void Validate()
        {
            radius = Mathf.Max(0.01f, radius);
        }
#endif
    }
}
