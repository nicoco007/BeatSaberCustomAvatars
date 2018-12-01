using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRUI;
using CustomUI.MenuButton;
using CustomUI.Utilities;

namespace CustomAvatar
{
	class AvatarUI
	{
		private class AvatarListFlowCoordinator : GenericFlowCoordinator<AvatarListViewController, AvatarSettingsViewController> { }

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
			if (scene.name == "Menu")
			{
				AddMainButton();
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
