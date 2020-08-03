using System;

namespace CustomAvatar.Tracking
{
    internal interface ITrackedDeviceManager
    {
        ITrackedDeviceState head { get; }
        ITrackedDeviceState leftHand { get; }
        ITrackedDeviceState rightHand { get; }
        ITrackedDeviceState leftFoot { get; }
        ITrackedDeviceState rightFoot { get; }
        ITrackedDeviceState waist { get; }

        event Action<ITrackedDeviceState> deviceAdded;
        event Action<ITrackedDeviceState> deviceRemoved;
        event Action<ITrackedDeviceState> deviceTrackingAcquired;
        event Action<ITrackedDeviceState> deviceTrackingLost;
    }
}
