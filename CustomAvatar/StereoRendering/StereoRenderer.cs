//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using UnityEngine;
using System;
using System.Collections.Generic;

namespace CustomAvatar.StereoRendering
{
	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Renderer))]
	public class StereoRenderer : MonoBehaviour
	{
		#region variables

		//--------------------------------------------------------------------------------
		// getting/setting stereo anchor pose

		public Transform canvasOrigin;
		[SerializeField]
		private Vector3 m_canvasOriginWorldPosition = new Vector3(0.0f, 0.0f, 0.0f);
		[SerializeField]
		private Vector3 m_canvasOriginWorldRotation = new Vector3(0.0f, 0.0f, 0.0f);

		public Vector3 canvasOriginPos
		{
			get
			{
				if (canvasOrigin == null) { return m_canvasOriginWorldPosition; }
				return canvasOrigin.position;
			}
			set
			{
				m_canvasOriginWorldPosition = value;
			}
		}

		public Vector3 canvasOriginEuler
		{
			get
			{
				if (canvasOrigin == null) { return m_canvasOriginWorldRotation; }
				return canvasOrigin.eulerAngles;
			}
			set
			{
				m_canvasOriginWorldRotation = value;
			}
		}

		public Quaternion canvasOriginRot
		{
			get { return Quaternion.Euler(canvasOriginEuler); }
			set { canvasOriginEuler = value.eulerAngles; }
		}

		public Vector3 canvasOriginForward
		{
			get { return canvasOriginRot * Vector3.forward; }
		}

		public Vector3 canvasOriginUp
		{
			get { return canvasOriginRot * Vector3.up; }
		}

		public Vector3 canvasOriginRight
		{
			get { return canvasOriginRot * Vector3.right; }
		}

		public Vector3 localCanvasOriginPos
		{
			get { return transform.InverseTransformPoint(canvasOriginPos); }
			set { canvasOriginPos = transform.InverseTransformPoint(value); }
		}

		public Vector3 localCanvasOriginEuler
		{
			get { return (Quaternion.Inverse(transform.rotation) * Quaternion.Euler(canvasOriginEuler)).eulerAngles; }
			set { canvasOriginEuler = (transform.rotation * Quaternion.Euler(value)).eulerAngles; }
		}

		public Quaternion localCanvasOriginRot
		{
			get { return Quaternion.Inverse(transform.rotation) * canvasOriginRot; }
			set { canvasOriginRot = transform.rotation * value; }
		}

		//--------------------------------------------------------------------------------
		// getting/setting stereo anchor pose

		public Transform anchorTransform;
		[SerializeField]
		private Vector3 m_anchorWorldPosition = new Vector3(0.0f, 0.0f, 0.0f);
		[SerializeField]
		private Vector3 m_anchorWorldRotation = new Vector3(0.0f, 0.0f, 0.0f);

		public Vector3 anchorPos
		{
			get
			{
				if (anchorTransform == null) { return m_anchorWorldPosition; }
				return anchorTransform.position;
			}
			set
			{
				m_anchorWorldPosition = value;
			}
		}

		public Vector3 anchorEuler
		{
			get
			{
				if (anchorTransform == null) { return m_anchorWorldRotation; }
				return anchorTransform.eulerAngles;
			}
			set
			{
				m_anchorWorldRotation = value;
			}
		}

		public Quaternion anchorRot
		{
			get { return Quaternion.Euler(anchorEuler); }
			set { anchorEuler = value.eulerAngles; }
		}

		public Vector3 anchorForward
		{
			get { return anchorRot * Vector3.forward; }
		}

		public Vector3 anchorUp
		{
			get { return anchorRot * Vector3.up; }
		}

		//--------------------------------------------------------------------------------
		// stereo matrices

		private Matrix4x4 leftProjMatrix;
		private Matrix4x4 rightProjMatrix;

		//--------------------------------------------------------------------------------
		// other variables

		// flags
		public bool shouldRender = true;
		public bool useObliqueClip = true;
		public bool useScissor = true;

		// camera rig for stereo rendering, which is on the object this component attached to
		public GameObject stereoCameraHead = null;
		public Camera stereoCameraEye = null;

		// render texture for stereo rendering
		private RenderTexture leftEyeTexture = null;
		private RenderTexture rightEyeTexture = null;

		public float textureResolutionScale = 1.0f;

		// the materials for displaying render result
		private Material stereoMaterial;

		// list of objects that should be ignored when rendering
		[SerializeField]
		private List<GameObject> ignoreWhenRender = new List<GameObject>();
		private List<int> ignoreObjOriginalLayer = new List<int>();

		// other params
		public float reflectionOffset = 0.05f;
		private Rect fullViewport = new Rect(0, 0, 1, 1);

		// for mirror rendering
		public bool isMirror = false;
		private Matrix4x4 reflectionMat;

		// for callbacks
		private Action preRenderListeners;
		private Action postRenderListeners;

		#endregion

		/////////////////////////////////////////////////////////////////////////////////
		// initialization

		private void Start()
		{
			// initialize parameter factory
			StereoRenderManager.Instance.InitParamFactory();

			// initialize stereo camera rig
			if (stereoCameraHead == null)
				CreateStereoCameraRig();

			// get and store projection matrices
			leftProjMatrix = StereoRenderManager.Instance.paramFactory.GetProjectionMatrix(
				0, stereoCameraEye.nearClipPlane, stereoCameraEye.farClipPlane);
			rightProjMatrix = StereoRenderManager.Instance.paramFactory.GetProjectionMatrix(
				1, stereoCameraEye.nearClipPlane, stereoCameraEye.farClipPlane);

			// use first material from renderer as stereo material
			Renderer renderer = GetComponent<Renderer>();
			stereoMaterial = renderer.materials[0];
			stereoMaterial.SetFloat("_Cutout", 0.01f);

			// create render textures as target of stereo rendering
			CreateRenderTextures(
				StereoRenderManager.Instance.paramFactory.GetRenderWidth(),
				StereoRenderManager.Instance.paramFactory.GetRenderHeight());

			// get main camera and registor to StereoRenderManager
			StereoRenderManager.Instance.AddToManager(this);
		}

		private void OnDestroy()
		{
			StereoRenderManager.Instance.RemoveFromManager(this);
		}

		private void CreateStereoCameraRig()
		{
			stereoCameraHead = new GameObject("Stereo Camera Head [" + gameObject.name + "]");
			stereoCameraHead.transform.parent = transform;

			GameObject stereoCameraEyeObj = new GameObject("Stereo Camera Eye [" + gameObject.name + "]");
			stereoCameraEyeObj.transform.parent = stereoCameraHead.transform;
			stereoCameraEye = stereoCameraEyeObj.AddComponent<Camera>();
			stereoCameraEye.enabled = false;
		}

		private void CreateRenderTextures(int sceneWidth, int sceneHeight, int aaLevel = 4)
		{
			int depth = 32;
			int w = (int)(textureResolutionScale * sceneWidth);
			int h = (int)(textureResolutionScale * sceneHeight);

			leftEyeTexture = new RenderTexture(w, h, depth, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
			leftEyeTexture.antiAliasing = aaLevel;

			rightEyeTexture = new RenderTexture(w, h, depth, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
			rightEyeTexture.antiAliasing = aaLevel;
		}

		/////////////////////////////////////////////////////////////////////////////////
		// support moving mirrors

		private void Update()
		{
			if (isMirror)
			{
				anchorPos = canvasOriginPos;
				anchorRot = canvasOriginRot;
			}
		}

		/////////////////////////////////////////////////////////////////////////////////
		// visibility and rendering

		public void Render(VRRenderEventDetector detector)
		{
			Camera camera = detector.unityCamera;

			// move stereo camera around based on HMD pose
			MoveStereoCameraBasedOnHmdPose(camera);

			// invoke pre-render events
			if (preRenderListeners != null)
				preRenderListeners.Invoke();

			// change layer of specified objects,
			// so that they become invisible to currect camera
			ignoreObjOriginalLayer.Clear();
			for (int i = 0; i < ignoreWhenRender.Count; i++)
			{
				ignoreObjOriginalLayer.Add(ignoreWhenRender[i].layer);
			}

			// invert backface culling when rendering a mirror
			if (isMirror)
				GL.invertCulling = true;

			// render the canvas
			RenderToTwoStereoTextures(camera);

			// reset backface culling
			if (isMirror)
				GL.invertCulling = false;

			// resume object layers
			for (int i = 0; i < ignoreWhenRender.Count; i++)
				ignoreWhenRender[i].layer = ignoreObjOriginalLayer[i];

			// invoke post-render events
			if (postRenderListeners != null)
				postRenderListeners.Invoke();
		}

		public void MoveStereoCameraBasedOnHmdPose(Camera camera)
		{
			Vector3 mainCamPos = camera.transform.position;
			Quaternion mainCamRot = camera.transform.rotation;

			if (isMirror)
			{
				// get reflection plane -- assume +y as normal
				float d = -Vector3.Dot(canvasOriginUp, canvasOriginPos);
				Vector4 reflectionPlane = new Vector4(canvasOriginUp.x, canvasOriginUp.y, canvasOriginUp.z, d);

				// get reflection matrix
				reflectionMat = Matrix4x4.zero;
				CalculateReflectionMatrix(ref reflectionMat, reflectionPlane);

				// set head position
				Vector3 reflectedPos = reflectionMat.MultiplyPoint(mainCamPos);
				stereoCameraHead.transform.position = reflectedPos;

				// set head orientation
				stereoCameraHead.transform.rotation = mainCamRot;
			}
			else
			{
				Vector3 posCanvasToMainCam = mainCamPos - canvasOriginPos;

				// compute the rotation between the portal entry and the portal exit
				Quaternion rotCanvasToAnchor = anchorRot * Quaternion.Inverse(canvasOriginRot);

				// move remote camera position
				Vector3 posAnchorToStereoCam = rotCanvasToAnchor * posCanvasToMainCam;
				stereoCameraHead.transform.position = anchorPos + posAnchorToStereoCam;

				// rotate remote camera
				stereoCameraHead.transform.rotation = rotCanvasToAnchor * mainCamRot;
			}
		}

		private void RenderToTwoStereoTextures(Camera camera)
		{
			// get eye poses
			var leftEyeOffset = StereoRenderManager.Instance.paramFactory.GetEyeSeperation(0);
			var leftEyeRotation = StereoRenderManager.Instance.paramFactory.GetEyeLocalRotation(0);

			var rightEyeOffset = StereoRenderManager.Instance.paramFactory.GetEyeSeperation(1);
			var rightEyeRotation = StereoRenderManager.Instance.paramFactory.GetEyeLocalRotation(1);

			// render stereo textures
			RenderEye(
				leftEyeOffset, leftEyeRotation,
				leftProjMatrix, camera.worldToCameraMatrix,
				leftEyeTexture, "_LeftEyeTexture");

			var rightEyeWorldToCameraMatrix = camera.worldToCameraMatrix;
			rightEyeWorldToCameraMatrix.m03 -= 2.0f * Mathf.Abs(leftEyeOffset.x);

			RenderEye(
				rightEyeOffset, rightEyeRotation,
				rightProjMatrix, rightEyeWorldToCameraMatrix,
				rightEyeTexture, "_RightEyeTexture");
		}

		private void RenderEye(
			Vector3 eyeOffset, Quaternion eyeRotation,
			Matrix4x4 projMat, Matrix4x4 worldToCameraMat,
			RenderTexture targetTexture, string textureName)
		{
			stereoCameraEye.transform.localPosition = eyeOffset;
			stereoCameraEye.transform.localRotation = eyeRotation;

			// set view matrix for mirrors
			if (isMirror)
			{
				stereoCameraEye.worldToCameraMatrix = worldToCameraMat * reflectionMat;
			}

			// set projection matrix
			stereoCameraEye.projectionMatrix = projMat;

			// simulate scissor test if flag is set
			if (useScissor)
			{
				var r = GetScissorRect(projMat * worldToCameraMat);
				stereoCameraEye.rect = r;
				stereoCameraEye.projectionMatrix = GetScissorMatrix(r) * stereoCameraEye.projectionMatrix;
			}
			else
			{
				stereoCameraEye.rect = fullViewport;
			}

			// set oblique near clip plane if flag is set
			if (useObliqueClip)
			{
				var clipPlane = GetObliqueNearClipPlane();
				stereoCameraEye.projectionMatrix = stereoCameraEye.CalculateObliqueMatrix(clipPlane);
			}

			// render
			stereoCameraEye.targetTexture = targetTexture;
			stereoCameraEye.Render();
			stereoMaterial.SetTexture(textureName, targetTexture);
		}

		public void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 normal)
		{
			reflectionMat.m00 = (1.0f - 2.0f * normal[0] * normal[0]);
			reflectionMat.m01 = (-2.0f * normal[0] * normal[1]);
			reflectionMat.m02 = (-2.0f * normal[0] * normal[2]);
			reflectionMat.m03 = (-2.0f * normal[3] * normal[0]);

			reflectionMat.m10 = (-2.0f * normal[1] * normal[0]);
			reflectionMat.m11 = (1.0f - 2.0f * normal[1] * normal[1]);
			reflectionMat.m12 = (-2.0f * normal[1] * normal[2]);
			reflectionMat.m13 = (-2.0f * normal[3] * normal[1]);

			reflectionMat.m20 = (-2.0f * normal[2] * normal[0]);
			reflectionMat.m21 = (-2.0f * normal[2] * normal[1]);
			reflectionMat.m22 = (1.0f - 2.0f * normal[2] * normal[2]);
			reflectionMat.m23 = (-2.0f * normal[3] * normal[2]);

			reflectionMat.m30 = 0.0f;
			reflectionMat.m31 = 0.0f;
			reflectionMat.m32 = 0.0f;
			reflectionMat.m33 = 1.0f;
		}

		private Vector4 GetCameraSpacePlane(Camera cam, Vector3 pt, Vector3 normal)
		{
			Matrix4x4 m = cam.worldToCameraMatrix;
			Vector3 camSpacePt = m.MultiplyPoint(pt);
			Vector3 camSpaceNormal = m.MultiplyVector(normal).normalized;
			return new Vector4(
				camSpaceNormal.x,
				camSpaceNormal.y,
				camSpaceNormal.z,
				-Vector3.Dot(camSpacePt, camSpaceNormal));
		}

		private Vector4 GetObliqueNearClipPlane()
		{
			Vector4 clipPlaneCameraSpace;

			if (!isMirror)
			{
				clipPlaneCameraSpace = GetCameraSpacePlane(stereoCameraEye, anchorPos, anchorForward);
			}
			else
			{
				// get reflection plane -- assume +y as normal
				float d = -Vector3.Dot(canvasOriginUp, canvasOriginPos) - reflectionOffset;
				Vector4 reflectionPlane = new Vector4(canvasOriginUp.x, canvasOriginUp.y, canvasOriginUp.z, d);

				clipPlaneCameraSpace = GetCameraSpacePlane(stereoCameraEye, canvasOriginPos, reflectionPlane);
			}

			return clipPlaneCameraSpace;
		}

		private Rect GetScissorRect(Matrix4x4 mat)
		{
			var renderer = GetComponent<Renderer>();
			Vector3 cen = renderer.bounds.center;
			Vector3 ext = renderer.bounds.extents;
			Vector3[] extentPoints = new Vector3[8]
			{
				 WorldPointToViewport(mat, new Vector3(cen.x-ext.x, cen.y-ext.y, cen.z-ext.z)),
				 WorldPointToViewport(mat, new Vector3(cen.x+ext.x, cen.y-ext.y, cen.z-ext.z)),
				 WorldPointToViewport(mat, new Vector3(cen.x-ext.x, cen.y-ext.y, cen.z+ext.z)),
				 WorldPointToViewport(mat, new Vector3(cen.x+ext.x, cen.y-ext.y, cen.z+ext.z)),
				 WorldPointToViewport(mat, new Vector3(cen.x-ext.x, cen.y+ext.y, cen.z-ext.z)),
				 WorldPointToViewport(mat, new Vector3(cen.x+ext.x, cen.y+ext.y, cen.z-ext.z)),
				 WorldPointToViewport(mat, new Vector3(cen.x-ext.x, cen.y+ext.y, cen.z+ext.z)),
				 WorldPointToViewport(mat, new Vector3(cen.x+ext.x, cen.y+ext.y, cen.z+ext.z))
			};

			bool invalidFlag = false;
			Vector2 min = extentPoints[0];
			Vector2 max = extentPoints[0];
			foreach (Vector3 v in extentPoints)
			{
				// if v.z < 0 means this projection is unreliable
				if (v.z < 0)
				{
					invalidFlag = true;
					break;
				}

				min = Vector2.Min(min, v);
				max = Vector2.Max(max, v);
			}

			if (invalidFlag)
			{
				return fullViewport;
			}
			else
			{
				min = Vector2.Max(min, Vector2.zero);
				max = Vector2.Min(max, Vector2.one);
				return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
			}
		}

		private Matrix4x4 GetScissorMatrix(Rect rect)
		{
			Matrix4x4 m2 = Matrix4x4.TRS(
				new Vector3((1 / rect.width - 1), (1 / rect.height - 1), 0),
				Quaternion.identity,
				new Vector3(1 / rect.width, 1 / rect.height, 1));

			Matrix4x4 m3 = Matrix4x4.TRS(
				new Vector3(-rect.x * 2 / rect.width, -rect.y * 2 / rect.height, 0),
				Quaternion.identity,
				Vector3.one);

			return m3 * m2;
		}

		private Vector3 WorldPointToViewport(Matrix4x4 mat, Vector3 point)
		{
			Vector3 result;
			result.x = mat.m00 * point.x + mat.m01 * point.y + mat.m02 * point.z + mat.m03;
			result.y = mat.m10 * point.x + mat.m11 * point.y + mat.m12 * point.z + mat.m13;
			result.z = mat.m20 * point.x + mat.m21 * point.y + mat.m22 * point.z + mat.m23;

			float a = mat.m30 * point.x + mat.m31 * point.y + mat.m32 * point.z + mat.m33;
			a = 1.0f / a;
			result.x *= a;
			result.y *= a;
			result.z = a;

			point = result;
			point.x = (point.x * 0.5f + 0.5f);
			point.y = (point.y * 0.5f + 0.5f);

			return point;
		}

		/////////////////////////////////////////////////////////////////////////////////
		// callbacks and utilities

		public void SetProjetionMatrices(Matrix4x4 leftMat, Matrix4x4 rightMat)
		{
			leftProjMatrix = leftMat;
			rightProjMatrix = rightMat;
		}

		public void AddPreRenderListener(Action listener)
		{
			if (listener == null) { return; }
			preRenderListeners += listener;
		}

		public void AddPostRenderListener(Action listener)
		{
			if (listener == null) { return; }
			postRenderListeners += listener;
		}

		public void RemovePreRenderListener(Action listener)
		{
			if (listener == null) { return; }
			preRenderListeners -= listener;
		}

		public void RemovePostRenderListener(Action listener)
		{
			if (listener == null) { return; }
			postRenderListeners -= listener;
		}
	}
}
