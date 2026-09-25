using Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace Item
{
    // 획득 시 반환하고 맵 종료 시 사용 중·대기 중인 객체를 모두 파괴한다.
    internal sealed class ItemPool : IDisposable
    {
        private readonly WorldItemPickup prefab;
        private readonly Transform root;
        private readonly List<WorldItemPickup> items = new List<WorldItemPickup>();
        private readonly Stack<WorldItemPickup> available = new Stack<WorldItemPickup>();
        private bool disposed;
        public ItemPool(WorldItemPickup prefab, UnityScene scene)
        {
            this.prefab = prefab;
            root = new GameObject(prefab.name + " Item Pool").transform;
            SceneManager.MoveGameObjectToScene(root.gameObject, scene);
        }
        public WorldItemPickup Take(ItemDefinition definition, Vector3 position, Quaternion rotation)
        {
            if (disposed) throw new ObjectDisposedException(nameof(ItemPool));
            WorldItemPickup item = null;
            while (available.Count > 0 && item == null) item = available.Pop();
            if (item == null)
            {
                item = Object.Instantiate(prefab, root);
                items.Add(item);
            }
            item.gameObject.SetActive(false);
            item.Prepare(this, definition);
            item.transform.SetPositionAndRotation(position, rotation);
            item.gameObject.SetActive(true);
            return item;
        }
        public void Return(WorldItemPickup item)
        {
            if (disposed || item == null || !item.IsTaken || !ReferenceEquals(item.OwnerPool, this)) return;
            item.IsTaken = false;
            item.gameObject.SetActive(false);
            available.Push(item);
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null) continue;
                items[i].IsTaken = false;
                items[i].gameObject.SetActive(false);
                if (Application.isPlaying) Object.Destroy(items[i].gameObject);
                else Object.DestroyImmediate(items[i].gameObject);
            }
            items.Clear();
            available.Clear();
            if (root != null)
            {
                if (Application.isPlaying) Object.Destroy(root.gameObject);
                else Object.DestroyImmediate(root.gameObject);
            }
        }
    }
}
