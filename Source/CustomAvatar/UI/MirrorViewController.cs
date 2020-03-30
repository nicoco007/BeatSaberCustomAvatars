using CustomAvatar.StereoRendering;
using CustomAvatar.Utilities;
using UnityEngine;
using HMUI;

namespace CustomAvatar.UI
{
    internal class MirrorViewController : ViewController
    {
        private GameObject _mirrorContainer;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            _mirrorContainer = new GameObject();

            base.DidActivate(firstActivation, activationType);

            if (firstActivation)
            {
                Vector2 mirrorSize = SettingsManager.settings.mirrorSize;
                MirrorHelper.CreateMirror(new Vector3(0, mirrorSize.y / 2, 1.5f), Quaternion.Euler(-90f, 0, 0), mirrorSize, _mirrorContainer.transform);
            }
        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            base.DidDeactivate(deactivationType);

            Destroy(_mirrorContainer);
        }
    }
}