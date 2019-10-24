using System.Collections.Generic;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;

namespace CustomAvatar.UI
{
	class SettingsViewController : BSMLResourceViewController
	{
		public override string ResourceName => "CustomAvatar.Views.SettingsViewController.bsml";

		protected override void DidActivate(bool firstActivation, ActivationType type)
		{
			base.DidActivate(firstActivation, type);
		}

		protected override void DidDeactivate(DeactivationType deactivationType)
		{
			base.DidDeactivate(deactivationType);
		}

		#region Properties

		[UIValue("resize-options")] private List<object> resizeModeOptions = new List<object> { "Height", "Arm Length", "None" };

		#endregion

		#region Values

		[UIValue("visible-first-person-value")] private bool visibleInFirstPerson;
		[UIValue("resize-value")] private string resizeMode;
		[UIValue("floor-adjust-value")] private bool floorHeightAdjust;

		#endregion

		#region Events

		[UIAction("visible-first-person-change")]
		private void OnVisibleInFirstPersonChanged()
		{

		}

		[UIAction("resize-change")]
		private void OnResizeModeChanged()
		{

		}

		[UIAction("floor-adjust-change")]
		private void OnFloorHeightAdjustChanged()
		{

		}

		[UIAction("measure-arm-span-click")]
		private void OnMeasureArmSpanButtonClicked()
		{

		}

		#endregion
	}
}
