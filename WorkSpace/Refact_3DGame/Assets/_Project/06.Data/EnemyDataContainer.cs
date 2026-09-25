using System;
using System.Collections.Generic;
using Core;
using Enemy;

namespace Data
{
    // 적 설정을 이름별로 보관한다. 맵의 소환 목록과 실행 객체는 관리하지 않는다.
    public sealed partial class EnemyDataContainer : Singleton<EnemyDataContainer>, IDisposable
    {
        private readonly Dictionary<string, EnemySpawnSettings> enemies = new Dictionary<string, EnemySpawnSettings>();

        /// <summary>Boots가 받은 적 설정 목록을 전역으로 등록한다.</summary>
        public static EnemyDataContainer Create(EnemySpawnSettings[] data) => StoreInstance(new EnemyDataContainer(data));

        // 맵이 적 이름으로 설정을 찾을 수 있도록 목록을 준비한다.
        private EnemyDataContainer(EnemySpawnSettings[] data)
        {
            foreach (EnemySpawnSettings enemy in data) enemies.Add(enemy.name, enemy);
        }

        /// <summary>맵이 지정한 이름에 해당하는 적 설정을 반환한다.</summary>
        public EnemySpawnSettings GetEnemy(string id) => enemies[id];

        /// <summary>게임 객체 반환 후 적 설정과 자신의 전역 등록을 비운다.</summary>
        public void Dispose()
        {
            enemies.Clear();
            ClearInstance();
        }
    }
}
