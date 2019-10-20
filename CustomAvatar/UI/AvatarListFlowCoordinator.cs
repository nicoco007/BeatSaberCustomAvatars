using System;
using BeatSaberMarkupLanguage;
using VRUI;
using UnityEngine;

namespace CustomAvatar.UI
{
	class AvatarListFlowCoordinator : FlowCoordinator
	{
		private AvatarPreviewController _contentViewController;
		private AvatarSettingsViewController _leftViewController;
		public VRUIViewController _rightViewController;
		public Func<AvatarListViewController, string> OnContentCreated;

		private Vector3 MainScreenPosition;
		private GameObject MainScreen;

		protected override void DidActivate(bool firstActivation, ActivationType activationType)
		{
			MainScreen = GameObject.Find("MainScreen");
			MainScreenPosition = MainScreen.transform.position;

			if (firstActivation)
			{
				_contentViewController = BeatSaberUI.CreateViewController<AvatarPreviewController>();
				_leftViewController = BeatSaberUI.CreateViewController<AvatarSettingsViewController>();
				_rightViewController = BeatSaberUI.CreateViewController<AvatarListViewController>();

				// title = OnContentCreated(_rightViewController);
			}

			if (activationType == ActivationType.AddedToHierarchy)
			{
				ProvideInitialViewControllers(_contentViewController, _leftViewController, _rightViewController);
				MirrorController.OnLoad();
				MainScreen.transform.position = new Vector3(0, -100, 0); // "If it works it's not stupid" - Caeden117
				//_rightViewController.onBackPressed += backButton_DidFinish;
			}
		}

		private void backButton_DidFinish()
		{
			MainScreen.transform.position = MainScreenPosition;
			Destroy(MirrorController.Instance.gameObject);
			//_rightViewController.onBackPressed -= backButton_DidFinish;
		}

		protected override void DidDeactivate(DeactivationType type)
		{
		}
	}
}
