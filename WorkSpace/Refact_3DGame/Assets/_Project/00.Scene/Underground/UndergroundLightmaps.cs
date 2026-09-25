using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace Scene
{
    // 기존 씬에서 구운 지하 조명을 보존한다. 구역별 재베이크 후에는 제거할 수 있다.
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class UndergroundLightmaps : MonoBehaviour
    {
        [Serializable]
        private struct BakedTexture
        {
            public Texture2D color;
            public Texture2D direction;
            public Texture2D shadowMask;
        }

        [Serializable]
        private struct RendererLighting
        {
            public Renderer renderer;
            public int textureIndex;
            public Vector4 scaleOffset;
        }

        [SerializeField] private BakedTexture[] textures = Array.Empty<BakedTexture>();
        [SerializeField] private RendererLighting[] renderers = Array.Empty<RendererLighting>();

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
            Apply();
#if UNITY_EDITOR
            // 에디터가 씬의 LightingData를 적용한 다음 좌표를 다시 연결한다.
            UnityEditor.EditorApplication.delayCall += ApplyAfterEditorLoad;
#endif
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall -= ApplyAfterEditorLoad;
#endif
        }

#if UNITY_EDITOR
        private void ApplyAfterEditorLoad()
        {
            if (this != null && isActiveAndEnabled)
            {
                Apply();
            }
        }
#endif

        private void HandleSceneLoaded(UnityScene scene, LoadSceneMode mode) => Apply();
        private void HandleSceneUnloaded(UnityScene scene) => Apply();

        public void Apply()
        {
            if (textures.Length == 0 || renderers.Length == 0)
            {
                return;
            }

            // 씬 경계에서만 실행한다. 기존 전역 슬롯을 재사용해 중복 텍스처를 막는다.
            LightmapData[] current = LightmapSettings.lightmaps;
            var combined = new List<LightmapData>(current);
            var indices = new int[textures.Length];
            for (int index = 0; index < textures.Length; index++)
            {
                BakedTexture texture = textures[index];
                indices[index] = -1;
                if (texture.color == null)
                {
                    continue;
                }

                for (int slot = 0; slot < combined.Count; slot++)
                {
                    LightmapData existing = combined[slot];
                    if (existing.lightmapColor == texture.color &&
                        existing.lightmapDir == texture.direction &&
                        existing.shadowMask == texture.shadowMask)
                    {
                        indices[index] = slot;
                        break;
                    }
                }

                if (indices[index] < 0)
                {
                    indices[index] = combined.Count;
                    combined.Add(new LightmapData
                    {
                        lightmapColor = texture.color,
                        lightmapDir = texture.direction,
                        shadowMask = texture.shadowMask
                    });
                }
            }

            if (combined.Count != current.Length)
            {
                LightmapSettings.lightmaps = combined.ToArray();
            }

            foreach (RendererLighting binding in renderers)
            {
                if (binding.renderer == null || binding.textureIndex < 0 ||
                    binding.textureIndex >= indices.Length || indices[binding.textureIndex] < 0)
                {
                    continue;
                }

                binding.renderer.lightmapIndex = indices[binding.textureIndex];
                binding.renderer.lightmapScaleOffset = binding.scaleOffset;
            }
        }
    }
}
