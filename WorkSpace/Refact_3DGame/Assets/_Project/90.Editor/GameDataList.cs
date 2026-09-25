using System;
using System.Collections.Generic;
using Boot;
using Data;
using UnityEditor;
using UnityEngine;

namespace EditorTools
{
    // 등록된 설정 목록과 실행 컨테이너의 참조를 읽는다. 게임 데이터의 생성·변경·해제는 하지 않는다.
    internal sealed class GameDataList
    {
        internal static readonly Type[] ContainerTypes =
        {
            typeof(PlayerDataContainer), typeof(EnemyDataContainer),
            typeof(ItemDataContainer), typeof(QuestDataContainer)
        };

        internal sealed class Entry
        {
            internal ScriptableObject Asset;
            internal bool Direct;
            internal string Source;
            internal string Path;
        }

        internal readonly List<Entry> Entries = new List<Entry>();
        private readonly HashSet<ScriptableObject> visited = new HashSet<ScriptableObject>();
        private readonly HashSet<ScriptableObject> used = new HashSet<ScriptableObject>();
        private readonly HashSet<ScriptableObject> previousUsed = new HashSet<ScriptableObject>();
        private readonly List<ScriptableObject> heldRoots = new List<ScriptableObject>();
        private readonly HashSet<ScriptableObject> previousRoots = new HashSet<ScriptableObject>();
        private Type containerType = typeof(PlayerDataContainer);
        private Boots boot;

        // 선택한 컨테이너가 실제로 보관한 원본 개수이며 연결 데이터는 포함하지 않는다.
        internal int HeldCount => heldRoots.Count;

        // 열린 씬의 Boot 입력을 우선 선택하고, 씬이 없어도 프로젝트의 등록 목록을 찾는다.
        internal GameData FindGameData()
        {
            FindBoot();
            if (boot != null && boot.CheatGameData != null) return boot.CheatGameData;
            string[] guids = AssetDatabase.FindAssets("t:GameData", new[] { "Assets/_Project" });
            System.Array.Sort(guids, System.StringComparer.Ordinal);
            return guids.Length == 0 ? null : AssetDatabase.LoadAssetAtPath<GameData>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        // 선택한 컨테이너의 등록 예정 원본과 실제 보관 원본을 함께 표시한다.
        internal void Read(GameData data, Type selectedContainer)
        {
            Entries.Clear();
            containerType = selectedContainer;
            RefreshUsage();
            visited.Clear();
            if (data != null)
            {
                if (containerType == typeof(PlayerDataContainer)) Add(data.Player, data.name + ".Player", true);
                if (containerType == typeof(EnemyDataContainer) && data.Enemies != null)
                    foreach (var enemy in data.Enemies) Add(enemy, data.name + ".Enemies", true);
                if (containerType == typeof(ItemDataContainer)) Add(data.Items, data.name + ".Items", true);
                if (containerType == typeof(QuestDataContainer)) Add(data.GroundQuest, data.name + ".GroundQuest", true);
            }
            foreach (var root in heldRoots) Add(root, containerType.Name + " (실행 보관)", true);
            Entries.Sort((a, b) =>
            {
                int direct = b.Direct.CompareTo(a.Direct);
                return direct != 0 ? direct : string.CompareOrdinal(a.Asset.name, b.Asset.name);
            });
        }

        // 설정의 직렬화된 참조를 따라가므로 공격 데이터 등 추가 연결도 목록에 포함된다.
        private void Add(ScriptableObject asset, string source, bool direct)
        {
            if (asset == null) return;
            if (!visited.Add(asset))
            {
                if (direct)
                    foreach (var entry in Entries) if (entry.Asset == asset) entry.Direct = true;
                return;
            }
            Entries.Add(new Entry { Asset = asset, Direct = direct, Source = source, Path = AssetDatabase.GetAssetPath(asset) });
            using (var serialized = new SerializedObject(asset))
            {
                var property = serialized.GetIterator();
                while (property.Next(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue is ScriptableObject child)
                        Add(child, asset.name + "." + property.propertyPath, false);
            }
        }

        // 실제 컨테이너가 보관한 원본과 그 연결 데이터를 모아 표시 변경 여부를 반환한다.
        internal bool RefreshUsage()
        {
            previousUsed.Clear();
            previousUsed.UnionWith(used);
            previousRoots.Clear();
            previousRoots.UnionWith(heldRoots);
            used.Clear();
            heldRoots.Clear();
            if (EditorApplication.isPlaying)
            {
                FindBoot();
                if (boot != null) boot.CopyCheatData(containerType, heldRoots);
                foreach (var root in heldRoots) AddUsed(root);
            }
            return !previousUsed.SetEquals(used) || !previousRoots.SetEquals(heldRoots);
        }

        // 엔티티 생존 여부 대신 데이터 컨테이너의 실제 참조 여부를 조회한다.
        internal bool IsUsed(ScriptableObject asset) => asset != null && used.Contains(asset);

        // 등록 예정 원본이 연결 경로로만 사용되는 경우와 실제 직접 보관을 구분한다.
        internal bool IsHeld(ScriptableObject asset) => asset != null && heldRoots.Contains(asset);

        // 데이터 그래프의 순환을 막고 연결된 원본을 사용 상태에 포함한다.
        private void AddUsed(ScriptableObject asset)
        {
            if (asset == null || !used.Add(asset)) return;
            using (var serialized = new SerializedObject(asset))
            {
                var property = serialized.GetIterator();
                while (property.Next(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue is ScriptableObject child)
                        AddUsed(child);
            }
        }

        // Boot 교체·Play 재시작 후에만 씬에서 다시 찾는다. 검색은 객체를 생성하지 않는다.
        private void FindBoot()
        {
            if (boot == null) boot = UnityEngine.Object.FindFirstObjectByType<Boots>(FindObjectsInactive.Include);
        }
    }
}
