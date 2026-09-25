#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Data
{
    // Editor 데이터 창에서 실제 적 설정 보관 목록을 읽는다.
    public sealed partial class EnemyDataContainer
    {
        /// <summary>보관 중인 적 설정을 결과 목록에 추가한다. 등록·반환 상태를 변경하지 않는다.</summary>
        public void CopyCheatData(List<ScriptableObject> result)
        {
            foreach (var data in enemies.Values) result.Add(data);
        }
    }
}
#endif
