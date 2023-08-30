//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2024  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
//
//  This library is free software: you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation, either
//  version 3 of the License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CustomAvatar.Logging;
using CustomAvatar.Utilities;
using UnityEngine;
using UnityEngine.Rendering;
using Valve.VR;
using Object = UnityEngine.Object;

namespace CustomAvatar.Tracking.OpenVR
{
    internal class OpenVRRenderModelLoader : IDisposable
    {
        private static readonly uint kRenderModelVertexStructSize = (uint)Marshal.SizeOf(typeof(RenderModel_Vertex_t));
        private static readonly string[] kOffsetComponentNames = new[] { "openxr_grip", "grip" };

        private readonly Dictionary<string, RenderModel> _renderModelCache = new();
        private readonly SemaphoreSlim _renderModelSemaphore = new(1);

        private readonly ILogger<OpenVRRenderModelLoader> _logger;
        private readonly AssetLoader _assetLoader;

        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by Zenject")]
        private OpenVRRenderModelLoader(ILogger<OpenVRRenderModelLoader> logger, AssetLoader assetLoader)
        {
            _logger = logger;
            _assetLoader = assetLoader;
        }

        public void Dispose()
        {
            _renderModelSemaphore.Wait();

            try
            {
                foreach (RenderModel renderModel in _renderModelCache.Values)
                {
                    Object.Destroy(renderModel.material.mainTexture);
                    Object.Destroy(renderModel.material);
                    Object.Destroy(renderModel.mesh);
                }

                _renderModelCache.Clear();
            }
            finally
            {
                _renderModelSemaphore.Release();
            }
        }

        internal async Task<RenderModel> GetRenderModelAsync(string renderModelName)
        {
            await _renderModelSemaphore.WaitAsync();

            try
            {
                if (!_renderModelCache.TryGetValue(renderModelName, out RenderModel renderModel))
                {
                    renderModel = await LoadRenderModelAsync(renderModelName);
                    _renderModelCache[renderModelName] = renderModel;
                }

                return renderModel;
            }
            finally
            {
                _renderModelSemaphore.Release();
            }
        }

