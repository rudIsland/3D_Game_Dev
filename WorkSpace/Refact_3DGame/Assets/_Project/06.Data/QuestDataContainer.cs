using System;
using Core;
using Quest;

namespace Data
{
    // 퀘스트 설정만 보관한다. 실행 객체의 상태와 수명은 관리하지 않는다.
    public sealed class QuestDataContainer : Singleton<QuestDataContainer>, IDisposable
    {
        /// <summary>게임 흐름이 엔티티에 전달할 퀘스트 설정이다.</summary>
        public GroundQuestConfig Ground { get; private set; }

        /// <summary>Boots가 받은 퀘스트 설정을 전역으로 등록한다.</summary>
        public static QuestDataContainer Create(GroundQuestConfig data) => StoreInstance(new QuestDataContainer(data));

        // 전달받은 설정 원본을 보관한다.
        private QuestDataContainer(GroundQuestConfig data) { Ground = data; }

        /// <summary>게임 객체 반환 후 설정 참조와 자신의 전역 등록을 비운다.</summary>
        public void Dispose()
        {
            Ground = null;
            ClearInstance();
        }
    }
}
