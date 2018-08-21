using UnityEngine;
using UnityEngine.XR;

namespace CustomAvatar
{
	public class PlayerAvatarInput : IAvatarInput
	{	
		public PosRot HeadPosRot
		{
			get { return GetXRNodeWorldPosRot(XRNode.Head); }
		}
		
		public PosRot LeftPosRot
		{
			get { return GetXRNodeWorldPosRot(XRNode.LeftHand); }
		}
		
		public PosRot RightPosRot
		{
			get { return GetXRNodeWorldPosRot(XRNode.RightHand); }
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
	}
}