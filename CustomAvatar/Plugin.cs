namespace CustomAvatar
{
	using StereoRendering;
	using IPA;
	using IPA.Logging;
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using UnityEngine;
	using UnityEngine.SceneManagement;
	using Harmony;

	public class Plugin : IBeatSaberPlugin
	{
		public static float PLAYER_SCALE = 1.0f;

		private const string CustomAvatarsPath = "CustomAvatars";
		private const string FirstPersonEnabledKey = "avatarFirstPerson";
		private const string PreviousAvatarKey = "previousAvatar";

		private bool _firstPersonEnabled;
		private AvatarUI _avatarUI;

		private GameScenesManager _scenesManager;

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
			get { return "4.8.0"; }
		}

		public static IPA.Logging.Logger Logger { get; private set; }

		private HarmonyInstance harmonyInstance;
		private OpenVRInputManager inputManager;

		public void Init(IPA.Logging.Logger logger)
		{
			Logger = logger;
			Instance = this;

			harmonyInstance = HarmonyInstance.Create("beatsabercustomavatars");

			Logger.Info("Applying Index controller patch");
			harmonyInstance.PatchAll();

			inputManager = PersistentSingleton<OpenVRInputManager>.instance;

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
				Logger.Warn("No custom avatars found in path " + Path.GetFullPath(CustomAvatarsPath));
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
				Plugin.Logger.Error($"Could not find camera with name {cameraName}!");
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

			Logger.Info("SceneTransitionDidFinish");

			var input = PersistentSingleton<TrackedDeviceManager>.instance;

			if (input.Head.Found && input.LeftFoot.Found && input.RightFoot.Found && input.Waist.Found && AvatarBehaviour.LeftLegCorrection == null)
			{
				Logger.Info("Calibrating full body tracking");

				TrackedDeviceState head = input.Head;
				TrackedDeviceState leftFoot = input.LeftFoot;
				TrackedDeviceState rightFoot = input.RightFoot;
				TrackedDeviceState pelvis = input.Waist;

				var eyeHeight = head.Position.y;
				var normal = Vector3.up;

				Vector3 leftFootForward = leftFoot.Rotation * Vector3.up; // forward on feet trackers is y (up)
				Vector3 leftFootStraightForward = Vector3.ProjectOnPlane(leftFootForward, normal); // get projection of forward vector on xz plane (floor)
				Quaternion leftRotationCorrection = Quaternion.Inverse(leftFoot.Rotation) * Quaternion.LookRotation(Vector3.up, leftFootStraightForward); // get difference between world rotation and flat forward rotation
				AvatarBehaviour.LeftLegCorrection = new PosRot(leftFoot.Position.y * Vector3.down, leftRotationCorrection);

				Plugin.Logger.Info("LeftLegCorrection: " + AvatarBehaviour.LeftLegCorrection);
				Plugin.Logger.Info("Position: " + leftFoot.Position.y * Vector3.down);
				Plugin.Logger.Info("Rotation: " + leftRotationCorrection);

				Vector3 rightFootForward = rightFoot.Rotation * Vector3.up;
			    Vector3 rightFootStraightForward = Vector3.ProjectOnPlane(rightFootForward, normal);
				Quaternion rightRotationCorrection = Quaternion.Inverse(rightFoot.Rotation) * Quaternion.LookRotation(Vector3.up, rightFootStraightForward);
				AvatarBehaviour.RightLegCorrection = new PosRot(rightFoot.Position.y * Vector3.down, rightRotationCorrection);

				// using "standard" 8 head high body proportions w/ eyes at 1/2 head height
				// http://carvinginnyc.com/wp-content/uploads/2018/09/aa94d39c207ade6ea850c86728296530.jpg
				// head height is multiplied by 3 to allow nice numbers
				Vector3 wantedPelvisPosition = new Vector3(0, eyeHeight / 22.5f * 14f, 0);
				Vector3 pelvisPositionCorrection = wantedPelvisPosition - Vector3.up * pelvis.Position.y;
				AvatarBehaviour.PelvisCorrection = new PosRot(pelvisPositionCorrection, Quaternion.identity);
			}

			if (mainCamera)
			{
				SetCameraCullingMask(mainCamera);
				mainCamera.nearClipPlane = 0.01f;
			}
			else
			{
				Logger.Error("Could not find main camera!");
			}
		}

		private void PlayerAvatarManagerOnAvatarChanged(CustomAvatar newAvatar)
		{
			PlayerPrefs.SetString(PreviousAvatarKey, newAvatar.FullPath);
		}

		public void OnUpdate()
		{
			Camera mainCamera = Camera.main;

			if (mainCamera != null)
			{
				mainCamera.transform.parent.localScale = Vector3.one * PLAYER_SCALE;
				mainCamera.transform.localScale = Vector3.one * 1 / PLAYER_SCALE;
			}

			var up = inputManager.Up;
			var down = inputManager.Down;
			var reset = inputManager.Reset;

			if (up.bState && up.bChanged)
			{
				PLAYER_SCALE *= 1.1f;
			}

			if (down.bState && down.bChanged)
			{
				PLAYER_SCALE /= 1.1f;
			}

			if (reset.bState && reset.bChanged)
			{
				PLAYER_SCALE = 1f;
			}

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
			else if (Input.GetKeyDown(KeyCode.End))
			{
				int policy = (int)Plugin.Instance.AvatarTailor.ResizePolicy + 1;
				if (policy > 2) policy = 0;
				Plugin.Instance.AvatarTailor.ResizePolicy = (AvatarTailor.ResizePolicyType)policy;
				Logger.Info($"Set Resize Policy to {Plugin.Instance.AvatarTailor.ResizePolicy}");
				Plugin.Instance.PlayerAvatarManager.ResizePlayerAvatar();
			}
			else if (Input.GetKeyDown(KeyCode.Insert))
			{
				if (Plugin.Instance.AvatarTailor.FloorMovePolicy == AvatarTailor.FloorMovePolicyType.AllowMove)
					Plugin.Instance.AvatarTailor.FloorMovePolicy = AvatarTailor.FloorMovePolicyType.NeverMove;
				else
					Plugin.Instance.AvatarTailor.FloorMovePolicy = AvatarTailor.FloorMovePolicyType.AllowMove;
				Logger.Info($"Set Floor Move Policy to {Plugin.Instance.AvatarTailor.FloorMovePolicy}");
				Plugin.Instance.PlayerAvatarManager.ResizePlayerAvatar();
			}
		}

		private void SetCameraCullingMask(Camera camera)
		{
			Logger.Debug("Adding third person culling mask to " + camera.name);

			camera.cullingMask &= ~(1 << AvatarLayers.OnlyInThirdPerson);
			camera.cullingMask |= 1 << AvatarLayers.Global;
		}

		public void OnFixedUpdate() { }

		public void OnSceneUnloaded(Scene scene) { }

		public void OnActiveSceneChanged(Scene prevScene, Scene nextScene) { }

		public void OnApplicationStart() { }
	}
}
