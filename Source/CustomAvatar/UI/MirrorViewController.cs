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
            base.DidActivate(firstActivation, activationType);

            if (activationType == ActivationType.AddedToHierarchy)
            {
                _mirrorContainer = new GameObject();
                Vector2 mirrorSize = SettingsManager.settings.mirror.size;
                MirrorHelper.CreateMirror(new Vector3(0, mirrorSize.y / 2, 3f) + SettingsManager.settings.mirror.positionOffset, Quaternion.Euler(-90f, 0, 0), mirrorSize, _mirrorContainer.transform, new Vector3(0, mirrorSize.y / 2, 1.5f));
            }
        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            base.DidDeactivate(deactivationType);

            Destroy(_mirrorContainer);
        }
    }
}