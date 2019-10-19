using System;

namespace CustomAvatar
{
	class TrackedDeviceTypeAttribute : Attribute
	{
		public string Name { get; set; }

		public TrackedDeviceTypeAttribute(string name)
		{
			Name = name;
		}
	}

	public enum TrackedDeviceType
	{
		[TrackedDeviceType("")] Unknown,
		[TrackedDeviceType("vive")] ViveHeadset,
		[TrackedDeviceType("knuckles")] ValveIndexController,
		[TrackedDeviceType("vive_tracker")] ViveTracker,
		[TrackedDeviceType("vive_tracker_handed")] HeldInHand,
		[TrackedDeviceType("vive_tracker_left_foot")] LeftFoot,
		[TrackedDeviceType("vive_tracker_right_foot")] RightFoot,
		[TrackedDeviceType("vive_tracker_left_shoulder")] LeftShoulder,
		[TrackedDeviceType("vive_tracker_right_shoulder")] RightShoulder,
		[TrackedDeviceType("vive_tracker_waist")] Waist,
		[TrackedDeviceType("vive_tracker_chest")] Chest,
		[TrackedDeviceType("vive_tracker_camera")] Camera,
		[TrackedDeviceType("vive_tracker_keyboard")] Keyboard
	}
}
