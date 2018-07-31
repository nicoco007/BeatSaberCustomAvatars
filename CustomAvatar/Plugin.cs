using System;
using System.IO;
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
		
		private bool _init;
		private bool _firstPersonEnabled;
		
		public Plugin()
		{
			Instance = this;
		}

		public event Action<bool> FirstPersonEnabledChanged;

		public static Plugin Instance { get; private set; }
		public AvatarsManager AvatarsManager { get; private set; }

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
			get { return "3.0"; }
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
			AvatarsManager = new AvatarsManager(CustomAvatarsPath);
			
			FirstPersonEnabled = PlayerPrefs.HasKey(FirstPersonEnabledKey);
			SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
		}

		public void OnApplicationQuit()
		{
			SceneManager.activeSceneChanged -= SceneManagerOnActiveSceneChanged;
		}

		private void SceneManagerOnActiveSceneChanged(Scene oldScene, Scene newScene)
		{
			var mainCamera = Camera.main;
			if (mainCamera == null) return;
			Console.WriteLine("Setting culling mask!");
			mainCamera.cullingMask &= ~(1 << (int) AvatarLayer.NotShownInFirstPerson);
		}

		public void OnUpdate()
		{
			if (Input.GetKeyDown(KeyCode.PageUp))
			{
				AvatarsManager.SwitchToNextAvatar();
			}
			else if (Input.GetKeyDown(KeyCode.PageDown))
			{
				AvatarsManager.SwitchToPreviousAvatar();
			}
			else if (Input.GetKeyDown(KeyCode.Home))
			{
				FirstPersonEnabled = !FirstPersonEnabled;
			}
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