using CustomAvatar.StereoRendering;
using IPA;
using System;
using System.Linq;
using CustomAvatar.UI;
using CustomAvatar.Utilities;
using CustomUI.MenuButton;
using DynamicOpenVR;
using DynamicOpenVR.IO;
using IPA.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;
using Input = UnityEngine.Input;
using Logger = IPA.Logging.Logger;

namespace CustomAvatar
{
	public class Plugin : IBeatSaberPlugin
	{
		private GameScenesManager _scenesManager;

		public event Action<Scene> SceneTransitioned;

		public static Plugin Instance { get; private set; }

		public static Logger Logger { get; private set; }

		public static SkeletalInput LeftHandAnimAction;
		public static SkeletalInput RightHandAnimAction;

		public Plugin()
		{
			OpenVRActionManager actionManager = OpenVRActionManager.Instance;

			LeftHandAnimAction = actionManager.RegisterAction(new SkeletalInput("/actions/customavatars/in/lefthandanim"));
			RightHandAnimAction = actionManager.RegisterAction(new SkeletalInput("/actions/customavatars/in/righthandanim"));
		}

		public void Init(Logger logger)
		{
			Logger = logger;
			Instance = this;

			AvatarManager.Instance.LoadAvatarFromSettingsAsync();
		}

		public void OnApplicationQuit()
		{
			if (_scenesManager != null)
				_scenesManager.transitionDidFinishEvent -= SceneTransitionDidFinish;
		}

		public void OnSceneLoaded(Scene newScene, LoadSceneMode mode)
		{
			if (_scenesManager == null)
			{
				_scenesManager = Resources.FindObjectsOfTypeAll<GameScenesManager>().FirstOrDefault();

				if (_scenesManager != null)
				{
					_scenesManager.transitionDidFinishEvent += SceneTransitionDidFinish;
					_scenesManager.transitionDidFinishEvent += () => SceneTransitioned?.Invoke(SceneManager.GetActiveScene());
				}
			}

			if (newScene.name == "HealthWarning" && Settings.calibrateFullBodyTrackingOnStart)
			{
				AvatarManager.Instance.AvatarTailor.CalibrateFullBodyTracking();
			}

			if (newScene.name == "MenuCore")
			{
				MenuButtonUI.AddButton("Avatars", () =>
				{
					var mainFlowCoordinator = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();
					var flowCoordinator = new GameObject("AvatarListFlowCoordinator").AddComponent<AvatarListFlowCoordinator>();
					mainFlowCoordinator.InvokePrivateMethod("PresentFlowCoordinator", flowCoordinator, null, false, false);
				});
			}
		}

		private void SceneTransitionDidFinish()
		{
			foreach (Camera camera in Camera.allCameras)
			{
				if (camera.gameObject.GetComponent<VRRenderEventDetector>() == null)
				{
					camera.gameObject.AddComponent<VRRenderEventDetector>();
					Logger.Info($"Added {nameof(VRRenderEventDetector)} to {camera}");
				}
			}
			
			Camera mainCamera = Camera.main;

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
				avatarManager.SwitchToNextAvatar();
			}
			else if (Input.GetKeyDown(KeyCode.PageUp))
			{
				avatarManager.SwitchToPreviousAvatar();
			}
			else if (Input.GetKeyDown(KeyCode.Home))
			{
				Settings.isAvatarVisibleInFirstPerson = !Settings.isAvatarVisibleInFirstPerson;
				Logger.Info($"{(Settings.isAvatarVisibleInFirstPerson ? "Enabled" : "Disabled")} first person visibility");
				avatarManager.OnFirstPersonEnabledChanged();
			}
			else if (Input.GetKeyDown(KeyCode.End))
			{
				Settings.resizeMode = (AvatarResizeMode) (((int)Settings.resizeMode + 1) % 3);
				Logger.Info($"Set resize mode to {Settings.resizeMode}");
				avatarManager.ResizeCurrentAvatar();
			}
			else if (Input.GetKeyDown(KeyCode.Insert))
			{
				Settings.enableFloorAdjust = !Settings.enableFloorAdjust;
				Logger.Info($"{(Settings.enableFloorAdjust ? "Enabled" : "Disabled")} floor adjust");
				avatarManager.ResizeCurrentAvatar();
			}
		}

		private void SetCameraCullingMask(Camera camera)
		{
			Logger.Debug("Adding third person culling mask to " + camera.name);

			camera.cullingMask &= ~(1 << AvatarLayers.OnlyInThirdPerson);
		}

		public void OnFixedUpdate() { }

		public void OnSceneUnloaded(Scene scene) { }

		public void OnActiveSceneChanged(Scene prevScene, Scene nextScene) { }

		public void OnApplicationStart() { }
	}
}
