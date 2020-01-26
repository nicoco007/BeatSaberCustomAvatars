using System.Linq;
using BeatSaberMarkupLanguage;
using HMUI;
using IPA.Utilities;
using UnityEngine;

namespace CustomAvatar.UI
{
    class AvatarListFlowCoordinator : FlowCoordinator
    {
        private GameObject _mainScreen;
        private Vector3 _mainScreenScale;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            _mainScreen = GameObject.Find("MainScreen");
            _mainScreenScale = _mainScreen.transform.localScale;

            showBackButton = true;

            if (firstActivation)
            {
                title = "Custom Avatars";

                ViewController contentViewController = BeatSaberUI.CreateViewController<MirrorViewController>();
                ViewController leftViewController = BeatSaberUI.CreateViewController<SettingsViewController>();
                ViewController rightViewController = BeatSaberUI.CreateViewController<AvatarListViewController>();

                ProvideInitialViewControllers(contentViewController, leftViewController, rightViewController);
                
                _mainScreen.transform.localScale = Vector3.zero;
            }
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            _mainScreen.transform.localScale = _mainScreenScale;
            var mainFlowCoordinator = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();
            mainFlowCoordinator.InvokePrivateMethod("DismissFlowCoordinator", new object[] { this, null, false });
        }

        protected override void DidDeactivate(DeactivationType deactivationType) { }
    }
}