using System;
using System.IO;
using IllusionPlugin;
using UnityEngine;

namespace CustomAvatar
{
	public class Plugin : IPlugin
	{
		private const string CustomAvatarsPath = "CustomAvatars";
		
		private AvatarsManager _avatarsManager;
		private bool _init;
		
		public string Name
		{
			get { return "Custom Avatars 2"; }
		}

		public string Version
		{
			get { return "1.0"; }
		}

		public static void Log(string message)
		{
			Console.WriteLine("[CustomAvatars2] " + message);
			File.AppendAllText("CustomAvatars2-log.txt", "[Avatar Plugin] " + message + Environment.NewLine);
		}

		public void OnApplicationStart()
		{
			if (_init) return;
			_init = true;
			
			_avatarsManager = new AvatarsManager(CustomAvatarsPath);
		}

		public void OnApplicationQuit()
		{
		}

		public void OnUpdate()
		{
			if (Input.GetKeyDown(KeyCode.PageUp))
			{
				_avatarsManager.SwitchToNextAvatar();
			}
			else if (Input.GetKeyDown(KeyCode.PageDown))
			{
				_avatarsManager.SwitchToPreviousAvatar();
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