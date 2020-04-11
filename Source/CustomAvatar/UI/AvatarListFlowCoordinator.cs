using System.Linq;
using CustomAvatar.Utilities;
using HMUI;
using UnityEngine;
using Zenject;

namespace CustomAvatar.UI
{
    internal class AvatarListFlowCoordinator : FlowCoordinator
    {
        [Inject] private AvatarListViewController _avatarListViewController;
        [Inject] private MirrorViewController _mirrorViewController;
        [Inject] private SettingsViewController _settingsViewController;

        private GameObject _mainScreen;
        private Vector3 _mainScreenScale;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            _mainScreen = GameObject.Find("MainScreen");

            showBackButton = true;

            if (firstActivation)
            {
                title = "Custom Avatars";
                _mainScreenScale = _mainScreen.transform.localScale;
            }

            if (activationType == ActivationType.AddedToHierarchy)
            {
                ProvideInitialViewControllers(_mirrorViewController, _settingsViewController, _avatarListViewController);
                _mainScreen.transform.localScale = Vector3.zero;
            }
        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            if (deactivationType == DeactivationType.RemovedFromHierarchy)
            {
                _mainScreen.transform.localScale = _mainScreenScale;
            }
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            var mainFlowCoordinator = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();
            mainFlowCoordinator.InvokePrivateMethod("DismissFlowCoordinator", this, null, false);
        }
    }
}