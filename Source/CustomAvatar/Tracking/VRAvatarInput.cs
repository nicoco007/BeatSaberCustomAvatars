using UnityEngine;

namespace CustomAvatar.Tracking
{
    internal class VRAvatarInput : AvatarInput
    {
        private readonly TrackedDeviceManager _deviceManager = PersistentSingleton<TrackedDeviceManager>.instance;

        public VRAvatarInput()
        {
            _deviceManager.deviceAdded += (device) => InvokeInputChanged();
            _deviceManager.deviceRemoved += (device) => InvokeInputChanged();
        }

        public override bool TryGetHeadPose(out Pose pose) => TryGetPose(_deviceManager.head, out pose);
        public override bool TryGetLeftHandPose(out Pose pose) => TryGetPose(_deviceManager.leftHand, out pose);
        public override bool TryGetRightHandPose(out Pose pose) => TryGetPose(_deviceManager.rightHand, out pose);
        public override bool TryGetWaistPose(out Pose pose) => TryGetPose(_deviceManager.waist, out pose);
        public override bool TryGetLeftFootPose(out Pose pose) => TryGetPose(_deviceManager.leftFoot, out pose);
        public override bool TryGetRightFootPose(out Pose pose) => TryGetPose(_deviceManager.rightFoot, out pose);

        private bool TryGetPose(TrackedDeviceState device, out Pose pose)
        {
            if (!device.found)
            {
                pose = Pose.identity;
                return false;
            }

            pose = new Pose(device.position, device.rotation);
            return true;
        }
    }
}
