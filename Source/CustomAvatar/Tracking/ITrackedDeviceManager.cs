using System;

namespace CustomAvatar.Tracking
{
    internal interface ITrackedDeviceManager
    {
        event Action<ITrackedDeviceState> deviceAdded;
        event Action<ITrackedDeviceState> deviceRemoved;
        event Action<ITrackedDeviceState> deviceTrackingAcquired;
        event Action<ITrackedDeviceState> deviceTrackingLost;

        bool TryGetDeviceState(DeviceUse use, out ITrackedDeviceState deviceState);
    }
}