        private async Task<RenderModel> LoadRenderModelAsync(string renderModelName)
        {
            _logger.LogInformation($"Loading render model '{renderModelName}'");

            CVRRenderModels renderModels = Valve.VR.OpenVR.RenderModels;

            IntPtr pRenderModel = IntPtr.Zero;
            EVRRenderModelError error;

            while (true)
            {
                error = renderModels.LoadRenderModel_Async(renderModelName, ref pRenderModel);

                if (error != EVRRenderModelError.Loading)
                {
                    break;
                }

                await Task.Delay(100);
            }

            if (error != EVRRenderModelError.None)
            {
                _logger.LogError($"Failed to load render model: {error}");
                return null;
            }

            _logger.LogTrace($"Creating mesh for '{renderModelName}'");

            RenderModel_t renderModel = Marshal.PtrToStructure<RenderModel_t>(pRenderModel);

            var vertices = new Vector3[renderModel.unVertexCount];
            var normals = new Vector3[renderModel.unVertexCount];
            var uv = new Vector2[renderModel.unVertexCount];

            for (int j = 0; j < renderModel.unVertexCount; ++j)
            {
                IntPtr ptr = new(renderModel.rVertexData.ToInt64() + j * kRenderModelVertexStructSize);
                RenderModel_Vertex_t vert = Marshal.PtrToStructure<RenderModel_Vertex_t>(ptr);

                vertices[j] = new Vector3(vert.vPosition.v0, vert.vPosition.v1, -vert.vPosition.v2);
                normals[j] = new Vector3(vert.vNormal.v0, vert.vNormal.v1, -vert.vNormal.v2);
                uv[j] = new Vector2(vert.rfTextureCoord0, vert.rfTextureCoord1);
            }

            uint indexCount = renderModel.unTriangleCount * 3;
            short[] indices = new short[indexCount];
            Marshal.Copy(renderModel.rIndexData, indices, 0, indices.Length);

            int[] triangles = new int[indexCount];
            for (int j = 0; j < renderModel.unTriangleCount; ++j)
            {
                triangles[j * 3] = indices[j * 3 + 2];
                triangles[j * 3 + 1] = indices[j * 3 + 1];
                triangles[j * 3 + 2] = indices[j * 3];
            }

            Mesh mesh = new()
            {
                name = renderModelName,
                vertices = vertices,
                normals = normals,
                uv = uv,
                triangles = triangles,
                hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset,
            };

            _logger.LogTrace($"Loading texture map for '{renderModelName}'");

            IntPtr textureMapPointer = IntPtr.Zero;
            RenderModel_TextureMap_t diffuseTexture;

            try
            {
                while (true)
                {
                    error = renderModels.LoadTexture_Async(renderModel.diffuseTextureId, ref textureMapPointer);

                    if (error != EVRRenderModelError.Loading)
                    {
                        break;
                    }

                    await Task.Delay(100);
                }

                if (error != EVRRenderModelError.None)
                {
                    _logger.LogError($"Failed to load texture map: {error}");
                    return null;
                }

                diffuseTexture = Marshal.PtrToStructure<RenderModel_TextureMap_t>(textureMapPointer);
            }
            finally
            {
                if (textureMapPointer != IntPtr.Zero)
                {
                    renderModels.FreeTexture(textureMapPointer);
                }
            }

            _logger.LogTrace($"Loading texture for '{renderModelName}'");

            Texture2D texture = new(diffuseTexture.unWidth, diffuseTexture.unHeight, TextureFormat.RGBA32, false)
            {
                name = renderModelName,
                hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset,
            };

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11)
            {
                texture.Apply();
                IntPtr texturePointer = texture.GetNativeTexturePtr();

                while (true)
                {
                    error = renderModels.LoadIntoTextureD3D11_Async(renderModel.diffuseTextureId, texturePointer);

                    if (error != EVRRenderModelError.Loading)
                    {
                        if (error != EVRRenderModelError.None)
                        {
                            _logger.LogError($"Failed to load texture: {error}");
                        }

                        break;
                    }

                    await Task.Delay(100);
                }
            }
            else
            {
                byte[] textureMapData = new byte[diffuseTexture.unWidth * diffuseTexture.unHeight * 4]; // RGBA
                Marshal.Copy(diffuseTexture.rubTextureMapData, textureMapData, 0, textureMapData.Length);

                var colors = new Color32[diffuseTexture.unWidth * diffuseTexture.unHeight];
                int iColor = 0;

                for (int iHeight = 0; iHeight < diffuseTexture.unHeight; iHeight++)
                {
                    for (int iWidth = 0; iWidth < diffuseTexture.unWidth; iWidth++)
                    {
                        byte r = textureMapData[iColor++];
                        byte g = textureMapData[iColor++];
                        byte b = textureMapData[iColor++];
                        byte a = textureMapData[iColor++];

                        colors[iHeight * diffuseTexture.unWidth + iWidth] = new Color32(r, g, b, a);
                    }
                }

                texture.SetPixels32(colors);
                texture.Apply();
            }

            await _assetLoader.WaitForAssetsLoadedAsync();

            Material material = new(_assetLoader.unlitShader)
            {
                name = renderModelName,
                hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset,
                color = new Color(1, 1, 1, 0.8f),
                mainTexture = texture,
            };

            _logger.LogInformation($"Successfully loaded render model '{renderModelName}'!");

            Pose offset = GetRenderModelOffset(renderModelName);

            return new RenderModel(mesh, material, offset);
        }

        private Pose GetRenderModelOffset(string renderModelName)
        {
            // grip pose isn't influenced by controller state so we can use a blank state
            VRControllerState_t controllerState = default;
            RenderModel_ControllerMode_State_t controllerModeState = default;
            RenderModel_ComponentState_t componentState = default;
            bool success = false;

            foreach (string name in kOffsetComponentNames)
            {
                if (success = Valve.VR.OpenVR.RenderModels.GetComponentState(renderModelName, name, ref controllerState, ref controllerModeState, ref componentState))
                {
                    break;
                }
            }

            if (!success)
            {
                return Pose.identity;
            }

            HmdMatrix34_t matrix = componentState.mTrackingToComponentLocal;

            // The component pose is the offset between the raw tracking position and the grip position used by OpenXR. Therefore,
            // we need to invert the pose so we get the raw position (render model origin) from the in-game tracked position.
            Vector3 position = -matrix.GetPosition();
            var rotation = Quaternion.Inverse(matrix.GetRotation());
            return new Pose(rotation * position, rotation);
        }
    }
}
