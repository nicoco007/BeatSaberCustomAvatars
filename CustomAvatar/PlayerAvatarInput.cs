using UnityEngine;
using UnityEngine.XR;

namespace CustomAvatar
{
	public class PlayerAvatarInput : IAvatarInput
	{
		private readonly MainSettingsModel _mainSettingsModel;
		
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

		public PlayerAvatarInput(MainSettingsModel mainSettingsModel)
		{
			_mainSettingsModel = mainSettingsModel;
		}

		private PosRot GetXRNodeWorldPosRot(XRNode node)
		{
			var pos = InputTracking.GetLocalPosition(node);
			var rot = InputTracking.GetLocalRotation(node);

			var roomCenter = _mainSettingsModel == null ? Vector3.zero : _mainSettingsModel.roomCenter;
			var roomRotation = _mainSettingsModel == null
				? Quaternion.identity
				: Quaternion.Euler(new Vector3(0f, _mainSettingsModel.roomRotation, 0f));
			pos += roomCenter;
			pos = roomRotation * pos;
			rot *= roomRotation;
			return new PosRot(pos, rot);
		}
	}
}