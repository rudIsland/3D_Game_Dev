using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using World.Zones;

namespace EditorTools
{
    // Zone 안에서 NavMesh를 벗어난 적 생성 지점만 가장 가까운 유효 위치로 옮긴다.
    public static class EnemySpawnPointNavMeshRepair
    {
        private const string ScenePath =
            "Assets/_Project/0_Scenes/Scene2.unity";
        private const float SearchStep = 0.5f;
        private const float CandidateSampleRadius = 1f;
        private const float ZoneEdgeMargin = 0.6f;
        private const float MinimumSpawnSeparation = 2f;

        [MenuItem("Tools/RPG3D/Fix Enemy Spawn Points")]
        public static void RepairEnemySpawnPoints()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    $"Scene2를 연 뒤 실행하세요. 현재 씬: {activeScene.path}");
            }

            EnemyZoneController[] zoneControllers =
                UnityEngine.Object.FindObjectsByType<EnemyZoneController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            int repairedCount = 0;
            for (int zoneIndex = 0;
                 zoneIndex < zoneControllers.Length;
                 zoneIndex++)
            {
                repairedCount += RepairZoneSpawnPoints(
                    zoneControllers[zoneIndex]);
            }

            ValidateSpawnPoints(zoneControllers);

            if (repairedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
            }

            Debug.Log(
                $"적 생성 지점 NavMesh 검사 완료: {repairedCount}개 위치 수정");
        }

        [MenuItem("Tools/RPG3D/Rebuild Scene2 NavMesh")]
        public static void RebuildSceneNavMesh()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    $"Scene2를 연 뒤 실행하세요. 현재 씬: {activeScene.path}");
            }

            EditorSceneManager.SaveScene(activeScene);

            GameObject navigationObject =
                GameObject.Find("GroundNavigation");
            Component navMeshSurface = navigationObject != null
                ? navigationObject.GetComponent("NavMeshSurface")
                : null;
            MethodInfo buildMethod = navMeshSurface != null
                ? navMeshSurface.GetType().GetMethod("BuildNavMesh")
                : null;

            if (buildMethod == null)
            {
                throw new InvalidOperationException(
                    "GroundNavigation의 NavMeshSurface를 찾지 못했습니다.");
            }

            buildMethod.Invoke(navMeshSurface, null);
            AssetDatabase.SaveAssets();

            EnemyZoneController[] zoneControllers =
                UnityEngine.Object.FindObjectsByType<EnemyZoneController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            ValidateSpawnPoints(zoneControllers);

            Debug.Log(
                "Scene2 GroundNavigation 재베이크 및 스폰 지점 검증 완료");
        }

        private static int RepairZoneSpawnPoints(
            EnemyZoneController zoneController)
        {
            SerializedObject serializedController =
                new SerializedObject(zoneController);
            Transform spawnPointRoot = serializedController
                .FindProperty("enemySpawnPoints")
                .objectReferenceValue as Transform;
            float sampleRadius = serializedController
                .FindProperty("navMeshSampleRadius")
                .floatValue;
            BoxCollider zoneCollider =
                zoneController.GetComponent<BoxCollider>();

            if (spawnPointRoot == null || zoneCollider == null)
            {
                return 0;
            }

            List<Vector3> reservedPositions = new List<Vector3>(
                spawnPointRoot.childCount);
            for (int index = 0;
                 index < spawnPointRoot.childCount;
                 index++)
            {
                Transform spawnPoint = spawnPointRoot.GetChild(index);
                if (NavMesh.SamplePosition(
                        spawnPoint.position,
                        out NavMeshHit validHit,
                        sampleRadius,
                        NavMesh.AllAreas))
                {
                    reservedPositions.Add(validHit.position);
                }
            }

            int repairedCount = 0;
            for (int index = 0;
                 index < spawnPointRoot.childCount;
                 index++)
            {
                Transform spawnPoint = spawnPointRoot.GetChild(index);
                if (NavMesh.SamplePosition(
                        spawnPoint.position,
                        out _,
                        sampleRadius,
                        NavMesh.AllAreas))
                {
                    continue;
                }

                if (!TryFindNearestZoneNavMeshPosition(
                        zoneCollider,
                        spawnPoint.position,
                        reservedPositions,
                        out Vector3 repairedPosition))
                {
                    throw new InvalidOperationException(
                        $"{zoneController.name}/{spawnPoint.name}을 " +
                        "옮길 유효한 NavMesh 위치가 Zone 안에 없습니다.");
                }

                Vector3 previousPosition = spawnPoint.position;
                Undo.RecordObject(spawnPoint, "Fix Enemy Spawn Point");
                spawnPoint.position = repairedPosition;
                EditorUtility.SetDirty(spawnPoint);
                reservedPositions.Add(repairedPosition);
                repairedCount++;

                Debug.Log(
                    $"{zoneController.name}/{spawnPoint.name}: " +
                    $"{previousPosition} -> {repairedPosition}",
                    spawnPoint);
            }

            return repairedCount;
        }

        private static bool TryFindNearestZoneNavMeshPosition(
            BoxCollider zoneCollider,
            Vector3 originalPosition,
            List<Vector3> reservedPositions,
            out Vector3 nearestPosition)
        {
            Vector3 center = zoneCollider.center;
            Vector3 halfSize = zoneCollider.size * 0.5f;
            float minX = center.x - halfSize.x + ZoneEdgeMargin;
            float maxX = center.x + halfSize.x - ZoneEdgeMargin;
            float minZ = center.z - halfSize.z + ZoneEdgeMargin;
            float maxZ = center.z + halfSize.z - ZoneEdgeMargin;
            float localY = zoneCollider.transform
                .InverseTransformPoint(originalPosition).y;
            float nearestSqrDistance = float.PositiveInfinity;
            nearestPosition = default;
            bool found = false;

            for (float localX = minX;
                 localX <= maxX;
                 localX += SearchStep)
            {
                for (float localZ = minZ;
                     localZ <= maxZ;
                     localZ += SearchStep)
                {
                    Vector3 candidatePosition =
                        zoneCollider.transform.TransformPoint(
                            new Vector3(localX, localY, localZ));
                    if (!NavMesh.SamplePosition(
                            candidatePosition,
                            out NavMeshHit hit,
                            CandidateSampleRadius,
                            NavMesh.AllAreas) ||
                        !ContainsHorizontalPoint(zoneCollider, hit.position) ||
                        !IsSeparatedFromReservedPositions(
                            hit.position,
                            reservedPositions))
                    {
                        continue;
                    }

                    float sqrDistance =
                        (hit.position - originalPosition).sqrMagnitude;
                    if (sqrDistance >= nearestSqrDistance)
                    {
                        continue;
                    }

                    nearestSqrDistance = sqrDistance;
                    nearestPosition = hit.position;
                    found = true;
                }
            }

            return found;
        }

        private static bool IsSeparatedFromReservedPositions(
            Vector3 candidatePosition,
            List<Vector3> reservedPositions)
        {
            float minimumSqrDistance =
                MinimumSpawnSeparation * MinimumSpawnSeparation;

            for (int index = 0;
                 index < reservedPositions.Count;
                 index++)
            {
                if ((reservedPositions[index] - candidatePosition)
                        .sqrMagnitude < minimumSqrDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ContainsHorizontalPoint(
            BoxCollider zoneCollider,
            Vector3 worldPosition)
        {
            Vector3 localPosition = zoneCollider.transform
                .InverseTransformPoint(worldPosition) -
                zoneCollider.center;
            Vector3 halfSize = zoneCollider.size * 0.5f;

            return Mathf.Abs(localPosition.x) <=
                       halfSize.x - ZoneEdgeMargin &&
                   Mathf.Abs(localPosition.z) <=
                       halfSize.z - ZoneEdgeMargin;
        }

        private static void ValidateSpawnPoints(
            EnemyZoneController[] zoneControllers)
        {
            for (int zoneIndex = 0;
                 zoneIndex < zoneControllers.Length;
                 zoneIndex++)
            {
                EnemyZoneController zoneController =
                    zoneControllers[zoneIndex];
                SerializedObject serializedController =
                    new SerializedObject(zoneController);
                Transform spawnPointRoot = serializedController
                    .FindProperty("enemySpawnPoints")
                    .objectReferenceValue as Transform;
                float sampleRadius = serializedController
                    .FindProperty("navMeshSampleRadius")
                    .floatValue;

                if (spawnPointRoot == null)
                {
                    continue;
                }

                for (int index = 0;
                     index < spawnPointRoot.childCount;
                     index++)
                {
                    Transform spawnPoint = spawnPointRoot.GetChild(index);
                    if (!NavMesh.SamplePosition(
                            spawnPoint.position,
                            out _,
                            sampleRadius,
                            NavMesh.AllAreas))
                    {
                        throw new InvalidOperationException(
                            $"검증 실패: {zoneController.name}/" +
                            $"{spawnPoint.name} 주변에 NavMesh가 없습니다.");
                    }
                }
            }
        }

    }
}
