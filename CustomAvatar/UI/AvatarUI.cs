using System.Linq;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using VRUI;
using CustomUI.MenuButton;
using CustomUI.Utilities;

namespace CustomAvatar
{
	class AvatarUI
	{
		private class AvatarListFlowCoordinator : GenericFlowCoordinator<AvatarListViewController, AvatarSettingsViewController, AvatarPreviewController> { }

		private FlowCoordinator _flowCoordinator = null;

		public AvatarUI()
		{
			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		~AvatarUI()
		{
			SceneManager.sceneLoaded -= OnSceneLoaded;
		}

		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			if (Plugin.Instance.AvatarLoader.Avatars.Count == 0)
			{
				Console.WriteLine("[CustomAvatarsPlugin] No avatars found. Button not created.");
			}
			else if (scene.name == "Menu")
			{
				AddMainButton();
				Console.WriteLine("[CustomAvatarsPlugin] Creating Avatars Button.");
			}
		}

		private void AddMainButton()
		{
			MenuButtonUI.AddButton("Avatars", delegate ()
			{
				var mainFlowCoordinator = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();
				if (_flowCoordinator == null)
				{
					var flowCoordinator = new GameObject("AvatarListFlowCoordinator").AddComponent<AvatarListFlowCoordinator>();
					flowCoordinator.OnContentCreated = (content) =>
					{
						content.onBackPressed = () =>
						{
							mainFlowCoordinator.InvokePrivateMethod("DismissFlowCoordinator", new object[] { flowCoordinator, null, false });
						};
						return "Avatar Select";
					};
					_flowCoordinator = flowCoordinator;
				}
				mainFlowCoordinator.InvokePrivateMethod("PresentFlowCoordinator", new object[] { _flowCoordinator, null, false, false });
			});
		}
	}
}
