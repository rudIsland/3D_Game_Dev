using System;
using System.IO;
using Characters.Player.Lifecycle;
using GameUI.Minimap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EditorTools
{
    [InitializeOnLoad]
    public static class MinimapSetupBuilder
    {
        private const string RequestPath = "Temp/BuildMinimap.request";
        private const string ScenePath = "Assets/_Project/Scenes/Scene2.unity";
        private const string MinimapFolder = "Assets/_Project/UI/Minimap";
        private const string RenderTexturePath =
            MinimapFolder + "/MinimapViewTexture.renderTexture";
        private const string MarkerMeshPath =
            MinimapFolder + "/MinimapPlayerMarker.asset";
        private const string MarkerMaterialPath =
            MinimapFolder + "/Materials/MinimapPlayerMarker.mat";
        private const int MinimapLayer = 23;

        static MinimapSetupBuilder()
        {
            EditorApplication.delayCall += BuildWhenRequested;
        }

        [MenuItem("Tools/rudIsland/UI/Build Minimap")]
        public static void BuildMinimap()
        {
            RenderTexture renderTexture = GetOrCreateRenderTexture();
            Mesh markerMesh = GetOrCreateMarkerMesh();
            Material markerMaterial = GetOrCreateMarkerMaterial();
            Scene scene = OpenTargetScene();

            GameObject minimapShapes = FindSceneObject(
                scene,
                "MinimapShapes_Generated");
            if (minimapShapes == null)
            {
                throw new InvalidOperationException(
                    "Scene2에서 MinimapShapes_Generated를 찾지 못했습니다.");
            }

            SetLayerRecursively(minimapShapes, MinimapLayer);

            PlayerController playerController =
                Object.FindFirstObjectByType<PlayerController>(
                    FindObjectsInactive.Include);
            if (playerController == null)
            {
                throw new InvalidOperationException(
                    "Scene2에서 PlayerController를 찾지 못했습니다.");
            }

            GameObject minimapSystem = GetOrCreateSceneObject(
                scene,
                "MinimapSystem");
            minimapSystem.layer = MinimapLayer;

            Transform marker = GetOrCreateChild(
                minimapSystem.transform,
                "MinimapPlayerMarker");
            ConfigureMarker(marker, markerMesh, markerMaterial);

            Transform cameraTransform = GetOrCreateChild(
                minimapSystem.transform,
                "MinimapCamera");
            Camera minimapCamera = ConfigureCamera(
                cameraTransform,
                playerController.transform,
                marker,
                renderTexture,
                minimapShapes.transform.position.y + 50f);

            ValidateRenderOutput(minimapCamera, renderTexture);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Scene2 미니맵 카메라, 플레이어 표시와 Render Texture 연결을 완료했습니다.");
        }

        private static void BuildWhenRequested()
        {
            if (!File.Exists(RequestPath))
            {
                return;
            }

            File.Delete(RequestPath);

            try
            {
                BuildMinimap();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static Scene OpenTargetScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.path == ScenePath)
            {
                return activeScene;
            }

            if (activeScene.isDirty)
            {
                throw new InvalidOperationException(
                    "현재 씬에 저장하지 않은 변경이 있습니다. 저장한 뒤 Build Minimap을 다시 실행해 주세요.");
            }

            return EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);
        }

        private static RenderTexture GetOrCreateRenderTexture()
        {
            RenderTexture renderTexture =
                AssetDatabase.LoadAssetAtPath<RenderTexture>(
                    RenderTexturePath);
            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(512, 512, 16)
                {
                    name = "MinimapViewTexture"
                };
                AssetDatabase.CreateAsset(renderTexture, RenderTexturePath);
            }

            renderTexture.Release();
            renderTexture.width = 512;
            renderTexture.height = 512;
            renderTexture.depth = 16;
            renderTexture.antiAliasing = 1;
            renderTexture.useMipMap = false;
            renderTexture.autoGenerateMips = false;
            renderTexture.filterMode = FilterMode.Bilinear;
            renderTexture.wrapMode = TextureWrapMode.Clamp;
            EditorUtility.SetDirty(renderTexture);
            return renderTexture;
        }

        private static Mesh GetOrCreateMarkerMesh()
        {
            Mesh markerMesh =
                AssetDatabase.LoadAssetAtPath<Mesh>(MarkerMeshPath);
            if (markerMesh == null)
            {
                markerMesh = new Mesh
                {
                    name = "MinimapPlayerMarker"
                };
                AssetDatabase.CreateAsset(markerMesh, MarkerMeshPath);
            }

            markerMesh.Clear();
            markerMesh.vertices = new[]
            {
                new Vector3(-0.7f, 0f, -0.65f),
                new Vector3(0f, 0f, 1f),
                new Vector3(0.7f, 0f, -0.65f)
            };
            markerMesh.normals = new[]
            {
                Vector3.up,
                Vector3.up,
                Vector3.up
            };
            markerMesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0.5f, 1f),
                new Vector2(1f, 0f)
            };
            markerMesh.triangles = new[] { 0, 1, 2 };
            markerMesh.RecalculateBounds();
            EditorUtility.SetDirty(markerMesh);
            return markerMesh;
        }

        private static Material GetOrCreateMarkerMaterial()
        {
            Material markerMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(
                    MarkerMaterialPath);
            if (markerMaterial == null)
            {
                Shader shader = Shader.Find(
                    "Universal Render Pipeline/Unlit");
                if (shader == null)
                {
                    throw new InvalidOperationException(
                        "URP Unlit Shader를 찾지 못했습니다.");
                }

                markerMaterial = new Material(shader)
                {
                    name = "MinimapPlayerMarker"
                };
                AssetDatabase.CreateAsset(
                    markerMaterial,
                    MarkerMaterialPath);
            }

            markerMaterial.color = new Color(0.95f, 0.22f, 0.12f, 1f);
            if (markerMaterial.HasProperty("_BaseColor"))
            {
                markerMaterial.SetColor(
                    "_BaseColor",
                    new Color(0.95f, 0.22f, 0.12f, 1f));
            }
            EditorUtility.SetDirty(markerMaterial);
            return markerMaterial;
        }

        private static void ConfigureMarker(
            Transform marker,
            Mesh markerMesh,
            Material markerMaterial)
        {
            marker.gameObject.layer = MinimapLayer;
            marker.localScale = Vector3.one;

            MeshFilter meshFilter =
                GetOrAddComponent<MeshFilter>(marker.gameObject);
            meshFilter.sharedMesh = markerMesh;

            MeshRenderer meshRenderer =
                GetOrAddComponent<MeshRenderer>(marker.gameObject);
            meshRenderer.sharedMaterial = markerMaterial;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static Camera ConfigureCamera(
            Transform cameraTransform,
            Transform player,
            Transform marker,
            RenderTexture renderTexture,
            float mapSurfaceHeight)
        {
            cameraTransform.gameObject.layer = MinimapLayer;
            cameraTransform.SetPositionAndRotation(
                new Vector3(
                    player.position.x,
                    mapSurfaceHeight + 30f,
                    player.position.z),
                Quaternion.Euler(90f, 0f, 0f));

            Camera minimapCamera =
                GetOrAddComponent<Camera>(cameraTransform.gameObject);
            minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor =
                new Color(0.055f, 0.065f, 0.06f, 1f);
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = 22f;
            minimapCamera.nearClipPlane = 0.1f;
            minimapCamera.farClipPlane = 100f;
            minimapCamera.cullingMask = 1 << MinimapLayer;
            minimapCamera.targetTexture = renderTexture;
            minimapCamera.allowHDR = false;
            minimapCamera.allowMSAA = false;
            minimapCamera.useOcclusionCulling = false;

            MinimapCameraController controller =
                GetOrAddComponent<MinimapCameraController>(
                    cameraTransform.gameObject);
            controller.ConnectForEditor(
                player,
                marker,
                mapSurfaceHeight);
            EditorUtility.SetDirty(controller);
            return minimapCamera;
        }

        private static void ValidateRenderOutput(
            Camera minimapCamera,
            RenderTexture renderTexture)
        {
            RenderTexture previousRenderTexture = RenderTexture.active;
            Texture2D capturedTexture = null;

            try
            {
                minimapCamera.Render();
                RenderTexture.active = renderTexture;
                capturedTexture = new Texture2D(
                    renderTexture.width,
                    renderTexture.height,
                    TextureFormat.RGB24,
                    false);
                capturedTexture.ReadPixels(
                    new Rect(
                        0f,
                        0f,
                        renderTexture.width,
                        renderTexture.height),
                    0,
                    0);
                capturedTexture.Apply(false, false);

                Color32[] pixels = capturedTexture.GetPixels32();
                Color32 background = pixels[0];
                int visiblePixelCount = 0;

                for (int index = 0; index < pixels.Length; index++)
                {
                    Color32 pixel = pixels[index];
                    int colorDifference =
                        Math.Abs(pixel.r - background.r) +
                        Math.Abs(pixel.g - background.g) +
                        Math.Abs(pixel.b - background.b);
                    if (colorDifference > 24)
                    {
                        visiblePixelCount++;
                    }
                }

                if (visiblePixelCount < 100)
                {
                    throw new InvalidOperationException(
                        "미니맵 Render Texture에서 지도 색상을 확인하지 못했습니다.");
                }

                Debug.Log(
                    $"미니맵 Render Texture 검증 완료: 지도 픽셀 {visiblePixelCount:N0}개");
            }
            finally
            {
                RenderTexture.active = previousRenderTexture;
                if (capturedTexture != null)
                {
                    Object.DestroyImmediate(capturedTexture);
                }
            }
        }

        private static T GetOrAddComponent<T>(GameObject target)
            where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null
                ? component
                : target.AddComponent<T>();
        }

        private static GameObject GetOrCreateSceneObject(
            Scene scene,
            string objectName)
        {
            GameObject sceneObject = FindSceneObject(scene, objectName);
            if (sceneObject != null)
            {
                return sceneObject;
            }

            sceneObject = new GameObject(objectName);
            SceneManager.MoveGameObjectToScene(sceneObject, scene);
            return sceneObject;
        }

        private static Transform GetOrCreateChild(
            Transform parent,
            string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                return child;
            }

            GameObject childObject = new GameObject(childName);
            childObject.transform.SetParent(parent, false);
            return childObject.transform;
        }

        private static GameObject FindSceneObject(
            Scene scene,
            string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                Transform found = FindChildRecursively(
                    roots[index].transform,
                    objectName);
                if (found != null)
                {
                    return found.gameObject;
                }
            }

            return null;
        }

        private static Transform FindChildRecursively(
            Transform current,
            string objectName)
        {
            if (current.name == objectName)
            {
                return current;
            }

            for (int index = 0; index < current.childCount; index++)
            {
                Transform found = FindChildRecursively(
                    current.GetChild(index),
                    objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void SetLayerRecursively(
            GameObject target,
            int layer)
        {
            target.layer = layer;

            Transform targetTransform = target.transform;
            for (int index = 0; index < targetTransform.childCount; index++)
            {
                SetLayerRecursively(
                    targetTransform.GetChild(index).gameObject,
                    layer);
            }
        }
    }
}
