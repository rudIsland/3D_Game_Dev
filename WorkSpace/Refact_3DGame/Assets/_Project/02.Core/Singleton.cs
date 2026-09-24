using System;

namespace Core
{
    // 타입마다 인스턴스 하나를 보관한다. 생성에 필요한 입력과 자원 정리는 각 자식 클래스가 맡는다.
    // Unity 메인 스레드에서 사용하는 일반 C# 객체용이며 GameObject를 생성하지 않는다.
    public abstract class Singleton<T> where T : Singleton<T>
    {
        private static T instance;

        /// <summary>준비된 단일 인스턴스를 반환한다. Create가 필요한 타입을 준비하지 않았으면 예외를 낸다.</summary>
        public static T Instance => instance ??
            throw new InvalidOperationException(typeof(T).Name + ".Create를 먼저 호출하세요.");

        // 자식 클래스가 중복 생성과 입력 변경 여부를 검사할 때 사용한다.
        protected static T CurrentInstance => instance;

        // 자식 클래스의 private 생성자로 만든 객체만 등록한다.
        protected static T StoreInstance(T value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (instance != null)
                throw new InvalidOperationException(typeof(T).Name + " 인스턴스가 이미 있습니다.");
            instance = value;
            return instance;
        }

        // Dispose를 끝낸 객체가 현재 등록된 객체일 때만 참조를 해제한다.
        protected void ClearInstance()
        {
            if (ReferenceEquals(instance, this)) instance = null;
        }

        // 각 구체 타입의 Play 초기화 콜백에서 호출한다. 게임 중 자원 정리를 대신하지 않는다.
        protected static void ResetInstance() { instance = null; }
    }
}
