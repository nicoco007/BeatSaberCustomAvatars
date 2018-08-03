using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IllusionPlugin;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CustomAvatar
{
	public class Plugin : IPlugin
	{
		public const float DefaultPlayerHeight = 1.75f;
		private const string CustomAvatarsPath = "CustomAvatars";
		private const string FirstPersonEnabledKey = "avatarFirstPerson";
		private const string PreviousAvatarKey = "previousAvatar";
		
		private bool _init;
		private bool _firstPersonEnabled;
		
		public Plugin()
		{
			Instance = this;
		}

		public event Action<bool> FirstPersonEnabledChanged;

		public static Plugin Instance { get; private set; }
		public AvatarLoader AvatarLoader { get; private set; }
		public PlayerAvatarManager PlayerAvatarManager { get; private set; }

		public bool FirstPersonEnabled
		{
			get { return _firstPersonEnabled; }
			private set
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

		public string Name
		{
			get { return "Custom Avatar Plugin"; }
		}

		public string Version
		{
			get { return "3.0-beta"; }
		}

		public static void Log(string message)
		{
			Console.WriteLine("[CustomAvatarPlugin] " + message);
			File.AppendAllText("CustomAvatarPlugin-log.txt", "[Custom Avatar Plugin] " + message + Environment.NewLine);
		}

		public void OnApplicationStart()
		{
			if (_init) return;
			_init = true;
			
			File.WriteAllText("CustomAvatarPlugin-log.txt", string.Empty);
			
			AvatarLoader = new AvatarLoader(CustomAvatarsPath, AvatarsLoaded);
			
			FirstPersonEnabled = PlayerPrefs.HasKey(FirstPersonEnabledKey);
			SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
		}

		public void OnApplicationQuit()
		{
			SceneManager.activeSceneChanged -= SceneManagerOnActiveSceneChanged;

			if (PlayerAvatarManager == null) return;
			PlayerAvatarManager.AvatarChanged -= PlayerAvatarManagerOnAvatarChanged;
		}

		private void AvatarsLoaded(IReadOnlyList<CustomAvatar> loadedAvatars)
		{
			if (loadedAvatars.Count == 0)
			{
				Log("No custom avatars found in path " + Path.GetFullPath(CustomAvatarsPath));
				return;
			}

			var previousAvatarPath = PlayerPrefs.GetString(PreviousAvatarKey, null);
			var previousAvatar = AvatarLoader.Avatars.FirstOrDefault(x => x.FullPath == previousAvatarPath);
			
			PlayerAvatarManager = new PlayerAvatarManager(AvatarLoader, previousAvatar);
			PlayerAvatarManager.AvatarChanged += PlayerAvatarManagerOnAvatarChanged;
		}

		private void SceneManagerOnActiveSceneChanged(Scene oldScene, Scene newScene)
		{	
			SetCameraCullingMask();
		}

		private void PlayerAvatarManagerOnAvatarChanged(CustomAvatar newAvatar)
		{
			PlayerPrefs.SetString(PreviousAvatarKey, newAvatar.FullPath);
		}

		public void OnUpdate()
		{
			if (Input.GetKeyDown(KeyCode.PageUp))
			{
				if (PlayerAvatarManager == null) return;
				PlayerAvatarManager.SwitchToNextAvatar();
			}
			else if (Input.GetKeyDown(KeyCode.PageDown))
			{
				if (PlayerAvatarManager == null) return;
				PlayerAvatarManager.SwitchToPreviousAvatar();
			}
			else if (Input.GetKeyDown(KeyCode.Home))
			{
				FirstPersonEnabled = !FirstPersonEnabled;
			}
		}

		private static void SetCameraCullingMask()
		{
			var mainCamera = Camera.main;
			if (mainCamera == null) return;
			mainCamera.cullingMask &= ~(1 << AvatarLayers.OnlyInThirdPerson);
			mainCamera.cullingMask |= 1 << AvatarLayers.OnlyInFirstPerson;
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