using System;
using System.Linq;
using BeatSaberMarkupLanguage;
using HMUI;
using IPA.Utilities;
using UnityEngine;

namespace CustomAvatar.UI
{
    class AvatarListFlowCoordinator : FlowCoordinator
    {
        private GameObject mainScreen;
        private Vector3 mainScreenScale;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            mainScreen = GameObject.Find("MainScreen");
            mainScreenScale = mainScreen.transform.localScale;

            showBackButton = true;

            if (firstActivation)
            {
                title = "Custom Avatars";

                ViewController contentViewController = BeatSaberUI.CreateViewController<MirrorViewController>();
                ViewController leftViewController = BeatSaberUI.CreateViewController<SettingsViewController>();
                ViewController rightViewController = BeatSaberUI.CreateViewController<AvatarListViewController>();

                ProvideInitialViewControllers(contentViewController, leftViewController, rightViewController);
                
                mainScreen.transform.localScale = Vector3.zero;
            }
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            mainScreen.transform.localScale = mainScreenScale;
            var mainFlowCoordinator = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();
            mainFlowCoordinator.InvokePrivateMethod("DismissFlowCoordinator", new object[] { this, null, false });
        }

        protected override void DidDeactivate(DeactivationType deactivationType) { }
    }
}