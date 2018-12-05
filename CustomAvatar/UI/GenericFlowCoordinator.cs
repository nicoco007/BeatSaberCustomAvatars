using System;
using VRUI;
using CustomUI.BeatSaber;

namespace CustomAvatar
{
	class GenericFlowCoordinator<TCONT, TLEFT, TRIGHT> : FlowCoordinator where TCONT : VRUIViewController where TLEFT : VRUIViewController where TRIGHT : VRUIViewController
	{
		private TCONT _contentViewController;
		private TLEFT _leftViewController;
		public TRIGHT _rightViewController;
		public Func<TCONT, string> OnContentCreated; 

		protected override void DidActivate(bool firstActivation, ActivationType activationType)
		{
			if (firstActivation)
			{
				_contentViewController = BeatSaberUI.CreateViewController<TCONT>();
				_leftViewController = BeatSaberUI.CreateViewController<TLEFT>();
				_rightViewController = BeatSaberUI.CreateViewController<TRIGHT>();
				title = OnContentCreated(_contentViewController);
			}
			if (activationType == FlowCoordinator.ActivationType.AddedToHierarchy)
			{
				ProvideInitialViewControllers(_contentViewController, _leftViewController, _rightViewController);
			}
		}

		protected override void DidDeactivate(DeactivationType type)
		{
		}
	}
}