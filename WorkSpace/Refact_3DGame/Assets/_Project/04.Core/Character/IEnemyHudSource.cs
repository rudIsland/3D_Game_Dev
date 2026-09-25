using System;
using System.Collections.Generic;

namespace Core
{
    /// <summary>HUD가 현재 적을 조회하고 표시 대상의 활성·비활성 알림을 받는 계약이다.</summary>
    public interface IEnemyHudSource
    {
        /// <summary>연결 시 이미 활성화된 적을 확인한다. 목록 생성·반환은 제공자가 담당한다.</summary>
        IReadOnlyList<Unit> ActiveObjects { get; }

        /// <summary>기존 활성화 처리가 끝난 적을 표시 대상으로 알린다.</summary>
        event Action<Unit> EnemyEnabled;

        /// <summary>기존 비활성화 흐름에서 표시 구독을 해제할 적을 알린다.</summary>
        event Action<Unit> EnemyDisabled;
    }
}
