using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IllusionPlugin;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace CustomAvatar
{
	public class Plugin : IPlugin
	{
		private const string CustomAvatarsPath = "CustomAvatars";
		private const string FirstPersonEnabledKey = "avatarFirstPerson";
		private const string PreviousAvatarKey = "previousAvatar";
		private const string RotatePreviewEnabledKey = "rotatePreview";

		private bool _init;
		private bool _firstPersonEnabled;
		private AvatarUI _avatarUI;

		private WaitForSecondsRealtime _sceneLoadWait = new WaitForSecondsRealtime(0.1f);
		private GameScenesManager _scenesManager;
		private static bool _isTrackerAsHand;

		public static List<XRNodeState> Trackers = new List<XRNodeState>();
		public static bool IsTrackerAsHand
		{
			get { return _isTrackerAsHand; }
			set
			{
				_isTrackerAsHand = value;
				List<XRNodeState> notes = new List<XRNodeState>();
				Trackers = new List<XRNodeState>();
				InputTracking.GetNodeStates(notes);
				foreach (XRNodeState note in notes)
				{
					if (note.nodeType != XRNode.HardwareTracker || !InputTracking.GetNodeName(note.uniqueID).Contains("LHR-"))
						continue;
					Trackers.Add(note);
				}
				if (Trackers.Count == 0)
					_isTrackerAsHand = false;
				Console.WriteLine("IsTrackerAsHand : " + IsTrackerAsHand);
			}
		}

		public static bool IsFullBodyTracking
		{
			get { return Plugin.FullBodyTrackingType != Plugin.TrackingType.None; ; }
			set
			{
				List<XRNodeState> notes = new List<XRNodeState>();
				Trackers = new List<XRNodeState>();
				InputTracking.GetNodeStates(notes);
				foreach (XRNodeState note in notes)
				{
					if (note.nodeType != XRNode.HardwareTracker || !InputTracking.GetNodeName(note.uniqueID).Contains("LHR-"))
						continue;
					Trackers.Add(note);
				}
				if (Trackers.Count >= 0 && Trackers.Count <= 3)
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
				Console.WriteLine(string.Concat("IsFullBodyTracking : ", isFullBodyTracking.ToString()));
				Console.WriteLine(string.Concat("FullBodyTrackingType: ", FullBodyTrackingType.ToString()));
			}
		}
		
		public Plugin()
		{
			Instance = this;
		}

		public event Action<bool> FirstPersonEnabledChanged;

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

				if (FirstPersonEnabledChanged != null)
				{
					FirstPersonEnabledChanged(value);
				}
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

		public bool RotatePreviewEnabled
		{
			get { return AvatarPreviewRotation.rotatePreview; }
			set
			{
				if (AvatarPreviewRotation.rotatePreview == value) return;

				AvatarPreviewRotation.rotatePreview = value;

				if (value)
				{
					PlayerPrefs.SetInt(RotatePreviewEnabledKey, 0);
				}
				else
				{
					PlayerPrefs.DeleteKey(RotatePreviewEnabledKey);
				}
			}
		}

		public string Name
		{
			get { return "Custom Avatars Plugin"; }
		}

		public string Version
		{
			get { return "4.4.0"; }
		}

		public static void Log(object message)
		{
			string fullMsg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.FFF}] [CustomAvatarsPlugin] {message}";

			Debug.Log(fullMsg);
			File.AppendAllText("CustomAvatarsPlugin-log.txt", fullMsg + Environment.NewLine);
		}

		public void OnApplicationStart()
		{
			if (_init) return;
			_init = true;
			
			File.WriteAllText("CustomAvatarsPlugin-log.txt", string.Empty);
			
			AvatarLoader = new AvatarLoader(CustomAvatarsPath, AvatarsLoaded);
			AvatarTailor = new AvatarTailor();
			_avatarUI = new AvatarUI();
			
			FirstPersonEnabled = PlayerPrefs.HasKey(FirstPersonEnabledKey);
			RotatePreviewEnabled = PlayerPrefs.HasKey(RotatePreviewEnabledKey);
			SceneManager.sceneLoaded += SceneManagerOnSceneLoaded;
		}

		public void OnApplicationQuit()
		{
			SceneManager.sceneLoaded -= SceneManagerOnSceneLoaded;

			if (PlayerAvatarManager == null) return;
			PlayerAvatarManager.AvatarChanged -= PlayerAvatarManagerOnAvatarChanged;

			if (_scenesManager != null)
				_scenesManager.transitionDidFinishEvent -= SceneTransitionDidFinish;
		}

		private void AvatarsLoaded(IReadOnlyList<CustomAvatar> loadedAvatars)
		{
			if (loadedAvatars.Count == 0)
			{
				Log("No custom avatars found in path " + Path.GetFullPath(CustomAvatarsPath));
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
			IsFullBodyTracking = true;
		}

		private void SceneManagerOnSceneLoaded(Scene newScene, LoadSceneMode mode)
		{
			if (_scenesManager == null)
			{
				_scenesManager = Resources.FindObjectsOfTypeAll<GameScenesManager>().FirstOrDefault();

				if (_scenesManager != null)
					_scenesManager.transitionDidFinishEvent += SceneTransitionDidFinish;
			}
		}

		private void SceneTransitionDidFinish()
		{
			Camera mainCamera = Camera.main;
			SetCameraCullingMask(mainCamera);
			
			PlayerAvatarManager?.OnSceneTransitioned(SceneManager.GetActiveScene());
		}

		private void PlayerAvatarManagerOnAvatarChanged(CustomAvatar newAvatar)
		{
			PlayerPrefs.SetString(PreviousAvatarKey, newAvatar.FullPath);
			IsFullBodyTracking = IsFullBodyTracking;
		}

		public void OnUpdate()
		{
			if (Input.GetKeyDown(KeyCode.PageDown))
			{
				PlayerAvatarManager?.SwitchToNextAvatar();
			}
			else if (Input.GetKeyDown(KeyCode.PageUp))
			{
				PlayerAvatarManager?.SwitchToPreviousAvatar();
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
		}

		private void SetCameraCullingMask(Camera camera)
		{
			Log("Adding third person culling mask to " + camera.name);

			camera.cullingMask &= ~(1 << AvatarLayers.OnlyInThirdPerson);
			camera.cullingMask |= 1 << AvatarLayers.OnlyInFirstPerson;
		}

		public void OnFixedUpdate()
		{
		}

		public void OnLevelWasInitialized(int level)
		{
		}

		public void OnLevelWasLoaded(int level)
		{
		}
	}
}
