using System;
using Core;
using Item;

namespace Data
{
    // 아이템 설정만 보관한다. 실행 객체의 상태와 수명은 관리하지 않는다.
    public sealed class ItemDataContainer : Singleton<ItemDataContainer>, IDisposable
    {
        /// <summary>게임 흐름이 엔티티에 전달할 아이템 설정이다.</summary>
        public ItemCatalog Catalog { get; private set; }

        /// <summary>Boots가 받은 아이템 설정을 전역으로 등록한다.</summary>
        public static ItemDataContainer Create(ItemCatalog data) => StoreInstance(new ItemDataContainer(data));

        // 전달받은 설정 원본을 보관한다.
        private ItemDataContainer(ItemCatalog data) { Catalog = data; }

        /// <summary>게임 객체 반환 후 설정 참조와 자신의 전역 등록을 비운다.</summary>
        public void Dispose()
        {
            Catalog = null;
            ClearInstance();
        }
    }
}
