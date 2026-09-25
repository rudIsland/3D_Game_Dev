using System;
using UnityEngine;

namespace Manager
{
    // 카메라의 리스너 참조와 듣기 상태를 관리한다. 컴포넌트의 생성·파괴는 소속 씬이 맡는다.
    public sealed class AudioListenerManager
    {
        private AudioListener listener;

        /// <summary>전달받은 카메라의 리스너를 한 번 확인해 보관한다. 없으면 설정 오류를 알린다.</summary>
        public AudioListenerManager(Camera camera)
        {
            if (camera == null) throw new ArgumentNullException(nameof(camera));
            listener = camera.GetComponent<AudioListener>();
            if (listener == null)
                throw new InvalidOperationException("전달한 카메라에 AudioListener가 필요합니다.");
        }

        /// <summary>활성 카메라 오브젝트의 리스너를 켠다. 참조 해제 후에는 다시 사용할 수 없다.</summary>
        public void EnableListener()
        {
            if (listener == null)
                throw new InvalidOperationException("사용할 AudioListener가 없거나 이미 참조를 해제했습니다.");
            if (!listener.gameObject.activeInHierarchy)
                throw new InvalidOperationException("AudioListener가 붙은 카메라 오브젝트를 먼저 활성화하세요.");
            listener.enabled = true;
        }

        /// <summary>연결된 리스너를 끈다. 반복 요청과 이미 파괴된 컴포넌트는 안전하게 처리한다.</summary>
        public void DisableListener()
        {
            if (listener != null) listener.enabled = false;
        }

        /// <summary>리스너를 끄고 보관한 참조를 지운다. 씬이 소유한 컴포넌트는 파괴하지 않는다.</summary>
        public void Release()
        {
            if (listener != null) listener.enabled = false;
            listener = null;
        }
    }
}
