using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AvatarScriptPack;
using CustomAvatar.StereoRendering;
using IPA;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using Logger = CustomAvatar.Util.Logger;

namespace CustomAvatar
{
	public class Plugin : IBeatSaberPlugin
	{
		public static float PLAYER_SCALE = 1.0f;

		private const string CustomAvatarsPath = "CustomAvatars";
		private const string FirstPersonEnabledKey = "avatarFirstPerson";
		private const string PreviousAvatarKey = "previousAvatar";

		private bool _firstPersonEnabled;
		private AvatarUI _avatarUI;

		private GameScenesManager _scenesManager;
		private static bool _isTrackerAsHand;

		private GameObject go;

		public static List<XRNodeState> Trackers = new List<XRNodeState>();
		public static bool IsTrackerAsHand
		{
			get { return _isTrackerAsHand; }
			set
			{
				_isTrackerAsHand = value;
				List<XRNodeState> nodes = new List<XRNodeState>();
				Trackers = new List<XRNodeState>();
				InputTracking.GetNodeStates(nodes);
				foreach (XRNodeState node in nodes)
				{
					Logger.Log($"XRNode: {InputTracking.GetNodeName(node.uniqueID)} - {node.nodeType}");
					if (node.nodeType != XRNode.HardwareTracker || (!InputTracking.GetNodeName(node.uniqueID).Contains("LHR-") && !InputTracking.GetNodeName(node.uniqueID).Contains("Vive Controller MV S/N")))
						continue;
					Trackers.Add(node);
				}
				if (Trackers.Count == 0)
					_isTrackerAsHand = false;
				//Logger.Log("IsTrackerAsHand : " + IsTrackerAsHand);
			}
		}

		public static bool IsFullBodyTracking
		{
			get { return Plugin.FullBodyTrackingType != Plugin.TrackingType.None; ; }
			set
			{
				List<XRNodeState> nodes = new List<XRNodeState>();
				Trackers = new List<XRNodeState>();
				InputTracking.GetNodeStates(nodes);
				foreach (XRNodeState node in nodes)
				{
					Logger.Log($"XRNode: {InputTracking.GetNodeName(node.uniqueID)} - {node.nodeType}");
					if (node.nodeType != XRNode.HardwareTracker || !(InputTracking.GetNodeName(node.uniqueID).Contains("LHR-") || InputTracking.GetNodeName(node.uniqueID).Contains("d4vr")) && !InputTracking.GetNodeName(node.uniqueID).Contains("Vive Controller MV S/N"))
						continue;
					Trackers.Add(node);
				}
				if (Trackers.Count > 0 && Trackers.Count <= 3)
					Plugin.FullBodyTrackingType = (Plugin.TrackingType)Plugin.Trackers.Count;
				else
					Plugin.FullBodyTrackingType = Plugin.TrackingType.None;
				var currentAvatar = Instance.PlayerAvatarManager.GetSpawnedAvatar();
				if (currentAvatar != null)
				{
					var _IKManagerAdvanced = currentAvatar.GameObject.GetComponentInChildren<AvatarScriptPack.IKManagerAdvanced>(true);
					if (_IKManagerAdvanced != null)
					{
						_IKManagerAdvanced.CheckFullBodyTracking();
					}

					
				}
				bool isFullBodyTracking = Plugin.IsFullBodyTracking;
				Logger.Log(string.Concat("IsFullBodyTracking : ", isFullBodyTracking.ToString()));
				Logger.Log(string.Concat("FullBodyTrackingType: ", FullBodyTrackingType.ToString()));
			}
		}

		public event Action<bool> FirstPersonEnabledChanged;
		public event Action<Scene> SceneTransitioned;

		public static Plugin Instance { get; private set; }
		public AvatarLoader AvatarLoader { get; private set; }
		public AvatarTailor AvatarTailor { get; private set; }
		public PlayerAvatarManager PlayerAvatarManager { get; private set; }

		public bool FirstPersonEnabled
		{
			get { return _firstPersonEnabled; }
			set
			{
				if (_firstPersonEnabled == value) return;

				_firstPersonEnabled = value;

				if (value)
				{
					PlayerPrefs.SetInt(FirstPersonEnabledKey, 0);
				}
				else
				{
					PlayerPrefs.DeleteKey(FirstPersonEnabledKey);
				}

				FirstPersonEnabledChanged?.Invoke(value);
			}
		}

