using System;
using UnityEngine;

namespace World
{
    // 맵의 시작 위치와 환경음 참조를 제공한다. 객체 검색과 에셋 로딩은 수행하지 않는다.
    [DisallowMultipleComponent]
    public sealed class MapScene : MonoBehaviour
    {
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private AudioSource ambientSource;

        /// <summary>Inspector에 연결된 플레이어 시작 위치다.</summary>
        public Transform PlayerSpawnPoint => playerSpawnPoint;

        /// <summary>맵 준비 전에 필수 참조와 같은 씬 소속을 확인한다. 누락은 예외로 알린다.</summary>
        public void Init()
        {
            if (playerSpawnPoint == null || playerSpawnPoint.gameObject.scene != gameObject.scene)
                throw new InvalidOperationException("Ground MapScene의 playerSpawnPoint에 같은 씬의 StartArrival을 연결하세요.");
            if (ambientSource == null || ambientSource.clip == null ||
                ambientSource.gameObject.scene != gameObject.scene || !ambientSource.isActiveAndEnabled)
                throw new InvalidOperationException("Ground MapScene의 활성 환경음 AudioSource와 AudioClip을 연결하세요.");
        }

        /// <summary>플레이어와 HUD 준비 후 환경음을 한 번 시작한다.</summary>
        public void PlayAmbient()
        {
            if (!ambientSource.isPlaying) ambientSource.Play();
        }

        /// <summary>맵 요청을 반환하기 전에 환경음을 정지한다.</summary>
        public void StopAmbient()
        {
            if (ambientSource != null) ambientSource.Stop();
        }
    }
}
