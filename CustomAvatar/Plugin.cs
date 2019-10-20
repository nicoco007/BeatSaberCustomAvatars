using CustomAvatar.StereoRendering;
using IPA;
using System;
using System.Linq;
using CustomAvatar.UI;
using CustomUI.MenuButton;
using DynamicOpenVR;
using DynamicOpenVR.IO;
using IPA.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;
using Input = UnityEngine.Input;

namespace CustomAvatar
{

	public class Plugin : IBeatSaberPlugin
	{
		private const string FirstPersonEnabledKey = "avatarFirstPerson";

		private bool _firstPersonEnabled;

		private GameScenesManager _scenesManager;

		public event Action<bool> FirstPersonEnabledChanged;
		public event Action<Scene> SceneTransitioned;

		public static Plugin Instance { get; private set; }

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

		public static IPA.Logging.Logger Logger { get; private set; }

		public static SkeletalInput LeftHandAnimAction;
		public static SkeletalInput RightHandAnimAction;

		public Plugin()
		{
			OpenVRActionManager actionManager = OpenVRActionManager.Instance;

			LeftHandAnimAction = actionManager.RegisterAction(new SkeletalInput("/actions/customavatars/in/lefthandanim"));
			RightHandAnimAction = actionManager.RegisterAction(new SkeletalInput("/actions/customavatars/in/righthandanim"));
		}

		public void Init(IPA.Logging.Logger logger)
		{
			Logger = logger;
			Instance = this;

			AvatarManager.Instance.LoadAvatarFromSettingsAsync();

			FirstPersonEnabled = PlayerPrefs.HasKey(FirstPersonEnabledKey);
			//RotatePreviewEnabled = PlayerPrefs.HasKey(RotatePreviewEnabledKey);
			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		public void OnApplicationQuit()
		{
			SceneManager.sceneLoaded -= OnSceneLoaded;

			if (_scenesManager != null)
				_scenesManager.transitionDidFinishEvent -= SceneTransitionDidFinish;
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
				Logger.Error($"Could not find camera with name {cameraName}!");
			}

			if (_scenesManager == null)
			{
				_scenesManager = Resources.FindObjectsOfTypeAll<GameScenesManager>().FirstOrDefault();

				if (_scenesManager != null)
				{
					_scenesManager.transitionDidFinishEvent += SceneTransitionDidFinish;
					_scenesManager.transitionDidFinishEvent += () => SceneTransitioned?.Invoke(SceneManager.GetActiveScene());
				}
			}

			if (newScene.name == "MenuCore")
			{
				MenuButtonUI.AddButton("Avatars", delegate ()
				{
					var mainFlowCoordinator = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();
					var flowCoordinator = new GameObject("AvatarListFlowCoordinator").AddComponent<AvatarListFlowCoordinator>();
					mainFlowCoordinator.InvokePrivateMethod("PresentFlowCoordinator", flowCoordinator, null, false, false);
				});
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

				Vector3 rightFootForward = rightFoot.Rotation * Vector3.up;
			    Vector3 rightFootStraightForward = Vector3.ProjectOnPlane(rightFootForward, normal);
				Quaternion rightRotationCorrection = Quaternion.Inverse(rightFoot.Rotation) * Quaternion.LookRotation(Vector3.up, rightFootStraightForward);
				AvatarBehaviour.RightLegCorrection = new PosRot(rightFoot.Position.y * Vector3.down, rightRotationCorrection);

				// using "standard" 8 head high body proportions w/ eyes at 1/2 head height
				// reference: https://miro.medium.com/max/3200/1*cqTRyEGl26l4CImEmWz68Q.jpeg
				Vector3 wantedPelvisPosition = new Vector3(0, eyeHeight / 15f * 10f, 0);
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

		public void OnUpdate()
		{
			AvatarManager avatarManager = AvatarManager.Instance;

			if (Input.GetKeyDown(KeyCode.PageDown))
			{
				avatarManager?.SwitchToNextAvatar();
			}
			else if (Input.GetKeyDown(KeyCode.PageUp))
			{
				avatarManager?.SwitchToPreviousAvatar();
			}
			else if (Input.GetKeyDown(KeyCode.Home))
			{
				FirstPersonEnabled = !FirstPersonEnabled;
			}
			else if (Input.GetKeyDown(KeyCode.End))
			{
				int policy = (int)avatarManager.AvatarTailor.ResizePolicy + 1;
				if (policy > 2) policy = 0;
				avatarManager.AvatarTailor.ResizePolicy = (AvatarTailor.ResizePolicyType)policy;
				Logger.Info($"Set Resize Policy to {avatarManager.AvatarTailor.ResizePolicy}");
				avatarManager.ResizePlayerAvatar();
			}
			else if (Input.GetKeyDown(KeyCode.Insert))
			{
				if (avatarManager.AvatarTailor.FloorMovePolicy == AvatarTailor.FloorMovePolicyType.AllowMove)
					avatarManager.AvatarTailor.FloorMovePolicy = AvatarTailor.FloorMovePolicyType.NeverMove;
				else
					avatarManager.AvatarTailor.FloorMovePolicy = AvatarTailor.FloorMovePolicyType.AllowMove;
				Logger.Info($"Set Floor Move Policy to {avatarManager.AvatarTailor.FloorMovePolicy}");
				avatarManager.ResizePlayerAvatar();
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