		public enum TrackingType
		{
			None,
			Hips,
			Feet,
			Full
		}

		public static Plugin.TrackingType FullBodyTrackingType
		{
			get;
			set;
		}

		public string Name
		{
			get { return "Custom Avatars"; }
		}

		public string Version
		{
			get { return "4.7.4"; }
		}

		public void Init(IPA.Logging.Logger log)
		{
			Util.Logger.logger = log;
			Instance = this;

			AvatarLoader = new AvatarLoader(CustomAvatarsPath, AvatarsLoaded);
			AvatarTailor = new AvatarTailor();
			_avatarUI = new AvatarUI();

			FirstPersonEnabled = PlayerPrefs.HasKey(FirstPersonEnabledKey);
			//RotatePreviewEnabled = PlayerPrefs.HasKey(RotatePreviewEnabledKey);
			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		public void OnApplicationQuit()
		{
			SceneManager.sceneLoaded -= OnSceneLoaded;

			if (PlayerAvatarManager == null) return;
			PlayerAvatarManager.AvatarChanged -= PlayerAvatarManagerOnAvatarChanged;

			if (_scenesManager != null)
				_scenesManager.transitionDidFinishEvent -= SceneTransitionDidFinish;
		}

		private void AvatarsLoaded(IReadOnlyList<CustomAvatar> loadedAvatars)
		{
			if (loadedAvatars.Count == 0)
			{
				Logger.Log("No custom avatars found in path " + Path.GetFullPath(CustomAvatarsPath));
				return;
			}

			var previousAvatarPath = PlayerPrefs.GetString(PreviousAvatarKey, null);
			if (!File.Exists(previousAvatarPath))
			{
				previousAvatarPath = AvatarLoader.Avatars[0].FullPath;
			}

			var previousAvatar = AvatarLoader.Avatars.FirstOrDefault(x => x.FullPath == previousAvatarPath);
			
			PlayerAvatarManager = new PlayerAvatarManager(AvatarLoader, AvatarTailor, previousAvatar);
			PlayerAvatarManager.AvatarChanged += PlayerAvatarManagerOnAvatarChanged;
		}

		public void OnSceneLoaded(Scene newScene, LoadSceneMode mode)
		{
			string cameraName = "MenuMainCamera";
			Camera mainMenuCamera = Resources.FindObjectsOfTypeAll<Camera>().FirstOrDefault(c => c.name == cameraName);

			if (mainMenuCamera)
			{
				StereoRenderManager.Initialize(mainMenuCamera);
			}
			else
			{
				Debug.LogWarning($"Could not find camera with name {cameraName}!");
			}

			if (_scenesManager == null)
			{
				_scenesManager = Resources.FindObjectsOfTypeAll<GameScenesManager>().FirstOrDefault();

				if (_scenesManager != null)
				{
					_scenesManager.transitionDidFinishEvent += SceneTransitionDidFinish;
					_scenesManager.transitionDidFinishEvent += () => SceneTransitioned.Invoke(SceneManager.GetActiveScene());
				}
			}
		}

		private void SceneTransitionDidFinish()
		{
			Camera mainCamera = Camera.main;

			if (PlayerAvatarInput.LeftLegCorrection.Rotation == Quaternion.identity)
			{
				Console.WriteLine("Scene transition finished");
				var tempInput = new PlayerAvatarInput();
				var normal = Vector3.up;

				PosRot leftFoot = tempInput.LeftLegPosRot;
				Vector3 leftFootForward = leftFoot.Rotation * Vector3.up; // forward on feet trackers is y (up)
				Vector3 leftFootStraightForward = Vector3.ProjectOnPlane(leftFootForward, normal); // get projection of forward vector on xz plane (floor)
				Quaternion leftRotationCorrection = Quaternion.Inverse(leftFoot.Rotation) * Quaternion.LookRotation(Vector3.up, leftFootStraightForward); // get difference between world rotation and flat forward rotation
				PlayerAvatarInput.LeftLegCorrection = new PosRot(leftFoot.Position.y * Vector3.down, leftRotationCorrection);

				PosRot rightFoot = tempInput.RightLegPosRot;
				Vector3 rightFootForward = rightFoot.Rotation * Vector3.up;
			    Vector3 rightFootStraightForward = Vector3.ProjectOnPlane(rightFootForward, normal);
				Quaternion rightRotationCorrection = Quaternion.Inverse(rightFoot.Rotation) * Quaternion.LookRotation(Vector3.up, rightFootStraightForward);
				PlayerAvatarInput.RightLegCorrection = new PosRot(rightFoot.Position.y * Vector3.down, rightRotationCorrection);
			}

			IsFullBodyTracking = true;

			if (mainCamera)
			{
				SetCameraCullingMask(mainCamera);
				mainCamera.nearClipPlane = 0.01f;
			}
			else
			{
				Debug.LogWarning("Could not find main camera!");
			}
		}

		private void PlayerAvatarManagerOnAvatarChanged(CustomAvatar newAvatar)
		{
			PlayerPrefs.SetString(PreviousAvatarKey, newAvatar.FullPath);
			IsFullBodyTracking = IsFullBodyTracking;
		}

		public void OnUpdate()
		{
			Camera mainCamera = Camera.main;

			if (mainCamera != null)
			{
				mainCamera.transform.parent.localScale = Vector3.one * PLAYER_SCALE;
				mainCamera.transform.localScale = Vector3.one * 1 / PLAYER_SCALE;
			}

			if (Input.GetKeyDown(KeyCode.PageDown))
			{
				PLAYER_SCALE /= 1.1f;
				//PlayerAvatarManager?.SwitchToNextAvatar();
			}
			else if (Input.GetKeyDown(KeyCode.PageUp))
			{
				PLAYER_SCALE *= 1.1f;
				//PlayerAvatarManager?.SwitchToPreviousAvatar();
			}
			else if (Input.GetKeyDown(KeyCode.Home))
			{
				FirstPersonEnabled = !FirstPersonEnabled;
			}
			else if (Input.GetKeyDown(KeyCode.F6))
			{
				IsTrackerAsHand = !IsTrackerAsHand;
			}
			else if (Input.GetKeyDown(KeyCode.F5))
			{
				IsFullBodyTracking = !IsFullBodyTracking;
			}
			else if (Input.GetKeyDown(KeyCode.End))
			{
				int policy = (int)Plugin.Instance.AvatarTailor.ResizePolicy + 1;
				if (policy > 2) policy = 0;
				Plugin.Instance.AvatarTailor.ResizePolicy = (AvatarTailor.ResizePolicyType)policy;
				Logger.Log($"Set Resize Policy to {Plugin.Instance.AvatarTailor.ResizePolicy}");
				Plugin.Instance.PlayerAvatarManager.ResizePlayerAvatar();
			}
			else if (Input.GetKeyDown(KeyCode.Insert))
			{
				if (Plugin.Instance.AvatarTailor.FloorMovePolicy == AvatarTailor.FloorMovePolicyType.AllowMove)
					Plugin.Instance.AvatarTailor.FloorMovePolicy = AvatarTailor.FloorMovePolicyType.NeverMove;
				else
					Plugin.Instance.AvatarTailor.FloorMovePolicy = AvatarTailor.FloorMovePolicyType.AllowMove;
				Logger.Log($"Set Floor Move Policy to {Plugin.Instance.AvatarTailor.FloorMovePolicy}");
				Plugin.Instance.PlayerAvatarManager.ResizePlayerAvatar();
			}
		}

		private void SetCameraCullingMask(Camera camera)
		{
			Logger.Log("Adding third person culling mask to " + camera.name);

			camera.cullingMask &= ~(1 << AvatarLayers.OnlyInThirdPerson);
			camera.cullingMask |= 1 << AvatarLayers.Global;
		}

		public void OnFixedUpdate() { }

		public void OnSceneUnloaded(Scene scene) { }

		public void OnActiveSceneChanged(Scene prevScene, Scene nextScene) { }

		public void OnApplicationStart() { }
	}
}
