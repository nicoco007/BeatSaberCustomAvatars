using UnityEngine;

namespace CustomAvatar.Tracking
{
    internal class VRAvatarInput : AvatarInput
    {
        private readonly TrackedDeviceManager _deviceManager;

        public VRAvatarInput(TrackedDeviceManager trackedDeviceManager)
        {
            _deviceManager = trackedDeviceManager;
            _deviceManager.deviceAdded += (device, use) => InvokeInputChanged();
            _deviceManager.deviceRemoved += (device, use) => InvokeInputChanged();
            _deviceManager.deviceTrackingAcquired += (device, use) => InvokeInputChanged();
            _deviceManager.deviceTrackingLost += (device, use) => InvokeInputChanged();
        }

        public override bool TryGetHeadPose(out Pose pose) => TryGetPose(_deviceManager.head, out pose);
        public override bool TryGetLeftHandPose(out Pose pose) => TryGetPose(_deviceManager.leftHand, out pose);
        public override bool TryGetRightHandPose(out Pose pose) => TryGetPose(_deviceManager.rightHand, out pose);
        public override bool TryGetWaistPose(out Pose pose) => TryGetPose(_deviceManager.waist, out pose);
        public override bool TryGetLeftFootPose(out Pose pose) => TryGetPose(_deviceManager.leftFoot, out pose);
        public override bool TryGetRightFootPose(out Pose pose) => TryGetPose(_deviceManager.rightFoot, out pose);

        private bool TryGetPose(TrackedDeviceState device, out Pose pose)
        {
            if (!device.found || !device.tracked)
            {
                pose = Pose.identity;
                return false;
            }

            pose = new Pose(device.position, device.rotation);
            return true;
        }
    }
}
