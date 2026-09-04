using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Characters.Player.Combat.Attack
{
    // 검의 시작점과 끝점을 기록해 실제로 지나간 자리에 잔상 면을 만든다.
    [DisallowMultipleComponent]
    internal sealed class PlayerBladeTrailRenderer : MonoBehaviour
    {
        private const int MaxSampleCount = 24;
        private const float TrailKeepTime = 0.14f;
        private const float MinimumSampleDistance = 0.015f;
        private const float TrailBladeLengthRatio = 0.12f;

        private static readonly Color BladeStartColor = new Color(0.18f, 0.55f, 1f, 0.08f);
        private static readonly Color BladeEndColor = new Color(0.65f, 0.92f, 1f, 0.50f);

        private readonly Vector3[] bladeStartPositions = new Vector3[MaxSampleCount];
        private readonly Vector3[] bladeEndPositions = new Vector3[MaxSampleCount];
        private readonly float[] sampleTimes = new float[MaxSampleCount];
        private readonly Vector3[] meshVertices = new Vector3[MaxSampleCount * 2];
        private readonly Color[] meshColors = new Color[MaxSampleCount * 2];
        private readonly Vector2[] meshUvs = new Vector2[MaxSampleCount * 2];
        private readonly int[] meshTriangles = new int[(MaxSampleCount - 1) * 12];

        private Transform weaponHitStart;
        private Transform weaponHitEnd;
        private GameObject trailObject;
        private Mesh trailMesh;
        private MeshRenderer trailMeshRenderer;
        private Material trailMaterial;
        private int sampleCount;
        private bool isCreated;
        private bool isEmitting;

        internal void Create(Transform hitStart, Transform hitEnd)
        {
            if (isCreated)
            {
                return;
            }

            isCreated = true;
            weaponHitStart = hitStart;
            weaponHitEnd = hitEnd;
            CreateTrailObject();
            CreateTriangleIndices();
            ClearTrail();
        }

        internal void BeginTrail()
        {
            if (!IsReady())
            {
                return;
            }

            ClearTrail();
            isEmitting = true;
            AddCurrentSample(Time.time);
        }

        internal void EndTrail()
        {
            isEmitting = false;
        }

        internal void ClearTrail()
        {
            isEmitting = false;
            sampleCount = 0;
            if (trailMesh != null)
            {
                trailMesh.Clear(false);
            }

            if (trailMeshRenderer != null)
            {
                trailMeshRenderer.enabled = false;
            }
        }

        private void LateUpdate()
        {
            if (!IsReady())
            {
                return;
            }

            float currentTime = Time.time;
            RemoveOldSamples(currentTime);
            if (isEmitting)
            {
                AddCurrentSample(currentTime);
            }

            UpdateTrailMesh(currentTime);
        }

        private void OnDisable()
        {
            ClearTrail();
        }

        private void OnDestroy()
        {
            if (trailObject != null)
            {
                Destroy(trailObject);
            }

            if (trailMaterial != null)
            {
                Destroy(trailMaterial);
            }

            if (trailMesh != null)
            {
                Destroy(trailMesh);
            }
        }

        private bool IsReady()
        {
            return isCreated &&
                weaponHitStart != null &&
                weaponHitEnd != null &&
                trailMesh != null &&
                trailMeshRenderer != null;
        }

        private void CreateTrailObject()
        {
            trailObject = new GameObject($"{name} Blade Trail");
            SceneManager.MoveGameObjectToScene(trailObject, gameObject.scene);

            MeshFilter meshFilter = trailObject.AddComponent<MeshFilter>();
            trailMeshRenderer = trailObject.AddComponent<MeshRenderer>();
            trailMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            trailMeshRenderer.receiveShadows = false;
            trailMeshRenderer.lightProbeUsage = LightProbeUsage.Off;
            trailMeshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            trailMesh = new Mesh
            {
                name = $"{name} Blade Trail Mesh"
            };
            trailMesh.MarkDynamic();
            meshFilter.sharedMesh = trailMesh;

            Shader trailShader = Shader.Find("Sprites/Default");
            if (trailShader == null)
            {
                Debug.LogError("검 궤적에 필요한 Sprites/Default Shader를 찾지 못했습니다.", this);
                return;
            }

            trailMaterial = new Material(trailShader)
            {
                name = $"{name} Blade Trail Material",
                color = Color.white
            };
            trailMeshRenderer.sharedMaterial = trailMaterial;
        }

        private void CreateTriangleIndices()
        {
            for (int segmentIndex = 0;
                 segmentIndex < MaxSampleCount - 1;
                 segmentIndex++)
            {
                int vertexIndex = segmentIndex * 2;
                int triangleIndex = segmentIndex * 12;

                meshTriangles[triangleIndex] = vertexIndex;
                meshTriangles[triangleIndex + 1] = vertexIndex + 2;
                meshTriangles[triangleIndex + 2] = vertexIndex + 1;
                meshTriangles[triangleIndex + 3] = vertexIndex + 2;
                meshTriangles[triangleIndex + 4] = vertexIndex + 3;
                meshTriangles[triangleIndex + 5] = vertexIndex + 1;

                meshTriangles[triangleIndex + 6] = vertexIndex + 1;
                meshTriangles[triangleIndex + 7] = vertexIndex + 2;
                meshTriangles[triangleIndex + 8] = vertexIndex;
                meshTriangles[triangleIndex + 9] = vertexIndex + 1;
                meshTriangles[triangleIndex + 10] = vertexIndex + 3;
                meshTriangles[triangleIndex + 11] = vertexIndex + 2;
            }
        }

        private void AddCurrentSample(float currentTime)
        {
            Vector3 bladeStart = weaponHitStart.position;
            Vector3 currentEnd = weaponHitEnd.position;
            Vector3 currentStart = Vector3.Lerp(
                currentEnd,
                bladeStart,
                TrailBladeLengthRatio);

            if (sampleCount > 0)
            {
                int lastIndex = sampleCount - 1;
                bool startMoved = Vector3.SqrMagnitude(currentStart - bladeStartPositions[lastIndex]) >=
                    MinimumSampleDistance * MinimumSampleDistance;
                bool endMoved = Vector3.SqrMagnitude(currentEnd - bladeEndPositions[lastIndex]) >=
                    MinimumSampleDistance * MinimumSampleDistance;
                if (!startMoved && !endMoved)
                {
                    bladeStartPositions[lastIndex] = currentStart;
                    bladeEndPositions[lastIndex] = currentEnd;
                    sampleTimes[lastIndex] = currentTime;
                    return;
                }
            }

            if (sampleCount >= MaxSampleCount)
            {
                ShiftSamplesLeft(1);
                sampleCount--;
            }

            bladeStartPositions[sampleCount] = currentStart;
            bladeEndPositions[sampleCount] = currentEnd;
            sampleTimes[sampleCount] = currentTime;
            sampleCount++;
        }

        private void RemoveOldSamples(float currentTime)
        {
            int removeCount = 0;
            while (removeCount < sampleCount &&
                   currentTime - sampleTimes[removeCount] >= TrailKeepTime)
            {
                removeCount++;
            }

            if (removeCount == 0)
            {
                return;
            }

            ShiftSamplesLeft(removeCount);
            sampleCount -= removeCount;
        }

        private void ShiftSamplesLeft(int moveCount)
        {
            int remainingCount = sampleCount - moveCount;
            for (int index = 0; index < remainingCount; index++)
            {
                int sourceIndex = index + moveCount;
                bladeStartPositions[index] = bladeStartPositions[sourceIndex];
                bladeEndPositions[index] = bladeEndPositions[sourceIndex];
                sampleTimes[index] = sampleTimes[sourceIndex];
            }
        }

        private void UpdateTrailMesh(float currentTime)
        {
            if (sampleCount < 2)
            {
                trailMesh.Clear(false);
                trailMeshRenderer.enabled = false;
                return;
            }

            float uvStep = 1f / (sampleCount - 1);
            for (int sampleIndex = 0;
                 sampleIndex < sampleCount;
                 sampleIndex++)
            {
                int vertexIndex = sampleIndex * 2;
                float ageRatio = Mathf.Clamp01((currentTime - sampleTimes[sampleIndex]) / TrailKeepTime);
                float alpha = 1f - ageRatio;

                meshVertices[vertexIndex] = bladeStartPositions[sampleIndex];
                meshVertices[vertexIndex + 1] = bladeEndPositions[sampleIndex];
                meshColors[vertexIndex] = new Color(
                    BladeStartColor.r,
                    BladeStartColor.g,
                    BladeStartColor.b,
                    BladeStartColor.a * alpha);
                meshColors[vertexIndex + 1] = new Color(
                    BladeEndColor.r,
                    BladeEndColor.g,
                    BladeEndColor.b,
                    BladeEndColor.a * alpha);

                float uvY = sampleIndex * uvStep;
                meshUvs[vertexIndex] = new Vector2(0f, uvY);
                meshUvs[vertexIndex + 1] = new Vector2(1f, uvY);
            }

            int vertexCount = sampleCount * 2;
            int indexCount = (sampleCount - 1) * 12;
            trailMesh.Clear(false);
            trailMesh.SetVertices(meshVertices, 0, vertexCount);
            trailMesh.SetColors(meshColors, 0, vertexCount);
            trailMesh.SetUVs(0, meshUvs, 0, vertexCount);
            trailMesh.SetIndices(
                meshTriangles,
                0,
                indexCount,
                MeshTopology.Triangles,
                0,
                true,
                0);
            trailMeshRenderer.enabled = true;
        }
    }
}