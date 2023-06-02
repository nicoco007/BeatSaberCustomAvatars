//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2023  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Rendering
{
    // this class is based on the code available at http://wiki.unity3d.com/index.php/MirrorReflection4 and adapted for stereo rendering
    [RequireComponent(typeof(Renderer))]
    internal class StereoMirrorRenderer : MonoBehaviour
    {
        private const int kUserInterfaceLayer = 5;
        private const int kNonReflectedParticlesLayer = 19;

        private static readonly Type[] kCameraComponentsToKeep = { typeof(Camera), typeof(BloomPrePass) };
        private static readonly int kTexturePropertyId = Shader.PropertyToID("_ReflectionTex");
        private static readonly int[] kValidAntiAliasingValues = { 1, 2, 4, 8 };

        private static readonly Rect kLeftRect = new Rect(0f, 0f, 0.5f, 1f);
        private static readonly Rect kRightRect = new Rect(0.5f, 0f, 0.5f, 1f);
        private static readonly Rect kFullRect = new Rect(0f, 0f, 1f, 1f);

        private ShaderLoader _shaderLoader;
        private ActiveCameraManager _activeCameraManager;
        private Settings _settings;

        private Renderer _renderer;
        private Camera _mirrorCamera;
        private int _antiAliasing = 2;
        private readonly Dictionary<Camera, RenderTexture> _renderTextures = new Dictionary<Camera, RenderTexture>();

        public float renderScale { get; set; } = 1;

        public int antiAliasing
        {
            get => _antiAliasing;
            set
            {
                if (!kValidAntiAliasingValues.Contains(value))
                {
                    throw new ArgumentException("Antialiasing must be one of 1, 2, 4, or 8");
                }

                _antiAliasing = value;
            }
        }


        #region Behaviour Lifecycle
#pragma warning disable IDE0051

        [Inject]
        private void Inject(ShaderLoader shaderLoader, ActiveCameraManager activeCameraManager, Settings settings)
        {
            _shaderLoader = shaderLoader;
            _activeCameraManager = activeCameraManager;
            _settings = settings;
        }

        private void Start()
        {
            _renderer = GetComponent<Renderer>();
            _renderer.material = new Material(_shaderLoader.stereoMirrorShader);

            CreateMirrorCamera();
        }

        private void Update()
        {
            PrepareForNextFrame();
        }

        private void OnWillRenderObject()
        {
            Vector3 position = transform.position;
            Vector3 up = transform.up;

            Texture mirrorTexture = GetMirrorTexture(position, up);

            _renderer.material.SetTexture(kTexturePropertyId, mirrorTexture);
        }

#pragma warning restore IDE0051
        #endregion

        private void PrepareForNextFrame()
        {
            foreach (RenderTexture texture in _renderTextures.Values)
            {
                RenderTexture.ReleaseTemporary(texture);
            }

            _renderTextures.Clear();
        }

        private Texture GetMirrorTexture(Vector3 reflectionPlanePosition, Vector3 reflectionPlaneNormal)
        {
            Camera camera = Camera.current;

            if (!camera || camera == _mirrorCamera || renderScale <= 0)
            {
                return Texture2D.blackTexture;
            }

            if (!_settings.mirror.renderInExternalCameras && camera != _activeCameraManager.current)
            {
                return Texture2D.blackTexture;
            }

            // return immediately if we've already rendered for this frame
            if (_renderTextures.TryGetValue(camera, out RenderTexture renderTexture))
            {
                return renderTexture;
            }

            Transform cameraTransform = camera.transform;
            Vector3 cameraPosition = cameraTransform.position;
            Quaternion cameraRotation = cameraTransform.rotation;
            var plane = new Plane(reflectionPlaneNormal, reflectionPlanePosition);

            // don't render if the camera is too close to the mirror to prevent errors
            if (plane.GetDistanceToPoint(cameraPosition) <= Mathf.Epsilon || (camera.orthographic && Mathf.Abs(Vector3.Dot(camera.transform.forward, reflectionPlaneNormal)) <= Mathf.Epsilon))
            {
                return Texture2D.blackTexture;
            }

            bool stereoEnabled = camera.stereoEnabled;
            int renderWidth = Mathf.RoundToInt(camera.pixelWidth * renderScale);
            int renderHeight = Mathf.RoundToInt(camera.pixelHeight * renderScale);

            renderTexture = RenderTexture.GetTemporary(Mathf.Min(stereoEnabled ? renderWidth * 2 : renderWidth, SystemInfo.maxTextureSize), Mathf.Min(renderHeight, SystemInfo.maxTextureSize), 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, _antiAliasing);

            _renderTextures[camera] = renderTexture;

            UpdateMirrorCamera(camera, renderTexture);

            bool invertCulling = GL.invertCulling;
            GL.invertCulling = !invertCulling;

            if (stereoEnabled)
            {
                Quaternion targetRotation = cameraRotation;

                if (camera.stereoTargetEye == StereoTargetEyeMask.Both || camera.stereoTargetEye == StereoTargetEyeMask.Left)
                {
                    Vector3 targetPosition = camera.ViewportToWorldPoint(Vector3.zero, Camera.MonoOrStereoscopicEye.Left);
                    Matrix4x4 stereoProjectionMatrix = camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);

                    RenderMirror(targetPosition, targetRotation, stereoProjectionMatrix, kLeftRect, reflectionPlanePosition, reflectionPlaneNormal);
                }

                if (camera.stereoTargetEye == StereoTargetEyeMask.Both || camera.stereoTargetEye == StereoTargetEyeMask.Right)
                {
                    Vector3 targetPosition = camera.ViewportToWorldPoint(Vector3.zero, Camera.MonoOrStereoscopicEye.Right);
                    Matrix4x4 stereoProjectionMatrix = camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);

                    RenderMirror(targetPosition, targetRotation, stereoProjectionMatrix, kRightRect, reflectionPlanePosition, reflectionPlaneNormal);
                }
            }
            else
            {
                RenderMirror(cameraPosition, cameraRotation, camera.projectionMatrix, kFullRect, reflectionPlanePosition, reflectionPlaneNormal);
            }

            GL.invertCulling = invertCulling;
            GL.Flush();

            return renderTexture;
        }

        private void RenderMirror(Vector3 cameraPosition, Quaternion cameraRotation, Matrix4x4 projectionMatrix, Rect screenRect, Vector3 reflectionPlanePosition, Vector3 reflectionPlaneNormal)
        {
            _mirrorCamera.rect = screenRect;
            _mirrorCamera.projectionMatrix = projectionMatrix;

            Matrix4x4 reflectionMatrix = CalculateReflectionMatrix(Plane(reflectionPlanePosition, reflectionPlaneNormal));

            _mirrorCamera.ResetWorldToCameraMatrix();
            _mirrorCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
            _mirrorCamera.worldToCameraMatrix *= reflectionMatrix;

            Vector4 clipPlane = CameraSpacePlane(_mirrorCamera.worldToCameraMatrix, reflectionPlanePosition, reflectionPlaneNormal);

            _mirrorCamera.projectionMatrix = _mirrorCamera.CalculateObliqueMatrix(clipPlane);
            _mirrorCamera.Render();
        }

        private void CreateMirrorCamera()
        {
            GameObject cameraGameObject = Instantiate(Camera.main.gameObject, transform);
            cameraGameObject.name = "MirrorCamera";

            foreach (Transform transform in cameraGameObject.transform)
            {
                Destroy(transform.gameObject);
            }

            foreach (Behaviour behaviour in cameraGameObject.GetComponents<Behaviour>())
            {
                if (!kCameraComponentsToKeep.Contains(behaviour.GetType()))
                {
                    Destroy(behaviour);
                }
            }

            _mirrorCamera = cameraGameObject.GetComponent<Camera>();
            _mirrorCamera.hideFlags = HideFlags.HideAndDontSave;
            _mirrorCamera.enabled = false;
        }

        private void UpdateMirrorCamera(Camera currentCamera, RenderTexture renderTexture)
        {
            _mirrorCamera.CopyFrom(currentCamera);
            _mirrorCamera.targetTexture = renderTexture;
            _mirrorCamera.depthTextureMode = DepthTextureMode.None;
            _mirrorCamera.clearFlags = CameraClearFlags.Color;
            _mirrorCamera.cullingMask = (_mirrorCamera.cullingMask | AvatarLayers.kAllLayersMask) & ~(1 << kUserInterfaceLayer) & ~(1 << kNonReflectedParticlesLayer);
        }

        private Vector4 Plane(Vector3 pos, Vector3 normal)
        {
            return new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(pos, normal));
        }

        private Vector4 CameraSpacePlane(Matrix4x4 worldToCameraMatrix, Vector3 pos, Vector3 normal)
        {
            Vector3 pos2 = worldToCameraMatrix.MultiplyPoint(pos);
            Vector3 normalized = worldToCameraMatrix.MultiplyVector(normal).normalized;

            return Plane(pos2, normalized);
        }

        private Matrix4x4 CalculateReflectionMatrix(Vector4 plane)
        {
            Matrix4x4 identity = Matrix4x4.identity;

            identity.m00 = 1f - 2f * plane.x * plane.x;
            identity.m01 = -2f * plane.x * plane.y;
            identity.m02 = -2f * plane.x * plane.z;
            identity.m03 = -2f * plane.x * plane.w;

            identity.m10 = -2f * plane.y * plane.x;
            identity.m11 = 1f - 2f * plane.y * plane.y;
            identity.m12 = -2f * plane.y * plane.z;
            identity.m13 = -2f * plane.y * plane.w;

            identity.m20 = -2f * plane.z * plane.x;
            identity.m21 = -2f * plane.z * plane.y;
            identity.m22 = 1f - 2f * plane.z * plane.z;
            identity.m23 = -2f * plane.z * plane.w;

            identity.m30 = 0f;
            identity.m31 = 0f;
            identity.m32 = 0f;
            identity.m33 = 1f;

            return identity;
        }
    }
}
