using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Logger = CustomAvatar.Util.Logger;

namespace CustomAvatar
{
	public class PlayerAvatarInput : IAvatarFullBodyInput
	{	
		public PlayerAvatarInput()
		{
		}

		public PosRot HeadPosRot
		{
			get { return GetXRNodeWorldPosRot(XRNode.Head); }
		}
		
		public PosRot LeftPosRot
		{
			get
			{
				if(Plugin.IsTrackerAsHand && Plugin.Trackers.Count >= 2)
					return GetTrackerWorldPosRot(Plugin.Trackers[0]);
				else
					return GetXRNodeWorldPosRot(XRNode.LeftHand);
			}
		}
		
		public PosRot RightPosRot
		{
			get
			{
				if (Plugin.IsTrackerAsHand && Plugin.Trackers.Count >= 2)
					return GetTrackerWorldPosRot(Plugin.Trackers[1]);
				else
					return GetXRNodeWorldPosRot(XRNode.RightHand);
			}
		}

		public PosRot LeftLegPosRot
		{
			get
			{
				if (Plugin.FullBodyTrackingType >= Plugin.TrackingType.Feet && Plugin.Trackers.Count >= 2)
				{
					return GetTrackerWorldPosRot(Plugin.Trackers[0]);
				}
				else
					return new PosRot(new Vector3(), new Quaternion());
			}
		}

		public PosRot RightLegPosRot
		{
			get
			{
				if (Plugin.FullBodyTrackingType >= Plugin.TrackingType.Feet && Plugin.Trackers.Count >= 2)
				{
					return GetTrackerWorldPosRot(Plugin.Trackers[1]);
				}
				else
					return new PosRot(new Vector3(), new Quaternion());
			}
		}

		public PosRot PelvisPosRot
		{
			get
			{
				if (Plugin.FullBodyTrackingType == Plugin.TrackingType.Hips && Plugin.Trackers.Count >= 1)
				{
					return GetTrackerWorldPosRot(Plugin.Trackers[0]);
				}
				else
				{
					if (Plugin.FullBodyTrackingType == Plugin.TrackingType.Full && Plugin.Trackers.Count >= 3)
					{
						return GetTrackerWorldPosRot(Plugin.Trackers[2]);
					}
					else
						return new PosRot(new Vector3(), new Quaternion());
				}
			}
		}

		private static PosRot GetXRNodeWorldPosRot(XRNode node)
		{
			var pos = InputTracking.GetLocalPosition(node);
			var rot = InputTracking.GetLocalRotation(node);

			var roomCenter = BeatSaberUtil.GetRoomCenter();
			var roomRotation = BeatSaberUtil.GetRoomRotation();
			pos = roomRotation * pos;
			pos += roomCenter;
			rot = roomRotation * rot;
			return new PosRot(pos, rot);
		}

		private static PosRot GetTrackerWorldPosRot(XRNodeState tracker)
		{
			Vector3 pos = new Vector3();
			Quaternion rot = new Quaternion();
			try
			{
				var notes = new List<XRNodeState>();
				InputTracking.GetNodeStates(notes);
				foreach (XRNodeState note in notes)
				{
					if (note.uniqueID != tracker.uniqueID)
						continue;
					if (note.TryGetPosition(out pos) && note.TryGetRotation(out rot))
					{
						var roomCenter = BeatSaberUtil.GetRoomCenter();
						var roomRotation = BeatSaberUtil.GetRoomRotation();
						pos = roomRotation * pos;
						pos += roomCenter;
						rot = roomRotation * rot;
					}
				}
			}
			catch (Exception e)
			{
				Logger.Log(e.Message + "\n" + e.StackTrace, Logger.LogLevel.Error);
			}
			return new PosRot(pos, rot);
		}
	}
}
