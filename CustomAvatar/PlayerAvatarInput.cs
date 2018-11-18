using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

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
                if(Plugin.IsTrackerAsHand && Plugin.Trackers.Capacity >= 2)
                    return GetTrackerWorldPosRot(Plugin.Trackers[0]);
                else
                    return GetXRNodeWorldPosRot(XRNode.LeftHand);
            }
		}
		
		public PosRot RightPosRot
		{
			get
            {
                if (Plugin.IsTrackerAsHand && Plugin.Trackers.Capacity >= 2)
                    return GetTrackerWorldPosRot(Plugin.Trackers[1]);
                else
                    return GetXRNodeWorldPosRot(XRNode.RightHand);
            }
        }

        public PosRot LeftLegPosRot
        {
            get
            {
                if (Plugin.IsFullBodyTracking && Plugin.Trackers.Capacity >= 2)
                    return GetTrackerWorldPosRot(Plugin.Trackers[0]);
                else
                    return new PosRot(new Vector3(), new Quaternion());
            }
        }

        public PosRot RightLegPosRot
        {
            get
            {
                if (Plugin.IsFullBodyTracking && Plugin.Trackers.Capacity >= 2)
                    return GetTrackerWorldPosRot(Plugin.Trackers[1]);
                else
                    return new PosRot(new Vector3(), new Quaternion());
            }
        }

        public PosRot PelvisPosRot
        {
            get
            {
                if (Plugin.IsFullBodyTracking && Plugin.Trackers.Capacity >= 3)
                    return GetTrackerWorldPosRot(Plugin.Trackers[3]);
                else
                    return new PosRot(new Vector3(), new Quaternion());
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
                Console.WriteLine(e.Message + "\n" + e.StackTrace);
            }
            return new PosRot(pos, rot);
        }
    }
}