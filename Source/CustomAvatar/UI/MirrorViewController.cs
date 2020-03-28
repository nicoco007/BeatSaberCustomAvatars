using CustomAvatar.StereoRendering;
using System.Collections.Generic;
using UnityEngine;
using HMUI;

namespace CustomAvatar.UI
{
    internal class MirrorViewController : ViewController
    {
        private static readonly Vector3 kMirrorPosition = new Vector3(0, 0, 1.5f); // origin is bottom center
        private static readonly Quaternion kMirrorRotation = Quaternion.Euler(-90f, 0, 0);
        private static readonly Vector3 kMirrorScale = new Vector3(0.50f, 1f, 0.25f);

        private GameObject _mirrorContainer;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            _mirrorContainer = new GameObject();

            base.DidActivate(firstActivation, activationType);

            if (firstActivation)
            {
                StartCoroutine(MirrorHelper.SpawnMirror(kMirrorPosition, kMirrorRotation, kMirrorScale, _mirrorContainer.transform));
            }
        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            base.DidDeactivate(deactivationType);

            Destroy(_mirrorContainer);
        }
    }
}