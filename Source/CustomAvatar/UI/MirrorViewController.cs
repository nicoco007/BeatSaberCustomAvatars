using CustomAvatar.StereoRendering;
using UnityEngine;
using HMUI;

namespace CustomAvatar.UI
{
    internal class MirrorViewController : ViewController
    {
        private static readonly Vector3 kMirrorPosition = new Vector3(0, 1f, 1.5f);
        private static readonly Quaternion kMirrorRotation = Quaternion.Euler(-90f, 0, 0);
        private static readonly Vector2 kMirrorScale = new Vector2(5f, 2f);

        private GameObject _mirrorContainer;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            _mirrorContainer = new GameObject();

            base.DidActivate(firstActivation, activationType);

            if (firstActivation)
            {
                MirrorHelper.CreateMirror(kMirrorPosition, kMirrorRotation, kMirrorScale, _mirrorContainer.transform);
            }
        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            base.DidDeactivate(deactivationType);

            Destroy(_mirrorContainer);
        }
    }
}