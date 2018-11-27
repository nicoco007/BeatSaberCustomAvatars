using System;
using VRUI;
using CustomUI.BeatSaber;

namespace CustomAvatar
{
	class GenericFlowCoordinator<TCONT, TLEFT> : FlowCoordinator where TCONT : VRUIViewController where TLEFT : VRUIViewController
	{
		private TCONT _contentViewController;
		private TLEFT _leftViewController;
		public Func<TCONT, string> OnContentCreated; 

		protected override void DidActivate(bool firstActivation, ActivationType activationType)
		{
			if (firstActivation)
			{
				_contentViewController = BeatSaberUI.CreateViewController<TCONT>();
				_leftViewController = BeatSaberUI.CreateViewController<TLEFT>();
				title = OnContentCreated(_contentViewController);
			}
			if (activationType == FlowCoordinator.ActivationType.AddedToHierarchy)
			{
				ProvideInitialViewControllers(_contentViewController, _leftViewController, null);
			}
		}

		protected override void DidDeactivate(DeactivationType type)
		{
		}
	}
}